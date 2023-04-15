using BanchoSharp.Multiplayer;

namespace BanchoSharp.Messaging;

/// <summary>
/// Responsible for handling the parsing of BanchoBot's response to !mp set
/// </summary>
public class MpSetResponseParser
{
    private readonly string _response;

    /// <summary>
    /// Initializes a new instance of the MpSetResponseParser class
    /// </summary>
    /// <param name="banchoResponse">response won from Bancho Bot</param>  
    public MpSetResponseParser(string banchoResponse)
    {
        _response = banchoResponse;

        IsMpSetResponse = _response.StartsWith("Changed match settings to ");
        if (!IsMpSetResponse)
        {
            return;
        }

        ResolvedConfiguration = Parse();
    }

    /// <summary>
    /// Gets if the response is a mpset one
    /// </summary>
    public bool IsMpSetResponse { get; }

    /// <summary>
    /// Gets the mpset configuration depending on the banchoResponse
    /// </summary>
    public MpSetConfig? ResolvedConfiguration { get; }

    private MpSetConfig? Parse()
    {
        // Regex to match the format, win condition, and size
        try
        {
            var config = new MpSetConfig();
            string relevantInfo = _response.Split("Changed match settings to ")[1];
            string[] tokens = relevantInfo.Split(',');

            if (tokens.Length == 0)
            {
                return null;
            }

            if (tokens.Length == 1)
            {
                // Only the format was set
                config.Format = (LobbyFormat)Enum.Parse(typeof(LobbyFormat), tokens[0].Trim());
            }
            else if (tokens.Length == 2)
            {
                // The format and win condition were set
                config.Format = (LobbyFormat)Enum.Parse(typeof(LobbyFormat), tokens[0].Trim());
                config.WinCondition = (WinCondition)Enum.Parse(typeof(WinCondition), tokens[1].Trim());
            }
            else if (tokens.Length == 3)
            {
                // Slots, format, and win condition were set
                string sizeParse = tokens[0].Split("slots")[0].Trim();
                if (int.TryParse(sizeParse, out int size))
                {
                    config.Size = size;
                }
                else
                {
                    Logger.Warn($"Failed to parse {sizeParse} as an integer. Size unable to be determined.");
                }

                config.Format = (LobbyFormat)Enum.Parse(typeof(LobbyFormat), tokens[1].Trim());
                config.WinCondition = (WinCondition)Enum.Parse(typeof(WinCondition), tokens[2].Trim());
            }

            return config;
        }
        catch (Exception e)
        {
            Logger.Warn($"Failed to parse mp set response! {e.Message}");
            return null;
        }
    }
}

/// <summary>
/// A structure to hold the mpset configuration fields
/// </summary>
public struct MpSetConfig
{
    /// <summary>
    /// Gets or Sets the format of the multiplayer lobby
    /// </summary>
    public LobbyFormat Format { get; set; }         // AKA teammode in docs

    /// <summary>
    /// Gets or sets the win condition of the multiplayer lobby
    /// </summary>
    public WinCondition? WinCondition { get; set; } // AKA scoremode in docs

    /// <summary>
    /// Gets or Sets the number of max available slots for the multiplayer lobby
    /// </summary>
    public int? Size { get; set; }
}