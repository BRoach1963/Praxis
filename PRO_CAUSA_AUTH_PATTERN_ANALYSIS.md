# Pro Causa User/Authentication Pattern Analysis

## Executive Summary

Pro Causa implements a **dual-database architecture** supporting both SQL Server and Supabase (PostgreSQL), with a clear separation between **authentication identity** (Supabase auth users) and **application-level user personas** (Users table). They do NOT use Supabase auth users directly; instead, they maintain application-managed Users with a multi-role hierarchy.

---

## 1. Authentication Approach: Application-Managed Users + Supabase

### Answer: HYBRID APPROACH
Pro Causa uses **application-managed users** rather than Supabase auth users directly.

- **Supabase Auth**: Used for primary authentication only (email/password in `auth.users` table)
- **Application Users**: `Users` table in SQL Server (or ProCausa DB) holds the actual user records with roles and professional information
- **Bridge**: `firm_users` table links Supabase auth users to application-managed Users within a Firm context

### Key Insight
The architecture keeps authentication (Supabase auth) completely separate from authorization/roles (application Users table). This allows:
- Multiple authentication providers in the future
- Application-level role management independent of auth provider
- Flexible multi-tenancy per firm

---

## 2. User/UserProfile Structure

### Database Schema (SQL Server)

#### `Users` Table (Core User Entity)
```sql
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    FirmId UNIQUEIDENTIFIER NOT NULL,  -- Multi-tenancy: Links to Firms table
    
    -- AUTHENTICATION FIELDS
    Email NVARCHAR(255) NOT NULL,          -- Used for login
    PasswordHash NVARCHAR(500) NULL,       -- Only if local auth
    TwoFactorEnabled BIT DEFAULT 0,
    TwoFactorSecret NVARCHAR(100) NULL,
    
    -- PROFILE FIELDS
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    MiddleName NVARCHAR(50) NULL,
    Prefix NVARCHAR(20) NULL,              -- e.g., "Mr.", "Ms."
    Suffix NVARCHAR(20) NULL,              -- e.g., "Esq.", "PhD"
    Title NVARCHAR(100) NULL,              -- e.g., "Managing Partner", "Senior Associate"
    
    -- PROFESSIONAL CLASSIFICATION
    UserType INT DEFAULT 1,                -- Enum: 1=Attorney, 2=Paralegal, 3=...
    BarNumber NVARCHAR(50) NULL,           -- Attorney bar license
    BarState NVARCHAR(5) NULL,             -- e.g., "OR", "CA"
    BarAdmissionDate DATE NULL,
    
    -- CONTACT
    Phone NVARCHAR(30) NULL,               -- Office phone
    Mobile NVARCHAR(30) NULL,
    Extension NVARCHAR(10) NULL,
    
    -- BILLING & TIME TRACKING
    DefaultHourlyRate DECIMAL(10,2) NULL,
    TimekeeperCode NVARCHAR(20) NULL,
    IsBillable BIT DEFAULT 1,
    
    -- STATUS & SECURITY
    IsActive BIT DEFAULT 1,
    LastLoginAt DATETIME2 NULL,
    PasswordChangedAt DATETIME2 NULL,
    FailedLoginAttempts INT DEFAULT 0,
    LockoutEndAt DATETIME2 NULL,
    
    -- AUTHORIZATION (Single Role)
    Role INT DEFAULT 4,                    -- Enum: 1=Admin, 2=Attorney, 3=Paralegal, 4=Staff
    
    -- DISPLAY
    Initials NVARCHAR(10) NULL,
    
    -- AUDIT
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    ModifiedAt DATETIME2 NULL,
    CreatedById UNIQUEIDENTIFIER NULL,
    ModifiedById UNIQUEIDENTIFIER NULL,
    RowVersion ROWVERSION,
    
    CONSTRAINT FK_Users_Firm FOREIGN KEY (FirmId) REFERENCES Firms(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Users_FirmEmail UNIQUE (FirmId, Email)
);

CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_FirmActive ON Users(FirmId, IsActive);
CREATE INDEX IX_Users_FirmRole ON Users(FirmId, Role);
CREATE INDEX IX_Users_FirmUserType ON Users(FirmId, UserType);
```

