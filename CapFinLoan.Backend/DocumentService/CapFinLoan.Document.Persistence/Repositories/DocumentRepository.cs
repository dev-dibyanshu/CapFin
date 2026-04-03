using CapFinLoan.Document.Application.Interfaces;
using CapFinLoan.Document.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace CapFinLoan.Document.Persistence.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly DocumentDbContext _dbContext;

    public DocumentRepository(DocumentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Domain.Entities.Document document, CancellationToken cancellationToken = default)
    {
        await _dbContext.Documents.AddAsync(document, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Domain.Entities.Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Documents.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Domain.Entities.Document>> GetByLoanApplicationIdAsync(Guid loanApplicationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Documents
            .AsNoTracking()
            .Where(x => x.LoanApplicationId == loanApplicationId)
            .OrderByDescending(x => x.UploadedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Domain.Entities.Document document, CancellationToken cancellationToken = default)
    {
        _dbContext.Documents.Update(document);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.Documents.FindAsync(new object[] { id }, cancellationToken);
        if (document != null)
        {
            _dbContext.Documents.Remove(document);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
