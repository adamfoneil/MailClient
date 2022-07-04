using MailClientBase.Models;

namespace Mailgun.Models
{
    public class Options : OptionsBase
    {
        public string? Url { get; set; }
        public string? Domain { get; set; }
        public string? SenderName { get; set; }
        public string? ApiKey { get; set; }                
    }
}