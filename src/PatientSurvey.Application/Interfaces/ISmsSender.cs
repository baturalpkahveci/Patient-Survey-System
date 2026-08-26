namespace PatientSurvey.Application.Interfaces;

public interface ISmsSender
{
    Task<DeliverySendResult> SendSurveyLinkAsync(string phoneNumber, string surveyLink, CancellationToken cancellationToken);
}
