using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Contracts;

public interface IUserRepository :
    IRepositoryBase<User>
{
    IQueryable<User> GetAllUsers(bool trackChanges);
    Task<User?> GetOneUserByIdAsync(int userId, bool trackChanges, CancellationToken cancellationToken);
    Task<User?> GetActiveUserByUsernameAsync(string username, CancellationToken cancellationToken);
    void CreateOneUser(User user);
    void UpdateOneUser(User user);
    void DeleteOneUser(User user);
}
