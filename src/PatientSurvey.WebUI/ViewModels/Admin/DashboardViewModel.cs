using PatientSurvey.Application.DTOs.Report;

namespace PatientSurvey.WebUI.ViewModels.Admin;

public sealed class DashboardViewModel
{
    public DashboardOverviewDto Overview { get; set; } = new(0, 0, 0, 0, 0, 0);
    public string AreaName { get; set; } = "Admin";
}
