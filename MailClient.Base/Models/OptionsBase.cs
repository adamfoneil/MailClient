namespace MailClientBase.Models
{
    public enum SendModeOptions
    {
        /// <summary>
        /// don't send, just log
        /// </summary>
        LogOnly,
        /// <summary>
        /// allow sending only if MailClientBase.FilterMessageAsync returns true
        /// </summary>
        Filter,
        /// <summary>
        /// send all messages without filtering
        /// </summary>
        SendAll
    }

    public class OptionsBase
    {
        public SendModeOptions SendMode { get; set; }
    }
}
