using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Praxis.Data;
using Praxis.Models;

namespace Praxis.Services;

/// <summary>
/// Service for managing session data.
/// Works with Session → ClinicalNote directly. No lightweight wrapper concepts.
/// </summary>
public class SessionService : ISessionService
{
    private readonly IServiceProvider _serviceProvider;

    public SessionService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private PraxisDbContext CreateDbContext()
    {
        return _serviceProvider.GetRequiredService<PraxisDbContext>();
    }

    /// <summary>
    /// Get or create a session for a given case file on a specific date.
    /// </summary>
    public async Task<Session?> GetOrCreateSessionAsync(Guid caseFileId, DateTime sessionDate)
    {
        using var db = CreateDbContext();
        
        // Try to find existing session for this date
        var existing = await db.Sessions
            .Include(s => s.ClinicalNote)
            .FirstOrDefaultAsync(s => s.CaseFileId == caseFileId && 
                                       s.StartUtc.Date == sessionDate.Date);
        
        if (existing != null)
            return existing;
        
        // Create new session (scheduled for today)
        var session = new Session
        {
            SessionId = Guid.NewGuid(),
            CaseFileId = caseFileId,
            StartUtc = DateTime.UtcNow,
            Status = SessionStatus.Scheduled
        };
        
        db.Sessions.Add(session);
        await db.SaveChangesAsync();
        
        return session;
    }

    /// <summary>
    /// Save a clinical note for a session (creates if new, updates if existing).
    /// </summary>
    public async Task SaveClinicalNoteAsync(ClinicalNote note)
    {
        using var db = CreateDbContext();
        
        var existing = await db.ClinicalNotes.FindAsync(note.ClinicalNoteId);
        if (existing != null)
        {
            // Update existing
            note.UpdatedOnUtc = DateTime.UtcNow;
            db.Entry(existing).CurrentValues.SetValues(note);
        }
        else
        {
            // Create new
            note.CreatedOnUtc = DateTime.UtcNow;
            note.UpdatedOnUtc = DateTime.UtcNow;
            db.ClinicalNotes.Add(note);
        }
        
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Get clinical notes for a client across all case files.
    /// </summary>
    public async Task<List<ClinicalNote>> GetClientNotesAsync(Guid clientId, DateTime? from = null, DateTime? to = null)
    {
        using var db = CreateDbContext();
        
        var query = db.ClinicalNotes
            .Include(n => n.Session)
            .Where(n => n.Session.CaseFile.ClientId == clientId);
        
        if (from.HasValue)
            query = query.Where(n => n.CreatedOnUtc >= from);
        
        if (to.HasValue)
            query = query.Where(n => n.CreatedOnUtc <= to);
        
        return await query
            .OrderByDescending(n => n.CreatedOnUtc)
            .ToListAsync();
    }

    /// <summary>
    /// Get clinical notes by type (DAP, SOAP, Progress, etc).
    /// </summary>
    public async Task<List<ClinicalNote>> GetNotesByTypeAsync(Guid clientId, ClinicalNoteType noteType)
    {
        using var db = CreateDbContext();
        
        return await db.ClinicalNotes
            .Include(n => n.Session)
            .Where(n => n.Session.CaseFile.ClientId == clientId && n.NoteType == noteType)
            .OrderByDescending(n => n.CreatedOnUtc)
            .ToListAsync();
    }
}
