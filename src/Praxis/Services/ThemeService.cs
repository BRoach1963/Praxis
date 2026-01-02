using System.Windows;

namespace Praxis.Services;

/// <summary>
/// Service for managing application themes.
/// Handles runtime theme switching between Light and Dark modes.
/// </summary>
public class ThemeService : IThemeService
{
    private const string LightThemeSource = "Themes/Praxis.Light.xaml";
    private const string DarkThemeSource = "Themes/Praxis.Dark.xaml";
    
    public PraxisTheme CurrentTheme { get; private set; } = PraxisTheme.Light;
    
    public event EventHandler<PraxisTheme>? ThemeChanged;

    public void SetTheme(PraxisTheme theme)
    {
        if (CurrentTheme == theme) return;
        
        var app = Application.Current;
        if (app == null) return;
        
        var mergedDictionaries = app.Resources.MergedDictionaries;
        
        // Find and remove current theme dictionary
        ResourceDictionary? themeToRemove = null;
        foreach (var dict in mergedDictionaries)
        {
            if (dict.Source != null && 
                (dict.Source.OriginalString.Contains("Praxis.Light.xaml") || 
                 dict.Source.OriginalString.Contains("Praxis.Dark.xaml")))
            {
                themeToRemove = dict;
                break;
            }
        }
        
        if (themeToRemove != null)
        {
            mergedDictionaries.Remove(themeToRemove);
        }
        
        // Add new theme dictionary
        var newThemeSource = theme == PraxisTheme.Light ? LightThemeSource : DarkThemeSource;
        var newTheme = new ResourceDictionary
        {
            Source = new Uri(newThemeSource, UriKind.Relative)
        };
        
        // Insert after Praxis.Colors.xaml (index 0) to maintain proper order
        var insertIndex = 1;
        if (mergedDictionaries.Count > insertIndex)
        {
            mergedDictionaries.Insert(insertIndex, newTheme);
        }
        else
        {
            mergedDictionaries.Add(newTheme);
        }
        
        CurrentTheme = theme;
        ThemeChanged?.Invoke(this, theme);
    }

    public void ToggleTheme()
    {
        SetTheme(CurrentTheme == PraxisTheme.Light ? PraxisTheme.Dark : PraxisTheme.Light);
    }
}
