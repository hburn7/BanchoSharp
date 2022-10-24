using BanchoSharp;
using BanchoSharp.Exceptions;

namespace BanchoSharpTests;

public class ExceptionTests
{
	[SetUp]
	public void Setup() {}

	[Test]
	public void TestNotConnectedException()
	{
		BanchoClient client = new();
		Assert.Multiple(() =>
		{
			Assert.That(!client.IsConnected);
			Assert.Throws<IrcClientNotConnectedException>(() =>
			{
				client.SendAsync("test").GetAwaiter().GetResult();
			});
		});
	}

	[Test]
	public void TestNotAuthenticatedExceptionOnConnection()
	{
		BanchoClient client = new();
		Assert.Throws<IrcClientNotAuthenticatedException>(() =>
		{
			client.ConnectAsync().GetAwaiter().GetResult();
		});
	}

	[Test]
	public void TestNotAuthenticatedExceptionOnSend()
	{
		BanchoClient client = new();

		try
		{
			client.ConnectAsync().GetAwaiter().GetResult();
		}
		catch {}
		finally
		{
			Assert.That(client.IsConnected);
			Assert.Throws<IrcClientNotAuthenticatedException>(() => { client.SendAsync("Test").GetAwaiter().GetResult(); });
		}
	}
}