using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Application.Services;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.UnitTests;

public sealed class SurveyAccessTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetTokensAsync_orders_newest_first_and_maps_usage()
    {
        var repository = new FakeSurveyAccessTokenRepository();
        repository.Tokens.Add(new SurveyAccessToken
        {
            Id = 1,
            Token = "old",
            SurveyId = 1,
            Survey = new Survey { Id = 1, Title = "A" },
            CreatedAtUtc = Now.AddDays(-1)
        });
        repository.Tokens.Add(new SurveyAccessToken
        {
            Id = 2,
            Token = "new",
            SurveyId = 2,
            Survey = new Survey { Id = 2, Title = "B" },
            CreatedAtUtc = Now,
            SurveyResponse = new SurveyResponse()
        });

        var result = await CreateService(repository).GetTokensAsync();

        Assert.Equal(new[] { "new", "old" }, result.Select(token => token.Token));
        Assert.True(result.First().HasResponse);
    }

    [Fact]
    public async Task CreateTokenAsync_validates_survey_and_expiration()
    {
        var repository = new FakeSurveyAccessTokenRepository();
        var service = CreateService(repository);

        Assert.Equal("survey_not_found", (await service.CreateTokenAsync(new CreateSurveyAccessTokenRequestDto(99, null))).ErrorCode);

        repository.Surveys.Add(new Survey { Id = 1, Title = "A", IsActive = false });
        Assert.Equal("survey_inactive", (await service.CreateTokenAsync(new CreateSurveyAccessTokenRequestDto(1, null))).ErrorCode);

        repository.Surveys[0].IsActive = true;
        Assert.Equal("expires_invalid", (await service.CreateTokenAsync(new CreateSurveyAccessTokenRequestDto(1, Now))).ErrorCode);
    }

    [Fact]
    public async Task CreateTokenAsync_generates_unique_token_and_persists_it()
    {
        var repository = new FakeSurveyAccessTokenRepository();
        repository.Surveys.Add(new Survey { Id = 1, Title = "A", IsActive = true });
        var service = CreateService(repository);

        var result = await service.CreateTokenAsync(new CreateSurveyAccessTokenRequestDto(1, Now.AddDays(1)));

        Assert.True(result.IsSuccess);
        var token = Assert.Single(repository.AddedTokens);
        Assert.Equal(1, token.SurveyId);
        Assert.Equal(Now, token.CreatedAtUtc);
        Assert.Equal(Now.AddDays(1), token.ExpiresAtUtc);
        Assert.False(string.IsNullOrWhiteSpace(token.Token));
        Assert.DoesNotContain("+", token.Token);
        Assert.DoesNotContain("/", token.Token);
        Assert.DoesNotContain("=", token.Token);
        Assert.Equal(token.Token, result.Value!.Token);
    }

    [Fact]
    public async Task GenerateUniqueTokenAsync_retries_when_collision_occurs()
    {
        var repository = new FakeSurveyAccessTokenRepository { CollisionCount = 2 };
        var service = CreateService(repository);

        var token = await service.GenerateUniqueTokenAsync();

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(3, repository.TokenExistsCallCount);
    }

    private static SurveyAccessTokenService CreateService(FakeSurveyAccessTokenRepository repository)
    {
        return new SurveyAccessTokenService(repository, new FixedClock(Now));
    }

    private sealed class FakeSurveyAccessTokenRepository : ISurveyAccessTokenRepository
    {
        public List<Survey> Surveys { get; } = new();
        public List<SurveyAccessToken> Tokens { get; } = new();
        public List<SurveyAccessToken> AddedTokens { get; } = new();
        public int CollisionCount { get; set; }
        public int TokenExistsCallCount { get; private set; }

        public Task<IReadOnlyCollection<SurveyAccessToken>> GetAllTokensWithSurveysAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<SurveyAccessToken>>(Tokens);
        }

        public Task<Survey?> GetSurveyByIdAsync(int surveyId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Surveys.FirstOrDefault(survey => survey.Id == surveyId));
        }

        public Task<bool> TokenExistsAsync(string token, CancellationToken cancellationToken)
        {
            TokenExistsCallCount++;
            if (CollisionCount <= 0)
            {
                return Task.FromResult(false);
            }

            CollisionCount--;
            return Task.FromResult(true);
        }

        public void AddToken(SurveyAccessToken token)
        {
            token.Id = AddedTokens.Count + 1;
            AddedTokens.Add(token);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }
}
