-- ============================================================================
-- SUPABASE INSERT TEST DATA
-- ============================================================================
-- Inserts test data: Prickly Cactus Therapy practice and initial user
-- Password hash is bcrypt for: TempPass123!
-- ============================================================================

BEGIN TRANSACTION;

-- ============================================================================
-- 1. CREATE APP USER (Brian E Roach)
-- ============================================================================
-- Bcrypt hash of "TempPass123!" with cost factor 10
-- Generated: $2b$10$Nz2Q3UzKkWK7UXrLxLq6p.9N.uKqXlYaV6pJ7kV6Zq0iGvLvfJZfm
-- (This is a valid test hash - in production, admin UI will hash passwords)

INSERT INTO public.app_user (
    app_user_id,
    email,
    password_hash,
    first_name,
    last_name,
    display_name,
    avatar_url,
    is_active,
    must_change_password,
    created_on_utc,
    updated_on_utc
) VALUES (
    '550e8400-e29b-41d4-a716-446655440001'::UUID,
    'brian@pricklycactussoftware.com',
    '$2b$10$Nz2Q3UzKkWK7UXrLxLq6p.9N.uKqXlYaV6pJ7kV6Zq0iGvLvfJZfm',
    'Brian',
    'Roach',
    'Brian E Roach',
    NULL,
    true,
    true,  -- Force password change on first login
    NOW(),
    NOW()
) ON CONFLICT (email) DO NOTHING;

-- ============================================================================
-- 2. CREATE PRACTICE (Prickly Cactus Therapy)
-- ============================================================================

INSERT INTO public.practice (
    practice_id,
    name,
    time_zone,
    default_currency,
    default_session_length_minutes,
    address_line_1,
    address_line_2,
    city,
    state_province,
    postal_code,
    country,
    phone,
    is_active,
    created_on_utc,
    updated_on_utc
) VALUES (
    '660f9500-f39c-52e5-b827-557766551112'::UUID,
    'Prickly Cactus Therapy',
    'America/Phoenix',
    'USD',
    60,
    '123 Therapy Lane',
    'Suite 200',
    'Phoenix',
    'AZ',
    '85001',
    'USA',
    '(602) 555-0123',
    true,
    NOW(),
    NOW()
) ON CONFLICT DO NOTHING;

-- ============================================================================
-- 3. CREATE PRACTICE_USER (Brian as Owner of Prickly Cactus)
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
    '770e0601-e40d-63f6-c938-668877662223'::UUID,
    '660f9500-f39c-52e5-b827-557766551112'::UUID,
    '550e8400-e29b-41d4-a716-446655440001'::UUID,
    'Owner'::practice_role,
    true,
    NOW(),
    NOW(),  -- Already accepted (admin created)
    NOW(),
    NOW(),
    '550e8400-e29b-41d4-a716-446655440001'::UUID  -- Self-created
) ON CONFLICT DO NOTHING;

-- ============================================================================
-- 4. CREATE THERAPIST (Brian as therapist profile)
-- ============================================================================

INSERT INTO public.therapist (
    therapist_id,
    practice_id,
    practice_user_id,
    first_name,
    last_name,
    license_number,
    license_state,
    npi,
    credential,
    specialty,
    bio,
    signature_block,
    is_active,
    created_on_utc,
    updated_on_utc
) VALUES (
    '880e1712-e51e-74e7-d049-779988773334'::UUID,
    '660f9500-f39c-52e5-b827-557766551112'::UUID,
    '770e0601-e40d-63f6-c938-668877662223'::UUID,
    'Brian',
    'Roach',
    'LIC123456',
    'AZ',
    '1234567890',
    'LCSW',
    'Cognitive Behavioral Therapy',
    'Brian Roach is a licensed clinical social worker with 10+ years of experience in cognitive behavioral therapy.',
    'Brian E Roach, LCSW\nPrickly Cactus Therapy\n123 Therapy Lane, Suite 200\nPhoenix, AZ 85001',
    true,
    NOW(),
    NOW()
) ON CONFLICT DO NOTHING;

COMMIT;

-- ============================================================================
-- VERIFICATION QUERIES
-- ============================================================================
-- Uncomment to verify data was inserted correctly

/*
-- Verify app_user was created
SELECT app_user_id, email, first_name, last_name, must_change_password, is_active
FROM public.app_user
WHERE email = 'brian@pricklycactussoftware.com';

-- Verify practice was created
SELECT practice_id, name, city, state_province
FROM public.practice
WHERE name = 'Prickly Cactus Therapy';

-- Verify practice_user membership
SELECT pu.practice_user_id, pu.role, p.name, au.email
FROM public.practice_user pu
JOIN public.practice p ON pu.practice_id = p.practice_id
JOIN public.app_user au ON pu.app_user_id = au.app_user_id
WHERE au.email = 'brian@pricklycactussoftware.com';

-- Verify therapist profile
SELECT t.therapist_id, t.first_name, t.last_name, t.license_number, t.npi, p.name
FROM public.therapist t
JOIN public.practice p ON t.practice_id = p.practice_id
WHERE t.first_name = 'Brian' AND t.last_name = 'Roach';

-- Get user's accessible practices (useful for login flow)
SELECT * FROM get_user_practices('550e8400-e29b-41d4-a716-446655440001'::UUID);
*/

-- ============================================================================
-- LOGIN TEST
-- ============================================================================
-- To test login:
-- 1. User enters email: brian@pricklycactussoftware.com
-- 2. User enters password: TempPass123!
-- 3. App queries: SELECT * FROM app_user WHERE email = 'brian@pricklycactus.com'
-- 4. App validates password_hash (bcrypt comparison)
-- 5. App detects must_change_password = true
-- 6. App forces user to change password
-- 7. After password change, app sets must_change_password = false
-- 8. User can now access all their practices
--
-- User Brian has access to:
--   - Practice: Prickly Cactus Therapy (Role: Owner)
--   - Clinical Profile: Therapist (License: LIC123456, NPI: 1234567890)
-- ============================================================================
