using EmailAbstractions.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Smtp2Go;

namespace Smtp2GoTest
{
    public class Smtp2GoSample : Smtp2GoClient
    {
        public Smtp2GoSample(IHttpClientFactory httpClientFactory, ILogger<Smtp2GoSample> logger, IOptions<Smtp2Go.Models.Options> options) : base(httpClientFactory, logger, options)
        {
        }

        protected override async Task<bool> FilterMessageAsync(Message message)
        {
            var result = message.Recipient?.Equals("adamosoftware@gmail.com") ?? throw new Exception("Recipient is required");
            return await Task.FromResult(result);
        }
    }
}
