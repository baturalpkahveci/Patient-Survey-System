using PatientSurvey.Application.DTOs.Audit;
using PatientSurvey.Application.Interfaces;

namespace PatientSurvey.Application.Services;

public sealed class AuditLogService
{
    private readonly IAuditLogRepository _repository;

    public AuditLogService(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<AuditLogListItemDto>> GetAuditLogsAsync(
        CancellationToken cancellationToken = default)
    {
        var logs = await _repository.GetAuditLogsAsync(cancellationToken);
        return logs
            .OrderByDescending(log => log.OccurredAtUtc)
            .Select(log => new AuditLogListItemDto(
                log.Id,
                log.OccurredAtUtc,
                log.UserId,
                log.Username,
                log.UserRole,
                log.Action,
                log.EntityName,
                log.EntityId,
                log.Summary,
                log.ChangesJson,
                log.IpAddress,
                log.RequestPath))
            .ToArray();
    }
}