#### `UserSettings` Table (Per-User Preferences)
```sql
CREATE TABLE UserSettings (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    FirmId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    
    -- UI PREFERENCES
    Theme NVARCHAR(20) DEFAULT 'Light',        -- Light/Dark/System
    DefaultCalendarView NVARCHAR(20) DEFAULT 'Week',
    ShowCompletedTasks BIT DEFAULT 0,
    
    -- NOTIFICATIONS
    EmailNotifications BIT DEFAULT 1,
    PushNotifications BIT DEFAULT 1,
    TaskReminders BIT DEFAULT 1,
    CalendarReminders BIT DEFAULT 1,
    DefaultReminderMinutes INT DEFAULT 15,
    
    -- TIME TRACKING
    DefaultTimeIncrement INT DEFAULT 6,        -- Minutes (6 = 0.1 hour)
    AutoStartTimer BIT DEFAULT 0,
    
    -- DASHBOARD
    DashboardLayout NVARCHAR(4000) NULL,
    
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    ModifiedAt DATETIME2 NULL,
    CreatedById UNIQUEIDENTIFIER NULL,
    ModifiedById UNIQUEIDENTIFIER NULL,
    RowVersion ROWVERSION,
    
    CONSTRAINT FK_UserSettings_User FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_UserSettings_User UNIQUE (UserId)
);
```

### Supabase Schema (PostgreSQL)

Pro Causa also maintains a **read-only view** into Supabase for multi-tenant SaaS features:

#### `user_profiles` Table (Supabase)
```sql
CREATE TABLE public.user_profiles (
    id UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
    email TEXT NOT NULL,
    first_name TEXT,
    last_name TEXT,
    display_name TEXT,
    avatar_url TEXT,
    phone TEXT,
    is_admin BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_user_profiles_email ON user_profiles(email);
CREATE INDEX idx_user_profiles_is_admin ON user_profiles(is_admin) WHERE is_admin = TRUE;
```

**IMPORTANT**: Supabase `user_profiles` is minimal—it's synced from `auth.users` metadata on account creation via a trigger. Pro Causa uses this for **Supabase-specific features only** (e.g., tenant management, subscription).

---

## 3. Firm/FirmUser/Attorney Hierarchy

### The Multi-Tenancy Pattern

#### `Firms` Table (Tenant Root - SQL Server)
```sql
CREATE TABLE Firms (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    
    -- IDENTITY
    Name NVARCHAR(200) NOT NULL,           -- e.g., "Roach Law"
    DBA NVARCHAR(100) NULL,                -- "Doing Business As"
    TaxId NVARCHAR(50) NULL,               -- EIN (encrypted)
    
    -- ADDRESS
    Address1 NVARCHAR(200) NULL,
    Address2 NVARCHAR(200) NULL,
    City NVARCHAR(100) NULL,
    State NVARCHAR(50) NULL,
    PostalCode NVARCHAR(20) NULL,
    Country NVARCHAR(50) NULL,
    
    -- CONTACT
    Phone NVARCHAR(30) NULL,
    Fax NVARCHAR(30) NULL,
    Website NVARCHAR(200) NULL,
    Email NVARCHAR(255) NULL,
    
    -- SETTINGS
    FiscalYearStart NVARCHAR(50) NULL,     -- e.g., "01-01" or "07-01"
    DefaultCurrency NVARCHAR(20) DEFAULT 'USD',
    TimeZone NVARCHAR(50) NULL,
    
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    ModifiedAt DATETIME2 NULL,
    CreatedById UNIQUEIDENTIFIER NULL,
    ModifiedById UNIQUEIDENTIFIER NULL,
    RowVersion ROWVERSION
);
```

#### `Firms` Table (Supabase)
```sql
CREATE TABLE public.firms (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT NOT NULL,
    slug TEXT UNIQUE,
    owner_id UUID NOT NULL REFERENCES auth.users(id),  -- Links to Supabase auth user
    subscription_tier TEXT DEFAULT 'trial',             -- trial/professional/enterprise
    subscription_status TEXT DEFAULT 'trialing',
    trial_ends_at TIMESTAMPTZ DEFAULT (NOW() + INTERVAL '14 days'),
    max_users INTEGER DEFAULT 1,
    stripe_customer_id TEXT,
    stripe_subscription_id TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

#### `firm_users` Table (Supabase - Links Supabase Auth to Firm)
```sql
CREATE TABLE public.firm_users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    firm_id UUID NOT NULL REFERENCES firms(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    role TEXT NOT NULL DEFAULT 'member',               -- owner/admin/member
    job_role TEXT,                                     -- attorney/paralegal/staff/admin
    invited_by UUID REFERENCES auth.users(id),
    invited_at TIMESTAMPTZ,
    joined_at TIMESTAMPTZ DEFAULT NOW(),
    is_active BOOLEAN DEFAULT TRUE,
    UNIQUE(firm_id, user_id)
);

