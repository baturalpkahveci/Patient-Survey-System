using PatientSurvey.Application.Common;
using PatientSurvey.Application.DTOs.Response;
using PatientSurvey.Application.Exceptions;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Domain.Enums;

namespace PatientSurvey.Application.Services;

public sealed class SurveySubmissionService
{
    private readonly ISurveySubmissionRepository _repository;
    private readonly IClock _clock;

    public SurveySubmissionService(ISurveySubmissionRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<ServiceResult<SubmitSurveyResultDto>> SubmitAsync(
        SubmitSurveyRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return Failure("invalid_token", "Bu anket bağlantısı geçersiz veya artık kullanılamıyor.");
        }

        await using var transaction = await _repository.BeginTransactionAsync(cancellationToken);

        try
        {
            var accessToken = await _repository.GetTokenWithSurveyAsync(request.Token.Trim(), cancellationToken);
            var validationFailure = ValidateToken(accessToken);
            if (validationFailure is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return validationFailure;
            }

            var invitationValidationFailure = ValidateInvitationState(accessToken!, request);
            if (invitationValidationFailure is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return invitationValidationFailure;
            }

            Department? department = null;
            if (accessToken!.SurveyInvitationId is null)
            {
                if (!request.DepartmentId.HasValue)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Failure("inactive_department", "Lütfen geçerli bir bölüm seçin.");
                }

                department = await _repository.GetDepartmentAsync(request.DepartmentId.Value, cancellationToken);
                if (department is null || !department.IsActive)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Failure("inactive_department", "Lütfen geçerli bir bölüm seçin.");
                }
            }

            var survey = accessToken.Survey!;
            var activeQuestions = survey.Questions
                .Where(question => question.IsActive)
                .ToDictionary(question => question.Id);

            var answerValidationFailure = ValidateAnswers(request.Answers, activeQuestions);
            if (answerValidationFailure is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return answerValidationFailure;
            }

            var response = new SurveyResponse
            {
                TokenId = accessToken.Id,
                DepartmentId = accessToken.SurveyInvitationId.HasValue ? survey.DepartmentId : department!.Id,
                SubmittedAtUtc = _clock.UtcNow
            };

            foreach (var submittedAnswer in request.Answers)
            {
                if (!activeQuestions.ContainsKey(submittedAnswer.QuestionId))
                {
                    continue;
                }

                response.Answers.Add(new Answer
                {
                    QuestionId = submittedAnswer.QuestionId,
                    ScoreValue = submittedAnswer.ScoreValue,
                    TextValue = NormalizeText(submittedAnswer.TextValue),
                    BooleanValue = submittedAnswer.BooleanValue
                });
            }

            accessToken.UsedAtUtc = _clock.UtcNow;
            _repository.AddSurveyResponse(response);
            if (accessToken.SurveyInvitationId.HasValue)
            {
                _repository.AddSurveyConsent(new SurveyConsent
                {
                    SurveyInvitationId = accessToken.SurveyInvitationId.Value,
                    NoticeVersion = request.ConsentNoticeVersion!.Trim(),
                    AcceptedAtUtc = _clock.UtcNow
                });
            }

            await _repository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return ServiceResult<SubmitSurveyResultDto>.Success(
                new SubmitSurveyResultDto(response.Id, response.SubmittedAtUtc));
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

    private ServiceResult<SubmitSurveyResultDto>? ValidateToken(SurveyAccessToken? accessToken)
    {
        if (accessToken?.Survey is null)
        {
            return Failure("invalid_token", "Bu anket bağlantısı geçersiz veya artık kullanılamıyor.");
        }

        if (accessToken.ExpiresAtUtc.HasValue && accessToken.ExpiresAtUtc.Value <= _clock.UtcNow)
        {
            return Failure("expired_token", "Bu anket bağlantısının kullanım süresi dolmuş.");
        }

        if (accessToken.UsedAtUtc.HasValue || accessToken.SurveyResponse is not null)
        {
            return Failure("used_token", "Bu anket daha önce gönderilmiş.");
        }

        if (!accessToken.Survey.IsActive)
        {
            return Failure("inactive_survey", "Bu anket bağlantısı geçersiz veya artık kullanılamıyor.");
        }

        return null;
    }

