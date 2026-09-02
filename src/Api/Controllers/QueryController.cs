using System.Diagnostics;
using System.Security.Claims;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/query")]
[Authorize]
public class QueryController : ControllerBase
{
    private readonly IRagService _ragService;
    private readonly IQueryHistoryService _queryHistoryService;

    public QueryController(
        IRagService ragService,
        IQueryHistoryService queryHistoryService)
    {
        _ragService = ragService;
        _queryHistoryService = queryHistoryService;
    }

    [HttpPost]
    public async Task<ActionResult<RagResult>> Query(
        [FromBody] RagRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var stopwatch = Stopwatch.StartNew();

        var result = await _ragService.GenerateAnswerAsync(
            request,
            userId,
            cancellationToken);

        stopwatch.Stop();

        var sources = result.Sources
            .Select(source =>
                new QueryHistorySourceRequest
                {
                    DocumentChunkId = source.DocumentChunkId,
                    RelevanceScore = (float)source.SimilarityScore
                })
            .ToList();

        await _queryHistoryService.CreateAsync(
            userId,
            request.Query,
            result.Answer,
            result.Sources.Count > 0,
            (int)stopwatch.ElapsedMilliseconds,
            sources);

        return Ok(result);
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