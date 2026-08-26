using PatientSurvey.Application.Interfaces;
using PatientSurvey.Application.Services;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Domain.Enums;

namespace PatientSurvey.UnitTests;

public sealed class ReportServiceTests
{
    [Fact]
    public async Task GetDashboardOverviewAsync_counts_surveys_questions_tokens_responses_and_unused_tokens()
    {
        var repository = new FakeManagementReportRepository();
        repository.Surveys.Add(new Survey
        {
            Id = 1,
            IsActive = true,
            Questions = { new Question(), new Question() },
            AccessTokens =
            {
                new SurveyAccessToken { SurveyResponse = new SurveyResponse() },
                new SurveyAccessToken()
            }
        });
        repository.Surveys.Add(new Survey
        {
            Id = 2,
            IsActive = false,
            AccessTokens = { new SurveyAccessToken { UsedAtUtc = DateTimeOffset.UtcNow } }
        });

        var overview = await new ReportService(repository).GetDashboardOverviewAsync();

        Assert.Equal(2, overview.SurveyCount);
        Assert.Equal(1, overview.ActiveSurveyCount);
        Assert.Equal(2, overview.QuestionCount);
        Assert.Equal(1, overview.ResponseCount);
        Assert.Equal(3, overview.TokenCount);
        Assert.Equal(1, overview.UnusedTokenCount);
    }

    [Fact]
    public async Task GetResultsAsync_orders_newest_first_and_calculates_score_average()
    {
        var repository = new FakeManagementReportRepository();
        repository.Responses.Add(Response(1, "A", "Acil", new DateTimeOffset(2026, 8, 19, 8, 0, 0, TimeSpan.Zero), 2, 4));
        repository.Responses.Add(Response(2, "B", "Kardiyoloji", new DateTimeOffset(2026, 8, 20, 8, 0, 0, TimeSpan.Zero), 5));

        var results = await new ReportService(repository).GetResultsAsync();

        Assert.Equal(new[] { 2, 1 }, results.Select(result => result.Id));
        Assert.Equal(5, results.First().AverageScore);
        Assert.Equal(3, results.Last().AverageScore);
    }

    [Fact]
    public async Task GetResultsAsync_without_permission_returns_patient_reference_only()
    {
        var repository = new FakeManagementReportRepository();
        var response = Response(1, "A", "Acil", DateTimeOffset.UtcNow, 4);
        response.Token!.SurveyInvitation = new SurveyInvitation
        {
            PatientVisit = new PatientVisit
            {
                PatientId = 8,
                Patient = new Patient
                {
                    Id = 8,
                    FirstName = "Emre",
                    LastName = "Aktaş",
                    PhoneNumber = "5551002030",
                    Email = "emre@example.test"
                }
            }
        };
        repository.Responses.Add(response);

        var results = await new ReportService(repository).GetResultsAsync();

        var result = Assert.Single(results);
        Assert.Equal("Hasta #8", result.PatientName);
        Assert.Null(result.PatientPhone);
        Assert.Null(result.PatientEmail);
        Assert.False(repository.IncludePatientPersonalData);
    }


    [Fact]
    public async Task GetResultDetailAsync_returns_not_found_or_maps_answers_in_display_order()
    {
        var repository = new FakeManagementReportRepository();
        var service = new ReportService(repository);

        Assert.Equal("response_not_found", (await service.GetResultDetailAsync(404)).ErrorCode);

        var response = Response(7, "Memnuniyet", "Acil", DateTimeOffset.UtcNow);
        response.Answers.Add(new Answer
        {
            Question = new Question { Text = "Yorum", Type = QuestionType.Text, DisplayOrder = 2 },
            TextValue = "iyi"
        });
        response.Answers.Add(new Answer
        {
            Question = new Question { Text = "Puan", Type = QuestionType.Score, DisplayOrder = 1 },
            ScoreValue = 5
        });
        repository.ResponseDetail = response;

        var detail = await service.GetResultDetailAsync(7);

        Assert.True(detail.IsSuccess);
        Assert.Equal("Memnuniyet", detail.Value!.SurveyTitle);
        Assert.Equal(new[] { "Puan", "Yorum" }, detail.Value.Answers.Select(answer => answer.QuestionText));
    }