    private static ServiceResult<SubmitSurveyResultDto>? ValidateInvitationState(
        SurveyAccessToken accessToken,
        SubmitSurveyRequestDto request)
    {
        if (accessToken.SurveyInvitationId is null)
        {
            return null;
        }

        if (request.VerifiedSurveyInvitationId != accessToken.SurveyInvitationId)
        {
            return Failure("identity_required", "Bu anketi göndermek için kimlik doğrulama gereklidir.");
        }

        if (string.IsNullOrWhiteSpace(request.ConsentNoticeVersion))
        {
            return Failure("kvkk_required", "Devam etmek için aydınlatma/onay adımını tamamlayın.");
        }

        if (accessToken.SurveyInvitation?.Consent is not null)
        {
            return Failure("used_token", "Bu anket daha önce gönderilmiş.");
        }

        return null;
    }

    private static ServiceResult<SubmitSurveyResultDto>? ValidateAnswers(
        IReadOnlyCollection<SubmitAnswerDto> submittedAnswers,
        IReadOnlyDictionary<int, Question> activeQuestions)
    {
        var duplicateQuestionId = submittedAnswers
            .GroupBy(answer => answer.QuestionId)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;

        if (duplicateQuestionId.HasValue)
        {
            return Failure("duplicate_answer", "Aynı soru için birden fazla cevap gönderilemez.");
        }

        var submittedByQuestionId = submittedAnswers.ToDictionary(answer => answer.QuestionId);

        foreach (var submittedAnswer in submittedAnswers)
        {
            if (!activeQuestions.TryGetValue(submittedAnswer.QuestionId, out var question))
            {
                return Failure("invalid_question", "Gönderilen cevaplardan biri bu ankete ait değil.");
            }

            var typeFailure = ValidateAnswerType(question, submittedAnswer);
            if (typeFailure is not null)
            {
                return typeFailure;
            }
        }

        var missingRequired = activeQuestions.Values
            .Where(question => question.IsRequired)
            .Any(question => !submittedByQuestionId.TryGetValue(question.Id, out var answer) || IsBlankAnswer(question, answer));

        if (missingRequired)
        {
            return Failure("required_question_missing", "Lütfen zorunlu alanları doldurun.");
        }

        return null;
    }

    private static ServiceResult<SubmitSurveyResultDto>? ValidateAnswerType(Question question, SubmitAnswerDto answer)
    {
        return question.Type switch
        {
            QuestionType.Score when answer.ScoreValue is < 1 or > 5 =>
                Failure("invalid_score", "Puan değerleri 1 ile 5 arasında olmalıdır."),
            QuestionType.Score when answer.ScoreValue is null && question.IsRequired =>
                Failure("required_question_missing", "Lütfen zorunlu alanları doldurun."),
            QuestionType.Score when HasAny(answer.TextValue, answer.BooleanValue) =>
                Failure("invalid_answer_type", "Cevap türü soru türüyle uyumlu değil."),
            QuestionType.Text when HasAny(answer.ScoreValue, answer.BooleanValue) =>
                Failure("invalid_answer_type", "Cevap türü soru türüyle uyumlu değil."),
            QuestionType.Text when string.IsNullOrWhiteSpace(answer.TextValue) && question.IsRequired =>
                Failure("required_question_missing", "Lütfen zorunlu alanları doldurun."),
            QuestionType.Boolean when answer.BooleanValue is null && question.IsRequired =>
                Failure("required_question_missing", "Lütfen zorunlu alanları doldurun."),
            QuestionType.Boolean when HasAny(answer.ScoreValue, answer.TextValue) =>
                Failure("invalid_answer_type", "Cevap türü soru türüyle uyumlu değil."),
            _ => null
        };
    }

    private static bool IsBlankAnswer(Question question, SubmitAnswerDto answer)
    {
        return question.Type switch
        {
            QuestionType.Score => answer.ScoreValue is null,
            QuestionType.Text => string.IsNullOrWhiteSpace(answer.TextValue),
            QuestionType.Boolean => answer.BooleanValue is null,
            _ => true
        };
    }

    private static bool HasAny(params object?[] values) => values.Any(value => value is not null);

    private static string? NormalizeText(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static ServiceResult<SubmitSurveyResultDto> Failure(string code, string message)
    {
        return ServiceResult<SubmitSurveyResultDto>.Failure(code, message);
    }
}
