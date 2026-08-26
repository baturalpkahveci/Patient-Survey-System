using PatientSurvey.Application.DTOs.Doctor;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Application.Services;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.UnitTests;

public sealed class DoctorServiceTests
{
    [Fact]
    public async Task GetDoctorProfileAsync_returns_not_found_when_profile_or_department_is_missing()
    {
        var repository = new FakeDoctorManagementRepository();
        var service = new DoctorService(repository);

        Assert.Equal("doctor_not_found", (await service.GetDoctorProfileAsync(10)).ErrorCode);

        repository.Doctors.Add(new Doctor { Id = 1, UserId = 10, DepartmentId = 2 });

        Assert.Equal("doctor_not_found", (await service.GetDoctorProfileAsync(10)).ErrorCode);
    }

    [Fact]
    public async Task GetDoctorProfileAsync_maps_profile_and_department_status()
    {
        var repository = new FakeDoctorManagementRepository();
        repository.Doctors.Add(new Doctor
        {
            Id = 4,
            UserId = 10,
            FirstName = "Ayse",
            LastName = "Yilmaz",
            DepartmentId = 2,
            Department = new Department { Id = 2, Name = "Dahiliye", IsActive = false },
            IsActive = true
        });

        var result = await new DoctorService(repository).GetDoctorProfileAsync(10);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value!.Id);
        Assert.Equal("Ayse", result.Value.FirstName);
        Assert.Equal("Yilmaz", result.Value.LastName);
        Assert.Equal("Dahiliye", result.Value.DepartmentName);
        Assert.True(result.Value.IsActive);
        Assert.False(result.Value.DepartmentIsActive);
    }

    [Fact]
    public async Task GetDoctorsAsync_and_department_options_are_ordered_and_mapped()
    {
        var repository = new FakeDoctorManagementRepository();
        repository.Departments.AddRange(new[]
        {
            new Department { Id = 2, Name = "Z", IsActive = true },
            new Department { Id = 1, Name = "A", IsActive = true },
            new Department { Id = 3, Name = "Inactive", IsActive = false }
        });
        repository.Doctors.AddRange(new[]
        {
            new Doctor
            {
                Id = 2,
                UserId = 20,
                FirstName = "B",
                LastName = "Y",
                User = new User { Username = "doctor-y" },
                DepartmentId = 2,
                Department = repository.Departments[0],
                IsActive = true
            },
            new Doctor
            {
                Id = 1,
                UserId = 10,
                FirstName = "A",
                LastName = "X",
                User = new User { Username = "doctor-x" },
                DepartmentId = 1,
                Department = repository.Departments[1],
                IsActive = false
            }
        });
        var service = new DoctorService(repository);

        var doctors = await service.GetDoctorsAsync();
        var departments = await service.GetDepartmentOptionsAsync();

        Assert.Equal(new[] { 1, 2 }, doctors.Select(doctor => doctor.Id));
        Assert.Equal("doctor-x", doctors.First().Username);
        Assert.Equal("A", doctors.First().DepartmentName);
        Assert.Equal(new[] { "A", "Z" }, departments.Select(department => department.Name));
    }

    [Fact]
    public async Task UpdateDoctorDepartmentAsync_validates_doctor_and_active_department_then_saves()
    {
        var repository = new FakeDoctorManagementRepository();
        var service = new DoctorService(repository);

        Assert.Equal("doctor_not_found", (await service.UpdateDoctorDepartmentAsync(new UpdateDoctorDepartmentRequestDto(1, 2))).ErrorCode);

        repository.Doctors.Add(new Doctor { Id = 1, UserId = 10, DepartmentId = 1 });
        repository.Departments.Add(new Department { Id = 2, Name = "Pasif", IsActive = false });
        Assert.Equal("department_invalid", (await service.UpdateDoctorDepartmentAsync(new UpdateDoctorDepartmentRequestDto(1, 2))).ErrorCode);

        repository.Departments.Add(new Department { Id = 3, Name = "Acil", IsActive = true });
        var result = await service.UpdateDoctorDepartmentAsync(new UpdateDoctorDepartmentRequestDto(1, 3));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, repository.Doctors[0].DepartmentId);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task UpsertDoctorProfileAsync_validates_role_department_and_creates_or_updates_profile()
    {
        var repository = new FakeDoctorManagementRepository();
        var service = new DoctorService(repository);

        Assert.Equal("doctor_first_name_required", (await service.UpsertDoctorProfileAsync(new UpsertDoctorProfileRequestDto(10, " ", "Yilmaz", 1))).ErrorCode);
        Assert.Equal("doctor_last_name_required", (await service.UpsertDoctorProfileAsync(new UpsertDoctorProfileRequestDto(10, "Ayse", " ", 1))).ErrorCode);

        repository.Users.Add(new User
        {
            Id = 10,
            Role = new Role { Id = 2, Name = "Manager", IsActive = true }
        });
        Assert.Equal("user_not_doctor", (await service.UpsertDoctorProfileAsync(new UpsertDoctorProfileRequestDto(10, "Ayse", "Yilmaz", 1))).ErrorCode);

        repository.Users[0].Role = new Role { Id = 3, Name = "Doctor", IsActive = true };
        repository.Departments.Add(new Department { Id = 1, Name = "Pasif", IsActive = false });
        Assert.Equal("department_invalid", (await service.UpsertDoctorProfileAsync(new UpsertDoctorProfileRequestDto(10, "Ayse", "Yilmaz", 1))).ErrorCode);

        repository.Departments.Add(new Department { Id = 2, Name = "Acil", IsActive = true });
        var createResult = await service.UpsertDoctorProfileAsync(new UpsertDoctorProfileRequestDto(10, " Ayse ", " Yilmaz ", 2));

        Assert.True(createResult.IsSuccess);
        var doctor = Assert.Single(repository.AddedDoctors);
        Assert.Equal("Ayse", doctor.FirstName);
        Assert.Equal("Yilmaz", doctor.LastName);
        Assert.Equal(2, doctor.DepartmentId);
        Assert.True(doctor.IsActive);

        repository.Doctors.Add(doctor);
        var updateResult = await service.UpsertDoctorProfileAsync(new UpsertDoctorProfileRequestDto(10, "Ali", "Can", 2));

        Assert.True(updateResult.IsSuccess);
        Assert.Equal("Ali", doctor.FirstName);
        Assert.Equal("Can", doctor.LastName);
        Assert.Equal(2, repository.SaveCount);
    }

    private sealed class FakeDoctorManagementRepository : IDoctorManagementRepository
    {
        public List<Doctor> Doctors { get; } = new();
        public List<Department> Departments { get; } = new();
        public List<User> Users { get; } = new();
        public List<Doctor> AddedDoctors { get; } = new();
        public int SaveCount { get; private set; }

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
            return Task.FromResult<IReadOnlyCollection<Department>>(Departments.Where(department => department.IsActive).ToArray());
        }

        public Task<Department?> GetDepartmentByIdAsync(int departmentId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Departments.FirstOrDefault(department => department.Id == departmentId));
        }

        public Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Users.FirstOrDefault(user => user.Id == userId));
        }

        public void AddDoctor(Doctor doctor)
        {
            doctor.Id = AddedDoctors.Count + 1;
            AddedDoctors.Add(doctor);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
