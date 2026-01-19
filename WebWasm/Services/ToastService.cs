namespace WebWasm.Services;

public class ToastService
{
	public event Action<string, ToastType>? OnShow;

	public void ShowSuccess(string message) => OnShow?.Invoke(message, ToastType.Success);
	public void ShowError(string message) => OnShow?.Invoke(message, ToastType.Error);
	public void ShowInfo(string message) => OnShow?.Invoke(message, ToastType.Info);
	public void ShowWarning(string message) => OnShow?.Invoke(message, ToastType.Warning);
}

public enum ToastType
{
	Success,
	Error,
	Info,
	Warning
}
