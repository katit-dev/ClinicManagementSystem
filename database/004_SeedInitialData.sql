USE ClinicManagementSystem;
GO

-- ============================================================
-- SEED SYSTEM ROLES
-- ============================================================

IF NOT EXISTS (
    SELECT 1
    FROM auth.roles
    WHERE name = 'Admin'
)
BEGIN
    INSERT INTO auth.roles
    (
        name,
        description,
        is_system
    )
    VALUES
    (
        'Admin',
        N'Quản trị viên',
        1
    );
END;
GO


IF NOT EXISTS (
    SELECT 1
    FROM auth.roles
    WHERE name = 'Receptionist'
)
BEGIN
    INSERT INTO auth.roles
    (
        name,
        description,
        is_system
    )
    VALUES
    (
        'Receptionist',
        N'Lễ tân',
        1
    );
END;
GO


IF NOT EXISTS (
    SELECT 1
    FROM auth.roles
    WHERE name = 'Doctor'
)
BEGIN
    INSERT INTO auth.roles
    (
        name,
        description,
        is_system
    )
    VALUES
    (
        'Doctor',
        N'Bác sĩ',
        1
    );
END;
GO


IF NOT EXISTS (
    SELECT 1
    FROM auth.roles
    WHERE name = 'Patient'
)
BEGIN
    INSERT INTO auth.roles
    (
        name,
        description,
        is_system
    )
    VALUES
    (
        'Patient',
        N'Bệnh nhân',
        1
    );
END;
GO


USE ClinicManagementSystem;
GO


-- ============================================================
-- SEED SPECIALTIES
-- ============================================================

IF NOT EXISTS
(
    SELECT 1
    FROM scheduling.specialties
    WHERE code = 'CARD'
)
BEGIN
    INSERT INTO scheduling.specialties
    (
        name,
        description,
        is_active,
        created_at,
        code
    )
    VALUES
    (
        N'Tim mạch',
        N'Khám và điều trị các bệnh lý về tim mạch.',
        1,
        GETDATE(),
        'CARD'
    );
END;
GO


IF NOT EXISTS
(
    SELECT 1
    FROM scheduling.specialties
    WHERE code = 'GEN'
)
BEGIN
    INSERT INTO scheduling.specialties
    (
        name,
        description,
        is_active,
        created_at,
        code
    )
    VALUES
    (
        N'Nội tổng quát',
        N'Khám và điều trị các bệnh lý nội khoa tổng quát.',
        1,
        GETDATE(),
        'GEN'
    );
END;
GO


IF NOT EXISTS
(
    SELECT 1
    FROM scheduling.specialties
    WHERE code = 'DERM'
)
BEGIN
    INSERT INTO scheduling.specialties
    (
        name,
        description,
        is_active,
        created_at,
        code
    )
    VALUES
    (
        N'Da liễu',
        N'Khám và điều trị các bệnh lý về da.',
        1,
        GETDATE(),
        'DERM'
    );
END;
GO


IF NOT EXISTS
(
    SELECT 1
    FROM scheduling.specialties
    WHERE code = 'PED'
)
BEGIN
    INSERT INTO scheduling.specialties
    (
        name,
        description,
        is_active,
        created_at,
        code
    )
    VALUES
    (
        N'Nhi khoa',
        N'Khám và điều trị bệnh cho trẻ em.',
        1,
        GETDATE(),
        'PED'
    );
END;
GO

-- ============================================================
-- SEED DOCTOR ACCOUNTS + DOCTOR PROFILES
--
-- Flow:
--
-- auth.users
--      ↓
-- auth.user_roles
--      ↓
-- Role = Doctor
--      ↓
-- scheduling.doctors
--      ↓
-- scheduling.specialties
--
-- NOTE:
-- Các tài khoản seed sẽ sử dụng cùng password_hash
-- với Doctor đã tồn tại trong database.
-- ============================================================


-- ============================================================
-- GET DOCTOR ROLE
-- ============================================================

DECLARE @DoctorRoleId INT;

SELECT @DoctorRoleId = id
FROM auth.roles
WHERE name = 'Doctor';


IF @DoctorRoleId IS NULL
BEGIN
    THROW 50001, 'Doctor role does not exist.', 1;
END;


-- ============================================================
-- GET EXISTING DOCTOR PASSWORD HASH
--
-- Database hiện đã có ít nhất 1 Doctor.
-- Ta dùng password_hash của tài khoản đó
-- cho các tài khoản Doctor seed.
-- ============================================================

