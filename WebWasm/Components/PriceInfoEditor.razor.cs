using Microsoft.AspNetCore.Components;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class PriceInfoEditor : ComponentBase
{
	[Parameter] public PriceInfo? ExistingPrice { get; set; }
	[Parameter] public EventCallback<PriceInfo> OnPriceChanged { get; set; }

	private decimal _minPrice = 0;
	private decimal _pricePerHour = 0;
	private decimal _pricePerKm = 0;
	private decimal _maxPrice = 0;
	private string _errorMessage = string.Empty;

	public bool IsValid
	{
		get
		{
			ClearError();
			
			if (_minPrice < 0 || _pricePerHour < 0 || _pricePerKm < 0 || _maxPrice < 0)
			{
				_errorMessage = "All prices must be greater than or equal to 0.";
				return false;
			}

			if (_minPrice > _maxPrice && _maxPrice > 0)
			{
				_errorMessage = "Min Price cannot be greater than Max Price.";
				return false;
			}

			return true;
		}
	}

	protected override void OnParametersSet()
	{
		if (ExistingPrice is not null)
		{
			_minPrice = ExistingPrice.Value.MinPrice;
			_pricePerHour = ExistingPrice.Value.PricePerHour;
			_pricePerKm = ExistingPrice.Value.PricePerKm;
			_maxPrice = ExistingPrice.Value.MaxPrice;
		}
	}

	public async Task Submit()
	{
		if (!IsValid)
			return;

		var priceInfo = new PriceInfo(_minPrice, _pricePerHour, _pricePerKm, _maxPrice);
		await OnPriceChanged.InvokeAsync(priceInfo);
	}

	private void ClearError()
	{
		_errorMessage = string.Empty;
	}

	public PriceInfo? GetPrice()
	{
		if (!IsValid)
			return null;

		return new PriceInfo(_minPrice, _pricePerHour, _pricePerKm, _maxPrice);
	}
}
