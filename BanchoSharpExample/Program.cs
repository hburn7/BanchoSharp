using BanchoSharp;

var username = Environment.GetEnvironmentVariable("IRC_USERNAME");
var password = Environment.GetEnvironmentVariable("IRC_PASS");

if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
{
	Console.WriteLine("Please set IRC_USERNAME and IRC_PASS environment variables.");
	return;
}

var client = new BanchoClient(new BanchoClientConfig(new IrcCredentials(username, password), LogLevel.Trace));

client.OnAuthenticated += () =>
{
	client.BanchoBotEvents.OnTournamentLobbyCreated += lobby => Console.WriteLine($"Tournament lobby created: {lobby.Id}");
};

await client.ConnectAsync();