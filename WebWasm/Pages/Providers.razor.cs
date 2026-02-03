using Microsoft.AspNetCore.Components;
using WebWasm.Components;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Providers : ComponentBase
{
	[Inject] private CashService CashService { get; set; } = default!;
	[Inject] private ApiClient ApiClient { get; set; } = default!;
	[Inject] private ToastService ToastService { get; set; } = default!;
	[Inject] private LoadingService LoadingService { get; set; } = default!;

	private List<Producer> _producers = [];
	private List<Company> _companies = [];
	private List<MaterialType> _materialTypes = [];
	private ProducersTable? _producersTableRef;

	// Producer Modal
	private bool _isProducerModalOpen = false;
	private Producer? _editingProducer = null;

	// LoadingPlace Modal
	private bool _isLoadingPlaceModalOpen = false;
	private LoadingPlace? _editingLoadingPlace = null;
	private Guid _currentProducerId = Guid.Empty;

	// Confirm Dialog
	private bool _showConfirmDialog = false;
	private string _confirmTitle = string.Empty;
	private string _confirmMessage = string.Empty;
	private Func<Task>? _confirmAction = null;

	protected override async Task OnInitializedAsync()
	{
		await LoadData(true);
	}

	private async Task LoadData(bool useCash)
	{
		_producers = [.. await CashService.GetData<Producer>(useCash)];
		_companies = [.. await CashService.GetData<Company>(useCash)];
		_materialTypes = [.. await CashService.GetData<MaterialType>(useCash)];
	}

	// Producer Actions
	private void OpenAddProducerModal()
	{
		_editingProducer = null;
		_isProducerModalOpen = true;
	}

	private void HandleEditProducer(Producer producer)
	{
		_editingProducer = producer;
		_isProducerModalOpen = true;
	}

	private void CloseProducerModal()
	{
		_isProducerModalOpen = false;
		_editingProducer = null;
	}

	private async Task HandleProducerSubmit((Guid? CompanyId, string Name, ICollection<ProducerWorkingTime> WorkingTimes) data)
	{
		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				if (_editingProducer is not null)
				{
					// Update producer
					var companyId = _editingProducer.Company?.Id ?? Guid.Empty;
					var updateProducer = new UpdateProducer(data.WorkingTimes);
					await ApiClient.Put($"Producers/{_editingProducer.Id}?companyId={companyId}", updateProducer);
					ToastService.ShowSuccess("Producer updated successfully!");
				}
				else
				{
					// Create producer
					var companyId = data.CompanyId ?? Guid.Empty;
					var createProducer = new CreateProducer(data.WorkingTimes, data.Name);
					await ApiClient.Post($"Producers?companyId={companyId}", createProducer);
					ToastService.ShowSuccess("Producer created successfully!");
				}

				await LoadData(false);
				CloseProducerModal();
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to save producer: {ex.Message}");
			}
		});
	}

	private void HandleDeleteProducer(Guid producerId)
	{
		var producer = _producers.FirstOrDefault(p => p.Id == producerId);
		if (producer is null)
			return;

		var loadingPlacesCount = producer.LoadingPlaces.Count;
		_confirmTitle = "Confirm Delete Producer";
		_confirmMessage = loadingPlacesCount > 0
			? $"Are you sure you want to delete producer '{producer.Name}'? This will also delete all {loadingPlacesCount} loading place(s). This action cannot be undone."
			: $"Are you sure you want to delete producer '{producer.Name}'? This action cannot be undone.";
		_confirmAction = async () => await DeleteProducerConfirmed(producerId);
		_showConfirmDialog = true;
	}

	private async Task DeleteProducerConfirmed(Guid producerId)
	{
		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				var producer = _producers.FirstOrDefault(p => p.Id == producerId);
				var companyId = producer?.Company?.Id ?? Guid.Empty;
				await ApiClient.Delete($"Producers/{producerId}?companyId={companyId}");
				ToastService.ShowSuccess("Producer deleted successfully!");
				await LoadData(false);
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to delete producer: {ex.Message}");
			}
		});
	}

	// LoadingPlace Actions
	private void HandleAddLoadingPlace(Producer producer)
	{
		_currentProducerId = producer.Id;
		_editingLoadingPlace = null;
		_isLoadingPlaceModalOpen = true;
	}

	private void HandleEditLoadingPlace((Guid ProducerId, LoadingPlace LoadingPlace) data)
	{
		_currentProducerId = data.ProducerId;
		_editingLoadingPlace = data.LoadingPlace;
		_isLoadingPlaceModalOpen = true;
	}

	private void CloseLoadingPlaceModal()
	{
		_isLoadingPlaceModalOpen = false;
		_editingLoadingPlace = null;
		_currentProducerId = Guid.Empty;
	}

	private async Task HandleLoadingPlaceSubmit(MutateLoadingPlace loadingPlaceData)
	{
		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				var producer = _producers.FirstOrDefault(p => p.Id == _currentProducerId);
				var companyId = producer?.Company?.Id ?? Guid.Empty;

				if (_editingLoadingPlace is not null)
				{
					// Update loading place
					await ApiClient.Put(
						$"Producers/{_currentProducerId}/loading-place/{_editingLoadingPlace.Id}?companyId={companyId}",
						loadingPlaceData
					);
					ToastService.ShowSuccess("Loading place updated successfully!");
				}
				else
				{
					// Create loading place
					await ApiClient.Post(
						$"Producers/{_currentProducerId}/loading-place?companyId={companyId}",
						loadingPlaceData
					);
					ToastService.ShowSuccess("Loading place created successfully!");
				}

				await LoadData(false);
				CloseLoadingPlaceModal();
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to save loading place: {ex.Message}");
			}
		});
	}

	private void HandleDeleteLoadingPlace((Guid ProducerId, Guid LoadingPlaceId) data)
	{
		var producer = _producers.FirstOrDefault(p => p.Id == data.ProducerId);
		var loadingPlace = producer?.LoadingPlaces.FirstOrDefault(lp => lp.Id == data.LoadingPlaceId);

		if (loadingPlace is null)
			return;

		_confirmTitle = "Confirm Delete Loading Place";
		_confirmMessage = $"Are you sure you want to delete loading place '{loadingPlace.Name}'? This action cannot be undone.";
		_confirmAction = async () => await DeleteLoadingPlaceConfirmed(data.ProducerId, data.LoadingPlaceId);
		_showConfirmDialog = true;
	}

	private async Task DeleteLoadingPlaceConfirmed(Guid producerId, Guid loadingPlaceId)
	{
		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				var producer = _producers.FirstOrDefault(p => p.Id == producerId);
				var companyId = producer?.Company?.Id ?? Guid.Empty;
				await ApiClient.Delete($"Producers/{producerId}/loading-place/{loadingPlaceId}?companyId={companyId}");
				ToastService.ShowSuccess("Loading place deleted successfully!");
				await LoadData(false);
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to delete loading place: {ex.Message}");
			}
		});
	}

	// Confirm Dialog
	private void CloseConfirmDialog()
	{
		_showConfirmDialog = false;
		_confirmTitle = string.Empty;
		_confirmMessage = string.Empty;
		_confirmAction = null;
	}

	private async Task HandleConfirm()
	{
		_showConfirmDialog = false;
		if (_confirmAction is not null)
		{
			await _confirmAction.Invoke();
		}
		CloseConfirmDialog();
	}
}
