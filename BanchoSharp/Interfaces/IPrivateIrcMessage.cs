namespace BanchoSharp.Interfaces
{
    public interface IPrivateIrcMessage : IIrcMessage
    {
        /// <summary>
        /// The sender of this message
        /// </summary>
        public string Sender { get; }
        /// <summary>
        /// The recipient of this message
        /// </summary>
        public string Recipient { get; }
        /// <summary>
        /// The content of the message
        /// </summary>
        public string Content { get; }
        /// <summary>
        /// Determines if the message is being sent directly to the logged-in user
        /// </summary>
        public bool IsDirect { get; }
        /// <summary>
        /// Determines if the message is from BanchoBot
        /// </summary>
        public bool IsBanchoBotMessage { get; }
    }
}