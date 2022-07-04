using MailgunClient.Package.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MailgunClient.Package
{
    public class MailgunClient
    {
        private readonly MailgunOptions _options;
        private readonly HttpClient _httpClient;
        private readonly ILogger<MailgunClient> _logger;

        public MailgunClient(HttpClient httpClient, IOptions<MailgunOptions> options, ILogger<MailgunClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        protected virtual async Task<(bool AllowReplies, string Recipient)> GetReplyToAsync() => await Task.FromResult((false, default(string)));

        protected virtual async Task LogMessageAsync(bool isLive, string messageId, MailgunMessage message)
        {            
            // for inspecting all outgoing content
            _logger.LogDebug("{@logData}", new {
                isLive,
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
                isLive,
                messageId,
                message.Recipient,
                message.Subject,
                message.Metadata
            });

            await Task.CompletedTask;
        }

        public async Task<string> SendAsync(MailgunMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);

            string messageId = string.Empty;

            if (_options.IsLive)
            {                
                var body = new Dictionary<string, string>()
                {
                    ["from"] = _options.SenderName,                    
                    ["to"] = message.Recipient,
                    ["subject"] = message.Subject,
                    ["text"] = message.TextBody,
                    ["html"] = message.HtmlBody
                };

                var replyTo = await GetReplyToAsync();
                if (replyTo.AllowReplies)
                {
                    body.Add("h:reply-to", replyTo.Recipient);
                }

                // help from https://github.com/lukencode/FluentEmail/blob/master/src/Senders/FluentEmail.Mailgun/HttpHelpers/HttpClientHelpers.cs#L26
                var content = new MultipartFormDataContent();
                foreach (var kp in body.Where(kp => kp.Value is not null)) content.Add(new StringContent(kp.Value), kp.Key);

                var url = $"{_options.Url}/{_options.Domain}/messages";
                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
                var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                messageId = doc?.RootElement.GetProperty("id").GetString() ?? throw new Exception("Mailgun result did not include an expected message Id");
            }

            await LogMessageAsync(_options.IsLive, messageId, message);

            return messageId;
        }
    }
}
