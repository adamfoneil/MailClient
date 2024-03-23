namespace MailerSend;

public class MailerSendOptions
{
    public string Url { get; set; } = default!;
    public string ApiKey { get; set; } = default!;
    public string SenderEmail { get; set; } = default!;
    public int SendDelayMS { get; set; } = 1000;
    public string SenderPhone { get; set; } = default!;
}
