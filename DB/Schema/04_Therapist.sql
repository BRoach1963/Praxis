-- ============================================================================
-- PRAXIS DATABASE SCHEMA: Therapist (Clinical Persona)
-- ============================================================================
-- Clinical practitioner profile with license and credentials.
-- Linked to FirmUser for login capability (optional - some therapists may not login).
-- Therapist ≠ FirmUser role "Therapist" - they are separate concepts.
-- ============================================================================

CREATE TABLE IF NOT EXISTS therapist (
    therapist_id        uuid        PRIMARY KEY,
    firm_id             uuid        NOT NULL REFERENCES firm (firm_id),
    firm_user_id        uuid        NOT NULL REFERENCES firm_user (firm_user_id),
    first_name          text        NOT NULL,
    last_name           text        NOT NULL,
    license_type        text        NULL,           -- LCSW, LPC, PhD, etc.
    license_number      text        NULL,
    license_state       text        NULL,           -- State abbreviation
    npi_number          text        NULL,           -- National Provider Identifier
    is_clinical_active  boolean     NOT NULL DEFAULT true,
    created_utc         timestamptz NOT NULL,
    updated_utc         timestamptz NOT NULL,
    version             integer     NOT NULL DEFAULT 1,
    is_deleted          boolean     NOT NULL DEFAULT false,
    deleted_utc         timestamptz NULL
);

-- One therapist per firm_user (only among non-deleted)
CREATE UNIQUE INDEX IF NOT EXISTS ux_therapist_firm_user
ON therapist (firm_user_id)
WHERE is_deleted = false;

-- Fast lookup by firm
CREATE INDEX IF NOT EXISTS ix_therapist_firm
ON therapist (firm_id);
