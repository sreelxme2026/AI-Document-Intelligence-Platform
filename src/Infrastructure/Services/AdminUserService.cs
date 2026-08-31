using Application.DTOs;
using Application.Entities;
using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class AdminUserService : IAdminUserService
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly IFileStorageService _fileStorageService;

    public AdminUserService(
        AppDbContext dbContext,
        UserManager<User> userManager,
        IFileStorageService fileStorageService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _fileStorageService = fileStorageService;
    }

    public async Task<AdminUserListResponse> GetUsersAsync(
        AdminUserQueryParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var page = parameters.Page < 1
            ? 1
            : parameters.Page;

        var pageSize = parameters.PageSize < 1
            ? 10
            : Math.Min(parameters.PageSize, 100);

        var query = _dbContext.Users
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim();

            query = query.Where(user =>
                (user.Email != null &&
                 user.Email.Contains(search)) ||
                (user.UserName != null &&
                 user.UserName.Contains(search)));
        }

        var totalCount = await query.CountAsync();

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(
                totalCount / (double)pageSize);

        var users = await query
            .OrderByDescending(user => user.CreatedAt)
            .ThenBy(user => user.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new AdminUserListResponse
        {
            Items = users
                .Select(MapToResponse)
                .ToList(),

            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<bool> DeleteAsync(
        Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(userId));
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(user =>
                user.Id == userId);

        if (user is null)
        {
            return false;
        }

        var documents = await _dbContext.Documents
            .Where(document =>
                document.UploadedByUserId == userId)
            .ToListAsync();

        var documentIds = documents
            .Select(document => document.Id)
            .ToList();

        var documentChunkIds = await _dbContext.DocumentChunks
            .Where(chunk =>
                documentIds.Contains(chunk.DocumentId))
            .Select(chunk => chunk.Id)
            .ToListAsync();

        if (documentChunkIds.Count > 0)
        {
            var queryHistorySources =
                await _dbContext.QueryHistorySources
                    .Where(source =>
                        documentChunkIds.Contains(
                            source.DocumentChunkId))
                    .ToListAsync();

            _dbContext.QueryHistorySources.RemoveRange(
                queryHistorySources);
        }

        _dbContext.Documents.RemoveRange(documents);

        var queryHistories = await _dbContext.QueryHistories
            .Where(history =>
                history.UserId == userId)
            .ToListAsync();

        _dbContext.QueryHistories.RemoveRange(
            queryHistories);

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    result.Errors.Select(error =>
                        error.Description));

                throw new InvalidOperationException(
                    $"Failed to delete user: {errors}");
            }

            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        foreach (var document in documents)
        {
            await _fileStorageService.DeleteAsync(
                document.StoragePath);
        }

        return true;
    }

    private static AdminUserResponse MapToResponse(
        User user)
    {
        return new AdminUserResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt
        };
    }
}