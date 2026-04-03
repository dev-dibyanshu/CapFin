using CapFinLoan.Application.Domain.Entities;

namespace CapFinLoan.Application.Application.Interfaces;

public interface IApplicationStatusHistoryRepository
{
    Task AddAsync(ApplicationStatusHistory history, CancellationToken cancellationToken = default);
    Task AddWithoutSaveAsync(ApplicationStatusHistory history, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
