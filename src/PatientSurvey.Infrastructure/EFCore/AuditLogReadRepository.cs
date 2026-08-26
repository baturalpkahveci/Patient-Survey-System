using Microsoft.EntityFrameworkCore;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;
using AppIAuditLogRepository = PatientSurvey.Application.Interfaces.IAuditLogRepository;

namespace PatientSurvey.Infrastructure.EFCore;

internal sealed class AuditLogReadRepository : AppIAuditLogRepository
{
    private readonly IRepositoryManager _repositoryManager;

    public AuditLogReadRepository(IRepositoryManager repositoryManager)
    {
        _repositoryManager = repositoryManager;
    }

    public async Task<IReadOnlyCollection<AuditLog>> GetAuditLogsAsync(CancellationToken cancellationToken)
    {
        return await _repositoryManager.AuditLogs
            .FindAll(trackChanges: false)
            .Include(log => log.User)
            .ToArrayAsync(cancellationToken);
    }
}
