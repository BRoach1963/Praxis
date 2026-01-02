# Tracker WPF Application - Authentication & Session Management Analysis

## Overview
This document provides a comprehensive analysis of the Tracker WPF application's authentication, session management, and login flows based on code examination.

---

## 1. CurrentUser Session Storage

### **Session Storage Pattern: NOT Static in App.xaml.cs**
The Tracker application uses a **distributed session management approach** rather than simple static properties:

#### Key Storage Locations:

**a) Supabase Cloud Backend (Primary)**
- **Location**: `Services/Backend/SupabaseService.cs` (singleton)
- **Properties**:
  ```csharp
  public bool IsSignedIn => _client?.Auth.CurrentUser != null;
  public User? CurrentUser => _client?.Auth.CurrentUser;
  public UserProfile? CurrentProfile { get; private set; }
  public UserSubscription? CurrentSubscription { get; private set; }
  public string? AccessToken => _client?.Auth.CurrentSession?.AccessToken;
  ```

**b) Secure Local Storage (Encrypted)**
- **Location**: `Services/Backend/SecureTokenStorage.cs` (static helper)
- **Files Stored in**: `%LOCALAPPDATA%\Tracker\auth\`
  - `rt.dat` - Refresh token (encrypted with Windows DPAPI)
  - `pwd.dat` - Password for "Remember Me" (encrypted with Windows DPAPI)
  - `slack.dat` - Slack OAuth token (encrypted)

**c) User Settings (Per-User)**
- **Location**: `Managers/UserSettingsManager.Instance`
- **Stores**:
  - `CurrentUser` display name
  - `Settings.Authentication.SavedEmail`
  - `Settings.Authentication.CloudUserId`
  - `Settings.Authentication.CloudAccountLinked` flag
  - `Settings.Authentication.RememberMe` flag

#### Session Restoration Flow:
```
App Startup
  ↓
InitializeAsync()
  ↓
SupabaseService.InitializeAsync()
  ↓
TryRestoreSessionAsync()
  ├─ Reads SecureTokenStorage.GetRefreshToken()
  ├─ Calls Auth.RefreshSession()
  ├─ On success: UserSettingsManager.SwitchToUser(userId, isNewAccount: false)
  └─ Loads CurrentProfile & CurrentSubscription
```

---

## 2. LoginWindow Architecture

### **Actual Implementation: LoginDialog (Not LoginWindow)**
The app uses a **LoginDialog** modal instead of a separate window.

#### **XAML Structure** (`Views/Dialogs/LoginDialog.xaml`)

```xaml
<controls:BaseWindow x:Class="Tracker.Views.Dialogs.LoginDialog"
    Title="Tracker"
    Height="650" Width="420"
    WindowStyle="None"
    AllowsTransparency="True"
    Background="Transparent"
    ResizeMode="NoResize"
    ShowInTaskbar="True">

    <Border Background="{DynamicResource BackgroundBrush}"
            BorderBrush="{DynamicResource AccentBrush}"
            BorderThickness="2"
            CornerRadius="12">
        
        <!-- Header with Logo, Title, Tagline -->
        <Border Grid.Row="0" Background="{DynamicResource AccentBrush}">
            <!-- Icon + "Tracker" Title + Tagline -->
        </Border>

        <!-- Content Area with ScrollViewer -->
        <ScrollViewer Grid.Row="1">
            <StackPanel>
                <!-- Mode Toggle Buttons: "Sign In" / "Create Account" -->
                <!-- Display Name (Create Account only) -->
                <!-- Email TextBox -->
                <!-- Password Box + TextBox + Show/Hide Button -->
                <!-- Confirm Password (Create Account only) -->
                <!-- Remember Me Checkbox -->
                <!-- Admin Login Checkbox -->
                <!-- Status Message (Error/Success) -->
                <!-- Primary Action Button (Sign In / Create Account) -->
                <!-- Forgot Password Button (Sign In only) -->
                <!-- Processing Indicator (Hourglass + "Please wait...") -->
                <!-- Cancel & Exit Buttons -->
                <!-- Help Section (Help Center / Contact Support links) -->
            </StackPanel>
        </ScrollViewer>
    </Border>
