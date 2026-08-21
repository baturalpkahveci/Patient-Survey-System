using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Contracts;

public interface IRoleRepository :
    IRepositoryBase<Role>
{
    Task<IReadOnlyCollection<Role>> GetActiveRolesAsync(CancellationToken cancellationToken);
    Task<Role?> GetOneRoleByIdAsync(int roleId, bool trackChanges, CancellationToken cancellationToken);
}
