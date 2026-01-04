-- ============================================================================
-- PRAXIS DATABASE: Run All Schema Scripts
-- ============================================================================
-- Execute this file to create all tables in the correct order.
-- Run from psql: \i DB/Schema/run_all.sql
-- Or execute each file individually in order.
-- ============================================================================

-- Extensions
\i 00_Extensions.sql

-- Core identity tables
\i 01_Firm.sql
\i 02_UserProfile.sql
\i 03_FirmUser.sql

-- Clinical structure
\i 04_Therapist.sql
\i 05_Client.sql
\i 06_TherapistClient.sql
\i 07_CaseFile.sql
\i 08_Session.sql

-- Encryption and notes
\i 09_KeyRing.sql
\i 10_ClinicalNote.sql

-- Confirm completion
SELECT 'Schema creation complete' AS status;
