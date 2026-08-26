using PatientSurvey.Application.DTOs.Survey;

namespace PatientSurvey.Application.Interfaces;

public interface IKvkkNoticeProvider
{
    KvkkNoticeDto GetCurrentNotice();
}
