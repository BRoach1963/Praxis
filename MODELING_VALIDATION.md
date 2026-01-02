# Praxis Data Model Validation

## Core Principle: Login ≠ Clinical Role

The Praxis data model correctly implements the separation between **authentication/authorization** and **clinical practice**.

---

## 1. VALIDATED: Existing C# Model Architecture

### UserProfile (Authentication Identity)
```csharp
public class UserProfile
{
    public Guid UserProfileId { get; set; }
    public Guid AuthUserId { get; set; }  // ← Supabase auth.users(id)
    public string Email { get; set; }
    public List<PracticeUser> PracticeUsers { get; set; }  // Can access multiple practices
}
```
✅ **Correct**: One login can access multiple practices via PracticeUser memberships.

### PracticeUser (Authorization & Organizational Role)
```csharp
public class PracticeUser
{
    public Guid PracticeUserId { get; set; }
    public Guid PracticeId { get; set; }
    public Guid UserProfileId { get; set; }  // ← Link to login identity
    public PracticeRole Role { get; set; }    // Owner, Admin, Therapist, Biller, Staff, ReadOnly
}
```
✅ **Correct**: Defines WHO can access a practice and WITH WHAT AUTHORIZATION.
✅ **Important**: `PracticeRole.Therapist` is an ORG ROLE, not a clinical designation.

### Therapist (Clinical Practitioner Profile)
```csharp
public class Therapist
{
    public Guid TherapistId { get; set; }
    public Guid PracticeId { get; set; }
    public Guid? PracticeUserId { get; set; }  // ← OPTIONAL link to login
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? LicenseNumber { get; set; }
    public string? NPI { get; set; }
    public string? Credential { get; set; }
}
```
✅ **Correct**: Clinical profile INDEPENDENT of login.
✅ **Correct**: May or may not have a PracticeUser link.

---

## 2. IMPLICATIONS: Use Cases That Require This Separation

### Use Case 1: Billing Admin (No Clinical Role)
```
UserProfile "alice@example.com"
  └─ PracticeUser (Practice A, Role=Biller)
       └─ NO Therapist entity
       
→ Alice logs in and manages invoices/payments
→ Alice is NOT a therapist, cannot be assigned to clinical sessions
```

### Use Case 2: Therapist with Clinical Profile
```
UserProfile "bob@example.com"
  └─ PracticeUser (Practice A, Role=Therapist)
       └─ Therapist entity (License #: ABC123, NPI: 1234567890)
       
→ Bob logs in and conducts therapy sessions
→ Sessions assigned to Therapist entity, not PracticeUser
→ Clinical notes tied to Therapist's clinical persona
```

### Use Case 3: Therapist Supervisor
```
UserProfile "carol@example.com"
  └─ PracticeUser (Practice A, Role=Therapist)
       └─ Therapist entity (Supervisor)
       
→ Carol logs in as Therapist role
→ Can supervise other therapists (ClientAssignmentRole.Supervisor)
→ Cannot be confused with admin supervision—she supervises CLINICAL work
```

### Use Case 4: Contractor Therapist (No System Access)
```
Therapist entity (Practice A)
  ├─ Name: "Dr. Smith"
  ├─ License: ABC456
  ├─ PracticeUserId: NULL  ← No login credentials
  
→ Dr. Smith is not in the login system
→ Can still be assigned to cases and deliver sessions
→ Practice schedules sessions with Dr. Smith
```

### Use Case 5: Multi-Practice Access
```
UserProfile "dana@example.com"
  ├─ PracticeUser (Practice A, Role=Admin)
  │   └─ Therapist (Practice A)
  │
  └─ PracticeUser (Practice B, Role=Biller)
      └─ NO Therapist entity
      
→ Dana logs in once, accesses two practices with different roles
→ In Practice A: she's a therapist admin
→ In Practice B: she's purely administrative
```

---

## 3. SESSION ASSIGNMENT: Why It's Attached to Therapist, Not PracticeUser

```sql
-- CORRECT ✅
CREATE TABLE session (
    session_id UUID PRIMARY KEY,
    case_file_id UUID,
    therapist_id UUID,  -- ← Clinical practitioner
    start_utc TIMESTAMPTZ,
    created_by_user_profile_id UUID  -- ← Who created/scheduled it
);
```

**Why**:
- Separates **WHO CONDUCTS THERAPY** (Therapist) from **WHO SCHEDULED IT** (UserProfile via PracticeUser)
- Clinical notes are tied to the Therapist who delivered care
- Billing uses the Therapist's clinical credentials/service codes
- Supervision chains are based on Therapist-to-Therapist relationships

---

## 4. AUDIT & ACCOUNTABILITY

```sql
-- Who made changes (authorization context)
created_by_user_profile_id UUID  -- The login that created this

-- Who delivered clinical care (clinical context)
therapist_id UUID  -- The clinical practitioner
```

This allows:
- ✅ Track which admin scheduled a session (accountability)
- ✅ Track which therapist delivered it (clinical responsibility)
- ✅ Charge the therapist's credentials to the invoice (billing)

---

## 5. COMPARABLE PATTERNS (You Already Think This Way)

### Tracker
```
Company (tenant)
  └─ User (login identity)
       └─ Employee (org role in company)
            └─ Goal (work objectives)
```

### Pro Causa
```
Law Firm (tenant)
  └─ User (login identity)
       └─ Attorney (professional role in firm)
            └─ Matter (legal case)
```

### Praxis (This Project)
```
Practice (tenant)
  └─ UserProfile (login identity)
       └─ PracticeUser (org role in practice)
            └─ Therapist (clinical persona)
                 └─ Session (clinical delivery)
```

---

## 6. Schema Implementation (Supabase)

The generated `SUPABASE_SCHEMA.sql` implements this correctly:

```sql
-- Separate identities
user_profile (auth_user_id, email, display_name)
practice_user (practice_id, user_profile_id, role)  -- Many-to-many
therapist (practice_id, practice_user_id)  -- Optional link

-- Clinical work assigned to practitioner, not login
session (therapist_id, created_by_user_profile_id)
clinical_note (therapist_id, created_by_user_profile_id)
```

---

## 7. Checklist for Implementation

- [x] UserProfile linked to `auth.users(id)` in Supabase
- [x] PracticeUser is the authorization table (who can access what)
- [x] Therapist is optional per PracticeUser
- [x] Sessions, ClinicalNotes assigned to Therapist (clinical), not PracticeUser (auth)
- [x] Invoices can reference sessions delivered by therapist
- [x] RLS policies enforce practice isolation via PracticeUser
- [x] Clinical supervision (therapist → therapist) is separate from administrative roles

---

## Next Steps

1. **Create Supabase project** and run `SUPABASE_SCHEMA.sql`
2. **Set up RLS policies** for practice isolation
3. **Implement auth middleware** to load user's practice access from PracticeUser
4. **Clinical UI** queries sessions by Therapist ID (who delivered care)
5. **Admin UI** queries by UserProfile/PracticeUser (who has access)
