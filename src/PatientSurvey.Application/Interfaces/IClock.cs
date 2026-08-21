namespace PatientSurvey.Application.Interfaces;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
