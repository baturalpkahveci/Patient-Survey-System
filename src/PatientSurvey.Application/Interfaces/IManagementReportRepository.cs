using PatientSurvey.Domain.Entities;

namespace PatientSurvey.Application.Interfaces;

public interface IManagementReportRepository
{
    Task<IReadOnlyCollection<Survey>> GetSurveysForDashboardAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<SurveyResponse>> GetResponsesForResultsAsync(CancellationToken cancellationToken);
    Task<SurveyResponse?> GetResponseDetailAsync(int responseId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Survey>> GetSurveysForReportsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Doctor>> GetDoctorsForReportsAsync(CancellationToken cancellationToken);
}
