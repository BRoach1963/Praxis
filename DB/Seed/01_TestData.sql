-- ============================================================================
-- PRAXIS DATABASE: Test Seed Data
-- ============================================================================
-- Creates test data for local development:
--   - 1 Firm (Prickly Cactus Counseling)
--   - 1 UserProfile (brian@pricklycactus.com)
--   - 1 FirmUser (Owner role)
--   - 1 Therapist record
--   - 1 KeyRing entry (for encryption testing)
--   - 1 Client
--   - 1 TherapistClient assignment
--   - 1 CaseFile
--   - 1 Session
-- ============================================================================

-- UUIDs for referential integrity
DO $$
DECLARE
    v_firm_id           uuid := 'a0000000-0000-0000-0000-000000000001'::uuid;
    v_user_profile_id   uuid := 'b0000000-0000-0000-0000-000000000001'::uuid;
    v_firm_user_id      uuid := 'c0000000-0000-0000-0000-000000000001'::uuid;
    v_therapist_id      uuid := 'd0000000-0000-0000-0000-000000000001'::uuid;
    v_key_id            uuid := 'e0000000-0000-0000-0000-000000000001'::uuid;
    v_client_id         uuid := 'f0000000-0000-0000-0000-000000000001'::uuid;
    v_therapist_client_id uuid := 'f1000000-0000-0000-0000-000000000001'::uuid;
    v_case_file_id      uuid := 'f2000000-0000-0000-0000-000000000001'::uuid;
    v_session_id        uuid := 'f3000000-0000-0000-0000-000000000001'::uuid;
    v_now               timestamptz := now();
    -- BCrypt hash of 'TempPass123!' (generated with cost factor 11)
    v_password_hash     text := '$2a$11$tDBaDGbzFh093BJ0Zt2eluFf1zQ4cegSi3VfPpQE3rZ7wpakEtsD2';
BEGIN
    -- ========================================================================
    -- FIRM
    -- ========================================================================
    INSERT INTO firm (firm_id, name, time_zone_iana, status, created_utc, updated_utc)
    VALUES (v_firm_id, 'Prickly Cactus Counseling', 'America/Chicago', 'Active', v_now, v_now)
    ON CONFLICT (firm_id) DO NOTHING;

    -- ========================================================================
    -- USER PROFILE (Authentication)
    -- ========================================================================
    INSERT INTO user_profile (user_profile_id, auth_user_id, email, password_hash, display_name, created_utc, updated_utc)
    VALUES (v_user_profile_id, v_user_profile_id, 'brian@pricklycactus.com', v_password_hash, 'Brian Roach', v_now, v_now)
    ON CONFLICT (user_profile_id) DO NOTHING;

    -- ========================================================================
    -- FIRM USER (Membership)
    -- ========================================================================
    INSERT INTO firm_user (firm_user_id, firm_id, user_profile_id, role, created_utc, updated_utc)
    VALUES (v_firm_user_id, v_firm_id, v_user_profile_id, 'Owner', v_now, v_now)
    ON CONFLICT (firm_user_id) DO NOTHING;

    -- ========================================================================
    -- THERAPIST (Clinical Persona)
    -- ========================================================================
    INSERT INTO therapist (therapist_id, firm_id, firm_user_id, first_name, last_name, 
                          license_type, license_number, license_state, created_utc, updated_utc)
    VALUES (v_therapist_id, v_firm_id, v_firm_user_id, 'Brian', 'Roach', 
            'LCSW', 'LCSW-12345', 'TX', v_now, v_now)
    ON CONFLICT (therapist_id) DO NOTHING;

    -- ========================================================================
    -- KEY RING (Encryption Key Metadata)
    -- ========================================================================
    INSERT INTO key_ring (key_id, firm_id, key_name, algorithm, key_version, status, 
                         created_utc, activated_utc)
    VALUES (v_key_id, v_firm_id, 'PCC-Key-2025-01', 'AES-256-GCM', 1, 'Active', 
            v_now, v_now)
    ON CONFLICT (key_id) DO NOTHING;

    -- ========================================================================
    -- CLIENT
    -- ========================================================================
    INSERT INTO client (client_id, firm_id, first_name, last_name, preferred_name,
                       date_of_birth, email, phone, intake_date, created_utc, updated_utc)
    VALUES (v_client_id, v_firm_id, 'Test', 'Client', 'Testy',
            '1990-05-15', 'test.client@example.com', '555-123-4567', CURRENT_DATE, v_now, v_now)
    ON CONFLICT (client_id) DO NOTHING;

    -- ========================================================================
    -- THERAPIST-CLIENT ASSIGNMENT
    -- ========================================================================
    INSERT INTO therapist_client (therapist_client_id, therapist_id, client_id, 
                                 assignment_type, assigned_date, is_active, created_utc, updated_utc)
    VALUES (v_therapist_client_id, v_therapist_id, v_client_id,
            'Primary', CURRENT_DATE, true, v_now, v_now)
    ON CONFLICT (therapist_client_id) DO NOTHING;

    -- ========================================================================
    -- CASE FILE
    -- ========================================================================
    INSERT INTO case_file (case_file_id, client_id, therapist_id, case_number,
                          opened_date, status, presenting_problem, diagnosis_primary,
                          treatment_modality, session_frequency, created_utc, updated_utc)
    VALUES (v_case_file_id, v_client_id, v_therapist_id, 'PCC-2025-001',
            CURRENT_DATE, 'Open', 'Anxiety and work-related stress', 'F41.1',
            'Individual', 'Weekly', v_now, v_now)
    ON CONFLICT (case_file_id) DO NOTHING;

    -- ========================================================================
    -- SESSION
    -- ========================================================================
    INSERT INTO session (session_id, case_file_id, therapist_id, session_date,
                        start_time, duration_minutes, session_type, session_format,
                        status, billing_code, created_utc, updated_utc)
    VALUES (v_session_id, v_case_file_id, v_therapist_id, CURRENT_DATE,
            '10:00:00', 50, 'Individual', 'InPerson',
            'Completed', '90834', v_now, v_now)
    ON CONFLICT (session_id) DO NOTHING;

    RAISE NOTICE 'Test seed data created successfully.';
END $$;

-- Verify data
SELECT 'Firms:' AS entity, COUNT(*) AS count FROM firm
UNION ALL
SELECT 'User Profiles:', COUNT(*) FROM user_profile
UNION ALL
SELECT 'Firm Users:', COUNT(*) FROM firm_user
UNION ALL
SELECT 'Therapists:', COUNT(*) FROM therapist
UNION ALL
SELECT 'Key Ring:', COUNT(*) FROM key_ring
UNION ALL
SELECT 'Clients:', COUNT(*) FROM client
UNION ALL
SELECT 'Therapist-Clients:', COUNT(*) FROM therapist_client
UNION ALL
SELECT 'Case Files:', COUNT(*) FROM case_file
UNION ALL
SELECT 'Sessions:', COUNT(*) FROM session;
