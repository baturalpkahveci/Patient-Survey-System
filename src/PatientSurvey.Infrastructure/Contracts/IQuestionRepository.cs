using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Contracts;

public interface IQuestionRepository :
    IRepositoryBase<Question>
{
    IQueryable<Question> GetQuestionsBySurveyId(int surveyId, bool trackChanges);
    Task<Question?> GetOneQuestionByIdAsync(int questionId, bool trackChanges, CancellationToken cancellationToken);
    void CreateOneQuestion(Question question);
    void UpdateOneQuestion(Question question);
    void DeleteOneQuestion(Question question);
}
