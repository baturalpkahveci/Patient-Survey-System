using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Contracts;

public interface IPatientRepository :
    IRepositoryBase<Patient>
{
    Task<Patient?> GetOnePatientByTcHashAsync(string tcIdentityLookupHash, bool trackChanges, CancellationToken cancellationToken);
    void CreateOnePatient(Patient patient);
    void UpdateOnePatient(Patient patient);
}
