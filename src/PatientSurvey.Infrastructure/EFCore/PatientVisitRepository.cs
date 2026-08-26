using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;
using PatientSurvey.Infrastructure.Persistence;

namespace PatientSurvey.Infrastructure.EFCore;

public sealed class PatientVisitRepository :
    RepositoryBase<PatientVisit>,
    IPatientVisitRepository
{
    public PatientVisitRepository(AppDbContext context)
        : base(context)
    {
    }

    public void CreateOnePatientVisit(PatientVisit visit)
    {
        Create(visit);
    }
}
