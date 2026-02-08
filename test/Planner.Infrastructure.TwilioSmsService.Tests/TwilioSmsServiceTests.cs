using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Planner.Infrastructure.TwilioSmsService.Tests;

public class TwilioSmsServiceTests
{
    private readonly Mock<ILogger<TwilioSmsService>> _loggerMock;
    private readonly TwilioSmsOptions _options;

    public TwilioSmsServiceTests()
    {
        _loggerMock = new Mock<ILogger<TwilioSmsService>>();
        _options = new TwilioSmsOptions
        {
            AccountSid = "test-account-sid",
            AuthToken = "test-auth-token",
            FromPhoneNumber = "+1234567890"
        };
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenOptionsIsNull()
    {
        // Act & Assert
        var act = () => new TwilioSmsService(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenLoggerIsNull()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<TwilioSmsOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);

        // Act & Assert
        var act = () => new TwilioSmsService(optionsMock.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task SendMessageAsync_ShouldReturnFalse_WhenToPhoneNumberIsNull()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<TwilioSmsOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);
        var service = new TwilioSmsService(optionsMock.Object, _loggerMock.Object);

        // Act
        var result = await service.SendMessageAsync(null!, "Test message");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendMessageAsync_ShouldReturnFalse_WhenToPhoneNumberIsEmpty()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<TwilioSmsOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);
        var service = new TwilioSmsService(optionsMock.Object, _loggerMock.Object);

        // Act
        var result = await service.SendMessageAsync(string.Empty, "Test message");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendMessageAsync_ShouldReturnFalse_WhenMessageIsNull()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<TwilioSmsOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);
        var service = new TwilioSmsService(optionsMock.Object, _loggerMock.Object);

        // Act
        var result = await service.SendMessageAsync("+1234567890", null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendMessageAsync_ShouldReturnFalse_WhenMessageIsEmpty()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<TwilioSmsOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);
        var service = new TwilioSmsService(optionsMock.Object, _loggerMock.Object);

        // Act
        var result = await service.SendMessageAsync("+1234567890", string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendMessageAsync_ShouldReturnFalse_WhenFromPhoneNumberIsEmpty()
    {
        // Arrange
        var emptyFromOptions = new TwilioSmsOptions
        {
            AccountSid = "test-account-sid",
            AuthToken = "test-auth-token",
            FromPhoneNumber = string.Empty
        };
        var optionsMock = new Mock<IOptions<TwilioSmsOptions>>();
        optionsMock.Setup(o => o.Value).Returns(emptyFromOptions);
        var service = new TwilioSmsService(optionsMock.Object, _loggerMock.Object);

        // Act
        var result = await service.SendMessageAsync("+1234567890", "Test message");

        // Assert
        result.Should().BeFalse();
    }
}
