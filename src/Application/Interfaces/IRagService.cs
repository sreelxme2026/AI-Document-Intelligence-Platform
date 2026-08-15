using Application.DTOs;

namespace Application.Interfaces;

public interface IRagService
{
    Task<RagResult> GenerateAnswerAsync(
        RagRequest request,
        CancellationToken cancellationToken);
}