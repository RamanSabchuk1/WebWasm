using Microsoft.AspNetCore.Components;
using WebWasm.Components;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Providers
{
	[Inject] private CashService CashService { get; set; } = default!;
	[Inject] private ApiClient ApiClient { get; set; } = default!;
	[Inject] private ToastService ToastService { get; set; } = default!;
	[Inject] private LoadingService LoadingService { get; set; } = default!;

	private List<Producer> _producers = [];
	private List<Company> _companies = [];
	private List<MaterialType> _materialTypes = [];

	// Producer Modal
	private bool _isProducerModalOpen = false;
	private Producer? _editingProducer = null;

	// LoadingPlace Modal
	private bool _isLoadingPlaceModalOpen = false;
	private LoadingPlace? _editingLoadingPlace = null;
	private Guid _currentProducerId = Guid.Empty;

	// Confirm Dialog
	private readonly ConfirmState _confirm = new();

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
		await LoadingService.Run(ToastService, async () =>
		{
			if (_editingProducer is not null)
			{
				// Update producer
				var companyId = _editingProducer.Company?.Id ?? Guid.Empty;
				var updateProducer = new UpdateProducer(data.WorkingTimes);
				await ApiClient.Put($"Producers/{_editingProducer.Id}?companyId={companyId}", updateProducer);
				ToastService.ShowSuccess(T.Providers.Updated);
			}
			else
			{
				// Create producer
				var companyId = data.CompanyId ?? Guid.Empty;
				var createProducer = new CreateProducer(data.WorkingTimes, data.Name);
				await ApiClient.Post($"Producers?companyId={companyId}", createProducer);
				ToastService.ShowSuccess(T.Providers.Created);
			}

			await LoadData(false);
			CloseProducerModal();
		}, T.Providers.SaveFailed);
	}

	private void HandleDeleteProducer(Guid producerId)
	{
		var producer = _producers.FirstOrDefault(p => p.Id == producerId);
		if (producer is null)
		{
			return;
		}

		var loadingPlacesCount = producer.LoadingPlaces.Count;
		_confirm.Ask(T.Providers.DeleteTitle, loadingPlacesCount > 0
			? T.Providers.DeleteWithPlacesMessage(producer.Name, loadingPlacesCount)
			: T.Providers.DeleteMessage(producer.Name), async () => await DeleteProducerConfirmed(producerId));
	}

	private async Task DeleteProducerConfirmed(Guid producerId)
	{
		await LoadingService.Run(ToastService, async () =>
		{
			var producer = _producers.FirstOrDefault(p => p.Id == producerId);
			var companyId = producer?.Company?.Id ?? Guid.Empty;
			await ApiClient.Delete($"Producers/{producerId}?companyId={companyId}");
			ToastService.ShowSuccess(T.Providers.DeletedToast);
			await LoadData(false);
		}, T.Providers.DeleteFailed);
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
		await LoadingService.Run(ToastService, async () =>
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
				ToastService.ShowSuccess(T.Providers.PlaceUpdated);
			}
			else
			{
				// Create loading place
				await ApiClient.Post(
					$"Producers/{_currentProducerId}/loading-place?companyId={companyId}",
					loadingPlaceData
				);
				ToastService.ShowSuccess(T.Providers.PlaceCreated);
			}

			await LoadData(false);
			CloseLoadingPlaceModal();
		}, T.Providers.PlaceSaveFailed);
	}

	private void HandleDeleteLoadingPlace((Guid ProducerId, Guid LoadingPlaceId) data)
	{
		var producer = _producers.FirstOrDefault(p => p.Id == data.ProducerId);
		var loadingPlace = producer?.LoadingPlaces.FirstOrDefault(lp => lp.Id == data.LoadingPlaceId);

		if (loadingPlace is null)
		{
			return;
		}

		_confirm.Ask(T.Providers.DeletePlaceTitle, T.Providers.DeletePlaceMessage(loadingPlace.Name), async () => await DeleteLoadingPlaceConfirmed(data.ProducerId, data.LoadingPlaceId));
	}

	private async Task DeleteLoadingPlaceConfirmed(Guid producerId, Guid loadingPlaceId)
	{
		await LoadingService.Run(ToastService, async () =>
		{
			var producer = _producers.FirstOrDefault(p => p.Id == producerId);
			var companyId = producer?.Company?.Id ?? Guid.Empty;
			await ApiClient.Delete($"Producers/{producerId}/loading-place/{loadingPlaceId}?companyId={companyId}");
			ToastService.ShowSuccess(T.Providers.PlaceDeleted);
			await LoadData(false);
		}, T.Providers.PlaceDeleteFailed);
	}
}
