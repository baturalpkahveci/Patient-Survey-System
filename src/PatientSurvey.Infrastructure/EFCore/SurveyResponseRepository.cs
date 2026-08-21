using Microsoft.EntityFrameworkCore;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;
using PatientSurvey.Infrastructure.Persistence;

namespace PatientSurvey.Infrastructure.EFCore;

public sealed class SurveyResponseRepository :
    RepositoryBase<SurveyResponse>,
    ISurveyResponseRepository
{
    public SurveyResponseRepository(AppDbContext context)
        : base(context)
    {
    }

    public Task<SurveyResponse?> GetOneSurveyResponseByIdAsync(
        int responseId,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        return FindByCondition(response => response.Id == responseId, trackChanges)
            .Include(response => response.Answers)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void CreateOneSurveyResponse(SurveyResponse response)
    {
        Create(response);
    }
}
