using Microsoft.EntityFrameworkCore;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;
using AppIPermissionRepository = PatientSurvey.Application.Interfaces.IPermissionRepository;

namespace PatientSurvey.Infrastructure.EFCore;

internal sealed class PermissionManagementRepository : AppIPermissionRepository
{
    private readonly IRepositoryManager _repositoryManager;

    public PermissionManagementRepository(IRepositoryManager repositoryManager)
    {
        _repositoryManager = repositoryManager;
    }

    public Task<User?> GetUserPermissionProfileAsync(
        int userId,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        return _repositoryManager.Users
            .FindByCondition(user => user.Id == userId, trackChanges)
            .Include(user => user.Role)
            .Include(user => user.UserPermissions)
                .ThenInclude(userPermission => userPermission.Permission)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<IReadOnlyCollection<Permission>> GetActivePermissionsAsync(CancellationToken cancellationToken)
    {
        return _repositoryManager.Permissions.GetAllActivePermissionsAsync(cancellationToken);
    }

    public Task<Permission?> GetActivePermissionByNameAsync(
        string permissionName,
        CancellationToken cancellationToken)
    {
        return _repositoryManager.Permissions.GetOneActivePermissionByNameAsync(permissionName, cancellationToken);
    }

    public void AddUserPermission(UserPermission userPermission)
    {
        _repositoryManager.UserPermissions.CreateOneUserPermission(userPermission);
    }

    public void RemoveUserPermission(UserPermission userPermission)
    {
        _repositoryManager.UserPermissions.DeleteOneUserPermission(userPermission);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _repositoryManager.SaveAsync(cancellationToken);
    }
}
