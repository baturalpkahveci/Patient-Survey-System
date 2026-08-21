using Microsoft.EntityFrameworkCore;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;
using PatientSurvey.Infrastructure.Persistence;

namespace PatientSurvey.Infrastructure.EFCore;

public sealed class QuestionRepository :
    RepositoryBase<Question>,
    IQuestionRepository
{
    public QuestionRepository(AppDbContext context)
        : base(context)
    {
    }

    public IQueryable<Question> GetQuestionsBySurveyId(int surveyId, bool trackChanges)
    {
        return FindByCondition(question => question.SurveyId == surveyId, trackChanges)
            .OrderBy(question => question.DisplayOrder);
    }

    public Task<Question?> GetOneQuestionByIdAsync(int questionId, bool trackChanges, CancellationToken cancellationToken)
    {
        return FindByCondition(question => question.Id == questionId, trackChanges)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void CreateOneQuestion(Question question)
    {
        Create(question);
    }

    public void UpdateOneQuestion(Question question)
    {
        Update(question);
    }

    public void DeleteOneQuestion(Question question)
    {
        Delete(question);
    }
}
