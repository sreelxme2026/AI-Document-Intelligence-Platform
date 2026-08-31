using Api.Controllers;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tests;

public class AdminUsersControllerTests
{
    [Fact]
    public async Task GetUsers_ReturnsOkWithUsers()
    {
        var expected = new AdminUserListResponse
        {
            Items =
            [
                new AdminUserResponse
                {
                    Id = Guid.NewGuid(),
                    Email = "admin@example.com",
                    UserName = "admin@example.com",
                    Role = "Admin",
                    CreatedAt = DateTime.UtcNow
                }
            ],
            Page = 1,
            PageSize = 10,
            TotalCount = 1,
            TotalPages = 1
        };

        var service = new FakeAdminUserService
        {
            UsersResult = expected
        };

        var controller = new AdminUsersController(service);

        var result = await controller.GetUsers(
            new AdminUserQueryParameters());

        var okResult = Assert.IsType<OkObjectResult>(
            result.Result);

        var response = Assert.IsType<AdminUserListResponse>(
            okResult.Value);

        Assert.Equal(
            expected.TotalCount,
            response.TotalCount);

        Assert.Single(response.Items);
        Assert.Equal(
            "admin@example.com",
            response.Items[0].Email);
    }

    [Fact]
    public async Task GetUsers_PassesParametersToService()
    {
        var service = new FakeAdminUserService
        {
            UsersResult = new AdminUserListResponse()
        };

        var controller = new AdminUsersController(service);

        var parameters = new AdminUserQueryParameters
        {
            Page = 2,
            PageSize = 25,
            Search = "john"
        };

        await controller.GetUsers(parameters);

        Assert.Same(
            parameters,
            service.LastParameters);
    }

    [Fact]
    public async Task Delete_ExistingUser_ReturnsNoContent()
    {
        var service = new FakeAdminUserService
        {
            DeleteResult = true
        };

        var controller = new AdminUsersController(service);

        var userId = Guid.NewGuid();

        var result = await controller.Delete(userId);

        Assert.IsType<NoContentResult>(result);

        Assert.Equal(
            userId,
            service.LastDeletedUserId);
    }

    [Fact]
    public async Task Delete_MissingUser_ReturnsNotFound()
    {
        var service = new FakeAdminUserService
        {
            DeleteResult = false
        };

        var controller = new AdminUsersController(service);

        var result = await controller.Delete(
            Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Controller_RequiresAdminRole()
    {
        var attribute = typeof(AdminUsersController)
            .GetCustomAttributes(
                typeof(AuthorizeAttribute),
                inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal(
            "Admin",
            attribute.Roles);
    }

    private sealed class FakeAdminUserService
        : IAdminUserService
    {
        public AdminUserListResponse UsersResult { get; set; }
            = new();

        public bool DeleteResult { get; set; }

        public AdminUserQueryParameters? LastParameters { get; private set; }

        public Guid LastDeletedUserId { get; private set; }

        public Task<AdminUserListResponse> GetUsersAsync(
            AdminUserQueryParameters parameters)
        {
            LastParameters = parameters;

            return Task.FromResult(UsersResult);
        }

        public Task<bool> DeleteAsync(
            Guid userId)
        {
            LastDeletedUserId = userId;

            return Task.FromResult(DeleteResult);
        }
    }
}