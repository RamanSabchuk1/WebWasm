using Microsoft.AspNetCore.Components;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Supports(CashService cashService, ApiClient apiClient, ToastService toastService, LoadingService loadingService) : ComponentBase
{
	private ICollection<Suggestion> _suggestions = [];

	protected override async Task OnInitializedAsync()
	{
		await LoadSuggestions(true);
	}

	private async Task LoadSuggestions(bool useCash)
	{
		_suggestions = await cashService.GetData<Suggestion>(useCash);
	}

	private async Task HandleApply(Guid suggestionId)
	{
		await loadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await apiClient.Post($"Supports/suggestion/apply?suggestionId={suggestionId}");
				toastService.ShowSuccess("Suggestion applied successfully!");
				await LoadSuggestions(false);
			}
			catch (Exception ex)
			{
				toastService.ShowError($"Failed to apply suggestion: {ex.Message}");
			}
		});
	}
}
