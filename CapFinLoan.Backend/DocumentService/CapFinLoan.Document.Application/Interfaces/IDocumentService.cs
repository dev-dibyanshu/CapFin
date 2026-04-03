using CapFinLoan.Document.Application.Contracts.Requests;
using CapFinLoan.Document.Application.Contracts.Responses;

namespace CapFinLoan.Document.Application.Interfaces;

public interface IDocumentService
{
    Task<DocumentResponse> UploadAsync(Guid uploaderUserId, UploadDocumentRequest request, CancellationToken cancellationToken = default);
    Task<DocumentResponse> GetByIdAsync(Guid documentId, Guid requesterUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<DocumentResponse>> GetByLoanApplicationIdAsync(Guid loanApplicationId, Guid requesterUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<DocumentResponse> VerifyAsync(Guid documentId, Guid verifierUserId, VerifyDocumentRequest request, CancellationToken cancellationToken = default);
    Task<byte[]> DownloadAsync(Guid documentId, Guid requesterUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<(DocumentResponse metadata, byte[] fileBytes)> GetDocumentWithFileAsync(Guid documentId, Guid requesterUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid documentId, Guid requesterUserId, bool isAdmin, CancellationToken cancellationToken = default);
}
