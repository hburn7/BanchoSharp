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
			Assert.DoesNotThrowAsync(async () =>
			{
				await client.SendAsync("test");
			});
		});
	}

	[Test]
	public async Task TestNotAuthenticatedExceptionOnSend()
	{
		BanchoClient client = new();

		try
		{
			await client.ConnectAsync();
		}
		catch
		{
			// ignored
		}
		finally
		{
			Assert.That(client.IsConnected);
			Assert.ThrowsAsync<IrcClientNotAuthenticatedException>(async () => { await client.SendAsync("Test"); });
		}
	}

	[Test]
	public async Task TestExceptionBypassOnSend()
	{
		BanchoClient client = new();
		try
		{
			await client.ConnectAsync();
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