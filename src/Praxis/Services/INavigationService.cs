namespace Praxis.Services;

/// <summary>
/// Service for navigating between views in Praxis.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Event raised when the current view changes.
    /// </summary>
    event EventHandler<object?>? CurrentViewChanged;
    
    /// <summary>
    /// Navigate to a view by ViewModel type.
    /// </summary>
    void NavigateTo<TViewModel>() where TViewModel : class;
    
    /// <summary>
    /// Navigate to a view with a parameter (e.g., client ID).
    /// </summary>
    void NavigateTo<TViewModel>(object? parameter) where TViewModel : class;
    
    /// <summary>
    /// Navigate back to the previous view.
    /// </summary>
    void GoBack();
    
    /// <summary>
    /// Whether navigation back is possible.
    /// </summary>
    bool CanGoBack { get; }
}
