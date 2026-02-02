# Planner.Infrastructure.TwilioSmsService

This project provides an SMS notification service implementation using Twilio for the Planner application.

## Features

- **ISmsService Interface**: Defined in the Application layer for decoupling
- **Twilio Integration**: Implements SMS sending via Twilio REST API
- **Configuration-based**: Uses .NET Options pattern for flexible configuration
- **Error Handling**: Comprehensive logging and error handling for message delivery
- **Dependency Injection**: Easy integration with ASP.NET Core DI container

## Configuration

Add the following configuration to your `appsettings.json` or environment variables:

```json
{
  "TwilioSms": {
    "AccountSid": "your-twilio-account-sid",
    "AuthToken": "your-twilio-auth-token",
    "FromPhoneNumber": "+1234567890"
  }
}
```

### Environment Variables

For Docker or production deployments, use environment variables:

```bash
TwilioSms__AccountSid=your-twilio-account-sid
TwilioSms__AuthToken=your-twilio-auth-token
TwilioSms__FromPhoneNumber=+1234567890
```

### Docker Compose

The service is pre-configured in `docker-compose.yml` to use environment variables:

```yaml
environment:
  - TwilioSms__AccountSid=${TWILIO_ACCOUNT_SID:-}
  - TwilioSms__AuthToken=${TWILIO_AUTH_TOKEN:-}
  - TwilioSms__FromPhoneNumber=${TWILIO_FROM_PHONE_NUMBER:-}
```

Set these in your shell before running docker-compose:

```bash
export TWILIO_ACCOUNT_SID=your-twilio-account-sid
export TWILIO_AUTH_TOKEN=your-twilio-auth-token
export TWILIO_FROM_PHONE_NUMBER=+1234567890
docker-compose up
```

## Usage

### Dependency Injection Registration

The service is automatically registered in `Program.cs`:

```csharp
builder.Services.AddTwilioSmsService(builder.Configuration);
```

### Using the Service

Inject `ISmsService` into your controllers or services:

```csharp
public class NotificationController : ControllerBase
{
    private readonly ISmsService _smsService;

    public NotificationController(ISmsService smsService)
    {
        _smsService = smsService;
    }

    [HttpPost("notify")]
    public async Task<IActionResult> NotifyUser(string phoneNumber, string message)
    {
        var success = await _smsService.SendMessageAsync(phoneNumber, message);
        
        if (success)
        {
            return Ok("Message sent successfully");
        }
        
        return StatusCode(500, "Failed to send message");
    }
}
```

### Example: Optimization Complete Notification

```csharp
public class OptimizationService
{
    private readonly ISmsService _smsService;

    public async Task NotifyOptimizationComplete(string userPhone, string jobId)
    {
        var message = $"Your route optimization job {jobId} is complete. " +
                     $"Results are now available in your dashboard.";
        
        await _smsService.SendMessageAsync(userPhone, message);
    }
}
```

## Phone Number Format

Phone numbers must be in E.164 format:
- Include country code
- No spaces, dashes, or parentheses
- Example: `+12345678901` (US number)

## Error Handling

The service returns `false` if:
- Phone number is invalid or empty
- Message body is empty
- Twilio credentials are not configured
- Twilio API returns an error
- Any exception occurs during sending

All errors are logged using `ILogger<TwilioSmsService>` for troubleshooting.

## Testing

Unit tests are available in `Planner.Infrastructure.TwilioSmsService.Tests`:

```bash
dotnet test test/Planner.Infrastructure.TwilioSmsService.Tests
```

## Security Considerations

- **Never commit credentials**: Always use environment variables or secure configuration providers
- **Secrets Management**: Use Azure Key Vault, AWS Secrets Manager, or similar for production
- **Rate Limiting**: Be aware of Twilio's rate limits and implement appropriate throttling
- **Cost Management**: Monitor SMS usage to avoid unexpected charges

## Dependencies

- `Twilio` (v7.8.3): Official Twilio SDK
- `Microsoft.Extensions.Options`: .NET Options pattern support
- `Microsoft.Extensions.Logging`: Logging abstractions

## Support

For Twilio-specific issues, consult the [Twilio Documentation](https://www.twilio.com/docs).

For application integration questions, refer to the main Planner repository documentation.
