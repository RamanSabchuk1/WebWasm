using Microsoft.AspNetCore.Components;
using WebWasm.Helpers;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Supports(CashService cashService, ApiClient apiClient, ToastService toastService, LoadingService loadingService)
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
		await loadingService.Run(toastService, async () =>
		{
			await apiClient.Post($"Supports/suggestion/apply?suggestionId={suggestionId}");
			toastService.ShowSuccess(T.Support.AppliedToast);
			await LoadSuggestions(false);
		}, T.Support.ApplyFailed);
	}

	public record SuggestionsWithUser(Suggestion Suggestion, User? User)
	{
		public string? GetUserName() => User.ShortName;
	}
}
