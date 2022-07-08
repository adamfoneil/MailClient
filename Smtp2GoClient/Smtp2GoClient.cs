﻿using MailClientBase.Models;
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
            var (allowReplies, recipient) = await GetReplyToAsync(message);
            var json = SerializeMessage(message, _options, allowReplies, recipient);

            var response = await _httpClient.SendAsync(new HttpRequestMessage()
            {
                RequestUri = new Uri(_options.BaseUrl + "/email/send"),
                Method = HttpMethod.Post,
                Content = JsonContent.Create(json)
            });

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
        /// spurious model validation error coming from Smtp2Go        
        /// </summary>
        public static string SerializeMessage(Message message, Models.Options options, bool allowReplies, string recipient)
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

            return JsonSerializer.Serialize(envelope, new JsonSerializerOptions()
            {
                WriteIndented = true
            });
        }

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