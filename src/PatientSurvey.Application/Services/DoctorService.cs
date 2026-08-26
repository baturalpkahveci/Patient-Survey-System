using PatientSurvey.Application.Common;
using PatientSurvey.Application.DTOs.Department;
using PatientSurvey.Application.DTOs.Doctor;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Application.Services;

public sealed class DoctorService
{
    private readonly IDoctorManagementRepository _repository;

    public DoctorService(IDoctorManagementRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<DoctorProfileDto>> GetDoctorProfileAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var doctor = await _repository.GetDoctorByUserIdAsync(userId, cancellationToken);
        if (doctor?.Department is null)
        {
            return ServiceResult<DoctorProfileDto>.Failure("doctor_not_found", "Doktor kaydı bulunamadı.");
        }

        return ServiceResult<DoctorProfileDto>.Success(new DoctorProfileDto(
            doctor.Id,
            doctor.UserId,
            doctor.DepartmentId,
            doctor.FirstName,
            doctor.LastName,
            doctor.Department.Name,
            doctor.IsActive,
            doctor.Department.IsActive));
    }

    public async Task<IReadOnlyCollection<AdminDoctorListItemDto>> GetDoctorsAsync(
        CancellationToken cancellationToken = default)
    {
        var doctors = await _repository.GetAllDoctorsWithDepartmentsAsync(cancellationToken);
        return doctors
            .OrderBy(doctor => doctor.LastName)
            .ThenBy(doctor => doctor.FirstName)
            .Select(doctor => new AdminDoctorListItemDto(
                doctor.Id,
                doctor.UserId,
                doctor.User?.Username ?? string.Empty,
                doctor.FirstName,
                doctor.LastName,
                doctor.DepartmentId,
                doctor.Department?.Name ?? string.Empty,
                doctor.IsActive))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<DepartmentDto>> GetDepartmentOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var departments = await _repository.GetActiveDepartmentsAsync(cancellationToken);
        return departments
            .OrderBy(department => department.Name)
            .Select(department => new DepartmentDto(department.Id, department.Name))
            .ToArray();
    }

    public async Task<ServiceResult> UpdateDoctorDepartmentAsync(
        UpdateDoctorDepartmentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var doctor = await _repository.GetDoctorByIdAsync(request.DoctorId, cancellationToken);
        if (doctor is null)
        {
            return ServiceResult.Failure("doctor_not_found", "Doktor bulunamadı.");
        }

        var department = await _repository.GetDepartmentByIdAsync(request.DepartmentId, cancellationToken);
        if (department is null || !department.IsActive)
        {
            return ServiceResult.Failure("department_invalid", "Geçerli ve aktif bir bölüm seçin.");
        }

        doctor.DepartmentId = department.Id;
        await _repository.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpsertDoctorProfileAsync(
        UpsertDoctorProfileRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return ServiceResult.Failure("doctor_first_name_required", "Doktor adı zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return ServiceResult.Failure("doctor_last_name_required", "Doktor soyadı zorunludur.");
        }

        var user = await _repository.GetUserByIdAsync(request.UserId, cancellationToken);
        if (user?.Role is null || !string.Equals(user.Role.Name, "Doctor", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult.Failure("user_not_doctor", "Sadece doktor rolündeki kullanıcılar doktor profiline bağlanabilir.");
        }

        var department = await _repository.GetDepartmentByIdAsync(request.DepartmentId, cancellationToken);
        if (department is null || !department.IsActive)
        {
            return ServiceResult.Failure("department_invalid", "Geçerli ve aktif bir bölüm seçin.");
        }

        var doctor = await _repository.GetDoctorByUserIdAsync(request.UserId, cancellationToken);
        if (doctor is null)
        {
            _repository.AddDoctor(new Doctor
            {
                UserId = user.Id,
                FirstName = firstName,
                LastName = lastName,
                DepartmentId = department.Id,
                IsActive = true
            });
        }
        else
        {
            doctor.FirstName = firstName;
            doctor.LastName = lastName;
            doctor.DepartmentId = department.Id;
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }
}
