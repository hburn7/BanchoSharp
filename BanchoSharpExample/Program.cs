using BanchoSharp;

var client = new BanchoClient(new BanchoClientConfig(new IrcCredentials(Environment.GetEnvironmentVariable("IRC_USERNAME"),
	Environment.GetEnvironmentVariable("IRC_PASS")), LogLevel.Trace));

client.OnAuthenticated += async () =>
{
	client.BanchoBotEvents.OnTournamentLobbyCreated += lobby => Console.WriteLine($"Tournament lobby created: {lobby.Id}");
};

await client.ConnectAsync();