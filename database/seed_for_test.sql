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

USE ClinicManagementSystem

-- test get medical record
DECLARE @AppointmentId INT = 2;

INSERT INTO clinical.medical_records
(
    appointment_id,
    patient_id,
    doctor_id,
    symptoms,
    diagnosis,
    note,
    created_at,
    icd10_code,
    treatment_plan,
    follow_up_date,
    status,
    finalized_at
)
SELECT
    a.id,
    a.patient_id,
    a.doctor_id,
    N'Đau đầu, chóng mặt',
    N'Đau đầu do căng thẳng',
    N'Theo dõi thêm nếu triệu chứng kéo dài',
    GETDATE(),
    'R51',
    N'Nghỉ ngơi, uống đủ nước và theo dõi triệu chứng',
    DATEADD(DAY, 7, CAST(GETDATE() AS DATE)),
    1,
    GETDATE()
FROM scheduling.appointments a
WHERE a.id = @AppointmentId
AND NOT EXISTS
(
    SELECT 1
    FROM clinical.medical_records m
    WHERE m.appointment_id = a.id
);

SELECT
    id,
    appointment_id,
    patient_id,
    doctor_id,
    symptoms,
    diagnosis,
    icd10_code,
    treatment_plan,
    follow_up_date,
    status,
    finalized_at
FROM clinical.medical_records
WHERE appointment_id = 2;

-- tao medical record status = 0
DECLARE @AppointmentId INT = 3;

INSERT INTO clinical.medical_records
(
    appointment_id,
    patient_id,
    doctor_id,
    symptoms,
    diagnosis,
    note,
    created_at,
    updated_at,
    icd10_code,
    treatment_plan,
    follow_up_date,
    status,
    finalized_at
)
SELECT
    a.id,
    a.patient_id,
    a.doctor_id,
    N'Ho, đau họng nhẹ',
    N'Đang chờ bác sĩ hoàn tất chẩn đoán',
    N'Bệnh án đang ở trạng thái nháp để test',
    GETDATE(),
    NULL,
    NULL,
    N'Tiếp tục theo dõi',
    NULL,
    0,          -- Draft
    NULL        -- Draft thì chưa có finalized_at
FROM scheduling.appointments a
WHERE a.id = @AppointmentId
  AND NOT EXISTS
  (
      SELECT 1
      FROM clinical.medical_records m
      WHERE m.appointment_id = a.id
  );

  SELECT
    id,
    appointment_id,
    patient_id,
    doctor_id,
    symptoms,
    diagnosis,
    status,
    finalized_at,
    created_at
FROM clinical.medical_records
WHERE appointment_id = 3;


-- =====================================================
-- TEST MEDICAL RECORD FOR VC-05
-- =====================================================
USE ClinicManagementSystem;
GO

SELECT *
FROM scheduling.appointments
WHERE status = 3
ORDER BY start_time DESC;

-- CHECK EXISTING MEDICAL RECORD
SELECT *
FROM clinical.medical_records
WHERE appointment_id = 2;

-- =====================================================
-- CREATE TEST PRESCRIPTION
-- =====================================================

INSERT INTO clinical.prescriptions
(
    medical_record_id,
    note,
    created_at,
    doctor_id,
    status,
    dispensed_at,
    dispensed_by
)
VALUES
(
    1,
    N'Đơn thuốc dùng để test VC-05.',
    SYSDATETIME(),
    5,
    0,
    NULL,
    NULL
);


SELECT *
FROM clinical.prescriptions
WHERE medical_record_id = 1;

-- =====================================================
-- FIND MEDICINE TABLE
-- =====================================================

USE ClinicManagementSystem;
GO

SELECT
    TABLE_SCHEMA,
    TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME LIKE '%medicine%';

-- =====================================================
-- CREATE TEST MEDICINE
-- =====================================================

INSERT INTO clinical.medicines
(
    name,
    unit,
    price,
    stock_quantity,
    description,
    is_active,
    code,
    active_ingredient,
    concentration,
    cost_price,
    min_stock
)
VALUES
(
    N'Paracetamol 500mg',
    N'Viên',
    2000,
    100,
    N'Thuốc giảm đau, hạ sốt dùng để test VC-05.',
    1,
    'MED-PARA-500',
    N'Paracetamol',
    N'500mg',
    1000,
    10
);

-- =====================================================
-- CREATE TEST PRESCRIPTION ITEM
-- =====================================================

USE ClinicManagementSystem;
GO

INSERT INTO clinical.prescription_items
(
    prescription_id,
    medicine_id,
    quantity,
    dosage,
    instruction,
    medicine_name_snapshot,
    unit_price_snapshot,
    duration_days,
    frequency
)
VALUES
(
    1,
    1,
    20,
    N'1 viên/lần',
    N'Uống sau ăn.',
    N'Paracetamol 500mg',
    2000,
    10,
    N'2 lần/ngày'
);

