using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Contracts;

public interface IPatientVisitRepository :
    IRepositoryBase<PatientVisit>
{
    void CreateOnePatientVisit(PatientVisit visit);
}
