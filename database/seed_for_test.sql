USE ClinicManagementSystem;
GO

INSERT INTO scheduling.patients
(
    user_id,
    full_name,
    date_of_birth,
    phone,
    patient_code
)
VALUES
(
    NULL,
    N'Nguyễn Văn Test',
    '2000-05-10',
    '0901234567',
    'BNTEST000000000001'
);

-- kiem tra
SELECT *
FROM scheduling.patients
WHERE phone = '0901234567';

DELETE FROM scheduling.patients
WHERE patient_code = 'BNTEST000000000001';

SELECT *
FROM auth.roles;

--kiem tra db sau khi test register api success
SELECT *
FROM auth.users
WHERE phone = '0901111111';

SELECT *
FROM scheduling.patients
WHERE phone = '0901111111';

SELECT ur.*
FROM auth.user_roles ur
JOIN auth.users u ON u.id = ur.user_id
WHERE u.phone = '0901111111';


-- Có Patient cũ nhưng chưa có account
INSERT INTO scheduling.patients
(
    user_id,
    full_name,
    date_of_birth,
    phone,
    patient_code
)
VALUES
(
    NULL,
    N'Trần Thị Vãng Lai',
    '1995-08-20',
    '0902222222',
    'BNTESTVANGLAI00001'
);

SELECT id, user_id, full_name, phone, patient_code
FROM scheduling.patients
WHERE phone = '0902222222';

-- insert role bác sĩ
UPDATE ur
SET ur.role_id = rDoctor.id
FROM auth.user_roles ur
CROSS JOIN auth.roles rDoctor
WHERE ur.user_id = 5
  AND rDoctor.name = 'Doctor';

-- insert 1 specialty
  INSERT INTO scheduling.specialties
(
    code,
    name
)
VALUES
(
    'NOI',
    N'Nội khoa'
);

-- tao doctor profile
INSERT INTO scheduling.doctors
(
    user_id,
    specialty_id,
    full_name,
    phone,
    email,
    license_number
)
VALUES
(
    3,
    1,
    N'Nguyễn Văn Bác Sĩ',
    '0903333333',
    'doctor.test@gmail.com',
    'BS-TEST-001'
);

-- kiem tra
SELECT
    u.id,
    u.username,
    u.full_name,
    r.name AS role_name
FROM auth.users u
LEFT JOIN auth.user_roles ur
    ON ur.user_id = u.id
LEFT JOIN auth.roles r
    ON r.id = ur.role_id
WHERE u.phone = '0903333333';

-- 
UPDATE scheduling.patients
SET user_id = NULL
WHERE user_id = 3;

DELETE FROM scheduling.patients
WHERE id = 4
  AND user_id IS NULL;

  -- TEST REFRESH TOKEN
  SELECT
    id,
    user_id,
    token_hash,
    expires_at,
    revoked_at,
    created_at
FROM auth.refresh_tokens
WHERE user_id = 3
ORDER BY id DESC;


-- test case: forgot password
SELECT TOP 10
    id,
    user_id,
    token_hash,
    expires_at,
    used_at,
    revoked_at,
    created_at
FROM auth.password_reset_tokens
ORDER BY id DESC;



-- ============================================================
-- TEST DATA - DOCTOR TIME OFF
-- chon doctorId = 5, ngay test 2026-09-21, gio nghi 09:00-10:00
-- ============================================================
USE ClinicManagementSystem;
GO


DECLARE @DoctorId INT = 5;

DECLARE @StartAt DATETIME =
    '2026-09-21T09:00:00';

DECLARE @EndAt DATETIME =
    '2026-09-21T10:00:00';


IF NOT EXISTS
(
    SELECT 1
    FROM scheduling.doctor_time_off
    WHERE
        doctor_id = @DoctorId
        AND start_at = @StartAt
        AND end_at = @EndAt
)
BEGIN

    INSERT INTO scheduling.doctor_time_off
    (
        doctor_id,
        start_at,
        end_at,
        reason,
        created_at,
        type,
        is_full_day,
        approved_by
    )
    VALUES
    (
        @DoctorId,
        @StartAt,
        @EndAt,
        N'Test DoctorTimeOff',
        GETDATE(),
        NULL,
        0,
        NULL
    );

END;

-- ============================================================
-- TEST DATA - BOOKED APPOINTMENT - test getAvailableSlot(khi lay thi se chua ra slot co appointment)
-- ============================================================

DECLARE @DoctorId INT = 5;
DECLARE @PatientId INT = 1;

DECLARE @StartTime DATETIME =
    '2026-09-21T10:30:00';

DECLARE @EndTime DATETIME =
    '2026-09-21T11:00:00';


IF NOT EXISTS
(
    SELECT 1
    FROM scheduling.appointments
    WHERE
        doctor_id = @DoctorId
        AND start_time = @StartTime
        AND end_time = @EndTime
        AND status < 4
)
BEGIN

    INSERT INTO scheduling.appointments
    (
        patient_id,
        doctor_id,
        start_time,
        end_time,
        status,
        reason,
        note,
        created_by,
        created_at,
        appointment_code,
        queue_number,
        checked_in_at,
        completed_at,
        cancel_reason,
        source,
        fee_snapshot
    )
    VALUES
    (
        @PatientId,
        @DoctorId,
        @StartTime,
        @EndTime,

        0,                          -- Status test: < 4

        N'Test booked slot',
        NULL,
        NULL,
        GETDATE(),

        CONCAT(
            'TEST-',
            FORMAT(GETDATE(), 'yyyyMMddHHmmss')
        ),

        NULL,
        NULL,
        NULL,
        NULL,

        0,                          -- Source test placeholder

        200000
    );

END;


-- ============================================================
-- VERIFY
-- ============================================================

SELECT
    id,
    appointment_code,
    patient_id,
    doctor_id,
    start_time,
    end_time,
    status,
    source,
    fee_snapshot
FROM scheduling.appointments
WHERE
    doctor_id = @DoctorId
    AND start_time = @StartTime;