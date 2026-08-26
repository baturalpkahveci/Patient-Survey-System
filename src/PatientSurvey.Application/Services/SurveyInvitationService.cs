using System.Security.Cryptography;
using PatientSurvey.Application.Common;
using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.Application.Exceptions;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Domain.Enums;

namespace PatientSurvey.Application.Services;

public sealed class SurveyInvitationService
{
    private readonly ISurveyInvitationRepository _repository;
    private readonly IPatientIdentityProtector _patientIdentityProtector;
    private readonly ISmsSender _smsSender;
    private readonly IEmailSender _emailSender;
    private readonly IClock _clock;

    public SurveyInvitationService(
        ISurveyInvitationRepository repository,
        IPatientIdentityProtector patientIdentityProtector,
        ISmsSender smsSender,
        IEmailSender emailSender,
        IClock clock)
    {
        _repository = repository;
        _patientIdentityProtector = patientIdentityProtector;
        _smsSender = smsSender;
        _emailSender = emailSender;
        _clock = clock;
    }

    public async Task<ServiceResult<CreatedSurveyInvitationDto>> CreateInvitationAsync(
        CreateSurveyInvitationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidatePatientInput(request);
        if (validation is not null)
        {
            return validation;
        }

        var normalizedTc = _patientIdentityProtector.NormalizeTcIdentityNumber(request.TcIdentityNumber);
        if (!_patientIdentityProtector.IsValidTcIdentityNumber(normalizedTc))
        {
            return Failure("tc_invalid", "Geçerli bir T.C. Kimlik Numarası girin.");
        }

        if (request.ExpiresAtUtc.HasValue && request.ExpiresAtUtc.Value <= _clock.UtcNow)
        {
            return Failure("expires_invalid", "Son kullanma tarihi gelecekte olmalıdır.");
        }

        var survey = await _repository.GetSurveyByIdAsync(request.SurveyId, trackChanges: false, cancellationToken);
        if (survey is null)
        {
            return Failure("survey_not_found", "Anket bulunamadı.");
        }

        if (!survey.IsActive)
        {
            return Failure("survey_inactive", "Pasif anket için link oluşturulamaz.");
        }

        if (survey.DoctorId.HasValue != survey.DepartmentId.HasValue)
        {
            return Failure("survey_scope_invalid", "Anket kapsamı tutarsız.");
        }

        var tokenValue = await GenerateUniqueTokenAsync(cancellationToken);
        SurveyInvitation invitation;
        Patient patient;
        await using (var transaction = await _repository.BeginTransactionAsync(cancellationToken))
        {
            try
            {
                var patientHash = _patientIdentityProtector.CreateLookupHash(normalizedTc);
                patient = await FindOrCreatePatientAsync(request, patientHash, cancellationToken);

                var visit = new PatientVisit
                {
                    Patient = patient,
                    DoctorId = survey.DoctorId,
                    DepartmentId = survey.DepartmentId,
                    CreatedByUserId = request.CreatedByUserId,
                    ExaminedAtUtc = _clock.UtcNow,
                    CreatedAtUtc = _clock.UtcNow
                };

                invitation = new SurveyInvitation
                {
                    SurveyId = survey.Id,
                    PatientVisit = visit,
                    CreatedByUserId = request.CreatedByUserId,
                    DeliveryMethod = request.DeliveryMethod,
                    DeliveryStatus = SurveyDeliveryStatus.LinkCreated,
                    CreatedAtUtc = _clock.UtcNow
                };

                var token = new SurveyAccessToken
                {
                    SurveyId = survey.Id,
                    SurveyInvitation = invitation,
                    Token = tokenValue,
                    CreatedAtUtc = _clock.UtcNow,
                    ExpiresAtUtc = request.ExpiresAtUtc
                };

                _repository.AddPatientVisit(visit);
                _repository.AddSurveyInvitation(invitation);
                _repository.AddToken(token);
                await _repository.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (BusinessRuleException exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Failure("business_rule_failure", exception.Message);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        var deliveryStatus = await TryDeliverAsync(request, tokenValue, invitation, cancellationToken);
        var message = DeliveryMessage(request.DeliveryMethod, deliveryStatus);
        await _repository.SaveChangesAsync(cancellationToken);

        return ServiceResult<CreatedSurveyInvitationDto>.Success(new CreatedSurveyInvitationDto(
            survey.Id,
            invitation.Id,
            patient.Id,
            tokenValue,
            request.DeliveryMethod,
            invitation.DeliveryStatus,
            message));
    }

    private async Task<Patient> FindOrCreatePatientAsync(
        CreateSurveyInvitationRequestDto request,
        string patientHash,
        CancellationToken cancellationToken)
    {
        var patient = await _repository.GetPatientByTcHashAsync(patientHash, cancellationToken);
        if (patient is null)
        {
            patient = new Patient
            {
                FirstName = request.PatientFirstName.Trim(),
                LastName = request.PatientLastName.Trim(),
                TcIdentityLookupHash = patientHash,
                PhoneNumber = request.PhoneNumber.Trim(),
                Email = request.Email.Trim(),
                CreatedAtUtc = _clock.UtcNow,
                UpdatedAtUtc = _clock.UtcNow
            };
            _repository.AddPatient(patient);
            return patient;
        }

        patient.FirstName = request.PatientFirstName.Trim();
        patient.LastName = request.PatientLastName.Trim();
        patient.PhoneNumber = request.PhoneNumber.Trim();
        patient.Email = request.Email.Trim();
        patient.UpdatedAtUtc = _clock.UtcNow;
        return patient;
    }

    private async Task<SurveyDeliveryStatus> TryDeliverAsync(
        CreateSurveyInvitationRequestDto request,
        string token,
        SurveyInvitation invitation,
        CancellationToken cancellationToken)
    {
        var surveyLink = $"{request.SurveyUrlPrefix}{token}";
        var result = request.DeliveryMethod switch
        {
            SurveyDeliveryMethod.Sms => await _smsSender.SendSurveyLinkAsync(request.PhoneNumber, surveyLink, cancellationToken),
            SurveyDeliveryMethod.Email => await _emailSender.SendSurveyLinkAsync(request.Email, surveyLink, cancellationToken),
            _ => new DeliverySendResult(false, true)
        };

        invitation.SentAtUtc = result.IsSent ? _clock.UtcNow : null;
        invitation.DeliveryStatus = request.DeliveryMethod switch
        {
            SurveyDeliveryMethod.LinkOnly => SurveyDeliveryStatus.LinkCreated,
            _ when result.IsSent => SurveyDeliveryStatus.Sent,
            _ when !result.IsConfigured => SurveyDeliveryStatus.NotConfigured,
            _ => SurveyDeliveryStatus.Failed
        };

        return invitation.DeliveryStatus;
    }

    private static ServiceResult<CreatedSurveyInvitationDto>? ValidatePatientInput(CreateSurveyInvitationRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.PatientFirstName))
        {
            return Failure("patient_first_name_required", "Hasta adı zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(request.PatientLastName))
        {
            return Failure("patient_last_name_required", "Hasta soyadı zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(request.TcIdentityNumber))
        {
            return Failure("tc_required", "T.C. Kimlik Numarası zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return Failure("phone_required", "Telefon zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Failure("email_required", "E-posta zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(request.SurveyUrlPrefix))
        {
            return Failure("survey_url_required", "Anket linki oluşturulamadı.");
        }

        return null;
    }

    private async Task<string> GenerateUniqueTokenAsync(CancellationToken cancellationToken)
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

    private static string DeliveryMessage(SurveyDeliveryMethod method, SurveyDeliveryStatus status)
    {
        return (method, status) switch
        {
            (SurveyDeliveryMethod.Sms, SurveyDeliveryStatus.Sent) => "Link oluşturuldu ve SMS gönderildi.",
            (SurveyDeliveryMethod.Sms, SurveyDeliveryStatus.NotConfigured) => "Link oluşturuldu ancak SMS gönderimi yapılandırılmamış.",
            (SurveyDeliveryMethod.Sms, SurveyDeliveryStatus.Failed) => "Link oluşturuldu ancak SMS gönderilemedi.",
            (SurveyDeliveryMethod.Email, SurveyDeliveryStatus.Sent) => "Link oluşturuldu ve e-posta gönderildi.",
            (SurveyDeliveryMethod.Email, SurveyDeliveryStatus.NotConfigured) => "Link oluşturuldu ancak e-posta gönderimi yapılandırılmamış.",
            (SurveyDeliveryMethod.Email, SurveyDeliveryStatus.Failed) => "Link oluşturuldu ancak e-posta gönderilemedi.",
            _ => "Link oluşturuldu."
        };
    }

    private static ServiceResult<CreatedSurveyInvitationDto> Failure(string code, string message)
    {
        return ServiceResult<CreatedSurveyInvitationDto>.Failure(code, message);
    }
}
