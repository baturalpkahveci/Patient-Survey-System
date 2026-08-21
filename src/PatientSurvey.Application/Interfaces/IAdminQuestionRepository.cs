using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Application.Interfaces;

public interface IAdminQuestionRepository
{
    Task<IReadOnlyCollection<Question>> GetAllQuestionsWithSurveysAsync(CancellationToken cancellationToken);
    Task<Survey?> GetSurveyByIdAsync(int surveyId, CancellationToken cancellationToken);
    Task<Question?> GetQuestionByIdAsync(int questionId, CancellationToken cancellationToken);
    void AddQuestion(Question question);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
