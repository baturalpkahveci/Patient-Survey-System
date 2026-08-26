using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Contracts;

public interface IUserPermissionRepository : IRepositoryBase<UserPermission>
{
    void CreateOneUserPermission(UserPermission userPermission);
    void DeleteOneUserPermission(UserPermission userPermission);
}
