using Microsoft.AspNetCore.Components;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Supports(CashService cashService, ApiClient apiClient, ToastService toastService, LoadingService loadingService) : ComponentBase
{
	private ICollection<SuggestionsWithUser> _suggestions = [];

	protected override async Task OnInitializedAsync()
	{
		await LoadSuggestions(true);
	}

	private async Task LoadSuggestions(bool useCash)
	{
		var suggestions = await cashService.GetData<Suggestion>(useCash);
		var users = await cashService.GetData<User>(useCash);
		var userDict = users.ToDictionary(u => u.UserInfo.Id);
		_suggestions = [.. suggestions.Select(s => new SuggestionsWithUser(s, userDict.TryGetValue(s.UserInfoId, out var user) ? user : null))];
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

	public record SuggestionsWithUser(Suggestion Suggestion, User? User)
	{
		public string GetUserName()
		{
			return User is null ? "Anonymous" : $"{User.Login}";
		}
	}
}
