namespace Domain.Contracts;

public interface IRepository<T, V> where T : class, V
{
    Task<IList<V>> GetAll();
    Task<V?> Get(int id);
    Task<V> Add(V entity);
    Task<IEnumerable<V>> Add(IEnumerable<V> entities);
    Task<V> Update(V entity);
    Task<IEnumerable<V>> Update(IEnumerable<V> entities);
    Task<V?> Delete(int id);
    IQueryable<T> GetQuery();
    Task DeleteAll(IEnumerable<T> records);
}
