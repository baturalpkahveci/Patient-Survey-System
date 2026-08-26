using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Application.Interfaces;

public interface IAuditLogRepository
{
    Task<IReadOnlyCollection<AuditLog>> GetAuditLogsAsync(CancellationToken cancellationToken);
}
