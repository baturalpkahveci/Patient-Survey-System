using Microsoft.EntityFrameworkCore;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;
using PatientSurvey.Infrastructure.Persistence;

namespace PatientSurvey.Infrastructure.EFCore;

public sealed class RoleRepository :
    RepositoryBase<Role>,
    IRoleRepository
{
    public RoleRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<IReadOnlyCollection<Role>> GetActiveRolesAsync(CancellationToken cancellationToken)
    {
        return await FindByCondition(role => role.IsActive, trackChanges: false)
            .OrderBy(role => role.Name)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Role?> GetOneRoleByIdAsync(int roleId, bool trackChanges, CancellationToken cancellationToken)
    {
        return FindByCondition(role => role.Id == roleId, trackChanges)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
