/// <summary>
/// Interface for IRC Credentials containing the properties Username and Password. 
/// </summary>
public interface IIrcCredentials
{
    /// <summary>
    /// Gets the username for the IRC credentials.
    /// </summary>
    public string Username { get; }
    
    /// <summary>
    /// Gets the password for the IRC credentials.
    /// </summary>
    public string Password { get; }
}
