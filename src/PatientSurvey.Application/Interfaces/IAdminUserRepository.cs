using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Application.Interfaces;

public interface IAdminUserRepository
{
    Task<IReadOnlyCollection<User>> GetAllUsersWithRolesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Role>> GetActiveRolesAsync(CancellationToken cancellationToken);
    Task<Role?> GetRoleByIdAsync(int roleId, CancellationToken cancellationToken);
    Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken);
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken);
    void AddUser(User user);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
