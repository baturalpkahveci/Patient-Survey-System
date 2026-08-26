using Microsoft.EntityFrameworkCore;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;

namespace PatientSurvey.Infrastructure.EFCore;

internal sealed class PatientVisitReadRepository : IPatientVisitReadRepository
{
    private readonly IRepositoryManager _repositoryManager;

    public PatientVisitReadRepository(IRepositoryManager repositoryManager)
    {
        _repositoryManager = repositoryManager;
    }

    public async Task<IReadOnlyCollection<PatientVisit>> GetPatientVisitsAsync(
        bool includePatientPersonalData,
        CancellationToken cancellationToken)
    {
        return await BaseQuery(includePatientPersonalData)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PatientVisit>> GetPatientVisitsByDoctorAsync(
        int doctorId,
        bool includePatientPersonalData,
        CancellationToken cancellationToken)
    {
        return await BaseQuery(includePatientPersonalData)
            .Where(visit => visit.DoctorId == doctorId)
            .ToArrayAsync(cancellationToken);
    }

    private IQueryable<PatientVisit> BaseQuery(bool includePatientPersonalData)
    {
        var query = _repositoryManager.PatientVisits
            .FindAll(trackChanges: false)
            .Include(visit => visit.Doctor)
            .Include(visit => visit.Department)
            .Include(visit => visit.CreatedByUser)
            .Include(visit => visit.Invitations)
                .ThenInclude(invitation => invitation.Survey)
            .Include(visit => visit.Invitations)
                .ThenInclude(invitation => invitation.AccessToken);

        return includePatientPersonalData
            ? query.Include(visit => visit.Patient)
            : query;
    }
}
