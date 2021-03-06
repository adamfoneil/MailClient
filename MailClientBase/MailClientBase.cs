using MailClientBase.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MailSender
{
    public abstract class MailClientBase<TOptions> where TOptions : OptionsBase
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
                Options.SendMode,
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
                Options.SendMode,
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

            string messageId = string.Empty;

            if (await ShouldSendAsync(message))
            {
                messageId = await SendImplementationAsync(message);
            }

            await LogMessageAsync(messageId, message);

            return messageId;
        }

        private async Task<bool> ShouldSendAsync(Message message)
        {
            switch (Options.SendMode)
            {
                case SendModeOptions.LogOnly:
                    return false;

                case SendModeOptions.Filter:
                    return await FilterMessageAsync(message);
            }

            return true;
        }

        protected async Task<string> LogSendErrorAsync(HttpResponseMessage response, Message message)
        {
            var msgJson = JsonSerializer.Serialize(message, new JsonSerializerOptions()
            {
                WriteIndented = true
            });

            Logger.LogDebug("{message}", msgJson);

            var errorMessage = "Email failed to send: " + (await response.Content.ReadAsStringAsync());
            Logger.LogError(errorMessage);

            return errorMessage;
        }
    }
}
