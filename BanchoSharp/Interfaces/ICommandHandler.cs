namespace BanchoSharp.Interfaces;

/// <summary>
/// Interface responsible for handling user-provided slash commands. This is useful when
/// processing user input from, say, a text prompt.
/// </summary>
public interface ISlashCommandHandler
{
	/// <summary>
	/// The IRC command associated with the slash command message. Null if the input to process is not a slash command
	/// or if a relevant slash command could not be determined from the input.
	/// </summary>
	public string? IrcCommand { get; }
	/// <summary>
	/// An optional list of relevant arguments. This property is guaranteed to only
	/// contain parameters relevant to the processing of an IRC command. If the prompt
	/// contains additional irrelevant arguments, they will be included in
	/// <see cref="ISlashCommandHandler.Parameters"/> but not in <see cref="ISlashCommandHandler.RelevantParameters"/>.
	/// If the prompt contains no arguments, this will be null. If it does not contain any *relevant* arguments, or if
	/// the relevant arguments cannot be determined due to the command not being supported, the array will be empty.
	///
	/// Supported commands: JOIN, PART, ME, IGNORE, AWAY, QUERY
	/// </summary>
	public string[]? RelevantParameters { get; }
	/// <summary>
	/// An optional list of additional space-separated arguments provided to the command. If none
	/// were provided, this array will be empty (not null).
	/// </summary>
	public string[]? Parameters { get; }
}