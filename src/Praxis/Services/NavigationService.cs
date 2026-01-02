using Microsoft.Extensions.DependencyInjection;
using Praxis.ViewModels;

namespace Praxis.Services;

/// <summary>
/// Simple navigation service for Praxis.
/// Manages view transitions and maintains navigation history.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Stack<object> _navigationStack = new();
    private object? _currentView;

    public event EventHandler<object?>? CurrentViewChanged;

    public bool CanGoBack => _navigationStack.Count > 0;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void NavigateTo<TViewModel>() where TViewModel : class
    {
        NavigateTo<TViewModel>(null);
    }

    public void NavigateTo<TViewModel>(object? parameter) where TViewModel : class
    {
        // Save current view to stack
        if (_currentView != null)
        {
            _navigationStack.Push(_currentView);
        }

        // Create new ViewModel
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        
        // Initialize with parameter if applicable
        if (parameter != null && viewModel is ClientDetailViewModel clientDetail && parameter is Guid clientId)
        {
            _ = clientDetail.LoadClientAsync(clientId);
        }
        
        // Call navigation lifecycle
        if (viewModel is ViewModelBase vmBase)
        {
            _ = vmBase.OnNavigatedToAsync();
        }

        _currentView = viewModel;
        CurrentViewChanged?.Invoke(this, _currentView);
    }

    public void GoBack()
    {
        if (!CanGoBack) return;

        // Call lifecycle on current
        if (_currentView is ViewModelBase currentVm)
        {
            _ = currentVm.OnNavigatedFromAsync();
        }

        _currentView = _navigationStack.Pop();
        
        // Call lifecycle on restored view
        if (_currentView is ViewModelBase restoredVm)
        {
            _ = restoredVm.OnNavigatedToAsync();
        }

        CurrentViewChanged?.Invoke(this, _currentView);
    }
}
