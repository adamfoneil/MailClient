This came from a need to send emails with different providers -- [Mailgun](https://www.mailgun.com/) and [Smtp2Go](https://www.smtp2go.com/) -- leveraging some shared  functionality for logging and environment-specific behavior (different behavior for QA vs prod, for example).

I started with a base abstract class [MailClientBase](https://github.com/adamfoneil/MailClient/blob/master/MailClient.Base/MailClientBase.cs), then added implementations for 

- [Mailgun](https://github.com/adamfoneil/MailClient/blob/master/MailgunClient/MailgunClient.cs)
- [Smtp2Go](https://github.com/adamfoneil/MailClient/blob/master/Smtp2GoClient/Smtp2GoClient.cs)

These will be offered as NuGet packages when I have the basics all working.
