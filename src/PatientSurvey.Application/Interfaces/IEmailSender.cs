namespace PatientSurvey.Application.Interfaces;

public interface IEmailSender
{
    Task<DeliverySendResult> SendSurveyLinkAsync(string email, string surveyLink, CancellationToken cancellationToken);
}
