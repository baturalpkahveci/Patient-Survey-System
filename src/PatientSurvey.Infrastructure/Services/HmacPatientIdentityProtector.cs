using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using PatientSurvey.Application.Interfaces;

namespace PatientSurvey.Infrastructure.Services;

public sealed class HmacPatientIdentityProtector : IPatientIdentityProtector
{
    private readonly byte[] _key;

    public HmacPatientIdentityProtector(IConfiguration configuration)
    {
        var configuredKey = configuration["PATIENT_IDENTITY_KEY"]
            ?? configuration["PatientIdentity:Key"];

        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            throw new InvalidOperationException("PATIENT_IDENTITY_KEY environment variable is required for patient identity lookup hashing.");
        }

        _key = Encoding.UTF8.GetBytes(configuredKey);
    }

    public string NormalizeTcIdentityNumber(string tcIdentityNumber)
    {
        return new string((tcIdentityNumber ?? string.Empty).Where(char.IsDigit).ToArray());
    }

    public bool IsValidTcIdentityNumber(string normalizedTcIdentityNumber)
    {
        if (normalizedTcIdentityNumber.Length != 11 ||
            normalizedTcIdentityNumber[0] == '0' ||
            !normalizedTcIdentityNumber.All(char.IsDigit))
        {
            return false;
        }

        var digits = normalizedTcIdentityNumber.Select(character => character - '0').ToArray();
        var oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        var evenSum = digits[1] + digits[3] + digits[5] + digits[7];
        var tenthDigit = ((oddSum * 7) - evenSum) % 10;
        var eleventhDigit = digits.Take(10).Sum() % 10;

        return digits[9] == tenthDigit && digits[10] == eleventhDigit;
    }

    public string CreateLookupHash(string normalizedTcIdentityNumber)
    {
        using var hmac = new HMACSHA256(_key);
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(normalizedTcIdentityNumber));
        return Convert.ToHexString(bytes);
    }
}
