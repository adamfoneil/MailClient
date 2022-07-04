using MailClientBase.Models;
using MailSender;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Smtp2Go
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

            var response = await _httpClient.PostAsJsonAsync(_options.BaseUrl + "/email/send", send);  
            //var content = JsonContent.Create(send);
            //var response = await _httpClient.PostAsync(_options.BaseUrl + "/email/send", content, default);
            
            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                // adapted from https://apidoc.smtp2go.com/documentation/?_ga=2.43864229.1814788429.1656899456-2091240974.1656899456#/POST%20/email/send
                return doc?.RootElement.GetProperty("data").GetProperty("email_id").GetString() ?? throw new Exception("Smtp2GoClient API response did not include an expected email_id");
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
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public Dictionary<string, string>? CustomHeaders { get; set; }
            [JsonPropertyName("attachments")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
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