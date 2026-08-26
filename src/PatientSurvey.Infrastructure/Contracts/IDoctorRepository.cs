using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Contracts;

public interface IDoctorRepository :
    IRepositoryBase<Doctor>
{
    IQueryable<Doctor> GetAllDoctors(bool trackChanges);
    Task<Doctor?> GetOneDoctorByIdAsync(int doctorId, bool trackChanges, CancellationToken cancellationToken);
    Task<Doctor?> GetOneDoctorByUserIdAsync(int userId, bool trackChanges, CancellationToken cancellationToken);
    void CreateOneDoctor(Doctor doctor);
    void UpdateOneDoctor(Doctor doctor);
}
