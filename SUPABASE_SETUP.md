# Supabase Integration for Praxis

Guide to setting up Supabase for user authentication and firm/practice data.

## Overview

Supabase provides:
- **Authentication** — Managed login via AuthUserId (UUID)
- **Cloud Database** — Real-time events (future sync)
- **Storage** — Optional for documents (currently local)

Praxis uses **hybrid architecture:**
- Supabase = Authentication source of truth + optional cloud backup
- Local SQLite = Primary working database (firm-first, offline-capable)

---

## 1. Supabase Project Setup

### Create Project

1. Go to https://supabase.com
2. Create new project
   - Organization: "Prickly Cactus Software"
   - Project name: "praxis-auth"
   - Database password: (save securely)
   - Region: US East (us-east-1) or closest to your users
3. Note the credentials:
   - **Project URL:** `https://[project-ref].supabase.co`
   - **Anon Key:** Public, safe to embed in app
   - **Service Role Key:** Private, never share
   - **Database connection string:** For admin access

### Enable Providers

In Supabase dashboard → Authentication → Providers:
- ✅ Email (password-based)
- ✅ Google OAuth (optional)
- ✅ Microsoft OAuth (optional)

---

## 2. Database Schema in Supabase

Supabase provides `auth.users` table automatically. Add cloud-side firm data:

### supabase/migrations/001_initial.sql

```sql
-- Practice (firm) data lives in Supabase for multi-device sync
CREATE TABLE public.practices (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  name TEXT NOT NULL,
  time_zone TEXT DEFAULT 'America/Chicago',
  default_currency TEXT DEFAULT 'USD',
  default_session_length INT DEFAULT 60,
  phone TEXT,
  city TEXT,
  state TEXT,
  postal_code TEXT,
  is_active BOOLEAN DEFAULT TRUE,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  deleted_at TIMESTAMP WITH TIME ZONE,
  created_by UUID REFERENCES auth.users(id)
);

-- User profile linking (local + cloud)
CREATE TABLE public.user_profiles (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  auth_user_id UUID NOT NULL UNIQUE REFERENCES auth.users(id) ON DELETE CASCADE,
  display_name TEXT,
  avatar_url TEXT,
  is_active BOOLEAN DEFAULT TRUE,
  last_login_at TIMESTAMP WITH TIME ZONE,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Practice membership + roles
CREATE TABLE public.practice_users (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  practice_id UUID NOT NULL REFERENCES public.practices(id) ON DELETE CASCADE,
  user_profile_id UUID NOT NULL REFERENCES public.user_profiles(id) ON DELETE CASCADE,
  role TEXT NOT NULL CHECK (role IN ('Owner', 'Admin', 'Therapist', 'Biller', 'Staff', 'ReadOnly')),
  is_active BOOLEAN DEFAULT TRUE,
  invited_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  accepted_at TIMESTAMP WITH TIME ZONE,
  created_by UUID REFERENCES auth.users(id),
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  UNIQUE(practice_id, user_profile_id)
);

-- Row-level security policies
ALTER TABLE public.practices ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.user_profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.practice_users ENABLE ROW LEVEL SECURITY;

-- Allow users to read their own profile
CREATE POLICY "Users can read own profile"
  ON public.user_profiles
  FOR SELECT
  USING (auth_user_id = auth.uid());

-- Allow users to read practices they're members of
CREATE POLICY "Users can read practices they're members of"
  ON public.practices
  FOR SELECT
  USING (
    id IN (
      SELECT practice_id 
      FROM public.practice_users 
      WHERE user_profile_id IN (
        SELECT id FROM public.user_profiles 
        WHERE auth_user_id = auth.uid()
      ) AND is_active = TRUE
    )
  );

-- Create function to sync local UserProfile on auth signup
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER AS $$
BEGIN
  INSERT INTO public.user_profiles (auth_user_id, display_name)
  VALUES (NEW.id, NEW.email);
  RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

CREATE TRIGGER on_auth_user_created
  AFTER INSERT ON auth.users
  FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();
```

Apply migrations in Supabase dashboard → SQL Editor.

---

## 3. Local Configuration

### appsettings.json

```json
{
  "Supabase": {
    "Url": "https://[project-ref].supabase.co",
    "AnonKey": "[your-anon-key]",
    "ServiceRoleKey": "[your-service-role-key]"
  },
  "Authentication": {
    "JwtSecret": "[your-jwt-secret-from-supabase]"
  }
}
```

### User Secrets (Development)

```powershell
dotnet user-secrets set "Supabase:AnonKey" "eyJhbGc..."
dotnet user-secrets set "Supabase:ServiceRoleKey" "eyJhbGc..."
```

---

## 4. C# Integration

### Supabase Client Setup

Install NuGet:
```powershell
dotnet add package Supabase.Core
dotnet add package Supabase.Gotrue
```

### AuthService.cs

