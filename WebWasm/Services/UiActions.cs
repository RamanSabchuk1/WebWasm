using Microsoft.JSInterop;

namespace WebWasm.Services;

public static class UiActions
{
	extension(LoadingService loading)
	{
		/// <summary>
		/// Runs a UI action under the loading overlay; an exception becomes an error toast "<paramref name="errorPrefix"/>{message}".
		/// Replaces the repeated ExecuteWithLoading + try/catch + ShowError block of the pages.
		/// </summary>
		public Task Run(ToastService toast, Func<Task> action, string errorPrefix) =>
			loading.ExecuteWithLoading(async () =>
			{
				try
				{
					await action();
				}
				catch (Exception ex)
				{
					toast.ShowError($"{errorPrefix}{ex.Message}");
				}
			});
	}

	extension(IJSRuntime js)
	{
		/// <summary>Copies to the clipboard and reports the result as a toast (the browser may refuse without focus or HTTPS).</summary>
		public async Task CopyToClipboard(ToastService toast, string text, string successMessage)
		{
			try
			{
				await js.InvokeVoidAsync("navigator.clipboard.writeText", text);
				toast.ShowSuccess(successMessage);
			}
			catch (JSException)
			{
				toast.ShowError("Failed to copy");
			}
		}
	}
}