</controls:BaseWindow>
```

#### **Code-Behind Logic** (`Views/Dialogs/LoginDialog.xaml.cs`)

Key features:
- **PasswordBox Management**: Dual controls (PasswordBox for security + TextBox for visibility toggle)
- **Password Visibility Toggle**: Show/Hide button swaps between PasswordBox and TextBox
- **Mode Switching**: Updates button styles dynamically when mode changes
- **Enter Key Handling**:
  - In Sign In mode: Execute sign-in
  - In Create Account mode: Move to confirm password field
  - In confirm password: Execute create account

```csharp
private bool _isPasswordVisible;
private bool _isConfirmPasswordVisible;
private bool _isSyncing; // Prevent infinite loops

// Password sync between PasswordBox and TextBox
private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
{
    if (_isSyncing) return;
    if (DataContext is LoginDialogViewModel vm && sender is PasswordBox pb)
    {
        _isSyncing = true;
        vm.Password = pb.Password;
        PasswordTextBox.Text = pb.Password;
        _isSyncing = false;
    }
}

// Show/Hide Password
private void ShowPasswordButton_Click(object sender, RoutedEventArgs e)
{
    _isPasswordVisible = !_isPasswordVisible;
    if (_isPasswordVisible)
    {
        PasswordTextBox.Text = PasswordBox.Password;
        PasswordBox.Visibility = Visibility.Collapsed;
        PasswordTextBox.Visibility = Visibility.Visible;
    }
    else
    {
        PasswordBox.Password = PasswordTextBox.Text;
        PasswordTextBox.Visibility = Visibility.Collapsed;
        PasswordBox.Visibility = Visibility.Visible;
    }
}
```

---

## 3. Authentication Service - Password Validation

### **SupabaseService (Services/Backend/SupabaseService.cs)**

#### **Sign In Flow**:
```csharp
public async Task<(bool Success, string? Error)> SignInAsync(string email, string password)
{
    EnsureInitialized();
    
    try
    {
        var session = await _client!.Auth.SignIn(email, password);
        
        if (session?.User != null)
        {
            await SaveSessionAsync();        // Save refresh token
            await LoadUserDataAsync();        // Load profile
            await UpdateLastLoginAsync();     // Track login
            await RegisterInstallationAsync(); // Device tracking
            return (true, null);
        }
        
        return (false, "Sign in failed. Please check your credentials.");
    }
    catch (GotrueException ex)
    {
        return (false, GetFriendlyAuthError(ex));
    }
}

