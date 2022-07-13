This came from a need to send emails with different providers -- [Mailgun](https://www.mailgun.com/) and [Smtp2Go](https://www.smtp2go.com/) -- leveraging some shared  functionality for logging and environment-specific behavior (different behavior for QA vs prod, for example).

I started with a base abstract class [MailClientBase](https://github.com/adamfoneil/MailClient/blob/master/MailClientBase/MailClientBase.cs), then added implementations for 

- [Mailgun](https://github.com/adamfoneil/MailClient/blob/master/MailgunClient/MailgunClient.cs)
- [Smtp2Go](https://github.com/adamfoneil/MailClient/blob/master/Smtp2GoClient/Smtp2GoClient.cs)

I offer these as NuGet packages:
- [AO.Mailgun](https://www.nuget.org/packages/AO.Mailgun)
- [AO.Smtp2Go](https://www.nuget.org/packages/AO.Smtp2Go)

## In a Nutshell
All mail clients inherit from [MailClientBase\<TOptions\>](https://github.com/adamfoneil/MailClient/blob/master/MailClientBase/MailClientBase.cs#L7). Use `TOptions` to define the settings required for that client. Examples: [Mailgun Options](https://github.com/adamfoneil/MailClient/blob/master/MailgunClient/Models/Options.cs), [Smtp2Go Options](https://github.com/adamfoneil/MailClient/blob/master/Smtp2GoClient/Models/Options.cs).

For the two email providers in this repo I'm implementing, I overrode the abstract method [SendImplementationAsync](https://github.com/adamfoneil/MailClient/blob/master/MailClientBase/MailClientBase.cs#L52).

If you want to take advantage of some optional functionality, you'll need to create your own subclass of `MailgunClient` or `Smtp2GoClient`. These are the optional overrides:
- If you want to allow replies to your messages, override [GetReplyToAsync](https://github.com/adamfoneil/MailClient/blob/master/MailClientBase/MailClientBase.cs#L18). By default, replies are not allowed.
- If you want logging behavior specific to your application, override [LogMessageAsync](https://github.com/adamfoneil/MailClient/blob/master/MailClientBase/MailClientBase.cs#L22). Both Mailgun and Smtp2Go log send activity automatically, but you may want to capture outgoing messages in a database table, for example. That's what this is for.

## Safe Local and QA Testing
A common requirement with email is you want to make sure QA and local dev environments don't send to production recipients. One reason for this library is that I wanted a definitive solution for this that works with any email provider. I approached this by having a common configuration property [OptionsBase.SendMode](https://github.com/adamfoneil/MailClient/blob/master/MailClientBase/Models/OptionsBase.cs#L21). All mail client implementations must base their `TOptions` class on `OptionsBase`, so they will inherit the `SendMode` property.

`SendMode` has 3 options: 
- `LogOnly` disables all sending, suitable for local dev testing. In this scenario, you'd override `LogMessageAsync` to capture what *would* be sent in production. This can still be useful in production (not merely for development) if you want to capture outgoing emails in a database table, for example.
- `Filter` allows conditional sending. You would override [MailClientBase.FilterMessageAsync](https://github.com/adamfoneil/MailClient/blob/master/MailClientBase/MailClientBase.cs#L21) to control whether a message sends. You could use this to check for a certain domain, block or allow specific recipients, and so on. Suitable for QA environments or local dev testing. By default, all messages are rejected, so you'll need to override this if you use the `Filter` send mode.
- `SendAll` sends everything, intended as the production setting

When you send a message, the `SendMode` is checked within method [ShouldSendAsync](https://github.com/adamfoneil/MailClient/blob/master/MailClientBase/MailClientBase.cs#L71).

## Html Email
Html email rendering is a related but separate concern from this. I have a different project [RazorToString](https://github.com/adamfoneil/RazorToString) that addresses this specifically.
