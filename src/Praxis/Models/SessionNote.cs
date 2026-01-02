namespace Praxis.Models;

/// <summary>
/// Lightweight session note view - maps to ClinicalNote in new model.
/// Kept for backward compatibility with ViewModels.
/// </summary>
public class SessionNote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid ClientId { get; set; }
    
    public Guid SessionId { get; set; }
    
    public DateTime SessionDate { get; set; }
    
    public string? PrivateReflection { get; set; }
    
    public string? Themes { get; set; }
    
    public string? FollowUp { get; set; }
    
    public string? Summary { get; set; }
    
    public SessionMood? Mood { get; set; }
    
    public string? Interventions { get; set; }
    
    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;
    
    public Client? Client { get; set; }
}

public enum SessionMood
{
    None,
    Difficult,
    Neutral,
    Positive,
    Breakthrough
}
