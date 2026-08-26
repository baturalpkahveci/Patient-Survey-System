using Microsoft.EntityFrameworkCore;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;
using PatientSurvey.Infrastructure.Persistence;

namespace PatientSurvey.Infrastructure.EFCore;

public sealed class UserRepository :
    RepositoryBase<User>,
    IUserRepository,
    PatientSurvey.Application.Interfaces.IUserRepository
{
    public UserRepository(AppDbContext context)
        : base(context)
    {
    }

    public Task<User?> GetActiveUserByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        return FindByCondition(user => user.Username == username && user.IsActive, trackChanges: false)
            .Include(user => user.Role)
            .Include(user => user.UserPermissions)
                .ThenInclude(userPermission => userPermission.Permission)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public IQueryable<User> GetAllUsers(bool trackChanges)
    {
        return FindAll(trackChanges)
            .Include(user => user.Role)
            .Include(user => user.Doctor!)
                .ThenInclude(doctor => doctor.Department)
            .Include(user => user.UserPermissions)
                .ThenInclude(userPermission => userPermission.Permission)
            .OrderBy(user => user.Username);
    }

    public Task<User?> GetOneUserByIdAsync(int userId, bool trackChanges, CancellationToken cancellationToken)
    {
        return FindByCondition(user => user.Id == userId, trackChanges)
            .Include(user => user.Role)
            .Include(user => user.Doctor!)
                .ThenInclude(doctor => doctor.Department)
            .Include(user => user.UserPermissions)
                .ThenInclude(userPermission => userPermission.Permission)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void CreateOneUser(User user)
    {
        Create(user);
    }

    public void UpdateOneUser(User user)
    {
        Update(user);
    }

    public void DeleteOneUser(User user)
    {
        Delete(user);
    }
}
