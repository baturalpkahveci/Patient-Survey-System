using Microsoft.EntityFrameworkCore;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;

namespace PatientSurvey.Infrastructure.EFCore;

internal sealed class DoctorManagementRepository : IDoctorManagementRepository
{
    private readonly IRepositoryManager _repositoryManager;

    public DoctorManagementRepository(IRepositoryManager repositoryManager)
    {
        _repositoryManager = repositoryManager;
    }

    public async Task<IReadOnlyCollection<Doctor>> GetAllDoctorsWithDepartmentsAsync(CancellationToken cancellationToken)
    {
        return await _repositoryManager.Doctors
            .GetAllDoctors(trackChanges: false)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Doctor?> GetDoctorByIdAsync(int doctorId, CancellationToken cancellationToken)
    {
        return _repositoryManager.Doctors.GetOneDoctorByIdAsync(doctorId, trackChanges: true, cancellationToken);
    }

    public Task<Doctor?> GetDoctorByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        return _repositoryManager.Doctors.GetOneDoctorByUserIdAsync(userId, trackChanges: true, cancellationToken);
    }

    public Task<IReadOnlyCollection<Department>> GetActiveDepartmentsAsync(CancellationToken cancellationToken)
    {
        return _repositoryManager.Departments.GetActiveDepartmentsAsync(cancellationToken);
    }

    public Task<Department?> GetDepartmentByIdAsync(int departmentId, CancellationToken cancellationToken)
    {
        return _repositoryManager.Departments.GetOneDepartmentByIdAsync(
            departmentId,
            trackChanges: false,
            cancellationToken);
    }

    public Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken)
    {
        return _repositoryManager.Users.GetOneUserByIdAsync(userId, trackChanges: false, cancellationToken);
    }

    public void AddDoctor(Doctor doctor)
    {
        _repositoryManager.Doctors.CreateOneDoctor(doctor);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _repositoryManager.SaveAsync(cancellationToken);
    }
}
