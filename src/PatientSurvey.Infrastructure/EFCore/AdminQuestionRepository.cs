using Microsoft.EntityFrameworkCore;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;

namespace PatientSurvey.Infrastructure.EFCore;

internal sealed class AdminQuestionRepository : IAdminQuestionRepository
{
    private readonly IRepositoryManager _repositoryManager;

    public AdminQuestionRepository(IRepositoryManager repositoryManager)
    {
        _repositoryManager = repositoryManager;
    }

    public async Task<IReadOnlyCollection<Question>> GetAllQuestionsWithSurveysAsync(CancellationToken cancellationToken)
    {
        return await _repositoryManager.Questions
            .FindAll(trackChanges: false)
            .Include(question => question.Survey)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Survey?> GetSurveyByIdAsync(int surveyId, CancellationToken cancellationToken)
    {
        return _repositoryManager.Surveys.GetOneSurveyByIdAsync(surveyId, trackChanges: false, cancellationToken);
    }

    public Task<Question?> GetQuestionByIdAsync(int questionId, CancellationToken cancellationToken)
    {
        return _repositoryManager.Questions.GetOneQuestionByIdAsync(questionId, trackChanges: true, cancellationToken);
    }

    public void AddQuestion(Question question)
    {
        _repositoryManager.Questions.CreateOneQuestion(question);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _repositoryManager.SaveAsync(cancellationToken);
    }
}
