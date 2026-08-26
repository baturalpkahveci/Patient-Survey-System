using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private static readonly HashSet<string> SensitiveProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(User.PasswordHash),
        nameof(Patient.TcIdentityLookupHash),
        nameof(Patient.FirstName),
        nameof(Patient.LastName),
        nameof(Patient.PhoneNumber),
        nameof(Patient.Email),
        nameof(SurveyAccessToken.Token)
    };

    private readonly ICurrentUserContext? _currentUserContext;
    private bool _savingAudit;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserContext currentUserContext)
        : base(options)
    {
        _currentUserContext = currentUserContext;
    }

    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();
    public DbSet<Answer> Answers => Set<Answer>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<SurveyAccessToken> SurveyAccessTokens => Set<SurveyAccessToken>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientVisit> PatientVisits => Set<PatientVisit>();
    public DbSet<SurveyInvitation> SurveyInvitations => Set<SurveyInvitation>();
    public DbSet<SurveyConsent> SurveyConsents => Set<SurveyConsent>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_savingAudit)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

        var pendingAuditLogs = CreatePendingAuditLogs();
        foreach (var pendingAuditLog in pendingAuditLogs)
        {
            AuditLogs.Add(pendingAuditLog.AuditLog);
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        var logsToRefreshAfterSave = pendingAuditLogs
            .Where(pending =>
                pending.State == EntityState.Added ||
                string.IsNullOrWhiteSpace(pending.AuditLog.EntityId))
            .ToArray();

        if (logsToRefreshAfterSave.Length > 0)
        {
            foreach (var pending in logsToRefreshAfterSave)
            {
                pending.AuditLog.EntityId = GetPrimaryKeyValue(pending.Entry);
                pending.AuditLog.Summary = BuildSummary(
                    pending.AuditLog.Action,
                    pending.AuditLog.EntityName,
                    pending.AuditLog.EntityId,
                    pending.Entry);

                if (pending.State == EntityState.Added)
                {
                    var changes = BuildAddedChangesAfterSave(pending.Entry);
                    pending.AuditLog.ChangesJson = changes.Count == 0 ? null : JsonSerializer.Serialize(changes);
                }
            }

            _savingAudit = true;
            try
            {
                await base.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                _savingAudit = false;
            }
        }

        return result;
    }

    private List<PendingAuditLog> CreatePendingAuditLogs()
    {
        ChangeTracker.DetectChanges();

        return ChangeTracker.Entries()
            .Where(entry =>
                entry.Entity is not AuditLog &&
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
                !entry.Metadata.IsOwned())
            .Select(CreatePendingAuditLog)
            .Where(pending => pending is not null)
            .Select(pending => pending!)
            .ToList();
    }

    private PendingAuditLog? CreatePendingAuditLog(EntityEntry entry)
    {
        var action = entry.State switch
        {
            EntityState.Added => "Ekleme",
            EntityState.Modified => "Güncelleme",
            EntityState.Deleted => "Silme",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(action))
        {
            return null;
        }

        var changes = BuildChanges(entry);
        if (entry.State == EntityState.Modified && changes.Count == 0)
        {
            return null;
        }

        var entityName = ToEntityDisplayName(entry.Metadata.ClrType.Name);
        var entityId = GetPrimaryKeyValue(entry);
        var summary = BuildSummary(action, entityName, entityId, entry);
        var currentUser = _currentUserContext;

        return new PendingAuditLog(
            entry,
            new AuditLog
            {
                OccurredAtUtc = DateTimeOffset.UtcNow,
                UserId = currentUser?.UserId,
                Username = string.IsNullOrWhiteSpace(currentUser?.Username) ? "Sistem" : currentUser!.Username,
                UserRole = currentUser?.Role,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Summary = summary,
                ChangesJson = changes.Count == 0 ? null : JsonSerializer.Serialize(changes),
                IpAddress = currentUser?.IpAddress,
                RequestPath = currentUser?.RequestPath
            },
            entry.State);
    }

    private static Dictionary<string, object?> BuildChanges(EntityEntry entry)
    {
        var changes = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsPrimaryKey())
            {
                continue;
            }

            var propertyName = property.Metadata.Name;
            var shouldRecord = entry.State is EntityState.Added or EntityState.Deleted
                || (entry.State == EntityState.Modified
                    && property.IsModified
                    && !Equals(property.OriginalValue, property.CurrentValue));

            if (!shouldRecord)
            {
                continue;
            }

            if (SensitiveProperties.Contains(propertyName))
            {
                changes[propertyName] = "[gizli]";
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                changes[propertyName] = NormalizeValue(property.CurrentValue);
            }
            else if (entry.State == EntityState.Deleted)
            {
                changes[propertyName] = NormalizeValue(property.OriginalValue);
            }
            else
            {
                changes[propertyName] = new
                {
                    Eski = NormalizeValue(property.OriginalValue),
                    Yeni = NormalizeValue(property.CurrentValue)
                };
            }
        }

        return changes;
    }

    private static Dictionary<string, object?> BuildAddedChangesAfterSave(EntityEntry entry)
    {
        var changes = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsPrimaryKey())
            {
                continue;
            }

            var propertyName = property.Metadata.Name;
            changes[propertyName] = SensitiveProperties.Contains(propertyName)
                ? "[gizli]"
                : NormalizeValue(property.CurrentValue);
        }

        return changes;
    }

    private static object? NormalizeValue(object? value)
    {
        return value switch
        {
            null => null,
            DateTimeOffset dateTime => dateTime.ToString("O"),
            DateTime dateTime => dateTime.ToString("O"),
            _ => value
        };
    }

    private static string? GetPrimaryKeyValue(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null)
        {
            return null;
        }

        var values = key.Properties
            .Select(property =>
            {
                var propertyEntry = entry.Property(property.Name);
                return propertyEntry.IsTemporary ? null : propertyEntry.CurrentValue?.ToString();
            })
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        return values.Length == 0 ? null : string.Join(",", values);
    }

    private static string BuildSummary(string action, string entityName, string? entityId, EntityEntry entry)
    {
        var label = TryGetEntityLabel(entry);
        var target = string.IsNullOrWhiteSpace(label)
            ? string.IsNullOrWhiteSpace(entityId) ? entityName : $"{entityName} #{entityId}"
            : $"{entityName}: {label}";

        return $"{target} için {action.ToLowerInvariant()} işlemi yapıldı.";
    }

    private static string? TryGetEntityLabel(EntityEntry entry)
    {
        return entry.Entity switch
        {
            User user => user.Username,
            Role role => role.Name,
            Department department => department.Name,
            Doctor doctor => $"Dr. {doctor.FirstName} {doctor.LastName}".Trim(),
            Patient patient => patient.Id > 0 ? $"Hasta #{patient.Id}" : "Hasta",
            Permission permission => permission.Name,
            UserPermission userPermission => userPermission.Permission?.Name ?? $"Yetki #{userPermission.PermissionId}",
            Survey survey => survey.Title,
            Question question => question.Text,
            SurveyAccessToken token => $"Anket #{token.SurveyId}",
            SurveyInvitation invitation => $"Davet #{invitation.Id}",
            PatientVisit visit => $"Hasta ziyareti #{visit.Id}",
            SurveyResponse response => $"Anket cevabı #{response.Id}",
            Answer answer => $"Cevap #{answer.Id}",
            SurveyConsent consent => $"KVKK onayı #{consent.Id}",
            _ => null
        };
    }

    private static string ToEntityDisplayName(string entityName)
    {
        return entityName switch
        {
            nameof(User) => "Kullanıcı",
            nameof(Role) => "Rol",
            nameof(Permission) => "Yetki",
            nameof(UserPermission) => "Kullanıcı Yetkisi",
            nameof(Department) => "Bölüm",
            nameof(Doctor) => "Doktor",
            nameof(Patient) => "Hasta",
            nameof(PatientVisit) => "Hasta Kaydı",
            nameof(Survey) => "Anket",
            nameof(Question) => "Soru",
            nameof(SurveyAccessToken) => "Anket Linki",
            nameof(SurveyInvitation) => "Hasta Daveti",
            nameof(SurveyResponse) => "Anket Cevabı",
            nameof(Answer) => "Cevap",
            nameof(SurveyConsent) => "KVKK Onayı",
            _ => entityName
        };
    }

    private sealed record PendingAuditLog(EntityEntry Entry, AuditLog AuditLog, EntityState State);
}
