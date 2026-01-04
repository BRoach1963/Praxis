using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Praxis.Data.Entities;

namespace Praxis.Data;

/// <summary>
/// Entity Framework DbContext for Praxis using PostgreSQL.
/// Maps to the local PostgreSQL praxis database.
/// </summary>
public class PraxisContext : DbContext
{
    private readonly string _connectionString;

    public PraxisContext()
    {
        // Default connection string for design-time and standalone usage
        _connectionString = "Host=localhost;Port=5432;Database=praxis;Username=postgres;Password=$teelers4Ever";
    }

    public PraxisContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public PraxisContext(DbContextOptions<PraxisContext> options) : base(options)
    {
        _connectionString = string.Empty; // Will use options
    }

    // Core entities
    public DbSet<Firm> Firms => Set<Firm>();
    public DbSet<UserProfileEntity> UserProfiles => Set<UserProfileEntity>();
    public DbSet<FirmUser> FirmUsers => Set<FirmUser>();
    public DbSet<Entities.Therapist> Therapists => Set<Entities.Therapist>();
    public DbSet<Entities.Client> Clients => Set<Entities.Client>();
    public DbSet<TherapistClient> TherapistClients => Set<TherapistClient>();
    public DbSet<Entities.CaseFile> CaseFiles => Set<Entities.CaseFile>();
    public DbSet<Entities.Session> Sessions => Set<Entities.Session>();
    public DbSet<KeyRing> KeyRings => Set<KeyRing>();
    public DbSet<Entities.ClinicalNote> ClinicalNotes => Set<Entities.ClinicalNote>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql(_connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Firm
        modelBuilder.Entity<Firm>(entity =>
        {
            entity.HasKey(e => e.FirmId);
            entity.HasIndex(e => e.Name).IsUnique().HasFilter("is_deleted = false");
        });

        // UserProfile
        modelBuilder.Entity<UserProfileEntity>(entity =>
        {
            entity.HasKey(e => e.UserProfileId);
            entity.HasIndex(e => e.AuthUserId).IsUnique().HasFilter("is_deleted = false");
            entity.HasIndex(e => e.Email).IsUnique().HasFilter("is_deleted = false");
        });

        // FirmUser
        modelBuilder.Entity<FirmUser>(entity =>
        {
            entity.HasKey(e => e.FirmUserId);
            entity.HasIndex(e => new { e.FirmId, e.UserProfileId }).IsUnique().HasFilter("is_deleted = false");
            entity.HasOne(e => e.Firm).WithMany(f => f.FirmUsers).HasForeignKey(e => e.FirmId);
            entity.HasOne(e => e.UserProfile).WithMany(u => u.FirmUsers).HasForeignKey(e => e.UserProfileId);
        });

        // Therapist
        modelBuilder.Entity<Entities.Therapist>(entity =>
        {
            entity.HasKey(e => e.TherapistId);
            entity.HasIndex(e => e.FirmUserId).IsUnique().HasFilter("is_deleted = false");
            entity.HasOne(e => e.Firm).WithMany(f => f.Therapists).HasForeignKey(e => e.FirmId);
            entity.HasOne(e => e.FirmUser).WithOne(fu => fu.Therapist).HasForeignKey<Entities.Therapist>(e => e.FirmUserId);
        });

        // Client
        modelBuilder.Entity<Entities.Client>(entity =>
        {
            entity.HasKey(e => e.ClientId);
            entity.HasIndex(e => new { e.FirmId, e.LastName, e.FirstName }).HasFilter("is_deleted = false");
            entity.HasOne(e => e.Firm).WithMany(f => f.Clients).HasForeignKey(e => e.FirmId);
        });

        // TherapistClient
        modelBuilder.Entity<TherapistClient>(entity =>
        {
            entity.HasKey(e => e.TherapistClientId);
            entity.HasOne(e => e.Therapist).WithMany(t => t.TherapistClients).HasForeignKey(e => e.TherapistId);
            entity.HasOne(e => e.Client).WithMany(c => c.TherapistClients).HasForeignKey(e => e.ClientId);
        });

        // CaseFile
        modelBuilder.Entity<Entities.CaseFile>(entity =>
        {
            entity.HasKey(e => e.CaseFileId);
            entity.HasOne(e => e.Client).WithMany(c => c.CaseFiles).HasForeignKey(e => e.ClientId);
            entity.HasOne(e => e.Therapist).WithMany(t => t.CaseFiles).HasForeignKey(e => e.TherapistId);
        });

        // Session
        modelBuilder.Entity<Entities.Session>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            entity.HasOne(e => e.CaseFile).WithMany(cf => cf.Sessions).HasForeignKey(e => e.CaseFileId);
            entity.HasOne(e => e.Therapist).WithMany(t => t.Sessions).HasForeignKey(e => e.TherapistId);
        });

        // KeyRing
        modelBuilder.Entity<KeyRing>(entity =>
        {
            entity.HasKey(e => e.KeyId);
            entity.HasIndex(e => e.FirmId).HasFilter("status = 'Active'");
            entity.HasOne(e => e.Firm).WithMany(f => f.KeyRings).HasForeignKey(e => e.FirmId);
        });

        // ClinicalNote
        modelBuilder.Entity<Entities.ClinicalNote>(entity =>
        {
            entity.HasKey(e => e.ClinicalNoteId);
            entity.HasIndex(e => new { e.SessionId, e.NoteType }).IsUnique().HasFilter("is_deleted = false");
            entity.HasOne(e => e.Session).WithMany(s => s.ClinicalNotes).HasForeignKey(e => e.SessionId);
            entity.HasOne(e => e.KeyRing).WithMany(k => k.ClinicalNotes).HasForeignKey(e => e.KeyId);
        });
    }
}