    [Fact]
    public async Task GetSurveyReportsAsync_aggregates_totals_and_department_averages()
    {
        var repository = new FakeManagementReportRepository();
        var survey = new Survey
        {
            Id = 1,
            Title = "Memnuniyet",
            IsActive = true,
            Questions = { new Question(), new Question() }
        };
        survey.AccessTokens.Add(new SurveyAccessToken { SurveyResponse = Response(1, "Memnuniyet", "Acil", DateTimeOffset.UtcNow, 5, 3) });
        survey.AccessTokens.Add(new SurveyAccessToken { SurveyResponse = Response(2, "Memnuniyet", "Acil", DateTimeOffset.UtcNow, 1) });
        survey.AccessTokens.Add(new SurveyAccessToken { SurveyResponse = Response(3, "Memnuniyet", "Kardiyoloji", DateTimeOffset.UtcNow, 4) });
        survey.AccessTokens.Add(new SurveyAccessToken());
        repository.Surveys.Add(survey);

        var reports = await new ReportService(repository).GetSurveyReportsAsync();
        var report = Assert.Single(reports);

        Assert.Equal(2, report.QuestionCount);
        Assert.Equal(4, report.TokenCount);
        Assert.Equal(3, report.ResponseCount);
        Assert.Equal(3.25, report.AverageScore);
        Assert.Collection(report.Departments,
            department =>
            {
                Assert.Equal("Acil", department.DepartmentName);
                Assert.Equal(2, department.ResponseCount);
                Assert.Equal(3, department.AverageScore);
            },
            department =>
            {
                Assert.Equal("Kardiyoloji", department.DepartmentName);
                Assert.Equal(1, department.ResponseCount);
                Assert.Equal(4, department.AverageScore);
            });
    }

    [Fact]
    public async Task GetManagerReportDashboardAsync_includes_doctors_without_responses()
    {
        var repository = new FakeManagementReportRepository();
        repository.Doctors.Add(new Doctor
        {
            Id = 8,
            FirstName = "Ayşe",
            LastName = "Yılmaz",
            Department = new Department { Name = "Dahiliye" }
        });

        var dashboard = await new ReportService(repository).GetManagerReportDashboardAsync();

        var doctor = Assert.Single(dashboard.Doctors);
        Assert.Equal("Dr. Ayşe Yılmaz", doctor.DoctorName);
        Assert.Equal("Dahiliye", doctor.DepartmentName);
        Assert.Equal(0, doctor.ResponseCount);
        Assert.Null(doctor.AverageScore);
    }

    private static SurveyResponse Response(
        int id,
        string surveyTitle,
        string departmentName,
        DateTimeOffset submittedAtUtc,
        params int[] scoreValues)
    {
        return new SurveyResponse
        {
            Id = id,
            SubmittedAtUtc = submittedAtUtc,
            Department = new Department { Name = departmentName },
            Token = new SurveyAccessToken
            {
                SurveyId = 10,
                Survey = new Survey { Id = 10, Title = surveyTitle }
            },
            Answers = scoreValues
                .Select((score, index) => new Answer
                {
                    ScoreValue = score,
                    Question = new Question
                    {
                        Text = $"Q{index}",
                        Type = QuestionType.Score,
                        DisplayOrder = index
                    }
                })
                .ToList()
        };
    }

    private sealed class FakeManagementReportRepository : IManagementReportRepository
    {
        public List<Survey> Surveys { get; } = new();
        public List<SurveyResponse> Responses { get; } = new();
        public List<Doctor> Doctors { get; } = new();
        public SurveyResponse? ResponseDetail { get; set; }
        public bool? IncludePatientPersonalData { get; private set; }

        public Task<IReadOnlyCollection<Survey>> GetSurveysForDashboardAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Survey>>(Surveys);
        }

        public Task<IReadOnlyCollection<SurveyResponse>> GetResponsesForResultsAsync(
            bool includePatientPersonalData,
            CancellationToken cancellationToken)
        {
            IncludePatientPersonalData = includePatientPersonalData;
            return Task.FromResult<IReadOnlyCollection<SurveyResponse>>(Responses);
        }

        public Task<SurveyResponse?> GetResponseDetailAsync(
            int responseId,
            bool includePatientPersonalData,
            CancellationToken cancellationToken)
        {
            IncludePatientPersonalData = includePatientPersonalData;
            return Task.FromResult(ResponseDetail?.Id == responseId ? ResponseDetail : null);
        }

        public Task<IReadOnlyCollection<Survey>> GetSurveysForReportsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Survey>>(Surveys);
        }

        public Task<IReadOnlyCollection<Doctor>> GetDoctorsForReportsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Doctor>>(Doctors);
        }
    }
}
