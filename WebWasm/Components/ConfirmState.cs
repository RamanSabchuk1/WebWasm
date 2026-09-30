namespace WebWasm.Components;

/// <summary>
/// State of a page's <see cref="ConfirmDialog"/>: what to ask and what to run on "Confirm".
/// Replaces the title/message/action/isOpen fields and the confirm/cancel handlers each page had.
/// </summary>
public sealed class ConfirmState
{
	private Func<Task>? _action;

	public bool IsOpen { get; private set; }
	public string Title { get; private set; } = string.Empty;
	public string Message { get; private set; } = string.Empty;

	public void Ask(string title, string message, Func<Task> action)
	{
		Title = title;
		Message = message;
		_action = action;
		IsOpen = true;
	}

	/// <summary>Closes the dialog, then runs the action (the dialog does not hang over the loader).</summary>
	public async Task Confirm()
	{
		var action = _action;
		Cancel();
		if (action is not null)
		{
			await action();
		}
	}

	public void Cancel()
	{
		IsOpen = false;
		_action = null;
	}
}
