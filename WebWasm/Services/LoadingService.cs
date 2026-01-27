namespace WebWasm.Services;

public class LoadingService
{
	private int _loadingCount = 0;
	public event Action? OnChange;

	public bool IsLoading => _loadingCount > 0;

	public void Show()
	{
		_loadingCount++;
		OnChange?.Invoke();
	}

	public void Hide()
	{
		if (_loadingCount > 0)
		{
			_loadingCount--;
			OnChange?.Invoke();
		}
	}

	public async Task<T> ExecuteWithLoading<T>(Func<Task<T>> action, Func<Task>? callBack = null)
	{
		Show();
		try
		{
			return await action();
		}
		finally
		{
			Hide();
			if (callBack is not null)
			{
				await callBack();
			}
		}
	}

	public async Task ExecuteWithLoading(Func<Task> action, Func<Task>? callBack = null)
	{
		Show();
		try
		{
			await action();
		}
		finally
		{
			Hide();
            if (callBack is not null)
            {
                await callBack();
            }
        }
	}
}
