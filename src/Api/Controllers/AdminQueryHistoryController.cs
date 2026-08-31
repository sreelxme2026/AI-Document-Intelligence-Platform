using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/admin/query-history")]
[Authorize(Roles = "Admin")]
public class AdminQueryHistoryController : ControllerBase
{
    private readonly IAdminQueryHistoryService _adminQueryHistoryService;

    public AdminQueryHistoryController(
        IAdminQueryHistoryService adminQueryHistoryService)
    {
        _adminQueryHistoryService = adminQueryHistoryService;
    }

    [HttpGet]
    public async Task<ActionResult<QueryHistoryListResponse>> GetHistory(
        [FromQuery] AdminQueryHistoryQueryParameters parameters)
    {
        var response =
            await _adminQueryHistoryService.GetHistoryAsync(
                parameters);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QueryHistoryResponse>> GetById(
        Guid id)
    {
        var response =
            await _adminQueryHistoryService.GetByIdAsync(id);

        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }
}