-- ============================================================================
-- SUPABASE SCHEMA ALTERATIONS
-- ============================================================================
-- Run this script to add missing columns and tables to an existing Praxis schema
-- This handles upgrading from older schema versions
-- ============================================================================

-- Add missing column to app_user if it doesn't exist
ALTER TABLE public.app_user 
ADD COLUMN IF NOT EXISTS must_change_password BOOLEAN NOT NULL DEFAULT false;

-- Create index on must_change_password if it doesn't exist
CREATE INDEX IF NOT EXISTS idx_app_user_must_change_password ON public.app_user(must_change_password);

-- Create password_reset_token table if it doesn't exist
CREATE TABLE IF NOT EXISTS public.password_reset_token (
    token_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    app_user_id UUID NOT NULL REFERENCES public.app_user(app_user_id) ON DELETE CASCADE,
    token VARCHAR(255) NOT NULL UNIQUE,
    expires_on_utc TIMESTAMPTZ NOT NULL,
    used_on_utc TIMESTAMPTZ,
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_password_reset_token_app_user_id ON public.password_reset_token(app_user_id);
CREATE INDEX IF NOT EXISTS idx_password_reset_token_token ON public.password_reset_token(token);
CREATE INDEX IF NOT EXISTS idx_password_reset_token_expires_on_utc ON public.password_reset_token(expires_on_utc);

-- Create or replace user management helper functions

-- Function to create a user with temp password (for admin onboarding)
CREATE OR REPLACE FUNCTION create_app_user_with_temp_password(
    p_email CITEXT,
    p_first_name VARCHAR,
    p_last_name VARCHAR,
    p_password_hash VARCHAR,
    p_display_name VARCHAR DEFAULT NULL,
    p_avatar_url VARCHAR DEFAULT NULL
)
RETURNS UUID AS $$
DECLARE
    v_app_user_id UUID;
BEGIN
    INSERT INTO public.app_user (
        email, 
        password_hash, 
        first_name, 
        last_name, 
        display_name,
        avatar_url,
        must_change_password,
        is_active
    ) VALUES (
        p_email,
        p_password_hash,
        p_first_name,
        p_last_name,
        COALESCE(p_display_name, p_first_name || ' ' || p_last_name),
        p_avatar_url,
        true,
        true
    )
    RETURNING app_user_id INTO v_app_user_id;
    
    RETURN v_app_user_id;
EXCEPTION WHEN unique_violation THEN
    RAISE EXCEPTION 'Email % already exists', p_email;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to generate a password reset token
CREATE OR REPLACE FUNCTION generate_password_reset_token(p_app_user_id UUID)
RETURNS VARCHAR AS $$
DECLARE
    v_token VARCHAR;
    v_expires_on_utc TIMESTAMPTZ;
BEGIN
    v_token := encode(gen_random_bytes(32), 'hex');
    v_expires_on_utc := NOW() + INTERVAL '24 hours';
    
    INSERT INTO public.password_reset_token (app_user_id, token, expires_on_utc)
    VALUES (p_app_user_id, v_token, v_expires_on_utc);
    
    RETURN v_token;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to validate and mark a password reset token as used
CREATE OR REPLACE FUNCTION use_password_reset_token(p_token VARCHAR)
RETURNS BOOLEAN AS $$
DECLARE
    v_token_id UUID;
BEGIN
    SELECT token_id INTO v_token_id
    FROM public.password_reset_token
    WHERE token = p_token
    AND used_on_utc IS NULL
    AND expires_on_utc > NOW()
    LIMIT 1;
    
    IF v_token_id IS NULL THEN
        RETURN false;
    END IF;
    
    UPDATE public.password_reset_token
    SET used_on_utc = NOW()
    WHERE token_id = v_token_id;
    
    RETURN true;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to get app_user_id from valid password reset token
CREATE OR REPLACE FUNCTION get_user_from_password_reset_token(p_token VARCHAR)
RETURNS UUID AS $$
DECLARE
    v_app_user_id UUID;
BEGIN
    SELECT app_user_id INTO v_app_user_id
    FROM public.password_reset_token
    WHERE token = p_token
    AND used_on_utc IS NULL
    AND expires_on_utc > NOW()
    LIMIT 1;
    
    RETURN v_app_user_id;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- END OF ALTERATIONS
-- ============================================================================
