using PatientSurvey.Application.DTOs.Department;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Application.Services;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.UnitTests;

public sealed class DepartmentServiceTests
{
    [Fact]
    public async Task GetActiveDepartmentsAsync_returns_only_active_departments_ordered()
    {
        var readRepository = new FakeSurveyReadRepository();
        readRepository.Departments.Add(new Department { Id = 2, Name = "Z", IsActive = true });
        readRepository.Departments.Add(new Department { Id = 1, Name = "A", IsActive = true });
        readRepository.Departments.Add(new Department { Id = 3, Name = "Hidden", IsActive = false });
        var service = new DepartmentService(readRepository, new FakeAdminDepartmentRepository());

        var result = await service.GetActiveDepartmentsAsync();

        Assert.Equal(new[] { "A", "Z" }, result.Select(department => department.Name));
    }

    [Fact]
    public async Task GetAdminDepartmentsAsync_counts_related_responses_surveys_and_doctors()
    {
        var adminRepository = new FakeAdminDepartmentRepository();
        adminRepository.Departments.Add(new Department
        {
            Id = 1,
            Name = "Acil",
            IsActive = true,
            SurveyResponses = { new SurveyResponse(), new SurveyResponse() },
            Surveys = { new Survey(), new Survey(), new Survey() },
            Doctors = { new Doctor(), new Doctor() }
        });
        var service = new DepartmentService(new FakeSurveyReadRepository(), adminRepository);

        var result = await service.GetAdminDepartmentsAsync();

        var department = Assert.Single(result);
        Assert.Equal(2, department.ResponseCount);
        Assert.Equal(3, department.SurveyCount);
        Assert.Equal(2, department.DoctorCount);
    }

    [Fact]
    public async Task CreateDepartmentAsync_validates_name_and_duplicates()
    {
        var adminRepository = new FakeAdminDepartmentRepository();
        adminRepository.ExistingNames.Add("Acil");
        var service = new DepartmentService(new FakeSurveyReadRepository(), adminRepository);

        Assert.Equal("department_name_required", (await service.CreateDepartmentAsync(new CreateDepartmentRequestDto(" ", true))).ErrorCode);
        Assert.Equal("department_exists", (await service.CreateDepartmentAsync(new CreateDepartmentRequestDto(" Acil ", true))).ErrorCode);
    }

    [Fact]
    public async Task CreateDepartmentAsync_persists_and_toggle_flips_status()
    {
        var adminRepository = new FakeAdminDepartmentRepository();
        var service = new DepartmentService(new FakeSurveyReadRepository(), adminRepository);

        var createResult = await service.CreateDepartmentAsync(new CreateDepartmentRequestDto(" Acil ", false));

        Assert.True(createResult.IsSuccess);
        var department = Assert.Single(adminRepository.AddedDepartments);
        Assert.Equal("Acil", department.Name);
        Assert.False(department.IsActive);

        Assert.Equal("department_not_found", (await service.ToggleDepartmentStatusAsync(5)).ErrorCode);
        adminRepository.Departments.Add(new Department { Id = 5, Name = "Acil", IsActive = false });
        var toggleResult = await service.ToggleDepartmentStatusAsync(5);

        Assert.True(toggleResult.IsSuccess);
        Assert.True(adminRepository.Departments[0].IsActive);
    }

    private sealed class FakeSurveyReadRepository : ISurveyReadRepository
    {
        public List<Department> Departments { get; } = new();

        public Task<SurveyAccessToken?> GetTokenWithActiveSurveyAsync(string token, CancellationToken cancellationToken)
        {
            return Task.FromResult<SurveyAccessToken?>(null);
        }

        public Task<IReadOnlyCollection<Department>> GetActiveDepartmentsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Department>>(Departments.Where(department => department.IsActive).ToArray());
        }
    }

    private sealed class FakeAdminDepartmentRepository : IAdminDepartmentRepository
    {
        public List<Department> Departments { get; } = new();
        public HashSet<string> ExistingNames { get; } = new(StringComparer.Ordinal);
        public List<Department> AddedDepartments { get; } = new();

        public Task<IReadOnlyCollection<Department>> GetAllDepartmentsWithResponsesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Department>>(Departments);
        }

        public Task<Department?> GetDepartmentByIdAsync(int departmentId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Departments.FirstOrDefault(department => department.Id == departmentId));
        }

        public Task<bool> DepartmentNameExistsAsync(string name, CancellationToken cancellationToken)
        {
            return Task.FromResult(ExistingNames.Contains(name));
        }

        public Task<bool> DepartmentNameExistsForAnotherAsync(int departmentId, string name, CancellationToken cancellationToken)
        {
            return Task.FromResult(Departments.Any(department => department.Id != departmentId && department.Name == name));
        }

        public void AddDepartment(Department department) => AddedDepartments.Add(department);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }
}
