using EmailAbstractions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EmailAbstractions;

public abstract class MailClientBase<TOptions> : IEmailClient where TOptions : class
{
    protected readonly ILogger Logger;
    protected readonly TOptions Options;

    public MailClientBase(ILogger logger, IOptions<TOptions> options)
    {
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        Logger = logger;
        Options = options.Value;
    }

    protected virtual async Task<(bool AllowReplies, string Recipient)> GetReplyToAsync(Message message) => await Task.FromResult((false, default(string)));

    protected virtual async Task<bool> FilterMessageAsync(Message message) => await Task.FromResult(false);

    protected virtual async Task LogMessageAsync(string messageId, Message message)
    {
        // for inspecting all outgoing content
        Logger.LogDebug("{@logData}", new
        {
            messageId,
            message.Recipient,
            message.Subject,
            message.Metadata,
            Body = new
            {
                message.HtmlBody,
                message.TextBody
            }
        });

        // for inspecting just the top level info (sender, recipient, subject)
        Logger.LogInformation("{@logData}", new
        {
            messageId,
            message.Recipient,
            message.Subject,
            message.Metadata
        });

        await Task.CompletedTask;
    }

    protected abstract Task<string> SendImplementationAsync(Message message);

    public async Task<string> SendAsync(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);

        string messageId = await SendImplementationAsync(message);

        await LogMessageAsync(messageId, message);

        return messageId;
    }

    protected async Task<string> LogSendErrorAsync(HttpResponseMessage response, Message message)
    {
        var msgJson = JsonSerializer.Serialize(message, new JsonSerializerOptions()
        {
            WriteIndented = true
        });

        Logger.LogDebug("{@message}", msgJson);

        var errorMessage = await response.Content.ReadAsStringAsync();
        Logger.LogError("Email failed to send: {reason}", errorMessage);

        return errorMessage;
    }
}
