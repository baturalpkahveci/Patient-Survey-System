using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Application.Interfaces;

public interface IAdminSurveyRepository
{
    Task<IReadOnlyCollection<Survey>> GetAllSurveysWithQuestionsAsync(CancellationToken cancellationToken);
    Task<Survey?> GetSurveyWithQuestionsAsync(int surveyId, CancellationToken cancellationToken);
    Task<Survey?> GetSurveyByIdAsync(int surveyId, CancellationToken cancellationToken);
    void AddSurvey(Survey survey);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