DECLARE @DoctorPasswordHash NVARCHAR(255);

SELECT TOP 1
    @DoctorPasswordHash = u.password_hash
FROM auth.users u
INNER JOIN scheduling.doctors d
    ON d.user_id = u.id
WHERE
    u.is_active = 1
    AND u.is_deleted = 0;


IF @DoctorPasswordHash IS NULL
BEGIN
    THROW 50002,
        'No existing Doctor account found to copy password hash.',
        1;
END;


-- ============================================================
-- GET SPECIALTY IDS
-- ============================================================

DECLARE @NoiKhoaId INT;
DECLARE @CardiologyId INT;
DECLARE @GeneralId INT;
DECLARE @DermatologyId INT;
DECLARE @PediatricsId INT;


SELECT @NoiKhoaId = id
FROM scheduling.specialties
WHERE code = 'NOI';


SELECT @CardiologyId = id
FROM scheduling.specialties
WHERE code = 'CARD';


SELECT @GeneralId = id
FROM scheduling.specialties
WHERE code = 'GEN';


SELECT @DermatologyId = id
FROM scheduling.specialties
WHERE code = 'DERM';


SELECT @PediatricsId = id
FROM scheduling.specialties
WHERE code = 'PED';


-- ============================================================
-- DOCTOR 01
-- TIM MẠCH
-- ============================================================

IF NOT EXISTS
(
    SELECT 1
    FROM auth.users
    WHERE username = 'doctor.card01'
)
BEGIN

    INSERT INTO auth.users
    (
        username,
        password_hash,
        email,
        full_name,
        phone,
        is_active,
        created_at,
        failed_login_count,
        email_confirmed,
        is_deleted
    )
    VALUES
    (
        'doctor.card01',
        @DoctorPasswordHash,
        'doctor.card01@clinic.local',
        N'Nguyễn Minh Khang',
        '0910000001',
        1,
        GETDATE(),
        0,
        1,
        0
    );

END;


DECLARE @DoctorUserId01 INT;

SELECT @DoctorUserId01 = id
FROM auth.users
WHERE username = 'doctor.card01';


-- ============================================================
-- ASSIGN DOCTOR ROLE
-- ============================================================

IF NOT EXISTS
(
    SELECT 1
    FROM auth.user_roles
    WHERE
        user_id = @DoctorUserId01
        AND role_id = @DoctorRoleId
)
BEGIN

    INSERT INTO auth.user_roles
    (
        user_id,
        role_id,
        assigned_at,
        assigned_by
    )
    VALUES
    (
        @DoctorUserId01,
        @DoctorRoleId,
        GETDATE(),
        NULL
    );

END;


-- ============================================================
-- CREATE DOCTOR PROFILE
-- ============================================================

IF @CardiologyId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1
    FROM scheduling.doctors
    WHERE user_id = @DoctorUserId01
)
BEGIN

    INSERT INTO scheduling.doctors
    (
        user_id,
        specialty_id,
        full_name,
        title,
        license_number,
        phone,
        email,
        room,
        is_active,
        created_at,
        consultation_fee,
        bio,
        experience_years,
        max_patients_per_day
    )
    VALUES
    (
        @DoctorUserId01,
        @CardiologyId,
        N'Nguyễn Minh Khang',
        N'TS.BS',
        'LIC-CARD-001',
        '0910000001',
        'doctor.card01@clinic.local',
        N'P.201',
        1,
        GETDATE(),
        300000,
        N'Bác sĩ chuyên khoa Tim mạch.',
        12,
        30
    );

END;


-- ============================================================
-- DOCTOR 02
-- TIM MẠCH
-- ============================================================

IF NOT EXISTS
(
    SELECT 1
    FROM auth.users
    WHERE username = 'doctor.card02'
)
BEGIN

    INSERT INTO auth.users
    (
        username,
        password_hash,
        email,
        full_name,
        phone,
        is_active,
        created_at,
        failed_login_count,
        email_confirmed,
        is_deleted
    )
    VALUES
    (
        'doctor.card02',
        @DoctorPasswordHash,
        'doctor.card02@clinic.local',
        N'Trần Hoàng Nam',
        '0910000002',
        1,
        GETDATE(),
        0,
        1,
        0
    );

END;


DECLARE @DoctorUserId02 INT;

SELECT @DoctorUserId02 = id
FROM auth.users
WHERE username = 'doctor.card02';


