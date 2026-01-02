using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Praxis.Services;

namespace Praxis.ViewModels;

/// <summary>
/// Main application ViewModel.
/// Manages navigation and global application state.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IThemeService _themeService;

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isDarkTheme;

    public MainViewModel(INavigationService navigationService, IThemeService themeService)
    {
        _navigationService = navigationService;
        _themeService = themeService;
        
        // Initialize theme state
        _isDarkTheme = _themeService.CurrentTheme == PraxisTheme.Dark;
        _themeService.ThemeChanged += OnThemeChanged;
        
        // Subscribe to navigation changes
        _navigationService.CurrentViewChanged += HandleNavigationChanged;
        
        // Navigate to clients by default
        _navigationService.NavigateTo<ClientListViewModel>();
    }

    private void OnThemeChanged(object? sender, PraxisTheme theme)
    {
        IsDarkTheme = theme == PraxisTheme.Dark;
    }

    private void HandleNavigationChanged(object? sender, object? view)
    {
        CurrentView = view;
    }

    [RelayCommand]
    private void NavigateToClients()
    {
        _navigationService.NavigateTo<ClientListViewModel>();
    }

    [RelayCommand]
    private void NavigateToPrep()
    {
        _navigationService.NavigateTo<SessionPrepViewModel>();
    }

    [RelayCommand]
    private void NavigateToReflect()
    {
        _navigationService.NavigateTo<SessionReflectViewModel>();
    }

    [RelayCommand]
    private void NewClient()
    {
        _navigationService.NavigateTo<ClientDetailViewModel>();
    }

    [RelayCommand]
    private void Search()
    {
        // TODO: Implement global search
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        _themeService.ToggleTheme();
    }

    [RelayCommand]
    private void SetLightTheme()
    {
        _themeService.SetTheme(PraxisTheme.Light);
    }

    [RelayCommand]
    private void SetDarkTheme()
    {
        _themeService.SetTheme(PraxisTheme.Dark);
    }
}
