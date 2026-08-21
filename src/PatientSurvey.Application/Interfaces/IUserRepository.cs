using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetActiveUserByUsernameAsync(string username, CancellationToken cancellationToken);
}
