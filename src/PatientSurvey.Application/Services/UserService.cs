using PatientSurvey.Application.Common;
using PatientSurvey.Application.DTOs.User;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Application.Services;

public sealed class UserService
{
    private readonly IUserRepository _repository;
    private readonly IAdminUserRepository _adminRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(
        IUserRepository repository,
        IAdminUserRepository adminRepository,
        IPasswordHasher passwordHasher)
    {
        _repository = repository;
        _adminRepository = adminRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<ServiceResult<AuthenticatedUserDto>> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return LoginFailure();
        }

        var user = await _repository.GetActiveUserByUsernameAsync(username.Trim(), cancellationToken);
        if (user?.Role is null || !user.Role.IsActive)
        {
            return LoginFailure();
        }

        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
        {
            return LoginFailure();
        }

        return ServiceResult<AuthenticatedUserDto>.Success(
            new AuthenticatedUserDto(user.Id, user.Username, user.Role.Name));
    }

    public async Task<IReadOnlyCollection<UserListItemDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _adminRepository.GetAllUsersWithRolesAsync(cancellationToken);
        return users
            .OrderBy(user => user.Username)
            .Select(user => new UserListItemDto(
                user.Id,
                user.Username,
                user.Role?.Name ?? string.Empty,
                user.IsActive,
                user.Doctor?.Id,
                user.Doctor?.FirstName,
                user.Doctor?.LastName,
                user.Doctor?.DepartmentId,
                user.Doctor?.Department?.Name,
                user.Doctor?.IsActive))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<RoleOptionDto>> GetRoleOptionsAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _adminRepository.GetActiveRolesAsync(cancellationToken);
        return roles
            .OrderBy(role => role.Name)
            .Select(role => new RoleOptionDto(role.Id, role.Name))
            .ToArray();
    }

    public async Task<ServiceResult> CreateUserAsync(
        CreateUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await CreateUserAndReturnIdAsync(request, cancellationToken);
        return result.IsSuccess
            ? ServiceResult.Success()
            : ServiceResult.Failure(result.ErrorCode ?? "user_create_failed", result.Message ?? "Kullanıcı oluşturulamadı.");
    }

    public async Task<ServiceResult<int>> CreateUserAndReturnIdAsync(
        CreateUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            return ServiceResult<int>.Failure("username_required", "Kullanıcı adı zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return ServiceResult<int>.Failure("password_invalid", "Şifre en az 8 karakter olmalıdır.");
        }

        var role = await _adminRepository.GetRoleByIdAsync(request.RoleId, cancellationToken);
        if (role is null || !role.IsActive)
        {
            return ServiceResult<int>.Failure("role_invalid", "Geçerli bir rol seçin.");
        }

        if (await _adminRepository.UsernameExistsAsync(username, cancellationToken))
        {
            return ServiceResult<int>.Failure("username_exists", "Bu kullanıcı adı zaten kullanılıyor.");
        }

        var user = new User
        {
            Username = username,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            RoleId = role.Id,
            IsActive = request.IsActive
        };

        _adminRepository.AddUser(user);
        await _adminRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<int>.Success(user.Id);
    }

    public async Task<ServiceResult> ToggleUserStatusAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _adminRepository.GetUserByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ServiceResult.Failure("user_not_found", "Kullanıcı bulunamadı.");
        }

        user.IsActive = !user.IsActive;
        await _adminRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    private static ServiceResult<AuthenticatedUserDto> LoginFailure()
    {
        return ServiceResult<AuthenticatedUserDto>.Failure("invalid_login", "Kullanıcı adı veya şifre hatalı.");
    }
}
