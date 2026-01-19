using Microsoft.AspNetCore.Components;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Supports(ApiClient apiClient, ToastService toastService, LoadingService loadingService) : ComponentBase
{
	private ICollection<Suggestion> _suggestions = [];

	protected override async Task OnInitializedAsync()
	{
		await LoadSuggestions();
	}

	private async Task LoadSuggestions()
	{
		await loadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				_suggestions = await apiClient.Get<ICollection<Suggestion>>("Supports/suggestion/all");
			}
			catch (Exception ex)
			{
				toastService.ShowError($"Failed to load suggestions: {ex.Message}");
			}
		});
	}

	private async Task HandleApply(Guid suggestionId)
	{
		await loadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await apiClient.Post($"Supports/suggestion/apply?suggestionId={suggestionId}");
				toastService.ShowSuccess("Suggestion applied successfully!");
				await LoadSuggestions();
			}
			catch (Exception ex)
			{
				toastService.ShowError($"Failed to apply suggestion: {ex.Message}");
			}
		});
	}
}
