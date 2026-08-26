using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Application.Interfaces;

public interface IPatientVisitReadRepository
{
    Task<IReadOnlyCollection<PatientVisit>> GetPatientVisitsAsync(
        bool includePatientPersonalData,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PatientVisit>> GetPatientVisitsByDoctorAsync(
        int doctorId,
        bool includePatientPersonalData,
        CancellationToken cancellationToken);
}
