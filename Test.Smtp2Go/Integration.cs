using MailClientBase.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Smtp2Go;
using System.Reflection;

namespace Smtp2GoTest
{
    [TestClass]
    public class Integration
    {
        private static IServiceProvider Services => new ServiceCollection()
            .AddHttpClient()
            .BuildServiceProvider();

        /// <summary>
        /// waiting for help from Smtp2Go ticket #211689 
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Smtp2GoSampleEmail()
        {
            //using var httpClient = new HttpClient(new LoggingHandler());
            var httpClient = Services.GetRequiredService<IHttpClientFactory>().CreateClient();

            var options = new Smtp2Go.Models.Options();
            Config.GetSection("Smtp2Go").Bind(options);

            var logger = LoggerFactory.Create(config => 
            { 
                config.AddDebug();
                config.SetMinimumLevel(LogLevel.Debug);
            }).CreateLogger<Smtp2GoSample>();

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

        /// <summary>
        /// The Smtp2Go API is returning an error
        /// E_ApiResponseCodes.NON_VALIDATING_IN_PAYLOAD", 
        /// "model_validation_errors": "Model Validation failed: Either 'text_body', 'html_body' or 'template_id' must be passed"
        /// This test is here to prove that the text
        /// </summary>
        [TestMethod]
        public void MessageContent()
        {
            var actualResult = Smtp2GoClient.SerializeMessage(new Message()
            {
                Recipient = "adamosoftware@gmail.com",
                Subject = "this new message",
                TextBody = "This is a test only",
                HtmlBody = "<p>This is a test only</p>"
            }, new Smtp2Go.Models.Options()
            {
                ApiKey = "whatever-key",
                Sender = "test@nowhere.org"
            }, true, "reply-to@nowhere.org");

            var expectedResult = GetResource("Resources.MessageJson.txt");
            Assert.IsTrue(actualResult.Equals(expectedResult));
        }

        private string GetResource(string resourceName)
        {
            //var names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Test.Smtp2Go.{resourceName}") ?? throw new Exception($"Resource {resourceName} not found");
            return new StreamReader(stream).ReadToEnd();
        }

        private IConfiguration Config => new ConfigurationBuilder()
            .AddUserSecrets("3fcdd101-0e8a-46dd-b499-8e4f40823c01")
            .Build();

        /// <summary>
        /// help from https://stackoverflow.com/a/18925296/2023653
        /// </summary>
        class LoggingHandler : DelegatingHandler
        {
            public LoggingHandler() : base(new HttpClientHandler())
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