using Microsoft.JSInterop;

namespace WebWasm.Services;

public static class UiActions
{
	extension(LoadingService loading)
	{
		/// <summary>
		/// Runs a UI action under the loading overlay; an exception becomes an error toast built from its message:
		/// <c>T.Materials.SaveFailed</c>. Replaces the repeated ExecuteWithLoading + try/catch + ShowError block of the pages.
		/// </summary>
		public Task Run(ToastService toast, Func<Task> action, Func<string, string> errorMessage) =>
			loading.ExecuteWithLoading(async () =>
			{
				try
				{
					await action();
				}
				catch (Exception ex)
				{
					toast.ShowError(errorMessage(ex.Message));
				}
			});
	}

	extension(IJSRuntime js)
	{
		/// <summary>Copies to the clipboard and reports the result as a toast (the browser may refuse without focus or HTTPS).</summary>
		public async Task CopyToClipboard(ToastService toast, string text, string successMessage, string failureMessage)
		{
			try
			{
				await js.InvokeVoidAsync("navigator.clipboard.writeText", text);
				toast.ShowSuccess(successMessage);
			}
			catch (JSException)
			{
				toast.ShowError(failureMessage);
			}
		}
	}
}