CREATE INDEX idx_firm_users_user ON firm_users(user_id);
CREATE INDEX idx_firm_users_firm ON firm_users(firm_id);
```

### Relationship Chain

```
SQL SERVER:
Firms (tenant root)
  ↓
Users (users.FirmId → firms.Id)
  ↓
UserSettings (usersettings.UserId → users.Id)

SUPABASE:
auth.users (Supabase authentication identity)
  ↓
user_profiles (metadata sync)
  ↓
firm_users (role assignment)
  ↓
firms (tenant association)
```

---

## 4. Separation Between Login Identity and Professional Persona

### Three-Layer Pattern

#### Layer 1: Authentication Identity (Supabase)
```
auth.users (managed by Supabase)
  ├─ id (UUID)
  ├─ email
  ├─ encrypted_password
  ├─ raw_user_meta_data (JSON, e.g., first_name, last_name)
  └─ created_at, updated_at
```

#### Layer 2: Application User Profile (SQL Server)
```
Users (application-managed)
  ├─ Id (GUID)
  ├─ FirmId (multi-tenancy)
  ├─ Email (matches Supabase auth.users.email)
  ├─ FirstName, LastName (core profile)
  ├─ UserType (Attorney, Paralegal, etc.)
  ├─ Role (Admin, Attorney, Paralegal, Staff)
  ├─ BarNumber, BarState, BarAdmissionDate (attorney-specific)
  ├─ DefaultHourlyRate, IsBillable (billing persona)
  └─ IsActive, FailedLoginAttempts, LockoutEndAt (security state)
```

#### Layer 3: Role Assignment per Firm (Supabase)
```
firm_users (bridges auth to business role)
  ├─ user_id (auth.users.id)
  ├─ firm_id (firms.id)
  ├─ role (owner/admin/member - platform role)
  └─ job_role (attorney/paralegal/staff - job title)
```

### Example Workflow

**Scenario**: Brian Roach logs in to the system

1. **Authentication**: Enters email `brian.roach@roachlaw.com` + password
   - Validates against `auth.users` in Supabase
   - Auth system returns JWT with `user_id` = `{UUID-1}`

2. **Load User Profile**: App queries SQL Server
   ```sql
   SELECT * FROM Users 
   WHERE Email = 'brian.roach@roachlaw.com' AND FirmId = {current-firm}
   ```
   - Returns: User record with `Id = {GUID-1}`, `Role = Attorney`, `UserType = Attorney`, `DefaultHourlyRate = 375.00`

3. **Load Firm Context**: Query Supabase for firm access
   ```sql
   SELECT * FROM firm_users WHERE user_id = {UUID-1}
   ```
   - Returns: firm role = `owner`, job_role = `administrator` for the law firm

4. **Full Context**: App now knows:
   - **Who logged in**: Brian Roach (authentication identity)
   - **What firm they work for**: Roach Law (firm_id)
   - **What role in the firm**: Owner/Administrator (platform role)
   - **What job they do**: Managing Partner (job title)
   - **Billing info**: $375/hr, billable hours tracked (professional persona)

---

## 5. C# Models Demonstrating the Pattern

### User Entity (Application-Managed)
```csharp
// File: src/ProCausa.Core/Entities/Firm/User.cs

public class User : FirmEntityBase
{
    // AUTHENTICATION
    [Required, MaxLength(255), EmailAddressValidation]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? PasswordHash { get; set; }
    
    public bool TwoFactorEnabled { get; set; }
    
    [MaxLength(200)]
    public string? TwoFactorSecretEncrypted { get; set; }