IF NOT EXISTS
(
    SELECT 1
    FROM auth.user_roles
    WHERE
        user_id = @DoctorUserId02
        AND role_id = @DoctorRoleId
)
BEGIN

    INSERT INTO auth.user_roles
    (
        user_id,
        role_id,
        assigned_at,
        assigned_by
    )
    VALUES
    (
        @DoctorUserId02,
        @DoctorRoleId,
        GETDATE(),
        NULL
    );

END;


IF @CardiologyId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1
    FROM scheduling.doctors
    WHERE user_id = @DoctorUserId02
)
BEGIN

    INSERT INTO scheduling.doctors
    (
        user_id,
        specialty_id,
        full_name,
        title,
        license_number,
        phone,
        email,
        room,
        is_active,
        created_at,
        consultation_fee,
        bio,
        experience_years,
        max_patients_per_day
    )
    VALUES
    (
        @DoctorUserId02,
        @CardiologyId,
        N'Trần Hoàng Nam',
        N'BS.CKI',
        'LIC-CARD-002',
        '0910000002',
        'doctor.card02@clinic.local',
        N'P.202',
        1,
        GETDATE(),
        250000,
        N'Bác sĩ chuyên khoa Tim mạch.',
        8,
        25
    );

END;


-- ============================================================
-- DOCTOR 03
-- NỘI TỔNG QUÁT
-- ============================================================

IF NOT EXISTS
(
    SELECT 1
    FROM auth.users
    WHERE username = 'doctor.gen01'
)
BEGIN

    INSERT INTO auth.users
    (
        username,
        password_hash,
        email,
        full_name,
        phone,
        is_active,
        created_at,
        failed_login_count,
        email_confirmed,
        is_deleted
    )
    VALUES
    (
        'doctor.gen01',
        @DoctorPasswordHash,
        'doctor.gen01@clinic.local',
        N'Lê Thu Hà',
        '0910000003',
        1,
        GETDATE(),
        0,
        1,
        0
    );

END;


DECLARE @DoctorUserId03 INT;

SELECT @DoctorUserId03 = id
FROM auth.users
WHERE username = 'doctor.gen01';


IF NOT EXISTS
(
    SELECT 1
    FROM auth.user_roles
    WHERE
        user_id = @DoctorUserId03
        AND role_id = @DoctorRoleId
)
BEGIN

    INSERT INTO auth.user_roles
    (
        user_id,
        role_id,
        assigned_at,
        assigned_by
    )
    VALUES
    (
        @DoctorUserId03,
        @DoctorRoleId,
        GETDATE(),
        NULL
    );

END;


IF @GeneralId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1
    FROM scheduling.doctors
    WHERE user_id = @DoctorUserId03
)
BEGIN

    INSERT INTO scheduling.doctors
    (
        user_id,
        specialty_id,
        full_name,
        title,
        license_number,
        phone,
        email,
        room,
        is_active,
        created_at,
        consultation_fee,
        bio,
        experience_years,
        max_patients_per_day
    )
    VALUES
    (
        @DoctorUserId03,
        @GeneralId,
        N'Lê Thu Hà',
        N'BS.CKI',
        'LIC-GEN-001',
        '0910000003',
        'doctor.gen01@clinic.local',
        N'P.203',
        1,
        GETDATE(),
        200000,
        N'Bác sĩ chuyên khoa Nội tổng quát.',
        7,
        30
    );

END;


-- ============================================================
-- DOCTOR 04
-- DA LIỄU
-- ============================================================

IF NOT EXISTS
(
    SELECT 1
    FROM auth.users
    WHERE username = 'doctor.derm01'
)
BEGIN

    INSERT INTO auth.users
    (
        username,
        password_hash,
        email,
        full_name,
        phone,
        is_active,
        created_at,
        failed_login_count,
        email_confirmed,
        is_deleted
    )
    VALUES
    (
        'doctor.derm01',
        @DoctorPasswordHash,
        'doctor.derm01@clinic.local',
        N'Phạm Ngọc Lan',
        '0910000004',
        1,
        GETDATE(),
        0,
        1,
        0
    );

END;


DECLARE @DoctorUserId04 INT;

SELECT @DoctorUserId04 = id
FROM auth.users
WHERE username = 'doctor.derm01';


IF NOT EXISTS
(
    SELECT 1
    FROM auth.user_roles
    WHERE
        user_id = @DoctorUserId04
        AND role_id = @DoctorRoleId
)
BEGIN

    INSERT INTO auth.user_roles
    (
        user_id,
        role_id,
        assigned_at,
        assigned_by
    )
    VALUES
    (
        @DoctorUserId04,
        @DoctorRoleId,
        GETDATE(),
        NULL
    );

