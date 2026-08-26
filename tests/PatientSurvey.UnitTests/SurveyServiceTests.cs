using PatientSurvey.Application.DTOs.Survey;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Application.Services;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Domain.Enums;

namespace PatientSurvey.UnitTests;

public sealed class SurveyServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetSurveyFormAsync_rejects_blank_missing_used_expired_and_inactive_tokens()
    {
        var readRepository = new FakeSurveyReadRepository();
        var service = CreateService(readRepository);

        Assert.Equal("invalid_token", (await service.GetSurveyFormAsync(" ")).ErrorCode);
        Assert.Equal("invalid_token", (await service.GetSurveyFormAsync("missing")).ErrorCode);

        readRepository.Token = ValidToken();
        readRepository.Token.UsedAtUtc = Now;
        Assert.Equal("invalid_token", (await service.GetSurveyFormAsync("token")).ErrorCode);

        readRepository.Token = ValidToken();
        readRepository.Token.ExpiresAtUtc = Now.AddTicks(-1);
        Assert.Equal("expired_token", (await service.GetSurveyFormAsync("token")).ErrorCode);

        readRepository.Token = ValidToken();
        readRepository.Token.Survey!.IsActive = false;
        Assert.Equal("invalid_token", (await service.GetSurveyFormAsync("token")).ErrorCode);
    }

    [Fact]
    public async Task GetSurveyFormAsync_maps_only_active_questions_ordered_with_departments()
    {
        var readRepository = new FakeSurveyReadRepository
        {
            Token = ValidToken(),
            Departments =
            {
                new Department { Id = 2, Name = "Acil", IsActive = true },
                new Department { Id = 1, Name = "Kardiyoloji", IsActive = true }
            }
        };
        readRepository.Token.Survey!.Questions.Add(new Question
        {
            Id = 2,
            Text = "Pasif",
            Type = QuestionType.Text,
            DisplayOrder = 1,
            IsActive = false
        });
        readRepository.Token.Survey.Questions.Add(new Question
        {
            Id = 3,
            Text = "Ikinci",
            Type = QuestionType.Boolean,
            DisplayOrder = 2,
            IsActive = true
        });
        readRepository.Token.Survey.Questions.Add(new Question
        {
            Id = 1,
            Text = "Birinci",
            Type = QuestionType.Score,
            DisplayOrder = 1,
            IsActive = true
        });
        var service = CreateService(readRepository);

        var result = await service.GetSurveyFormAsync(" token ");

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { 1, 3 }, result.Value!.Questions.Select(question => question.Id));
        Assert.Equal(2, result.Value.Departments.Count);
        Assert.Equal("token", readRepository.LastToken);
    }

    [Fact]
    public async Task GetAdminSurveysAsync_counts_questions_tokens_and_responses()
    {
        var adminRepository = new FakeAdminSurveyRepository();
        adminRepository.Surveys.Add(new Survey
        {
            Id = 2,
            Title = "B",
            Questions = { new Question(), new Question() },
            AccessTokens =
            {
                new SurveyAccessToken { SurveyResponse = new SurveyResponse() },
                new SurveyAccessToken()
            }
        });
        var service = CreateService(adminRepository: adminRepository);

        var surveys = await service.GetAdminSurveysAsync();
        var survey = Assert.Single(surveys);

        Assert.Equal(2, survey.QuestionCount);
        Assert.Equal(2, survey.TokenCount);
        Assert.Equal(1, survey.ResponseCount);
    }

    [Fact]
    public async Task CreateSurveyAsync_validates_title_trims_values_and_uses_clock()
    {
        var adminRepository = new FakeAdminSurveyRepository();
        var service = CreateService(adminRepository: adminRepository);

        Assert.Equal("title_required", (await service.CreateSurveyAsync(new CreateSurveyRequestDto(" ", null, true))).ErrorCode);

        var result = await service.CreateSurveyAsync(new CreateSurveyRequestDto(" Memnuniyet ", " Aciklama ", false));

        Assert.True(result.IsSuccess);
        var survey = Assert.Single(adminRepository.AddedSurveys);
        Assert.Equal("Memnuniyet", survey.Title);
        Assert.Equal("Aciklama", survey.Description);
        Assert.False(survey.IsActive);
        Assert.Equal(Now, survey.CreatedAtUtc);
    }

    [Fact]
    public async Task CreateSurveyAsync_rejects_general_survey_for_doctor_role()
    {
        var service = new SurveyService(
            new FakeSurveyReadRepository(),
            new FakeAdminSurveyRepository(),
            new FixedClock(Now),
            new FakeInvitationRepository());

        var result = await service.CreateSurveyAsync(new CreateSurveyRequestDto(
            "Doktor Anketi",
            null,
            true,
            IsGeneral: true,
            CreatedByUserId: 10,
            CreatedByRole: "Doctor"));

        Assert.False(result.IsSuccess);
        Assert.Equal("doctor_general_not_allowed", result.ErrorCode);
    }

    [Fact]
    public async Task CreateSurveyAsync_for_doctor_uses_current_doctor_department_scope()
    {
        var adminRepository = new FakeAdminSurveyRepository();
        var invitationRepository = new FakeInvitationRepository
        {
            DoctorByUserId = new Doctor
            {
                Id = 3,
                UserId = 10,
                DepartmentId = 5,
                IsActive = true,
                Department = new Department { Id = 5, Name = "Kardiyoloji", IsActive = true }
            }
        };
        var service = new SurveyService(
            new FakeSurveyReadRepository(),
            adminRepository,
            new FixedClock(Now),
            invitationRepository);

        var result = await service.CreateSurveyAsync(new CreateSurveyRequestDto(
            "Doktor Anketi",
            null,
            true,
            IsGeneral: false,
            CreatedByUserId: 10,
            CreatedByRole: "Doctor"));

        Assert.True(result.IsSuccess);
        var survey = Assert.Single(adminRepository.AddedSurveys);
        Assert.Equal(3, survey.DoctorId);
        Assert.Equal(5, survey.DepartmentId);
    }

    [Fact]
    public async Task CreateSurveyAsync_for_doctor_rejects_missing_user_inactive_doctor_and_inactive_department()
    {
        var invitationRepository = new FakeInvitationRepository();
        var service = new SurveyService(
            new FakeSurveyReadRepository(),
            new FakeAdminSurveyRepository(),
            new FixedClock(Now),
            invitationRepository);

        Assert.Equal("doctor_user_required", (await service.CreateSurveyAsync(new CreateSurveyRequestDto(
            "Doktor Anketi",
            null,
            true,
            IsGeneral: false,
            CreatedByRole: "Doctor"))).ErrorCode);

        invitationRepository.DoctorByUserId = new Doctor
        {
            Id = 3,
            UserId = 10,
            DepartmentId = 5,
            IsActive = false,
            Department = new Department { Id = 5, Name = "Kardiyoloji", IsActive = true }
        };
        Assert.Equal("doctor_inactive", (await service.CreateSurveyAsync(new CreateSurveyRequestDto(
            "Doktor Anketi",
            null,
            true,
            IsGeneral: false,
            CreatedByUserId: 10,
            CreatedByRole: "Doctor"))).ErrorCode);

        invitationRepository.DoctorByUserId.IsActive = true;
        invitationRepository.DoctorByUserId.Department.IsActive = false;
        Assert.Equal("doctor_inactive", (await service.CreateSurveyAsync(new CreateSurveyRequestDto(
            "Doktor Anketi",
            null,
            true,
            IsGeneral: false,
            CreatedByUserId: 10,
            CreatedByRole: "Doctor"))).ErrorCode);
    }

    [Fact]
    public async Task CreateSurveyAsync_for_admin_targeted_survey_validates_department_doctor_pair()
    {
        var invitationRepository = new FakeInvitationRepository
        {
            DoctorById = new Doctor
            {
                Id = 4,
                DepartmentId = 8,
                IsActive = true,
                Department = new Department { Id = 8, Name = "Dahiliye", IsActive = true }
            }
        };
        var service = new SurveyService(
            new FakeSurveyReadRepository(),
            new FakeAdminSurveyRepository(),
            new FixedClock(Now),
            invitationRepository);

        Assert.Equal("department_required", (await service.CreateSurveyAsync(new CreateSurveyRequestDto(
            "Hedefli",
            null,
            true,
            IsGeneral: false,
            DoctorId: 4))).ErrorCode);

        Assert.Equal("doctor_required", (await service.CreateSurveyAsync(new CreateSurveyRequestDto(
            "Hedefli",
            null,
            true,
            IsGeneral: false,
            DepartmentId: 8))).ErrorCode);

        Assert.Equal("doctor_department_mismatch", (await service.CreateSurveyAsync(new CreateSurveyRequestDto(
            "Hedefli",
            null,
            true,
            IsGeneral: false,
            DepartmentId: 7,
            DoctorId: 4))).ErrorCode);
    }

    [Fact]
    public async Task ToggleSurveyStatusAsync_flips_status_or_returns_not_found()
    {
        var adminRepository = new FakeAdminSurveyRepository();
        var service = CreateService(adminRepository: adminRepository);

        Assert.Equal("survey_not_found", (await service.ToggleSurveyStatusAsync(99)).ErrorCode);

        adminRepository.Surveys.Add(new Survey { Id = 3, Title = "A", IsActive = true });
        var result = await service.ToggleSurveyStatusAsync(3);

        Assert.True(result.IsSuccess);
        Assert.False(adminRepository.Surveys[0].IsActive);
    }

    private static SurveyService CreateService(
        FakeSurveyReadRepository? readRepository = null,
        FakeAdminSurveyRepository? adminRepository = null)
    {
        return new SurveyService(
            readRepository ?? new FakeSurveyReadRepository(),
            adminRepository ?? new FakeAdminSurveyRepository(),
            new FixedClock(Now));
    }

    private static SurveyAccessToken ValidToken()
    {
        return new SurveyAccessToken
        {
            Token = "token",
            ExpiresAtUtc = Now.AddHours(1),
            Survey = new Survey { Id = 10, Title = "Memnuniyet", IsActive = true }
        };
    }

    private sealed class FakeSurveyReadRepository : ISurveyReadRepository
    {
        public SurveyAccessToken? Token { get; set; }
        public List<Department> Departments { get; } = new();
        public string? LastToken { get; private set; }

        public Task<SurveyAccessToken?> GetTokenWithActiveSurveyAsync(string token, CancellationToken cancellationToken)
        {
            LastToken = token;
            return Task.FromResult(Token is { Survey.IsActive: true } && Token.Token == token ? Token : null);
        }

        public Task<IReadOnlyCollection<Department>> GetActiveDepartmentsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Department>>(Departments.Where(department => department.IsActive).ToArray());
        }
    }

    private sealed class FakeAdminSurveyRepository : IAdminSurveyRepository
    {
        public List<Survey> Surveys { get; } = new();
        public List<Survey> AddedSurveys { get; } = new();

        public Task<IReadOnlyCollection<Survey>> GetAllSurveysWithQuestionsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Survey>>(Surveys);
        }

        public Task<Survey?> GetSurveyWithQuestionsAsync(int surveyId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Surveys.FirstOrDefault(survey => survey.Id == surveyId));
        }

        public Task<Survey?> GetSurveyByIdAsync(int surveyId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Surveys.FirstOrDefault(survey => survey.Id == surveyId));
        }

        public void AddSurvey(Survey survey)
        {
            survey.Id = AddedSurveys.Count + 1;
            AddedSurveys.Add(survey);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class FakeInvitationRepository : ISurveyInvitationRepository
    {
        public Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<Survey?> GetSurveyByIdAsync(int surveyId, bool trackChanges, CancellationToken cancellationToken) => Task.FromResult<Survey?>(null);
        public Doctor? DoctorById { get; init; }
        public Doctor? DoctorByUserId { get; set; }

        public Task<Doctor?> GetDoctorByIdAsync(int doctorId, CancellationToken cancellationToken)
        {
            return Task.FromResult(DoctorById?.Id == doctorId ? DoctorById : null);
        }

        public Task<Doctor?> GetDoctorByUserIdAsync(int userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(DoctorByUserId?.UserId == userId ? DoctorByUserId : null);
        }

        public Task<IReadOnlyCollection<Doctor>> GetActiveDoctorsByDepartmentAsync(int departmentId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<Doctor>>(Array.Empty<Doctor>());
        public Task<IReadOnlyCollection<Department>> GetActiveDepartmentsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<Department>>(Array.Empty<Department>());
        public Task<Patient?> GetPatientByTcHashAsync(string tcIdentityLookupHash, CancellationToken cancellationToken) => Task.FromResult<Patient?>(null);
        public Task<bool> TokenExistsAsync(string token, CancellationToken cancellationToken) => Task.FromResult(false);
        public void AddPatient(Patient patient) { }
        public void AddPatientVisit(PatientVisit visit) { }
        public void AddSurveyInvitation(SurveyInvitation invitation) { }
        public void AddToken(SurveyAccessToken token) { }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;
        public DateTimeOffset UtcNow { get; }
    }
}