```csharp
using Supabase;
using Supabase.Gotrue;

public interface IAuthService
{
    Task<AuthSession?> SignUpAsync(string email, string password);
    Task<AuthSession?> SignInAsync(string email, string password);
    Task SignOutAsync();
    Task<User?> GetCurrentUserAsync();
    string? GetCurrentAuthUserId();
    event EventHandler<User?>? AuthStateChanged;
}

public class AuthService : IAuthService
{
    private readonly Supabase.Client _supabase;
    public event EventHandler<User?>? AuthStateChanged;

    public AuthService(IConfiguration config)
    {
        var url = config["Supabase:Url"] 
            ?? throw new InvalidOperationException("Missing Supabase:Url");
        var anonKey = config["Supabase:AnonKey"]
            ?? throw new InvalidOperationException("Missing Supabase:AnonKey");

        _supabase = new Supabase.Client(url, anonKey);
    }

    public async Task<AuthSession?> SignUpAsync(string email, string password)
    {
        var session = await _supabase.Auth.SignUp(email, password);
        return session;
    }

    public async Task<AuthSession?> SignInAsync(string email, string password)
    {
        var session = await _supabase.Auth.SignIn(email, password);
        AuthStateChanged?.Invoke(this, session?.User);
        return session;
    }

    public async Task SignOutAsync()
    {
        await _supabase.Auth.SignOut();
        AuthStateChanged?.Invoke(this, null);
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        return _supabase.Auth.CurrentUser;
    }

    public string? GetCurrentAuthUserId()
    {
        return _supabase.Auth.CurrentUser?.Id.ToString();
    }
}
```

### Dependency Injection

In `App.xaml.cs`:

```csharp
services.AddSingleton<IAuthService, AuthService>();
services.AddSingleton<IPracticeService, PracticeService>();
```

---

## 5. Login Flow

### Step 1: User enters email/password

```csharp
var session = await _authService.SignInAsync(email, password);
if (session != null)
{
    var authUserId = session.User.Id;
    // Proceed to load local practice + user data
}
```

### Step 2: Local lookup via AuthUserId

In Praxis (local SQLite):

```csharp
var userProfile = await _dbContext.UserProfiles
    .FirstOrDefaultAsync(u => u.AuthUserId == Guid.Parse(authUserId));

var practiceUsers = await _dbContext.PracticeUsers
    .Where(pu => pu.UserProfileId == userProfile.Id && pu.IsActive)
    .ToListAsync();

// User now has access to their practices
```

### Step 3: Load practice data

```csharp
var practices = practiceUsers.Select(pu => pu.Practice).ToList();
// Show practice picker or load first practice
```

---

## 6. User Invitation Flow

**Owner invites a new therapist:**

1. Owner provides email
2. System creates PracticeUser record (AcceptedAt = null)
3. Send email with invitation link + temp code
4. New user signs up → UserProfile auto-created
5. New user visits link → AcceptedAt = now

**Invitation endpoint:**

```csharp
public async Task InviteUserAsync(Guid practiceId, string email, string role)
{
    // Create PracticeUser before auth signup
    var practiceUser = new PracticeUser
    {
        PracticeId = practiceId,
        UserProfileId = ???, // Not yet exists
        Role = role,
        InvitedOnUtc = DateTime.UtcNow,
        CreatedByUserProfileId = currentUser.UserProfileId
    };
    
    // Send email with link to signup
    // On signup completion, match by email and link PracticeUser
}
```

---

## 7. Syncing Local ↔ Cloud

### Download on startup (optional)

```csharp
public async Task SyncPracticesAsync(string authUserId)
{
    // Fetch from Supabase
    var cloudPractices = await _supabase
        .From<Practice>("practices")
        .Select("*, practice_users(*)")
        .Where(p => p.UserId == authUserId)
        .Get();
    
    // Merge into local DB
    foreach (var cloudPractice in cloudPractices)
    {
        var localPractice = await _db.Practices
            .FirstOrDefaultAsync(p => p.PracticeId == cloudPractice.Id);
        
        if (localPractice == null)
        {
            _db.Practices.Add(cloudPractice);
        }
        else if (cloudPractice.UpdatedAt > localPractice.UpdatedAt)
        {
            // Cloud is newer, update local
            localPractice.UpdateFrom(cloudPractice);
        }
    }
    
    await _db.SaveChangesAsync();
}
```

### Outbox pattern (for eventual cloud sync)

```csharp
// On create/update
var outboxEvent = new OutboxEvent
{
    EventType = "PracticeUpdated",
    AggregateId = practice.PracticeId,
    Payload = JsonSerializer.Serialize(practice)
};

_db.Outbox.Add(outboxEvent);
await _db.SaveChangesAsync();

// Background job syncs unprocessed events to cloud
```

---

## 8. Security Best Practices

### Tokens & Storage
- **Access Token:** Short-lived (1 hour)
- **Refresh Token:** Long-lived (7 days), stored securely
- **Local Storage:** None (tokens in memory during session)
- **On Logout:** Tokens cleared

### Row-Level Security (RLS)
- Every query to Supabase enforced by RLS policies
- Users can only read their own data + practices they're members of

### Encryption
- Clinical notes encrypted before saving locally
- Supabase connection via HTTPS only
- JWT tokens validated server-side

### Password Policy
- Minimum 8 characters
- Email verification required
- OAuth optional (Google, Microsoft)

---

## 9. Troubleshooting

### "Missing Supabase configuration"
- Check `appsettings.json` and user secrets
- Verify Supabase project exists and is active

### "Auth user not found"
- Check Supabase dashboard → Authentication
- Verify email was confirmed (if required)

### "CORS error"
- Supabase dashboard → Authentication → URL Configuration
- Add your app domain (localhost:7000, etc.)

### "RLS policy denied"
- Verify user is authenticated (`auth.uid()` is not null)
- Check RLS policy logic in Supabase

---

## 10. Next Steps

1. **Create Supabase project** (see step 1)
2. **Run migrations** (see step 2)
3. **Install NuGet packages** (Supabase.Core, Supabase.Gotrue)
4. **Implement AuthService** (see step 4)
5. **Add login screen** (WPF XAML)
6. **Test sign-up → practice creation flow**
7. **Implement role-based access** (Therapist vs Admin UI)

---

*Last updated: 2026-01-02*
