using System.Security.Claims;
using CapFinLoan.Document.Application.Contracts.Requests;
using CapFinLoan.Document.Application.Interfaces;
using CapFinLoan.Document.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapFinLoan.Document.API.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".jpg", ".jpeg", ".png" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    [HttpPost("upload")]
    [Authorize(Roles = RoleNames.Applicant)]
    public async Task<IActionResult> Upload([FromForm] UploadDocumentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { success = false, message = "Request cannot be null.", data = (object?)null });
            }

            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new { success = false, message = "File is required.", data = (object?)null });
            }

            if (request.File.Length > MaxFileSizeBytes)
            {
                return BadRequest(new { success = false, message = $"File size exceeds maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)} MB.", data = (object?)null });
            }

            var extension = Path.GetExtension(request.File.FileName);
            if (!AllowedExtensions.Contains(extension))
            {
                return BadRequest(new { success = false, message = $"File type {extension} is not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}", data = (object?)null });
            }

            var document = await _documentService.UploadAsync(GetUserId(), request, cancellationToken);
            return Ok(new { success = true, message = "Document uploaded successfully.", data = document });
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogError(exception, "Invalid operation during document upload");
            return BadRequest(new { success = false, message = exception.Message, data = (object?)null });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error uploading document");
            return StatusCode(500, new 
            { 
                success = false, 
                message = exception.Message,
                inner = exception.InnerException?.Message,
                stack = exception.StackTrace
            });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var document = await _documentService.GetByIdAsync(id, GetUserId(), IsAdmin(), cancellationToken);
            return Ok(new { success = true, message = "Document retrieved successfully.", data = document });
        }
        catch (KeyNotFoundException exception)
        {
            _logger.LogWarning(exception, "Document not found: {DocumentId}", id);
            return NotFound(new { success = false, message = exception.Message, data = (object?)null });
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Unauthorized access to document: {DocumentId}", id);
            return StatusCode(403, new { success = false, message = "You are not authorized to access this document.", data = (object?)null });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error retrieving document: {DocumentId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving the document.", data = (object?)null });
        }
    }

    [HttpGet("application/{loanApplicationId:guid}")]
    public async Task<IActionResult> GetByLoanApplicationId(Guid loanApplicationId, CancellationToken cancellationToken)
    {
        try
        {
            var documents = await _documentService.GetByLoanApplicationIdAsync(loanApplicationId, GetUserId(), IsAdmin(), cancellationToken);
            return Ok(new { success = true, message = "Documents retrieved successfully.", data = documents });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error retrieving documents for application: {ApplicationId}", loanApplicationId);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving documents.", data = (object?)null });
        }
    }

    [HttpPut("{id:guid}/verify")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Verify(Guid id, [FromBody] VerifyDocumentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { success = false, message = "Request cannot be null.", data = (object?)null });
            }

            if (string.IsNullOrWhiteSpace(request.Status))
            {
                return BadRequest(new { success = false, message = "Status is required.", data = (object?)null });
            }

            var document = await _documentService.VerifyAsync(id, GetUserId(), request, cancellationToken);
            return Ok(new { success = true, message = "Document verified successfully.", data = document });
        }
        catch (KeyNotFoundException exception)
        {
            _logger.LogWarning(exception, "Document not found for verification: {DocumentId}", id);
            return NotFound(new { success = false, message = exception.Message, data = (object?)null });
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogError(exception, "Invalid operation during document verification");
            return BadRequest(new { success = false, message = exception.Message, data = (object?)null });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error verifying document: {DocumentId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while verifying the document.", data = (object?)null });
        }
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var (metadata, fileBytes) = await _documentService.GetDocumentWithFileAsync(id, GetUserId(), IsAdmin(), cancellationToken);
            
            return File(fileBytes, "application/octet-stream", metadata.FileName);
        }
        catch (KeyNotFoundException exception)
        {
            _logger.LogWarning(exception, "Document not found for download: {DocumentId}", id);
            return NotFound(new { success = false, message = exception.Message, data = (object?)null });
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Unauthorized download attempt: {DocumentId}", id);
            return StatusCode(403, new { success = false, message = "You are not authorized to download this document.", data = (object?)null });
        }
        catch (FileNotFoundException exception)
        {
            _logger.LogError(exception, "File not found in storage: {DocumentId}", id);
            return NotFound(new { success = false, message = exception.Message, data = (object?)null });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error downloading document: {DocumentId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while downloading the document.", data = (object?)null });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _documentService.DeleteAsync(id, GetUserId(), IsAdmin(), cancellationToken);
            return Ok(new { success = true, message = "Document deleted successfully.", data = (object?)null });
        }
        catch (KeyNotFoundException exception)
        {
            _logger.LogWarning(exception, "Document not found for deletion: {DocumentId}", id);
            return NotFound(new { success = false, message = exception.Message, data = (object?)null });
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Unauthorized deletion attempt: {DocumentId}", id);
            return StatusCode(403, new { success = false, message = "You are not authorized to delete this document.", data = (object?)null });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error deleting document: {DocumentId}", id);
            return StatusCode(500, new { success = false, message = "An error occurred while deleting the document.", data = (object?)null });
        }
    }

    private Guid GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("User identifier claim is missing.");
    }

    private bool IsAdmin()
    {
        return User.IsInRole(RoleNames.Admin);
    }
}
