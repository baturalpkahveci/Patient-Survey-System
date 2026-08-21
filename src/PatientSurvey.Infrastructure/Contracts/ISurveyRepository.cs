using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Contracts;

public interface ISurveyRepository :
    IRepositoryBase<Survey>
{
    IQueryable<Survey> GetAllSurveys(bool trackChanges);
    Task<Survey?> GetOneSurveyByIdAsync(int surveyId, bool trackChanges, CancellationToken cancellationToken);
    void CreateOneSurvey(Survey survey);
    void UpdateOneSurvey(Survey survey);
    void DeleteOneSurvey(Survey survey);
}
