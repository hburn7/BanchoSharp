namespace BanchoSharp.Exceptions;

public class IrcConnectionFailureException : IrcException
{
	public IrcConnectionFailureException() {}
	public IrcConnectionFailureException(string message) : base(message) {}
	public IrcConnectionFailureException(string message, Exception? innerException) : base(message, innerException) {}
}