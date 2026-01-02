-- ============================================================================
-- PRAXIS SUPABASE SCHEMA
-- ============================================================================
-- CRITICAL MODELING INSIGHT: Login ≠ Clinical Role
-- ============================================================================
--
-- SEPARATION OF CONCERNS:
--   UserProfile (with AuthUserId)
--     └─ Authentication identity linked to Supabase auth.users
--     └─ Can access multiple practices via PracticeUser memberships
--
--   PracticeUser (membership + role)
--     └─ Login authorization (who can access this practice and with what role)
--     └─ Roles: Owner, Admin, Therapist, Biller, Staff, ReadOnly
--     └─ A billing admin logs in via PracticeUser but is NOT a Therapist entity
--
--   Therapist (clinical persona)
--     └─ Clinical practitioner profile with license, NPI, credentials
--     └─ OPTIONAL link to PracticeUser (some therapists may not log in)
--     └─ Can supervise other therapists
--     └─ Therapist ≠ PracticeRole.Therapist
--
-- WHY THIS MATTERS:
--   ✓ A billing admin can log in (PracticeUser with Biller role) without being a Therapist
--   ✓ A therapist can exist in the system for scheduling/assignments without login access
--   ✓ A therapist supervisor can log in and oversee other therapists
--   ✓ Clinical logic is not baked into auth assumptions
--   ✓ Mirrors Pro Causa's "Attorney vs Login" split
--
-- Canonical Relationship Model (validated against C# models):
-- 
-- Practice (Firm)
--  ├─ UserProfile (login identity - can access multiple practices)
--  │   └─ PracticeUser (membership + organizational role per practice)
--  │
--  ├─ Therapist (clinical persona, optional link to PracticeUser for login)
--  │
--  ├─ Client (individual in care)
--  │   ├─ ClientAssignment (links Client to Therapist(s))
--  │   └─ CaseFile (episode of care)
--  │       ├─ Session (appointment, assigned to Therapist not PracticeUser)
--  │       │   └─ ClinicalNote (clinical documentation)
--  │       ├─ TreatmentPlan (course of treatment)
--  │       │   └─ TreatmentGoal
--  │       │       └─ TreatmentIntervention
--  │       └─ Assessment
--  │
--  ├─ ServiceCode (billable service codes)
--  ├─ Invoice (charges)
--  │   └─ InvoiceLine (line items - can be linked to Session)
--  │       └─ Payment
--  │
--  ├─ Tag (flexible labeling)
--  │   └─ EntityTag (many-to-many tagging)
--  │
--  ├─ PraxisTask (operational tasks)
--  │   └─ AvailabilityRule (therapist availability/schedule)
--  │
--  └─ AuditLog (change tracking)
--
-- ============================================================================

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Enable CITEXT for case-insensitive text comparison
CREATE EXTENSION IF NOT EXISTS citext;

-- ============================================================================
-- APP USER MANAGEMENT
-- ============================================================================
-- AppUser: Application-managed user created by the app.
-- Each user is created within the context of a Practice (governing entity).
-- Users can be invited to additional practices via PracticeUser.
-- Password/auth is managed by the application layer, not Supabase auth.
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.app_user (
    app_user_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email CITEXT NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,  -- Bcrypt or similar, application manages hashing
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    display_name VARCHAR(255),
    avatar_url VARCHAR(500),
    is_active BOOLEAN NOT NULL DEFAULT true,
    must_change_password BOOLEAN NOT NULL DEFAULT false,  -- Set true when admin creates user with temp password
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_login_utc TIMESTAMPTZ
);

CREATE INDEX idx_app_user_email ON public.app_user(email);
CREATE INDEX idx_app_user_is_active ON public.app_user(is_active);
CREATE INDEX idx_app_user_must_change_password ON public.app_user(must_change_password);

-- ============================================================================
-- PASSWORD RESET TOKENS
-- ============================================================================
-- Used for password resets and initial password setup from temp password.
-- Token expires after 24 hours or after first use (whichever comes first).
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.password_reset_token (
    token_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    app_user_id UUID NOT NULL REFERENCES public.app_user(app_user_id) ON DELETE CASCADE,
    token VARCHAR(255) NOT NULL UNIQUE,  -- Random token (hashed/salted version)
    expires_on_utc TIMESTAMPTZ NOT NULL,  -- Default 24 hours from creation
    used_on_utc TIMESTAMPTZ,  -- NULL until token is used
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_password_reset_token_app_user_id ON public.password_reset_token(app_user_id);
CREATE INDEX idx_password_reset_token_token ON public.password_reset_token(token);
CREATE INDEX idx_password_reset_token_expires_on_utc ON public.password_reset_token(expires_on_utc);

-- ============================================================================
-- TENANT / FIRM
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.practice (
    practice_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    time_zone VARCHAR(50) DEFAULT 'America/Chicago',
    default_currency VARCHAR(3) DEFAULT 'USD',
    default_session_length_minutes INT DEFAULT 60,
    address_line_1 VARCHAR(255),
    address_line_2 VARCHAR(255),
    city VARCHAR(100),
    state_province VARCHAR(50),
    postal_code VARCHAR(20),
    country VARCHAR(100),
    phone VARCHAR(20),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_on_utc TIMESTAMPTZ,
    deleted_by_app_user_id UUID REFERENCES public.app_user(app_user_id)
);

CREATE INDEX idx_practice_is_active ON public.practice(is_active);
CREATE INDEX idx_practice_is_deleted ON public.practice(is_deleted);

-- ============================================================================
-- PRACTICE MEMBERSHIP & ROLES
-- ============================================================================
-- PracticeUser represents AUTHORIZATION: who can log in to this practice and with what role.
-- Important: PracticeRole.Therapist is an ORGANIZATIONAL ROLE, NOT a clinical designation.
-- The clinical practitioner profile is the separate Therapist entity.
-- Example: A billing admin has PracticeRole.Biller but is not a Therapist entity.
-- ============================================================================

CREATE TYPE practice_role AS ENUM ('Owner', 'Admin', 'Therapist', 'Biller', 'Staff', 'ReadOnly');

CREATE TABLE IF NOT EXISTS public.practice_user (
    practice_user_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    practice_id UUID NOT NULL REFERENCES public.practice(practice_id) ON DELETE CASCADE,
    app_user_id UUID NOT NULL REFERENCES public.app_user(app_user_id) ON DELETE CASCADE,
    role practice_role NOT NULL,  -- Authorization role: Owner, Admin, Therapist (org), Biller, Staff, ReadOnly
    is_active BOOLEAN NOT NULL DEFAULT true,
    invited_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    accepted_on_utc TIMESTAMPTZ,  -- NULL until user accepts invitation
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by_app_user_id UUID REFERENCES public.app_user(app_user_id)
);

CREATE INDEX idx_practice_user_practice_id ON public.practice_user(practice_id);
CREATE INDEX idx_practice_user_app_user_id ON public.practice_user(app_user_id);
CREATE INDEX idx_practice_user_role ON public.practice_user(role);
CREATE UNIQUE INDEX idx_practice_user_unique_member 
    ON public.practice_user(practice_id, app_user_id) 
    WHERE is_active = true;

-- ============================================================================
-- THERAPIST (Clinical Practitioner Profile) - SEPARATE FROM LOGIN
-- ============================================================================
-- Therapist is a CLINICAL PERSONA, not a login role.
-- A Therapist entity MAY have an optional link to PracticeUser for system access.
-- A Therapist MAY NOT have a PracticeUser (e.g., contractors not in the system).
-- A user with PracticeRole.Therapist MUST have a linked Therapist entity if they deliver clinical services.
-- A user with PracticeRole.Biller or PracticeRole.Admin is NOT a Therapist entity.
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.therapist (
    therapist_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    practice_id UUID NOT NULL REFERENCES public.practice(practice_id) ON DELETE CASCADE,
    practice_user_id UUID REFERENCES public.practice_user(practice_user_id) ON DELETE SET NULL,  -- Optional: may not log in
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    license_number VARCHAR(50),
    license_state VARCHAR(2),
    npi VARCHAR(20),
    credential VARCHAR(100),
    specialty VARCHAR(255),
    bio TEXT,
    signature_block TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_on_utc TIMESTAMPTZ
);

CREATE INDEX idx_therapist_practice_id ON public.therapist(practice_id);
CREATE INDEX idx_therapist_practice_user_id ON public.therapist(practice_user_id);
CREATE INDEX idx_therapist_npi ON public.therapist(npi);
CREATE INDEX idx_therapist_is_active ON public.therapist(is_active);

-- ============================================================================
-- CLIENT (Individual in Care)
-- ============================================================================

CREATE TYPE client_status AS ENUM ('Active', 'Inactive', 'Archived');

CREATE TABLE IF NOT EXISTS public.client (
    client_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    practice_id UUID NOT NULL REFERENCES public.practice(practice_id) ON DELETE CASCADE,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    preferred_name VARCHAR(100),
    pronouns VARCHAR(50),
    date_of_birth DATE,
    gender VARCHAR(50),
    email VARCHAR(255),
    phone VARCHAR(20),
    emergency_contact_name VARCHAR(255),
    emergency_contact_phone VARCHAR(20),
    status client_status NOT NULL DEFAULT 'Active',
    intake_date DATE,
    termination_date DATE,
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_on_utc TIMESTAMPTZ
);

CREATE INDEX idx_client_practice_id ON public.client(practice_id);
CREATE INDEX idx_client_status ON public.client(status);
CREATE INDEX idx_client_email ON public.client(email);
CREATE INDEX idx_client_is_deleted ON public.client(is_deleted);

-- ============================================================================
-- CLIENT ASSIGNMENT (Links Client to Therapist(s))
-- ============================================================================

CREATE TYPE client_assignment_role AS ENUM ('Primary', 'Secondary', 'Supervisor');

CREATE TABLE IF NOT EXISTS public.client_assignment (
    client_assignment_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES public.client(client_id) ON DELETE CASCADE,
    therapist_id UUID NOT NULL REFERENCES public.therapist(therapist_id) ON DELETE CASCADE,
    role client_assignment_role NOT NULL,
    start_date DATE NOT NULL DEFAULT CURRENT_DATE,
    end_date DATE,
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_client_assignment_client_id ON public.client_assignment(client_id);
CREATE INDEX idx_client_assignment_therapist_id ON public.client_assignment(therapist_id);
CREATE INDEX idx_client_assignment_role ON public.client_assignment(role);

-- ============================================================================
-- CASE FILE (Episode of Care)
-- ============================================================================

CREATE TYPE case_file_status AS ENUM ('Active', 'Paused', 'Closed');

CREATE TABLE IF NOT EXISTS public.case_file (
    case_file_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES public.client(client_id) ON DELETE CASCADE,
    primary_therapist_id UUID NOT NULL REFERENCES public.therapist(therapist_id),
    start_date DATE NOT NULL DEFAULT CURRENT_DATE,
    end_date DATE,
    presenting_problems TEXT,
    status case_file_status NOT NULL DEFAULT 'Active',
    version INT NOT NULL DEFAULT 1,
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by_app_user_id UUID NOT NULL REFERENCES public.app_user(app_user_id)
);

CREATE INDEX idx_case_file_client_id ON public.case_file(client_id);
CREATE INDEX idx_case_file_primary_therapist_id ON public.case_file(primary_therapist_id);
CREATE INDEX idx_case_file_status ON public.case_file(status);

-- ============================================================================
-- SESSION (Appointment)
-- ============================================================================
-- Sessions are assigned to THERAPIST (clinical), not to PracticeUser (login).
-- This separates clinical delivery from administrative access.
-- ============================================================================

CREATE TYPE session_location_type AS ENUM ('InPerson', 'Telehealth', 'Phone');
CREATE TYPE session_status AS ENUM ('Scheduled', 'InProgress', 'Completed', 'NoShow', 'Cancelled');

CREATE TABLE IF NOT EXISTS public.session (
    session_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    case_file_id UUID NOT NULL REFERENCES public.case_file(case_file_id) ON DELETE CASCADE,
    therapist_id UUID NOT NULL REFERENCES public.therapist(therapist_id),  -- Clinical assignment, not administrative
    start_utc TIMESTAMPTZ NOT NULL,
    end_utc TIMESTAMPTZ NOT NULL,
    location_type session_location_type NOT NULL,
    telehealth_join_link VARCHAR(500),
    status session_status NOT NULL DEFAULT 'Scheduled',
    attendees JSONB,  -- JSON array of attendee info
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by_app_user_id UUID NOT NULL REFERENCES public.app_user(app_user_id),  -- The user who created/scheduled the session
    updated_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_session_case_file_id ON public.session(case_file_id);
CREATE INDEX idx_session_therapist_id ON public.session(therapist_id);
CREATE INDEX idx_session_start_utc ON public.session(start_utc);
CREATE INDEX idx_session_status ON public.session(status);

-- ============================================================================
-- CLINICAL NOTE (Clinical Documentation - IMMUTABLE)
-- ============================================================================

CREATE TYPE clinical_note_type AS ENUM ('DAP', 'SOAP', 'BIRP', 'Progress', 'Intake', 'Termination');

CREATE TABLE IF NOT EXISTS public.clinical_note (
    clinical_note_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    session_id UUID NOT NULL UNIQUE REFERENCES public.session(session_id) ON DELETE CASCADE,
    note_type clinical_note_type NOT NULL,
    content TEXT NOT NULL,  -- Encrypted at rest (application handles encryption/decryption)
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by_app_user_id UUID NOT NULL REFERENCES public.app_user(app_user_id),
    updated_on_utc TIMESTAMPTZ,
    updated_by_app_user_id UUID,
    locked_on_utc TIMESTAMPTZ,  -- When locked, note becomes immutable
    locked_by_app_user_id UUID,
    CHECK (
        -- If locked, don't allow updates
        locked_on_utc IS NULL OR 
        (locked_on_utc IS NOT NULL AND updated_on_utc IS NULL)
    )
);

CREATE INDEX idx_clinical_note_session_id ON public.clinical_note(session_id);
CREATE INDEX idx_clinical_note_created_on ON public.clinical_note(created_on_utc);
CREATE INDEX idx_clinical_note_locked ON public.clinical_note(locked_on_utc);

-- ============================================================================
-- TREATMENT PLAN
-- ============================================================================

CREATE TYPE treatment_plan_status AS ENUM ('Draft', 'Active', 'Archived');

CREATE TABLE IF NOT EXISTS public.treatment_plan (
    treatment_plan_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    case_file_id UUID NOT NULL REFERENCES public.case_file(case_file_id) ON DELETE CASCADE,
    plan_version INT NOT NULL DEFAULT 1,
    summary TEXT NOT NULL,
    status treatment_plan_status NOT NULL DEFAULT 'Draft',
    effective_date DATE NOT NULL DEFAULT CURRENT_DATE,
    review_date DATE,
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by_app_user_id UUID NOT NULL REFERENCES public.app_user(app_user_id)
);

CREATE INDEX idx_treatment_plan_case_file_id ON public.treatment_plan(case_file_id);
CREATE INDEX idx_treatment_plan_status ON public.treatment_plan(status);

-- ============================================================================
-- TREATMENT GOAL
-- ============================================================================

CREATE TYPE treatment_goal_status AS ENUM ('Active', 'Achieved', 'Modified', 'Discontinued');

CREATE TABLE IF NOT EXISTS public.treatment_goal (
    treatment_goal_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    treatment_plan_id UUID NOT NULL REFERENCES public.treatment_plan(treatment_plan_id) ON DELETE CASCADE,
    goal_text TEXT NOT NULL,
    target_date DATE NOT NULL,
    status treatment_goal_status NOT NULL DEFAULT 'Active',
    measurement_method TEXT,
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by_app_user_id UUID NOT NULL REFERENCES public.app_user(app_user_id)
);

CREATE INDEX idx_treatment_goal_treatment_plan_id ON public.treatment_goal(treatment_plan_id);
CREATE INDEX idx_treatment_goal_status ON public.treatment_goal(status);

-- ============================================================================
-- TREATMENT INTERVENTION
-- ============================================================================

CREATE TYPE treatment_intervention_status AS ENUM ('Active', 'Completed', 'Discontinued');

CREATE TABLE IF NOT EXISTS public.treatment_intervention (
    treatment_intervention_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    treatment_goal_id UUID NOT NULL REFERENCES public.treatment_goal(treatment_goal_id) ON DELETE CASCADE,
    intervention_text TEXT NOT NULL,
    frequency VARCHAR(100),
    status treatment_intervention_status NOT NULL DEFAULT 'Active',
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_treatment_intervention_treatment_goal_id ON public.treatment_intervention(treatment_goal_id);
CREATE INDEX idx_treatment_intervention_status ON public.treatment_intervention(status);

-- ============================================================================
-- ASSESSMENT (Clinical Assessment)
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.assessment (
    assessment_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    case_file_id UUID NOT NULL REFERENCES public.case_file(case_file_id) ON DELETE CASCADE,
    assessment_type VARCHAR(100) NOT NULL,
    assessment_date DATE NOT NULL,
    findings TEXT,
    recommendations TEXT,
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by_app_user_id UUID NOT NULL REFERENCES public.app_user(app_user_id)
);

CREATE INDEX idx_assessment_case_file_id ON public.assessment(case_file_id);
CREATE INDEX idx_assessment_assessment_date ON public.assessment(assessment_date);

-- ============================================================================
-- BILLING: SERVICE CODES
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.service_code (
    service_code_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    practice_id UUID NOT NULL REFERENCES public.practice(practice_id) ON DELETE CASCADE,
    code VARCHAR(20) NOT NULL,  -- e.g., "90834"
    description VARCHAR(255) NOT NULL,
    default_duration_minutes INT NOT NULL DEFAULT 60,
    default_rate_usd NUMERIC(10, 2) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_service_code_practice_id ON public.service_code(practice_id);
CREATE UNIQUE INDEX idx_service_code_practice_code 
    ON public.service_code(practice_id, code);

-- ============================================================================
-- BILLING: INVOICE
-- ============================================================================

CREATE TYPE invoice_status AS ENUM ('Draft', 'Sent', 'Viewed', 'PartiallyPaid', 'Paid', 'Overdue', 'Cancelled');

CREATE TABLE IF NOT EXISTS public.invoice (
    invoice_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    practice_id UUID NOT NULL REFERENCES public.practice(practice_id) ON DELETE CASCADE,
    client_id UUID NOT NULL REFERENCES public.client(client_id) ON DELETE CASCADE,
    invoice_number VARCHAR(50) NOT NULL UNIQUE,
    issue_date DATE NOT NULL DEFAULT CURRENT_DATE,
    due_date DATE NOT NULL,
    total_amount NUMERIC(10, 2) NOT NULL,
    paid_amount NUMERIC(10, 2) NOT NULL DEFAULT 0.00,
    status invoice_status NOT NULL DEFAULT 'Draft',
    notes TEXT,
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by_app_user_id UUID NOT NULL REFERENCES public.app_user(app_user_id),
    updated_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_invoice_practice_id ON public.invoice(practice_id);
CREATE INDEX idx_invoice_client_id ON public.invoice(client_id);
CREATE INDEX idx_invoice_invoice_number ON public.invoice(invoice_number);
CREATE INDEX idx_invoice_status ON public.invoice(status);
CREATE INDEX idx_invoice_due_date ON public.invoice(due_date);

-- ============================================================================
-- BILLING: INVOICE LINE ITEM
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.invoice_line (
    invoice_line_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id UUID NOT NULL REFERENCES public.invoice(invoice_id) ON DELETE CASCADE,
    session_id UUID REFERENCES public.session(session_id) ON DELETE SET NULL,
    service_code_id UUID REFERENCES public.service_code(service_code_id),
    description VARCHAR(500) NOT NULL,
    quantity NUMERIC(10, 2) NOT NULL DEFAULT 1,
    unit_rate NUMERIC(10, 2) NOT NULL,
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_invoice_line_invoice_id ON public.invoice_line(invoice_id);
CREATE INDEX idx_invoice_line_session_id ON public.invoice_line(session_id);
CREATE INDEX idx_invoice_line_service_code_id ON public.invoice_line(service_code_id);

-- ============================================================================
-- BILLING: PAYMENT
-- ============================================================================

CREATE TYPE payment_method AS ENUM ('CreditCard', 'ACH', 'Check', 'Cash', 'Other');
CREATE TYPE payment_status AS ENUM ('Pending', 'Completed', 'Failed', 'Refunded');

CREATE TABLE IF NOT EXISTS public.payment (
    payment_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id UUID NOT NULL REFERENCES public.invoice(invoice_id) ON DELETE CASCADE,
    amount NUMERIC(10, 2) NOT NULL,
    payment_date DATE NOT NULL DEFAULT CURRENT_DATE,
    payment_method payment_method NOT NULL,
    transaction_ref VARCHAR(100),
    status payment_status NOT NULL DEFAULT 'Pending',
    notes TEXT,
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_payment_invoice_id ON public.payment(invoice_id);
CREATE INDEX idx_payment_payment_date ON public.payment(payment_date);
CREATE INDEX idx_payment_status ON public.payment(status);

-- ============================================================================
-- TAGGING (Flexible Labeling)
-- ============================================================================

CREATE TABLE IF NOT EXISTS public.tag (
    tag_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    practice_id UUID NOT NULL REFERENCES public.practice(practice_id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    category VARCHAR(100),
    color VARCHAR(7),  -- Hex color
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_tag_practice_id ON public.tag(practice_id);
CREATE UNIQUE INDEX idx_tag_practice_name 
    ON public.tag(practice_id, name);

-- ============================================================================
-- ENTITY TAG (Many-to-Many Tagging)
-- ============================================================================

CREATE TYPE entity_tag_type AS ENUM ('Client', 'CaseFile', 'Session', 'ClinicalNote');

CREATE TABLE IF NOT EXISTS public.entity_tag (
    entity_tag_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tag_id UUID NOT NULL REFERENCES public.tag(tag_id) ON DELETE CASCADE,
    entity_type entity_tag_type NOT NULL,
    entity_id UUID NOT NULL,
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_entity_tag_tag_id ON public.entity_tag(tag_id);
CREATE INDEX idx_entity_tag_entity ON public.entity_tag(entity_type, entity_id);
CREATE UNIQUE INDEX idx_entity_tag_unique 
    ON public.entity_tag(tag_id, entity_type, entity_id);

-- ============================================================================
-- TASKS (Operational Tasks)
-- ============================================================================

CREATE TYPE task_status AS ENUM ('Open', 'InProgress', 'Completed', 'Cancelled');
CREATE TYPE task_priority AS ENUM ('Low', 'Normal', 'High', 'Critical');

CREATE TABLE IF NOT EXISTS public.praxis_task (
    task_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    practice_id UUID NOT NULL REFERENCES public.practice(practice_id) ON DELETE CASCADE,
    client_id UUID REFERENCES public.client(client_id) ON DELETE SET NULL,
    case_file_id UUID REFERENCES public.case_file(case_file_id) ON DELETE SET NULL,
    assigned_to_app_user_id UUID REFERENCES public.app_user(app_user_id),
    title VARCHAR(255) NOT NULL,
    description TEXT,
    status task_status NOT NULL DEFAULT 'Open',
    priority task_priority NOT NULL DEFAULT 'Normal',
    due_date DATE,
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by_app_user_id UUID NOT NULL REFERENCES public.app_user(app_user_id),
    completed_on_utc TIMESTAMPTZ
);

CREATE INDEX idx_praxis_task_practice_id ON public.praxis_task(practice_id);
CREATE INDEX idx_praxis_task_assigned_to ON public.praxis_task(assigned_to_app_user_id);
CREATE INDEX idx_praxis_task_status ON public.praxis_task(status);
CREATE INDEX idx_praxis_task_due_date ON public.praxis_task(due_date);

-- ============================================================================
-- THERAPIST AVAILABILITY
-- ============================================================================

CREATE TYPE availability_day_of_week AS ENUM ('Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday');

CREATE TABLE IF NOT EXISTS public.availability_rule (
    availability_rule_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    therapist_id UUID NOT NULL REFERENCES public.therapist(therapist_id) ON DELETE CASCADE,
    day_of_week availability_day_of_week NOT NULL,
    start_time_utc TIME NOT NULL,
    end_time_utc TIME NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_availability_rule_therapist_id ON public.availability_rule(therapist_id);
CREATE INDEX idx_availability_rule_day_of_week ON public.availability_rule(day_of_week);

-- ============================================================================
-- AUDIT LOG (Change Tracking)
-- ============================================================================

CREATE TYPE audit_action AS ENUM ('Create', 'Update', 'Delete', 'Lock');

CREATE TABLE IF NOT EXISTS public.audit_log (
    audit_log_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    practice_id UUID REFERENCES public.practice(practice_id) ON DELETE SET NULL,
    app_user_id UUID REFERENCES public.app_user(app_user_id),
    entity_type VARCHAR(100) NOT NULL,
    entity_id UUID NOT NULL,
    action audit_action NOT NULL,
    changes JSONB,  -- Old and new values
    created_on_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_audit_log_practice_id ON public.audit_log(practice_id);
CREATE INDEX idx_audit_log_entity ON public.audit_log(entity_type, entity_id);
CREATE INDEX idx_audit_log_created_on ON public.audit_log(created_on_utc);
CREATE INDEX idx_audit_log_app_user_id ON public.audit_log(app_user_id);

-- ============================================================================
-- ROW LEVEL SECURITY (RLS) POLICIES
-- ============================================================================

-- Enable RLS on all tables
ALTER TABLE public.app_user ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.practice ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.practice_user ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.therapist ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.client ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.client_assignment ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.case_file ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.session ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.clinical_note ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.treatment_plan ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.treatment_goal ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.treatment_intervention ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.assessment ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.service_code ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.invoice ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.invoice_line ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.payment ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.tag ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.entity_tag ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.praxis_task ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.availability_rule ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.audit_log ENABLE ROW LEVEL SECURITY;

-- RLS Note: Application will use authenticated user context (app_user_id) at the application layer
-- rather than relying on Supabase auth.uid(). Enforce practice isolation in application logic.

-- ============================================================================
-- HELPER FUNCTIONS
-- ============================================================================

-- Function to get user's practice access
CREATE OR REPLACE FUNCTION get_user_practices(p_app_user_id UUID)
RETURNS TABLE(practice_id UUID, role practice_role) AS $$
BEGIN
    RETURN QUERY
    SELECT pu.practice_id, pu.role
    FROM public.practice_user pu
    WHERE pu.app_user_id = p_app_user_id
    AND pu.is_active = true;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to check user role in practice
CREATE OR REPLACE FUNCTION user_has_practice_role(p_app_user_id UUID, p_practice_id UUID, p_required_role practice_role)
RETURNS BOOLEAN AS $$
DECLARE
    user_role practice_role;
BEGIN
    SELECT pu.role INTO user_role
    FROM public.practice_user pu
    WHERE pu.app_user_id = p_app_user_id
    AND pu.practice_id = p_practice_id
    AND pu.is_active = true;
    
    RETURN COALESCE(user_role = p_required_role, false);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to update updated_on_utc timestamp
CREATE OR REPLACE FUNCTION update_updated_on_utc()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_on_utc = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- USER MANAGEMENT HELPER FUNCTIONS
-- ============================================================================

-- Function to create a user with temp password (for admin onboarding)
-- Returns the new app_user_id
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
        true,  -- Force password change on first login
        true
    )
    RETURNING app_user_id INTO v_app_user_id;
    
    RETURN v_app_user_id;
EXCEPTION WHEN unique_violation THEN
    RAISE EXCEPTION 'Email % already exists', p_email;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to generate a password reset token
-- Returns the token (not hashed; app will hash/store in DB)
CREATE OR REPLACE FUNCTION generate_password_reset_token(p_app_user_id UUID)
RETURNS VARCHAR AS $$
DECLARE
    v_token VARCHAR;
    v_expires_on_utc TIMESTAMPTZ;
BEGIN
    -- Generate random token (app will hash this before storing)
    v_token := encode(gen_random_bytes(32), 'hex');
    v_expires_on_utc := NOW() + INTERVAL '24 hours';
    
    INSERT INTO public.password_reset_token (app_user_id, token, expires_on_utc)
    VALUES (p_app_user_id, v_token, v_expires_on_utc);
    
    RETURN v_token;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to validate and mark a password reset token as used
-- Returns true if token is valid and not expired, false otherwise
CREATE OR REPLACE FUNCTION use_password_reset_token(p_token VARCHAR)
RETURNS BOOLEAN AS $$
DECLARE
    v_token_id UUID;
BEGIN
    -- Find valid, unused, non-expired token
    SELECT token_id INTO v_token_id
    FROM public.password_reset_token
    WHERE token = p_token
    AND used_on_utc IS NULL
    AND expires_on_utc > NOW()
    LIMIT 1;
    
    IF v_token_id IS NULL THEN
        RETURN false;
    END IF;
    
    -- Mark token as used
    UPDATE public.password_reset_token
    SET used_on_utc = NOW()
    WHERE token_id = v_token_id;
    
    RETURN true;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to get app_user_id from valid password reset token
-- Returns NULL if token is invalid or expired
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

-- Triggers to automatically update updated_on_utc
CREATE TRIGGER trigger_app_user_updated_on_utc
BEFORE UPDATE ON public.app_user
FOR EACH ROW
EXECUTE FUNCTION update_updated_on_utc();

CREATE TRIGGER trigger_practice_updated_on_utc
BEFORE UPDATE ON public.practice
FOR EACH ROW
EXECUTE FUNCTION update_updated_on_utc();

CREATE TRIGGER trigger_practice_user_updated_on_utc
BEFORE UPDATE ON public.practice_user
FOR EACH ROW
EXECUTE FUNCTION update_updated_on_utc();

CREATE TRIGGER trigger_therapist_updated_on_utc
BEFORE UPDATE ON public.therapist
FOR EACH ROW
EXECUTE FUNCTION update_updated_on_utc();

CREATE TRIGGER trigger_client_updated_on_utc
BEFORE UPDATE ON public.client
FOR EACH ROW
EXECUTE FUNCTION update_updated_on_utc();

CREATE TRIGGER trigger_case_file_updated_on_utc
BEFORE UPDATE ON public.case_file
FOR EACH ROW
EXECUTE FUNCTION update_updated_on_utc();

CREATE TRIGGER trigger_session_updated_on_utc
BEFORE UPDATE ON public.session
FOR EACH ROW
EXECUTE FUNCTION update_updated_on_utc();

CREATE TRIGGER trigger_treatment_plan_updated_on_utc
BEFORE UPDATE ON public.treatment_plan
FOR EACH ROW
EXECUTE FUNCTION update_updated_on_utc();

CREATE TRIGGER trigger_treatment_goal_updated_on_utc
BEFORE UPDATE ON public.treatment_goal
FOR EACH ROW
EXECUTE FUNCTION update_updated_on_utc();

CREATE TRIGGER trigger_treatment_intervention_updated_on_utc
BEFORE UPDATE ON public.treatment_intervention
FOR EACH ROW
EXECUTE FUNCTION update_updated_on_utc();

CREATE TRIGGER trigger_service_code_updated_on_utc
BEFORE UPDATE ON public.service_code
FOR EACH ROW
EXECUTE FUNCTION update_updated_on_utc();

CREATE TRIGGER trigger_invoice_updated_on_utc
BEFORE UPDATE ON public.invoice
FOR EACH ROW
EXECUTE FUNCTION update_updated_on_utc();

CREATE TRIGGER trigger_availability_rule_updated_on_utc
BEFORE UPDATE ON public.availability_rule
FOR EACH ROW
EXECUTE FUNCTION update_updated_on_utc();

-- ============================================================================
-- END OF SCHEMA
-- ============================================================================
