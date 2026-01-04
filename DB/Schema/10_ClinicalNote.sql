-- ============================================================================
-- PRAXIS DATABASE SCHEMA: ClinicalNote (Encrypted Clinical Documentation)
-- ============================================================================
-- Stores encrypted clinical documentation for sessions.
-- The actual note content is stored as encrypted ciphertext (bytea).
-- Application layer handles encryption/decryption using AES-256-GCM.
-- References key_ring.key_id to identify which key was used for encryption.
-- ============================================================================

CREATE TABLE IF NOT EXISTS clinical_note (
    clinical_note_id    uuid        PRIMARY KEY,
    session_id          uuid        NOT NULL REFERENCES session (session_id),
    key_id              uuid        NOT NULL REFERENCES key_ring (key_id),
    note_type           text        NOT NULL DEFAULT 'Progress',  -- Progress, Intake, Discharge, Treatment Plan, etc.
    
    -- Encrypted payload
    ciphertext          bytea       NOT NULL,       -- AES-256-GCM encrypted note content
    nonce               bytea       NOT NULL,       -- 12-byte IV/nonce for AES-GCM
    algorithm           text        NOT NULL DEFAULT 'AES-256-GCM',
    aad                 bytea       NULL,           -- Additional Authenticated Data (optional)
    content_hash        bytea       NULL,           -- SHA-256 hash of plaintext for integrity verification
    
    -- Metadata (not encrypted - needed for queries)
    status              text        NOT NULL DEFAULT 'Draft',  -- Draft, Final, Amended, Addendum
    finalized_utc       timestamptz NULL,           -- When note was finalized/signed
    finalized_by_id     uuid        NULL REFERENCES therapist (therapist_id),
    word_count          integer     NULL,           -- Approximate word count (for reporting)
    
    -- Standard audit columns
    created_utc         timestamptz NOT NULL,
    updated_utc         timestamptz NOT NULL,
    version             integer     NOT NULL DEFAULT 1,
    is_deleted          boolean     NOT NULL DEFAULT false,
    deleted_utc         timestamptz NULL
);

-- One note per session per type (among non-deleted)
CREATE UNIQUE INDEX IF NOT EXISTS ux_clinical_note_session_type
ON clinical_note (session_id, note_type)
WHERE is_deleted = false;

-- Fast lookup by session
CREATE INDEX IF NOT EXISTS ix_clinical_note_session
ON clinical_note (session_id)
WHERE is_deleted = false;

-- Fast lookup by key (for key rotation - find notes using a specific key)
CREATE INDEX IF NOT EXISTS ix_clinical_note_key
ON clinical_note (key_id);

-- Draft notes by therapist (for "my incomplete notes" view)
-- Note: Need to join through session -> case_file -> therapist
-- This index supports that join
CREATE INDEX IF NOT EXISTS ix_clinical_note_status
ON clinical_note (status)
WHERE is_deleted = false AND status = 'Draft';

-- ============================================================================
-- ENCRYPTION FLOW:
-- ============================================================================
-- ENCRYPT (Application Layer):
--   1. Generate random 12-byte nonce
--   2. Look up active key_id from key_ring for user's firm
--   3. Retrieve actual key material from secure storage (DPAPI, etc.)
--   4. Encrypt plaintext with AES-256-GCM using key + nonce
--   5. Compute SHA-256 hash of plaintext
--   6. Store: ciphertext, nonce, key_id, algorithm, content_hash
--
-- DECRYPT (Application Layer):
--   1. Read key_id from clinical_note record
--   2. Retrieve actual key material from secure storage
--   3. Decrypt ciphertext using key + nonce
--   4. Verify content_hash matches decrypted plaintext
--   5. Return plaintext to application
--
-- KEY ROTATION:
--   - New notes use new active key
--   - Old notes remain encrypted with old key (still readable)
--   - Background job can re-encrypt old notes if needed
-- ============================================================================
