using BanchoSharp.Interfaces;

namespace BanchoSharp.Messaging;

public class SlashCommandHandler : ISlashCommandHandler
{
	private readonly string _prompt;
	private readonly string[] _splits;

	/// <summary>
	/// A <see cref="ISlashCommandHandler"/> is responsible for parsing
	/// a user-provided slash command.
	/// 
	/// <example>
	/// <code>
	/// string prompt = "/join #osu with invalid args"
	/// var handler = new SlashCommandHandler(prompt);
	/// // handler.Command = "join"
	/// // handler.RelevantParameters = { "#osu" }
	/// // handler.Parameters = { "#osu", "with", "invalid", "args" }
	/// </code>
	/// </example>
	/// </summary>
	/// <param name="prompt">The input to process</param>
	public SlashCommandHandler(string prompt)
	{
		_prompt = prompt;
		_splits = _prompt.Split();
		
		if (!IsSlashCommand())
		{
			Logger.Warn($"Message {prompt} is not a slash command.");
			return;
		}

		IrcCommand = _prompt.Split('/')[1].Split()[0];

		if (_splits.Length > 1)
		{
			// Params provided
			RelevantParameters = GetRelevantParamsForCommand();
			Parameters = GetParameters();
		}
		else
		{
			// No params provided
			RelevantParameters = null;
			Parameters = null;
		}
	}
	
	public string IrcCommand { get; }
	public string[]? RelevantParameters { get; }
	public string[]? Parameters { get; }

	private bool IsSlashCommand() => !string.IsNullOrWhiteSpace(_prompt) &&
	                                 _prompt.StartsWith("/") &&
	                                 _prompt.Length > 1;

	private string[] GetRelevantParamsForCommand() => IrcCommand.ToLower() switch
	{
		"join" => GetFirstArgOrDefault(),
		"part" => GetFirstArgOrDefault(),
		"me" => GetAllAsStringOrDefault(),
		"ignore" => GetFirstArgOrDefault(),
		"unignore" => GetFirstArgOrDefault(), // Not tested, I do not know if /unignore works. It's here just in case.
		"away" => GetAllAsStringOrDefault(),
		"query" => GetFirstArgOrDefault(),
		_ => Array.Empty<string>()
	};

	private string[] GetParameters() => IrcCommand.ToLower() switch
	{
		"me" => GetAllAsStringOrDefault(),
		"away" => GetAllAsStringOrDefault(),
		_ => _splits[1..]
	};
	
	private string[] GetFirstArgOrDefault() => _splits.Length >= 2 ? new string[] { _splits[1] } : Array.Empty<string>();
	private string[] GetAllAsStringOrDefault() => _splits.Length >= 2 ? 
		new string[] {string.Join(" ", _splits[1..])} // Combine into one string, return as a 1-parameter array containing the string.
		: Array.Empty<string>();
}