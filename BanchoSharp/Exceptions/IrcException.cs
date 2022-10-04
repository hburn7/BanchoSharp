namespace BanchoSharp.Exceptions;

public class IrcException : Exception
{
	public IrcException() {}
	public IrcException(string message) : base(message) {}
	public IrcException(string message, Exception? innerException) : base(message, innerException) {}
}