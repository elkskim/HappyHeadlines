using DraftDatabase.Models;

namespace DraftDatabase.Data;

public interface IRepository<T>
{
    IEnumerable<T> GetDrafts();
    T? GetById(int id);
    Task<Draft> Create(T entity);
    Task<Draft> Update(T entity);
    Task<bool> Delete(T entity);
    
}