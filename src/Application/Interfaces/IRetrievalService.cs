using Application.DTOs;

namespace Application.Interfaces;

public interface IRetrievalService
{
    Task<RetrievalResult> RetrieveAsync(
        RetrievalRequest request,
        CancellationToken cancellationToken);
}