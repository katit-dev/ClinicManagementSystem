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