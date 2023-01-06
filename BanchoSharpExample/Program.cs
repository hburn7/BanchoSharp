using BanchoSharp;

var client = new BanchoClient(new BanchoClientConfig(new IrcCredentials(Environment.GetEnvironmentVariable("IRC_USERNAME"),
	Environment.GetEnvironmentVariable("IRC_PASS")), LogLevel.Trace));

client.OnAuthenticated += async () =>
{
	client.OnChannelJoined += async (channel) =>
	{
		Console.WriteLine("Hello world!");
	};
	
	client.BanchoBotEvents.OnTournamentLobbyCreated += lobby => Console.WriteLine($"Tournament lobby created: {lobby.Id}");
	
	await client.JoinChannelAsync("#osu");
	await client.QueryUserAsync("BanchoBot");
	// Do cool stuff here!
	await client.SendPrivateMessageAsync("BanchoBot", "!mp make test");
};

await client.ConnectAsync();