using BanchoSharp.Multiplayer;

namespace BanchoSharp.Interfaces;

public interface IMultiplayerPlayer
{
	int? Id { get; set; }
	string Name { get; }
	TeamColor Team { get; set; }
	int Slot { get; set; }
	Mods Mods { get; set; }
	int? Score { get; set; }
	bool? Passed { get; set; }
	bool IsReady { get; set; }
	IMultiplayerLobby Lobby { get; }
	bool Equals(object? other);
	bool Equals(MultiplayerPlayer other);
	int GetHashCode();
}