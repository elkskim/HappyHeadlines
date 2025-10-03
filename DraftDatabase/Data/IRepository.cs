using DraftDatabase.Models;

namespace DraftDatabase.Data;

public interface IRepository<T>
{
    IEnumerable<Draft>? GetDrafts();
    T? GetById(int id);
    Task<Draft> Create(T entity);
    Task<Draft> Update(T oldEntity, T newEntity);
    Task<bool> Delete(T entity);
}