using Application.DTOs;

public interface IRagService
{
    Task<RagResult> GenerateAnswerAsync(
        RagRequest request,
        Guid? userId,
        CancellationToken cancellationToken);
}