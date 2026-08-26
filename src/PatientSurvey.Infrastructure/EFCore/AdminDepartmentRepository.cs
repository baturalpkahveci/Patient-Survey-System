using Microsoft.EntityFrameworkCore;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;

namespace PatientSurvey.Infrastructure.EFCore;

internal sealed class AdminDepartmentRepository : IAdminDepartmentRepository
{
    private readonly IRepositoryManager _repositoryManager;

    public AdminDepartmentRepository(IRepositoryManager repositoryManager)
    {
        _repositoryManager = repositoryManager;
    }

    public async Task<IReadOnlyCollection<Department>> GetAllDepartmentsWithResponsesAsync(CancellationToken cancellationToken)
    {
        return await _repositoryManager.Departments
            .FindAll(trackChanges: false)
            .Include(department => department.SurveyResponses)
            .Include(department => department.Surveys)
            .Include(department => department.Doctors)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Department?> GetDepartmentByIdAsync(int departmentId, CancellationToken cancellationToken)
    {
        return _repositoryManager.Departments.GetOneDepartmentByIdAsync(
            departmentId,
            trackChanges: true,
            cancellationToken);
    }

    public Task<bool> DepartmentNameExistsAsync(string name, CancellationToken cancellationToken)
    {
        return _repositoryManager.Departments
            .FindByCondition(department => department.Name == name, trackChanges: false)
            .AnyAsync(cancellationToken);
    }

    public Task<bool> DepartmentNameExistsForAnotherAsync(
        int departmentId,
        string name,
        CancellationToken cancellationToken)
    {
        return _repositoryManager.Departments
            .FindByCondition(department => department.Id != departmentId && department.Name == name, trackChanges: false)
            .AnyAsync(cancellationToken);
    }

    public void AddDepartment(Department department)
    {
        _repositoryManager.Departments.CreateOneDepartment(department);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _repositoryManager.SaveAsync(cancellationToken);
    }
}
