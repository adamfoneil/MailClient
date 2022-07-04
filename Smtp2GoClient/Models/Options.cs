using MailClientBase.Models;

namespace Smtp2GoClient.Models
{
    public class Options : OptionsBase
    {
        public string? BaseUrl { get; set; } = "https://api.smtp2go.com/v3/";
        public string? ApiKey { get; set; }
    }
}
