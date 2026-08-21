using Microsoft.EntityFrameworkCore;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;

namespace PatientSurvey.Infrastructure.EFCore;

internal sealed class AdminUserRepository : IAdminUserRepository
{
    private readonly IRepositoryManager _repositoryManager;

    public AdminUserRepository(IRepositoryManager repositoryManager)
    {
        _repositoryManager = repositoryManager;
    }

    public async Task<IReadOnlyCollection<User>> GetAllUsersWithRolesAsync(CancellationToken cancellationToken)
    {
        return await _repositoryManager.Users.GetAllUsers(trackChanges: false)
            .ToArrayAsync(cancellationToken);
    }

    public Task<IReadOnlyCollection<Role>> GetActiveRolesAsync(CancellationToken cancellationToken)
    {
        return _repositoryManager.Roles.GetActiveRolesAsync(cancellationToken);
    }

    public Task<Role?> GetRoleByIdAsync(int roleId, CancellationToken cancellationToken)
    {
        return _repositoryManager.Roles.GetOneRoleByIdAsync(roleId, trackChanges: false, cancellationToken);
    }

    public Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken)
    {
        return _repositoryManager.Users.GetOneUserByIdAsync(userId, trackChanges: true, cancellationToken);
    }

    public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken)
    {
        return _repositoryManager.Users
            .FindByCondition(user => user.Username == username, trackChanges: false)
            .AnyAsync(cancellationToken);
    }

    public void AddUser(User user)
    {
        _repositoryManager.Users.CreateOneUser(user);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _repositoryManager.SaveAsync(cancellationToken);
    }
}
