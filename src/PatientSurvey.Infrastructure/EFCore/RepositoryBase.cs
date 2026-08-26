using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PatientSurvey.Application.Exceptions;
using PatientSurvey.Application.Interfaces;
using PatientSurvey.Infrastructure.Contracts;
using PatientSurvey.Infrastructure.Persistence;

namespace PatientSurvey.Infrastructure.EFCore;

public abstract class RepositoryBase<T> : IRepositoryBase<T>
    where T : class
{
    private const string UniqueViolation = "23505"; // PostgreSQL unique violation error code

    protected RepositoryBase(AppDbContext context)
    {
        Context = context;
    }

    protected AppDbContext Context { get; }

    public IQueryable<T> FindAll(bool trackChanges)
    {
        return !trackChanges
            ? Context.Set<T>().AsNoTracking()
            : Context.Set<T>();
    }

    public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges)
    {
        return !trackChanges
            ? Context.Set<T>().Where(expression).AsNoTracking()
            : Context.Set<T>().Where(expression);
    }

    public void Create(T entity)
    {
        Context.Set<T>().Add(entity);
    }

    public void Update(T entity)
    {
        Context.Set<T>().Update(entity);
    }

    public void Delete(T entity)
    {
        Context.Set<T>().Remove(entity);
    }

    protected IQueryable<TEntity> FindAllEntity<TEntity>(bool trackChanges)
        where TEntity : class
    {
        return !trackChanges
            ? Context.Set<TEntity>().AsNoTracking()
            : Context.Set<TEntity>();
    }

    protected IQueryable<TEntity> FindEntityByCondition<TEntity>(
        Expression<Func<TEntity, bool>> expression,
        bool trackChanges)
        where TEntity : class
    {
        return !trackChanges
            ? Context.Set<TEntity>().Where(expression).AsNoTracking()
            : Context.Set<TEntity>().Where(expression);
    }

    protected void CreateEntity<TEntity>(TEntity entity)
        where TEntity : class
    {
        Context.Set<TEntity>().Add(entity);
    }

    protected async Task<IAppTransaction> BeginTransactionCoreAsync(CancellationToken cancellationToken)
    {
        var transaction = await Context.Database.BeginTransactionAsync(cancellationToken);
        return new EfAppTransaction(transaction);
    }

    protected async Task<int> SaveChangesCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await Context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: UniqueViolation })
        {
            throw new BusinessRuleException("Bu anket daha önce gönderilmiş.");
        }
    }
}
