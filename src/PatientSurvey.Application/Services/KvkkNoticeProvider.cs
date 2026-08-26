using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.Application.Interfaces;

namespace PatientSurvey.Application.Services;

public sealed class KvkkNoticeProvider : IKvkkNoticeProvider
{
    private const string Version = "1.0";
    private const string Text =
        "Bu aydınlatma metni, hasta memnuniyet anketi kapsamında kişisel verilerinizin kimlik doğrulama, anketin doğru davet ile eşleştirilmesi ve hizmet kalitesinin değerlendirilmesi amacıyla işlenebileceğini açıklar. Production kullanımı öncesinde kurumun hukuk/KVKK sorumlusu tarafından doğrulanmalıdır.";

    public KvkkNoticeDto GetCurrentNotice()
    {
        return new KvkkNoticeDto(Version, Text);
    }
}
