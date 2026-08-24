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

    public SurveyAccessTokenService(ISurveyAccessTokenRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<IReadOnlyCollection<SurveyAccessTokenListItemDto>> GetTokensAsync(
        CancellationToken cancellationToken = default)
    {
        var tokens = await _repository.GetAllTokensWithSurveysAsync(cancellationToken);
        return tokens
            .OrderByDescending(token => token.CreatedAtUtc)
            .Select(token => new SurveyAccessTokenListItemDto(
                token.Id,
                token.SurveyId,
                token.Survey?.Title ?? string.Empty,
                token.Token,
                token.CreatedAtUtc,
                token.ExpiresAtUtc,
                token.UsedAtUtc,
                token.SurveyResponse is not null))
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
            survey.Title,
            token.Token,
            token.CreatedAtUtc,
            token.ExpiresAtUtc,
            token.UsedAtUtc,
            HasResponse: false));
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
