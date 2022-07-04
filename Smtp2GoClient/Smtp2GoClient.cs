using MailClientBase.Models;
using MailSender;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Smtp2GoClient
{
    public class Smtp2GoClient : MailClientBase<Models.Options>
    {
        private readonly HttpClient _httpClient;

        public Smtp2GoClient(HttpClient httpClient, ILogger<Smtp2GoClient> logger, IOptions<Models.Options> options) : base(logger, options)
        {
            _httpClient = httpClient;
        }

        protected override Task<string> SendImplementationAsync(Message message)
        {
            throw new NotImplementedException();
        }
    }
}