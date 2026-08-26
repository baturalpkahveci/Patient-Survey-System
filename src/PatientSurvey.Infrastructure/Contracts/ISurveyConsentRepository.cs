using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Contracts;

public interface ISurveyConsentRepository :
    IRepositoryBase<SurveyConsent>
{
    void CreateOneSurveyConsent(SurveyConsent consent);
}
