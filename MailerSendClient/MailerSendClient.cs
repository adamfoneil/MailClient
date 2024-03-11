using EmailAbstractions;
using EmailAbstractions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MailerSend;

public class MailerSendClient(IHttpClientFactory httpClientFactory, ILogger<MailerSendClient> logger, IOptions<MailerSendOptions> options) : MailClientBase<MailerSendOptions>(logger, options)
{    
    private readonly IHttpClientFactory HttpClientFactory = httpClientFactory;

    protected override async Task<string> SendImplementationAsync(Message message)
    {
        // prevent Too Many Requests
        await Task.Delay(Options.SendDelayMS);

        var client = HttpClientFactory.CreateClient();

        var request = SendRequest.FromMessage(message, Options.SenderEmail);

        var json = JsonSerializer.Serialize(request, SerializerOptions);

        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Options.ApiKey);

        var response = await client.PostAsync(
               new Uri(Options.Url + "/email"),
               new StringContent(json, Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();

        if (response.Headers.TryGetValues("X-Message-Id", out var values))
        {
            return values.First();
        }

        // sometimes the msgId is not returned even though the message sent
        return $"fake:{Guid.NewGuid()}";
    }

    private static JsonSerializerOptions SerializerOptions => new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // to allow inline html in HtmlBody
        WriteIndented = true
    };

    /// <summary>
    /// based on https://developers.mailersend.com/api/v1/email.html#send-an-email
    /// </summary>
    internal class SendRequest
    {
        [JsonPropertyName("from")]
        public Recipient From { get; set; } = new();
        [JsonPropertyName("to")]
        public Recipient[] To { get; set; } = [];
        [JsonPropertyName("reply_to")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Recipient? ReplyTo { get; set; }
        [JsonPropertyName("subject")]
        public string Subject { get; set; } = default!;
        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; set; } = default!;
        [JsonPropertyName("html")]
        public string? Html { get; set; } = default!;

        internal static SendRequest FromMessage(Message message, string sender)
        {
            var result = new SendRequest()
            {
                From = new() { Email = sender, Name = sender },
                To =
                [
                    new()
                    {
                        Email = message.Recipient ?? throw new ArgumentNullException(nameof(Recipient)),
                        Name = message.Recipient
                    }
                ],
                Subject = message.Subject ?? throw new ArgumentNullException(nameof(Subject)),
                Html = message.HtmlBody,
                Text = message.TextBody
            };

            if (message.ReplyTo != null) result.ReplyTo = new() { Email = message.ReplyTo };

            return result;
        }
    }

    internal class Recipient
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = default!;
        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;
    }
}