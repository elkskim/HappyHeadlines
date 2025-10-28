using SubscriberDatabase.Model;

namespace SubscriberDatabase.Data;

public interface IRepository<T>
{
    Task<IEnumerable<Subscriber>> GetSubscribersAsync();
    Task<Subscriber?> GetByIdAsync(int id);
    Task<Subscriber> CreateAsync(T entity);
    Task<Subscriber?> UpdateAsync(int id, T newEntity);
    Task<bool> DeleteAsync(int id);
}