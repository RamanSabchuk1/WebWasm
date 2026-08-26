namespace WebWasm.Models;

/// <summary>
/// Persisted sort state for a table. ColumnKey == null means no sort (None).
/// Cycle: None → Asc → Desc → None.
/// </summary>
public record SortState(string? ColumnKey = null, bool Descending = false);