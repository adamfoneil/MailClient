using EmailAbstractions.Models;
using MailerSend;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MailerSendTest
{
	[TestClass]
	public class Integration
	{
		private static IServiceProvider Services => new ServiceCollection()
			.AddHttpClient()
			.BuildServiceProvider();

		private static IConfiguration Config => new ConfigurationBuilder()
			.AddUserSecrets("a1d1d363-1ca5-40ec-a5c2-02258217882b")
			.Build();

		[TestMethod]
		public async Task Send()
		{
			var httpClientFactory = Services.GetRequiredService<IHttpClientFactory>();

			var options = new MailerSendOptions();
            Config.GetSection("MailerSend").Bind(options);

			var logger = LoggerFactory.Create(config =>
			{
				//config.AddConsole();
				config.SetMinimumLevel(LogLevel.Debug);
			}).CreateLogger<MailerSendClient>();

			var client = new MailerSendClient(httpClientFactory, logger, Options.Create(options));

			var messageId = await client.SendAsync(new Message()
			{
				Recipient = "adamosoftware@gmail.com",
				Subject = "Test from MailerSend",
				TextBody = "This is a test only",
				HtmlBody = "<p>This is a test only</p>"
			});

			Assert.IsTrue(!string.IsNullOrEmpty(messageId));
		}
	}
}