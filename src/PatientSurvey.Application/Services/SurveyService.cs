using PatientSurvey.Application.Common;
using PatientSurvey.Application.DTOs.Department;
using PatientSurvey.Application.DTOs.Doctor;
using PatientSurvey.Application.DTOs.Question;
using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Domain.Entities;
using SurveyEntity = PatientSurvey.Domain.Entities.Survey;

namespace PatientSurvey.Application.Services;

public sealed class SurveyService
{
    private readonly ISurveyReadRepository _repository;
    private readonly IAdminSurveyRepository _adminRepository;
    private readonly IClock _clock;
    private readonly ISurveyInvitationRepository? _invitationRepository;
    private readonly IPatientIdentityProtector? _patientIdentityProtector;
    private readonly IKvkkNoticeProvider? _kvkkNoticeProvider;

    public SurveyService(
        ISurveyReadRepository repository,
        IAdminSurveyRepository adminRepository,
        IClock clock,
        ISurveyInvitationRepository? invitationRepository = null,
        IPatientIdentityProtector? patientIdentityProtector = null,
        IKvkkNoticeProvider? kvkkNoticeProvider = null)
    {
        _repository = repository;
        _adminRepository = adminRepository;
        _clock = clock;
        _invitationRepository = invitationRepository;
        _patientIdentityProtector = patientIdentityProtector;
        _kvkkNoticeProvider = kvkkNoticeProvider;
    }

    public async Task<ServiceResult<SurveyFormDto>> GetSurveyFormAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var accessToken = await GetUsableTokenAsync(token, cancellationToken);
        if (!accessToken.IsSuccess || accessToken.Value?.Survey is null)
        {
            return Failure(accessToken.ErrorCode ?? "invalid_token", accessToken.Message ?? "Bu anket bağlantısı geçersiz veya artık kullanılamıyor.");
        }

        var departments = accessToken.Value.SurveyInvitationId.HasValue
            ? Array.Empty<Department>()
            : await _repository.GetActiveDepartmentsAsync(cancellationToken);

