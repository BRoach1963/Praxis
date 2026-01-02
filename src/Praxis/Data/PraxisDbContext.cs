using System.IO;
using Microsoft.EntityFrameworkCore;
using Praxis.Models;

namespace Praxis.Data;

/// <summary>
/// Entity Framework DbContext for Praxis.
/// Uses SQLite for local-first data storage.
/// All data is practice-scoped (multi-tenant).
/// </summary>
public class PraxisDbContext : DbContext
{
    // Organizational
    public DbSet<Practice> Practices => Set<Practice>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<PracticeUser> PracticeUsers => Set<PracticeUser>();
    public DbSet<Therapist> Therapists => Set<Therapist>();

    // Clients & Relationships
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ClientAssignment> ClientAssignments => Set<ClientAssignment>();

    // Episodes of Care
    public DbSet<CaseFile> CaseFiles => Set<CaseFile>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<ClinicalNote> ClinicalNotes => Set<ClinicalNote>();

    // Treatment Planning
    public DbSet<TreatmentPlan> TreatmentPlans => Set<TreatmentPlan>();
    public DbSet<TreatmentGoal> TreatmentGoals => Set<TreatmentGoal>();
    public DbSet<TreatmentIntervention> TreatmentInterventions => Set<TreatmentIntervention>();

    // Assessments
    public DbSet<Assessment> Assessments => Set<Assessment>();

    // Scheduling
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AvailabilityRule> AvailabilityRules => Set<AvailabilityRule>();

    // Billing
    public DbSet<ServiceCode> ServiceCodes => Set<ServiceCode>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Payment> Payments => Set<Payment>();

    // Cross-cutting
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<EntityTag> EntityTags => Set<EntityTag>();
    public DbSet<PraxisTask> PraxisTasks => Set<PraxisTask>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Outbox> Outbox => Set<Outbox>();
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Store database in user's local app data for privacy and portability
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var praxisDataPath = Path.Combine(appDataPath, "Praxis");
        
        // Ensure directory exists
        Directory.CreateDirectory(praxisDataPath);
        
        var dbPath = Path.Combine(praxisDataPath, "praxis.db");
        
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Practice
        modelBuilder.Entity<Practice>(entity =>
        {
            entity.HasKey(e => e.PracticeId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsDeleted);
        });

