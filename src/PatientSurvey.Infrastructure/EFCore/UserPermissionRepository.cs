using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;
using PatientSurvey.Infrastructure.Persistence;

namespace PatientSurvey.Infrastructure.EFCore;

public sealed class UserPermissionRepository : RepositoryBase<UserPermission>, IUserPermissionRepository
{
    public UserPermissionRepository(AppDbContext context)
        : base(context)
    {
    }

    public void CreateOneUserPermission(UserPermission userPermission)
    {
        Create(userPermission);
    }

    public void DeleteOneUserPermission(UserPermission userPermission)
    {
        Delete(userPermission);
    }
}
