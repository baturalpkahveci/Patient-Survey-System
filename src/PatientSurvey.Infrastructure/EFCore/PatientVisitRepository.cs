using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;
using PatientSurvey.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace PatientSurvey.Infrastructure.EFCore;

public sealed class PatientVisitRepository :
    RepositoryBase<PatientVisit>,
    IPatientVisitRepository
{
    public PatientVisitRepository(AppDbContext context)
        : base(context)
    {
    }

    public Task<PatientVisit?> GetOnePatientVisitByIdAsync(
        int visitId,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        return FindByCondition(visit => visit.Id == visitId, trackChanges)
            .Include(visit => visit.Patient)
            .Include(visit => visit.Doctor)
            .Include(visit => visit.Department)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void CreateOnePatientVisit(PatientVisit visit)
    {
        Create(visit);
    }
}
