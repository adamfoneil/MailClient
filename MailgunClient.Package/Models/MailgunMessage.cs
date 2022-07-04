namespace MailgunClient.Package.Models
{
    public class MailgunMessage
    {
        public string? Recipient { get; set; }
        public string? Subject { get; set; }
        public string? TextBody { get; set; }
        public string? HtmlBody { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
