using PatientSurvey.Application.Interfaces;
using PatientSurvey.Application.Services;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Domain.Enums;

namespace PatientSurvey.UnitTests;

public sealed class PatientVisitServiceTests
{
    [Fact]
    public async Task GetPatientVisitsAsync_maps_full_and_masked_patient_names()
    {
        var service = new PatientVisitService(new FakePatientVisitReadRepository());

        var visits = await service.GetPatientVisitsAsync();

        var visit = Assert.Single(visits);
        Assert.Equal("Emre Aktaş", visit.PatientName);
        Assert.Equal("Em*** Ak***", visit.MaskedPatientName);
        Assert.Equal("Dr. Ayşe Kaya", visit.DoctorName);
        Assert.Equal("Gönderildi", visit.LatestDeliveryStatusLabel);
        Assert.Equal("Kontrol Anketi", visit.LatestSurveyTitle);
    }

    [Fact]
    public async Task GetPatientVisitsByDoctorAsync_uses_repository_scope()
    {
        var repository = new FakePatientVisitReadRepository();
        var service = new PatientVisitService(repository);

        var visits = await service.GetPatientVisitsByDoctorAsync(7);

        Assert.True(repository.DoctorScopeWasUsed);
        Assert.All(visits, visit => Assert.Equal(7, visit.DoctorId));
    }

    private sealed class FakePatientVisitReadRepository : IPatientVisitReadRepository
    {
        public bool DoctorScopeWasUsed { get; private set; }

        public Task<IReadOnlyCollection<PatientVisit>> GetPatientVisitsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<PatientVisit>>(BuildVisits());
        }

        public Task<IReadOnlyCollection<PatientVisit>> GetPatientVisitsByDoctorAsync(int doctorId, CancellationToken cancellationToken)
        {
            DoctorScopeWasUsed = true;
            return Task.FromResult<IReadOnlyCollection<PatientVisit>>(BuildVisits()
                .Where(visit => visit.DoctorId == doctorId)
                .ToArray());
        }

        private static PatientVisit[] BuildVisits()
        {
            var department = new Department { Id = 3, Name = "Kardiyoloji", IsActive = true };
            var doctor = new Doctor
            {
                Id = 7,
                FirstName = "Ayşe",
                LastName = "Kaya",
                DepartmentId = department.Id,
                Department = department
            };
            var visit = new PatientVisit
            {
                Id = 11,
                PatientId = 5,
                Patient = new Patient
                {
                    Id = 5,
                    FirstName = "Emre",
                    LastName = "Aktaş",
                    PhoneNumber = "5551002030",
                    Email = "emre@example.test"
                },
                DoctorId = doctor.Id,
                Doctor = doctor,
                DepartmentId = department.Id,
                Department = department,
                CreatedByUser = new User { Id = 2, Username = "doctor" },
                ExaminedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
                Invitations =
                {
                    new SurveyInvitation
                    {
                        Id = 9,
                        DeliveryStatus = SurveyDeliveryStatus.Sent,
                        CreatedAtUtc = DateTimeOffset.UtcNow,
                        Survey = new Survey { Id = 4, Title = "Kontrol Anketi" }
                    }
                }
            };

            return new[] { visit };
        }
    }
}
