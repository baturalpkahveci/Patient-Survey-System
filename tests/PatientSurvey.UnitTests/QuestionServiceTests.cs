using PatientSurvey.Application.DTOs.Question;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Application.Services;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Domain.Enums;

namespace PatientSurvey.UnitTests;

public sealed class QuestionServiceTests
{
    [Fact]
    public async Task GetAdminQuestionsAsync_orders_by_survey_then_display_order()
    {
        var repository = new FakeAdminQuestionRepository();
        repository.Questions.Add(new Question { Id = 1, Text = "B2", DisplayOrder = 2, Survey = new Survey { Id = 2, Title = "B" } });
        repository.Questions.Add(new Question { Id = 2, Text = "A1", DisplayOrder = 1, Survey = new Survey { Id = 1, Title = "A" } });
        repository.Questions.Add(new Question { Id = 3, Text = "B1", DisplayOrder = 1, Survey = new Survey { Id = 2, Title = "B" } });

        var result = await new QuestionService(repository).GetAdminQuestionsAsync();

        Assert.Equal(new[] { 2, 3, 1 }, result.Select(question => question.Id));
    }

    [Fact]
    public async Task CreateQuestionAsync_validates_text_and_survey()
    {
        var repository = new FakeAdminQuestionRepository();
        var service = new QuestionService(repository);

        Assert.Equal("text_required", (await service.CreateQuestionAsync(new CreateQuestionRequestDto(1, " ", QuestionType.Score, true, true, 1))).ErrorCode);
        Assert.Equal("survey_not_found", (await service.CreateQuestionAsync(new CreateQuestionRequestDto(99, "Q", QuestionType.Score, true, true, 1))).ErrorCode);
    }

    [Fact]
    public async Task CreateQuestionAsync_persists_normalized_question()
    {
        var repository = new FakeAdminQuestionRepository();
        repository.Surveys.Add(new Survey { Id = 5, Title = "A" });
        var service = new QuestionService(repository);

        var result = await service.CreateQuestionAsync(new CreateQuestionRequestDto(5, " Soru ", QuestionType.Boolean, false, true, 7));

        Assert.True(result.IsSuccess);
        var question = Assert.Single(repository.AddedQuestions);
        Assert.Equal(5, question.SurveyId);
        Assert.Equal("Soru", question.Text);
        Assert.Equal(QuestionType.Boolean, question.Type);
        Assert.False(question.IsRequired);
        Assert.True(question.IsActive);
        Assert.Equal(7, question.DisplayOrder);
    }

    [Fact]
    public async Task ToggleQuestionStatusAsync_flips_status_or_returns_not_found()
    {
        var repository = new FakeAdminQuestionRepository();
        var service = new QuestionService(repository);

        Assert.Equal("question_not_found", (await service.ToggleQuestionStatusAsync(12)).ErrorCode);

        repository.Questions.Add(new Question { Id = 12, Text = "Q", IsActive = true });
        var result = await service.ToggleQuestionStatusAsync(12);

        Assert.True(result.IsSuccess);
        Assert.False(repository.Questions[0].IsActive);
    }

    private sealed class FakeAdminQuestionRepository : IAdminQuestionRepository
    {
        public List<Survey> Surveys { get; } = new();
        public List<Question> Questions { get; } = new();
        public List<Question> AddedQuestions { get; } = new();

        public Task<IReadOnlyCollection<Question>> GetAllQuestionsWithSurveysAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Question>>(Questions);
        }

        public Task<Survey?> GetSurveyByIdAsync(int surveyId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Surveys.FirstOrDefault(survey => survey.Id == surveyId));
        }

        public Task<Question?> GetQuestionByIdAsync(int questionId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Questions.FirstOrDefault(question => question.Id == questionId));
        }

        public void AddQuestion(Question question)
        {
            question.Id = AddedQuestions.Count + 1;
            AddedQuestions.Add(question);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }
}