-- =====================================================
-- CHECK PRESCRIPTION ITEM
-- =====================================================

SELECT *
FROM clinical.prescription_items
WHERE prescription_id = 1;


-- =====================================================
-- CREATE TEST LAB SERVICE
-- =====================================================

USE ClinicManagementSystem;
GO

INSERT INTO billing.services
(
    name,
    description,
    price,
    is_active,
    code,
    specialty_id,
    duration_minutes
)
VALUES
(
    N'Xét nghiệm công thức máu',
    N'Dịch vụ xét nghiệm dùng để test VC-05.',
    150000,
    1,
    'LAB-CBC',
    NULL,
    30
);

-- =====================================================
-- CHECK CREATED SERVICE
-- =====================================================

SELECT *
FROM billing.services
WHERE code = 'LAB-CBC';

-- =====================================================
-- CREATE TEST MEDICAL RECORD SERVICE
-- =====================================================

INSERT INTO clinical.medical_record_services
(
    medical_record_id,
    service_id,
    unit_price_snapshot
)
VALUES
(
    1,
    1,
    150000
);


-- =====================================================
-- CREATE TEST LAB RESULT
-- =====================================================

INSERT INTO clinical.lab_results
(
    medical_record_service_id,
    result_value,
    reference_range,
    conclusion,
    resulted_at,
    recorded_by
)
VALUES
(
    1,
    N'HGB 13.2 g/dL',
    N'12 - 16 g/dL',
    N'Kết quả trong giới hạn bình thường.',
    SYSDATETIME(),
    NULL
);

-- =====================================================
-- CREATE TEST ATTACHMENT
-- =====================================================

USE ClinicManagementSystem;
GO

INSERT INTO clinical.attachments
(
    medical_record_id,
    file_url,
    file_type,
    uploaded_by
)
VALUES
(
    1,
    N'test-files/lab-result-cbc.pdf',
    'application/pdf',
    NULL
);


-- =====================================================
-- CHECK INVOICE CONSTRAINTS
-- =====================================================

USE ClinicManagementSystem;
GO

SELECT
    cc.name AS constraint_name,
    cc.definition
FROM sys.check_constraints cc
INNER JOIN sys.tables t
    ON cc.parent_object_id = t.object_id
INNER JOIN sys.schemas s
    ON t.schema_id = s.schema_id
WHERE t.name = 'invoices';

-- =====================================================
-- CHECK PAYMENT CONSTRAINTS
-- =====================================================

SELECT
    cc.name AS constraint_name,
    cc.definition
FROM sys.check_constraints cc
INNER JOIN sys.tables t
    ON cc.parent_object_id = t.object_id
INNER JOIN sys.schemas s
    ON t.schema_id = s.schema_id
WHERE t.name = 'payments';

SELECT DISTINCT status
FROM billing.invoices
ORDER BY status;

SELECT DISTINCT method
FROM billing.payments
ORDER BY method;

USE ClinicManagementSystem;
GO

-- =====================================================
-- TEST DATA - PATIENT INVOICE
-- =====================================================

DECLARE @PatientId INT = 5;
DECLARE @AppointmentId INT = 2;
DECLARE @MedicalRecordId INT = 1;

DECLARE @MedicineId INT = 1;
DECLARE @MedicalRecordServiceId INT = 1;

DECLARE @InvoiceId INT;
DECLARE @InvoiceNo VARCHAR(30) = 'INV-TEST-0001';

DECLARE @PatientName NVARCHAR(255);

SELECT @PatientName = full_name
FROM scheduling.patients
WHERE id = @PatientId;


-- =====================================================
-- CREATE INVOICE
-- =====================================================

IF NOT EXISTS
(
    SELECT 1
    FROM billing.invoices
    WHERE invoice_no = @InvoiceNo
)
BEGIN
    INSERT INTO billing.invoices
    (
        patient_id,
        appointment_id,
        medical_record_id,
        patient_name,
        total_amount,
        status,
        created_by,
        created_at,
        invoice_no,
        discount_amount,
        tax_amount,
        paid_amount,
        insurance_amount
    )
    VALUES
    (
        @PatientId,
        @AppointmentId,
        @MedicalRecordId,
        @PatientName,
        190000,
        1,              -- PartiallyPaid
        NULL,
        SYSUTCDATETIME(),
        @InvoiceNo,
        0,
        0,
        100000,
        0
    );
END;


-- =====================================================
-- GET INVOICE ID
-- =====================================================

SELECT @InvoiceId = id
FROM billing.invoices
WHERE invoice_no = @InvoiceNo;


