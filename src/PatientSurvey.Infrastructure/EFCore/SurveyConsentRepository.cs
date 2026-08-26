using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;
using PatientSurvey.Infrastructure.Persistence;

namespace PatientSurvey.Infrastructure.EFCore;

public sealed class SurveyConsentRepository :
    RepositoryBase<SurveyConsent>,
    ISurveyConsentRepository
{
    public SurveyConsentRepository(AppDbContext context)
        : base(context)
    {
    }

    public void CreateOneSurveyConsent(SurveyConsent consent)
    {
        Create(consent);
    }
}
