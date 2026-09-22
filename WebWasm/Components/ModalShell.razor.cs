using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace WebWasm.Components;

public partial class ModalShell : ComponentBase
{
	[Inject] private IJSRuntime JS { get; set; } = default!;

	[Parameter] public bool IsOpen { get; set; }
	[Parameter] public string ContainerClass { get; set; } = string.Empty;
	[Parameter] public RenderFragment? ChildContent { get; set; }
	[Parameter] public EventCallback OnClose { get; set; }
	[Parameter] public bool TopLayer { get; set; }

	/// <summary>
	/// S5: обработчик success-path по Enter — оппозит <see cref="OnClose"/> (Escape).
	/// Не задан ⇒ Enter ничего не делает (старое поведение).
	/// Вложенный кейс (модалка → ConfirmDialog поверх) работает сам собой: верхняя модалка
	/// при открытии забирает фокус на свой overlay, и второй Enter приходит уже ей.
	/// </summary>
	[Parameter] public EventCallback OnSubmit { get; set; }

	private ElementReference _overlayRef;
	private bool _prevIsOpen;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (IsOpen && !_prevIsOpen)
		{
			_prevIsOpen = true;
			try { await _overlayRef.FocusAsync(); }
			catch { /* focus may fail in some scenarios */ }
		}
		else if (!IsOpen)
		{
			_prevIsOpen = false;
		}
	}

	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		switch (e.Key)
		{
			case "Escape" when OnClose.HasDelegate:
				await OnClose.InvokeAsync();
				break;

			// Модификаторы пропускаем: Shift+Enter — перевод строки, Ctrl/Alt+Enter — чужие шорткаты.
			case "Enter" when OnSubmit.HasDelegate && !e.ShiftKey && !e.CtrlKey && !e.AltKey && !e.MetaKey:
				if (await IsEnterBlocked())
				{
					return;
				}

				await OnSubmit.InvokeAsync();
				break;
		}
	}

	/// <summary>
	/// Enter не должен сабмитить, пока фокус в textarea/select/на кнопке или в открытом
	/// автокомплите: там у клавиши своё значение. KeyboardEventArgs не даёт target ⇒ спрашиваем DOM.
	/// </summary>
	private async Task<bool> IsEnterBlocked()
	{
		try
		{
			return await JS.InvokeAsync<bool>("isEnterSubmitBlocked");
		}
		catch
		{
			// Хелпер недоступен — лучше не сабмитить вслепую.
			return true;
		}
	}
}
