using Microsoft.AspNetCore.Components;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class SortHeader
{
	[Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

	/// <summary>Column key of <see cref="SortState.ColumnKey"/>.</summary>
	[Parameter, EditorRequired] public string Key { get; set; } = string.Empty;

	[Parameter, EditorRequired] public SortState State { get; set; } = new();

	/// <summary>Gets <see cref="Key"/>; the table cycles its SortState and saves it.</summary>
	[Parameter] public EventCallback<string> OnSort { get; set; }

	private bool IsSorted => State.ColumnKey == Key;

	private IconName SortIcon => !IsSorted ? IconName.Sort : State.Descending ? IconName.SortDesc : IconName.SortAsc;
}
