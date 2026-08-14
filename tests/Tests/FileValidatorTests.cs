using Application.Exceptions;
using Infrastructure.Services;

namespace Tests;

public class FileValidatorTests
{
    private readonly FileValidator _validator = new();

    [Fact]
    public void Validate_PdfUnder25Mb_DoesNotThrow()
    {
        _validator.Validate(
            "document.pdf",
            "application/pdf",
            1024);
    }

    [Fact]
    public void Validate_TxtUnder25Mb_DoesNotThrow()
    {
        _validator.Validate(
            "document.txt",
            "text/plain",
            1024);
    }

    [Fact]
    public void Validate_EmptyFile_ThrowsInvalidFileException()
    {
        var exception = Assert.Throws<InvalidFileException>(() =>
            _validator.Validate(
                "document.pdf",
                "application/pdf",
                0));

        Assert.Equal("File cannot be empty.", exception.Message);
    }

    [Fact]
    public void Validate_FileOver25Mb_ThrowsInvalidFileException()
    {
        const long twentyFiveMbPlusOne = (25 * 1024 * 1024) + 1;

        var exception = Assert.Throws<InvalidFileException>(() =>
            _validator.Validate(
                "document.pdf",
                "application/pdf",
                twentyFiveMbPlusOne));

        Assert.Equal(
            "File size cannot exceed 25 MB.",
            exception.Message);
    }

    [Fact]
    public void Validate_UnsupportedContentType_ThrowsInvalidFileException()
    {
        var exception = Assert.Throws<InvalidFileException>(() =>
            _validator.Validate(
                "document.jpg",
                "image/jpeg",
                1024));

        Assert.Equal(
            "Only PDF and TXT files are supported.",
            exception.Message);
    }

    [Fact]
    public void Validate_EmptyFileName_ThrowsInvalidFileException()
    {
        var exception = Assert.Throws<InvalidFileException>(() =>
            _validator.Validate(
                "",
                "application/pdf",
                1024));

        Assert.Equal(
            "File name is required.",
            exception.Message);
    }
}