-- ============================================================================
-- PRAXIS DATABASE SCHEMA: CaseFile (Episode of Care)
-- ============================================================================
-- Represents an episode of care / treatment episode for a client.
-- A client can have multiple case files over time (e.g., re-admission).
-- Contains presenting problem, diagnosis, treatment goals.
-- ============================================================================

CREATE TABLE IF NOT EXISTS case_file (
    case_file_id        uuid        PRIMARY KEY,
    client_id           uuid        NOT NULL REFERENCES client (client_id),
    therapist_id        uuid        NOT NULL REFERENCES therapist (therapist_id),
    case_number         text        NULL,           -- Optional case reference number
    opened_date         date        NOT NULL,
    closed_date         date        NULL,
    status              text        NOT NULL DEFAULT 'Open',  -- Open, Closed, OnHold
    presenting_problem  text        NULL,
    diagnosis_primary   text        NULL,           -- ICD-10 or DSM-5 code
    diagnosis_secondary text        NULL,
    treatment_modality  text        NULL,           -- Individual, Group, Family, etc.
    session_frequency   text        NULL,           -- Weekly, Biweekly, PRN, etc.
    notes               text        NULL,           -- General case notes (non-clinical)
    created_utc         timestamptz NOT NULL,
    updated_utc         timestamptz NOT NULL,
    version             integer     NOT NULL DEFAULT 1,
    is_deleted          boolean     NOT NULL DEFAULT false,
    deleted_utc         timestamptz NULL
);

-- Fast lookup by client (all case files for a client)
CREATE INDEX IF NOT EXISTS ix_case_file_client
ON case_file (client_id)
WHERE is_deleted = false;

-- Fast lookup by therapist (all my cases)
CREATE INDEX IF NOT EXISTS ix_case_file_therapist
ON case_file (therapist_id)
WHERE is_deleted = false;

-- Open cases by therapist
CREATE INDEX IF NOT EXISTS ix_case_file_therapist_open
ON case_file (therapist_id)
WHERE status = 'Open' AND is_deleted = false;
