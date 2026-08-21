using Microsoft.EntityFrameworkCore;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Infrastructure.Contracts;
using PatientSurvey.Infrastructure.Persistence;

namespace PatientSurvey.Infrastructure.EFCore;

public sealed class SurveyRepository :
    RepositoryBase<Survey>,
    ISurveyRepository
{
    public SurveyRepository(AppDbContext context)
        : base(context)
    {
    }

    public IQueryable<Survey> GetAllSurveys(bool trackChanges)
    {
        return FindAll(trackChanges)
            .OrderBy(survey => survey.Title);
    }

    public Task<Survey?> GetOneSurveyByIdAsync(int surveyId, bool trackChanges, CancellationToken cancellationToken)
    {
        return FindByCondition(survey => survey.Id == surveyId, trackChanges)
            .Include(survey => survey.Questions)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void CreateOneSurvey(Survey survey)
    {
        Create(survey);
    }

    public void UpdateOneSurvey(Survey survey)
    {
        Update(survey);
    }

    public void DeleteOneSurvey(Survey survey)
    {
        Delete(survey);
    }
}
