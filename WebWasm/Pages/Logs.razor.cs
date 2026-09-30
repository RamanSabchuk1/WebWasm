using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Logs : ComponentBase
{
	[Inject] private ApiClient ApiClient { get; set; } = default!;
	[Inject] private LoadingService LoadingService { get; set; } = default!;
	[Inject] private ToastService ToastService { get; set; } = default!;
	[Inject] private IJSRuntime JSRuntime { get; set; } = default!;

	private List<LogItem> _logItems = [];

	private sealed class LogItem
	{
		public Log Log { get; set; } = default!;
		public bool IsExpanded { get; set; }
	}

	private static bool HasException(Log log) => !string.IsNullOrEmpty(log.Exception);

	private static string GetRowClass(string level) => level?.ToLower() switch
	{
		"error" or "fatal" => "log-row-error",
		"warning" => "log-row-warning",
		_ => "log-row-info"
	};

	private static void ToggleExpand(LogItem item)
	{
		if (HasException(item.Log))
		{
			item.IsExpanded = !item.IsExpanded;
		}
	}

	private Task CopyToClipboard(string text) => JSRuntime.CopyToClipboard(ToastService, text, "Template copied");

	protected override async Task OnInitializedAsync()
	{
		await LoadingService.Run(ToastService, async () =>
		{
			var logs = await ApiClient.Get<Log[]>("Counts/logs");
			_logItems = [.. logs.Select(l => new LogItem { Log = l })];
			ToastService.ShowSuccess("Logs loaded successfully");
		}, "Failed to load logs: ");
	}
}