    // PROFILE
    [Required, MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required, MaxLength(50)]
    public string LastName { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? MiddleName { get; set; }
    
    [MaxLength(20)]
    public string? Prefix { get; set; }
    
    [MaxLength(20)]
    public string? Suffix { get; set; }
    
    [MaxLength(100)]
    public string? Title { get; set; }

    // PROFESSIONAL CLASSIFICATION
    [Required]
    public Entities.UserType UserType { get; set; } = Entities.UserType.Attorney;

    // BAR INFORMATION (for attorneys only)
    [MaxLength(100)]
    public string? BarNumberEncrypted { get; set; }
    
    [MaxLength(5)]
    public string? BarState { get; set; }
    
    public DateTime? BarAdmissionDate { get; set; }

    // CONTACT
    [MaxLength(50)]
    public string? Phone { get; set; }
    
    [MaxLength(50)]
    public string? Mobile { get; set; }

    // TIME TRACKING & BILLING
    [Column(TypeName = "decimal(18,2)")]
    public decimal? DefaultHourlyRate { get; set; }
    
    [MaxLength(20)]
    public string? TimekeeperCode { get; set; }
    
    public bool IsBillable { get; set; } = true;

    // STATUS & SECURITY
    public bool IsActive { get; set; } = true;
    
    public DateTime? LastLoginAt { get; set; }
    
    public DateTime? PasswordChangedAt { get; set; }
    
    [Range(0, 100)]
    public int FailedLoginAttempts { get; set; }
    
    public DateTime? LockoutEndAt { get; set; }
    
    [NotMapped]
    public bool IsLockedOut => LockoutEndAt.HasValue && LockoutEndAt.Value > DateTime.UtcNow;

    // AUTHORIZATION
    [Required]
    public Entities.UserRole Role { get; set; } = Entities.UserRole.Staff;

    // COMPUTED PROPERTIES
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();
    
    [NotMapped]
    public string DisplayName => string.IsNullOrEmpty(MiddleName)
        ? $"{LastName}, {FirstName}"
        : $"{LastName}, {FirstName} {MiddleName[0]}.";
    
    [MaxLength(10)]
    public string? Initials { get; set; }

    // NAVIGATION
    public virtual Entities.Firm.Firm Firm { get; set; } = null!;
    public virtual UserSettings? Settings { get; set; }
    public virtual ICollection<Matters.MatterTeamMember> MatterAssignments { get; set; }
    public virtual ICollection<TimeTracking.TimeEntry> TimeEntries { get; set; }
    public virtual ICollection<Calendar.CalendarEvent> CalendarEvents { get; set; }
    public virtual ICollection<Tasks.TaskAssignee> AssignedTasks { get; set; }
}
```

### UserSettings Entity
```csharp
public class UserSettings : FirmEntityBase
{
    public Guid UserId { get; set; }

    // UI PREFERENCES
    [MaxLength(20)]
    public string Theme { get; set; } = "Light";
    
    [MaxLength(20)]
    public string DefaultCalendarView { get; set; } = "Week";
    
    public bool ShowCompletedTasks { get; set; }

    // NOTIFICATION PREFERENCES
    public bool EmailNotifications { get; set; } = true;
    public bool PushNotifications { get; set; } = true;
    public bool TaskReminders { get; set; } = true;
    public bool CalendarReminders { get; set; } = true;
    
    [Range(5, 1440)]
    public int DefaultReminderMinutes { get; set; } = 15;

    // TIME ENTRY PREFERENCES
    [Range(1, 60)]
    public int DefaultTimeIncrement { get; set; } = 6;  // 6 min = 0.1 hour
    
    public bool AutoStartTimer { get; set; }

    // DASHBOARD
    [MaxLength(4000)]
    public string? DashboardLayout { get; set; }

    public virtual User User { get; set; } = null!;
}
```

### Firm Entity (Tenant Root)
```csharp
// File: src/ProCausa.Core/Entities/Firm/Firm.cs

public class Firm : EntityBase
{
    // IDENTITY
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? DBA { get; set; }
    
    [MaxLength(50)]
    public string? TaxIdEncrypted { get; set; }

    // ADDRESS
    [MaxLength(200)]
    public string? Address1 { get; set; }
    
    [MaxLength(200)]
    public string? Address2 { get; set; }
    
    [MaxLength(100)]
    public string? City { get; set; }
    
    [MaxLength(50)]
    public string? State { get; set; }
    
    [MaxLength(20)]
    public string? PostalCode { get; set; }
    
    [MaxLength(50)]
    public string? Country { get; set; }

    // CONTACT
    [MaxLength(50)]
    public string? Phone { get; set; }
    
    [MaxLength(50)]
    public string? Fax { get; set; }
    
    [MaxLength(200), Url]
    public string? Website { get; set; }
    
    [MaxLength(255), EmailAddressValidation]
    public string? Email { get; set; }

    // SETTINGS
    [MaxLength(50), FiscalYearStart]
    public string? FiscalYearStart { get; set; }
    
    [MaxLength(20), CurrencyCode]
    public string? DefaultCurrency { get; set; } = "USD";
    
    [MaxLength(50), TimeZoneId]
    public string? TimeZone { get; set; }

    public bool IsActive { get; set; } = true;

