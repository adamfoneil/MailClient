using EmailAbstractions.Models;
using Mailgun;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MailgunTest
{
    public class MailgunSample : MailgunClient
    {
        public MailgunSample(IHttpClientFactory httpClientFactory, ILogger<MailgunClient> logger, IOptions<Mailgun.Models.Options> options) : base(httpClientFactory, logger, options)
        {
        }

        protected override async Task<bool> FilterMessageAsync(Message message)
        {
            var result = message.Recipient?.Equals("adamosoftware@gmail.com") ?? throw new Exception("Recipient is required");
            return await Task.FromResult(result);
        }
    }
}
