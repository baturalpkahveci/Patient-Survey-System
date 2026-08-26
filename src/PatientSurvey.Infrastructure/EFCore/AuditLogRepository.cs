using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;
using PatientSurvey.Infrastructure.Persistence;

namespace PatientSurvey.Infrastructure.EFCore;

internal sealed class AuditLogRepository : RepositoryBase<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(AppDbContext context)
        : base(context)
    {
    }

    public void CreateOneAuditLog(AuditLog auditLog)
    {
        Create(auditLog);
    }
}
