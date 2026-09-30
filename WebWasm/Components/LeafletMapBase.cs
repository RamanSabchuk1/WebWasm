using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace WebWasm.Components;

/// <summary>
/// Leaflet map component: after the first render imports its JS module (<see cref="ModulePath"/>), lets the
/// subclass create the map in <see cref="MapElement"/> and disposes both on removal.
/// </summary>
public abstract class LeafletMapBase : ComponentBase, IAsyncDisposable
{
	[Inject] protected IJSRuntime JS { get; set; } = default!;

	protected ElementReference MapElement;
	private IJSObjectReference? _module;

	/// <summary>Map object returned by the module (has <c>dispose</c>); null until created or if creation failed.</summary>
	protected IJSObjectReference? MapInstance { get; private set; }

	protected abstract string ModulePath { get; }

	/// <summary>False skips the map entirely (e.g. nothing to show).</summary>
	protected virtual bool ShouldCreateMap => true;

	/// <summary>Module function that creates the map, and its arguments after the map element.</summary>
	protected abstract (string Function, object?[] Args) MapInit();

	protected virtual void OnMapError(string message) => Console.WriteLine($"{GetType().Name}: {message}");

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender || !ShouldCreateMap)
		{
			return;
		}

		try
		{
			_module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
		}
		catch (JSException ex)
		{
			OnMapError($"Failed to load map: {ex.Message}");
			return;
		}

		try
		{
			var (function, args) = MapInit();
			MapInstance = await _module.InvokeAsync<IJSObjectReference>(function, [MapElement, .. args]);
		}
		catch (JSException ex)
		{
			OnMapError($"Failed to initialize map: {ex.Message}");
		}
	}

	public virtual async ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		if (MapInstance is not null)
		{
			try
			{
				await MapInstance.InvokeVoidAsync("dispose");
			}
			catch (JSException)
			{
				// The map may already be gone together with its element.
			}

			await MapInstance.DisposeAsync();
		}

		if (_module is not null)
		{
			await _module.DisposeAsync();
		}
	}
}
