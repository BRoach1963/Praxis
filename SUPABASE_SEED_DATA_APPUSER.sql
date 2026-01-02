-- ============================================================================
-- PRAXIS TEST DATA SETUP
-- ============================================================================
-- Firm: Prickly Cactus Therapy
-- User: Brian E Roach (Therapist)
-- Email: brian@pricklycactussoftware.com
-- Password: 12345678 (hashed in application)
-- ============================================================================

-- STEP 1: Create AppUser
-- ============================================================================
-- The password_hash should be created by the application (bcrypt or similar).
-- For testing purposes, use a bcrypt hash of "12345678"
-- You can generate one using: https://bcrypt-generator.com/ or your app's hashing

INSERT INTO public.app_user (
    app_user_id,
    email,
    password_hash,
    first_name,
    last_name,
    display_name,
    is_active,
    created_on_utc,
    updated_on_utc
) VALUES (
    '550e8400-e29b-41d4-a716-446655440001'::UUID,
    'brian@pricklycactussoftware.com',
    '$2b$10$Nz2Q3UzKkWK7UXrLxLq6p.9N.uKqXlYaV6pJ7kV6Zq0iGvLvfJZfm',  -- bcrypt hash for "12345678"
    'Brian',
    'Roach',
    'Brian E Roach',
    true,
    NOW(),
    NOW()
) ON CONFLICT (email) DO NOTHING;

-- STEP 2: Create Practice
-- ============================================================================
INSERT INTO public.practice (
    practice_id,
    name,
    time_zone,
    default_currency,
    default_session_length_minutes,
    is_active,
    created_on_utc,
    updated_on_utc
) VALUES (
    '550e8400-e29b-41d4-a716-446655440002'::UUID,
    'Prickly Cactus Therapy',
    'America/Chicago',
    'USD',
    60,
    true,
    NOW(),
    NOW()
) ON CONFLICT DO NOTHING;

-- STEP 3: Create PracticeUser (Brian as Therapist in Prickly Cactus)
-- ============================================================================
INSERT INTO public.practice_user (
    practice_user_id,
    practice_id,
    app_user_id,
    role,
    is_active,
    invited_on_utc,
    accepted_on_utc,
    created_on_utc,
    updated_on_utc,
    created_by_app_user_id
) VALUES (
    '550e8400-e29b-41d4-a716-446655440003'::UUID,
    '550e8400-e29b-41d4-a716-446655440002'::UUID,
    '550e8400-e29b-41d4-a716-446655440001'::UUID,
    'Therapist'::practice_role,
    true,
    NOW(),
    NOW(),  -- Auto-accept for testing
    NOW(),
    NOW(),
    '550e8400-e29b-41d4-a716-446655440001'::UUID  -- Self-created
) ON CONFLICT DO NOTHING;

-- STEP 4: Create Therapist Clinical Profile
-- ============================================================================
INSERT INTO public.therapist (
    therapist_id,
    practice_id,
    practice_user_id,
    first_name,
    last_name,
    is_active,
    created_on_utc,
    updated_on_utc
) VALUES (
    '550e8400-e29b-41d4-a716-446655440004'::UUID,
    '550e8400-e29b-41d4-a716-446655440002'::UUID,
    '550e8400-e29b-41d4-a716-446655440003'::UUID,
    'Brian',
    'Roach',
    true,
    NOW(),
    NOW()
) ON CONFLICT DO NOTHING;

-- ============================================================================
-- VERIFY SETUP
-- ============================================================================
SELECT 
    au.app_user_id,
    au.display_name,
    au.email,
    p.practice_id,
    p.name AS practice_name,
    pu.role AS practice_role,
    t.therapist_id,
    t.first_name || ' ' || t.last_name AS therapist_name
FROM public.app_user au
LEFT JOIN public.practice_user pu ON au.app_user_id = pu.app_user_id
LEFT JOIN public.practice p ON pu.practice_id = p.practice_id
LEFT JOIN public.therapist t ON pu.practice_user_id = t.practice_user_id
WHERE au.email = 'brian@pricklycactussoftware.com';

-- ============================================================================
-- OPTIONAL: Add therapist credentials/credentials
-- ============================================================================
-- UPDATE public.therapist
-- SET 
--     license_number = 'TX123456',
--     license_state = 'TX',
--     npi = '1234567890',
--     credential = 'LCSW',
--     specialty = 'Cognitive Behavioral Therapy',
--     bio = 'Experienced therapist specializing in CBT.',
--     updated_on_utc = NOW()
-- WHERE therapist_id = '550e8400-e29b-41d4-a716-446655440004'::UUID;
