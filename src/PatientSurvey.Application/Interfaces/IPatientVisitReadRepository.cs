using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Application.Interfaces;

public interface IPatientVisitReadRepository
{
    Task<IReadOnlyCollection<PatientVisit>> GetPatientVisitsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PatientVisit>> GetPatientVisitsByDoctorAsync(int doctorId, CancellationToken cancellationToken);
}
