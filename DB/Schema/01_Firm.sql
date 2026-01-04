-- ============================================================================
-- PRAXIS DATABASE SCHEMA: Firm (Tenant)
-- ============================================================================
-- The root tenant entity. All business data is scoped to a Firm.
-- ============================================================================

CREATE TABLE IF NOT EXISTS firm (
    firm_id             uuid        PRIMARY KEY,
    name                text        NOT NULL,
    time_zone_iana      text        NOT NULL,
    status              text        NOT NULL,
    created_utc         timestamptz NOT NULL,
    updated_utc         timestamptz NOT NULL,
    version             integer     NOT NULL DEFAULT 1,
    is_deleted          boolean     NOT NULL DEFAULT false,
    deleted_utc         timestamptz NULL
);

-- Unique firm name (only among non-deleted)
CREATE UNIQUE INDEX IF NOT EXISTS ux_firm_name
ON firm (name)
WHERE is_deleted = false;
