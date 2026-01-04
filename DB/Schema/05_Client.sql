-- ============================================================================
-- PRAXIS DATABASE SCHEMA: Client (Patient/Client)
-- ============================================================================
-- Person receiving services from the practice.
-- Belongs to a firm (tenant isolation).
-- ============================================================================

CREATE TABLE IF NOT EXISTS client (
    client_id           uuid        PRIMARY KEY,
    firm_id             uuid        NOT NULL REFERENCES firm (firm_id),
    first_name          text        NOT NULL,
    last_name           text        NOT NULL,
    preferred_name      text        NULL,
    date_of_birth       date        NULL,
    email               text        NULL,
    phone               text        NULL,
    address_line1       text        NULL,
    address_line2       text        NULL,
    city                text        NULL,
    state               text        NULL,
    postal_code         text        NULL,
    emergency_contact   text        NULL,
    emergency_phone     text        NULL,
    intake_date         date        NULL,
    is_active           boolean     NOT NULL DEFAULT true,
    created_utc         timestamptz NOT NULL,
    updated_utc         timestamptz NOT NULL,
    version             integer     NOT NULL DEFAULT 1,
    is_deleted          boolean     NOT NULL DEFAULT false,
    deleted_utc         timestamptz NULL
);

-- Fast lookup by firm
CREATE INDEX IF NOT EXISTS ix_client_firm
ON client (firm_id);

-- Search by name within firm
CREATE INDEX IF NOT EXISTS ix_client_firm_name
ON client (firm_id, last_name, first_name)
WHERE is_deleted = false;
