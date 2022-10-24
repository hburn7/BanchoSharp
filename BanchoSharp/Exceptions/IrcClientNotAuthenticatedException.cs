namespace BanchoSharp.Exceptions;

public class IrcClientNotAuthenticatedException : IrcException
{
	public IrcClientNotAuthenticatedException() {}
	public IrcClientNotAuthenticatedException(string s) : base(s) {}
}