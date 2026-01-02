namespace Praxis.Models;

/// <summary>
/// Tag for flexible labeling.
/// </summary>
public class Tag
{
    public Guid TagId { get; set; } = Guid.NewGuid();

    public Guid PracticeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Category { get; set; }

    public string? Color { get; set; } // Hex

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Practice? Practice { get; set; }
    public List<EntityTag> EntityTags { get; set; } = [];
}

/// <summary>
/// Many-to-many: Tag applied to entity.
/// </summary>
public class EntityTag
{
    public Guid EntityTagId { get; set; } = Guid.NewGuid();

    public Guid TagId { get; set; }

    public EntityTagType EntityType { get; set; }

    public Guid EntityId { get; set; }

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tag? Tag { get; set; }
}

public enum EntityTagType
{
    Client,
    CaseFile,
    Session,
    ClinicalNote
}

/// <summary>
/// Operational task.
/// </summary>
public class PraxisTask
{
    public Guid TaskId { get; set; } = Guid.NewGuid();

    public Guid PracticeId { get; set; }

    public Guid? ClientId { get; set; }

    public Guid? CaseFileId { get; set; }

    public Guid? AssignedToUserProfileId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TaskStatus Status { get; set; } = TaskStatus.Open;

    public TaskPriority Priority { get; set; } = TaskPriority.Normal;

    public DateTime? DueDate { get; set; }

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    public Guid CreatedByUserProfileId { get; set; }

    public DateTime? CompletedOnUtc { get; set; }

    // Navigation
    public Practice? Practice { get; set; }
    public Client? Client { get; set; }
    public CaseFile? CaseFile { get; set; }
}

public enum TaskStatus
{
    Open,
    InProgress,
    Completed,
    Cancelled
}

public enum TaskPriority
{
    Low,
    Normal,
    High,
    Urgent
}

/// <summary>
/// Audit log entry.
/// </summary>
public class AuditLog
{
    public Guid AuditLogId { get; set; } = Guid.NewGuid();

    public Guid PracticeId { get; set; }

    public Guid? UserProfileId { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public AuditAction Action { get; set; }

    public string? Changes { get; set; } // JSON

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public string? IpAddress { get; set; }

    // Navigation
    public Practice? Practice { get; set; }
}

public enum AuditAction
{
    Created,
    Updated,
    Deleted,
    Locked,
    Archived
}

/// <summary>
/// Events for eventual cloud sync.
/// </summary>
public class Outbox
{
    public Guid OutboxId { get; set; } = Guid.NewGuid();

    public Guid PracticeId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public Guid AggregateId { get; set; }

    public string Payload { get; set; } = string.Empty; // JSON

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedOnUtc { get; set; }

    // Navigation
    public Practice? Practice { get; set; }
}

/// <summary>
/// Document reference (file stored locally).
/// </summary>
public class Document
{
    public Guid DocumentId { get; set; } = Guid.NewGuid();

    public Guid PracticeId { get; set; }

    public Guid? ClientId { get; set; }

    public Guid? CaseFileId { get; set; }

    public string DocumentType { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string LocalPath { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public string FileHash { get; set; } = string.Empty; // SHA256

    public DateTime UploadedOnUtc { get; set; } = DateTime.UtcNow;

    public Guid UploadedByUserProfileId { get; set; }

    public DateTime? SignedOnUtc { get; set; }

    public Guid? SignedByUserProfileId { get; set; }

    // Navigation
    public Practice? Practice { get; set; }
    public Client? Client { get; set; }
    public CaseFile? CaseFile { get; set; }
}
