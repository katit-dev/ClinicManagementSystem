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

