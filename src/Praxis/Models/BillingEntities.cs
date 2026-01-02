namespace Praxis.Models;

/// <summary>
/// Billable service code.
/// </summary>
public class ServiceCode
{
    public Guid ServiceCodeId { get; set; } = Guid.NewGuid();

    public Guid PracticeId { get; set; }

    public string Code { get; set; } = string.Empty; // e.g., "90834"

    public string Description { get; set; } = string.Empty;

    public int DefaultDurationMinutes { get; set; } = 60;

    public decimal DefaultRateUsd { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Practice? Practice { get; set; }
    public List<InvoiceLine> InvoiceLines { get; set; } = [];
}

/// <summary>
/// Invoice to client or insurance.
/// </summary>
public class Invoice
{
    public Guid InvoiceId { get; set; } = Guid.NewGuid();

    public Guid PracticeId { get; set; }

    public Guid ClientId { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime IssueDate { get; set; } = DateTime.Today;

    public DateTime DueDate { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public string? Notes { get; set; }

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    public Guid CreatedByUserProfileId { get; set; }

    public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Practice? Practice { get; set; }
    public Client? Client { get; set; }
    public List<InvoiceLine> Lines { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
}

public enum InvoiceStatus
{
    Draft,
    Sent,
    Viewed,
    PartiallyPaid,
    Paid,
    Overdue,
    Cancelled
}

/// <summary>
/// Line item on invoice.
/// </summary>
public class InvoiceLine
{
    public Guid InvoiceLineId { get; set; } = Guid.NewGuid();

    public Guid InvoiceId { get; set; }

    public Guid? SessionId { get; set; }

    public Guid? ServiceCodeId { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; } = 1;

    public decimal UnitRate { get; set; }

    public decimal LineAmount => Quantity * UnitRate;

    public int LineOrder { get; set; }

    // Navigation
    public Invoice? Invoice { get; set; }
    public Session? Session { get; set; }
    public ServiceCode? ServiceCode { get; set; }
}

/// <summary>
/// Payment against invoice.
/// </summary>
public class Payment
{
    public Guid PaymentId { get; set; } = Guid.NewGuid();

    public Guid InvoiceId { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; }

    public string? Reference { get; set; }

    public DateTime PaidOnUtc { get; set; }

    public DateTime? ReceivedOnUtc { get; set; }

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    public Guid CreatedByUserProfileId { get; set; }

    // Navigation
    public Invoice? Invoice { get; set; }
}

public enum PaymentMethod
{
    Check,
    ACH,
    CreditCard,
    Cash,
    Other
}
