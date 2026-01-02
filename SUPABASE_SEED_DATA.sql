-- ============================================================================
-- PRAXIS TEST DATA SETUP
-- ============================================================================
-- Firm: Prickly Cactus Therapy
-- User: Brian E Roach (Therapist)
-- ============================================================================

-- STEP 1: Create auth user in Supabase Auth
-- ============================================================================
-- This MUST be done via Supabase Dashboard or Auth API, NOT direct SQL INSERT
-- Navigate to: Authentication → Users → Add User
-- Email: brian@pricklycactussoftware.com
-- Password: 12345678
-- Auto-confirm user (check the box)
-- 
-- After creating, you'll get an auth_user_id (UUID). Use that UUID in the INSERT below.
-- For this example, I'm using a placeholder UUID: 550e8400-e29b-41d4-a716-446655440000
-- Replace this with the actual auth_user_id from Supabase.

-- STEP 2: Insert UserProfile
-- ============================================================================
INSERT INTO public.user_profile (
    user_profile_id,
    auth_user_id,
    email,
    display_name,
    is_active,
    created_on_utc,
    updated_on_utc
) VALUES (
    '550e8400-e29b-41d4-a716-446655440001'::UUID,
    '550e8400-e29b-41d4-a716-446655440000'::UUID,  -- Replace with actual auth_user_id from Supabase
    'brian@pricklycactussoftware.com',
    'Brian E Roach',
    true,
    NOW(),
    NOW()
) ON CONFLICT (auth_user_id) DO NOTHING;

-- STEP 3: Insert Practice
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

-- STEP 4: Insert PracticeUser (Brian as Therapist in Prickly Cactus)
-- ============================================================================
INSERT INTO public.practice_user (
    practice_user_id,
    practice_id,
    user_profile_id,
    role,
    is_active,
    invited_on_utc,
    accepted_on_utc,
    created_on_utc,
    updated_on_utc
) VALUES (
    '550e8400-e29b-41d4-a716-446655440003'::UUID,
    '550e8400-e29b-41d4-a716-446655440002'::UUID,
    '550e8400-e29b-41d4-a716-446655440001'::UUID,
    'Therapist'::practice_role,
    true,
    NOW(),
    NOW(),  -- Automatically accept invitation for testing
    NOW(),
    NOW()
) ON CONFLICT DO NOTHING;

-- STEP 5: Insert Therapist (Clinical Profile)
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
-- Query to verify the setup worked:
SELECT 
    up.display_name,
    up.email,
    p.name AS practice_name,
    pu.role AS practice_role,
    t.first_name || ' ' || t.last_name AS therapist_name
FROM public.user_profile up
LEFT JOIN public.practice_user pu ON up.user_profile_id = pu.user_profile_id
LEFT JOIN public.practice p ON pu.practice_id = p.practice_id
LEFT JOIN public.therapist t ON pu.practice_user_id = t.practice_user_id
WHERE up.email = 'brian@pricklycactussoftware.com';

-- ============================================================================
-- OPTIONAL: Add therapist credentials
-- ============================================================================
-- Uncomment and fill in if you have license information:

-- UPDATE public.therapist
-- SET 
--     license_number = 'TX123456',
--     license_state = 'TX',
--     npi = '1234567890',
--     credential = 'LCSW',
--     specialty = 'Cognitive Behavioral Therapy',
--     updated_on_utc = NOW()
-- WHERE therapist_id = '550e8400-e29b-41d4-a716-446655440004'::UUID;
