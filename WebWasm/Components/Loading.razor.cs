using Microsoft.AspNetCore.Components;
using WebWasm.Services;

namespace WebWasm.Components;

public partial class Loading : ComponentBase, IDisposable
{
	[Inject] private LoadingService LoadingSvc { get; set; } = default!;

	private bool _isLoading;

	protected override void OnInitialized()
	{
		LoadingSvc.OnChange += OnLoadingStateChanged;
		_isLoading = LoadingSvc.IsLoading;
	}

	private void OnLoadingStateChanged()
	{
		_isLoading = LoadingSvc.IsLoading;
		InvokeAsync(StateHasChanged);
	}

	public void Dispose()
	{
		LoadingSvc.OnChange -= OnLoadingStateChanged;
	}
}
