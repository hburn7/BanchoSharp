using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;

namespace BanchoSharp.Messaging;

public class BanchoBotChecks
{
	private readonly IBanchoClient _client;

	public BanchoBotChecks(IBanchoClient client) { _client = client; }
	
	public IMultiplayerLobby? IsTournamentCreation(IPrivateIrcMessage msg)
	{
		if (!msg.IsBanchoBotMessage)
		{
			return null;
		}

		if (!msg.Content.Contains("Created the tournament match", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		string[] splits = msg.Content.Split(" ");
		string url = splits[4];
		string name = splits[5];
		int id = int.Parse(url.Split("/").Last());
		string channel = $"#mp_{id}";
		return new MultiplayerLobby(_client, channel, name);
	}
}