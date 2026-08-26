using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Application.Interfaces;

public interface IPermissionRepository
{
    Task<User?> GetUserPermissionProfileAsync(int userId, bool trackChanges, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Permission>> GetActivePermissionsAsync(CancellationToken cancellationToken);
    Task<Permission?> GetActivePermissionByNameAsync(string permissionName, CancellationToken cancellationToken);
    void AddUserPermission(UserPermission userPermission);
    void RemoveUserPermission(UserPermission userPermission);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
