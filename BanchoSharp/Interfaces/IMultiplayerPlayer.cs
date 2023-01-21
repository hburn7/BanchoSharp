using BanchoSharp.Multiplayer;

namespace BanchoSharp.Interfaces;

public interface IMultiplayerPlayer
{
	public int? Id { get; set; }
	public string Name { get; }
	public TeamColor Team { get; set; }
	public int Slot { get; set; }
	public Mods Mods { get; set; }
	public int? Score { get; set; }
	public bool? Passed { get; set; }
	public bool IsReady { get; set; }
	public IMultiplayerLobby? Lobby { get; set; }

	/// <summary>
	///  This is used for targeting a <see cref="IMultiplayerPlayer" /> in the chat room
	/// </summary>
	/// <returns>#ID or NAME of player</returns>
	public string TargetableName();

	public bool Equals(object? other);
	public bool Equals(MultiplayerPlayer other);
	public int GetHashCode();
}