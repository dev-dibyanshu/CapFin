namespace CapFinLoan.Document.Application.Contracts.Responses;

public class DocumentResponse
{
    public Guid Id { get; set; }
    public Guid LoanApplicationId { get; set; }
    public Guid UploadedByUserId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public Guid? VerifiedByUserId { get; set; }
}
