using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.DTOs.User;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Application.Services;
using PatientSurvey.Domain.Entities;
using PatientSurvey.WebUI.Areas.Admin.Controllers;
using PatientSurvey.WebUI.ViewModels.Admin;

namespace PatientSurvey.IntegrationTests;

public sealed class AdminUsersControllerTests
{
    [Fact]
    public async Task Create_does_not_require_doctor_fields_for_non_doctor_role()
    {
        var adminRepository = new FakeAdminUserRepository();
        var doctorRepository = new FakeDoctorManagementRepository();
        var controller = CreateController(adminRepository, doctorRepository);

        var result = await controller.Create(new CreateUserViewModel
        {
            Username = "manager1",
            Password = "password1",
            RoleId = 2,
            IsActive = true
        }, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        var user = Assert.Single(adminRepository.AddedUsers);
        Assert.Equal("manager1", user.Username);
        Assert.Equal(2, user.RoleId);
        Assert.Empty(doctorRepository.AddedDoctors);
    }

    [Fact]
    public async Task Create_requires_doctor_fields_only_when_selected_role_is_doctor()
    {
        var adminRepository = new FakeAdminUserRepository();
        var doctorRepository = new FakeDoctorManagementRepository();
        var controller = CreateController(adminRepository, doctorRepository);

        var result = await controller.Create(new CreateUserViewModel
        {
            Username = "doctor1",
            Password = "password1",
            RoleId = 3,
            IsActive = true
        }, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.NotNull(view.Model);
        Assert.Empty(adminRepository.AddedUsers);
        Assert.Empty(doctorRepository.AddedDoctors);
        Assert.True(controller.ModelState.ContainsKey(nameof(CreateUserViewModel.DoctorFirstName)));
        Assert.True(controller.ModelState.ContainsKey(nameof(CreateUserViewModel.DoctorLastName)));
        Assert.True(controller.ModelState.ContainsKey(nameof(CreateUserViewModel.DoctorDepartmentId)));
    }

    [Fact]
    public async Task Create_creates_doctor_profile_when_selected_role_is_doctor()
    {
        var adminRepository = new FakeAdminUserRepository();
        var doctorRepository = new FakeDoctorManagementRepository();
        var controller = CreateController(adminRepository, doctorRepository);

        var result = await controller.Create(new CreateUserViewModel
        {
            Username = "doctor1",
            Password = "password1",
            RoleId = 3,
            IsActive = true,
            DoctorFirstName = "Ayse",
            DoctorLastName = "Yilmaz",
            DoctorDepartmentId = 10
        }, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        var user = Assert.Single(adminRepository.AddedUsers);
        var doctor = Assert.Single(doctorRepository.AddedDoctors);
        Assert.Equal(user.Id, doctor.UserId);
        Assert.Equal("Ayse", doctor.FirstName);
        Assert.Equal("Yilmaz", doctor.LastName);
        Assert.Equal(10, doctor.DepartmentId);
    }

    private static UsersController CreateController(
        FakeAdminUserRepository adminRepository,
        FakeDoctorManagementRepository doctorRepository)
    {
        doctorRepository.Departments.Add(new Department { Id = 10, Name = "Dahiliye", IsActive = true });

        return new UsersController(
            new UserService(new FakeUserRepository(), adminRepository, new FakePasswordHasher()),
            new DoctorService(doctorRepository))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public Task<User?> GetActiveUserByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            return Task.FromResult<User?>(null);
        }
    }

    private sealed class FakeAdminUserRepository : IAdminUserRepository
    {
        private readonly List<Role> _roles = new()
        {
            new Role { Id = 1, Name = "Admin", IsActive = true },
            new Role { Id = 2, Name = "Manager", IsActive = true },
            new Role { Id = 3, Name = "Doctor", IsActive = true }
        };

        public List<User> AddedUsers { get; } = new();

        public Task<IReadOnlyCollection<User>> GetAllUsersWithRolesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<User>>(AddedUsers);
        }

        public Task<IReadOnlyCollection<Role>> GetActiveRolesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Role>>(_roles);
        }

        public Task<Role?> GetRoleByIdAsync(int roleId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_roles.FirstOrDefault(role => role.Id == roleId));
        }

        public Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(AddedUsers.FirstOrDefault(user => user.Id == userId));
        }

        public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken)
        {
            return Task.FromResult(AddedUsers.Any(user => user.Username == username));
        }

        public void AddUser(User user)
        {
            user.Id = AddedUsers.Count + 1;
            user.Role = _roles.First(role => role.Id == user.RoleId);
            AddedUsers.Add(user);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }

    private sealed class FakeDoctorManagementRepository : IDoctorManagementRepository
    {
        public List<Department> Departments { get; } = new();
        public List<Doctor> AddedDoctors { get; } = new();

        public Task<IReadOnlyCollection<Doctor>> GetAllDoctorsWithDepartmentsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Doctor>>(AddedDoctors);
        }

        public Task<Doctor?> GetDoctorByIdAsync(int doctorId, CancellationToken cancellationToken)
        {
            return Task.FromResult(AddedDoctors.FirstOrDefault(doctor => doctor.Id == doctorId));
        }

        public Task<Doctor?> GetDoctorByUserIdAsync(int userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(AddedDoctors.FirstOrDefault(doctor => doctor.UserId == userId));
        }

        public Task<IReadOnlyCollection<Department>> GetActiveDepartmentsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Department>>(Departments.Where(department => department.IsActive).ToArray());
        }

        public Task<Department?> GetDepartmentByIdAsync(int departmentId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Departments.FirstOrDefault(department => department.Id == departmentId));
        }

        public Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<User?>(new User
            {
                Id = userId,
                Role = new Role { Id = 3, Name = "Doctor", IsActive = true }
            });
        }

        public void AddDoctor(Doctor doctor)
        {
            doctor.Id = AddedDoctors.Count + 1;
            AddedDoctors.Add(doctor);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => $"hash:{password}";
        public bool VerifyPassword(string password, string passwordHash) => passwordHash == $"hash:{password}";
    }
}
