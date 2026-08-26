using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.Application.DTOs.Audit;
using PatientSurvey.Application.Services;
using PatientSurvey.WebUI.ViewModels.Admin;

namespace PatientSurvey.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class SystemHistoryController : Controller
{
    private readonly AuditLogService _auditLogService;

    public SystemHistoryController(AuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    public async Task<IActionResult> Index(
        string? search,
        string? username,
        [FromQuery(Name = "Action")] string? actionFilter,
        string? entityName,
        DateTime? fromDate,
        DateTime? toDate,
        string? sortDirection,
        CancellationToken cancellationToken)
    {
        var logs = await _auditLogService.GetAuditLogsAsync(cancellationToken);
        var filtered = ApplyFilters(logs, search, username, actionFilter, entityName, fromDate, toDate);
        filtered = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase)
            ? filtered.OrderBy(log => log.OccurredAtUtc)
            : filtered.OrderByDescending(log => log.OccurredAtUtc);
        var filteredArray = filtered.ToArray();

        return View(new SystemHistoryIndexViewModel
        {
            Logs = filteredArray,
            UserOptions = logs
                .Select(log => log.Username)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .Select(value => new FilterOptionViewModel(value, value))
                .ToArray(),
            ActionOptions = logs
                .Select(log => log.Action)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .Select(value => new FilterOptionViewModel(value, value))
                .ToArray(),
            EntityOptions = logs
                .Select(log => log.EntityName)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .Select(value => new FilterOptionViewModel(value, value))
                .ToArray(),
            Search = search,
            Username = username,
            Action = actionFilter,
            EntityName = entityName,
            FromDate = fromDate,
            ToDate = toDate,
            SortDirection = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc",
            TotalCount = logs.Count
        });
    }

    private static IEnumerable<AuditLogListItemDto> ApplyFilters(
        IReadOnlyCollection<AuditLogListItemDto> logs,
        string? search,
        string? username,
        string? action,
        string? entityName,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var filtered = logs.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filtered = filtered.Where(log =>
                log.Summary.Contains(term, StringComparison.OrdinalIgnoreCase)
                || log.EntityId?.Contains(term, StringComparison.OrdinalIgnoreCase) == true
                || log.RequestPath?.Contains(term, StringComparison.OrdinalIgnoreCase) == true);
        }

        if (!string.IsNullOrWhiteSpace(username))
        {
            filtered = filtered.Where(log => string.Equals(log.Username, username, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            filtered = filtered.Where(log => string.Equals(log.Action, action, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            filtered = filtered.Where(log => string.Equals(log.EntityName, entityName, StringComparison.OrdinalIgnoreCase));
        }

        if (fromDate.HasValue)
        {
            filtered = filtered.Where(log => log.OccurredAtUtc.LocalDateTime.Date >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            filtered = filtered.Where(log => log.OccurredAtUtc.LocalDateTime.Date <= toDate.Value.Date);
        }

        return filtered;
    }
}
