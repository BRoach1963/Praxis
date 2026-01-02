using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Praxis.Models;
using Praxis.Services;

namespace Praxis.ViewModels;

/// <summary>
/// ViewModel for the client list view.
/// Displays all clients with search and filtering.
/// </summary>
public partial class ClientListViewModel : ViewModelBase
{
    private readonly IClientService _clientService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private List<Client> _clients = [];

    [ObservableProperty]
    private Client? _selectedClient;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ClientListViewModel(
        IClientService clientService,
        INavigationService navigationService)
    {
        _clientService = clientService;
        _navigationService = navigationService;
    }

    public override async Task OnNavigatedToAsync()
    {
        await LoadClientsAsync();
    }

    private async Task LoadClientsAsync()
    {
        IsLoading = true;
        try
        {
            Clients = await _clientService.GetAllClientsAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void OpenClient(Client? client)
    {
        if (client == null) return;
        _navigationService.NavigateTo<ClientDetailViewModel>(client.ClientId);
    }

    [RelayCommand]
    private void NewClient()
    {
        _navigationService.NavigateTo<ClientDetailViewModel>();
    }

    partial void OnSearchTextChanged(string value)
    {
        // TODO: Filter clients based on search text
    }
}
