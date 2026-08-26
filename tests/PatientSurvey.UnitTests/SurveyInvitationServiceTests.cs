using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Application.Services;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Domain.Enums;

namespace PatientSurvey.UnitTests;

public sealed class SurveyInvitationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 24, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateInvitationAsync_creates_patient_visit_invitation_and_token()
    {
        var repository = FakeInvitationRepository.WithSurvey(new Survey { Id = 10, Title = "Memnuniyet", IsActive = true });
        var service = CreateService(repository);

        var result = await service.CreateInvitationAsync(ValidRequest());

        Assert.True(result.IsSuccess);
        Assert.True(repository.Transaction!.Committed);
        var patient = Assert.Single(repository.Patients);
        var visit = Assert.Single(repository.Visits);
        var invitation = Assert.Single(repository.Invitations);
        var token = Assert.Single(repository.Tokens);
        Assert.Equal(patient, visit.Patient);
        Assert.Equal(visit, invitation.PatientVisit);
        Assert.Equal(invitation, token.SurveyInvitation);
        Assert.Equal(SurveyDeliveryStatus.LinkCreated, invitation.DeliveryStatus);
        Assert.Equal(token.Token, result.Value!.Token);
    }

    [Fact]
    public async Task CreateInvitationAsync_reuses_patient_with_same_tc_hash()
    {
        var existing = new Patient
        {
            Id = 7,
            FirstName = "Eski",
            LastName = "Hasta",
            TcIdentityLookupHash = "hash:10000000146",
            PhoneNumber = "old",
            Email = "old@example.test"
        };
        var repository = FakeInvitationRepository.WithSurvey(new Survey { Id = 10, Title = "Memnuniyet", IsActive = true });
        repository.Patients.Add(existing);
        var service = CreateService(repository);

        var result = await service.CreateInvitationAsync(ValidRequest(patientFirstName: "Yeni"));

        Assert.True(result.IsSuccess);
        Assert.Single(repository.Patients);
        Assert.Equal("Yeni", existing.FirstName);
        Assert.Equal(Now, existing.UpdatedAtUtc);
    }

    [Fact]
    public async Task CreateInvitationAsync_sms_not_configured_keeps_created_link()
    {
        var repository = FakeInvitationRepository.WithSurvey(new Survey { Id = 10, Title = "Memnuniyet", IsActive = true });
        var service = CreateService(repository);

        var result = await service.CreateInvitationAsync(ValidRequest(method: SurveyDeliveryMethod.Sms));

        Assert.True(result.IsSuccess);
        Assert.Equal(SurveyDeliveryStatus.NotConfigured, result.Value!.DeliveryStatus);
        Assert.Single(repository.Tokens);
        Assert.Contains("SMS gönderimi yapılandırılmamış", result.Value.Message);
    }

    [Fact]
    public async Task CreateInvitationAsync_targeted_survey_sets_matching_visit_scope()
    {
        var survey = new Survey
        {
            Id = 10,
            Title = "Hedefli",
            IsActive = true,
            DoctorId = 3,
            DepartmentId = 4
        };
        var repository = FakeInvitationRepository.WithSurvey(survey);
        var service = CreateService(repository);

        var result = await service.CreateInvitationAsync(ValidRequest());

        Assert.True(result.IsSuccess);
        var visit = Assert.Single(repository.Visits);
        Assert.Equal(3, visit.DoctorId);
        Assert.Equal(4, visit.DepartmentId);
    }

    [Fact]
    public async Task CreateInvitationAsync_rejects_invalid_tc_without_persisting_records()
    {
        var repository = FakeInvitationRepository.WithSurvey(new Survey { Id = 10, Title = "Memnuniyet", IsActive = true });
        var service = CreateService(repository);

        var result = await service.CreateInvitationAsync(ValidRequest(tc: "123"));

        Assert.False(result.IsSuccess);
        Assert.Equal("tc_invalid", result.ErrorCode);
        Assert.Empty(repository.Tokens);
        Assert.Empty(repository.Invitations);
    }

    private static SurveyInvitationService CreateService(FakeInvitationRepository repository)
    {
        return new SurveyInvitationService(
            repository,
            new FakeIdentityProtector(),
            new NotConfiguredSmsSender(),
            new NotConfiguredEmailSender(),
            new FixedClock(Now));
    }

    private static CreateSurveyInvitationRequestDto ValidRequest(
        string patientFirstName = "Ayşe",
        string tc = "10000000146",
        SurveyDeliveryMethod method = SurveyDeliveryMethod.LinkOnly)
    {
        return new CreateSurveyInvitationRequestDto(
            10,
            patientFirstName,
            "Yılmaz",
            tc,
            "05551234567",
            "hasta@example.test",
            method,
            Now.AddDays(1),
            1,
            "https://example.test/Survey/");
    }

    private sealed class FakeInvitationRepository : ISurveyInvitationRepository
    {
        public Survey? Survey { get; private init; }
        public FakeTransaction? Transaction { get; private set; }
        public List<Patient> Patients { get; } = new();
        public List<PatientVisit> Visits { get; } = new();
        public List<SurveyInvitation> Invitations { get; } = new();
        public List<SurveyAccessToken> Tokens { get; } = new();

        public static FakeInvitationRepository WithSurvey(Survey survey)
        {
            return new FakeInvitationRepository { Survey = survey };
        }

        public Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        {
            Transaction = new FakeTransaction();
            return Task.FromResult<IAppTransaction>(Transaction);
        }

        public Task<Survey?> GetSurveyByIdAsync(int surveyId, bool trackChanges, CancellationToken cancellationToken)
        {
            return Task.FromResult(Survey?.Id == surveyId ? Survey : null);
        }

        public Task<Doctor?> GetDoctorByIdAsync(int doctorId, CancellationToken cancellationToken) => Task.FromResult<Doctor?>(null);
        public Task<Doctor?> GetDoctorByUserIdAsync(int userId, CancellationToken cancellationToken) => Task.FromResult<Doctor?>(null);
        public Task<IReadOnlyCollection<Doctor>> GetActiveDoctorsByDepartmentAsync(int departmentId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<Doctor>>(Array.Empty<Doctor>());
        public Task<IReadOnlyCollection<Department>> GetActiveDepartmentsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<Department>>(Array.Empty<Department>());

        public Task<Patient?> GetPatientByTcHashAsync(string tcIdentityLookupHash, CancellationToken cancellationToken)
        {
            return Task.FromResult(Patients.FirstOrDefault(patient => patient.TcIdentityLookupHash == tcIdentityLookupHash));
        }

        public Task<bool> TokenExistsAsync(string token, CancellationToken cancellationToken) => Task.FromResult(false);

        public void AddPatient(Patient patient)
        {
            patient.Id = Patients.Count + 1;
            Patients.Add(patient);
        }

        public void AddPatientVisit(PatientVisit visit)
        {
            visit.Id = Visits.Count + 1;
            Visits.Add(visit);
        }

        public void AddSurveyInvitation(SurveyInvitation invitation)
        {
            invitation.Id = Invitations.Count + 1;
            Invitations.Add(invitation);
        }

        public void AddToken(SurveyAccessToken token)
        {
            token.Id = Tokens.Count + 1;
            Tokens.Add(token);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class FakeIdentityProtector : IPatientIdentityProtector
    {
        public string NormalizeTcIdentityNumber(string tcIdentityNumber) => new(tcIdentityNumber.Where(char.IsDigit).ToArray());
        public bool IsValidTcIdentityNumber(string normalizedTcIdentityNumber) => normalizedTcIdentityNumber is "10000000146" or "10000000154";
        public string CreateLookupHash(string normalizedTcIdentityNumber) => $"hash:{normalizedTcIdentityNumber}";
    }

    private sealed class NotConfiguredSmsSender : ISmsSender
    {
        public Task<DeliverySendResult> SendSurveyLinkAsync(string phoneNumber, string surveyLink, CancellationToken cancellationToken)
        {
            return Task.FromResult(new DeliverySendResult(false, false));
        }
    }

    private sealed class NotConfiguredEmailSender : IEmailSender
    {
        public Task<DeliverySendResult> SendSurveyLinkAsync(string email, string surveyLink, CancellationToken cancellationToken)
        {
            return Task.FromResult(new DeliverySendResult(false, false));
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }

    private sealed class FakeTransaction : IAppTransaction
    {
        public bool Committed { get; private set; }
        public bool RolledBack { get; private set; }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            Committed = true;
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken)
        {
            RolledBack = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
