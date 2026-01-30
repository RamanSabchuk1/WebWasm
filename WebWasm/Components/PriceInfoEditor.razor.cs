using Microsoft.AspNetCore.Components;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class PriceInfoEditor : ComponentBase
{
	[Parameter] public Dictionary<uint, PriceInfo>? ExistingPrices { get; set; }
	[Parameter] public EventCallback<Dictionary<uint, PriceInfo>> OnPricesChanged { get; set; }

	private List<PriceEntryModel> _entries = [];
	private string _globalError = string.Empty;
	private int? _expandedIndex = null;
	private bool _isInitialized = false;
	private Dictionary<uint, PriceInfo>? _lastExistingPrices = null;

	private class PriceEntryModel
	{
		public uint Key { get; set; }
		public decimal MinPrice { get; set; }
		public decimal PricePerHour { get; set; }
		public decimal PricePerKm { get; set; }
		public decimal MaxPrice { get; set; }
		public string Error { get; set; } = string.Empty;

		public bool IsValid()
		{
			if (Key == 0)
			{
				Error = "Key cannot be 0";
				return false;
			}

			if (MinPrice < 0 || PricePerHour < 0 || PricePerKm < 0 || MaxPrice < 0)
			{
				Error = "All prices must be >= 0";
				return false;
			}

			if (MinPrice > MaxPrice && MaxPrice > 0)
			{
				Error = "Min Price cannot exceed Max Price";
				return false;
			}

			Error = string.Empty;
			return true;
		}

		public PriceInfo ToPriceInfo()
		{
			return new PriceInfo(MinPrice, PricePerHour, PricePerKm, MaxPrice);
		}
	}

	protected override void OnInitialized()
	{
		InitializeEntries();
	}

	protected override void OnParametersSet()
	{
		// Only reinitialize if ExistingPrices reference actually changed
		if (!ReferenceEquals(_lastExistingPrices, ExistingPrices))
		{
			_lastExistingPrices = ExistingPrices;
			InitializeEntries();
		}
	}

	private void InitializeEntries()
	{
		_entries.Clear();
		_expandedIndex = null;
		_globalError = string.Empty;
		_isInitialized = true;

		if (ExistingPrices?.Count > 0)
		{
			// Load existing prices into accordion
			foreach (var kvp in ExistingPrices)
			{
				_entries.Add(new PriceEntryModel
				{
					Key = kvp.Key,
					MinPrice = kvp.Value.MinPrice,
					PricePerHour = kvp.Value.PricePerHour,
					PricePerKm = kvp.Value.PricePerKm,
					MaxPrice = kvp.Value.MaxPrice,
					Error = string.Empty
				});
			}
			// Expand first entry for convenience
			_expandedIndex = 0;
		}
		else
		{
			// Create a new empty entry if no existing prices
			AddNewEntry();
		}
	}

	private void ToggleExpand(int index)
	{
		if (_expandedIndex == index)
		{
			_expandedIndex = null;
		}
		else
		{
			_expandedIndex = index;
		}
		_globalError = string.Empty;
	}

	private void AddNewEntry()
	{
		_entries.Add(new PriceEntryModel { Key = (uint)_entries.Count + 1 });
		_expandedIndex = _entries.Count - 1;
		_globalError = string.Empty;
	}

	private void RemoveEntry(int index)
	{
		if (index >= 0 && index < _entries.Count)
		{
			_entries.RemoveAt(index);
			_expandedIndex = null;
			_globalError = string.Empty;
		}
	}

	public bool IsValid
	{
		get
		{
			_globalError = string.Empty;

			if (_entries.Count == 0)
			{
				_globalError = "At least one price entry is required";
				return false;
			}

			var keys = new HashSet<uint>();
			foreach (var entry in _entries)
			{
				if (!entry.IsValid())
					return false;

				if (keys.Contains(entry.Key))
				{
					_globalError = "Duplicate keys are not allowed";
					return false;
				}
				keys.Add(entry.Key);
			}

			return true;
		}
	}

	public Dictionary<uint, PriceInfo>? GetPrices()
	{
		if (!IsValid)
			return null;

		var result = new Dictionary<uint, PriceInfo>();
		foreach (var entry in _entries)
		{
			result[entry.Key] = entry.ToPriceInfo();
		}

		return result;
	}
}
