namespace Praxis.Services;

/// <summary>
/// Service for managing application themes.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the current theme.
    /// </summary>
    PraxisTheme CurrentTheme { get; }
    
    /// <summary>
    /// Event raised when the theme changes.
    /// </summary>
    event EventHandler<PraxisTheme>? ThemeChanged;
    
    /// <summary>
    /// Sets the application theme.
    /// </summary>
    void SetTheme(PraxisTheme theme);
    
    /// <summary>
    /// Toggles between light and dark themes.
    /// </summary>
    void ToggleTheme();
}

/// <summary>
/// Available Praxis themes.
/// </summary>
public enum PraxisTheme
{
    Light,
    Dark
}
