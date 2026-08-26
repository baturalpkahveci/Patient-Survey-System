using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Application.Services;
using PatientSurvey.Domain.Entities;

namespace PatientSurvey.UnitTests;

public sealed class SurveyIdentityVerificationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 24, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task VerifyPatientIdentityAsync_accepts_matching_tc_for_this_invitation()
    {
        var repository = new FakeSurveyReadRepository { Token = ValidToken() };
        var service = CreateService(repository);

        var result = await service.VerifyPatientIdentityAsync(new VerifySurveyIdentityRequestDto("token", "10000000146", true));

        Assert.True(result.IsSuccess);
        Assert.Equal(25, result.Value!.InvitationId);
    }

    [Fact]
    public async Task VerifyPatientIdentityAsync_rejects_another_valid_patient_tc()
    {
        var repository = new FakeSurveyReadRepository { Token = ValidToken() };
        var service = CreateService(repository);

        var result = await service.VerifyPatientIdentityAsync(new VerifySurveyIdentityRequestDto("token", "10000000154", true));

        Assert.False(result.IsSuccess);
        Assert.Equal("identity_mismatch", result.ErrorCode);
    }

    [Fact]
    public async Task VerifyPatientIdentityAsync_requires_kvkk_acceptance()
    {
        var repository = new FakeSurveyReadRepository { Token = ValidToken() };
        var service = CreateService(repository);

        var result = await service.VerifyPatientIdentityAsync(new VerifySurveyIdentityRequestDto("token", "10000000146", false));

        Assert.False(result.IsSuccess);
        Assert.Equal("kvkk_required", result.ErrorCode);
    }

    private static SurveyService CreateService(FakeSurveyReadRepository repository)
    {
        return new SurveyService(
            repository,
            new FakeAdminSurveyRepository(),
            new FixedClock(Now),
            kvkkNoticeProvider: new FakeKvkkNoticeProvider(),
            patientIdentityProtector: new FakeIdentityProtector());
    }

    private static SurveyAccessToken ValidToken()
    {
        var patient = new Patient { Id = 5, TcIdentityLookupHash = "hash:10000000146" };
        var visit = new PatientVisit { Id = 6, Patient = patient };
        var invitation = new SurveyInvitation { Id = 25, PatientVisit = visit };

        return new SurveyAccessToken
        {
            Id = 50,
            Token = "token",
            SurveyId = 12,
            SurveyInvitationId = invitation.Id,
            SurveyInvitation = invitation,
            ExpiresAtUtc = Now.AddHours(1),
            Survey = new Survey { Id = 12, Title = "Memnuniyet", IsActive = true }
        };
    }

    private sealed class FakeSurveyReadRepository : ISurveyReadRepository
    {
        public SurveyAccessToken? Token { get; set; }

        public Task<SurveyAccessToken?> GetTokenWithActiveSurveyAsync(string token, CancellationToken cancellationToken)
        {
            return Task.FromResult(Token?.Token == token ? Token : null);
        }

        public Task<IReadOnlyCollection<Department>> GetActiveDepartmentsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Department>>(Array.Empty<Department>());
        }
    }

    private sealed class FakeAdminSurveyRepository : IAdminSurveyRepository
    {
        public Task<IReadOnlyCollection<Survey>> GetAllSurveysWithQuestionsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<Survey>>(Array.Empty<Survey>());
        public Task<Survey?> GetSurveyWithQuestionsAsync(int surveyId, CancellationToken cancellationToken) => Task.FromResult<Survey?>(null);
        public Task<Survey?> GetSurveyByIdAsync(int surveyId, CancellationToken cancellationToken) => Task.FromResult<Survey?>(null);
        public void AddSurvey(Survey survey) { }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class FakeIdentityProtector : IPatientIdentityProtector
    {
        public string NormalizeTcIdentityNumber(string tcIdentityNumber) => new(tcIdentityNumber.Where(char.IsDigit).ToArray());
        public bool IsValidTcIdentityNumber(string normalizedTcIdentityNumber) => normalizedTcIdentityNumber is "10000000146" or "10000000154";
        public string CreateLookupHash(string normalizedTcIdentityNumber) => $"hash:{normalizedTcIdentityNumber}";
    }

    private sealed class FakeKvkkNoticeProvider : IKvkkNoticeProvider
    {
        public KvkkNoticeDto GetCurrentNotice() => new("1.0", "notice");
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }
}
