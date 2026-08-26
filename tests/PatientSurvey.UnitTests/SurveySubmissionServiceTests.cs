using PatientSurvey.Application.DTOs.Response;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Application.Services;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Domain.Enums;

namespace PatientSurvey.UnitTests;

public sealed class SurveySubmissionServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SubmitAsync_rejects_used_token()
    {
        var repository = FakeSubmissionRepository.Valid(Now);
        repository.AccessToken!.UsedAtUtc = Now.AddMinutes(-5);
        var service = CreateService(repository);

        var result = await service.SubmitAsync(ValidRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal("used_token", result.ErrorCode);
        Assert.True(repository.Transaction!.RolledBack);
        Assert.Empty(repository.AddedResponses);
    }

    [Fact]
    public async Task SubmitAsync_rejects_expired_token()
    {
        var repository = FakeSubmissionRepository.Valid(Now);
        repository.AccessToken!.ExpiresAtUtc = Now.AddMinutes(-1);
        var service = CreateService(repository);

        var result = await service.SubmitAsync(ValidRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal("expired_token", result.ErrorCode);
        Assert.True(repository.Transaction!.RolledBack);
    }

    [Fact]
    public async Task SubmitAsync_rejects_inactive_department()
    {
        var repository = FakeSubmissionRepository.Valid(Now);
        repository.Department!.IsActive = false;
        var service = CreateService(repository);

        var result = await service.SubmitAsync(ValidRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal("inactive_department", result.ErrorCode);
    }

    [Fact]
    public async Task SubmitAsync_rejects_missing_required_question()
    {
        var repository = FakeSubmissionRepository.Valid(Now);
        var service = CreateService(repository);

        var result = await service.SubmitAsync(new SubmitSurveyRequestDto("token-1", 1, Array.Empty<SubmitAnswerDto>()));

        Assert.False(result.IsSuccess);
        Assert.Equal("required_question_missing", result.ErrorCode);
    }

    [Fact]
    public async Task SubmitAsync_rejects_score_above_five()
    {
        var repository = FakeSubmissionRepository.Valid(Now);
        var service = CreateService(repository);

        var result = await service.SubmitAsync(new SubmitSurveyRequestDto("token-1", 1, new[]
        {
            new SubmitAnswerDto(10, 6, null, null)
        }));

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid_score", result.ErrorCode);
    }

    [Fact]
    public async Task SubmitAsync_rejects_question_from_another_survey()
    {
        var repository = FakeSubmissionRepository.Valid(Now);
        var service = CreateService(repository);

        var result = await service.SubmitAsync(new SubmitSurveyRequestDto("token-1", 1, new[]
        {
            new SubmitAnswerDto(999, 5, null, null)
        }));

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid_question", result.ErrorCode);
    }

    [Fact]
    public async Task SubmitAsync_rejects_duplicate_answer()
    {
        var repository = FakeSubmissionRepository.Valid(Now);
        var service = CreateService(repository);

        var result = await service.SubmitAsync(new SubmitSurveyRequestDto("token-1", 1, new[]
        {
            new SubmitAnswerDto(10, 4, null, null),
            new SubmitAnswerDto(10, 5, null, null)
        }));

        Assert.False(result.IsSuccess);
        Assert.Equal("duplicate_answer", result.ErrorCode);
    }

    [Fact]
    public async Task SubmitAsync_persists_response_answers_and_used_token_atomically()
    {
        var repository = FakeSubmissionRepository.Valid(Now);
        var service = CreateService(repository);

        var result = await service.SubmitAsync(ValidRequest());

        Assert.True(result.IsSuccess);
        Assert.True(repository.Transaction!.Committed);
        Assert.False(repository.Transaction.RolledBack);
        var response = Assert.Single(repository.AddedResponses);
        Assert.Equal(1, response.DepartmentId);
        Assert.Equal(Now, repository.AccessToken!.UsedAtUtc);
        Assert.Collection(response.Answers, answer =>
        {
            Assert.Equal(10, answer.QuestionId);
            Assert.Equal(5, answer.ScoreValue);
        });
    }

    private static SurveySubmissionService CreateService(FakeSubmissionRepository repository)
    {
        return new SurveySubmissionService(repository, new FixedClock(Now));
    }

    private static SubmitSurveyRequestDto ValidRequest()
    {
        return new SubmitSurveyRequestDto("token-1", 1, new[]
        {
            new SubmitAnswerDto(10, 5, null, null)
        });
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class FakeSubmissionRepository : ISurveySubmissionRepository
    {
        public SurveyAccessToken? AccessToken { get; private init; }
        public Department? Department { get; set; }
        public FakeTransaction? Transaction { get; private set; }
        public List<SurveyResponse> AddedResponses { get; } = new();
        public List<SurveyConsent> AddedConsents { get; } = new();

        public static FakeSubmissionRepository Valid(DateTimeOffset now)
        {
            var survey = new Survey
            {
                Id = 100,
                Title = "Memnuniyet",
                IsActive = true,
                Questions = new List<Question>
                {
                    new()
                    {
                        Id = 10,
                        SurveyId = 100,
                        Text = "Hizmeti puanlayın",
                        Type = QuestionType.Score,
                        IsRequired = true,
                        IsActive = true,
                        DisplayOrder = 1
                    }
                }
            };

            return new FakeSubmissionRepository
            {
                Department = new Department { Id = 1, Name = "Kardiyoloji", IsActive = true },
                AccessToken = new SurveyAccessToken
                {
                    Id = 50,
                    SurveyId = survey.Id,
                    Token = "token-1",
                    CreatedAtUtc = now.AddHours(-1),
                    ExpiresAtUtc = now.AddHours(1),
                    Survey = survey
                }
            };
        }

        public Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        {
            Transaction = new FakeTransaction();
            return Task.FromResult<IAppTransaction>(Transaction);
        }

        public Task<SurveyAccessToken?> GetTokenWithSurveyAsync(string token, CancellationToken cancellationToken)
        {
            return Task.FromResult(token == AccessToken?.Token ? AccessToken : null);
        }

        public Task<Department?> GetDepartmentAsync(int departmentId, CancellationToken cancellationToken)
        {
            return Task.FromResult(departmentId == Department?.Id ? Department : null);
        }

        public void AddSurveyResponse(SurveyResponse response)
        {
            response.Id = 500 + AddedResponses.Count;
            AddedResponses.Add(response);
        }

        public void AddSurveyConsent(SurveyConsent consent)
        {
            AddedConsents.Add(consent);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }

    private sealed class FakeTransaction : IAppTransaction
    {
        public bool Committed { get; private set; }
        public bool RolledBack { get; private set; }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            Committed = true;
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken)
        {
            RolledBack = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
