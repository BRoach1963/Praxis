-- ============================================================================
-- PRAXIS DATABASE SCHEMA: FirmUser (Membership + Role)
-- ============================================================================
-- Links a UserProfile to a Firm with a specific role.
-- A user can be a member of multiple firms.
-- ============================================================================

CREATE TABLE IF NOT EXISTS firm_user (
    firm_user_id        uuid        PRIMARY KEY,
    firm_id             uuid        NOT NULL REFERENCES firm (firm_id),
    user_profile_id     uuid        NOT NULL REFERENCES user_profile (user_profile_id),
    role                text        NOT NULL,       -- Owner, Admin, Therapist, Biller, Staff, ReadOnly
    is_active           boolean     NOT NULL DEFAULT true,
    last_login_utc      timestamptz NULL,
    created_utc         timestamptz NOT NULL,
    updated_utc         timestamptz NOT NULL,
    version             integer     NOT NULL DEFAULT 1,
    is_deleted          boolean     NOT NULL DEFAULT false,
    deleted_utc         timestamptz NULL
);

-- Unique user per firm (only among non-deleted)
CREATE UNIQUE INDEX IF NOT EXISTS ux_firm_user_firm_user
ON firm_user (firm_id, user_profile_id)
WHERE is_deleted = false;

-- Fast lookup by firm
CREATE INDEX IF NOT EXISTS ix_firm_user_firm
ON firm_user (firm_id);
