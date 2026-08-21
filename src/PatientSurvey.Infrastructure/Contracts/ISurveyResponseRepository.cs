using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Contracts;

public interface ISurveyResponseRepository :
    IRepositoryBase<SurveyResponse>
{
    Task<SurveyResponse?> GetOneSurveyResponseByIdAsync(int responseId, bool trackChanges, CancellationToken cancellationToken);
    void CreateOneSurveyResponse(SurveyResponse response);
}
