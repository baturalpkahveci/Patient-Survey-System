using Microsoft.EntityFrameworkCore;
using Npgsql;
using PatientSurvey.Application.Exceptions;
using PatientSurvey.Infrastructure.Contracts;
using PatientSurvey.Infrastructure.Persistence;
using AppIAppTransaction = PatientSurvey.Application.Interfaces.IAppTransaction;
using InfraISurveyAccessTokenRepository = PatientSurvey.Infrastructure.Contracts.ISurveyAccessTokenRepository;
using InfraIUserRepository = PatientSurvey.Infrastructure.Contracts.IUserRepository;

namespace PatientSurvey.Infrastructure.EFCore;

public sealed class RepositoryManager : IRepositoryManager
{
    private const string UniqueViolation = "23505";
    private readonly AppDbContext _context;
    private readonly Lazy<ISurveyRepository> _surveyRepository;
    private readonly Lazy<InfraISurveyAccessTokenRepository> _surveyAccessTokenRepository;
    private readonly Lazy<IDepartmentRepository> _departmentRepository;
    private readonly Lazy<IQuestionRepository> _questionRepository;
    private readonly Lazy<ISurveyResponseRepository> _surveyResponseRepository;
    private readonly Lazy<IRoleRepository> _roleRepository;
    private readonly Lazy<IPermissionRepository> _permissionRepository;
    private readonly Lazy<IUserPermissionRepository> _userPermissionRepository;
    private readonly Lazy<InfraIUserRepository> _userRepository;
    private readonly Lazy<IDoctorRepository> _doctorRepository;
    private readonly Lazy<IPatientRepository> _patientRepository;
    private readonly Lazy<IPatientVisitRepository> _patientVisitRepository;
    private readonly Lazy<ISurveyInvitationRepository> _surveyInvitationRepository;
    private readonly Lazy<ISurveyConsentRepository> _surveyConsentRepository;
    private readonly Lazy<IAuditLogRepository> _auditLogRepository;

    public RepositoryManager(AppDbContext context)
    {
        _context = context;
        _surveyRepository = new Lazy<ISurveyRepository>(() => new SurveyRepository(_context));
        _surveyAccessTokenRepository = new Lazy<InfraISurveyAccessTokenRepository>(() => new SurveyAccessTokenRepository(_context));
        _departmentRepository = new Lazy<IDepartmentRepository>(() => new DepartmentRepository(_context));
        _questionRepository = new Lazy<IQuestionRepository>(() => new QuestionRepository(_context));
        _surveyResponseRepository = new Lazy<ISurveyResponseRepository>(() => new SurveyResponseRepository(_context));
        _roleRepository = new Lazy<IRoleRepository>(() => new RoleRepository(_context));
        _permissionRepository = new Lazy<IPermissionRepository>(() => new PermissionRepository(_context));
        _userPermissionRepository = new Lazy<IUserPermissionRepository>(() => new UserPermissionRepository(_context));
        _userRepository = new Lazy<InfraIUserRepository>(() => new UserRepository(_context));
        _doctorRepository = new Lazy<IDoctorRepository>(() => new DoctorRepository(_context));
        _patientRepository = new Lazy<IPatientRepository>(() => new PatientRepository(_context));
        _patientVisitRepository = new Lazy<IPatientVisitRepository>(() => new PatientVisitRepository(_context));
        _surveyInvitationRepository = new Lazy<ISurveyInvitationRepository>(() => new SurveyInvitationRepository(_context));
        _surveyConsentRepository = new Lazy<ISurveyConsentRepository>(() => new SurveyConsentRepository(_context));
        _auditLogRepository = new Lazy<IAuditLogRepository>(() => new AuditLogRepository(_context));
    }

    public ISurveyRepository Surveys => _surveyRepository.Value;
    public InfraISurveyAccessTokenRepository SurveyAccessTokens => _surveyAccessTokenRepository.Value;
    public IDepartmentRepository Departments => _departmentRepository.Value;
    public IQuestionRepository Questions => _questionRepository.Value;
    public ISurveyResponseRepository SurveyResponses => _surveyResponseRepository.Value;
    public IRoleRepository Roles => _roleRepository.Value;
    public IPermissionRepository Permissions => _permissionRepository.Value;
    public IUserPermissionRepository UserPermissions => _userPermissionRepository.Value;
    public InfraIUserRepository Users => _userRepository.Value;
    public IDoctorRepository Doctors => _doctorRepository.Value;
    public IPatientRepository Patients => _patientRepository.Value;
    public IPatientVisitRepository PatientVisits => _patientVisitRepository.Value;
    public ISurveyInvitationRepository SurveyInvitations => _surveyInvitationRepository.Value;
    public ISurveyConsentRepository SurveyConsents => _surveyConsentRepository.Value;
    public IAuditLogRepository AuditLogs => _auditLogRepository.Value;

    public async Task<AppIAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new EfAppTransaction(transaction);
    }

    public async Task<int> SaveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: UniqueViolation })
        {
            throw new BusinessRuleException("Bu anket daha önce gönderilmiş.");
        }
    }
}