END;


IF @DermatologyId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1
    FROM scheduling.doctors
    WHERE user_id = @DoctorUserId04
)
BEGIN

    INSERT INTO scheduling.doctors
    (
        user_id,
        specialty_id,
        full_name,
        title,
        license_number,
        phone,
        email,
        room,
        is_active,
        created_at,
        consultation_fee,
        bio,
        experience_years,
        max_patients_per_day
    )
    VALUES
    (
        @DoctorUserId04,
        @DermatologyId,
        N'Phạm Ngọc Lan',
        N'ThS.BS',
        'LIC-DERM-001',
        '0910000004',
        'doctor.derm01@clinic.local',
        N'P.301',
        1,
        GETDATE(),
        250000,
        N'Bác sĩ chuyên khoa Da liễu.',
        9,
        25
    );

END;


-- ============================================================
-- DOCTOR 05
-- NỘI KHOA
--
-- Database của bạn đã có 1 Doctor Nội khoa.
-- Đây là Doctor thứ hai để test specialty có nhiều bác sĩ.
-- ============================================================

IF NOT EXISTS
(
    SELECT 1
    FROM auth.users
    WHERE username = 'doctor.noi02'
)
BEGIN

    INSERT INTO auth.users
    (
        username,
        password_hash,
        email,
        full_name,
        phone,
        is_active,
        created_at,
        failed_login_count,
        email_confirmed,
        is_deleted
    )
    VALUES
    (
        'doctor.noi02',
        @DoctorPasswordHash,
        'doctor.noi02@clinic.local',
        N'Vũ Quốc Huy',
        '0910000005',
        1,
        GETDATE(),
        0,
        1,
        0
    );

END;


DECLARE @DoctorUserId05 INT;

SELECT @DoctorUserId05 = id
FROM auth.users
WHERE username = 'doctor.noi02';


IF NOT EXISTS
(
    SELECT 1
    FROM auth.user_roles
    WHERE
        user_id = @DoctorUserId05
        AND role_id = @DoctorRoleId
)
BEGIN

    INSERT INTO auth.user_roles
    (
        user_id,
        role_id,
        assigned_at,
        assigned_by
    )
    VALUES
    (
        @DoctorUserId05,
        @DoctorRoleId,
        GETDATE(),
        NULL
    );

END;


IF @NoiKhoaId IS NOT NULL
AND NOT EXISTS
(
    SELECT 1
    FROM scheduling.doctors
    WHERE user_id = @DoctorUserId05
)
BEGIN

    INSERT INTO scheduling.doctors
    (
        user_id,
        specialty_id,
        full_name,
        title,
        license_number,
        phone,
        email,
        room,
        is_active,
        created_at,
        consultation_fee,
        bio,
        experience_years,
        max_patients_per_day
    )
    VALUES
    (
        @DoctorUserId05,
        @NoiKhoaId,
        N'Vũ Quốc Huy',
        N'BS.CKI',
        'LIC-NOI-002',
        '0910000005',
        'doctor.noi02@clinic.local',
        N'P.102',
        1,
        GETDATE(),
        200000,
        N'Bác sĩ chuyên khoa Nội khoa.',
        6,
        30
    );

END;


-- ============================================================
-- VERIFY USERS
-- ============================================================

SELECT
    u.id,
    u.username,
    u.full_name,
    u.email,
    u.phone,
    u.is_active
FROM auth.users u
WHERE u.username IN
(
    'doctor.card01',
    'doctor.card02',
    'doctor.gen01',
    'doctor.derm01',
    'doctor.noi02'
)
ORDER BY u.id;


-- ============================================================
-- VERIFY USER ROLES
-- ============================================================

SELECT
    u.id AS user_id,
    u.username,
    r.name AS role_name
FROM auth.users u
INNER JOIN auth.user_roles ur
    ON ur.user_id = u.id
INNER JOIN auth.roles r
    ON r.id = ur.role_id
WHERE u.username IN
(
    'doctor.card01',
    'doctor.card02',
    'doctor.gen01',
    'doctor.derm01',
    'doctor.noi02'
)
ORDER BY u.id;


-- ============================================================
-- VERIFY DOCTORS
-- ============================================================

SELECT
    d.id AS doctor_id,
    d.user_id,
    d.full_name,
    d.title,
    s.code AS specialty_code,
    s.name AS specialty_name,
    d.room,
    d.consultation_fee,
    d.experience_years,
    d.max_patients_per_day,
    d.is_active
