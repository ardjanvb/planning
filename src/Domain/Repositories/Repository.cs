using Domain.Contexts;
using Domain.Contracts;
using Domain.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Domain.Repositories;

public abstract class Repository<T, V> : IRepository<T, V>
    where T : class, V, IEntity
{
    protected RepositoryContext Context { get; set; }

    protected Repository(RepositoryContext context)
    {
        Context = context;
    }

    public virtual async Task<V> Add(V entity)
    {
        if (entity != null)
        {
            Context.Set<T>().Add((T)entity);
            await Context.SaveChangesAsync();
            Context.Entry(entity).State = EntityState.Detached;
        }

        return entity!;
    }

    public virtual async Task<IEnumerable<V>> Add(IEnumerable<V> entities)
    {
        var entityList = new List<V>();
        foreach (var entity in entities)
        {
            if (entity != null)
                entityList.Add(await Add(entity));
        }

        return entityList;
    }

    public virtual async Task<V?> Delete(int id)
    {
        var entity = await Context.Set<T>().FindAsync(id);
        if (entity == null)
            return entity;

        Context.Set<T>().Remove(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task DeleteAll(IEnumerable<T> records)
    {
        Context.Set<T>().RemoveRange(records);
        await Context.SaveChangesAsync();
    }

    public virtual async Task<V?> Get(int id)
    {
        return await Context.Set<T>().FindAsync(id);
    }

    public virtual async Task<IList<V>> GetAll()
    {
        return await Context.Set<T>().Cast<V>().ToListAsync();
    }

    public virtual IQueryable<T> GetQuery()
    {
        return Context.Set<T>().AsQueryable();
    }

    public virtual async Task<V> Update(V entity)
    {
        if (entity != null)
        {
            Context.Set<T>().Update((T)entity);
            await Context.SaveChangesAsync();
            Context.Entry(entity).State = EntityState.Detached;
        }

        return entity!;
    }

    public virtual async Task<IEnumerable<V>> Update(IEnumerable<V> entities)
    {
        var entityList = new List<V>();
        foreach (var entity in entities)
        {
            if (entity != null)
                entityList.Add(await Update(entity));
        }

        return entityList;
    }
}
