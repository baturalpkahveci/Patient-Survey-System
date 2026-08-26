using PatientSurvey.Application.Interfaces;

namespace PatientSurvey.Infrastructure.Services;

public sealed class DevelopmentEmailSender : IEmailSender
{
    public Task<DeliverySendResult> SendSurveyLinkAsync(
        string email,
        string surveyLink,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new DeliverySendResult(false, false, "Email provider is not configured."));
    }
}
