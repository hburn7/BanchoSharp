/// <summary>
/// Interface for notifying object's state changes.
/// </summary>
public interface INotifyStateChanged
{
    /// <summary>
    /// Event triggered on state change.
    /// </summary>
    event Action OnStateChanged;

    /// <summary>
    /// Method to invoke OnStateChanged event.
    /// </summary>
    void InvokeOnStateChanged();
}
