using DevTrack.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DevTrack.Infrastructure.Data;

// This is the ONLY class in the whole solution that knows EF Core exists
// for basic CRUD. Controllers depend on IRepository<T> (from Core), and
// Dependency Injection hands them THIS class at runtime.
public class Repository<T> : IRepository<T> where T : class
{
    private readonly DevTrackDbContext _context;
    private readonly DbSet<T> _dbSet;

    public Repository(DevTrackDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();

    public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

    public void Update(T entity) => _dbSet.Update(entity);

    public void Delete(T entity) => _dbSet.Remove(entity);

    // Note: Add/Update/Delete above don't hit the database immediately —
    // EF Core just tracks the change in memory. SaveChangesAsync() is what
    // actually sends the SQL. This is the "Unit of Work" idea: you can
    // add a Project AND a Sprint in one method, then call SaveChangesAsync
    // once, and both go in a single transaction.
    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
}
