namespace Planner.Infrastructure.TwilioSmsService;

/// <summary>
/// Configuration options for Twilio SMS service.
/// </summary>
public sealed class TwilioSmsOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "TwilioSms";

    /// <summary>
    /// Twilio Account SID.
    /// </summary>
    public string AccountSid { get; set; } = string.Empty;

    /// <summary>
    /// Twilio Auth Token.
    /// </summary>
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>
    /// Twilio phone number to send messages from (in E.164 format).
    /// </summary>
    public string FromPhoneNumber { get; set; } = string.Empty;
}
