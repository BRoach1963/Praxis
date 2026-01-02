using Praxis.Models;

namespace Praxis.Services
{
    /// <summary>
    /// Manages the current user session and practice context.
    /// Provides thread-safe access to CurrentSession information.
    /// </summary>
    public class SessionManager
    {
        private static readonly Lazy<SessionManager> _instance =
            new(() => new SessionManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static SessionManager Instance => _instance.Value;

        private CurrentSession? _currentSession;
        private readonly object _lockObject = new object();

        /// <summary>
        /// Fired when the current session changes (login/logout).
        /// </summary>
        public event EventHandler<CurrentSession?>? SessionChanged;

        private SessionManager()
        {
        }

        /// <summary>
        /// Gets the current session, or null if user is not logged in.
        /// </summary>
        public CurrentSession? CurrentSession
        {
            get
            {
                lock (_lockObject)
                {
                    return _currentSession;
                }
            }
        }

        /// <summary>
        /// Gets the currently logged-in user, or null if not authenticated.
        /// </summary>
        public AppUser? CurrentUser => CurrentSession?.User;

        /// <summary>
        /// Gets the current practice context, or null if not set.
        /// </summary>
        public PracticeInfo? CurrentPractice => CurrentSession?.CurrentPractice;

        /// <summary>
        /// Gets the current user's role in the current practice.
        /// </summary>
        public PracticeRole? CurrentRole => CurrentSession?.Role;

        /// <summary>
        /// Returns true if a user is currently authenticated.
        /// </summary>
        public bool IsAuthenticated => CurrentSession != null;

        /// <summary>
        /// Sets the current session after successful login.
        /// </summary>
        public void SetSession(CurrentSession session)
        {
            lock (_lockObject)
            {
                _currentSession = session;
                SessionChanged?.Invoke(this, session);
            }
        }

        /// <summary>
        /// Switches the current practice context.
        /// </summary>
        public void SwitchPractice(PracticeInfo practice)
        {
            lock (_lockObject)
            {
                if (_currentSession != null)
                {
                    _currentSession.CurrentPractice = practice;
                    SessionChanged?.Invoke(this, _currentSession);
                }
            }
        }

        /// <summary>
        /// Clears the current session (logout).
        /// </summary>
        public void ClearSession()
        {
            lock (_lockObject)
            {
                _currentSession = null;
                SessionChanged?.Invoke(this, null);
            }
        }

        /// <summary>
        /// Updates the access token (typically after refresh).
        /// </summary>
        public void UpdateAccessToken(string accessToken, DateTime expiresAt)
        {
            lock (_lockObject)
            {
                if (_currentSession != null)
                {
                    _currentSession.AccessToken = accessToken;
                    _currentSession.TokenExpiresAt = expiresAt;
                }
            }
        }
    }
}
