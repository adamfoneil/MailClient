using MailClientBase.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MailSender
{
    public abstract class MailClientBase<TOptions> where TOptions : OptionsBase
    {
        private readonly ILogger _logger;
        protected readonly TOptions _options;

        public MailClientBase(ILogger logger, IOptions<TOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        protected virtual async Task<(bool AllowReplies, string Recipient)> GetReplyToAsync(Message message) => await Task.FromResult((false, default(string)));

        protected virtual async Task<bool> FilterMessageAsync(Message message) => await Task.FromResult(false);

        protected virtual async Task LogMessageAsync(string messageId, Message message)
        {
            // for inspecting all outgoing content
            _logger.LogDebug("{@logData}", new
            {
                _options.SendMode,
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
            _logger.LogInformation("{@logData}", new
            {
                _options.SendMode,
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
            switch (_options.SendMode)
            {
                case SendModeOptions.LogOnly:
                    return false;

                case SendModeOptions.Filter:
                    return await FilterMessageAsync(message);
            }

            return true;
        }
    }
}
