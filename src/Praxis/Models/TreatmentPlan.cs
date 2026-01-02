namespace Praxis.Models;

/// <summary>
/// Treatment plan for a case file.
/// </summary>
public class TreatmentPlan
{
    public Guid TreatmentPlanId { get; set; } = Guid.NewGuid();

    public Guid CaseFileId { get; set; }

    public int PlanVersion { get; set; } = 1;

    public string Summary { get; set; } = string.Empty;

    public TreatmentPlanStatus Status { get; set; } = TreatmentPlanStatus.Draft;

    public DateTime EffectiveDate { get; set; } = DateTime.Today;

    public DateTime? ReviewDate { get; set; }

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    public Guid CreatedByUserProfileId { get; set; }

    // Navigation
    public CaseFile? CaseFile { get; set; }
    public List<TreatmentGoal> Goals { get; set; } = [];
}

public enum TreatmentPlanStatus
{
    Draft,
    Active,
    Archived
}

/// <summary>
/// Specific therapeutic goal.
/// </summary>
public class TreatmentGoal
{
    public Guid TreatmentGoalId { get; set; } = Guid.NewGuid();

    public Guid TreatmentPlanId { get; set; }

    public string GoalText { get; set; } = string.Empty;

    public DateTime TargetDate { get; set; }

    public TreatmentGoalStatus Status { get; set; } = TreatmentGoalStatus.Active;

    public string? MeasurementMethod { get; set; }

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;

    public Guid CreatedByUserProfileId { get; set; }

    // Navigation
    public TreatmentPlan? TreatmentPlan { get; set; }
    public List<TreatmentIntervention> Interventions { get; set; } = [];
}

public enum TreatmentGoalStatus
{
    Active,
    Achieved,
    Modified,
    Discontinued
}

/// <summary>
/// Specific intervention toward a goal.
/// </summary>
public class TreatmentIntervention
{
    public Guid TreatmentInterventionId { get; set; } = Guid.NewGuid();

    public Guid TreatmentGoalId { get; set; }

    public string InterventionText { get; set; } = string.Empty;

    public string? Frequency { get; set; }

    public TreatmentInterventionStatus Status { get; set; } = TreatmentInterventionStatus.Active;

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public TreatmentGoal? Goal { get; set; }
}

public enum TreatmentInterventionStatus
{
    Active,
    Completed,
    OnHold
}