// Error mapping for user-friendly messages
private static string GetFriendlyAuthError(GotrueException ex)
{
    var message = ex.Message.ToLower();
    
    if (message.Contains("invalid login"))
        return "Invalid email or password. Please try again.";
    
    if (message.Contains("email not confirmed"))
        return "Please check your email and confirm your account.";
    
    if (message.Contains("user already registered"))
        return "An account with this email already exists. Try signing in instead.";
    
    if (message.Contains("password"))
        return "Password must be at least 6 characters.";
    
    if (message.Contains("rate limit"))
        return "Too many attempts. Please wait a moment and try again.";
    
    return ex.Message;
}
```

#### **Password Validation Rules**:
- ✅ Email format checked with regex: `^[^@\s]+@[^@\s]+\.[^@\s]+$`
- ✅ Password minimum 6 characters
- ✅ Create account: Passwords must match
- ✅ All validation done **client-side** before calling Supabase
- ⚠️ **No hashing done locally** - passwords sent to Supabase as plaintext over HTTPS (Supabase handles hashing server-side)

#### **Create Account Flow**:
```csharp
public async Task<(bool Success, string? Error)> SignUpAsync(
    string email,
    string password,
    string? displayName = null)
{
    try
    {
        var session = await _client!.Auth.SignUp(email, password, new SignUpOptions
        {
            Data = new Dictionary<string, object>
            {
                ["display_name"] = displayName ?? email.Split('@')[0]
            }
        });
        
        if (session?.User != null)
        {
            CurrentProfile = null;
            CurrentSubscription = null;
            
            await SaveSessionAsync();
            await LoadUserDataAsync();
            
            // Create default free subscription
            if (CurrentSubscription == null)
            {
                await CreateFreeSubscriptionAsync(session.User.Id);
            }
            
            return (true, null);
        }
    }
    catch (GotrueException ex)
    {
        return (false, GetFriendlyAuthError(ex));
    }
}
```

---

## 4. Password Reset Flow

### **Password Reset Implementation** (`LoginDialogViewModel.cs`)

The password reset is **email-based** with server-side handling:

```csharp
private async void ExecuteForgotPassword(object? parameter)
{
    if (string.IsNullOrWhiteSpace(Email))
    {
        SetStatus("Enter your email address first", true);
        return;
    }
    
    IsProcessing = true;
    
    try
    {
        if (!SupabaseService.Instance.IsInitialized)
        {
            await SupabaseService.Instance.InitializeAsync();
        }
        
        var (success, error) = await SupabaseService.Instance.ResetPasswordAsync(Email);
        
        if (success)
        {
            SetStatus("Password reset email sent! Check your inbox.", false);
        }
        else
        {
            SetStatus(error ?? "Failed to send reset email", true);
        }
    }
    finally
    {
        IsProcessing = false;
    }
}
```

#### **SupabaseService.ResetPasswordAsync()**:
```csharp
public async Task<(bool Success, string? Error)> ResetPasswordAsync(string email)
{
    try
    {
        var options = new Supabase.Gotrue.ResetPasswordForEmailOptions(email)
        {
            RedirectTo = "https://www.pricklycactussoftware.com/password-reset"
        };
        
        await _client!.Auth.ResetPasswordForEmail(options);
        return (true, null);
    }
    catch (GotrueException ex)
    {
        return (false, GetFriendlyAuthError(ex));
    }
}
```

#### **Password Reset Flow (UI Perspective)**:
1. User clicks "Forgot password?" on login dialog
2. Enters email address
3. Calls `SupabaseService.ResetPasswordAsync(email)`
4. Supabase sends email with reset link to configured redirect URL
5. **Redirect URL**: `https://www.pricklycactussoftware.com/password-reset`
6. User clicks link in email → taken to web form
7. User sets new password on web portal
8. Returns to app to log in with new password

#### **No In-App Password Reset UI**:
- The WPF app **does not have** a password reset dialog
- Reset is handled by **Supabase email template** system
- Configured in Supabase Dashboard → Authentication → Email Templates

---

## 5. SupabaseClient Instantiation & Management

### **Service Location**: `Services/Backend/SupabaseService.cs` (Singleton)

#### **Initialization Pattern**:
```csharp
public class SupabaseService
{
    private static readonly Lazy<SupabaseService> _instance =
        new(() => new SupabaseService(), LazyThreadSafetyMode.ExecutionAndPublication);
    
    public static SupabaseService Instance => _instance.Value;
    
    private Supabase.Client? _client;
    private bool _isInitialized;
    
    private SupabaseService() { }  // Private constructor
}
```

#### **Initialize Call**:
```csharp
public async Task InitializeAsync()
{
    if (_isInitialized) return;
    
    try
    {
        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = false // Not needed for now
        };
        
        _client = new Supabase.Client(
            SupabaseConfig.ProjectUrl,
            SupabaseConfig.AnonKey,
            options);
        
        await _client.InitializeAsync();
        
        // Listen for auth state changes
        _client.Auth.AddStateChangedListener(OnAuthStateChanged);
        
        _isInitialized = true;
        
        // Try to restore session from stored token
        await TryRestoreSessionAsync();
    }
}
```

#### **Configuration** (`Services/Backend/SupabaseConfig.cs`):
```csharp
public static class SupabaseConfig
{
    // Credentials stored here (not shown in code, injected at build time)
    public static string ProjectUrl { get; }
    public static string AnonKey { get; }
    public static string AvatarBucket { get; }
    public static int MaxAvatarSizeBytes { get; }
}
```

