using PatientSurvey.Application.Common;
using PatientSurvey.Application.DTOs.User;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Application.Security;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Application.Services;

public sealed class PermissionService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public PermissionService(
        IPermissionRepository permissionRepository,
        IAuditLogRepository auditLogRepository,
        ICurrentUserContext currentUserContext)
    {
        _permissionRepository = permissionRepository;
        _auditLogRepository = auditLogRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<bool> CanCurrentUserViewPatientPersonalDataAsync(
        string source,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.UserId.HasValue)
        {
            return false;
        }

        var canView = await UserHasPermissionAsync(
            _currentUserContext.UserId.Value,
            AppPermissions.CanViewPatientPersonalData,
            cancellationToken);

        if (canView)
        {
            await AuditPatientPersonalDataViewAsync(source, cancellationToken);
        }

        return canView;
    }

    public async Task<IReadOnlyCollection<string>> GetUserPermissionNamesAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _permissionRepository.GetUserPermissionProfileAsync(
            userId,
            trackChanges: false,
            cancellationToken);

        return user is null || IsDoctor(user)
            ? Array.Empty<string>()
            : user.UserPermissions
                .Where(userPermission => userPermission.Permission?.IsActive == true)
                .Select(userPermission => userPermission.Permission!.Name)
                .OrderBy(name => name)
                .ToArray();
    }

    public async Task<IReadOnlyCollection<PermissionOptionDto>> GetPermissionOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var permissions = await _permissionRepository.GetActivePermissionsAsync(cancellationToken);
        return permissions
            .OrderBy(permission => permission.Name)
            .Select(permission => new PermissionOptionDto(
                permission.Name,
                permission.Description ?? permission.Name))
            .ToArray();
    }

    public async Task<ServiceResult> SetCanViewPatientPersonalDataAsync(
        int userId,
        bool canView,
        int? grantedByUserId,
        CancellationToken cancellationToken = default)
    {
        var permission = await _permissionRepository.GetActivePermissionByNameAsync(
            AppPermissions.CanViewPatientPersonalData,
            cancellationToken);
        if (permission is null)
        {
            return ServiceResult.Failure("permission_not_found", "Yetki tanımı bulunamadı.");
        }

        var user = await _permissionRepository.GetUserPermissionProfileAsync(
            userId,
            trackChanges: true,
            cancellationToken);
        if (user is null)
        {
            return ServiceResult.Failure("user_not_found", "Kullanıcı bulunamadı.");
        }

        if (IsDoctor(user) && canView)
        {
            return ServiceResult.Failure(
                "doctor_permission_forbidden",
                "Doktor rolüne hasta kişisel verisi görüntüleme yetkisi verilemez.");
        }

        var existing = user.UserPermissions.FirstOrDefault(userPermission =>
            userPermission.PermissionId == permission.Id
            || string.Equals(
                userPermission.Permission?.Name,
                AppPermissions.CanViewPatientPersonalData,
                StringComparison.OrdinalIgnoreCase));

        if (canView && existing is null)
        {
            _permissionRepository.AddUserPermission(new UserPermission
            {
                UserId = user.Id,
                PermissionId = permission.Id,
                GrantedAtUtc = DateTimeOffset.UtcNow,
                GrantedByUserId = grantedByUserId
            });
        }
        else if (!canView && existing is not null)
        {
            _permissionRepository.RemoveUserPermission(existing);
        }

        await _permissionRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    private async Task<bool> UserHasPermissionAsync(
        int userId,
        string permissionName,
        CancellationToken cancellationToken)
    {
        var user = await _permissionRepository.GetUserPermissionProfileAsync(
            userId,
            trackChanges: false,
            cancellationToken);

        if (user is null || !user.IsActive || IsDoctor(user))
        {
            return false;
        }

        return user.UserPermissions.Any(userPermission =>
            userPermission.Permission?.IsActive == true
            && string.Equals(userPermission.Permission.Name, permissionName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task AuditPatientPersonalDataViewAsync(string source, CancellationToken cancellationToken)
    {
        _auditLogRepository.AddAuditLog(new AuditLog
        {
            OccurredAtUtc = DateTimeOffset.UtcNow,
            UserId = _currentUserContext.UserId,
            Username = string.IsNullOrWhiteSpace(_currentUserContext.Username)
                ? "Sistem"
                : _currentUserContext.Username!,
            UserRole = _currentUserContext.Role,
            Action = "Görüntüleme",
            EntityName = "Hasta Kişisel Verisi",
            EntityId = null,
            Summary = $"Hasta kişisel verileri {source} ekranı için görüntülendi.",
            ChangesJson = null,
            IpAddress = _currentUserContext.IpAddress,
            RequestPath = _currentUserContext.RequestPath
        });

        await _auditLogRepository.SaveChangesAsync(cancellationToken);
    }

    private static bool IsDoctor(User user)
    {
        return string.Equals(user.Role?.Name, "Doctor", StringComparison.OrdinalIgnoreCase);
    }
}
