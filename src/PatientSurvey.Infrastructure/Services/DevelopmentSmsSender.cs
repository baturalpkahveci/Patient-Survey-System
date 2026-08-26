using PatientSurvey.Application.Interfaces;

namespace PatientSurvey.Infrastructure.Services;

public sealed class DevelopmentSmsSender : ISmsSender
{
    public Task<DeliverySendResult> SendSurveyLinkAsync(
        string phoneNumber,
        string surveyLink,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new DeliverySendResult(false, false, "SMS provider is not configured."));
    }
}