        // UserProfile
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.UserProfileId);
            entity.HasIndex(e => e.AuthUserId).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.IsActive);
        });

        // PracticeUser
        modelBuilder.Entity<PracticeUser>(entity =>
        {
            entity.HasKey(e => e.PracticeUserId);
            entity.HasIndex(e => new { e.PracticeId, e.UserProfileId }).IsUnique();
            entity.HasIndex(e => e.PracticeId);
            entity.HasIndex(e => e.UserProfileId);
            entity.HasOne(e => e.Practice).WithMany(p => p.PracticeUsers).HasForeignKey(e => e.PracticeId);
            entity.HasOne(e => e.UserProfile).WithMany(u => u.PracticeUsers).HasForeignKey(e => e.UserProfileId);
        });

        // Therapist
        modelBuilder.Entity<Therapist>(entity =>
        {
            entity.HasKey(e => e.TherapistId);
            entity.HasIndex(e => new { e.PracticeId, e.IsActive });
            entity.HasIndex(e => e.NPI);
            entity.HasOne(e => e.Practice).WithMany(p => p.Therapists).HasForeignKey(e => e.PracticeId);
        });

        // Client
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.ClientId);
            entity.HasIndex(e => new { e.PracticeId, e.LastName, e.FirstName });
            entity.HasIndex(e => new { e.PracticeId, e.Status });
            entity.HasOne(e => e.Practice).WithMany(p => p.Clients).HasForeignKey(e => e.PracticeId);
        });

        // ClientAssignment
        modelBuilder.Entity<ClientAssignment>(entity =>
        {
            entity.HasKey(e => e.ClientAssignmentId);
            entity.HasIndex(e => new { e.ClientId, e.TherapistId });
            entity.HasIndex(e => e.TherapistId);
            entity.HasOne(e => e.Client).WithMany(c => c.Assignments).HasForeignKey(e => e.ClientId);
            entity.HasOne(e => e.Therapist).WithMany(t => t.ClientAssignments).HasForeignKey(e => e.TherapistId);
        });

        // CaseFile
        modelBuilder.Entity<CaseFile>(entity =>
        {
            entity.HasKey(e => e.CaseFileId);
            entity.HasIndex(e => new { e.ClientId, e.StartDate });
            entity.HasIndex(e => e.PrimaryTherapistId);
            entity.HasOne(e => e.Client).WithMany(c => c.CaseFiles).HasForeignKey(e => e.ClientId);
            entity.HasOne(e => e.PrimaryTherapist).WithMany(t => t.CaseFilesAsPrimary).HasForeignKey(e => e.PrimaryTherapistId);
        });

        // Session
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            entity.HasIndex(e => new { e.CaseFileId, e.StartUtc });
            entity.HasIndex(e => new { e.TherapistId, e.StartUtc });
            entity.HasOne(e => e.CaseFile).WithMany(c => c.Sessions).HasForeignKey(e => e.CaseFileId);
            entity.HasOne(e => e.Therapist).WithMany(t => t.Sessions).HasForeignKey(e => e.TherapistId);
        });

        // ClinicalNote
        modelBuilder.Entity<ClinicalNote>(entity =>
        {
            entity.HasKey(e => e.ClinicalNoteId);
            entity.HasIndex(e => e.SessionId).IsUnique();
            entity.HasOne(e => e.Session).WithOne(s => s.ClinicalNote).HasForeignKey<ClinicalNote>(e => e.SessionId);
        });

        // TreatmentPlan
        modelBuilder.Entity<TreatmentPlan>(entity =>
        {
            entity.HasKey(e => e.TreatmentPlanId);
            entity.HasIndex(e => e.CaseFileId);
            entity.HasOne(e => e.CaseFile).WithMany(c => c.TreatmentPlans).HasForeignKey(e => e.CaseFileId);
        });

        // TreatmentGoal
        modelBuilder.Entity<TreatmentGoal>(entity =>
        {
            entity.HasKey(e => e.TreatmentGoalId);
            entity.HasIndex(e => new { e.TreatmentPlanId, e.Status });
            entity.HasOne(e => e.TreatmentPlan).WithMany(tp => tp.Goals).HasForeignKey(e => e.TreatmentPlanId);
        });

        // TreatmentIntervention
        modelBuilder.Entity<TreatmentIntervention>(entity =>
        {
            entity.HasKey(e => e.TreatmentInterventionId);
            entity.HasIndex(e => e.TreatmentGoalId);
            entity.HasOne(e => e.Goal).WithMany(g => g.Interventions).HasForeignKey(e => e.TreatmentGoalId);
        });

        // Assessment
        modelBuilder.Entity<Assessment>(entity =>
        {
            entity.HasKey(e => e.AssessmentId);
            entity.HasIndex(e => new { e.CaseFileId, e.Instrument, e.CompletedOnUtc });
            entity.HasOne(e => e.CaseFile).WithMany(c => c.Assessments).HasForeignKey(e => e.CaseFileId);
        });

        // Appointment
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.AppointmentId);
            entity.HasIndex(e => new { e.TherapistId, e.StartUtc });
            entity.HasIndex(e => new { e.ClientId, e.StartUtc });
            entity.HasOne(e => e.Client).WithMany().HasForeignKey(e => e.ClientId);
            entity.HasOne(e => e.Therapist).WithMany().HasForeignKey(e => e.TherapistId);
        });

        // AvailabilityRule
        modelBuilder.Entity<AvailabilityRule>(entity =>
        {
            entity.HasKey(e => e.AvailabilityRuleId);
            entity.HasIndex(e => new { e.TherapistId, e.DayOfWeek });
            entity.HasOne(e => e.Therapist).WithMany(t => t.AvailabilityRules).HasForeignKey(e => e.TherapistId);
        });

        // ServiceCode
        modelBuilder.Entity<ServiceCode>(entity =>
        {
            entity.HasKey(e => e.ServiceCodeId);
            entity.HasIndex(e => new { e.PracticeId, e.Code }).IsUnique();
            entity.HasOne(e => e.Practice).WithMany(p => p.ServiceCodes).HasForeignKey(e => e.PracticeId);
        });

        // Invoice
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId);
            entity.HasIndex(e => new { e.PracticeId, e.InvoiceNumber }).IsUnique();
            entity.HasIndex(e => new { e.ClientId, e.IssueDate });
            entity.HasIndex(e => e.Status);
            entity.HasOne(e => e.Practice).WithMany(p => p.Invoices).HasForeignKey(e => e.PracticeId);
        });

        // InvoiceLine
        modelBuilder.Entity<InvoiceLine>(entity =>
        {
            entity.HasKey(e => e.InvoiceLineId);
            entity.HasIndex(e => e.InvoiceId);
            entity.HasIndex(e => e.SessionId);
            entity.HasOne(e => e.Invoice).WithMany(i => i.Lines).HasForeignKey(e => e.InvoiceId);
            entity.HasOne(e => e.Session).WithOne(s => s.InvoiceLine).HasForeignKey<InvoiceLine>(e => e.SessionId);
        });

        // Payment
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId);
            entity.HasIndex(e => new { e.InvoiceId, e.PaidOnUtc });
            entity.HasOne(e => e.Invoice).WithMany(i => i.Payments).HasForeignKey(e => e.InvoiceId);
        });

        // Tag
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.TagId);
            entity.HasIndex(e => new { e.PracticeId, e.Name }).IsUnique();
            entity.HasOne(e => e.Practice).WithMany(p => p.Tags).HasForeignKey(e => e.PracticeId);
        });

        // EntityTag
        modelBuilder.Entity<EntityTag>(entity =>
        {
            entity.HasKey(e => e.EntityTagId);
            entity.HasIndex(e => new { e.TagId, e.EntityType, e.EntityId }).IsUnique();
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasOne(e => e.Tag).WithMany(t => t.EntityTags).HasForeignKey(e => e.TagId);
        });

        // Task
        modelBuilder.Entity<PraxisTask>(entity =>
        {
            entity.HasKey(e => e.TaskId);
            entity.HasIndex(e => new { e.PracticeId, e.Status, e.DueDate });
            entity.HasIndex(e => new { e.AssignedToUserProfileId, e.Status });
            entity.HasOne(e => e.Practice).WithMany(p => p.Tasks).HasForeignKey(e => e.PracticeId);
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditLogId);
            entity.HasIndex(e => new { e.PracticeId, e.TimestampUtc });
            entity.HasIndex(e => new { e.EntityType, e.EntityId, e.TimestampUtc });
            entity.HasOne(e => e.Practice).WithMany(p => p.AuditLogs).HasForeignKey(e => e.PracticeId);
        });

        // Outbox
        modelBuilder.Entity<Outbox>(entity =>
        {
            entity.HasKey(e => e.OutboxId);
            entity.HasIndex(e => new { e.PracticeId, e.ProcessedOnUtc });
            entity.HasOne(e => e.Practice).WithMany().HasForeignKey(e => e.PracticeId);
        });

        // Document
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.DocumentId);
            entity.HasIndex(e => e.ClientId);
            entity.HasIndex(e => new { e.CaseFileId, e.DocumentType });
            entity.HasIndex(e => e.UploadedOnUtc);
            entity.HasOne(e => e.Practice).WithMany().HasForeignKey(e => e.PracticeId);
        });
    }
}
