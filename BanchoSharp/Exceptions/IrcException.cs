namespace BanchoSharp.Exceptions;

public class IrcException : Exception
{
	protected IrcException() {}
	protected IrcException(string message) : base(message) {}
	protected IrcException(string message, Exception? innerException) : base(message, innerException) {}
}