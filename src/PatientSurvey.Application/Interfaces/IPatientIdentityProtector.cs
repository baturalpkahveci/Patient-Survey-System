namespace PatientSurvey.Application.Interfaces;

public interface IPatientIdentityProtector
{
    string NormalizeTcIdentityNumber(string tcIdentityNumber);
    bool IsValidTcIdentityNumber(string normalizedTcIdentityNumber);
    string CreateLookupHash(string normalizedTcIdentityNumber);
}
