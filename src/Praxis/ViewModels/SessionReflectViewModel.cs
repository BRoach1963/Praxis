using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Praxis.Models;
using Praxis.Services;

namespace Praxis.ViewModels;

/// <summary>
/// ViewModel for post-session clinical documentation.
/// Creates ClinicalNote (DAP, SOAP, Progress, etc) records with full clinical detail.
/// </summary>
public partial class SessionReflectViewModel : ViewModelBase
{
    private readonly IClientService _clientService;
    private readonly ISessionService _sessionService;

    [ObservableProperty]
    private List<Client> _clients = [];

    [ObservableProperty]
    private Client? _selectedClient;

    [ObservableProperty]
    private Session? _currentSession;

    [ObservableProperty]
    private ClinicalNoteType _selectedNoteType = ClinicalNoteType.Progress;

    [ObservableProperty]
    private string _clinicalContent = string.Empty;

    public SessionReflectViewModel(
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
            _ = LoadSessionContextAsync(value.ClientId);
        }
    }

    private async Task LoadSessionContextAsync(Guid clientId)
    {
        IsLoading = true;
        try
        {
            // Load the client with their active case file
            var client = await _clientService.GetClientByIdAsync(clientId);
            if (client?.CaseFiles != null)
            {
                var activeCaseFile = client.CaseFiles
                    .FirstOrDefault(cf => cf.Status == CaseFileStatus.Active);
                
                if (activeCaseFile != null)
                {
                    // Get or create a session for today
                    CurrentSession = await _sessionService.GetOrCreateSessionAsync(
                        activeCaseFile.CaseFileId, 
                        DateTime.Today);
                }
            }
            
            // Clear form for new entry
            SelectedNoteType = ClinicalNoteType.Progress;
            ClinicalContent = string.Empty;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveClinicalNoteAsync()
    {
        if (CurrentSession == null || string.IsNullOrWhiteSpace(ClinicalContent))
        {
            ErrorMessage = "Session and clinical content required.";
            return;
        }

        IsLoading = true;
        try
        {
            var note = new ClinicalNote
            {
                ClinicalNoteId = Guid.NewGuid(),
                SessionId = CurrentSession.SessionId,
                NoteType = SelectedNoteType,
                Content = ClinicalContent,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };

            await _sessionService.SaveClinicalNoteAsync(note);
            
            // Clear for next entry
            ClinicalContent = string.Empty;
            ErrorMessage = "Clinical note saved successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save clinical note: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        // TODO: Implement Clear with proper observable properties
        // ReflectionText = string.Empty;
        // KeyThemes = string.Empty;
        // FollowUpItems = string.Empty;
        // ClearError();
    }
}
