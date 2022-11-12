using BanchoSharp;

var client = new BanchoClient(new BanchoClientConfig(new IrcCredentials(Environment.GetEnvironmentVariable("IRC_USERNAME"),
	Environment.GetEnvironmentVariable("IRC_PASS")), LogLevel.Trace));

client.OnAuthenticated += async () =>
{
	await client.JoinChannelAsync("#osu");
	await client.QueryUserAsync("BanchoBot");
	
	// Do cool stuff here!
};

await client.ConnectAsync();