using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Application.Interfaces;

public interface IDoctorManagementRepository
{
    Task<IReadOnlyCollection<Doctor>> GetAllDoctorsWithDepartmentsAsync(CancellationToken cancellationToken);
    Task<Doctor?> GetDoctorByIdAsync(int doctorId, CancellationToken cancellationToken);
    Task<Doctor?> GetDoctorByUserIdAsync(int userId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Department>> GetActiveDepartmentsAsync(CancellationToken cancellationToken);
    Task<Department?> GetDepartmentByIdAsync(int departmentId, CancellationToken cancellationToken);
    Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken);
    void AddDoctor(Doctor doctor);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
