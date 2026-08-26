using PatientSurvey.Application.Common;
using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.Domain.Entities;
using System.Security.Cryptography;
using PatientSurvey.Application.Interfaces;

namespace PatientSurvey.Application.Services;

public sealed class SurveyAccessTokenService
{
    private readonly ISurveyAccessTokenRepository _repository;
    private readonly IClock _clock;
    private readonly PermissionService? _permissionService;

    public SurveyAccessTokenService(
        ISurveyAccessTokenRepository repository,
        IClock clock,
        PermissionService? permissionService = null)
    {
        _repository = repository;
        _clock = clock;
        _permissionService = permissionService;
    }

    public async Task<IReadOnlyCollection<SurveyAccessTokenListItemDto>> GetTokensAsync(
        CancellationToken cancellationToken = default)
    {
        var canViewPatientPersonalData = _permissionService is not null
            && await _permissionService.CanCurrentUserViewPatientPersonalDataAsync("Anket linkleri", cancellationToken);
        var tokens = await _repository.GetAllTokensWithSurveysAsync(canViewPatientPersonalData, cancellationToken);
        return tokens
            .OrderByDescending(token => token.CreatedAtUtc)
            .Select(token => new SurveyAccessTokenListItemDto(
                token.Id,
                token.SurveyId,
                token.SurveyInvitationId,
                token.Survey?.Title ?? string.Empty,
                token.Token,
                token.CreatedAtUtc,
                token.ExpiresAtUtc,
                token.UsedAtUtc,
                token.SurveyResponse is not null,
                token.SurveyInvitation?.DeliveryStatus.ToString() ?? "Legacy",
                token.Survey?.DoctorId,
                token.Survey?.DepartmentId,
                token.Survey?.DoctorId is null && token.Survey?.DepartmentId is null,
                canViewPatientPersonalData
                    ? FormatPatientName(
                        token.SurveyInvitation?.PatientVisit?.Patient,
                        token.SurveyInvitation?.PatientVisit?.PatientId ?? 0)
                    : FormatPatientReference(token.SurveyInvitation?.PatientVisit?.PatientId ?? 0),
                canViewPatientPersonalData
                    ? NormalizePatientInfo(token.SurveyInvitation?.PatientVisit?.Patient?.PhoneNumber)
                    : null,
                canViewPatientPersonalData
                    ? NormalizePatientInfo(token.SurveyInvitation?.PatientVisit?.Patient?.Email)
                    : null))
            .ToArray();
    }

    public async Task<ServiceResult<SurveyAccessTokenListItemDto>> CreateTokenAsync(
        CreateSurveyAccessTokenRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var survey = await _repository.GetSurveyByIdAsync(request.SurveyId, cancellationToken);
        if (survey is null)
        {
            return ServiceResult<SurveyAccessTokenListItemDto>.Failure("survey_not_found", "Anket bulunamadı.");
        }

        if (!survey.IsActive)
        {
            return ServiceResult<SurveyAccessTokenListItemDto>.Failure("survey_inactive", "Pasif anket için link oluşturulamaz.");
        }

        if (request.ExpiresAtUtc.HasValue && request.ExpiresAtUtc.Value <= _clock.UtcNow)
        {
            return ServiceResult<SurveyAccessTokenListItemDto>.Failure("expires_invalid", "Son kullanma tarihi gelecekte olmalıdır.");
        }

        var tokenValue = await GenerateUniqueTokenAsync(cancellationToken);
        var token = new SurveyAccessToken
        {
            SurveyId = survey.Id,
            Token = tokenValue,
            CreatedAtUtc = _clock.UtcNow,
            ExpiresAtUtc = request.ExpiresAtUtc
        };

        _repository.AddToken(token);
        await _repository.SaveChangesAsync(cancellationToken);

        return ServiceResult<SurveyAccessTokenListItemDto>.Success(new SurveyAccessTokenListItemDto(
            token.Id,
            survey.Id,
            token.SurveyInvitationId,
            survey.Title,
            token.Token,
            token.CreatedAtUtc,
            token.ExpiresAtUtc,
            token.UsedAtUtc,
            HasResponse: false,
            DeliveryStatus: "Legacy",
            SurveyDoctorId: survey.DoctorId,
            SurveyDepartmentId: survey.DepartmentId,
            IsGeneralSurvey: survey.DoctorId is null && survey.DepartmentId is null));
    }

    private static string FormatPatientName(Patient? patient, int patientId)
    {
        if (patient is null)
        {
            return FormatPatientReference(patientId);
        }

        var fullName = $"{patient.FirstName} {patient.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? FormatPatientReference(patientId) : fullName;
    }

    private static string FormatPatientReference(int patientId)
    {
        return patientId > 0 ? $"Hasta #{patientId}" : "Anonim";
    }

    private static string? NormalizePatientInfo(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    public async Task<string> GenerateUniqueTokenAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-", StringComparison.Ordinal)
                .Replace("/", "_", StringComparison.Ordinal)
                .TrimEnd('=');

            if (!await _repository.TokenExistsAsync(token, cancellationToken))
            {
                return token;
            }
        }

        throw new InvalidOperationException("Unable to generate a unique survey access token.");
    }
}
