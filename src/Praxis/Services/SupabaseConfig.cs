namespace Praxis.Services
{
    /// <summary>
    /// Configuration for Supabase connection.
    /// Update these values with your Praxis Supabase project details.
    /// </summary>
    internal static class SupabaseConfig
    {
        /// <summary>
        /// Supabase project URL for Praxis.
        /// Replace with your actual Praxis Supabase project URL.
        /// </summary>
        internal const string ProjectUrl = "https://ewxiiqcbvsetkpiuydhw.supabase.co";

        /// <summary>
        /// Supabase publishable/public key (JWT format).
        /// This is safe to include in client apps - it only allows RLS-permitted operations.
        /// </summary>
        internal const string PublishableKey = "sb_publishable_x8djYVOoqT6tzqyB_ZP9gA_tCIzMJDw";
    }
}
