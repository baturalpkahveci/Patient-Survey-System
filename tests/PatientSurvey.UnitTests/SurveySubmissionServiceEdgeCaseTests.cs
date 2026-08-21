using PatientSurvey.Application.DTOs.Response;
using PatientSurvey.Application.Exceptions;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Application.Services;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Domain.Enums;

namespace PatientSurvey.UnitTests;

public sealed class SurveySubmissionServiceEdgeCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SubmitAsync_rejects_blank_token_without_opening_transaction()
    {
        var repository = FakeSubmissionRepository.Valid(Now);
        var service = CreateService(repository);

        var result = await service.SubmitAsync(new SubmitSurveyRequestDto(" ", 1, Array.Empty<SubmitAnswerDto>()));

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid_token", result.ErrorCode);
        Assert.Equal(0, repository.BeginTransactionCount);
    }

    [Fact]
    public async Task SubmitAsync_rolls_back_for_missing_token_null_survey_inactive_survey_and_existing_response()
    {
        var repository = FakeSubmissionRepository.Valid(Now);
        repository.AccessToken = null;
        Assert.Equal("invalid_token", (await CreateService(repository).SubmitAsync(ValidScoreRequest())).ErrorCode);
        Assert.True(repository.Transaction!.RolledBack);

        repository = FakeSubmissionRepository.Valid(Now);
        repository.AccessToken!.Survey = null;
        Assert.Equal("invalid_token", (await CreateService(repository).SubmitAsync(ValidScoreRequest())).ErrorCode);

        repository = FakeSubmissionRepository.Valid(Now);
        repository.AccessToken!.Survey!.IsActive = false;
        Assert.Equal("inactive_survey", (await CreateService(repository).SubmitAsync(ValidScoreRequest())).ErrorCode);

        repository = FakeSubmissionRepository.Valid(Now);
        repository.AccessToken!.SurveyResponse = new SurveyResponse();
        Assert.Equal("used_token", (await CreateService(repository).SubmitAsync(ValidScoreRequest())).ErrorCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task SubmitAsync_rejects_score_outside_allowed_range(int score)
    {
        var repository = FakeSubmissionRepository.Valid(Now);

        var result = await CreateService(repository).SubmitAsync(new SubmitSurveyRequestDto("token", 1, new[]
        {
            new SubmitAnswerDto(10, score, null, null)
        }));

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid_score", result.ErrorCode);
    }

    [Fact]
    public async Task SubmitAsync_rejects_answer_payload_that_does_not_match_question_type()
    {
        var repository = FakeSubmissionRepository.Valid(Now);

        Assert.Equal("invalid_answer_type", (await CreateService(repository).SubmitAsync(new SubmitSurveyRequestDto("token", 1, new[]
        {
            new SubmitAnswerDto(10, 5, "text", null)
        }))).ErrorCode);

        repository = FakeSubmissionRepository.WithQuestion(QuestionType.Text, required: false);
        Assert.Equal("invalid_answer_type", (await CreateService(repository).SubmitAsync(new SubmitSurveyRequestDto("token", 1, new[]
        {
            new SubmitAnswerDto(10, null, "ok", true)
        }))).ErrorCode);

        repository = FakeSubmissionRepository.WithQuestion(QuestionType.Boolean, required: false);
        Assert.Equal("invalid_answer_type", (await CreateService(repository).SubmitAsync(new SubmitSurveyRequestDto("token", 1, new[]
        {
            new SubmitAnswerDto(10, 1, null, true)
        }))).ErrorCode);
    }

    [Fact]
    public async Task SubmitAsync_allows_optional_blank_answers_but_requires_required_text_and_boolean()
    {
        var repository = FakeSubmissionRepository.WithQuestion(QuestionType.Text, required: false);
        var optionalResult = await CreateService(repository).SubmitAsync(new SubmitSurveyRequestDto("token", 1, Array.Empty<SubmitAnswerDto>()));

        Assert.True(optionalResult.IsSuccess);

        repository = FakeSubmissionRepository.WithQuestion(QuestionType.Text, required: true);
        Assert.Equal("required_question_missing", (await CreateService(repository).SubmitAsync(new SubmitSurveyRequestDto("token", 1, new[]
        {
            new SubmitAnswerDto(10, null, " ", null)
        }))).ErrorCode);

        repository = FakeSubmissionRepository.WithQuestion(QuestionType.Boolean, required: true);
        Assert.Equal("required_question_missing", (await CreateService(repository).SubmitAsync(new SubmitSurveyRequestDto("token", 1, Array.Empty<SubmitAnswerDto>()))).ErrorCode);
    }

    [Fact]
    public async Task SubmitAsync_normalizes_blank_text_to_null_and_persists_boolean_answers()
    {
        var repository = FakeSubmissionRepository.WithQuestions(
            new Question { Id = 10, Type = QuestionType.Text, IsRequired = false, IsActive = true },
            new Question { Id = 11, Type = QuestionType.Boolean, IsRequired = true, IsActive = true });
        var service = CreateService(repository);

        var result = await service.SubmitAsync(new SubmitSurveyRequestDto("token", 1, new[]
        {
            new SubmitAnswerDto(10, null, "   ", null),
            new SubmitAnswerDto(11, null, null, false)
        }));

        Assert.True(result.IsSuccess);
        var response = Assert.Single(repository.AddedResponses);
        Assert.Collection(response.Answers,
            answer => Assert.Null(answer.TextValue),
            answer => Assert.False(answer.BooleanValue));
    }

    [Fact]
    public async Task SubmitAsync_rolls_back_and_rethrows_unexpected_save_exception()
    {
        var repository = FakeSubmissionRepository.Valid(Now);
        repository.ThrowOnSave = true;
        var service = CreateService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitAsync(ValidScoreRequest()));

        Assert.True(repository.Transaction!.RolledBack);
    }

    [Fact]
    public async Task SubmitAsync_converts_business_rule_exception_to_failure()
    {
        var repository = FakeSubmissionRepository.Valid(Now);
        repository.ThrowBusinessRuleOnSave = true;
        var service = CreateService(repository);

        var result = await service.SubmitAsync(ValidScoreRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal("business_rule_failure", result.ErrorCode);
        Assert.True(repository.Transaction!.RolledBack);
    }

    private static SurveySubmissionService CreateService(FakeSubmissionRepository repository)
    {
        return new SurveySubmissionService(repository, new FixedClock(Now));
    }

    private static SubmitSurveyRequestDto ValidScoreRequest()
    {
        return new SubmitSurveyRequestDto("token", 1, new[] { new SubmitAnswerDto(10, 5, null, null) });
    }

    private sealed class FakeSubmissionRepository : ISurveySubmissionRepository
    {
        public SurveyAccessToken? AccessToken { get; set; }
        public Department? Department { get; set; }
        public FakeTransaction? Transaction { get; private set; }
        public List<SurveyResponse> AddedResponses { get; } = new();
        public int BeginTransactionCount { get; private set; }
        public bool ThrowOnSave { get; set; }
        public bool ThrowBusinessRuleOnSave { get; set; }

        public static FakeSubmissionRepository Valid(DateTimeOffset now)
        {
            return WithQuestion(QuestionType.Score, required: true, now);
        }

        public static FakeSubmissionRepository WithQuestion(
            QuestionType type,
            bool required,
            DateTimeOffset? now = null)
        {
            return WithQuestions(new[]
            {
                new Question
                {
                    Id = 10,
                    SurveyId = 100,
                    Text = "Q",
                    Type = type,
                    IsRequired = required,
                    IsActive = true,
                    DisplayOrder = 1
                }
            }, now);
        }

        public static FakeSubmissionRepository WithQuestions(params Question[] questions)
        {
            return WithQuestions(questions, Now);
        }

        private static FakeSubmissionRepository WithQuestions(IEnumerable<Question> questions, DateTimeOffset? now)
        {
            var survey = new Survey
            {
                Id = 100,
                Title = "Memnuniyet",
                IsActive = true,
                Questions = questions.ToList()
            };

            return new FakeSubmissionRepository
            {
                Department = new Department { Id = 1, Name = "Acil", IsActive = true },
                AccessToken = new SurveyAccessToken
                {
                    Id = 50,
                    SurveyId = survey.Id,
                    Token = "token",
                    CreatedAtUtc = (now ?? Now).AddHours(-1),
                    ExpiresAtUtc = (now ?? Now).AddHours(1),
                    Survey = survey
                }
            };
        }

        public Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        {
            BeginTransactionCount++;
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
            AddedResponses.Add(response);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            if (ThrowBusinessRuleOnSave)
            {
                throw new BusinessRuleException("duplicate");
            }

            if (ThrowOnSave)
            {
                throw new InvalidOperationException("boom");
            }

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

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }
}
