namespace BanchoSharp.Interfaces;

public interface INotifyStateChanged
{
	public event Action OnStateChanged;
	public void InvokeOnStateChanged();
}