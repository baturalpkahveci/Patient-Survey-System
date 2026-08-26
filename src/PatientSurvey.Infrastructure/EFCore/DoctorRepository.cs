using Microsoft.EntityFrameworkCore;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;
using PatientSurvey.Infrastructure.Persistence;

namespace PatientSurvey.Infrastructure.EFCore;

public sealed class DoctorRepository :
    RepositoryBase<Doctor>,
    IDoctorRepository
{
    public DoctorRepository(AppDbContext context)
        : base(context)
    {
    }

    public IQueryable<Doctor> GetAllDoctors(bool trackChanges)
    {
        return FindAll(trackChanges)
            .Include(doctor => doctor.Department)
            .Include(doctor => doctor.User)
            .OrderBy(doctor => doctor.LastName)
            .ThenBy(doctor => doctor.FirstName);
    }

    public Task<Doctor?> GetOneDoctorByIdAsync(int doctorId, bool trackChanges, CancellationToken cancellationToken)
    {
        return FindByCondition(doctor => doctor.Id == doctorId, trackChanges)
            .Include(doctor => doctor.Department)
            .Include(doctor => doctor.User)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Doctor?> GetOneDoctorByUserIdAsync(int userId, bool trackChanges, CancellationToken cancellationToken)
    {
        return FindByCondition(doctor => doctor.UserId == userId, trackChanges)
            .Include(doctor => doctor.Department)
            .Include(doctor => doctor.User)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void CreateOneDoctor(Doctor doctor)
    {
        Create(doctor);
    }

    public void UpdateOneDoctor(Doctor doctor)
    {
        Update(doctor);
    }
}
