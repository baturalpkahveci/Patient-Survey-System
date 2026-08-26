using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Application.Interfaces;

public interface IAuditLogRepository
{
    Task<IReadOnlyCollection<AuditLog>> GetAuditLogsAsync(CancellationToken cancellationToken);
    void AddAuditLog(AuditLog auditLog);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
