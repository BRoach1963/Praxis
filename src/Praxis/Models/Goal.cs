namespace Praxis.Models;

/// <summary>
/// Lightweight goal view - maps to TreatmentGoal in new model.
/// Kept for backward compatibility with ViewModels.
/// </summary>
public class Goal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid ClientId { get; set; }
    
    public Guid? TreatmentPlanId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public GoalStatus Status { get; set; } = GoalStatus.Active;
    
    public string? ProgressNotes { get; set; }
    
    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;
    
    public int DisplayOrder { get; set; }
    
    // Navigation
    public Client? Client { get; set; }
}

public enum GoalStatus
{
    Active,
    Achieved,
    Modified,
    Discontinued
}
