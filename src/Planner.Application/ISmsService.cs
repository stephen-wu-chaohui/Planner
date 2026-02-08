namespace Planner.Application;

/// <summary>
/// Service for sending SMS messages to users.
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Sends an SMS message asynchronously to the specified phone number.
    /// </summary>
    /// <param name="toPhoneNumber">The recipient's phone number in E.164 format (e.g., +1234567890).</param>
    /// <param name="message">The message body to send.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A task that represents the asynchronous operation. Returns true if message was sent successfully, false otherwise.</returns>
    Task<bool> SendMessageAsync(string toPhoneNumber, string message, CancellationToken cancellationToken = default);
}
