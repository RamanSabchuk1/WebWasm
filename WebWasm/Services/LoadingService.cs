namespace WebWasm.Services;

public class LoadingService
{
	private int _loadingCount = 0;
	public event Action? OnChange;

	public bool IsLoading => _loadingCount > 0;

	// Subscribers only read IsLoading, so they are notified only when it flips: nested loads no longer re-render the overlay each time.
	public void Show()
	{
		if (++_loadingCount == 1)
		{
			OnChange?.Invoke();
		}
	}

	public void Hide()
	{
		if (_loadingCount > 0 && --_loadingCount == 0)
		{
			OnChange?.Invoke();
		}
	}

	public async Task ExecuteWithLoading(Func<Task> action)
	{
		Show();
		try
		{
			await action();
		}
		finally
		{
			Hide();
		}
	}
}