        return ServiceResult<SurveyFormDto>.Success(ToFormDto(accessToken.Value, departments));
    }

    public async Task<ServiceResult<SurveyFormDto>> GetVerifiedSurveyFormAsync(
        string token,
        int verifiedInvitationId,
        CancellationToken cancellationToken = default)
    {
        var accessToken = await GetUsableTokenAsync(token, cancellationToken);
        if (!accessToken.IsSuccess || accessToken.Value?.Survey is null)
        {
            return Failure(accessToken.ErrorCode ?? "invalid_token", accessToken.Message ?? "Bu anket bağlantısı geçersiz veya artık kullanılamıyor.");
        }

        if (accessToken.Value.SurveyInvitationId != verifiedInvitationId)
        {
            return Failure("identity_required", "Bu anketi görüntülemek için kimlik doğrulama gereklidir.");
        }

        return ServiceResult<SurveyFormDto>.Success(ToFormDto(accessToken.Value, Array.Empty<Department>()));
    }

    public async Task<ServiceResult<SurveyEntryDto>> GetSurveyEntryAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var accessToken = await GetUsableTokenAsync(token, cancellationToken);
        if (!accessToken.IsSuccess || accessToken.Value?.SurveyInvitation is null || accessToken.Value.Survey is null)
        {
            return ServiceResult<SurveyEntryDto>.Failure(
                accessToken.ErrorCode ?? "invalid_token",
                accessToken.Message ?? "Bu anket bağlantısı geçersiz veya artık kullanılamıyor.");
        }

        var notice = _kvkkNoticeProvider?.GetCurrentNotice()
            ?? new KvkkNoticeDto("1.0", "KVKK Aydınlatma Metni yapılandırılmamış.");

        return ServiceResult<SurveyEntryDto>.Success(new SurveyEntryDto(
            accessToken.Value.Token,
            accessToken.Value.SurveyInvitation.Id,
            accessToken.Value.Survey.Title,
            accessToken.Value.Survey.Description,
            notice.Version,
            notice.Text));
    }

    public async Task<ServiceResult<VerifySurveyIdentityResultDto>> VerifyPatientIdentityAsync(
        VerifySurveyIdentityRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!request.KvkkAccepted)
        {
            return ServiceResult<VerifySurveyIdentityResultDto>.Failure(
                "kvkk_required",
                "Devam etmek için aydınlatma/onay adımını tamamlayın.");
        }

        if (_patientIdentityProtector is null)
        {
            return ServiceResult<VerifySurveyIdentityResultDto>.Failure(
                "identity_protector_missing",
                "Kimlik doğrulama yapılandırması eksik.");
        }

        var normalizedTc = _patientIdentityProtector.NormalizeTcIdentityNumber(request.TcIdentityNumber);
        if (!_patientIdentityProtector.IsValidTcIdentityNumber(normalizedTc))
        {
            return IdentityMismatch();
        }

        var accessToken = await GetUsableTokenAsync(request.Token, cancellationToken);
        if (!accessToken.IsSuccess || accessToken.Value?.SurveyInvitation?.PatientVisit?.Patient is null)
        {
            return ServiceResult<VerifySurveyIdentityResultDto>.Failure(
                accessToken.ErrorCode ?? "invalid_token",
                accessToken.Message ?? "Bu anket bağlantısı geçersiz veya artık kullanılamıyor.");
        }

        var lookupHash = _patientIdentityProtector.CreateLookupHash(normalizedTc);
        if (!string.Equals(
                lookupHash,
                accessToken.Value.SurveyInvitation.PatientVisit.Patient.TcIdentityLookupHash,
                StringComparison.Ordinal))
        {
            return IdentityMismatch();
        }

        var notice = _kvkkNoticeProvider?.GetCurrentNotice()
            ?? new KvkkNoticeDto("1.0", string.Empty);

        return ServiceResult<VerifySurveyIdentityResultDto>.Success(new VerifySurveyIdentityResultDto(
            accessToken.Value.Token,
            accessToken.Value.SurveyInvitation.Id,
            notice.Version));
    }

    public async Task<IReadOnlyCollection<AdminSurveyListItemDto>> GetAdminSurveysAsync(
        CancellationToken cancellationToken = default)
    {
        var surveys = await _adminRepository.GetAllSurveysWithQuestionsAsync(cancellationToken);
        return surveys
            .OrderBy(survey => survey.Title)
            .Select(survey =>
            {
                var scoreAnswers = survey.AccessTokens
                    .Where(token => token.SurveyResponse is not null)
                    .SelectMany(token => token.SurveyResponse!.Answers)
                    .Where(answer => answer.ScoreValue.HasValue)
                    .Select(answer => answer.ScoreValue!.Value)
                    .ToArray();

                return new AdminSurveyListItemDto(
                    survey.Id,
                    survey.Title,
                    survey.Description,
                    survey.IsActive,
                    survey.CreatedAtUtc,
                    survey.DoctorId is null && survey.DepartmentId is null,
                    survey.DepartmentId,
                    survey.Department?.Name,
                    survey.DoctorId,
                    survey.Doctor is null ? null : $"Dr. {survey.Doctor.FirstName} {survey.Doctor.LastName}",
                    survey.Questions.Count,
                    survey.AccessTokens.Count,
                    survey.AccessTokens.Count(token => token.SurveyResponse is not null),
                    scoreAnswers.Length == 0 ? null : Math.Round(scoreAnswers.Average(), 2));
            })
            .ToArray();
    }

    public async Task<ServiceResult<int>> CreateSurveyAsync(
        CreateSurveyRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var title = request.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return ServiceResult<int>.Failure("title_required", "Anket başlığı zorunludur.");
        }

        var scopeResult = await ResolveSurveyScopeAsync(request, cancellationToken);
        if (!scopeResult.IsSuccess)
        {
            return ServiceResult<int>.Failure(
                scopeResult.ErrorCode ?? "scope_invalid",
                scopeResult.Message ?? "Anket kapsamı geçersiz.");
        }

        var scope = scopeResult.Value!;
        var survey = new SurveyEntity
        {
            Title = title,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsActive = request.IsActive,
            DoctorId = scope.DoctorId,
            DepartmentId = scope.DepartmentId,
            CreatedAtUtc = _clock.UtcNow
        };

        _adminRepository.AddSurvey(survey);
        await _adminRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<int>.Success(survey.Id);
    }

    public async Task<IReadOnlyCollection<DepartmentDto>> GetActiveDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        var departments = _invitationRepository is not null
            ? await _invitationRepository.GetActiveDepartmentsAsync(cancellationToken)
            : await _repository.GetActiveDepartmentsAsync(cancellationToken);

        return departments
            .OrderBy(department => department.Name)
            .Select(department => new DepartmentDto(department.Id, department.Name))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<DoctorOptionDto>> GetActiveDoctorsByDepartmentAsync(
        int departmentId,
        CancellationToken cancellationToken = default)
    {
        if (_invitationRepository is null)
        {
            return Array.Empty<DoctorOptionDto>();
        }

        var doctors = await _invitationRepository.GetActiveDoctorsByDepartmentAsync(departmentId, cancellationToken);
        return doctors
            .Select(doctor => new DoctorOptionDto(
                doctor.Id,
                doctor.DepartmentId,
                $"Dr. {doctor.FirstName} {doctor.LastName}"))
            .ToArray();
    }

    public async Task<ServiceResult> ToggleSurveyStatusAsync(
        int surveyId,
        CancellationToken cancellationToken = default)
    {
        var survey = await _adminRepository.GetSurveyByIdAsync(surveyId, cancellationToken);
        if (survey is null)
        {
            return ServiceResult.Failure("survey_not_found", "Anket bulunamadı.");
        }

        survey.IsActive = !survey.IsActive;
        await _adminRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    private async Task<ServiceResult<SurveyScope>> ResolveSurveyScopeAsync(
        CreateSurveyRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.Equals(request.CreatedByRole, "Doctor", StringComparison.OrdinalIgnoreCase))
        {
            if (_invitationRepository is null)
            {
                return ServiceResult<SurveyScope>.Failure("scope_repository_missing", "Anket kapsamı doğrulanamadı.");
            }

            if (request.IsGeneral)
            {
                return ServiceResult<SurveyScope>.Failure("doctor_general_not_allowed", "Doktorlar yalnızca kendi bölümü için hedefli anket oluşturabilir.");
            }

            if (!request.CreatedByUserId.HasValue)
            {
                return ServiceResult<SurveyScope>.Failure("doctor_user_required", "Doktor kullanıcısı doğrulanamadı.");
            }

            var currentDoctor = await _invitationRepository.GetDoctorByUserIdAsync(request.CreatedByUserId.Value, cancellationToken);
            if (currentDoctor?.Department is null || !currentDoctor.IsActive || !currentDoctor.Department.IsActive)
            {
                return ServiceResult<SurveyScope>.Failure("doctor_inactive", "Pasif doktor veya pasif bölüm için anket oluşturulamaz.");
            }

            return ServiceResult<SurveyScope>.Success(new SurveyScope(currentDoctor.Id, currentDoctor.DepartmentId));
        }

        if (request.IsGeneral)
        {
            return ServiceResult<SurveyScope>.Success(new SurveyScope(null, null));
        }

        if (_invitationRepository is null)
        {
            return ServiceResult<SurveyScope>.Failure("scope_repository_missing", "Anket kapsamı doğrulanamadı.");
        }

        if (!request.DepartmentId.HasValue)
        {
            return ServiceResult<SurveyScope>.Failure("department_required", "Hedefli anket için bölüm zorunludur.");
        }

        if (!request.DoctorId.HasValue)
        {
            return ServiceResult<SurveyScope>.Failure("doctor_required", "Hedefli anket için doktor zorunludur.");
        }

        var doctor = await _invitationRepository.GetDoctorByIdAsync(request.DoctorId.Value, cancellationToken);
        if (doctor?.Department is null || !doctor.IsActive || !doctor.Department.IsActive)
        {
            return ServiceResult<SurveyScope>.Failure("doctor_inactive", "Pasif doktor veya pasif bölüm için anket oluşturulamaz.");
        }

        if (doctor.DepartmentId != request.DepartmentId.Value)
        {
            return ServiceResult<SurveyScope>.Failure("doctor_department_mismatch", "Doktor seçilen bölüme bağlı değil.");
        }

        return ServiceResult<SurveyScope>.Success(new SurveyScope(doctor.Id, doctor.DepartmentId));
    }

    private async Task<ServiceResult<SurveyAccessToken>> GetUsableTokenAsync(
        string token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return ServiceResult<SurveyAccessToken>.Failure("invalid_token", "Bu anket bağlantısı geçersiz veya artık kullanılamıyor.");
        }

        var accessToken = await _repository.GetTokenWithActiveSurveyAsync(token.Trim(), cancellationToken);
        if (accessToken?.Survey is null || accessToken.UsedAtUtc.HasValue || accessToken.SurveyResponse is not null)
        {
            return ServiceResult<SurveyAccessToken>.Failure("invalid_token", "Bu anket bağlantısı geçersiz veya artık kullanılamıyor.");
        }

        if (accessToken.ExpiresAtUtc.HasValue && accessToken.ExpiresAtUtc.Value <= _clock.UtcNow)
        {
            return ServiceResult<SurveyAccessToken>.Failure("expired_token", "Bu anket bağlantısının kullanım süresi dolmuş.");
        }

        return ServiceResult<SurveyAccessToken>.Success(accessToken);
    }

    private SurveyFormDto ToFormDto(SurveyAccessToken accessToken, IReadOnlyCollection<Department> departments)
    {
        var survey = accessToken.Survey!;
        var notice = accessToken.SurveyInvitationId.HasValue
            ? _kvkkNoticeProvider?.GetCurrentNotice() ?? new KvkkNoticeDto("1.0", "KVKK Aydınlatma Metni yapılandırılmamış.")
            : null;
        var questions = survey.Questions
            .Where(question => question.IsActive)
            .OrderBy(question => question.DisplayOrder)
            .Select(question => new SurveyQuestionDto(
                question.Id,
                question.Text,
                question.Type,
                question.IsRequired,
                question.DisplayOrder))
            .ToArray();

        return new SurveyFormDto(
            accessToken.Token,
            accessToken.SurveyInvitationId,
            survey.Id,
            survey.Title,
            survey.Description,
            questions,
            departments.Select(department => new DepartmentDto(department.Id, department.Name)).ToArray(),
            notice?.Version,
            notice?.Text);
    }

    private static ServiceResult<SurveyFormDto> Failure(string code, string message)
    {
        return ServiceResult<SurveyFormDto>.Failure(code, message);
    }

    private static ServiceResult<VerifySurveyIdentityResultDto> IdentityMismatch()
    {
        return ServiceResult<VerifySurveyIdentityResultDto>.Failure(
            "identity_mismatch",
            "Kimlik bilgisi bu anket davetiyle eşleşmedi.");
    }

    private sealed record SurveyScope(int? DoctorId, int? DepartmentId);
}
