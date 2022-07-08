using MailClientBase.Models;
using MailSender;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
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
            var (allowReplies, recipient) = await GetReplyToAsync(message);
            var env = BuildEnvelope2(message, _options, allowReplies, recipient);

            var response = await _httpClient.PostAsJsonAsync(_options.BaseUrl + "/email/send", env, SerializerOptions);

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

        private static Envelope2 BuildEnvelope2(Message message, Models.Options options, bool allowReplies, string recipient)
        {
            var result = new Envelope2()
            {
                api_key = options.ApiKey,
                sender = options.Sender,
                to = new string[] { message.Recipient },
                subject = message.Subject,
                html_body = message.HtmlBody,
                text_body = message.TextBody
            };

            if (allowReplies)
            {
                result.custom_headers = new[]
                {
                    new Envelope2.CustomHeader() { header = "Reply-To", value = recipient }
                };                
            }

            return result;
        }

        private static Envelope BuildEnvelope(Message message, Models.Options options, bool allowReplies, string recipient)
        {
            var envelope = new Envelope()
            {
                ApiKey = options.ApiKey,
                Recipients = new[] { new Recipient() { Email = message.Recipient } },
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

        /// <summary>
        /// trying to see if there's a problem with [JsonProperyName] attributes
        /// being ignore or something, but I'm still having the same behavior        
        /// </summary>
        private class Envelope2
        {
            public string api_key { get; set; }
            public string[] to { get; set; }
            public string sender { get; set; }
            public string subject { get; set; }
            public string text_body { get; set; }
            public string html_body { get; set; }
            public IEnumerable<CustomHeader> custom_headers { get; set; }

            public class CustomHeader
            {
                public string header { get; set; }
                public string value { get; set; }
            }
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