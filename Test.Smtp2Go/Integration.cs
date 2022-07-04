using MailClientBase.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Smtp2Go;

namespace Smtp2GoTest
{
    [TestClass]
    public class Integration
    {
        [TestMethod]
        public async Task Smtp2GoSampleEmail()
        {
            using var httpClient = new HttpClient(new LoggingHandler(new HttpClientHandler()));
            //using var httpClient = new HttpClient();

            var options = new Smtp2Go.Models.Options();
            Config.GetSection("Smtp2Go").Bind(options);

            var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger<Smtp2GoSample>();

            var client = new Smtp2GoSample(httpClient, logger, Options.Create(options));

            var messageId = await client.SendAsync(new Message()
            {
                Recipient = "adamosoftware@gmail.com",
                Subject = "Test from Smtp2Go",
                TextBody = "This is a test only",
                HtmlBody = "<p>This is a test only</p>"
            });

            Assert.IsTrue(!string.IsNullOrEmpty(messageId));
        }

        private IConfiguration Config => new ConfigurationBuilder()
            .AddUserSecrets("3fcdd101-0e8a-46dd-b499-8e4f40823c01")
            .Build();

        /// <summary>
        /// help from https://stackoverflow.com/a/18925296/2023653
        /// </summary>
        class LoggingHandler : DelegatingHandler
        {
            public LoggingHandler(HttpMessageHandler handler) : base(handler)
            {
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Console.WriteLine(request.ToString());
                if (request.Content != null)
                {
                    Console.WriteLine(await request.Content.ReadAsStringAsync());
                }

                return await base.SendAsync(request, cancellationToken);
            }
        }
    }
}