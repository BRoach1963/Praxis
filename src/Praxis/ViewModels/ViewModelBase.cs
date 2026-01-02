using CommunityToolkit.Mvvm.ComponentModel;

namespace Praxis.ViewModels;

/// <summary>
/// Base class for all ViewModels in Praxis.
/// Provides common functionality and MVVM infrastructure.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Called when the view is navigated to.
    /// Override to load data or initialize state.
    /// </summary>
    public virtual Task OnNavigatedToAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the view is navigated away from.
    /// Override to save state or cleanup.
    /// </summary>
    public virtual Task OnNavigatedFromAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears any error message.
    /// </summary>
    protected void ClearError()
    {
        ErrorMessage = null;
    }
}
