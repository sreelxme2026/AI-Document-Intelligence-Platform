using System.Security.Claims;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/query/history")]
[Authorize]
public class QueryHistoryController : ControllerBase
{
    private readonly IQueryHistoryService _queryHistoryService;

    public QueryHistoryController(
        IQueryHistoryService queryHistoryService)
    {
        _queryHistoryService = queryHistoryService;
    }

    [HttpGet]
    public async Task<ActionResult<QueryHistoryListResponse>> GetHistory(
        [FromQuery] QueryHistoryQueryParameters parameters)
    {
        var userId = GetUserId();

        var response = await _queryHistoryService.GetHistoryAsync(
            userId,
            parameters);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QueryHistoryResponse>> GetById(
        Guid id)
    {
        var userId = GetUserId();

        var response = await _queryHistoryService.GetByIdAsync(
            userId,
            id);

        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    private Guid GetUserId()
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
                userIdValue,
                out var userId))
        {
            throw new UnauthorizedAccessException(
                "Invalid user identity.");
        }

        return userId;
    }
}