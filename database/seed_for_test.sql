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