namespace BanchoSharp.Interfaces;

public interface IBanchoBotEventInvoker
{
	/// <summary>
	///  Listens for events from BanchoBot. Fires any necessary client events if detected.
	/// </summary>
	/// <returns></returns>
	public void ProcessMessage(IPrivateIrcMessage msg);
}