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
			Assert.DoesNotThrow(() =>
			{
				client.SendAsync("test").GetAwaiter().GetResult();
			});
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

	[Test]
	public void TestExceptionBypassOnSend()
	{
		BanchoClient client = new();
		try
		{
			client.ConnectAsync().GetAwaiter().GetResult();
		}
		catch
		{
			// ignored
		}
		finally
		{
			Assert.That(client.IsConnected);
		
			Assert.DoesNotThrowAsync(async () =>
			{
				await client.SendAsync("PASS test");
				await client.SendAsync("NICK test");
				await client.SendAsync("USER test");
			});
		}
	}
}