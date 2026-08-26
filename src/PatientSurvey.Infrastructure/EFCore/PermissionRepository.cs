using Microsoft.EntityFrameworkCore;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;
using PatientSurvey.Infrastructure.Persistence;

namespace PatientSurvey.Infrastructure.EFCore;

public sealed class PermissionRepository : RepositoryBase<Permission>, IPermissionRepository
{
    public PermissionRepository(AppDbContext context)
        : base(context)
    {
    }

    public Task<Permission?> GetOneActivePermissionByNameAsync(
        string permissionName,
        CancellationToken cancellationToken)
    {
        return FindByCondition(
                permission => permission.Name == permissionName && permission.IsActive,
                trackChanges: false)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Permission>> GetAllActivePermissionsAsync(CancellationToken cancellationToken)
    {
        return await FindByCondition(permission => permission.IsActive, trackChanges: false)
            .ToArrayAsync(cancellationToken);
    }
}
