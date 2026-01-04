-- ============================================================================
-- PRAXIS DATABASE SCHEMA: KeyRing (Encryption Key Metadata)
-- ============================================================================
-- Stores metadata about encryption keys used for clinical data.
-- DOES NOT store the actual key material - keys are stored securely elsewhere.
-- Enables key rotation: new keys can be added, old keys retired.
-- ClinicalNote references key_id to know which key was used for encryption.
-- ============================================================================

CREATE TABLE IF NOT EXISTS key_ring (
    key_id              uuid        PRIMARY KEY,
    firm_id             uuid        NOT NULL REFERENCES firm (firm_id),
    key_name            text        NOT NULL,           -- Human-readable identifier
    algorithm           text        NOT NULL DEFAULT 'AES-256-GCM',
    key_version         integer     NOT NULL DEFAULT 1,
    status              text        NOT NULL DEFAULT 'Active',  -- Active, Retired, Compromised
    created_utc         timestamptz NOT NULL,
    activated_utc       timestamptz NULL,       -- When key was put into active use
    retired_utc         timestamptz NULL,       -- When key was retired
    expires_utc         timestamptz NULL,       -- Optional expiration date
    notes               text        NULL        -- Reason for rotation, etc.
);

-- Only one active key per firm (current encryption key)
CREATE UNIQUE INDEX IF NOT EXISTS ux_key_ring_firm_active
ON key_ring (firm_id)
WHERE status = 'Active';

-- Lookup by firm
CREATE INDEX IF NOT EXISTS ix_key_ring_firm
ON key_ring (firm_id);

-- ============================================================================
-- KEY MANAGEMENT NOTES:
-- ============================================================================
-- 1. The actual key material is NOT stored in this table
-- 2. Key material should be stored in a secure location:
--    - Windows DPAPI / Credential Manager
--    - Hardware Security Module (HSM)
--    - Key Management Service (KMS)
--    - Encrypted configuration file (development only)
-- 3. key_id links ClinicalNote.key_id to this table
-- 4. When rotating keys:
--    a. Create new key_ring record with status='Active'
--    b. Set old key_ring record status='Retired'
--    c. New notes use new key, old notes still use old key
--    d. Optionally re-encrypt old notes with new key (background job)
-- ============================================================================
