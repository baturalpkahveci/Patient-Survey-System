using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Contracts;

public interface IPatientVisitRepository :
    IRepositoryBase<PatientVisit>
{
    Task<PatientVisit?> GetOnePatientVisitByIdAsync(int visitId, bool trackChanges, CancellationToken cancellationToken);
    void CreateOnePatientVisit(PatientVisit visit);
}
