using Api.Controllers;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tests;

public class AdminDocumentsControllerTests
{
    [Fact]
    public async Task GetDocuments_ReturnsOkWithDocuments()
    {
        var expected = new DocumentListResponse
        {
            Items =
            [
                new DocumentResponse
                {
                    Id = Guid.NewGuid(),
                    FileName = "test.pdf",
                    OriginalFileName = "test.pdf",
                    ContentType = "application/pdf",
                    Status = "Completed",
                    UploadedAt = DateTime.UtcNow
                }
            ],
            Page = 1,
            PageSize = 10,
            TotalCount = 1,
            TotalPages = 1
        };

        var service = new FakeAdminDocumentService
        {
            DocumentsResult = expected
        };

        var controller = new AdminDocumentsController(service);

        var result = await controller.GetDocuments(
            new AdminDocumentQueryParameters());

        var okResult = Assert.IsType<OkObjectResult>(
            result.Result);

        var response = Assert.IsType<DocumentListResponse>(
            okResult.Value);

        Assert.Equal(
            expected.TotalCount,
            response.TotalCount);

        Assert.Single(response.Items);

        Assert.Equal(
            "test.pdf",
            response.Items[0].FileName);
    }

    [Fact]
    public async Task GetDocuments_PassesParametersToService()
    {
        var service = new FakeAdminDocumentService
        {
            DocumentsResult = new DocumentListResponse()
        };

        var controller = new AdminDocumentsController(service);

        var parameters = new AdminDocumentQueryParameters
        {
            Page = 2,
            PageSize = 25,
            Search = "leave"
        };

        await controller.GetDocuments(parameters);

        Assert.Same(
            parameters,
            service.LastParameters);
    }

    [Fact]
    public async Task GetById_ExistingDocument_ReturnsOk()
    {
        var documentId = Guid.NewGuid();

        var expected = new DocumentResponse
        {
            Id = documentId,
            FileName = "test.pdf",
            OriginalFileName = "test.pdf",
            ContentType = "application/pdf",
            Status = "Completed",
            UploadedAt = DateTime.UtcNow
        };

        var service = new FakeAdminDocumentService
        {
            DocumentResult = expected
        };

        var controller = new AdminDocumentsController(service);

        var result = await controller.GetById(documentId);

        var okResult = Assert.IsType<OkObjectResult>(
            result.Result);

        var response = Assert.IsType<DocumentResponse>(
            okResult.Value);

        Assert.Equal(
            documentId,
            response.Id);

        Assert.Equal(
            "test.pdf",
            response.FileName);

        Assert.Equal(
            documentId,
            service.LastDocumentId);
    }

    [Fact]
    public async Task GetById_MissingDocument_ReturnsNotFound()
    {
        var service = new FakeAdminDocumentService
        {
            DocumentResult = null
        };

        var controller = new AdminDocumentsController(service);

        var result = await controller.GetById(
            Guid.NewGuid());

        Assert.IsType<NotFoundResult>(
            result.Result);
    }

    [Fact]
    public async Task View_ExistingDocument_ReturnsFileStream()
    {
        var documentId = Guid.NewGuid();

        var stream = new MemoryStream(
            "test document content"u8.ToArray());

        var service = new FakeAdminDocumentService
        {
            OpenResult = (
                stream,
                "application/pdf",
                "test.pdf")
        };

        var controller = new AdminDocumentsController(service);

        var result = await controller.View(documentId);

        var fileResult = Assert.IsType<FileStreamResult>(
            result);

        Assert.Same(
            stream,
            fileResult.FileStream);

        Assert.Equal(
            "application/pdf",
            fileResult.ContentType);

        Assert.True(
            fileResult.EnableRangeProcessing);

        Assert.Equal(
            documentId,
            service.LastOpenedDocumentId);
    }

    [Fact]
    public async Task View_MissingDocument_ReturnsNotFound()
    {
        var service = new FakeAdminDocumentService
        {
            OpenResult = null
        };

        var controller = new AdminDocumentsController(service);

        var result = await controller.View(
            Guid.NewGuid());

        Assert.IsType<NotFoundResult>(
            result);
    }

    [Fact]
    public async Task GrantAccess_ReturnsOkWithAccessResponse()
    {
        var documentId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var expected = new DocumentAccessResponse
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            UserId = userId,
            GrantedAt = DateTime.UtcNow
        };

        var service = new FakeAdminDocumentService
        {
            GrantAccessResult = expected
        };

        var controller = new AdminDocumentsController(service);

        var request = new AdminDocumentAccessRequest
        {
            UserId = userId
        };

        var result = await controller.GrantAccess(
            documentId,
            request);

        var okResult = Assert.IsType<OkObjectResult>(
            result.Result);

        var response = Assert.IsType<DocumentAccessResponse>(
            okResult.Value);

        Assert.Equal(
            expected.Id,
            response.Id);

        Assert.Equal(
            documentId,
            response.DocumentId);

        Assert.Equal(
            userId,
            response.UserId);

        Assert.Equal(
            documentId,
            service.LastGrantDocumentId);

