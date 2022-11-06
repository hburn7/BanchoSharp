namespace BanchoSharp.Interfaces;

/// <summary>
/// Class that contains BanchoBot-specific events
/// </summary>
public interface IBanchoBotEvents
{
	/// <summary>
	/// Fired whenever a tournament lobby is created via !mp make
	/// </summary>
	public event Action<IMultiplayerLobby> OnTournamentLobbyCreated;
}