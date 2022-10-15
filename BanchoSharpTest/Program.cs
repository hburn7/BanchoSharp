// See https://aka.ms/new-console-template for more information

using BanchoSharp;
using BanchoSharp.Multiplayer;

var client = new BanchoClient(new BanchoClientConfig(new IrcCredentials("Stage",
	Environment.GetEnvironmentVariable("IRC_PASS")), LogLevel.Debug));


await client.ConnectAsync();