        Assert.Equal(
            userId,
            service.LastGrantUserId);
    }

    [Fact]
    public async Task GrantAccess_InvalidRequest_ReturnsBadRequest()
    {
        var service = new FakeAdminDocumentService
        {
            GrantAccessException =
                new ArgumentException(
                    "User ID cannot be empty.")
        };

        var controller = new AdminDocumentsController(service);

        var documentId = Guid.NewGuid();

        var request = new AdminDocumentAccessRequest
        {
            UserId = Guid.Empty
        };

        var result = await controller.GrantAccess(
            documentId,
            request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(
            result.Result);

        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task GrantAccess_InvalidOperation_ReturnsBadRequest()
    {
        var service = new FakeAdminDocumentService
        {
            GrantAccessException =
                new InvalidOperationException(
                    "The specified document does not exist.")
        };

        var controller = new AdminDocumentsController(service);

        var result = await controller.GrantAccess(
            Guid.NewGuid(),
            new AdminDocumentAccessRequest
            {
                UserId = Guid.NewGuid()
            });

        var badRequest = Assert.IsType<BadRequestObjectResult>(
            result.Result);

        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task RevokeAccess_ExistingAccess_ReturnsNoContent()
    {
        var documentId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var service = new FakeAdminDocumentService
        {
            RevokeAccessResult = true
        };

        var controller = new AdminDocumentsController(service);

        var result = await controller.RevokeAccess(
            documentId,
            userId);

        Assert.IsType<NoContentResult>(result);

        Assert.Equal(
            documentId,
            service.LastRevokeDocumentId);

        Assert.Equal(
            userId,
            service.LastRevokeUserId);
    }

    [Fact]
    public async Task RevokeAccess_MissingAccess_ReturnsNotFound()
    {
        var service = new FakeAdminDocumentService
        {
            RevokeAccessResult = false
        };

        var controller = new AdminDocumentsController(service);

        var result = await controller.RevokeAccess(
            Guid.NewGuid(),
            Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task RevokeAccess_InvalidRequest_ReturnsBadRequest()
    {
        var service = new FakeAdminDocumentService
        {
            RevokeAccessException =
                new ArgumentException(
                    "User ID cannot be empty.")
        };

        var controller = new AdminDocumentsController(service);

        var result = await controller.RevokeAccess(
            Guid.NewGuid(),
            Guid.Empty);

        var badRequest = Assert.IsType<BadRequestObjectResult>(
            result);

        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public void Controller_RequiresAdminRole()
    {
        var attribute = typeof(AdminDocumentsController)
            .GetCustomAttributes(
                typeof(AuthorizeAttribute),
                inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal(
            "Admin",
            attribute.Roles);
    }

    private sealed class FakeAdminDocumentService
        : IAdminDocumentService
    {
        public DocumentListResponse DocumentsResult { get; set; }
            = new();

        public DocumentResponse? DocumentResult { get; set; }

        public (
            Stream Stream,
            string ContentType,
            string FileName)? OpenResult
        { get; set; }

        public DocumentAccessResponse GrantAccessResult { get; set; }
            = new();

        public bool RevokeAccessResult { get; set; }

        public Exception? GrantAccessException { get; set; }

        public Exception? RevokeAccessException { get; set; }

        public AdminDocumentQueryParameters? LastParameters
        {
            get;
            private set;
        }

        public Guid LastDocumentId
        {
            get;
            private set;
        }

        public Guid LastOpenedDocumentId
        {
            get;
            private set;
        }

        public Guid LastGrantDocumentId
        {
            get;
            private set;
        }

        public Guid LastGrantUserId
        {
            get;
            private set;
        }

        public Guid LastRevokeDocumentId
        {
            get;
            private set;
        }

        public Guid LastRevokeUserId
        {
            get;
            private set;
        }

        public Task<DocumentListResponse> GetDocumentsAsync(
            AdminDocumentQueryParameters parameters)
        {
            LastParameters = parameters;

            return Task.FromResult(
                DocumentsResult);
        }

        public Task<DocumentResponse?> GetByIdAsync(
            Guid documentId)
        {
            LastDocumentId = documentId;

            return Task.FromResult(
                DocumentResult);
        }

        public Task<(
            Stream Stream,
            string ContentType,
            string FileName)?> OpenAsync(
            Guid documentId)
        {
            LastOpenedDocumentId = documentId;

            return Task.FromResult(
                OpenResult);
        }

        public Task<DocumentResponse> UploadAsync(
            Guid userId,
            Stream fileStream,
            string fileName,
            string contentType,
            long fileSize,
            string? title,
            string? description,
            string? tags)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAsync(
            Guid documentId)
        {
            throw new NotImplementedException();
        }

        public Task<DocumentAccessResponse> GrantAccessAsync(
            Guid documentId,
            Guid userId)
        {
            LastGrantDocumentId = documentId;
            LastGrantUserId = userId;

            if (GrantAccessException is not null)
            {
                return Task.FromException<DocumentAccessResponse>(
                    GrantAccessException);
            }

            return Task.FromResult(
                GrantAccessResult);
        }

        public Task<bool> RevokeAccessAsync(
            Guid documentId,
            Guid userId)
        {
            LastRevokeDocumentId = documentId;
            LastRevokeUserId = userId;

            if (RevokeAccessException is not null)
            {
                return Task.FromException<bool>(
                    RevokeAccessException);
            }

            return Task.FromResult(
                RevokeAccessResult);
        }
    }
}