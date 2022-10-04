namespace BanchoSharp.Exceptions;

public class IrcClientNotConnectedException : IrcException
{
	public IrcClientNotConnectedException() {}
	public IrcClientNotConnectedException(string message) : base(message) {}
	public IrcClientNotConnectedException(string message, Exception? innerException) : base(message, innerException) {}
}