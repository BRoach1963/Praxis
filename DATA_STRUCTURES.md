# Praxis Data Structures

A comprehensive guide to Praxis's entity model. Built for multi-user, multi-practice clinical workflows with strong audit trails and clinical security.

---

## 1. Organizational Structure (Tenant/Practice)

### Practice
The top-level container. All other entities belong to a practice.

| Field | Type | Notes |
|-------|------|-------|
| PracticeId | GUID | Primary key |
| Name | string | Practice name |
| TimeZone | string | IANA timezone (e.g., "America/Chicago") |
| DefaultCurrency | string | ISO 4217 code (e.g., "USD") |
| DefaultSessionLength | int | Minutes (e.g., 60) |
| AddressLine1 | string | |
| AddressLine2 | string | Optional |
| City | string | |
| StateProvince | string | |
| PostalCode | string | |
| Country | string | |
| Phone | string | |
| IsActive | bool | Soft enable/disable |
| CreatedOnUtc | DateTime | |
| UpdatedOnUtc | DateTime | |
| IsDeleted | bool | Soft delete |
| DeletedOnUtc | DateTime? | |
| DeletedByUserProfileId | GUID? | Who deleted it |

**Keys:**
- Primary: PracticeId
- Indexes: (IsActive), (IsDeleted, DeletedOnUtc)

---

## 2. People & Access

### UserProfile
Maps to Supabase authentication. One login identity across all practices.

| Field | Type | Notes |
|-------|------|-------|
| UserProfileId | GUID | Primary key (local) |
| AuthUserId | UUID | From Supabase (unique) |
| Email | string | |
| DisplayName | string | |
| AvatarUrl | string? | Optional |
| IsActive | bool | Can deactivate without deleting |
| CreatedOnUtc | DateTime | |
| UpdatedOnUtc | DateTime | |
| LastLoginUtc | DateTime? | For tracking |

**Keys:**
- Primary: UserProfileId
- Unique: AuthUserId
- Indexes: (Email), (IsActive)

**Note:** Supabase manages AuthUserId. We store it locally for quick lookup without API calls.

### PracticeUser
Membership + role binding. The join table between Practice and UserProfile.

| Field | Type | Notes |
|-------|------|-------|
| PracticeUserId | GUID | Primary key |
| PracticeId | GUID | FK → Practice |
| UserProfileId | GUID | FK → UserProfile |
| Role | enum | Owner/Admin/Therapist/Biller/Staff/ReadOnly |
| IsActive | bool | Deactivate without delete |
| InvitedOnUtc | DateTime | |
| AcceptedOnUtc | DateTime? | NULL until user accepts invite |
| CreatedOnUtc | DateTime | |
| UpdatedOnUtc | DateTime | |
| CreatedByUserProfileId | GUID | Who invited them |

**Keys:**
- Primary: PracticeUserId
- Unique: (PracticeId, UserProfileId)
- Indexes: (PracticeId, IsActive), (UserProfileId)

**Roles:**
- `Owner` — Full control, can manage users
- `Admin` — Manage clients, therapists, invoices
- `Therapist` — See/edit own clients, create notes
- `Biller` — View invoices, payments
- `Staff` — Data entry, scheduling
- `ReadOnly` — View only

