using Application.DTOs;
namespace Application.Interfaces;

public interface ITextChunker
{
    IReadOnlyList<TextChunkResult> Chunk(string text);
}