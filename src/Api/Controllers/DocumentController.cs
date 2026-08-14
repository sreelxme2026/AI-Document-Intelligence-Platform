using System.Security.Claims;
using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/documents")]
[Authorize]
public class DocumentController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<DocumentResponse>> Upload(
        IFormFile file,
        [FromForm] string? title,
        [FromForm] string? description,
        [FromForm] string? tags)
    {
        var userId = GetUserId();

        try
        {
            var response = await _documentService.UploadAsync(
                userId,
                file.OpenReadStream(),
                file.FileName,
                file.ContentType,
                file.Length,
                title,
                description,
                tags);

            return StatusCode(
                StatusCodes.Status201Created,
                response);
        }
        catch (InvalidFileException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<DocumentListResponse>> GetDocuments(
        [FromQuery] DocumentQueryParameters parameters)
    {
        var userId = GetUserId();

        var response = await _documentService.GetDocumentsAsync(
            userId,
            parameters);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentResponse>> GetById(
        Guid id)
    {
        var userId = GetUserId();

        var response = await _documentService.GetByIdAsync(
            userId,
            id);

        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpGet("{id:guid}/status")]
    public async Task<ActionResult<DocumentStatusResponse>> GetStatus(
        Guid id)
    {
        var userId = GetUserId();

        var response = await _documentService.GetStatusAsync(
            userId,
            id);

        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id)
    {
        var userId = GetUserId();

        var deleted = await _documentService.DeleteAsync(
            userId,
            id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    private Guid GetUserId()
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedAccessException(
                "Invalid user identity.");
        }

        return userId;
    }
}