FROM scheduling.doctors d
INNER JOIN scheduling.specialties s
    ON s.id = d.specialty_id
ORDER BY
    s.id,
    d.id;

GO

USE ClinicManagementSystem;
GO


-- ============================================================
-- SEED DOCTOR SCHEDULES
--
-- DayOfWeek convention:
--
-- 0 = Sunday
-- 1 = Monday
-- 2 = Tuesday
-- 3 = Wednesday
-- 4 = Thursday
-- 5 = Friday
-- 6 = Saturday
--
-- Default schedule:
--
-- Morning / Afternoon:
-- 08:00 - 17:00
--
-- Break:
-- 12:00 - 13:30
--
-- Slot:
-- 30 minutes
-- ============================================================


DECLARE @EffectiveFrom DATE =
    CAST(GETDATE() AS DATE);


-- ============================================================
-- DOCTOR CARD 01
-- doctor.card01
--
-- Monday - Friday
-- ============================================================

DECLARE @DoctorCard01Id INT;

SELECT @DoctorCard01Id = d.id
FROM scheduling.doctors d
INNER JOIN auth.users u
    ON u.id = d.user_id
WHERE u.username = 'doctor.card01';


IF @DoctorCard01Id IS NOT NULL
BEGIN

    DECLARE @DayCard01 TINYINT = 1;

    WHILE @DayCard01 <= 5
    BEGIN

        IF NOT EXISTS
        (
            SELECT 1
            FROM scheduling.doctor_schedules
            WHERE
                doctor_id = @DoctorCard01Id
                AND day_of_week = @DayCard01
                AND effective_from = @EffectiveFrom
        )
        BEGIN

            INSERT INTO scheduling.doctor_schedules
            (
                doctor_id,
                day_of_week,
                start_time,
                end_time,
                slot_minutes,
                is_active,
                effective_from,
                effective_to,
                break_start,
                break_end
            )
            VALUES
            (
                @DoctorCard01Id,
                @DayCard01,
                '08:00',
                '17:00',
                30,
                1,
                @EffectiveFrom,
                NULL,
                '12:00',
                '13:30'
            );

        END;

        SET @DayCard01 = @DayCard01 + 1;

    END;

END;


-- ============================================================
-- DOCTOR CARD 02
-- doctor.card02
--
-- Monday - Friday
-- ============================================================

DECLARE @DoctorCard02Id INT;

SELECT @DoctorCard02Id = d.id
FROM scheduling.doctors d
INNER JOIN auth.users u
    ON u.id = d.user_id
WHERE u.username = 'doctor.card02';


IF @DoctorCard02Id IS NOT NULL
BEGIN

    DECLARE @DayCard02 TINYINT = 1;

    WHILE @DayCard02 <= 5
    BEGIN

        IF NOT EXISTS
        (
            SELECT 1
            FROM scheduling.doctor_schedules
            WHERE
                doctor_id = @DoctorCard02Id
                AND day_of_week = @DayCard02
                AND effective_from = @EffectiveFrom
        )
        BEGIN

            INSERT INTO scheduling.doctor_schedules
            (
                doctor_id,
                day_of_week,
                start_time,
                end_time,
                slot_minutes,
                is_active,
                effective_from,
                effective_to,
                break_start,
                break_end
            )
            VALUES
            (
                @DoctorCard02Id,
                @DayCard02,
                '08:00',
                '17:00',
                30,
                1,
                @EffectiveFrom,
                NULL,
                '12:00',
                '13:30'
            );

        END;

        SET @DayCard02 = @DayCard02 + 1;

    END;

END;


-- ============================================================
-- DOCTOR GENERAL
-- doctor.gen01
--
-- Monday - Friday
-- ============================================================

DECLARE @DoctorGen01Id INT;

SELECT @DoctorGen01Id = d.id
FROM scheduling.doctors d
INNER JOIN auth.users u
    ON u.id = d.user_id
WHERE u.username = 'doctor.gen01';


