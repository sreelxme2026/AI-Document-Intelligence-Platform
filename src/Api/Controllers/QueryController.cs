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

    public QueryController(IRagService ragService)
    {
        _ragService = ragService;
    }

    [HttpPost]
    public async Task<ActionResult<RagResult>> Query(
        [FromBody] RagRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ragService.GenerateAnswerAsync(
            request,
            cancellationToken);

        return Ok(result);
    }
}