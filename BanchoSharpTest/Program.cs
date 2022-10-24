// See https://aka.ms/new-console-template for more information

using BanchoSharp;

var client = new BanchoClient(new BanchoClientConfig(new IrcCredentials("Stage",
	Environment.GetEnvironmentVariable("IRC_PASS")), LogLevel.Debug));

client.OnAuthenticated += async () =>
{
	await client.JoinChannelAsync("#osu");
	await client.QueryUserAsync("BanchoBot");
};

await client.ConnectAsync();
