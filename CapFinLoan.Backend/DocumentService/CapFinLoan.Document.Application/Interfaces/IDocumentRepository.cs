namespace CapFinLoan.Document.Application.Interfaces;

public interface IDocumentRepository
{
    Task AddAsync(Domain.Entities.Document document, CancellationToken cancellationToken = default);
    Task<Domain.Entities.Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Domain.Entities.Document>> GetByLoanApplicationIdAsync(Guid loanApplicationId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Domain.Entities.Document document, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
