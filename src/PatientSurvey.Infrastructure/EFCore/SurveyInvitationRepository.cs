using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;
using PatientSurvey.Infrastructure.Persistence;

namespace PatientSurvey.Infrastructure.EFCore;

public sealed class SurveyInvitationRepository :
    RepositoryBase<SurveyInvitation>,
    ISurveyInvitationRepository
{
    public SurveyInvitationRepository(AppDbContext context)
        : base(context)
    {
    }

    public void CreateOneSurveyInvitation(SurveyInvitation invitation)
    {
        Create(invitation);
    }

    public void UpdateOneSurveyInvitation(SurveyInvitation invitation)
    {
        Update(invitation);
    }
}
