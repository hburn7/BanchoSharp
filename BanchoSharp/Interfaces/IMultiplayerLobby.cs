using BanchoSharp.Multiplayer;

namespace BanchoSharp.Interfaces;

public interface IMultiplayerLobby
{
	/// <summary>
	///  The constant name of the multiplayer channel
	/// </summary>
	public string Channel { get; }
	/// <summary>
	///  The current name of the multiplayer lobby
	/// </summary>
	public string Name { get; }
	/// <summary>
	///  The link to the match history
	/// </summary>
	public string HistoryUrl { get; }
	/// <summary>
	///  The current size of the lobby
	/// </summary>
	public int Size { get; }
	public LobbyFormat Format { get; }
	public WinCondition WinCondition { get; }
	public GameMode GameMode { get; }
	public List<MultiplayerPlayer> Players { get; }
	public List<string> Referees { get; }

	public Task UpdateSettingsAsync(LobbyFormat? format = LobbyFormat.TeamVs,
		WinCondition? winCondition = WinCondition.ScoreV2, GameMode? gameMode = GameMode.osu);

	public Task AbortAsync();
	public Task AbortTimerAsync();
	public Task DisplaySettingsAsync();
	public Task SetSizeAsync(int newSize);
	public Task MoveAsync(string player, int slot);
	public Task RenameAsync(string newName);
	public Task InviteAsync(string username);
	public Task LockAsync();
	public Task UnlockAsync();
	public Task CloseAsync();
	public Task ClearHostAsync();
	public Task SetHostAsync(string username);
	public Task SetModsAsync(params string[] mods);
	public Task SetModsAsync(string mods);
	public Task SetTimerAsync(int seconds);
	public Task SetStartTimerAsync(int seconds);
	public Task StartAsync();
	public Task KickAsync(string username);
	public Task BanAsync(string username);
	public Task AddRefereeAsync(params string[] usernames);
	public Task RemoveRefereesAsync(params string[] usernames);
	public Task SetMapAsync(int id, GameMode? gameMode = null);
	public Task SendHelpMessageAsync();
	/// <summary>
	/// Updates this object's properties based on what is currently
	/// set in the multplayer lobby
	/// </summary>
	/// <returns></returns>
	public Task UpdateAsync();
}