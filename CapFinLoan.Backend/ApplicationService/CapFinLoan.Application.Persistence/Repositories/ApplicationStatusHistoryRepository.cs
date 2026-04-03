using CapFinLoan.Application.Application.Interfaces;
using CapFinLoan.Application.Domain.Entities;
using CapFinLoan.Application.Persistence.Data;

namespace CapFinLoan.Application.Persistence.Repositories;

public class ApplicationStatusHistoryRepository : IApplicationStatusHistoryRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ApplicationStatusHistoryRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ApplicationStatusHistory history, CancellationToken cancellationToken = default)
    {
        await _dbContext.ApplicationStatusHistories.AddAsync(history, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddWithoutSaveAsync(ApplicationStatusHistory history, CancellationToken cancellationToken = default)
    {
        await _dbContext.ApplicationStatusHistories.AddAsync(history, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
