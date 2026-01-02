using Praxis.Models;

namespace Praxis.Services;

/// <summary>
/// Service for managing client data.
/// Works with the comprehensive model: Client → CaseFile → Sessions → ClinicalNotes → TreatmentPlans.
/// </summary>
public interface IClientService
{
    /// <summary>
    /// Get all clients, optionally filtered by active status.
    /// </summary>
    Task<List<Client>> GetAllClientsAsync(bool? activeOnly = true);
    
    /// <summary>
    /// Get a single client by ID with case files.
    /// </summary>
    Task<Client?> GetClientByIdAsync(Guid clientId);
    
    /// <summary>
    /// Create a new client.
    /// </summary>
    Task<Client> CreateClientAsync(Client client);
    
    /// <summary>
    /// Update an existing client.
    /// </summary>
    Task UpdateClientAsync(Client client);
    
    /// <summary>
    /// Get recent clinical notes for a client (across all case files).
    /// </summary>
    Task<List<ClinicalNote>> GetRecentNotesAsync(Guid clientId, int count = 5);
    
    /// <summary>
    /// Get active treatment goals for a client's current case file.
    /// </summary>
    Task<List<TreatmentGoal>> GetActiveGoalsAsync(Guid clientId);
    
    /// <summary>
    /// Search clients by name.
    /// </summary>
    Task<List<Client>> SearchClientsAsync(string searchText);
}
