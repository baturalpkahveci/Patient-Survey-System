using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Contracts;

public interface ISurveyInvitationRepository :
    IRepositoryBase<SurveyInvitation>
{
    void CreateOneSurveyInvitation(SurveyInvitation invitation);
    void UpdateOneSurveyInvitation(SurveyInvitation invitation);
}
