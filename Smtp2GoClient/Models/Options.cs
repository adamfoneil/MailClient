namespace Smtp2Go.Models
{
    public class Options
    {
        public string BaseUrl { get; set; } = "https://api.smtp2go.com/v3";
        public string? ApiKey { get; set; }
        public string? Sender { get; set; }
    }
}
