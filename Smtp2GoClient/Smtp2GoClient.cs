using MailClientBase.Models;
using MailSender;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Smtp2Go
{
    public class Smtp2GoClient(IHttpClientFactory httpClientFactory, ILogger<Smtp2GoClient> logger, IOptions<Models.Options> options) : MailClientBase<Models.Options>(logger, options)
    {
        private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

        protected override async Task<string> SendImplementationAsync(Message message)
        {
            var (allowReplies, recipient) = await GetReplyToAsync(message);
            var env = BuildEnvelope(message, Options, allowReplies, recipient);

            // this sets the content-length to a value Smtp2Go doesn't like
            //var response = await _httpClient.PostAsJsonAsync(_options.BaseUrl + "/email/send", env, SerializerOptions);

            var json = JsonSerializer.Serialize(env, SerializerOptions);
            
            var response = await _httpClient.PostAsync(
                new Uri(Options.BaseUrl + "/email/send"), 
                new StringContent(json, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                // adapted from https://apidoc.smtp2go.com/documentation/?_ga=2.43864229.1814788429.1656899456-2091240974.1656899456#/POST%20/email/send
                return doc?.RootElement.GetProperty("data").GetProperty("email_id").GetString() ?? throw new Exception("Smtp2GoClient API response did not include an expected email_id");
            }

            var errorMessage = await LogSendErrorAsync(response, message);
            throw new Exception(errorMessage);
        }

        /// <summary>
        /// extracting this method for test purposes to deal with 
        /// inexplicable model validation error coming from Smtp2Go        
        /// </summary>
        public static string SerializeMessage(Message message, Models.Options options, bool allowReplies, string recipient)
        {
            var envelope = BuildEnvelope(message, options, allowReplies, recipient);
            
            return JsonSerializer.Serialize(envelope, options: SerializerOptions);
        }

        private static Envelope BuildEnvelope(Message message, Models.Options options, bool allowReplies, string recipient)
        {
            var envelope = new Envelope()
            {
                ApiKey = options.ApiKey,
                Recipients = [new Recipient() { Email = message.Recipient }],
                Subject = message.Subject,
                Sender = options.Sender,
                TextBody = message.TextBody,
                HtmlBody = message.HtmlBody
            };

            if (allowReplies)
            {
                envelope.CustomHeaders.Add("Reply-To", recipient);
            }

            return envelope;
        }

        private static JsonSerializerOptions SerializerOptions => new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // to allow inline html in HtmlBody
            WriteIndented = true
        };

        private class Envelope
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
            [JsonConverter(typeof(HeaderConverter))]
            public Dictionary<string, string> CustomHeaders { get; set; } = new Dictionary<string, string>();
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