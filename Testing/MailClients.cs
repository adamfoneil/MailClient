using MailClientBase.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testing.Clients;

namespace Testing
{
    [TestClass]
    public class MailClients
    {
        [TestMethod]
        public async Task MailgunSampleEmail()
        {
            using var httpClient = new HttpClient();

            var options = new Mailgun.Models.Options();
            Config.GetSection("Mailgun").Bind(options);

            var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger<MailgunSample>();

            var client = new MailgunSample(httpClient, logger, Options.Create(options));

            var messageId = await client.SendAsync(new Message()
            {
                Recipient = "adamosoftware@gmail.com",
                Subject = "Test Message",
                TextBody = "This is a test only.",
                HtmlBody = "<p>This is a test only.<p>"
            });

            Assert.IsTrue(!string.IsNullOrEmpty(messageId));
        }

        private IConfiguration Config => new ConfigurationBuilder()
            .AddUserSecrets("556d0196-4f14-4d25-af70-9fe942500f39")
            .Build();
    }
}