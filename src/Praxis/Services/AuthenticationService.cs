using Praxis.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Praxis.Services
{
    /// <summary>
    /// JSON model for app_user table responses.
    /// </summary>
    public class AppUserRow
    {
        [JsonPropertyName("app_user_id")]
        public Guid AppUserId { get; set; }
        
        [JsonPropertyName("email")]
        public string? Email { get; set; }
        
        [JsonPropertyName("password_hash")]
        public string? PasswordHash { get; set; }
        
        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }
        
        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }
        
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }
        
        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }
        
        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }
        
        [JsonPropertyName("must_change_password")]
        public bool MustChangePassword { get; set; }
        
        [JsonPropertyName("created_on_utc")]
        public DateTime CreatedOnUtc { get; set; }
        
        [JsonPropertyName("updated_on_utc")]
        public DateTime UpdatedOnUtc { get; set; }
        
        [JsonPropertyName("last_login_utc")]
        public DateTime? LastLoginUtc { get; set; }
    }

    /// <summary>
    /// JSON model for practice_user table.
    /// </summary>
    public class PracticeUserRow
    {
        [JsonPropertyName("practice_user_id")]
        public Guid PracticeUserId { get; set; }
        
        [JsonPropertyName("practice_id")]
        public Guid PracticeId { get; set; }
        
        [JsonPropertyName("app_user_id")]
        public Guid AppUserId { get; set; }
        
        [JsonPropertyName("role")]
        public string? Role { get; set; }
        
        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// JSON model for practice table.
    /// </summary>
    public class PracticeRow
    {
        [JsonPropertyName("practice_id")]
        public Guid PracticeId { get; set; }
        
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("time_zone")]
        public string? TimeZone { get; set; }
        
        [JsonPropertyName("default_currency")]
        public string? DefaultCurrency { get; set; }
        
        [JsonPropertyName("default_session_length_minutes")]
        public int DefaultSessionLengthMinutes { get; set; }
        
        [JsonPropertyName("phone")]
        public string? Phone { get; set; }
        
        [JsonPropertyName("address_line_1")]
        public string? AddressLine1 { get; set; }
        
        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Service for authentication operations against Supabase PostgreSQL.
    /// Handles login, password validation, and password reset.
    /// 
    /// NOTE: Praxis uses APPLICATION-MANAGED authentication (not Supabase Auth).
    /// User credentials are stored in the app_user table with bcrypt password_hash.
    /// Uses HttpClient with publishable key in apikey header (new Supabase format).
    /// </summary>
    public class AuthenticationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public AuthenticationService()
        {
            _baseUrl = $"{SupabaseConfig.ProjectUrl}/rest/v1";
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("apikey", SupabaseConfig.PublishableKey);
            _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
        }

        /// <summary>
        /// Attempts to log in a user with email and password.
        /// Queries the app_user table, validates bcrypt password hash.
        /// </summary>
        public async Task<LoginResponse> LoginAsync(string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    return new LoginResponse
                    {
                        Success = false,
                        ErrorMessage = "Email and password are required."
                    };
                }

                // Query Supabase app_user table for user by email using PostgREST
                var url = $"{_baseUrl}/app_user?email=eq.{Uri.EscapeDataString(email.ToLower())}&select=*";
                System.Diagnostics.Debug.WriteLine($"LOGIN: Querying URL: {url}");
                
                var response = await _httpClient.GetAsync(url);
                var responseBody = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"LOGIN: Response status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"LOGIN: Response body: {responseBody}");
                
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"Supabase error: {response.StatusCode} - {responseBody}");
                    return new LoginResponse
                    {
                        Success = false,
                        ErrorMessage = $"Login failed: {response.StatusCode}"
                    };
                }

                var users = JsonSerializer.Deserialize<List<AppUserRow>>(responseBody);
                System.Diagnostics.Debug.WriteLine($"LOGIN: Found {users?.Count ?? 0} users");
                
                if (users == null || users.Count == 0)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid email or password."
                    };
                }

                var appUserRow = users[0];
                System.Diagnostics.Debug.WriteLine($"LOGIN: User found - Email: {appUserRow.Email}, Hash: {appUserRow.PasswordHash?.Substring(0, 20)}...");

                // Validate password using bcrypt
                System.Diagnostics.Debug.WriteLine($"LOGIN: Verifying password against hash");
                bool passwordValid = BCrypt.Net.BCrypt.Verify(password, appUserRow.PasswordHash ?? "");
                System.Diagnostics.Debug.WriteLine($"LOGIN: Password valid: {passwordValid}");
                if (!passwordValid)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid email or password."
                    };
                }

                // Check if user is active
                if (!appUserRow.IsActive)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        ErrorMessage = "This account has been deactivated. Please contact your administrator."
                    };
                }

                // Convert to AppUser model for response
                var user = new AppUser
                {
                    AppUserId = appUserRow.AppUserId,
                    Email = appUserRow.Email ?? "",
                    FirstName = appUserRow.FirstName ?? "",
                    LastName = appUserRow.LastName ?? "",
                    DisplayName = appUserRow.DisplayName ?? "",
                    AvatarUrl = appUserRow.AvatarUrl,
                    IsActive = appUserRow.IsActive,
                    MustChangePassword = appUserRow.MustChangePassword,
                    CreatedOnUtc = appUserRow.CreatedOnUtc,
                    UpdatedOnUtc = appUserRow.UpdatedOnUtc,
                    LastLoginUtc = appUserRow.LastLoginUtc
                };

                // Get user's accessible practices
                var practices = await GetUserPracticesAsync(user.AppUserId);

                return new LoginResponse
                {
                    Success = true,
                    User = user,
                    UserPractices = practices,
                    MustChangePassword = user.MustChangePassword
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return new LoginResponse
                {
                    Success = false,
                    ErrorMessage = $"Login failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Changes a user's password.
        /// </summary>
        public async Task<(bool Success, string? Error)> ChangePasswordAsync(
            Guid appUserId,
            string currentPassword,
            string newPassword)
        {
            try
            {
                // Validate password format
                if (!IsPasswordValid(newPassword))
                {
                    return (false, "Password must be at least 6 characters long.");
                }

                // Query Supabase to get current user and verify current password
                var url = $"{_baseUrl}/app_user?app_user_id=eq.{appUserId}&select=*";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return (false, "User not found.");
                }

                var users = await response.Content.ReadFromJsonAsync<List<AppUserRow>>();
                if (users == null || users.Count == 0)
                {
                    return (false, "User not found.");
                }

                var appUserRow = users[0];

                // Verify current password
                bool passwordValid = BCrypt.Net.BCrypt.Verify(currentPassword, appUserRow.PasswordHash ?? "");
                if (!passwordValid)
                {
                    return (false, "Current password is incorrect.");
                }

                // Hash new password
                string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

                // Update password and clear must_change_password flag via PATCH
                var patchUrl = $"{_baseUrl}/app_user?app_user_id=eq.{appUserId}";
                var patchContent = new StringContent(
                    JsonSerializer.Serialize(new { 
                        password_hash = newPasswordHash, 
                        must_change_password = false,
                        updated_on_utc = DateTime.UtcNow
                    }),
                    System.Text.Encoding.UTF8,
                    "application/json");
                
                var patchResponse = await _httpClient.PatchAsync(patchUrl, patchContent);
                if (!patchResponse.IsSuccessStatusCode)
                {
                    return (false, "Failed to update password.");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Password change error: {ex.Message}");
                return (false, "An error occurred while changing your password.");
            }
        }

        /// <summary>
        /// Initiates a password reset by creating a reset token.
        /// In a real app, this would send an email with the token.
        /// </summary>
        public async Task<(bool Success, string? Error, string? ResetToken)> RequestPasswordResetAsync(string email)
        {
            try
            {
                // Find user by email
                var url = $"{_baseUrl}/app_user?email=eq.{Uri.EscapeDataString(email.ToLower())}&select=app_user_id";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    // Don't reveal if user exists (security best practice)
                    return (true, null, null);
                }

                var users = await response.Content.ReadFromJsonAsync<List<AppUserRow>>();
                if (users == null || users.Count == 0)
                {
                    // Don't reveal if user exists (security best practice)
                    return (true, null, null);
                }

                // In production, would call Supabase function: generate_password_reset_token
                // For now, return success - actual token generation happens server-side
                return (true, null, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Password reset request error: {ex.Message}");
                return (false, "An error occurred. Please try again.", null);
            }
        }

        /// <summary>
        /// Gets all practices a user has access to via practice_user table.
        /// </summary>
        private async Task<List<PracticeInfo>> GetUserPracticesAsync(Guid appUserId)
        {
            try
            {
                // Query practice_user to find all practices for this user
                var puUrl = $"{_baseUrl}/practice_user?app_user_id=eq.{appUserId}&is_active=eq.true&select=practice_id,role";
                var puResponse = await _httpClient.GetAsync(puUrl);

                if (!puResponse.IsSuccessStatusCode)
                {
                    return new List<PracticeInfo>();
                }

                var practiceUsers = await puResponse.Content.ReadFromJsonAsync<List<PracticeUserRow>>();
                if (practiceUsers == null || practiceUsers.Count == 0)
                {
                    return new List<PracticeInfo>();
                }

                var practiceIds = practiceUsers.Select(pu => pu.PracticeId).ToList();
                
                // Get practice details - use 'in' filter for multiple IDs
                var idsParam = string.Join(",", practiceIds.Select(id => $"\"{id}\""));
                var pUrl = $"{_baseUrl}/practice?practice_id=in.({idsParam})&is_active=eq.true&select=*";
                var pResponse = await _httpClient.GetAsync(pUrl);

                if (!pResponse.IsSuccessStatusCode)
                {
                    return new List<PracticeInfo>();
                }

                var practiceRows = await pResponse.Content.ReadFromJsonAsync<List<PracticeRow>>();
                if (practiceRows == null || practiceRows.Count == 0)
                {
                    return new List<PracticeInfo>();
                }

                var practices = practiceRows.Select(p => new PracticeInfo
                {
                    PracticeId = p.PracticeId,
                    Name = p.Name ?? "",
                    TimeZone = p.TimeZone ?? "",
                    DefaultCurrency = p.DefaultCurrency ?? "",
                    DefaultSessionLengthMinutes = p.DefaultSessionLengthMinutes,
                    Phone = p.Phone,
                    Address = p.AddressLine1
                }).ToList();

                return practices;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching user practices: {ex.Message}");
                return new List<PracticeInfo>();
            }
        }

        /// <summary>
        /// Validates password format (client-side validation before sending to server).
        /// </summary>
        public static bool IsPasswordValid(string password)
        {
            // Minimum 6 characters (Tracker standard)
            return !string.IsNullOrEmpty(password) && password.Length >= 6;
        }

        /// <summary>
        /// Validates email format.
        /// </summary>
        public static bool IsEmailValid(string email)
        {
            // Simple regex from Tracker
            var regex = new System.Text.RegularExpressions.Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");
            return regex.IsMatch(email);
        }
    }
}