#### **Initialization in App Startup**:
Located in `App.xaml.cs` - `ContinueNormalStartup()` method, Stage 3.6:
```csharp
// Stage 3.6: Initialize cloud services
_splashScreen?.UpdateStatus("Connecting to cloud services...");
_splashScreen?.UpdateProgress(85);
try
{
    await Services.Backend.SupabaseService.Instance.InitializeAsync();
    await Services.Subscription.SubscriptionService.Instance.ValidateWithBackendAsync();
}
catch (Exception ex)
{
    LoggingManager.GetComponentLogger("App").Warn("Cloud services unavailable: {0}", ex.Message);
}
```

---

## 6. Theme/Styling Applied to Login Screen

### **Theme System**: Dynamic Resource Dictionary

#### **App.xaml - Theme Registration**:
```xaml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="Resources/Themes/DefaultTheme.xaml" />
            <ResourceDictionary Source="Resources/Icons.xaml" />
            <ResourceDictionary Source="Resources/Styles.xaml" />
            <ResourceDictionary Source="Resources/Styles/DialogStyles.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

#### **Available Themes** (`Resources/Themes/`):
- `DefaultTheme.xaml` - Applied at startup
- `LightTheme.xaml`
- `ModernTheme.xaml`
- `SpicyTheme.xaml`

#### **Theme Manager** (DeepEndControls):
```csharp
// In App.xaml.cs OnAppStartup():
private void InitializeTheme()
{
    UserSettingsManager.Instance.Initialize();
    ThemeManager.Instance.Initialize(UserSettingsManager.Instance.Settings.Theme);
}
```

#### **Login Dialog Theme Resources Used**:
```xaml
<!-- Colors referenced in LoginDialog.xaml -->
{DynamicResource BackgroundBrush}      <!-- Main window background -->
{DynamicResource AccentBrush}          <!-- Header & button accent color -->
{DynamicResource ForegroundBrush}      <!-- Text color -->
{DynamicResource HintTextBrush}        <!-- Hint/muted text -->
{DynamicResource SurfaceBrush}         <!-- Secondary surface -->
{DynamicResource BorderBrush}          <!-- Borders -->

<!-- Button Styles -->
{StaticResource PrimaryButtonStyle}    <!-- Main action buttons -->
{StaticResource LinkButtonStyle}       <!-- Hyperlink-style buttons -->

<!-- Converters -->
{x:Static converters:BoolToVisibilityConverter.Instance}
{StaticResource BoolToErrorBackgroundConverter}
{StaticResource BoolToErrorForegroundConverter}
```

#### **Visual Design Characteristics**:
- **Rounded corners**: `CornerRadius="12"` on border
- **Modern layout**: Header area with accent color
- **Transparent background**: `AllowsTransparency="True"`
- **No window chrome**: `WindowStyle="None"`
- **Responsive**: Uses `ScrollViewer` for small screens
- **High contrast**: Dynamic brushes adapt to theme

---

## 7. App Transition: LoginWindow → MainWindow

### **Startup Sequence** (in `App.xaml.cs`)

#### **1. Initial Startup Check**:
```csharp
private async void OnAppStartup(object sender, StartupEventArgs e)
{
    ShutdownMode = ShutdownMode.OnExplicitShutdown; // Prevent auto-close
    
    InitializeTheme();
    
    // If first-time setup not completed, show wizard
    if (!UserSettingsManager.Instance.Settings.Database.SetupCompleted)
    {
        ShowSetupWizard();
        return;
    }
    
    // Normal flow
    await ContinueNormalStartup();
}
```

#### **2. Normal Startup Flow**:
```
Show Splash Screen with progress indicators
  ↓
InitializeApplicationAsync() - stages:
  ├─ 20%: Initialize logging
  ├─ 50%: Connect to database
  ├─ 70%: Load data
  ├─ 80%: Initialize help system
  ├─ 82%: Initialize AI insights
  ├─ 85%: Connect to cloud services (Supabase)
  ├─ 95%: Restore integrations
  └─ 100%: Ready
  ↓
