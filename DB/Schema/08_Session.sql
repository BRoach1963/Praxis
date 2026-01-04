-- ============================================================================
-- PRAXIS DATABASE SCHEMA: Session (Clinical Encounter)
-- ============================================================================
-- A single clinical encounter / appointment.
-- Linked to a case file (episode of care).
-- Contains scheduling info and session metadata.
-- Clinical notes are stored separately in clinical_note table (encrypted).
-- ============================================================================

CREATE TABLE IF NOT EXISTS session (
    session_id          uuid        PRIMARY KEY,
    case_file_id        uuid        NOT NULL REFERENCES case_file (case_file_id),
    therapist_id        uuid        NOT NULL REFERENCES therapist (therapist_id),
    session_date        date        NOT NULL,
    start_time          time        NULL,
    end_time            time        NULL,
    duration_minutes    integer     NULL,
    session_type        text        NOT NULL DEFAULT 'Individual',  -- Individual, Group, Family, Intake, Discharge
    session_format      text        NOT NULL DEFAULT 'InPerson',    -- InPerson, Telehealth, Phone
    status              text        NOT NULL DEFAULT 'Scheduled',   -- Scheduled, Completed, Cancelled, NoShow
    cancellation_reason text        NULL,
    billing_code        text        NULL,           -- CPT code for billing
    billing_status      text        NULL,           -- Pending, Submitted, Paid, etc.
    created_utc         timestamptz NOT NULL,
    updated_utc         timestamptz NOT NULL,
    version             integer     NOT NULL DEFAULT 1,
    is_deleted          boolean     NOT NULL DEFAULT false,
    deleted_utc         timestamptz NULL
);

-- Fast lookup by case file (session history for a case)
CREATE INDEX IF NOT EXISTS ix_session_case_file
ON session (case_file_id)
WHERE is_deleted = false;

-- Fast lookup by therapist (my sessions)
CREATE INDEX IF NOT EXISTS ix_session_therapist
ON session (therapist_id)
WHERE is_deleted = false;

-- Sessions by date (for scheduling views)
CREATE INDEX IF NOT EXISTS ix_session_date
ON session (session_date)
WHERE is_deleted = false;

-- Therapist sessions by date (my schedule)
CREATE INDEX IF NOT EXISTS ix_session_therapist_date
ON session (therapist_id, session_date)
WHERE is_deleted = false;
