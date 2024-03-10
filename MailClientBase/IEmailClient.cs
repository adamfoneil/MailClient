using EmailAbstractions.Models;

namespace EmailAbstractions;

public interface IEmailClient
{
    Task<string> SendAsync(Message message);
}