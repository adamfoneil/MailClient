using EmailAbstractions;
using EmailAbstractions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MailerSend;

public class MailerSendClient(IHttpClientFactory httpClientFactory, ILogger<MailerSendClient> logger, IOptions<MailerSendOptions> options) : MailClientBase<MailerSendOptions>(logger, options)
{
	private readonly IHttpClientFactory HttpClientFactory = httpClientFactory;

	protected override async Task<string> SendImplementationAsync(Message message) =>
		await SendInnerAsync("/email", "X-Message-Id", SendEmailRequest.FromMessage(message, Options.SenderEmail));

	public async Task<string> SendTextAsync(string toNumber, string content) =>
		await SendInnerAsync("/sms", "X-SMS-Message-Id", new SendTextRequest() { From = Options.SenderPhone, Text = content, To = [toNumber] });

	private async Task<string> SendInnerAsync<TRequest>(string endpoint, string headerId, TRequest request)
	{
		// prevent Too Many Requests
		await Task.Delay(Options.SendDelayMS);

		var client = HttpClientFactory.CreateClient();
		client.DefaultRequestHeaders.Clear();
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Options.ApiKey);

		var response = await client.PostAsync(
			   new Uri(Options.Url + endpoint),
			   JsonContent.Create(request, options: SerializerOptions));

		//var responseContent = await response.Content.ReadAsStringAsync();

		response.EnsureSuccessStatusCode();

		if (response.Headers.TryGetValues(headerId, out var values))
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
	internal class SendEmailRequest
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

		internal static SendEmailRequest FromMessage(Message message, string sender)
		{
			var result = new SendEmailRequest()
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

	internal class SendTextRequest
	{
		[JsonPropertyName("from")]
		public string From { get; set; } = default!;
		[JsonPropertyName("to")]
		public string[] To { get; set; } = [];
		[JsonPropertyName("text")]
		public required string Text { get; set; } = default!;
		/*
		[JsonPropertyName("persoonalization")]
		public PersonalizationInfo[] Personalization { get; set; } = [];

		public class PersonalizationInfo
		{
			[JsonPropertyName("phone_number")]
			public string PhoneNumber { get; set; } = default!;
			[JsonPropertyName("data")]
			public Dictionary<string, object> Data { get; set; } = [];
		}*/
	}

	internal class Recipient
	{
		[JsonPropertyName("email")]
		public string Email { get; set; } = default!;
		[JsonPropertyName("name")]
		public string Name { get; set; } = default!;
	}
}