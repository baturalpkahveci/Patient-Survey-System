using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Contracts;

public interface IPermissionRepository : IRepositoryBase<Permission>
{
    Task<Permission?> GetOneActivePermissionByNameAsync(string permissionName, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Permission>> GetAllActivePermissionsAsync(CancellationToken cancellationToken);
}
