using Microsoft.EntityFrameworkCore;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();
    public DbSet<Answer> Answers => Set<Answer>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<SurveyAccessToken> SurveyAccessTokens => Set<SurveyAccessToken>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientVisit> PatientVisits => Set<PatientVisit>();
    public DbSet<SurveyInvitation> SurveyInvitations => Set<SurveyInvitation>();
    public DbSet<SurveyConsent> SurveyConsents => Set<SurveyConsent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
