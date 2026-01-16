using Microsoft.AspNetCore.Components;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Supports(ApiClient apiClient) : ComponentBase
{
    private ICollection<Suggestion> _suggestions = [];

    protected override async Task OnInitializedAsync()
    {
        _suggestions = await apiClient.Get<ICollection<Suggestion>>("Supports/suggestion/all");
    }
}
