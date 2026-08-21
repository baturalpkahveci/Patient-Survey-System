using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using PatientSurvey.Domain.Entities;
using PatientSurvey.Domain.Enums;
using PatientSurvey.Infrastructure.Persistence;

namespace PatientSurvey.IntegrationTests;

public sealed class EfModelConstraintTests
{
    [Fact]
    public void Model_defines_token_response_and_answer_uniqueness_constraints()
    {
        using var context = CreateContext();
        var model = context.Model;

        var tokenEntity = model.FindEntityType(typeof(SurveyAccessToken));
        var responseEntity = model.FindEntityType(typeof(SurveyResponse));
        var answerEntity = model.FindEntityType(typeof(Answer));

        Assert.NotNull(tokenEntity);
        Assert.NotNull(responseEntity);
        Assert.NotNull(answerEntity);

        Assert.Contains(tokenEntity!.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual(new[] { nameof(SurveyAccessToken.Token) }));

        Assert.Contains(responseEntity!.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual(new[] { nameof(SurveyResponse.TokenId) }));

        Assert.Contains(answerEntity!.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual(new[]
            {
                nameof(Answer.SurveyResponseId),
                nameof(Answer.QuestionId)
            }));
    }

    [Fact]
    public void Model_defines_expected_table_names_required_lengths_and_conversions()
    {
        using var context = CreateContext();
        var model = context.Model;

        var survey = model.FindEntityType(typeof(Survey))!;
        var question = model.FindEntityType(typeof(Question))!;
        var answer = model.FindEntityType(typeof(Answer))!;
        var user = model.FindEntityType(typeof(User))!;

        Assert.Equal("surveys", survey.GetTableName());
        Assert.Equal("questions", question.GetTableName());
        Assert.Equal("answers", answer.GetTableName());
        Assert.Equal("users", user.GetTableName());

        Assert.Equal(200, survey.FindProperty(nameof(Survey.Title))!.GetMaxLength());
        Assert.False(survey.FindProperty(nameof(Survey.Title))!.IsNullable);
        Assert.Equal(1000, question.FindProperty(nameof(Question.Text))!.GetMaxLength());
        Assert.False(question.FindProperty(nameof(Question.Text))!.IsNullable);
        Assert.Equal(4000, answer.FindProperty(nameof(Answer.TextValue))!.GetMaxLength());
        Assert.Equal(512, user.FindProperty(nameof(User.PasswordHash))!.GetMaxLength());
        Assert.Equal(typeof(int), question.FindProperty(nameof(Question.Type))!.GetProviderClrType());
        Assert.Equal(1, (int)QuestionType.Score);
    }

    [Fact]
    public void Model_defines_restrictive_relationships_for_core_entities()
    {
        using var context = CreateContext();
        var model = context.Model;

        AssertForeignKeyDeleteBehavior<SurveyAccessToken, Survey>(model, DeleteBehavior.Restrict);
        AssertForeignKeyDeleteBehavior<Question, Survey>(model, DeleteBehavior.Restrict);
        AssertForeignKeyDeleteBehavior<SurveyResponse, SurveyAccessToken>(model, DeleteBehavior.Restrict);
        AssertForeignKeyDeleteBehavior<SurveyResponse, Department>(model, DeleteBehavior.Restrict);
        AssertForeignKeyDeleteBehavior<User, Role>(model, DeleteBehavior.Restrict);
        AssertForeignKeyDeleteBehavior<Answer, Question>(model, DeleteBehavior.Restrict);
        AssertForeignKeyDeleteBehavior<Answer, SurveyResponse>(model, DeleteBehavior.Cascade);
    }

    [Fact]
    public void Model_seeds_admin_and_manager_roles()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var roleEntity = model.FindEntityType(typeof(Role))!;
        var seedData = roleEntity.GetSeedData();

        Assert.Contains(seedData, row =>
            row.TryGetValue(nameof(Role.Id), out var id)
            && row.TryGetValue(nameof(Role.Name), out var name)
            && row.TryGetValue(nameof(Role.IsActive), out var isActive)
            && id is 1
            && name is "Admin"
            && isActive is true);

        Assert.Contains(seedData, row =>
            row.TryGetValue(nameof(Role.Id), out var id)
            && row.TryGetValue(nameof(Role.Name), out var name)
            && row.TryGetValue(nameof(Role.IsActive), out var isActive)
            && id is 2
            && name is "Manager"
            && isActive is true);
    }

    [Fact]
    public void Model_defines_answer_score_range_check_constraint()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var answerEntity = model.FindEntityType(typeof(Answer))!;

        Assert.Contains(answerEntity.GetCheckConstraints(), constraint =>
            constraint.Name == "ck_answers_score_value_range"
            && constraint.Sql.Contains("score_value >= 1", StringComparison.Ordinal)
            && constraint.Sql.Contains("score_value <= 5", StringComparison.Ordinal));
    }

    private static void AssertForeignKeyDeleteBehavior<TDependent, TPrincipal>(
        IModel model,
        DeleteBehavior expectedDeleteBehavior)
    {
        var dependent = model.FindEntityType(typeof(TDependent))!;
        Assert.Contains(dependent.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(TPrincipal)
            && foreignKey.DeleteBehavior == expectedDeleteBehavior);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=patient_survey_test;Username=test;Password=test")
            .Options;

        return new AppDbContext(options);
    }
}
