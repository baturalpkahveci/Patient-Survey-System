using PatientSurvey.Application.DTOs.User;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Application.Services;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.UnitTests;

public sealed class UserServiceTests
{
    [Fact]
    public async Task AuthenticateAsync_rejects_blank_credentials_without_lookup()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);

        var result = await service.AuthenticateAsync(" ", "password");

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid_login", result.ErrorCode);
        Assert.Equal(0, repository.LookupCount);
    }

    [Fact]
    public async Task AuthenticateAsync_rejects_unknown_user_inactive_role_and_bad_password()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);

        Assert.False((await service.AuthenticateAsync("missing", "password")).IsSuccess);

        repository.User = new User
        {
            Username = "manager",
            PasswordHash = "hash:secret",
            IsActive = true,
            Role = new Role { Name = "Manager", IsActive = false }
        };
        Assert.False((await service.AuthenticateAsync("manager", "secret")).IsSuccess);

        repository.User.Role.IsActive = true;
        Assert.False((await service.AuthenticateAsync("manager", "wrong")).IsSuccess);
    }

    [Fact]
    public async Task AuthenticateAsync_returns_user_when_password_and_role_are_valid()
    {
        var repository = new FakeUserRepository
        {
            User = new User
            {
                Id = 10,
                Username = "admin",
                PasswordHash = "hash:secret",
                IsActive = true,
                Role = new Role { Id = 1, Name = "Admin", IsActive = true }
            }
        };
        var service = CreateService(repository);

        var result = await service.AuthenticateAsync(" admin ", "secret");

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value!.Id);
        Assert.Equal("Admin", result.Value.RoleName);
        Assert.Equal("admin", repository.LastUsername);
    }

    [Fact]
    public async Task CreateUserAsync_validates_username_password_role_and_duplicates()
    {
        var adminRepository = new FakeAdminUserRepository();
        var service = CreateService(adminRepository: adminRepository);

        Assert.Equal("username_required", (await service.CreateUserAsync(new CreateUserRequestDto(" ", "password1", 1, true))).ErrorCode);
        Assert.Equal("password_invalid", (await service.CreateUserAsync(new CreateUserRequestDto("new", "short", 1, true))).ErrorCode);
        Assert.Equal("role_invalid", (await service.CreateUserAsync(new CreateUserRequestDto("new", "password1", 999, true))).ErrorCode);

        adminRepository.Roles.Add(new Role { Id = 1, Name = "Admin", IsActive = false });
        Assert.Equal("role_invalid", (await service.CreateUserAsync(new CreateUserRequestDto("new", "password1", 1, true))).ErrorCode);

        adminRepository.Roles[0].IsActive = true;
        adminRepository.ExistingUsernames.Add("new");
        Assert.Equal("username_exists", (await service.CreateUserAsync(new CreateUserRequestDto(" new ", "password1", 1, true))).ErrorCode);
    }

    [Fact]
    public async Task CreateUserAsync_hashes_password_and_persists_active_flag()
    {
        var adminRepository = new FakeAdminUserRepository();
        adminRepository.Roles.Add(new Role { Id = 2, Name = "Manager", IsActive = true });
        var service = CreateService(adminRepository: adminRepository);

        var result = await service.CreateUserAsync(new CreateUserRequestDto(" manager ", "password1", 2, false));

        Assert.True(result.IsSuccess);
        var user = Assert.Single(adminRepository.AddedUsers);
        Assert.Equal("manager", user.Username);
        Assert.Equal("hash:password1", user.PasswordHash);
        Assert.Equal(2, user.RoleId);
        Assert.False(user.IsActive);
        Assert.Equal(1, adminRepository.SaveCount);
    }

    [Fact]
    public async Task ToggleUserStatusAsync_flips_status_or_returns_not_found()
    {
        var adminRepository = new FakeAdminUserRepository();
        var service = CreateService(adminRepository: adminRepository);

        Assert.Equal("user_not_found", (await service.ToggleUserStatusAsync(404)).ErrorCode);

        adminRepository.Users.Add(new User { Id = 5, Username = "admin", IsActive = true });
        var result = await service.ToggleUserStatusAsync(5);

        Assert.True(result.IsSuccess);
        Assert.False(adminRepository.Users[0].IsActive);
        Assert.Equal(1, adminRepository.SaveCount);
    }

    [Fact]
    public async Task GetUsersAsync_includes_doctor_profile_metadata_without_hiding_missing_profiles()
    {
        var adminRepository = new FakeAdminUserRepository();
        adminRepository.Users.AddRange(new[]
        {
            new User
            {
                Id = 2,
                Username = "doctor-with-profile",
                IsActive = true,
                Role = new Role { Id = 3, Name = "Doctor", IsActive = true },
                Doctor = new Doctor
                {
                    Id = 9,
                    FirstName = "Ayşe",
                    LastName = "Yılmaz",
                    DepartmentId = 4,
                    Department = new Department { Id = 4, Name = "Dahiliye", IsActive = true },
                    IsActive = true
                }
            },
            new User
            {
                Id = 1,
                Username = "doctor-missing-profile",
                IsActive = true,
                Role = new Role { Id = 3, Name = "Doctor", IsActive = true }
            }
        });
        var service = CreateService(adminRepository: adminRepository);

        var users = await service.GetUsersAsync();

        Assert.Equal(2, users.Count);
        var missingProfile = Assert.Single(users, user => user.Username == "doctor-missing-profile");
        Assert.Null(missingProfile.DoctorId);

        var doctor = Assert.Single(users, user => user.Username == "doctor-with-profile");
        Assert.Equal(9, doctor.DoctorId);
        Assert.Equal("Ayşe", doctor.DoctorFirstName);
        Assert.Equal("Yılmaz", doctor.DoctorLastName);
        Assert.Equal(4, doctor.DoctorDepartmentId);
        Assert.Equal("Dahiliye", doctor.DoctorDepartmentName);
        Assert.True(doctor.DoctorIsActive);
    }

    private static UserService CreateService(
        FakeUserRepository? repository = null,
        FakeAdminUserRepository? adminRepository = null)
    {
        return new UserService(
            repository ?? new FakeUserRepository(),
            adminRepository ?? new FakeAdminUserRepository(),
            new FakePasswordHasher());
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public User? User { get; set; }
        public string? LastUsername { get; private set; }
        public int LookupCount { get; private set; }

        public Task<User?> GetActiveUserByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            LookupCount++;
            LastUsername = username;
            return Task.FromResult(User is { IsActive: true } && User.Username == username ? User : null);
        }
    }

    private sealed class FakeAdminUserRepository : IAdminUserRepository
    {
        public List<User> Users { get; } = new();
        public List<Role> Roles { get; } = new();
        public HashSet<string> ExistingUsernames { get; } = new(StringComparer.Ordinal);
        public List<User> AddedUsers { get; } = new();
        public int SaveCount { get; private set; }

        public Task<IReadOnlyCollection<User>> GetAllUsersWithRolesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<User>>(Users);
        }

        public Task<IReadOnlyCollection<Role>> GetActiveRolesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Role>>(Roles.Where(role => role.IsActive).ToArray());
        }

        public Task<Role?> GetRoleByIdAsync(int roleId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Roles.FirstOrDefault(role => role.Id == roleId));
        }

        public Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Users.FirstOrDefault(user => user.Id == userId));
        }

        public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken)
        {
            return Task.FromResult(ExistingUsernames.Contains(username));
        }

        public void AddUser(User user)
        {
            AddedUsers.Add(user);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => $"hash:{password}";
        public bool VerifyPassword(string password, string passwordHash) => passwordHash == $"hash:{password}";
    }
}