    // NAVIGATION
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<SystemSetting> Settings { get; set; } = new List<SystemSetting>();
}
```

---

## 6. Key Differentiators: Pro Causa vs. Traditional Approaches

| Aspect | Pro Causa | Traditional Single-User Model |
|--------|-----------|-------------------------------|
| **Auth Provider** | Supabase (external) | Application-managed |
| **User Record** | `Users` table (app-managed) | Single user record |
| **Tenant Context** | `FirmId` per user | Implicit or config-based |
| **Professional Role** | `Role` + `UserType` (enums) | Role-based claims only |
| **Bar Licensure** | `BarNumber`, `BarState`, `BarAdmissionDate` | Usually in separate attorney table |
| **Billing Persona** | `DefaultHourlyRate`, `IsBillable` directly on User | Separate Attorney/Billing entity |
| **Firm Association** | Direct via `FirmId` FK | Via junction table or nested lookup |
| **Settings/Preferences** | Separate `UserSettings` table | Often in User denormalization |

---

## 7. Setup/Initialization Pattern

### SQL Server Setup (for on-premises or Windows client apps)

1. Create database and schema via `01_CreateSchema_Part1_FirmUsers.sql`
2. Create admin user via `03_CreateAdminUser.sql`
3. Seed sample users via `06_SeedSampleData_Users.sql`
4. Users can now log in locally with PasswordHash

### Supabase Setup (for SaaS multi-tenant)

1. Run `01_schema_setup.sql` to create `user_profiles`, `firms`, `firm_users` tables
2. Create Supabase auth user → auto-triggers `handle_new_user()` function
3. Create firm with `create_firm()` function
4. Add user to firm with `firm_users` insert
5. User can now log in via Supabase auth

### Sample User Creation (SQL Server)
```sql
DECLARE @FirmId UNIQUEIDENTIFIER = {firm-guid};

INSERT INTO Users (FirmId, Email, FirstName, LastName, Title, UserType, Role, 
                   BarNumber, BarState, DefaultHourlyRate, IsActive, CreatedAt)
VALUES (
    @FirmId,
    'brian.roach@roachlaw.com',
    'Brian', 'Roach',
    'Managing Partner',
    1,  -- Attorney
    2,  -- Attorney role
    'OR-54321',
    'OR',
    375.00,
    1,
    GETUTCDATE()
);
```

---

## Summary Table: User → Role → Professional Separation

```
┌─────────────────────────────────────────────────────────────────┐
│ LAYER 1: AUTHENTICATION (Supabase)                              │
├─────────────────────────────────────────────────────────────────┤
│ • auth.users (email, password, jwt)                             │
│ • Managed by Supabase Auth service                              │
│ • External to the application                                   │
└──────────────────────────┬──────────────────────────────────────┘
                           │ (Email match)
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ LAYER 2: APPLICATION USER (SQL Server Users table)              │
├─────────────────────────────────────────────────────────────────┤
│ • Id, Email, FirstName, LastName (identity)                    │
│ • FirmId (tenancy)                                              │
│ • Role enum (Admin, Attorney, Paralegal, Staff)                 │
│ • UserType enum (Attorney, Paralegal, etc.)                     │
│ • BarNumber, BarState, BarAdmissionDate (attorney-specific)     │
│ • DefaultHourlyRate, IsBillable (billing persona)               │
│ • UserSettings navigation (preferences)                         │
└──────────────────────────┬──────────────────────────────────────┘
                           │ (FirmId + UserId)
                           ↓
┌─────────────────────────────────────────────────────────────────┐
│ LAYER 3: FIRM CONTEXT (Supabase firm_users + firms)            │
├─────────────────────────────────────────────────────────────────┤
│ • firm_users.role (owner/admin/member - platform role)          │
│ • firm_users.job_role (attorney/paralegal/staff)                │
│ • firms.subscription_tier, max_users (SaaS licensing)           │
│ • Firm metadata (name, address, fiscal year, timezone)          │
└─────────────────────────────────────────────────────────────────┘
```

---

## Recommendation for Praxis

If Praxis is using Supabase for authentication, consider Pro Causa's pattern:

1. **Keep Supabase auth minimal**: Just email/password, auto-sync basic profile
2. **Enrich in application DB**: Create detailed `Users` table with professional metadata
3. **Create UserProfile/UserSettings** separate for preferences
4. **Use FirmId tenancy** throughout to support multi-firm scenarios
5. **Separate `UserType` from `Role`**: UserType = professional classification (Attorney, Therapist), Role = authorization level (Admin, Staff)
6. **Store audit trail**: `CreatedById`, `ModifiedById`, timestamps on all entities

This gives you:
- ✅ Flexibility to swap auth providers
- ✅ Rich application-level user data
- ✅ Multi-tenancy support built-in
- ✅ Clear separation of concerns (auth vs. business logic)
- ✅ Professional information tracking (bar licenses, billing rates, etc.)
