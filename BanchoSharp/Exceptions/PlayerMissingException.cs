namespace BanchoSharp.Exceptions;

public class PlayerMissingException : Exception
{
	public PlayerMissingException() {}
	public PlayerMissingException(string message) : base(message) {}
	public PlayerMissingException(string message, Exception? innerException) : base(message, innerException) {}
}