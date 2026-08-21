using PatientSurvey.Application.Interfaces;

namespace PatientSurvey.Infrastructure.Services;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
