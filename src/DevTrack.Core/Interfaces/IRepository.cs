namespace DevTrack.Core.Interfaces;

// A generic repository contract. Controllers/Services depend on THIS,
// never on EF Core's DbContext directly. That's what makes the Web
// and Api projects unit-testable without a real database.
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task<int> SaveChangesAsync();
}
