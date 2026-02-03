using Microsoft.AspNetCore.Components;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class ProducerModal : ComponentBase
{
	[Parameter] public bool IsOpen { get; set; }
	[Parameter] public Producer? EditingProducer { get; set; }
	[Parameter] public List<Company> Companies { get; set; } = [];
	[Parameter] public EventCallback OnClose { get; set; }
	[Parameter] public EventCallback<(Guid? CompanyId, string Name, ICollection<ProducerWorkingTime> WorkingTimes)> OnSubmit { get; set; }

	private string _name = string.Empty;
	private Guid? _companyId = null;
	private List<ProducerWorkingTimeEntry> _workingTimeEntries = [];
	private string _errorMessage = string.Empty;

	protected override void OnParametersSet()
	{
		if (IsOpen)
		{
			if (EditingProducer is not null)
			{
				_name = EditingProducer.Name;
				_companyId = EditingProducer.Company?.Id;
				_workingTimeEntries = EditingProducer.ProducerWorkingTime
					.Select(wt => new ProducerWorkingTimeEntry(wt))
					.ToList();
			}
			else
			{
				ResetForm();
			}
		}
	}

	private void ResetForm()
	{
		_name = string.Empty;
		_companyId = null;
		_workingTimeEntries = [];
		_errorMessage = string.Empty;
	}

	private void AddWorkingTimeEntry()
	{
		_workingTimeEntries.Add(new ProducerWorkingTimeEntry());
	}

	private void RemoveWorkingTimeEntry(int index)
	{
		if (index >= 0 && index < _workingTimeEntries.Count)
		{
			_workingTimeEntries.RemoveAt(index);
		}
	}

	private bool IsValid =>
		!string.IsNullOrWhiteSpace(_name) &&
		_workingTimeEntries.Count > 0 &&
		_workingTimeEntries.All(e => e.IsValid());

	private async Task HandleSubmit()
	{
		if (!IsValid)
		{
			_errorMessage = "Please fill in producer name and add at least one working time entry";
			return;
		}

		var workingTimes = _workingTimeEntries.Select(e => e.ToProducerWorkingTime()).ToList();

		await OnSubmit.InvokeAsync((_companyId, _name, workingTimes));
		ResetForm();
	}

	private async Task CloseModal()
	{
		ResetForm();
		await OnClose.InvokeAsync();
	}

	private bool IsEditMode => EditingProducer is not null;

	// Helper class to manage working time entries
	private class ProducerWorkingTimeEntry
	{
		public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Monday;
		public TimeOnly StartLoadingHours { get; set; } = new TimeOnly(8, 0);
		public TimeOnly EndLoadingHours { get; set; } = new TimeOnly(12, 0);
		public TimeOnly StartWorkingHours { get; set; } = new TimeOnly(8, 0);
		public TimeOnly EndWorkingHours { get; set; } = new TimeOnly(17, 0);

		public ProducerWorkingTimeEntry() { }

		public ProducerWorkingTimeEntry(ProducerWorkingTime wt)
		{
			DayOfWeek = wt.DayOfWeek;
			StartLoadingHours = wt.StartLoadingHours;
			EndLoadingHours = wt.EndLoadingHours;
			StartWorkingHours = wt.StartWorkingHours;
			EndWorkingHours = wt.EndWorkingHours;
		}

		public bool IsValid() =>
			StartLoadingHours < EndLoadingHours &&
			StartWorkingHours < EndWorkingHours;

		public ProducerWorkingTime ToProducerWorkingTime() =>
			new ProducerWorkingTime(
				StartLoadingHours,
				EndLoadingHours,
				StartWorkingHours,
				EndWorkingHours,
				DayOfWeek
			);
	}
}
