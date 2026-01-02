using Supabase;
using Supabase.Gotrue.Exceptions;

namespace Praxis.Services
{
    /// <summary>
    /// Service for managing Supabase PostgreSQL connection.
    /// Singleton pattern ensures only one Supabase client instance exists.
    /// 
    /// NOTE: Praxis does NOT use Supabase Authentication (auth.users).
    /// This service only manages the PostgreSQL database client for querying tables.
    /// </summary>
    public class SupabaseService
    {
        #region Singleton

        private static readonly Lazy<SupabaseService> _instance =
            new(() => new SupabaseService(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static SupabaseService Instance => _instance.Value;

        #endregion

        #region Fields

        private Client? _client;
        private bool _isInitialized;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the Supabase client is initialized and ready to use.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// The Supabase client instance. Throws if not initialized.
        /// </summary>
        public Client Client
        {
            get
            {
                if (_client == null)
                    throw new InvalidOperationException("SupabaseService not initialized. Call InitializeAsync() first.");
                return _client;
            }
        }

        #endregion

        #region Constructor

        private SupabaseService()
        {
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the Supabase PostgreSQL client.
        /// Must be called once at application startup before any database operations.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                System.Diagnostics.Debug.WriteLine("Initializing Supabase client...");

                var options = new SupabaseOptions
                {
                    AutoRefreshToken = false, // We don't use Supabase Auth
                    AutoConnectRealtime = false // Not using realtime for Praxis MVP
                };

                _client = new Client(
                    SupabaseConfig.ProjectUrl,
                    SupabaseConfig.PublishableKey,
                    options);

                await _client.InitializeAsync();

                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine("Supabase client initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize Supabase client: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Error Handling

        /// <summary>
        /// Converts Supabase exceptions to user-friendly error messages.
        /// </summary>
        public static string GetFriendlyError(Exception ex)
        {
            if (ex is GotrueException gex)
            {
                return gex.Message;
            }

            return ex.Message ?? "An unexpected error occurred.";
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Ensures the service is initialized before use.
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("SupabaseService not initialized. Call InitializeAsync() first.");
        }

        #endregion
    }
}
