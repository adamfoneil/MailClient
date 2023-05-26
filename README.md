This came from a need to send emails with different providers -- [Mailgun](https://www.mailgun.com/) and [Smtp2Go](https://www.smtp2go.com/) -- leveraging some shared low-level aspects. I know there are more mature libraries out there like [FluentMail](https://github.com/lukencode/FluentEmail). As usual, I like building and thinking through stuff. I did actually get a little help from the FluentMail code base [here](https://github.com/adamfoneil/MailClient/blob/master/MailgunClient/MailgunClient.cs#L41) for Mailgun.

I started with a base abstract class [MailClientBase](https://github.com/adamfoneil/MailClient/blob/master/MailClientBase/MailClientBase.cs), then added implementations for 

- [Mailgun](https://github.com/adamfoneil/MailClient/blob/master/MailgunClient/MailgunClient.cs)
- [Smtp2Go](https://github.com/adamfoneil/MailClient/blob/master/Smtp2GoClient/Smtp2GoClient.cs)

I offer these as NuGet packages:
- [AO.Mailgun](https://www.nuget.org/packages/AO.Mailgun)
- [AO.Smtp2Go](https://www.nuget.org/packages/AO.Smtp2Go)

## In a Nutshell
All mail clients inherit from [MailClientBase\<TOptions\>](https://github.com/adamfoneil/MailClient/blob/master/MailClientBase/MailClientBase.cs#L7). Use `TOptions` to define the settings required for that client. Examples: [Mailgun Options](https://github.com/adamfoneil/MailClient/blob/master/MailgunClient/Models/Options.cs), [Smtp2Go Options](https://github.com/adamfoneil/MailClient/blob/master/Smtp2GoClient/Models/Options.cs).

For the two email providers in this repo I'm implementing, I overrode the abstract method [SendImplementationAsync](https://github.com/adamfoneil/MailClient/blob/master/MailClientBase/MailClientBase.cs#L54).

## Html Email
Html email rendering is a related but separate concern from this. I have a different project [RazorToString](https://github.com/adamfoneil/RazorToString) that addresses this specifically.
