-- ============================================================================
-- PRAXIS DATABASE SCHEMA: TherapistClient (Assignment)
-- ============================================================================
-- Many-to-many assignment of therapists to clients.
-- Tracks date ranges for historical assignments.
-- A client may have multiple therapists (e.g., primary + supervisor).
-- ============================================================================

CREATE TABLE IF NOT EXISTS therapist_client (
    therapist_client_id uuid        PRIMARY KEY,
    therapist_id        uuid        NOT NULL REFERENCES therapist (therapist_id),
    client_id           uuid        NOT NULL REFERENCES client (client_id),
    assignment_type     text        NOT NULL DEFAULT 'Primary',  -- Primary, Supervisor, Consultant
    assigned_date       date        NOT NULL,
    unassigned_date     date        NULL,
    is_active           boolean     NOT NULL DEFAULT true,
    created_utc         timestamptz NOT NULL,
    updated_utc         timestamptz NOT NULL,
    version             integer     NOT NULL DEFAULT 1,
    is_deleted          boolean     NOT NULL DEFAULT false,
    deleted_utc         timestamptz NULL
);

-- Only one active primary therapist per client (among non-deleted)
CREATE UNIQUE INDEX IF NOT EXISTS ux_therapist_client_primary
ON therapist_client (client_id)
WHERE assignment_type = 'Primary' AND is_active = true AND is_deleted = false;

-- Fast lookup by therapist (my clients)
CREATE INDEX IF NOT EXISTS ix_therapist_client_therapist
ON therapist_client (therapist_id)
WHERE is_deleted = false;

-- Fast lookup by client (my therapists)
CREATE INDEX IF NOT EXISTS ix_therapist_client_client
ON therapist_client (client_id)
WHERE is_deleted = false;
