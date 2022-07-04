using MailClientBase.Models;
using MailSender;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Smtp2GoClient
{
    public class Smtp2GoClient : MailClientBase<Models.Options>
    {
        private readonly HttpClient _httpClient;

        public Smtp2GoClient(HttpClient httpClient, ILogger<Smtp2GoClient> logger, IOptions<Models.Options> options) : base(logger, options)
        {
            _httpClient = httpClient;
        }

        protected override async Task<string> SendImplementationAsync(Message message)
        {
            var replyTo = await GetReplyToAsync(message);

            var send = new SendModel()
            {
                ApiKey = _options.ApiKey,
                Recipients = new[] { new Recipient() { Email = message.Recipient } },
                Subject = message.Subject,
                Sender = (replyTo.AllowReplies) ? replyTo.Recipient : _options.Sender,
                TextBody = message.TextBody,
                HtmlBody = message.HtmlBody
            };

            var response = await _httpClient.PostAsJsonAsync(_options.BaseUrl, send);
            
            if (response.IsSuccessStatusCode)
            {

            }

            var errorMessage = await LogSendErrorAsync(response, message);
            throw new Exception(errorMessage);
        }

        private class SendModel
        {
            [JsonPropertyName("api_key")]
            public string? ApiKey { get; set; }
            [JsonIgnore]
            public IEnumerable<Recipient>? Recipients { get; set; }
            [JsonPropertyName("to")]
            public string?[] SendTo => Recipients?.Select(r => r.ToString()).ToArray() ?? throw new Exception("Must have at least one recipient");
            [JsonPropertyName("sender")]
            public string? Sender { get; set; }
            [JsonPropertyName("subject")]
            public string? Subject { get; set; }
            [JsonPropertyName("text_body")]
            public string? TextBody { get; set; }
            [JsonPropertyName("html_body")]
            public string? HtmlBody { get; set; }
            [JsonPropertyName("custom_headers")]
            public Dictionary<string, string>? CustomHeaders { get; set; }
            [JsonPropertyName("attachments")]
            public IEnumerable<Attachment>? Attachments { get; set; }
        }

        private class Recipient
        {
            public string? Email { get; set; }
            public string? Name { get; set; }
            public override string? ToString() => string.IsNullOrEmpty(Name) ? Email : $"{Name} <{Email}>";            
        }

        private class Attachment
        {
            [JsonPropertyName("filename")]
            public string? Filename { get; set; }
            [JsonPropertyName("fileblob")]
            public string? Data { get; set; }
            [JsonPropertyName("mimetype")]
            public string? MimeType { get; set; }
        }
    }
}