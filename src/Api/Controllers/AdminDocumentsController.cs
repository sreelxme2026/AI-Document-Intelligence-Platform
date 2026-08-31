using System.Security.Claims;
using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/admin/documents")]
[Authorize(Roles = "Admin")]
public class AdminDocumentsController : ControllerBase
{
    private readonly IAdminDocumentService _adminDocumentService;

    public AdminDocumentsController(
        IAdminDocumentService adminDocumentService)
    {
        _adminDocumentService = adminDocumentService;
    }

    [HttpGet]
    public async Task<ActionResult<DocumentListResponse>> GetDocuments(
        [FromQuery] AdminDocumentQueryParameters parameters)
    {
        var response =
            await _adminDocumentService.GetDocumentsAsync(
                parameters);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentResponse>> GetById(
        Guid id)
    {
        var response =
            await _adminDocumentService.GetByIdAsync(id);

        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<DocumentResponse>> Upload(
        IFormFile file,
        [FromForm] Guid userId,
        [FromForm] string? title,
        [FromForm] string? description,
        [FromForm] string? tags)
    {
        try
        {
            var response =
                await _adminDocumentService.UploadAsync(
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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id)
    {
        var deleted =
            await _adminDocumentService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}