using WebWasm.Services;

namespace WebWasm.Tests;

public class LoadingServiceTests
{
	[Fact]
	public async Task NestedLoads_NotifyOnlyOnStartAndEnd()
	{
		var loading = new LoadingService();
		var states = new List<bool>();
		loading.OnChange += () => states.Add(loading.IsLoading);

		await loading.ExecuteWithLoading(async () =>
		{
			await loading.ExecuteWithLoading(() => Task.CompletedTask);
			await loading.ExecuteWithLoading(() => Task.CompletedTask);
		});

		Assert.Equal([true, false], states);
	}

	[Fact]
	public async Task FailedLoad_StillHides()
	{
		var loading = new LoadingService();

		await Assert.ThrowsAsync<InvalidOperationException>(() => loading.ExecuteWithLoading(() => throw new InvalidOperationException()));

		Assert.False(loading.IsLoading);
	}

	[Fact]
	public void ExtraHide_IsIgnored()
	{
		var loading = new LoadingService();
		var notified = 0;
		loading.OnChange += () => notified++;

		loading.Hide();
		loading.Show();
		loading.Hide();
		loading.Hide();

		Assert.False(loading.IsLoading);
		Assert.Equal(2, notified);
	}
}
