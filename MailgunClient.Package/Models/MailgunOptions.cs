namespace MailgunClient.Package.Models
{
    public enum FilterModeOptions
    {
        LogOnly,
        FilterRecipients,
        SendAll
    }

    public class MailgunOptions
    {
        public string? Url { get; set; }
        public string? Domain { get; set; }
        public string? SenderName { get; set; }
        public string? ApiKey { get; set; }        
        public bool IsLive { get; set; }
    }
}