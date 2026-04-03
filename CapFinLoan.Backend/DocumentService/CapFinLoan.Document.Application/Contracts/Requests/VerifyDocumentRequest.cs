using System.ComponentModel.DataAnnotations;

namespace CapFinLoan.Document.Application.Contracts.Requests;

public class VerifyDocumentRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Remarks { get; set; } = string.Empty;
}