-- =====================================================
-- CREATE SERVICE INVOICE ITEM
-- =====================================================

IF NOT EXISTS
(
    SELECT 1
    FROM billing.invoice_items
    WHERE invoice_id = @InvoiceId
      AND medical_record_service_id = @MedicalRecordServiceId
)
BEGIN
    INSERT INTO billing.invoice_items
    (
        invoice_id,
        medicine_id,
        description,
        quantity,
        unit_price,
        amount,
        medical_record_service_id,
        discount_amount
    )
    VALUES
    (
        @InvoiceId,
        NULL,
        N'Xét nghiệm công thức máu',
        1,
        150000,
        150000,
        @MedicalRecordServiceId,
        0
    );
END;


-- =====================================================
-- CREATE MEDICINE INVOICE ITEM
-- =====================================================

IF NOT EXISTS
(
    SELECT 1
    FROM billing.invoice_items
    WHERE invoice_id = @InvoiceId
      AND medicine_id = @MedicineId
)
BEGIN
    INSERT INTO billing.invoice_items
    (
        invoice_id,
        medicine_id,
        description,
        quantity,
        unit_price,
        amount,
        medical_record_service_id,
        discount_amount
    )
    VALUES
    (
        @InvoiceId,
        @MedicineId,
        N'Paracetamol 500mg',
        20,
        2000,
        40000,
        NULL,
        0
    );
END;


-- =====================================================
-- CREATE PAYMENT
-- =====================================================

IF NOT EXISTS
(
    SELECT 1
    FROM billing.payments
    WHERE invoice_id = @InvoiceId
      AND reference_code = 'BANK-TEST-001'
)
BEGIN
    INSERT INTO billing.payments
    (
        invoice_id,
        amount,
        method,
        paid_at,
        note,
        reference_code,
        received_by,
        is_refund
    )
    VALUES
    (
        @InvoiceId,
        100000,
        2,              -- BankTransfer
        SYSUTCDATETIME(),
        N'Thanh toán thử nghiệm',
        'BANK-TEST-001',
        NULL,
        0
    );
END;


-- =====================================================
-- CHECK RESULT
-- =====================================================

SELECT *
FROM billing.invoices
WHERE id = @InvoiceId;

SELECT *
FROM billing.invoice_items
WHERE invoice_id = @InvoiceId;

SELECT *
FROM billing.payments
WHERE invoice_id = @InvoiceId;


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
ORDER BY u.id;

---------------------------
-- test checkin appointment
---------------------------
DECLARE @UserId INT = 1;

DECLARE @ReceptionistRoleId INT =
(
    SELECT id
    FROM auth.roles
    WHERE name = 'Receptionist'
);

IF @ReceptionistRoleId IS NULL
BEGIN
    THROW 50000, 'Receptionist role not found.', 1;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM auth.user_roles
    WHERE user_id = @UserId
      AND role_id = @ReceptionistRoleId
)
BEGIN
    INSERT INTO auth.user_roles
    (
        user_id,
        role_id,
        assigned_at
    )
    VALUES
    (
        @UserId,
        @ReceptionistRoleId,
        SYSUTCDATETIME()
    );
END;

-- kiem tra sau khi doi user 1 từ patient --> receptionist
SELECT
    u.id,
    u.username,
    r.name AS role_name
FROM auth.users u
JOIN auth.user_roles ur
    ON ur.user_id = u.id
JOIN auth.roles r
    ON r.id = ur.role_id
WHERE u.id = 1;


-- user dang co 2 role: patient va receptionist, xoa role patient
DECLARE @UserId INT = 1;

DECLARE @PatientRoleId INT =
(
    SELECT id
    FROM auth.roles
    WHERE name = 'Patient'
);

DECLARE @ReceptionistRoleId INT =
(
    SELECT id
    FROM auth.roles
    WHERE name = 'Receptionist'
);


-- =====================================================
-- REMOVE PATIENT ROLE
-- =====================================================

DELETE FROM auth.user_roles
WHERE user_id = @UserId
  AND role_id = @PatientRoleId;


-- =====================================================
-- ADD RECEPTIONIST ROLE
-- =====================================================

IF NOT EXISTS
(
    SELECT 1
    FROM auth.user_roles
    WHERE user_id = @UserId
      AND role_id = @ReceptionistRoleId
)
BEGIN
    INSERT INTO auth.user_roles
    (
        user_id,
        role_id,
        assigned_at
    )
    VALUES
    (
        @UserId,
        @ReceptionistRoleId,
        SYSUTCDATETIME()
    );
END;

-- seeding to test vc 09, dat lich qua receptionist
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
    5,
    2,
    '08:00',
    '17:00',
    30,
    1,
    '2026-09-26',
    NULL,
    '12:00',
    '13:30'
);