Check if already authenticated (from stored session)
  │
  ├─→ YES (IsSignedIn) 
  │     └─ Hide splash
  │     └─ Show LoadingWindow (500ms)
  │     └─ Call LaunchMainWindow()
  │
  └─→ NO
        └─ Show LoginDialog
        └─ Wait for login success
        └─ On success: LaunchMainWindow()
```

#### **3. MainWindow Launch**:
```csharp
private void LaunchMainWindow(bool isAdminLogin = false)
{
    try
    {
        // Create ViewModel
        var viewModel = App.ViewModelFactory.Create<TrackerMainViewModel>();
        
        // Create MainWindow with ViewModel
        var mainWindow = new MainWindow(viewModel);
        mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        
        // Handle --minimized argument (Windows startup)
        if (_startMinimized)
        {
            mainWindow.WindowState = WindowState.Minimized;
            mainWindow.ShowInTaskbar = false;
            Application.Current.MainWindow = mainWindow;
        }
        else
        {
            mainWindow.Show();
            Application.Current.MainWindow = mainWindow;
        }
        
        // Close loading window
        _loadingWindow?.CloseWithFade();
    }
    catch (Exception ex)
    {
        LoggingManager.GetComponentLogger("App").Exception(ex, "MainWindow creation failed");
        MessageBoxHelper.Show($"Failed to create main window: {ex.Message}", "Fatal Error", 
            MessageBoxButton.OK, MessageBoxImage.Error);
        Shutdown();
    }
}
```

#### **4. Post-Login Initialization**:
After successful login (LoginDialog closes):
```csharp
loginWindow.Closed += (s, e) =>
{
    if (loginCompletedSuccessfully)
    {
        ShutdownMode = ShutdownMode.OnLastWindowClose; // Now allow normal close
        
        // Show LoadingWindow briefly
        _loadingWindow = new Views.LoadingWindow();
        _loadingWindow.Show();
        _loadingWindow.Activate();
        _loadingWindow.Topmost = true;
        
        // After delay, launch main window
        Task.Run(async () =>
        {
            await Task.Delay(500);
            Dispatcher.Invoke(() => LaunchMainWindow());
        });
    }
    else
    {
        // Login cancelled - shut down
        Shutdown();
    }
};
```

### **Key Architectural Points**:
- ✅ **No standalone LoginWindow** - uses **modal LoginDialog** instead
- ✅ **Splash screen** shows progress during initialization
- ✅ **LoadingWindow** acts as transition between login and main app
- ✅ **ShutdownMode** switches from `OnExplicitShutdown` → `OnLastWindowClose` after auth succeeds
- ✅ **Session restoration** happens transparently - if user has stored session, MainWindow shows directly
- ✅ **Admin mode** parameter passed through to launch different features

---

## 8. Password Hashing & Validation Utilities

### **No Client-Side Password Hashing**

The app **deliberately does NOT hash passwords locally**:

```csharp
// LoginDialogViewModel.cs - ExecuteSignIn()
var (success, error) = await SupabaseService.Instance.SignInAsync(Email, Password);
// Password sent as plaintext to Supabase over HTTPS
```

### **Why This Design**:
1. **Supabase handles hashing** server-side with bcrypt
2. **HTTPS encryption** protects plaintext in transit
3. **Client-side hashing would be false security** (no benefit over HTTPS)
4. **Supabase manages password updates/resets** server-side

### **Secure Credential Storage** (Client-Side Only):

#### **Windows DPAPI (Data Protection API)**:
Two implementations use DPAPI for encryption:

**1. SecureTokenStorage** (`Services/Backend/SecureTokenStorage.cs`):
```csharp
private static void SaveEncrypted(string filePath, string data)
{
    var plainBytes = Encoding.UTF8.GetBytes(data);
    var encryptedBytes = ProtectedData.Protect(
        plainBytes,
        null,
        DataProtectionScope.CurrentUser);  // Only this Windows user can decrypt
    
    File.WriteAllBytes(filePath, encryptedBytes);
}

