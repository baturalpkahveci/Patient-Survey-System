namespace PatientSurvey.Infrastructure.Contracts;

public interface IAuditLogRepository : IRepositoryBase<PatientSurvey.Domain.Entities.AuditLog>
{
    void CreateOneAuditLog(PatientSurvey.Domain.Entities.AuditLog auditLog);
}
