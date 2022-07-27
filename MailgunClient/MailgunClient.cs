using MailClientBase.Models;
using MailSender;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Mailgun
{
    public class MailgunClient : MailClientBase<Models.Options>
    {
        private readonly HttpClient _httpClient;

        public MailgunClient(IHttpClientFactory httpClientFactory, ILogger<MailgunClient> logger, IOptions<Models.Options> options) : base(logger, options)
        {            
            _httpClient = httpClientFactory.CreateClient();
            var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{Options.ApiKey}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encoded);
        }

        protected override async Task<string> SendImplementationAsync(Message message)
        {
            ArgumentNullException.ThrowIfNull(message, nameof(message));

            var body = new Dictionary<string, string>()
            {
                ["from"] = Options.SenderName,
                ["to"] = message.Recipient,
                ["subject"] = message.Subject,
                ["text"] = message.TextBody,
                ["html"] = message.HtmlBody
            };

            var replyTo = await GetReplyToAsync(message);
            if (replyTo.AllowReplies)
            {
                body.Add("h:reply-to", replyTo.Recipient);
            }

            // help from https://github.com/lukencode/FluentEmail/blob/master/src/Senders/FluentEmail.Mailgun/HttpHelpers/HttpClientHelpers.cs#L26
            var content = new MultipartFormDataContent();
            foreach (var kp in body.Where(kp => kp.Value is not null)) content.Add(new StringContent(kp.Value), kp.Key);

            var url = $"{Options.Url}/{Options.Domain}/messages";
            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                return doc?.RootElement.GetProperty("id").GetString() ?? throw new Exception("Mailgun result did not include an expected message Id");
            }

            var errorMessage = await LogSendErrorAsync(response, message);
            throw new Exception(errorMessage);
        }
    }
}
