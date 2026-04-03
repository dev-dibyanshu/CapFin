using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CapFinLoan.Document.Application.Contracts.Requests;

public class UploadDocumentRequest
{
    [Required]
    public Guid LoanApplicationId { get; set; }

    [Required]
    public string DocumentType { get; set; } = string.Empty;

    [Required]
    public IFormFile File { get; set; } = null!;
}
