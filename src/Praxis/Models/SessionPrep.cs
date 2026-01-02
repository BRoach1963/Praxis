namespace Praxis.Models;

/// <summary>
/// Lightweight preparation view - maps to Session upcoming in CaseFile context.
/// For ViewModel use only; not a distinct DB entity in new model.
/// </summary>
public class SessionPrep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid ClientId { get; set; }
    
    public Guid CaseFileId { get; set; }
    
    public Guid SessionId { get; set; }
    
    public DateTime SessionDate { get; set; }
    
    public string? Notes { get; set; }
    
    public string? CheckInItems { get; set; }
    
    public string? PlannedInterventions { get; set; }
    
    public bool IsCompleted { get; set; }
    
    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public Client? Client { get; set; }
}
