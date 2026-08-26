using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Application.Services;
using PatientSurvey.Domain.Entities;
using PatientSurvey.WebUI.Controllers;
using PatientSurvey.WebUI.ViewModels.Account;

namespace PatientSurvey.IntegrationTests;

public sealed class AccountControllerTests
{
    [Fact]
    public async Task Login_with_doctor_credentials_adds_doctor_claims_and_redirects_to_doctor_dashboard()
    {
        var userRepository = new FakeUserRepository
        {
            User = new User
            {
                Id = 5,
                Username = "doctor1",
                PasswordHash = "hash:password1",
                IsActive = true,
                Role = new Role { Id = 3, Name = "Doctor", IsActive = true }
            }
        };
        var doctorRepository = new FakeDoctorManagementRepository();
        doctorRepository.Doctors.Add(new Doctor
        {
            Id = 9,
            UserId = 5,
            FirstName = "Ayse",
            LastName = "Yilmaz",
            DepartmentId = 2,
            Department = new Department { Id = 2, Name = "Dahiliye", IsActive = true },
            IsActive = true
        });
        var authService = new RecordingAuthenticationService();
        var controller = CreateController(userRepository, doctorRepository, authService);

        var result = await controller.Login(new LoginViewModel
        {
            Username = " doctor1 ",
            Password = "password1"
        }, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Dashboard", redirect.ControllerName);
        Assert.Equal("Doctor", redirect.RouteValues!["area"]);
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, authService.SignedInScheme);
        Assert.Contains(authService.SignedInPrincipal!.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "Doctor");
        Assert.Contains(authService.SignedInPrincipal.Claims, claim => claim.Type == "doctor_display_name" && claim.Value == "Dr. Ayse Yilmaz");
        Assert.Contains(authService.SignedInPrincipal.Claims, claim => claim.Type == "doctor_department_name" && claim.Value == "Dahiliye");
    }

    [Fact]
    public async Task Login_with_invalid_credentials_returns_view_and_does_not_sign_in()
    {
        var authService = new RecordingAuthenticationService();
        var controller = CreateController(new FakeUserRepository(), new FakeDoctorManagementRepository(), authService);

        var result = await controller.Login(new LoginViewModel
        {
            Username = "missing",
            Password = "password1"
        }, CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Null(authService.SignedInPrincipal);
    }

    private static AccountController CreateController(
        FakeUserRepository userRepository,
        FakeDoctorManagementRepository doctorRepository,
        RecordingAuthenticationService authService)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddControllersWithViews();
        serviceCollection.AddSingleton<IAuthenticationService>(authService);
        var services = serviceCollection.BuildServiceProvider();

        var controller = new AccountController(
            new UserService(userRepository, new EmptyAdminUserRepository(), new FakePasswordHasher()),
            new DoctorService(doctorRepository),
            NullLogger<AccountController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = services }
            },
            Url = new FakeUrlHelper()
        };

        return controller;
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public User? User { get; init; }

        public Task<User?> GetActiveUserByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            return Task.FromResult(User is { IsActive: true } && User.Username == username ? User : null);
        }
    }

    private sealed class EmptyAdminUserRepository : IAdminUserRepository
    {
        public Task<IReadOnlyCollection<User>> GetAllUsersWithRolesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<User>>(Array.Empty<User>());
        }

        public Task<IReadOnlyCollection<Role>> GetActiveRolesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Role>>(Array.Empty<Role>());
        }

        public Task<Role?> GetRoleByIdAsync(int roleId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Role?>(null);
        }

        public Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<User?>(null);
        }

        public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public void AddUser(User user)
        {
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }

    private sealed class FakeDoctorManagementRepository : IDoctorManagementRepository
    {
        public List<Doctor> Doctors { get; } = new();

        public Task<IReadOnlyCollection<Doctor>> GetAllDoctorsWithDepartmentsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Doctor>>(Doctors);
        }

        public Task<Doctor?> GetDoctorByIdAsync(int doctorId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Doctors.FirstOrDefault(doctor => doctor.Id == doctorId));
        }

        public Task<Doctor?> GetDoctorByUserIdAsync(int userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Doctors.FirstOrDefault(doctor => doctor.UserId == userId));
        }

        public Task<IReadOnlyCollection<Department>> GetActiveDepartmentsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Department>>(Array.Empty<Department>());
        }

        public Task<Department?> GetDepartmentByIdAsync(int departmentId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Department?>(null);
        }

        public Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<User?>(null);
        }

        public void AddDoctor(Doctor doctor)
        {
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

    private sealed class RecordingAuthenticationService : IAuthenticationService
    {
        public string? SignedInScheme { get; private set; }
        public ClaimsPrincipal? SignedInPrincipal { get; private set; }
        public string? SignedOutScheme { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            return Task.CompletedTask;
        }

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            return Task.CompletedTask;
        }

        public Task SignInAsync(
            HttpContext context,
            string? scheme,
            ClaimsPrincipal principal,
            AuthenticationProperties? properties)
        {
            SignedInScheme = scheme;
            SignedInPrincipal = principal;
            return Task.CompletedTask;
        }

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            SignedOutScheme = scheme;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext { get; } = new();

        public string? Action(UrlActionContext actionContext)
        {
            return "/";
        }

        public string? Content(string? contentPath)
        {
            return contentPath;
        }

        public bool IsLocalUrl(string? url)
        {
            return !string.IsNullOrWhiteSpace(url) && url.StartsWith("/", StringComparison.Ordinal);
        }

        public string? Link(string? routeName, object? values)
        {
            return "/";
        }

        public string? RouteUrl(UrlRouteContext routeContext)
        {
            return "/";
        }
    }
}
