using Microsoft.AspNetCore.Components;
using WebWasm.Services;

namespace WebWasm.Components;

public partial class Toast : ComponentBase, IDisposable
{
	[Inject] private ToastService ToastSvc { get; set; } = default!;

	private readonly List<ToastMessage> _messages = [];
	private readonly Dictionary<ToastMessage, CancellationTokenSource> _timers = [];

	protected override void OnInitialized()
	{
		ToastSvc.OnShow += ShowToast;
	}

	private void ShowToast(string message, ToastType type)
	{
		var toastMessage = new ToastMessage { Text = message, Type = type };
		_messages.Add(toastMessage);

		var cts = new CancellationTokenSource();
		_timers[toastMessage] = cts;
		_ = HideAfterDelay(toastMessage, cts.Token);

		StateHasChanged();
	}

	private async Task HideAfterDelay(ToastMessage message, CancellationToken token)
	{
		try
		{
			await Task.Delay(4000, token);
		}
		catch (TaskCanceledException)
		{
			return;
		}

		await InvokeAsync(() =>
		{
			RemoveMessage(message);
			StateHasChanged();
		});
	}

	private void RemoveMessage(ToastMessage message)
	{
		if (_timers.TryGetValue(message, out var cts))
		{
			cts.Cancel();
			cts.Dispose();
			_timers.Remove(message);
		}
		_messages.Remove(message);
	}

	private static string GetIcon(ToastType type) => type switch
	{
		ToastType.Success => "✓",
		ToastType.Error => "✕",
		ToastType.Warning => "⚠",
		ToastType.Info => "ℹ",
		_ => "ℹ"
	};

	public void Dispose()
	{
		ToastSvc.OnShow -= ShowToast;
		foreach (var cts in _timers.Values)
		{
			cts.Cancel();
			cts.Dispose();
		}
		_timers.Clear();
	}

	private class ToastMessage
	{
		public string Text { get; set; } = string.Empty;
		public ToastType Type { get; set; }
	}
}
