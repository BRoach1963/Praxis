# Praxis Database Scripts

PostgreSQL schema for Praxis therapy practice management.

## Architecture

- **Local PostgreSQL**: All application data (auth, clients, sessions, clinical notes)
- **Supabase (optional)**: API key storage only (AI APIs, external credentials)

## Schema Design Principles

- **UUIDs** for all primary keys
- **Soft delete** via `is_deleted` + `deleted_utc`
- **Optimistic concurrency** via `version` column
- **Audit timestamps** via `created_utc` + `updated_utc`
- **Multi-tenant** via `firm_id` on all business tables
- **Encrypted at rest** for clinical notes (application-layer AES-256-GCM)

## Folder Structure

```
DB/
├── Schema/
│   ├── 00_Extensions.sql       -- pgcrypto extension
│   ├── 01_Firm.sql             -- firm (tenant)
│   ├── 02_UserProfile.sql      -- user_profile (auth identity)
│   ├── 03_FirmUser.sql         -- firm_user (membership + role)
│   ├── 04_Therapist.sql        -- therapist (clinical persona)
│   ├── 05_Client.sql           -- client (patient)
│   ├── 06_TherapistClient.sql  -- therapist_client (assignment)
│   ├── 07_CaseFile.sql         -- case_file (episode of care)
│   ├── 08_Session.sql          -- session (encounter)
│   ├── 09_KeyRing.sql          -- key_ring (encryption key metadata)
│   └── 10_ClinicalNote.sql     -- clinical_note (encrypted payload)
├── Seed/
│   └── 01_TestData.sql         -- Test firm, user, therapist
└── README.md
```

## Setup Instructions

### Prerequisites

1. PostgreSQL 14+ installed locally
2. Create database and user:

```sql
CREATE USER praxis_app WITH PASSWORD 'your-secure-password';
CREATE DATABASE praxis OWNER praxis_app;
GRANT ALL PRIVILEGES ON DATABASE praxis TO praxis_app;
```

### Run Schema Scripts

Execute in order:

```bash
psql -U praxis_app -d praxis -f DB/Schema/00_Extensions.sql
psql -U praxis_app -d praxis -f DB/Schema/01_Firm.sql
# ... continue through 10_ClinicalNote.sql
```

Or run all at once:

```bash
cat DB/Schema/*.sql | psql -U praxis_app -d praxis
```

### Seed Test Data (Development Only)

```bash
psql -U praxis_app -d praxis -f DB/Seed/01_TestData.sql
```

## Entity Relationships

```
Firm (tenant)
 ├─ UserProfile (auth identity - can access multiple firms)
 │   └─ FirmUser (membership + role per firm)
 │       └─ Therapist (clinical persona, optional)
 │
 ├─ Client (patient)
 │   ├─ TherapistClient (assignment to therapist)
 │   └─ CaseFile (episode of care)
 │       └─ Session (encounter)
 │           └─ ClinicalNote (encrypted documentation)
 │
 └─ KeyRing (encryption key metadata per firm)
```

## Encryption Model

Clinical notes are encrypted at the application layer:

| Column | Purpose |
|--------|---------|
| `ciphertext` | AES-256-GCM encrypted content |
| `nonce` | Unique IV per encryption |
| `key_id` | Reference to KeyRing (which key version) |
| `algorithm` | e.g., "aes-256-gcm" |
| `aad` | Additional authenticated data (tamper binding) |
| `content_hash` | SHA-256 of plaintext for integrity |

### Key Rotation

1. Create new `key_ring` entry with incremented `key_version`
2. Set new key as `is_active = true`
3. Existing notes keep their `key_id` reference
4. Re-encrypt notes in background if needed

## Connection String

```json
{
  "ConnectionStrings": {
    "PraxisDb": "Host=localhost;Port=5432;Database=praxis;Username=praxis_app;Password=your-secure-password"
  }
}
```