private static string? GetEncrypted(string filePath)
{
    var encryptedBytes = File.ReadAllBytes(filePath);
    var plainBytes = ProtectedData.Unprotect(
        encryptedBytes,
        null,
        DataProtectionScope.CurrentUser);  // Fails if different user
    
    return Encoding.UTF8.GetString(plainBytes);
}
```

**2. TokenEncryptionService** (`Services/TokenEncryptionService.cs`):
```csharp
public string? Encrypt(string? plainText)
{
    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
    byte[] encryptedBytes = ProtectedData.Protect(
        plainBytes,
        null,
        DataProtectionScope.CurrentUser);  // DPAPI encryption
    
    return Convert.ToBase64String(encryptedBytes);
}

public string? Decrypt(string? encryptedText)
{
    byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
    byte[] plainBytes = ProtectedData.Unprotect(
        encryptedBytes,
        null,
        DataProtectionScope.CurrentUser);
    
    return Encoding.UTF8.GetString(plainBytes);
}
```

### **What Gets Encrypted**:
- ✅ **Refresh tokens** → `%LOCALAPPDATA%\Tracker\auth\rt.dat`
- ✅ **Passwords** (if "Remember Me" checked) → `%LOCALAPPDATA%\Tracker\auth\pwd.dat`
- ✅ **OAuth tokens** (Slack, Google, Microsoft) → `%LOCALAPPDATA%\Tracker\auth\*.dat`

### **Device ID Hashing** (for installation tracking):
```csharp
private static string GetDeviceId()
{
    var raw = $"{Environment.MachineName}-{Environment.UserName}-{Environment.ProcessorCount}";
    
    using var sha256 = SHA256.Create();
    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(raw));
    return Convert.ToBase64String(hash)[..32];  // Take first 32 chars
}
```

---

## Summary Table: Key Components

| Aspect | Implementation | Location |
|--------|---|---|
| **Session Storage** | Distributed: Supabase + Local encrypted files + Settings | SupabaseService, SecureTokenStorage, UserSettingsManager |
| **Login UI** | Modal LoginDialog (not separate window) | Views/Dialogs/LoginDialog.xaml |
| **Auth Service** | Supabase GoTrue API wrapper | Services/Backend/SupabaseService.cs |
| **Password Validation** | Email format + min 6 chars (client-side) | LoginDialogViewModel.ExecuteSignIn/CreateAccount |
| **Password Reset** | Email-based with server redirect | SupabaseService.ResetPasswordAsync() |
| **Supabase Client** | Singleton lazy-initialized | Services/Backend/SupabaseService.cs |
| **Encryption** | Windows DPAPI (CurrentUser scope) | SecureTokenStorage, TokenEncryptionService |
| **Theme System** | Dynamic ResourceDictionary with ThemeManager | Resources/Themes/, DeepEndControls |
| **App Transition** | Splash → Initialize → Auth Check → Loading → MainWindow | App.xaml.cs OnAppStartup/ContinueNormalStartup |
| **Password Hashing** | Server-side (Supabase bcrypt) - NOT client-side | SupabaseService delegates to Supabase |

---

## Key Code Patterns & Best Practices Found

### ✅ **Good Patterns**:
1. **Async/Await throughout** - No blocking calls
2. **Error Mapping** - User-friendly messages from Gotrue exceptions
3. **Session Restoration** - Silent automatic login if valid token exists
4. **DPAPI Encryption** - Credentials protected by Windows user scope
5. **Singleton Pattern** - SupabaseService managed as single instance
6. **DI Container** - ServiceCollection for dependency injection
7. **Modal Dialog** - LoginDialog blocks interaction with main app during auth
8. **Progress Indication** - Splash screen shows initialization stages

### ⚠️ **Considerations**:
1. **No client-side password hashing** - Intentional (Supabase does it)
2. **"Remember Me" stores plaintext** - Then encrypted; still recoverable locally
3. **No in-app password reset UI** - Redirects to web portal
4. **Admin mode flag** - Doesn't re-authenticate; relies on IsAdmin property from Supabase profile
5. **Install tracking** - Device hash sent to Supabase (privacy-conscious but tracked)