### Therapist
Clinical practitioner profile. May (but doesn't have to) map to a PracticeUser.

| Field | Type | Notes |
|-------|------|-------|
| TherapistId | GUID | Primary key |
| PracticeId | GUID | FK → Practice |
| PracticeUserId | GUID? | FK → PracticeUser (optional) |
| FirstName | string | |
| LastName | string | |
| LicenseNumber | string? | |
| LicenseState | string? | |
| NPI | string? | National Provider Identifier |
| Credential | string? | "LCSW", "Ph.D.", "LMFT", etc. |
| Specialty | string? | "Individual", "Family", etc. |
| Bio | string? | |
| SignatureBlock | string? | For notes/documents |
| IsActive | bool | |
| CreatedOnUtc | DateTime | |
| UpdatedOnUtc | DateTime | |
| IsDeleted | bool | Soft delete |
| DeletedOnUtc | DateTime? | |

**Keys:**
- Primary: TherapistId
- Indexes: (PracticeId, IsActive), (NPI, IsDeleted)

**Motivation:** Therapist ≠ Login. A practice may have therapist profiles that don't have system logins (e.g., supervisors, contractors).

---

## 3. Clients

### Client
Individual in care. Core clinical entity.

| Field | Type | Notes |
|-------|------|-------|
| ClientId | GUID | Primary key |
| PracticeId | GUID | FK → Practice |
| FirstName | string | Legal name |
| LastName | string | Legal name |
| PreferredName | string? | If different from first name |
| Pronouns | string? | "she/her", "they/them", etc. |
| DateOfBirth | DateTime? | |
| Gender | string? | |
| Email | string? | |
| Phone | string? | |
| EmergencyContactName | string? | |
| EmergencyContactPhone | string? | |
| Status | enum | Active/Inactive/Archived |
| IntakeDate | DateTime? | |
| TerminationDate | DateTime? | |
| CreatedOnUtc | DateTime | |
| UpdatedOnUtc | DateTime | |
| IsDeleted | bool | Soft delete |
| DeletedOnUtc | DateTime? | |

**Keys:**
- Primary: ClientId
- Indexes: (PracticeId, LastName, FirstName), (PracticeId, Status)

---

### ClientAssignment
Links a Client to one or more Therapists with a role.

| Field | Type | Notes |
|-------|------|-------|
| ClientAssignmentId | GUID | Primary key |
| ClientId | GUID | FK → Client |
| TherapistId | GUID | FK → Therapist |
| Role | enum | Primary/Secondary/Supervisor |
| StartDate | DateTime | |
| EndDate | DateTime? | NULL = ongoing |
| CreatedOnUtc | DateTime | |
| UpdatedOnUtc | DateTime | |

**Keys:**
- Primary: ClientAssignmentId
- Unique: (ClientId, TherapistId, Role, StartDate) — per role, one active assignment at a time
- Indexes: (TherapistId, StartDate)

---

## 4. Episodes of Care

### CaseFile
Also called "Episode" — defines a course of treatment for a client.

| Field | Type | Notes |
|-------|------|-------|
| CaseFileId | GUID | Primary key |
| ClientId | GUID | FK → Client |
| PrimaryTherapistId | GUID | FK → Therapist |
| StartDate | DateTime | |
| EndDate | DateTime? | NULL = ongoing |
| PresentingProblems | string | Initial reason for referral |
| Status | enum | Active/Paused/Closed |
| Version | int | Increment on major changes |
| CreatedOnUtc | DateTime | |
| UpdatedOnUtc | DateTime | |
| UpdatedByUserProfileId | GUID | |

**Keys:**
- Primary: CaseFileId
- Indexes: (ClientId, StartDate, EndDate), (PrimaryTherapistId, Status)

---

## 5. Treatment Planning

### TreatmentPlan
Formalizes goals, interventions, and expected outcomes.

| Field | Type | Notes |
|-------|------|-------|
| TreatmentPlanId | GUID | Primary key |
| CaseFileId | GUID | FK → CaseFile |
| PlanVersion | int | e.g., 1, 2, 3 for revisions |
| Summary | string | Plan overview |
| Status | enum | Draft/Active/Archived |
| CreatedOnUtc | DateTime | |
| CreatedByUserProfileId | GUID | |
| EffectiveDate | DateTime | When plan took effect |
| ReviewDate | DateTime? | Next review date |

**Keys:**
- Primary: TreatmentPlanId
- Indexes: (CaseFileId, PlanVersion)

### TreatmentGoal
Specific objective under a treatment plan.

| Field | Type | Notes |
|-------|------|-------|
| TreatmentGoalId | GUID | Primary key |
| TreatmentPlanId | GUID | FK → TreatmentPlan |
| GoalText | string | Goal description |
| TargetDate | DateTime | Expected achievement date |
| Status | enum | Active/Achieved/Modified/Discontinued |
| MeasurementMethod | string? | How we'll know it's achieved |
| CreatedOnUtc | DateTime | |
| UpdatedOnUtc | DateTime | |
| CreatedByUserProfileId | GUID | |

**Keys:**
- Primary: TreatmentGoalId
- Indexes: (TreatmentPlanId, Status)

### TreatmentIntervention
Specific technique/action to work toward a goal.

| Field | Type | Notes |
|-------|------|-------|
| TreatmentInterventionId | GUID | Primary key |
| TreatmentGoalId | GUID | FK → TreatmentGoal |
| InterventionText | string | Description |
| Frequency | string? | "Weekly", "As needed", etc. |
| Status | enum | Active/Completed/On Hold |
| CreatedOnUtc | DateTime | |
| UpdatedOnUtc | DateTime | |

**Keys:**
- Primary: TreatmentInterventionId
- Indexes: (TreatmentGoalId)

---

## 6. Sessions & Clinical Notes

### Session
A meeting between therapist(s) and client.

| Field | Type | Notes |
|-------|------|-------|
| SessionId | GUID | Primary key |
| CaseFileId | GUID | FK → CaseFile |
| TherapistId | GUID | FK → Therapist |
| StartUtc | DateTime | ISO 8601 UTC |
| EndUtc | DateTime | |
| DurationMinutes | int | Calculated; could be derived |
| LocationType | enum | InPerson/Telehealth/Phone |
| TelehealthJoinLink | string? | Zoom, Meet URL, etc. |
| Status | enum | Scheduled/InProgress/Completed/NoShow/Cancelled |
| Attendees | string? | JSON list of attendees |
| CreatedOnUtc | DateTime | |
| CreatedByUserProfileId | GUID | |
| UpdatedOnUtc | DateTime | |

**Keys:**
- Primary: SessionId
- Indexes: (CaseFileId, StartUtc), (TherapistId, StartUtc), (Status)

### ClinicalNote
The therapist's documentation of a session. **Immutable once locked.**

| Field | Type | Notes |
|-------|------|-------|
| ClinicalNoteId | GUID | Primary key |
| SessionId | GUID | FK → Session (unique) |
| NoteType | enum | DAP/SOAP/BIRP/Progress/Intake/Termination |
| Content | string | Encrypted at rest in local DB |
| CreatedOnUtc | DateTime | |
| CreatedByUserProfileId | GUID | |
| UpdatedOnUtc | DateTime? | NULL if never updated |
| UpdatedByUserProfileId | GUID? | |
| LockedOnUtc | DateTime? | When locked (immutable) |
| LockedByUserProfileId | GUID? | Who locked it |

**Keys:**
- Primary: ClinicalNoteId
- Unique: (SessionId)
- Indexes: (SessionId), (CreatedByUserProfileId, CreatedOnUtc)

**Encryption:** Content encrypted with practice-level key. Decrypted in-memory on load.

**Locking:** Once LockedOnUtc is set, the note cannot be edited—only locked notes can be deleted (with audit trail).

### Assessment
Standardized instrument results (PHQ-9, GAD-7, etc.).

| Field | Type | Notes |
|-------|------|-------|
| AssessmentId | GUID | Primary key |
| CaseFileId | GUID | FK → CaseFile |
| Instrument | string | "PHQ-9", "GAD-7", "PCL-5", etc. |
| Score | int | Raw score |
| ResponsesJson | string? | Full responses for records |
| Severity | string? | "Minimal", "Mild", "Moderate", "Severe" |
| CompletedOnUtc | DateTime | |
| CompletedByUserProfileId | GUID? | Who administered |
| CreatedOnUtc | DateTime | |

**Keys:**
- Primary: AssessmentId
- Indexes: (CaseFileId, Instrument, CompletedOnUtc)

---

## 7. Scheduling

### Appointment
Scheduled future meeting.

| Field | Type | Notes |
|-------|------|-------|
| AppointmentId | GUID | Primary key |
| ClientId | GUID | FK → Client |
| TherapistId | GUID | FK → Therapist |
| StartUtc | DateTime | |
| EndUtc | DateTime | |
| AppointmentType | string? | "Session", "Intake", "Consultation", etc. |
| LocationType | enum | InPerson/Telehealth/Phone |
| TelehealthJoinLink | string? | |
| Status | enum | Booked/Confirmed/NoShow/Cancelled/Completed |
| Notes | string? | |
| CreatedOnUtc | DateTime | |
| CreatedByUserProfileId | GUID | |
| CancelledOnUtc | DateTime? | |
| CancelledByUserProfileId | GUID? | |

**Keys:**
- Primary: AppointmentId
- Indexes: (TherapistId, StartUtc), (ClientId, StartUtc), (Status)

### AvailabilityRule
Recurring availability pattern.

| Field | Type | Notes |
|-------|------|-------|
| AvailabilityRuleId | GUID | Primary key |
| TherapistId | GUID | FK → Therapist |
| DayOfWeek | int | 0=Sun, 1=Mon, ..., 6=Sat |
| StartTimeUtc | TimeSpan | e.g., 09:00 |
| EndTimeUtc | TimeSpan | e.g., 17:00 |
| IsActive | bool | |
| Exceptions | string? | JSON: dates when rule doesn't apply |
| CreatedOnUtc | DateTime | |
| UpdatedOnUtc | DateTime | |

**Keys:**
- Primary: AvailabilityRuleId
- Indexes: (TherapistId, DayOfWeek)

---

## 8. Billing

### ServiceCode
Billable service (CPT-like).

| Field | Type | Notes |
|-------|------|-------|
| ServiceCodeId | GUID | Primary key |
| PracticeId | GUID | FK → Practice |
| Code | string | e.g., "90834" (CPT) |
| Description | string | "Individual psychotherapy 45 min" |
| DefaultDurationMinutes | int | |
| DefaultRateUsd | decimal | |
| Status | enum | Active/Inactive |
| CreatedOnUtc | DateTime | |
| UpdatedOnUtc | DateTime | |

**Keys:**
- Primary: ServiceCodeId
- Unique: (PracticeId, Code)
- Indexes: (PracticeId, Status)

### Invoice
Bill to client or insurance.

| Field | Type | Notes |
|-------|------|-------|
| InvoiceId | GUID | Primary key |
| PracticeId | GUID | FK → Practice |
| ClientId | GUID | FK → Client |
| InvoiceNumber | string | e.g., "INV-2026-0001" |
| IssueDate | DateTime | |
| DueDate | DateTime | |
| TotalAmount | decimal | Sum of lines |
| PaidAmount | decimal | Sum of payments |
| Status | enum | Draft/Sent/Viewed/PartiallyPaid/Paid/Overdue/Cancelled |
| Notes | string? | |
| CreatedOnUtc | DateTime | |
| CreatedByUserProfileId | GUID | |
| UpdatedOnUtc | DateTime | |

**Keys:**
- Primary: InvoiceId
- Unique: (PracticeId, InvoiceNumber)
- Indexes: (ClientId, IssueDate), (Status)

### InvoiceLine
A line item on an invoice.

| Field | Type | Notes |
|-------|------|-------|
| InvoiceLineId | GUID | Primary key |
| InvoiceId | GUID | FK → Invoice |
| SessionId | GUID? | FK → Session (optional) |
| ServiceCodeId | GUID? | FK → ServiceCode (optional) |
| Description | string | Service description |
| Quantity | decimal | e.g., 1, 0.5 |
| UnitRate | decimal | Rate per unit |
| LineAmount | decimal | Qty × Rate |
| LineOrder | int | Display order |

**Keys:**
- Primary: InvoiceLineId
- Indexes: (InvoiceId), (SessionId)

### Payment
Money received against an invoice.

| Field | Type | Notes |
|-------|------|-------|
| PaymentId | GUID | Primary key |
| InvoiceId | GUID | FK → Invoice |
| Amount | decimal | |
| Method | enum | Check/ACH/CreditCard/Cash/Other |
| Reference | string? | Check number, confirmation ID, etc. |
| PaidOnUtc | DateTime | |
| ReceivedOnUtc | DateTime | When deposited |
| CreatedOnUtc | DateTime | |
| CreatedByUserProfileId | GUID | |

**Keys:**
- Primary: PaymentId
- Indexes: (InvoiceId, PaidOnUtc)

---

## 9. Documents & Files

### Document
References to stored files (consent forms, intake, releases, etc.).

| Field | Type | Notes |
|-------|------|-------|
| DocumentId | GUID | Primary key |
| PracticeId | GUID | FK → Practice |
| ClientId | GUID? | FK → Client (optional) |
| CaseFileId | GUID? | FK → CaseFile (optional) |
| DocumentType | string | "Consent", "IntakeForm", "Release", "ProgressReport", etc. |
| FileName | string | Original filename |
| LocalPath | string | Relative path in practice folder |
| FileSize | long | Bytes |
| FileHash | string | SHA256 for integrity |
| UploadedOnUtc | DateTime | |
| UploadedByUserProfileId | GUID | |
| SignedOnUtc | DateTime? | If applicable |
| SignedByUserProfileId | GUID? | |

**Keys:**
- Primary: DocumentId
- Indexes: (ClientId), (CaseFileId, DocumentType), (UploadedOnUtc)

**Storage:** Files stored locally (not in DB). Path is relative to practice data folder.

---

## 10. Cross-Cutting

### Tag
Flexible labeling for clients, cases, sessions, etc.

| Field | Type | Notes |
|-------|------|-------|
| TagId | GUID | Primary key |
| PracticeId | GUID | FK → Practice |
| Name | string | e.g., "Trauma", "Medication Management" |
| Category | string? | e.g., "Diagnosis", "Treatment" |
| Color | string? | Hex color for UI |
| CreatedOnUtc | DateTime | |

**Keys:**
- Primary: TagId
- Unique: (PracticeId, Name)

### EntityTag
Many-to-many: Tag applied to Client, CaseFile, Session, etc.

| Field | Type | Notes |
|-------|------|-------|
| EntityTagId | GUID | Primary key |
| TagId | GUID | FK → Tag |
| EntityType | enum | Client/CaseFile/Session/ClinicalNote |
| EntityId | GUID | The ID of the tagged entity |
| CreatedOnUtc | DateTime | |

**Keys:**
- Primary: EntityTagId
- Unique: (TagId, EntityType, EntityId)
- Indexes: (EntityType, EntityId), (TagId)

### Task
Operational task tracker (follow-ups, documentation, etc.).

| Field | Type | Notes |
|-------|------|-------|
| TaskId | GUID | Primary key |
| PracticeId | GUID | FK → Practice |
| ClientId | GUID? | FK → Client (optional) |
| CaseFileId | GUID? | FK → CaseFile (optional) |
| AssignedToUserProfileId | GUID? | FK → UserProfile (optional) |
| Title | string | |
| Description | string? | |
| Status | enum | Open/InProgress/Completed/Cancelled |
| Priority | enum | Low/Normal/High/Urgent |
| DueDate | DateTime? | |
| CreatedOnUtc | DateTime | |
| CreatedByUserProfileId | GUID | |
| CompletedOnUtc | DateTime? | |

**Keys:**
- Primary: TaskId
- Indexes: (PracticeId, Status, DueDate), (AssignedToUserProfileId, Status)

### AuditLog
Complete audit trail of who did what, when.

| Field | Type | Notes |
|-------|------|-------|
| AuditLogId | GUID | Primary key |
| PracticeId | GUID | FK → Practice |
| UserProfileId | GUID? | FK → UserProfile (NULL for system actions) |
| EntityType | string | "Client", "ClinicalNote", "Invoice", etc. |
| EntityId | GUID | The ID of the entity |
| Action | enum | Created/Updated/Deleted/Locked/Archived |
| Changes | string? | JSON: {field: old→new} |
| TimestampUtc | DateTime | |
| IpAddress | string? | For tracking |

**Keys:**
- Primary: AuditLogId
- Indexes: (PracticeId, TimestampUtc), (EntityType, EntityId, TimestampUtc)

### Outbox
Events waiting to sync to cloud (for future cloud integration).

| Field | Type | Notes |
|-------|------|-------|
| OutboxId | GUID | Primary key |
| PracticeId | GUID | FK → Practice |
| EventType | string | "ClientCreated", "NoteSubmitted", etc. |
| AggregateId | GUID | The root entity (Client, Session, etc.) |
| Payload | string | JSON event data |
| CreatedOnUtc | DateTime | |
| ProcessedOnUtc | DateTime? | NULL until synced |

**Keys:**
- Primary: OutboxId
- Indexes: (PracticeId, ProcessedOnUtc)

---

## 11. Design Decisions & Patterns

### Multi-Tenancy
- **Top-level is Practice** — All queries begin with PracticeId filter
- **Practice isolation:** No cross-practice queries
- **Soft delete:** Preserve audit trail and referential integrity

### Clinical Security
- **Notes are immutable once locked** — Prevents accidental/deliberate modification
- **Encryption at rest:** ClinicalNote.Content encrypted with practice key
- **Audit trail:** Every change logged with user and timestamp
- **Role-based access:** Therapist can only see own clients + notes

### Performance Indexes
```sql
-- User lookup
CREATE UNIQUE INDEX idx_userprofile_authid ON UserProfile(AuthUserId);
CREATE INDEX idx_userprofile_active ON UserProfile(IsActive);

-- Practice membership
CREATE UNIQUE INDEX idx_practiceuser_pk ON PracticeUser(PracticeId, UserProfileId);
CREATE INDEX idx_practiceuser_practice ON PracticeUser(PracticeId, IsActive);

-- Clients
CREATE INDEX idx_client_practice ON Client(PracticeId, LastName, FirstName);
CREATE INDEX idx_client_status ON Client(PracticeId, Status);

-- Sessions & Notes
CREATE INDEX idx_session_casefile ON Session(CaseFileId, StartUtc);
CREATE INDEX idx_session_therapist ON Session(TherapistId, StartUtc);
CREATE UNIQUE INDEX idx_clinicalnote_session ON ClinicalNote(SessionId);

-- Appointments
CREATE INDEX idx_appointment_therapist ON Appointment(TherapistId, StartUtc);
CREATE INDEX idx_appointment_client ON Appointment(ClientId, StartUtc);

-- Invoices
CREATE INDEX idx_invoice_client ON Invoice(ClientId, IssueDate);
CREATE UNIQUE INDEX idx_invoice_number ON Invoice(PracticeId, InvoiceNumber);
```

### Soft Delete Pattern
```csharp
// Example: soft delete a client
client.IsDeleted = true;
client.DeletedOnUtc = DateTime.UtcNow;
client.DeletedByUserProfileId = currentUser.UserProfileId;
```

All queries automatically filter out soft-deleted records:
```csharp
var activeClients = dbContext.Clients
    .Where(c => c.PracticeId == practiceId && !c.IsDeleted)
    .ToList();
```

### Encryption Pattern
```csharp
// On save
var encrypted = EncryptionService.Encrypt(clinicalNote.Content, practiceKey);
clinicalNote.Content = encrypted;

// On load
var decrypted = EncryptionService.Decrypt(clinicalNote.Content, practiceKey);
```

---

## 12. What to Build First (MVP)

**Phase 1 (Week 1-2):**
- [x] Practice + Users (Supabase auth)
- [ ] Therapist profile CRUD
- [ ] Client CRUD
- [ ] Appointment + Session

**Phase 2 (Week 3-4):**
- [ ] Clinical notes (DAP, with locking)
- [ ] Basic treatment planning
- [ ] Assessments (PHQ-9, GAD-7 presets)

**Phase 3 (Week 5-6):**
- [ ] Invoices + Payments
- [ ] Invoice-to-Session linking
- [ ] Basic reporting

**Phase 4 (Later):**
- [ ] Insurance policies & claims
- [ ] Advanced scheduling features
- [ ] Cloud sync (Outbox pattern)
- [ ] Mobile companion app

---

## 13. Migration Path (SQLite → SQL Server/Azure)

All entities use **GUID primary keys** (not sequential integers) to simplify cross-platform migration.

1. Export SQLite as SQL (via EF Core)
2. Create matching schema in SQL Server
3. Bulk import with identity preserved
4. Update connection string
5. No application code changes needed

---

*Last updated: 2026-01-02*  
*Status: In-progress (Phase 1)*
