using Api.Configuration;
using Application.Configuration;

namespace Tests;

public class JwtConfigurationValidatorTests
{
    [Fact]
    public void Validate_ValidSettings_DoesNotThrow()
    {
        var settings = CreateValidSettings();

        var exception = Record.Exception(
            () => JwtConfigurationValidator.Validate(settings));

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_NullSettings_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => JwtConfigurationValidator.Validate(null!));
    }

    [Fact]
    public void Validate_MissingIssuer_ThrowsInvalidOperationException()
    {
        var settings = CreateValidSettings();
        settings.Issuer = string.Empty;

        var exception =
            Assert.Throws<InvalidOperationException>(
                () => JwtConfigurationValidator.Validate(settings));

        Assert.Equal(
            "JWT issuer is not configured.",
            exception.Message);
    }

    [Fact]
    public void Validate_MissingAudience_ThrowsInvalidOperationException()
    {
        var settings = CreateValidSettings();
        settings.Audience = string.Empty;

        var exception =
            Assert.Throws<InvalidOperationException>(
                () => JwtConfigurationValidator.Validate(settings));

        Assert.Equal(
            "JWT audience is not configured.",
            exception.Message);
    }

    [Fact]
    public void Validate_MissingSecretKey_ThrowsInvalidOperationException()
    {
        var settings = CreateValidSettings();
        settings.SecretKey = string.Empty;

        var exception =
            Assert.Throws<InvalidOperationException>(
                () => JwtConfigurationValidator.Validate(settings));

        Assert.Equal(
            "JWT secret key is not configured.",
            exception.Message);
    }

    [Fact]
    public void Validate_ShortSecretKey_ThrowsInvalidOperationException()
    {
        var settings = CreateValidSettings();
        settings.SecretKey = new string('a', 31);

        var exception =
            Assert.Throws<InvalidOperationException>(
                () => JwtConfigurationValidator.Validate(settings));

        Assert.Equal(
            "JWT secret key must be at least 32 characters long.",
            exception.Message);
    }

    [Fact]
    public void Validate_ZeroExpiration_ThrowsInvalidOperationException()
    {
        var settings = CreateValidSettings();
        settings.ExpirationMinutes = 0;

        var exception =
            Assert.Throws<InvalidOperationException>(
                () => JwtConfigurationValidator.Validate(settings));

        Assert.Equal(
            "JWT expiration must be greater than zero.",
            exception.Message);
    }

    [Fact]
    public void Validate_NegativeExpiration_ThrowsInvalidOperationException()
    {
        var settings = CreateValidSettings();
        settings.ExpirationMinutes = -1;

        var exception =
            Assert.Throws<InvalidOperationException>(
                () => JwtConfigurationValidator.Validate(settings));

        Assert.Equal(
            "JWT expiration must be greater than zero.",
            exception.Message);
    }

    private static JwtSettings CreateValidSettings()
    {
        return new JwtSettings
        {
            Issuer = "AI-Document-Intelligence-Platform",
            Audience = "AI-Document-Intelligence-Platform",
            SecretKey =
                "abcdefghijklmnopqrstuvwxyz123456",
            ExpirationMinutes = 60
        };
    }
}