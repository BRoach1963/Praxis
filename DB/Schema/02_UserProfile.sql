-- ============================================================================
-- PRAXIS DATABASE SCHEMA: UserProfile
-- ============================================================================
-- Authentication identity. Maps to local auth or external auth provider.
-- A user can belong to multiple firms via FirmUser.
-- ============================================================================

CREATE TABLE IF NOT EXISTS user_profile (
    user_profile_id     uuid        PRIMARY KEY,
    auth_user_id        uuid        NOT NULL,
    email               text        NOT NULL,
    password_hash       text        NULL,           -- bcrypt hash (local auth)
    display_name        text        NULL,
    created_utc         timestamptz NOT NULL,
    updated_utc         timestamptz NOT NULL,
    version             integer     NOT NULL DEFAULT 1,
    is_deleted          boolean     NOT NULL DEFAULT false,
    deleted_utc         timestamptz NULL
);

-- Unique auth_user_id (only among non-deleted)
CREATE UNIQUE INDEX IF NOT EXISTS ux_user_profile_auth_user_id
ON user_profile (auth_user_id)
WHERE is_deleted = false;

-- Unique email (case-insensitive, only among non-deleted)
CREATE UNIQUE INDEX IF NOT EXISTS ux_user_profile_email
ON user_profile (lower(email))
WHERE is_deleted = false;
