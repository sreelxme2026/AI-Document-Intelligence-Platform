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
    }
}