using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Praxis.Data;
using Praxis.Models;

namespace Praxis.Services;

/// <summary>
/// Service for managing client data.
/// Works directly with the comprehensive model: Client → CaseFile → Sessions → ClinicalNotes.
/// </summary>
public class ClientService : IClientService
{
    private readonly IServiceProvider _serviceProvider;

    public ClientService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private PraxisDbContext CreateDbContext()
    {
        return _serviceProvider.GetRequiredService<PraxisDbContext>();
    }

    public async Task<List<Client>> GetAllClientsAsync(bool? activeOnly = true)
    {
        using var db = CreateDbContext();
        
        var query = db.Clients.AsQueryable();
        
        if (activeOnly == true)
        {
            query = query.Where(c => c.Status == ClientStatus.Active);
        }
        
        return await query
            .OrderBy(c => c.PreferredName ?? c.FirstName)
            .ToListAsync();
    }

    public async Task<Client?> GetClientByIdAsync(Guid clientId)
    {
        using var db = CreateDbContext();
        
        return await db.Clients
            .Include(c => c.CaseFiles)
            .FirstOrDefaultAsync(c => c.ClientId == clientId);
    }

    public async Task<Client> CreateClientAsync(Client client)
    {
        using var db = CreateDbContext();
        
        client.ClientId = Guid.NewGuid();
        client.CreatedOnUtc = DateTime.UtcNow;
        client.UpdatedOnUtc = DateTime.UtcNow;
        
        db.Clients.Add(client);
        await db.SaveChangesAsync();
        
        return client;
    }

    public async Task UpdateClientAsync(Client client)
    {
        using var db = CreateDbContext();
        
        client.UpdatedOnUtc = DateTime.UtcNow;
        db.Clients.Update(client);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Get recent clinical notes for a client (across all case files).
    /// </summary>
    public async Task<List<ClinicalNote>> GetRecentNotesAsync(Guid clientId, int count = 5)
    {
        using var db = CreateDbContext();
        
        return await db.ClinicalNotes
            .Include(n => n.Session)
            .Where(n => n.Session.CaseFile.ClientId == clientId)
            .OrderByDescending(n => n.CreatedOnUtc)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>
    /// Get active treatment goals for a client's current case file.
    /// </summary>
    public async Task<List<TreatmentGoal>> GetActiveGoalsAsync(Guid clientId)
    {
        using var db = CreateDbContext();
        
        // Get active case file (or most recent)
        var activeCaseFile = await db.CaseFiles
            .Where(cf => cf.ClientId == clientId && cf.Status == CaseFileStatus.Active)
            .OrderByDescending(cf => cf.StartDate)
            .FirstOrDefaultAsync();
        
        if (activeCaseFile == null)
            return [];
        
        return await db.TreatmentGoals
            .Include(g => g.TreatmentPlan)
            .Where(g => g.TreatmentPlan.CaseFileId == activeCaseFile.CaseFileId && 
                        g.Status == TreatmentGoalStatus.Active)
            //.OrderBy(g => g.DisplayOrder)
            .ToListAsync();
    }

    public async Task<List<Client>> SearchClientsAsync(string searchText)
    {
        using var db = CreateDbContext();
        
        var lowerSearch = searchText.ToLowerInvariant();
        
        return await db.Clients
            .Where(c => (c.PreferredName != null && c.PreferredName.ToLower().Contains(lowerSearch)) ||
                        (c.FirstName + " " + c.LastName).ToLower().Contains(lowerSearch))
            .OrderBy(c => c.PreferredName ?? c.FirstName)
            .ToListAsync();
    }
}
