
using BanchoSharp.Interfaces;
namespace BanchoSharp.EventArgs
{
    /// <summary>
    /// Event arguments for when a player is banned from a lobby.
    /// </summary>
    public class PlayerBannedEventArgs : System.EventArgs
    {
        /// <summary>
        /// Constructs a new instance of the PlayerBannedEventArgs class.
        /// </summary>
        /// <param name="player">The player who was banned.</param>
        /// <param name="banTime">The time the ban occurred.</param>
        public PlayerBannedEventArgs(IMultiplayerPlayer player, DateTime banTime)
        {
            Player = player;
            BanTime = banTime;
        }

        /// <summary>
        /// The player who was banned.
        /// </summary>
        public IMultiplayerPlayer Player { get; }

        /// <summary>
        /// The time the player was banned.
        /// </summary>
        public DateTime BanTime { get; }
    }
}