IF @DoctorGen01Id IS NOT NULL
BEGIN

    DECLARE @DayGen01 TINYINT = 1;

    WHILE @DayGen01 <= 5
    BEGIN

        IF NOT EXISTS
        (
            SELECT 1
            FROM scheduling.doctor_schedules
            WHERE
                doctor_id = @DoctorGen01Id
                AND day_of_week = @DayGen01
                AND effective_from = @EffectiveFrom
        )
        BEGIN

            INSERT INTO scheduling.doctor_schedules
            (
                doctor_id,
                day_of_week,
                start_time,
                end_time,
                slot_minutes,
                is_active,
                effective_from,
                effective_to,
                break_start,
                break_end
            )
            VALUES
            (
                @DoctorGen01Id,
                @DayGen01,
                '08:00',
                '17:00',
                30,
                1,
                @EffectiveFrom,
                NULL,
                '12:00',
                '13:30'
            );

        END;

        SET @DayGen01 = @DayGen01 + 1;

    END;

END;


-- ============================================================
-- DOCTOR DERM
-- doctor.derm01
--
-- Monday - Saturday
-- ============================================================

DECLARE @DoctorDerm01Id INT;

SELECT @DoctorDerm01Id = d.id
FROM scheduling.doctors d
INNER JOIN auth.users u
    ON u.id = d.user_id
WHERE u.username = 'doctor.derm01';


IF @DoctorDerm01Id IS NOT NULL
BEGIN

    DECLARE @DayDerm01 TINYINT = 1;

    WHILE @DayDerm01 <= 6
    BEGIN

        IF NOT EXISTS
        (
            SELECT 1
            FROM scheduling.doctor_schedules
            WHERE
                doctor_id = @DoctorDerm01Id
                AND day_of_week = @DayDerm01
                AND effective_from = @EffectiveFrom
        )
        BEGIN

            INSERT INTO scheduling.doctor_schedules
            (
                doctor_id,
                day_of_week,
                start_time,
                end_time,
                slot_minutes,
                is_active,
                effective_from,
                effective_to,
                break_start,
                break_end
            )
            VALUES
            (
                @DoctorDerm01Id,
                @DayDerm01,
                '08:00',
                '17:00',
                30,
                1,
                @EffectiveFrom,
                NULL,
                '12:00',
                '13:30'
            );

        END;

        SET @DayDerm01 = @DayDerm01 + 1;

    END;

END;


-- ============================================================
-- DOCTOR INTERNAL MEDICINE
-- doctor.noi02
--
-- Monday - Friday
-- ============================================================

DECLARE @DoctorNoi02Id INT;

SELECT @DoctorNoi02Id = d.id
FROM scheduling.doctors d
INNER JOIN auth.users u
    ON u.id = d.user_id
WHERE u.username = 'doctor.noi02';


IF @DoctorNoi02Id IS NOT NULL
BEGIN

    DECLARE @DayNoi02 TINYINT = 1;

    WHILE @DayNoi02 <= 5
    BEGIN

        IF NOT EXISTS
        (
            SELECT 1
            FROM scheduling.doctor_schedules
            WHERE
                doctor_id = @DoctorNoi02Id
                AND day_of_week = @DayNoi02
                AND effective_from = @EffectiveFrom
        )
        BEGIN

            INSERT INTO scheduling.doctor_schedules
            (
                doctor_id,
                day_of_week,
                start_time,
                end_time,
                slot_minutes,
                is_active,
                effective_from,
                effective_to,
                break_start,
                break_end
            )
            VALUES
            (
                @DoctorNoi02Id,
                @DayNoi02,
                '08:00',
                '17:00',
                30,
                1,
                @EffectiveFrom,
                NULL,
                '12:00',
                '13:30'
            );

        END;

        SET @DayNoi02 = @DayNoi02 + 1;

    END;

END;


-- ============================================================
-- VERIFY
-- ============================================================

SELECT
    ds.id,
    d.id AS doctor_id,
    d.full_name,
    u.username,
    s.name AS specialty_name,

    ds.day_of_week,

    CASE ds.day_of_week
        WHEN 0 THEN 'Sunday'
        WHEN 1 THEN 'Monday'
        WHEN 2 THEN 'Tuesday'
        WHEN 3 THEN 'Wednesday'
        WHEN 4 THEN 'Thursday'
        WHEN 5 THEN 'Friday'
        WHEN 6 THEN 'Saturday'
    END AS day_name,

    ds.start_time,
    ds.end_time,
    ds.break_start,
    ds.break_end,
    ds.slot_minutes,
    ds.effective_from,
    ds.effective_to,
    ds.is_active

FROM scheduling.doctor_schedules ds

INNER JOIN scheduling.doctors d
    ON d.id = ds.doctor_id

INNER JOIN auth.users u
    ON u.id = d.user_id

INNER JOIN scheduling.specialties s
    ON s.id = d.specialty_id

ORDER BY
    d.id,
    ds.day_of_week;

GO