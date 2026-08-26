namespace PatientSurvey.Application.Interfaces;

public sealed record DeliverySendResult(
    bool IsSent,
    bool IsConfigured,
    string? SafeMessage = null);
