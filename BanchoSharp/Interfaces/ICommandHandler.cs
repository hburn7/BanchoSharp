namespace BanchoSharp.Interfaces;

/// <summary>
/// Interface responsible for handling user-provided slash commands. This is useful when
/// processing user input from, say, a text prompt.
/// </summary>
public interface ISlashCommandHandler
{
	/// <summary>
	/// The command the user is attempting to execute. Null if the command is not a slash command.
	/// </summary>
	public string? Command { get; }
	/// <summary>
	/// Parameters relevant to the command being executed. For JOIN, PART, ME, IGNORE, UNIGNORE, AWAY, and QUERY,
	/// this value will contain parameters relevant to the command. For example, if the user types /join #osu, this
	/// value equals ["#osu"]. For non-relevant commands, this value will contain an array of space-delimited parameters
	/// passed to the command. For example, if the user types /echo hello world, this value equals ["hello", "world"].
	/// If the user did not provide any parameters, this value will be null.
	/// </summary>
	public string[]? Parameters { get; }
}