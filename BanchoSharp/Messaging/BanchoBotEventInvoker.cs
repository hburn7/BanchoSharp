using BanchoSharp.Interfaces;
using BanchoSharp.Multiplayer;

namespace BanchoSharp.Messaging;

public class BanchoBotEventInvoker : IBanchoBotEventInvoker, IBanchoBotEvents
{
	private readonly IBanchoClient _client;

	public BanchoBotEventInvoker(IBanchoClient client)
	{
		_client = client;

		OnTournamentLobbyCreated += lobby => Logger.Info($"Joined tournament lobby: {lobby}");
	}

	public void ProcessMessage(IPrivateIrcMessage msg)
	{
		if (!msg.IsBanchoBotMessage)
		{
			return;
		}

		string content = msg.Content;

		if (IsMultiplayerLobbyCreation(content))
		{
			string lobbyId = content.Split("https://osu.ppy.sh/mp/")[1].Split()[0];
			string name = content.Split(lobbyId)[1].Trim();

			if (!long.TryParse(lobbyId, out long id))
			{
				Logger.Error($"Failed to parse lobby id {id}");
				return;
			}
			
			var mp = new MultiplayerLobby(_client, long.Parse(lobbyId), name);
			_client.Channels.Add(mp);
			OnTournamentLobbyCreated?.Invoke(mp);
		}
	}

	private bool IsMultiplayerLobbyCreation(string content) => content.StartsWith("Created the tournament match");
	public event Action<IMultiplayerLobby>? OnTournamentLobbyCreated;
}