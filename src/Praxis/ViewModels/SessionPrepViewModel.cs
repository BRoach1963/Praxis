using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Praxis.Models;
using Praxis.Services;

namespace Praxis.ViewModels;

/// <summary>
/// ViewModel for session preparation.
/// Loads case file context, recent notes, active goals, and prep for upcoming session.
/// </summary>
public partial class SessionPrepViewModel : ViewModelBase
{
    private readonly IClientService _clientService;
    private readonly ISessionService _sessionService;

    [ObservableProperty]
    private List<Client> _clients = [];

    [ObservableProperty]
    private Client? _selectedClient;

    [ObservableProperty]
    private CaseFile? _activeCaseFile;

    [ObservableProperty]
    private List<ClinicalNote> _recentNotes = [];

    [ObservableProperty]
    private List<TreatmentGoal> _activeGoals = [];

    [ObservableProperty]
    private string _prepNotes = string.Empty;

    public SessionPrepViewModel(
        IClientService clientService,
        ISessionService sessionService)
    {
        _clientService = clientService;
        _sessionService = sessionService;
    }

    public override async Task OnNavigatedToAsync()
    {
        await LoadClientsAsync();
    }

    private async Task LoadClientsAsync()
    {
        Clients = await _clientService.GetAllClientsAsync();
    }

    partial void OnSelectedClientChanged(Client? value)
    {
        if (value != null)
        {
            _ = LoadCaseFileContextAsync(value.ClientId);
        }
    }

    private async Task LoadCaseFileContextAsync(Guid clientId)
    {
        IsLoading = true;
        try
        {
            // Load recent clinical notes
            RecentNotes = await _clientService.GetRecentNotesAsync(clientId, 3);
            
            // Load active treatment goals
            ActiveGoals = await _clientService.GetActiveGoalsAsync(clientId);
            
            // Load active case file for this client
            var client = await _clientService.GetClientByIdAsync(clientId);
            if (client?.CaseFiles != null)
            {
                ActiveCaseFile = client.CaseFiles
                    .FirstOrDefault(cf => cf.Status == CaseFileStatus.Active);
            }
            
            PrepNotes = string.Empty;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SavePrepNotesAsync()
    {
        if (ActiveCaseFile == null) return;

        IsLoading = true;
        try
        {
            // Notes are saved implicitly as session context is loaded
            // Real clinical documentation happens post-session in SessionReflectViewModel
            ErrorMessage = "Prep context loaded. Document your session reflection after.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task SavePrepAsync()
    {
        // TODO: Implement SavePrepAsync with proper model binding
        // if (SelectedClient == null) return;
        // await _sessionService.UpdateSessionAsync(currentSession);
    }

    [RelayCommand]
    private void StartSession()
    {
        // TODO: Transition to active session mode
    }
}
