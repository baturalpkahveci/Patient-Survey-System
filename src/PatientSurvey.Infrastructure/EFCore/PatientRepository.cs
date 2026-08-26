using Microsoft.EntityFrameworkCore;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;
using PatientSurvey.Infrastructure.Persistence;

namespace PatientSurvey.Infrastructure.EFCore;

public sealed class PatientRepository :
    RepositoryBase<Patient>,
    IPatientRepository
{
    public PatientRepository(AppDbContext context)
        : base(context)
    {
    }

    public Task<Patient?> GetOnePatientByTcHashAsync(
        string tcIdentityLookupHash,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        return FindByCondition(patient => patient.TcIdentityLookupHash == tcIdentityLookupHash, trackChanges)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void CreateOnePatient(Patient patient)
    {
        Create(patient);
    }

    public void UpdateOnePatient(Patient patient)
    {
        Update(patient);
    }
}
