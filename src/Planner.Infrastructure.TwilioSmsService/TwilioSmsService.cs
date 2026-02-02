using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Planner.Application;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Planner.Infrastructure.TwilioSmsService;

/// <summary>
/// Implementation of ISmsService using Twilio for sending SMS messages.
/// </summary>
public sealed class TwilioSmsService : ISmsService
{
    private readonly TwilioSmsOptions _options;
    private readonly ILogger<TwilioSmsService> _logger;

    public TwilioSmsService(
        IOptions<TwilioSmsOptions> options,
        ILogger<TwilioSmsService> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize Twilio client
        TwilioClient.Init(_options.AccountSid, _options.AuthToken);
    }

    /// <inheritdoc />
    public async Task<bool> SendMessageAsync(
        string toPhoneNumber,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toPhoneNumber))
        {
            _logger.LogError("Cannot send SMS: recipient phone number is null or empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogError("Cannot send SMS: message body is null or empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_options.FromPhoneNumber))
        {
            _logger.LogError("Cannot send SMS: FromPhoneNumber configuration is missing");
            return false;
        }

        try
        {
            _logger.LogInformation(
                "Sending SMS to {PhoneNumber} from {FromNumber}",
                toPhoneNumber,
                _options.FromPhoneNumber);

            var messageResource = await MessageResource.CreateAsync(
                to: new PhoneNumber(toPhoneNumber),
                from: new PhoneNumber(_options.FromPhoneNumber),
                body: message);

            if (messageResource.ErrorCode.HasValue)
            {
                _logger.LogError(
                    "Failed to send SMS. Twilio error code: {ErrorCode}, message: {ErrorMessage}",
                    messageResource.ErrorCode,
                    messageResource.ErrorMessage);
                return false;
            }

            _logger.LogInformation(
                "SMS sent successfully. Message SID: {MessageSid}, Status: {Status}",
                messageResource.Sid,
                messageResource.Status);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception occurred while sending SMS to {PhoneNumber}",
                toPhoneNumber);
            return false;
        }
    }
}
