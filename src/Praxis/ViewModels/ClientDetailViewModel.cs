using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Praxis.Models;
using Praxis.Services;

namespace Praxis.ViewModels;

/// <summary>
/// ViewModel for viewing and editing client details.
/// </summary>
public partial class ClientDetailViewModel : ViewModelBase
{
    private readonly IClientService _clientService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private Client _client = new();

    [ObservableProperty]
    private bool _isNewClient = true;

    [ObservableProperty]
    private List<SessionNote> _recentNotes = [];

    public ClientDetailViewModel(
        IClientService clientService,
        INavigationService navigationService)
    {
        _clientService = clientService;
        _navigationService = navigationService;
    }

    public async Task LoadClientAsync(Guid clientId)
    {
        IsLoading = true;
        try
        {
            var client = await _clientService.GetClientByIdAsync(clientId);
            if (client != null)
            {
                Client = client;
                IsNewClient = false;
                // TODO: GetRecentNotesAsync needs to return List<SessionNote> not List<ClinicalNote>
                // RecentNotes = await _clientService.GetRecentNotesAsync(clientId, 5);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsLoading = true;
        try
        {
            if (IsNewClient)
            {
                await _clientService.CreateClientAsync(Client);
            }
            else
            {
                await _clientService.UpdateClientAsync(Client);
            }
            
            _navigationService.NavigateTo<ClientListViewModel>();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save client: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _navigationService.NavigateTo<ClientListViewModel>();
    }
}
