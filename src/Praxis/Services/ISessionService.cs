using Praxis.Models;

namespace Praxis.Services;

/// <summary>
/// Service for managing session and clinical note data.
/// Works directly with Session and ClinicalNote entities.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Get or create a session for a case file on a specific date.
    /// </summary>
    Task<Session?> GetOrCreateSessionAsync(Guid caseFileId, DateTime sessionDate);
    
    /// <summary>
    /// Save a clinical note (create or update).
    /// </summary>
    Task SaveClinicalNoteAsync(ClinicalNote note);
    
    /// <summary>
    /// Get all clinical notes for a client (across case files).
    /// </summary>
    Task<List<ClinicalNote>> GetClientNotesAsync(Guid clientId, DateTime? from = null, DateTime? to = null);
    
    /// <summary>
    /// Get clinical notes by type.
    /// </summary>
    Task<List<ClinicalNote>> GetNotesByTypeAsync(Guid clientId, ClinicalNoteType noteType);
}
