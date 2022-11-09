# BanchoSharp

## Synopsis
The solution for connecting to osu!Bancho in C#. This library provides various events and tools for working with osu!Bancho, [osu!](https://osu.ppy.sh/home)'s IRC server.

## Getting Started
* Add the [NuGet package](https://www.nuget.org/packages/BanchoSharp) to your project.
* Instantiate the client with the necessary credentials and subscribe to any relevant events.
> ```cs
> string username = "bob";
> string password = "12345";
>
> IBanchoClient client = new BanchoClient(new BanchoClientConfig(new IrcCredentials(username, password)));
> 
> client.OnAuthenticated += () => 
> {
>     // Do stuff here - user is now authenticated with osu!Bancho
> }
> ````
* BanchoBot events are also directly subscribable. These are events that fire when a message is received from BanchoBot containing important information.
>```cs
> // NOTE: BanchoBotEvents and MultiplayerLobbies are WIP features.
> client.BanchoBotEvents.OnTournamentLobbyCreated += (lobby) => 
> {
>    // Send notifcation, etc. 
> }
>```