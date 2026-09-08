-- ClinicManagementSystem - 003_UpdateDatabaseV1.sql
-- 19 bang goc -> 29 bang theo pham vi da chot.
-- Nguon: DBML goc + muc 3, 4, 7, 9 trong ban review cua thay.
-- Khong them permissions, role_permissions, clinic_settings.
-- Neu da chay 003 ban 28 bang, dung 004_AddNotifications.sql thay vi chay lai 003.
-- Khong tao database moi, khong DROP TABLE, khong seed du lieu.
--
-- CAC QUYET DINH TRIEN KHAI CAN PHAN BIET VOI NOI DUNG GOC:
-- * Giu 1 medical_record -> toi da 1 prescription.
-- * Giu slot co dinh tren UI; backend van phai kiem tra overlap trong
--   transaction. Unique (doctor_id,start_time) KHONG tu chan overlap.
-- * Unique schedule co them effective_from de cho phep luu nhieu phien
--   ban lich theo thoi gian. Can kiem tra overlap cac giai doan hieu luc.
-- * Invoice status: 0 Unpaid, 1 PartiallyPaid, 2 Paid, 3 Cancelled.
-- * Prescription status: 0 Draft, 1 Finalized, 2 Dispensed, 3 Cancelled.
-- * Medical record status: 0 Draft, 1 Finalized.
-- * audit_logs va insurance_policies: bo cot toi thieu do minh de xuat,
--   vi tai lieu chua cung cap DDL chi tiet cho hai bang nay.
-- * Phi kham duoc tinh thanh mot dong chi dinh dich vu "Kham" khi ket
--   chuyen hoa don. invoice_items khong tro thang vao danh muc services.
-- * So du kho: batches la nguon su that; medicines.stock_quantity la
--   tong hop. Moi thao tac kho phai cap nhat lo, tong va but toan trong
--   cung transaction. Script nay chua thay the nghiep vu phat thuoc.
-- * Thanh toan: refunds la but toan am, khong sua/xoa dong cu.
--
-- AN TOAN DU LIEU:
-- Script dung tren database da co 19 bang cua 002. Neu co ton kho cu
-- hoac invoice_items dich vu cu, script DUNG LAI de tranh mat du lieu.
-- Cac thay doi nam trong mot transaction; loi -> ROLLBACK.
-- Database-level extended property ghi phien ban, KHONG tao bang thu 29.

USE [ClinicManagementSystem];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'ClinicManagementSystem'
    THROW 50000, N'Sai database. Chi duoc chay tren ClinicManagementSystem.', 1;

DECLARE @Expected TABLE (
    schema_name SYSNAME NOT NULL,
    table_name SYSNAME NOT NULL,
    is_original BIT NOT NULL,
    PRIMARY KEY (schema_name, table_name)
);

INSERT INTO @Expected (schema_name, table_name, is_original)
VALUES
(N'auth',N'users',1),
(N'auth',N'roles',1),
(N'auth',N'user_roles',1),
(N'auth',N'refresh_tokens',1),
(N'scheduling',N'specialties',1),
(N'scheduling',N'doctors',1),
(N'scheduling',N'patients',1),
(N'scheduling',N'doctor_schedules',1),
(N'scheduling',N'doctor_time_off',1),
(N'scheduling',N'appointments',1),
(N'scheduling',N'appointment_status_history',1),
(N'clinical',N'medical_records',1),
(N'clinical',N'medicines',1),
(N'clinical',N'prescriptions',1),
(N'clinical',N'prescription_items',1),
(N'billing',N'services',1),
(N'billing',N'invoices',1),
(N'billing',N'invoice_items',1),
(N'billing',N'payments',1),
(N'auth',N'audit_logs',0),
(N'auth',N'notifications',0),
(N'clinical',N'medical_record_services',0),
(N'clinical',N'lab_results',0),
(N'clinical',N'attachments',0),
(N'clinical',N'medicine_batches',0),
(N'clinical',N'medicine_stock_transactions',0),
(N'clinical',N'patient_vitals',0),
(N'clinical',N'patient_allergies',0),
(N'billing',N'insurance_policies',0);

IF EXISTS (
    SELECT 1 FROM sys.extended_properties
    WHERE class = 0 AND name = N'ClinicSchemaVersion'
      AND CONVERT(NVARCHAR(100), value) = N'003'
)
BEGIN
    PRINT N'Migration 003 da duoc ap dung. Khong tao lai bang.';
END
ELSE
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS (
            SELECT 1 FROM @Expected AS e
            WHERE e.is_original = 1
              AND NOT EXISTS (
                  SELECT 1 FROM sys.tables AS t
                  JOIN sys.schemas AS s ON s.schema_id = t.schema_id
                  WHERE s.name = e.schema_name AND t.name = e.table_name
              )
        )
            THROW 50001, N'Thieu bang goc. Hay chay 002_CreateTables.sql truoc.', 1;

        IF EXISTS (
            SELECT 1 FROM @Expected AS e
            JOIN sys.schemas AS s ON s.name = e.schema_name
            JOIN sys.tables AS t ON t.schema_id = s.schema_id AND t.name = e.table_name
            WHERE e.is_original = 0
        )
            THROW 50002, N'Da co mot phan bang moi. Khong chay de tranh migration dang do.', 1;

        IF EXISTS (
            SELECT 1 FROM sys.tables AS t
            JOIN sys.schemas AS s ON s.schema_id = t.schema_id
            WHERE s.name IN (N'auth',N'scheduling',N'clinical',N'billing')
              AND NOT EXISTS (
                  SELECT 1 FROM @Expected AS e
                  WHERE e.schema_name = s.name AND e.table_name = t.name
              )
        )
            THROW 50003, N'Co bang ngoai danh sach 28. Hay kiem tra truoc khi migration.', 1;

        IF COL_LENGTH(N'clinical.medicines',N'expiry_date') IS NULL
           OR COL_LENGTH(N'billing.invoice_items',N'service_id') IS NULL
           OR COL_LENGTH(N'scheduling.patients',N'user_id') IS NULL
            THROW 50004, N'Cau truc bang goc khong khop 002. Dung lai de kiem tra.', 1;

        IF EXISTS (
            SELECT 1 FROM clinical.medicines
            WHERE stock_quantity <> 0 OR expiry_date IS NOT NULL
        )
            THROW 50005, N'Co ton kho/han dung cu. Can chuyen sang lo truoc khi bo expiry_date.', 1;

        IF EXISTS (SELECT 1 FROM billing.invoice_items WHERE service_id IS NOT NULL)
            THROW 50006, N'Co dong dich vu cu. Can map sang medical_record_services truoc khi doi FK.', 1;

        IF EXISTS (
            SELECT 1 FROM scheduling.doctor_schedules
            GROUP BY doctor_id, day_of_week, start_time HAVING COUNT(*) > 1
        )
            THROW 50007, N'Lich bac si co trung bo khoa goc. Can kiem tra truoc khi them unique.', 1;

        IF EXISTS (SELECT 1 FROM billing.payments WHERE amount <= 0)
            THROW 50008, N'Payments cu co so tien khong duong. Can doi soat truoc migration.', 1;

        -- ========================================================
        -- 1. AUTH: bo sung field vao 4 bang goc
        -- ========================================================
        ALTER TABLE auth.users ADD
            last_login_at DATETIME2 NULL,
            failed_login_count INT NOT NULL CONSTRAINT DF_auth_users_failed_login_count DEFAULT (0),
            lockout_end DATETIME2 NULL,
            email_confirmed BIT NOT NULL CONSTRAINT DF_auth_users_email_confirmed DEFAULT (0),
            avatar_url VARCHAR(500) NULL,
            is_deleted BIT NOT NULL CONSTRAINT DF_auth_users_is_deleted DEFAULT (0),
            deleted_at DATETIME2 NULL;

        ALTER TABLE auth.users ADD CONSTRAINT CK_auth_users_failed_login_count
            CHECK (failed_login_count >= 0);

        ALTER TABLE auth.roles ADD
            description NVARCHAR(255) NULL,
            is_system BIT NOT NULL CONSTRAINT DF_auth_roles_is_system DEFAULT (0);

        ALTER TABLE auth.user_roles ADD
            assigned_at DATETIME2 NOT NULL CONSTRAINT DF_auth_user_roles_assigned_at DEFAULT (SYSUTCDATETIME()),
            assigned_by INT NULL;

        ALTER TABLE auth.user_roles ADD CONSTRAINT FK_auth_user_roles_assigned_by
            FOREIGN KEY (assigned_by) REFERENCES auth.users(id);

        ALTER TABLE auth.refresh_tokens ADD
            device_info NVARCHAR(500) NULL,
            ip_address VARCHAR(45) NULL,
            replaced_by_token_hash CHAR(64) NULL;

        -- Audit: schema toi thieu de truy vet ai, hanh dong gi, tren ho so nao.
        CREATE TABLE auth.audit_logs (
            id INT IDENTITY(1,1) NOT NULL,
            user_id INT NULL,
            action VARCHAR(100) NOT NULL,
            entity_name VARCHAR(128) NOT NULL,
            entity_id INT NULL,
            details NVARCHAR(MAX) NULL,
            ip_address VARCHAR(45) NULL,
            succeeded BIT NOT NULL CONSTRAINT DF_auth_audit_logs_succeeded DEFAULT (1),
            occurred_at DATETIME2 NOT NULL CONSTRAINT DF_auth_audit_logs_occurred_at DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT PK_auth_audit_logs PRIMARY KEY (id),
            CONSTRAINT FK_auth_audit_logs_users FOREIGN KEY (user_id)
                REFERENCES auth.users(id)
        );

        CREATE INDEX IX_auth_audit_logs_entity
            ON auth.audit_logs(entity_name,entity_id,occurred_at);

        CREATE INDEX IX_auth_audit_logs_user_time
            ON auth.audit_logs(user_id,occurred_at);

        -- Notifications: de xuat cot toi thieu cho nhac lich/lich su gui.
        -- Tai lieu thay chua cung cap DDL chi tiet cho bang nay.
        -- Khong gui SMS/Email trong migration; chi tao noi luu du lieu.
        CREATE TABLE auth.notifications (
            id INT IDENTITY(1,1) NOT NULL,
            appointment_id INT NOT NULL,
            user_id INT NULL,
            channel VARCHAR(20) NOT NULL,
            recipient NVARCHAR(255) NULL,
            title NVARCHAR(255) NOT NULL,
            content NVARCHAR(2000) NOT NULL,
            status TINYINT NOT NULL CONSTRAINT DF_auth_notifications_status DEFAULT (0),
            scheduled_at DATETIME2 NULL,
            sent_at DATETIME2 NULL,
            error_message NVARCHAR(1000) NULL,
            created_at DATETIME2 NOT NULL CONSTRAINT DF_auth_notifications_created_at DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT PK_auth_notifications PRIMARY KEY (id),
            CONSTRAINT FK_auth_notifications_appointments FOREIGN KEY (appointment_id)
                REFERENCES scheduling.appointments(id),
            CONSTRAINT FK_auth_notifications_users FOREIGN KEY (user_id)
                REFERENCES auth.users(id),
            CONSTRAINT CK_auth_notifications_channel CHECK (channel IN ('Email','SMS','Zalo','InApp')),
            CONSTRAINT CK_auth_notifications_status CHECK (status BETWEEN 0 AND 3)
        );

        CREATE INDEX IX_auth_notifications_appointment
            ON auth.notifications(appointment_id);

        CREATE INDEX IX_auth_notifications_status_scheduled
            ON auth.notifications(status,scheduled_at);

        -- ========================================================
        -- 2. SCHEDULING
        -- ========================================================
        ALTER TABLE scheduling.specialties ADD code VARCHAR(20) NULL;
        CREATE UNIQUE INDEX UX_scheduling_specialties_code
            ON scheduling.specialties(code) WHERE code IS NOT NULL;

        ALTER TABLE scheduling.doctors ADD
            consultation_fee DECIMAL(12,2) NOT NULL CONSTRAINT DF_scheduling_doctors_consultation_fee DEFAULT (0),
            bio NVARCHAR(2000) NULL,
            avatar_url VARCHAR(500) NULL,
            experience_years INT NULL,
            max_patients_per_day INT NULL;

        ALTER TABLE scheduling.doctors ADD
            CONSTRAINT CK_scheduling_doctors_consultation_fee CHECK (consultation_fee >= 0),
            CONSTRAINT CK_scheduling_doctors_experience CHECK (experience_years IS NULL OR experience_years >= 0),
            CONSTRAINT CK_scheduling_doctors_max_patients CHECK (max_patients_per_day IS NULL OR max_patients_per_day > 0);

        -- Ma nghiep vu: backfill theo ID de giu du lieu cu va dam bao unique.
        -- Khi tao ban ghi moi, Application phai sinh code.
        ALTER TABLE scheduling.patients ADD
            patient_code VARCHAR(20) NULL,
            national_id VARCHAR(30) NULL,
            insurance_number VARCHAR(50) NULL,
            blood_type VARCHAR(10) NULL,
            emergency_contact_name NVARCHAR(255) NULL,
            emergency_contact_phone VARCHAR(20) NULL;

        UPDATE scheduling.patients
        SET patient_code = 'BN' + RIGHT(REPLICATE('0',10) + CONVERT(VARCHAR(20),id),10)
        WHERE patient_code IS NULL;

        ALTER TABLE scheduling.patients ALTER COLUMN patient_code VARCHAR(20) NOT NULL;
        CREATE UNIQUE INDEX UX_scheduling_patients_code
            ON scheduling.patients(patient_code);

        ALTER TABLE scheduling.patients ADD CONSTRAINT CK_scheduling_patients_gender
            CHECK (gender IS NULL OR gender BETWEEN 0 AND 2);

        -- Lich cu duoc coi la co hieu luc tu 1900-01-01.
        -- Khong de default nay cho cac lich moi: Application phai chon ngay.
        ALTER TABLE scheduling.doctor_schedules ADD
            effective_from DATE NULL,
            effective_to DATE NULL,
            break_start TIME NULL,
            break_end TIME NULL;

        UPDATE scheduling.doctor_schedules
        SET effective_from = CONVERT(DATE,'19000101',112)
        WHERE effective_from IS NULL;

        ALTER TABLE scheduling.doctor_schedules ALTER COLUMN effective_from DATE NOT NULL;

        ALTER TABLE scheduling.doctor_schedules ADD
            CONSTRAINT CK_scheduling_schedules_day CHECK (day_of_week BETWEEN 0 AND 6),
            CONSTRAINT CK_scheduling_schedules_time CHECK (start_time < end_time),
            CONSTRAINT CK_scheduling_schedules_slot CHECK (slot_minutes > 0),
            CONSTRAINT CK_scheduling_schedules_effective CHECK (effective_to IS NULL OR effective_to >= effective_from),
            CONSTRAINT CK_scheduling_schedules_break CHECK (
                (break_start IS NULL AND break_end IS NULL)
                OR (break_start IS NOT NULL AND break_end IS NOT NULL
                    AND start_time <= break_start AND break_start < break_end
                    AND break_end <= end_time)
            );

        -- Dieu chinh co chu dich: them effective_from vao unique cua thay
        -- de luu duoc hai phien ban lich co cung gio bat dau.
        CREATE UNIQUE INDEX UX_scheduling_schedules_version
            ON scheduling.doctor_schedules(doctor_id,day_of_week,start_time,effective_from);

        ALTER TABLE scheduling.doctor_time_off ADD
            type TINYINT NULL,
            is_full_day BIT NOT NULL CONSTRAINT DF_scheduling_time_off_full_day DEFAULT (0),
            approved_by INT NULL;

        ALTER TABLE scheduling.doctor_time_off ADD
            CONSTRAINT FK_scheduling_time_off_approved_by FOREIGN KEY (approved_by) REFERENCES auth.users(id),
            CONSTRAINT CK_scheduling_time_off_time CHECK (start_at < end_at);

        ALTER TABLE scheduling.appointments ADD
            appointment_code VARCHAR(20) NULL,
            queue_number INT NULL,
            checked_in_at DATETIME2 NULL,
            completed_at DATETIME2 NULL,
            cancel_reason NVARCHAR(500) NULL,
            source TINYINT NOT NULL CONSTRAINT DF_scheduling_appointments_source DEFAULT (0),
            fee_snapshot DECIMAL(12,2) NOT NULL CONSTRAINT DF_scheduling_appointments_fee_snapshot DEFAULT (0);

        UPDATE scheduling.appointments
        SET appointment_code = 'AP' + RIGHT(REPLICATE('0',10) + CONVERT(VARCHAR(20),id),10)
        WHERE appointment_code IS NULL;

        ALTER TABLE scheduling.appointments ALTER COLUMN appointment_code VARCHAR(20) NOT NULL;
        CREATE UNIQUE INDEX UX_scheduling_appointments_code
            ON scheduling.appointments(appointment_code);

        ALTER TABLE scheduling.appointments ADD
            CONSTRAINT CK_scheduling_appointments_time CHECK (start_time < end_time),
            CONSTRAINT CK_scheduling_appointments_status CHECK (status BETWEEN 0 AND 5),
            CONSTRAINT CK_scheduling_appointments_source CHECK (source BETWEEN 0 AND 2),
            CONSTRAINT CK_scheduling_appointments_queue CHECK (queue_number IS NULL OR queue_number > 0),
            CONSTRAINT CK_scheduling_appointments_fee CHECK (fee_snapshot >= 0);

        ALTER TABLE scheduling.appointment_status_history ADD
            CONSTRAINT CK_scheduling_history_from CHECK (from_status IS NULL OR from_status BETWEEN 0 AND 5),
            CONSTRAINT CK_scheduling_history_to CHECK (to_status BETWEEN 0 AND 5);

        -- ========================================================
        -- 3. CLINICAL: benh an, don thuoc, chi dinh, ket qua, kho
        -- ========================================================
        ALTER TABLE clinical.medical_records ADD
            icd10_code VARCHAR(10) NULL,
            treatment_plan NVARCHAR(4000) NULL,
            follow_up_date DATE NULL,
            status TINYINT NOT NULL CONSTRAINT DF_clinical_records_status DEFAULT (0),
            finalized_at DATETIME2 NULL;

        ALTER TABLE clinical.medical_records ADD
            CONSTRAINT CK_clinical_records_status CHECK (status BETWEEN 0 AND 1),
            CONSTRAINT CK_clinical_records_finalized CHECK (
                (status = 0 AND finalized_at IS NULL)
                OR (status = 1 AND finalized_at IS NOT NULL)
            );

        ALTER TABLE clinical.medicines ADD
            code VARCHAR(30) NULL,
            active_ingredient NVARCHAR(255) NULL,
            concentration NVARCHAR(100) NULL,
            cost_price DECIMAL(12,2) NULL,
            min_stock INT NOT NULL CONSTRAINT DF_clinical_medicines_min_stock DEFAULT (0);

        CREATE UNIQUE INDEX UX_clinical_medicines_code
            ON clinical.medicines(code) WHERE code IS NOT NULL;

        ALTER TABLE clinical.medicines ADD
            CONSTRAINT CK_clinical_medicines_price CHECK (price >= 0),
            CONSTRAINT CK_clinical_medicines_cost CHECK (cost_price IS NULL OR cost_price >= 0),
            CONSTRAINT CK_clinical_medicines_stock CHECK (stock_quantity >= 0 AND min_stock >= 0);

        -- Han dung chuyen sang medicine_batches.
        ALTER TABLE clinical.medicines DROP COLUMN expiry_date;

        ALTER TABLE clinical.prescriptions ADD
            doctor_id INT NULL,
            status TINYINT NOT NULL CONSTRAINT DF_clinical_prescriptions_status DEFAULT (0),
            dispensed_at DATETIME2 NULL,
            dispensed_by INT NULL;

        UPDATE p SET doctor_id = mr.doctor_id
        FROM clinical.prescriptions AS p
        JOIN clinical.medical_records AS mr ON mr.id = p.medical_record_id;

        ALTER TABLE clinical.prescriptions ALTER COLUMN doctor_id INT NOT NULL;

        ALTER TABLE clinical.prescriptions ADD
            CONSTRAINT FK_clinical_prescriptions_doctors FOREIGN KEY (doctor_id) REFERENCES scheduling.doctors(id),
            CONSTRAINT FK_clinical_prescriptions_dispensed_by FOREIGN KEY (dispensed_by) REFERENCES auth.users(id),
            CONSTRAINT CK_clinical_prescriptions_status CHECK (status BETWEEN 0 AND 3),
            CONSTRAINT CK_clinical_prescriptions_dispensed CHECK (
                status <> 2 OR (dispensed_at IS NOT NULL AND dispensed_by IS NOT NULL)
            );

        ALTER TABLE clinical.prescription_items ADD
            medicine_name_snapshot NVARCHAR(255) NULL,
            unit_price_snapshot DECIMAL(12,2) NULL,
            duration_days INT NULL,
            frequency NVARCHAR(50) NULL;

        UPDATE pi
        SET medicine_name_snapshot = m.name,
            unit_price_snapshot = m.price
        FROM clinical.prescription_items AS pi
        JOIN clinical.medicines AS m ON m.id = pi.medicine_id;

        ALTER TABLE clinical.prescription_items ALTER COLUMN medicine_name_snapshot NVARCHAR(255) NOT NULL;
        ALTER TABLE clinical.prescription_items ALTER COLUMN unit_price_snapshot DECIMAL(12,2) NOT NULL;

        ALTER TABLE clinical.prescription_items ADD
            CONSTRAINT CK_clinical_prescription_items_quantity CHECK (quantity > 0),
            CONSTRAINT CK_clinical_prescription_items_price CHECK (unit_price_snapshot >= 0),
            CONSTRAINT CK_clinical_prescription_items_duration CHECK (duration_days IS NULL OR duration_days > 0);

        -- Chi dinh dich vu: source of truth cho dich vu thuc te.
        CREATE TABLE clinical.medical_record_services (
            id INT IDENTITY(1,1) NOT NULL,
            medical_record_id INT NOT NULL,
            service_id INT NOT NULL,
            quantity INT NOT NULL CONSTRAINT DF_clinical_mrs_quantity DEFAULT (1),
            unit_price_snapshot DECIMAL(12,2) NOT NULL,
            status TINYINT NOT NULL CONSTRAINT DF_clinical_mrs_status DEFAULT (0),
            performed_by INT NULL,
            ordered_at DATETIME2 NOT NULL CONSTRAINT DF_clinical_mrs_ordered_at DEFAULT (SYSUTCDATETIME()),
            completed_at DATETIME2 NULL,
            CONSTRAINT PK_clinical_medical_record_services PRIMARY KEY (id),
            CONSTRAINT FK_clinical_mrs_records FOREIGN KEY (medical_record_id) REFERENCES clinical.medical_records(id),
            CONSTRAINT FK_clinical_mrs_services FOREIGN KEY (service_id) REFERENCES billing.services(id),
            CONSTRAINT FK_clinical_mrs_performed_by FOREIGN KEY (performed_by) REFERENCES auth.users(id),
            CONSTRAINT CK_clinical_mrs_quantity CHECK (quantity > 0),
            CONSTRAINT CK_clinical_mrs_price CHECK (unit_price_snapshot >= 0),
            CONSTRAINT CK_clinical_mrs_status CHECK (status BETWEEN 0 AND 3)
        );

        CREATE INDEX IX_clinical_mrs_record_status
            ON clinical.medical_record_services(medical_record_id,status);

        CREATE TABLE clinical.lab_results (
            id INT IDENTITY(1,1) NOT NULL,
            medical_record_service_id INT NOT NULL,
            result_value NVARCHAR(4000) NULL,
            reference_range NVARCHAR(500) NULL,
            conclusion NVARCHAR(4000) NULL,
            resulted_at DATETIME2 NULL,
            recorded_by INT NULL,
            created_at DATETIME2 NOT NULL CONSTRAINT DF_clinical_lab_results_created_at DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT PK_clinical_lab_results PRIMARY KEY (id),
            CONSTRAINT UQ_clinical_lab_results_order UNIQUE (medical_record_service_id),
            CONSTRAINT FK_clinical_lab_results_order FOREIGN KEY (medical_record_service_id)
                REFERENCES clinical.medical_record_services(id),
            CONSTRAINT FK_clinical_lab_results_users FOREIGN KEY (recorded_by) REFERENCES auth.users(id)
        );

        CREATE TABLE clinical.attachments (
            id INT IDENTITY(1,1) NOT NULL,
            medical_record_id INT NOT NULL,
            file_url NVARCHAR(1000) NOT NULL,
            file_type VARCHAR(100) NULL,
            uploaded_at DATETIME2 NOT NULL CONSTRAINT DF_clinical_attachments_uploaded_at DEFAULT (SYSUTCDATETIME()),
            uploaded_by INT NULL,
            CONSTRAINT PK_clinical_attachments PRIMARY KEY (id),
            CONSTRAINT FK_clinical_attachments_records FOREIGN KEY (medical_record_id)
                REFERENCES clinical.medical_records(id),
            CONSTRAINT FK_clinical_attachments_users FOREIGN KEY (uploaded_by)
                REFERENCES auth.users(id)
        );

        CREATE INDEX IX_clinical_attachments_record
            ON clinical.attachments(medical_record_id);

        CREATE TABLE clinical.patient_vitals (
            id INT IDENTITY(1,1) NOT NULL,
            medical_record_id INT NOT NULL,
            temperature DECIMAL(4,1) NULL,
            pulse INT NULL,
            blood_pressure VARCHAR(20) NULL,
            weight DECIMAL(6,2) NULL,
            height DECIMAL(6,2) NULL,
            CONSTRAINT PK_clinical_patient_vitals PRIMARY KEY (id),
            CONSTRAINT UQ_clinical_patient_vitals_record UNIQUE (medical_record_id),
            CONSTRAINT FK_clinical_patient_vitals_records FOREIGN KEY (medical_record_id)
                REFERENCES clinical.medical_records(id),
            CONSTRAINT CK_clinical_patient_vitals_values CHECK (
                (temperature IS NULL OR temperature >= 0)
                AND (pulse IS NULL OR pulse > 0)
                AND (weight IS NULL OR weight > 0)
                AND (height IS NULL OR height > 0)
            )
        );

        CREATE TABLE clinical.patient_allergies (
            id INT IDENTITY(1,1) NOT NULL,
            patient_id INT NOT NULL,
            allergen NVARCHAR(255) NOT NULL,
            severity TINYINT NULL,
            note NVARCHAR(500) NULL,
            CONSTRAINT PK_clinical_patient_allergies PRIMARY KEY (id),
            CONSTRAINT FK_clinical_patient_allergies_patients FOREIGN KEY (patient_id)
                REFERENCES scheduling.patients(id)
        );

        CREATE INDEX IX_clinical_patient_allergies_patient
            ON clinical.patient_allergies(patient_id);

        CREATE TABLE clinical.medicine_batches (
            id INT IDENTITY(1,1) NOT NULL,
            medicine_id INT NOT NULL,
            batch_no VARCHAR(50) NOT NULL,
            expiry_date DATE NOT NULL,
            quantity INT NOT NULL CONSTRAINT DF_clinical_batches_quantity DEFAULT (0),
            import_price DECIMAL(12,2) NULL,
            CONSTRAINT PK_clinical_medicine_batches PRIMARY KEY (id),
            CONSTRAINT UQ_clinical_batches_medicine_no UNIQUE (medicine_id,batch_no),
            CONSTRAINT UQ_clinical_batches_id_medicine UNIQUE (id,medicine_id),
            CONSTRAINT FK_clinical_batches_medicines FOREIGN KEY (medicine_id) REFERENCES clinical.medicines(id),
            CONSTRAINT CK_clinical_batches_quantity CHECK (quantity >= 0),
            CONSTRAINT CK_clinical_batches_price CHECK (import_price IS NULL OR import_price >= 0)
        );

        CREATE INDEX IX_clinical_batches_expiry ON clinical.medicine_batches(expiry_date);

        -- before/after + reversal_of_transaction_id bo sung de dap ung
        -- checklist F2/F3. type: 0 IN, 1 OUT, 2 ADJUST.
        -- ADJUST co the tang hoac giam; quantity luon la tri tuyet doi.
        CREATE TABLE clinical.medicine_stock_transactions (
            id INT IDENTITY(1,1) NOT NULL,
            medicine_id INT NOT NULL,
            batch_id INT NOT NULL,
            type TINYINT NOT NULL,
            quantity INT NOT NULL,
            quantity_before INT NOT NULL,
            quantity_after INT NOT NULL,
            prescription_id INT NULL,
            reversal_of_transaction_id INT NULL,
            created_by INT NOT NULL,
            created_at DATETIME2 NOT NULL CONSTRAINT DF_clinical_stock_tx_created_at DEFAULT (SYSUTCDATETIME()),
            note NVARCHAR(500) NULL,
            CONSTRAINT PK_clinical_medicine_stock_transactions PRIMARY KEY (id),
            CONSTRAINT FK_clinical_stock_tx_medicines FOREIGN KEY (medicine_id) REFERENCES clinical.medicines(id),
            CONSTRAINT FK_clinical_stock_tx_batch_medicine FOREIGN KEY (batch_id,medicine_id)
                REFERENCES clinical.medicine_batches(id,medicine_id),
            CONSTRAINT FK_clinical_stock_tx_prescriptions FOREIGN KEY (prescription_id) REFERENCES clinical.prescriptions(id),
            CONSTRAINT FK_clinical_stock_tx_reversal FOREIGN KEY (reversal_of_transaction_id)
                REFERENCES clinical.medicine_stock_transactions(id),
            CONSTRAINT FK_clinical_stock_tx_users FOREIGN KEY (created_by) REFERENCES auth.users(id),
            CONSTRAINT CK_clinical_stock_tx_quantity CHECK (quantity > 0),
            CONSTRAINT CK_clinical_stock_tx_balances CHECK (quantity_before >= 0 AND quantity_after >= 0),
            CONSTRAINT CK_clinical_stock_tx_type CHECK (
                (type = 0 AND quantity_after = quantity_before + quantity)
                OR (type = 1 AND quantity_before = quantity_after + quantity)
                OR (type = 2 AND (
                    quantity_after = quantity_before + quantity
                    OR quantity_before = quantity_after + quantity
                ))
            )
        );

        CREATE INDEX IX_clinical_stock_tx_batch_time
            ON clinical.medicine_stock_transactions(batch_id,created_at);

        CREATE INDEX IX_clinical_stock_tx_prescription
            ON clinical.medicine_stock_transactions(prescription_id);

        -- ========================================================
        -- 4. BILLING
        -- ========================================================
        ALTER TABLE billing.services ADD
            code VARCHAR(30) NULL,
            specialty_id INT NULL,
            duration_minutes INT NULL;

        CREATE UNIQUE INDEX UX_billing_services_code
            ON billing.services(code) WHERE code IS NOT NULL;

        ALTER TABLE billing.services ADD
            CONSTRAINT FK_billing_services_specialties FOREIGN KEY (specialty_id) REFERENCES scheduling.specialties(id),
            CONSTRAINT CK_billing_services_price CHECK (price >= 0),
            CONSTRAINT CK_billing_services_duration CHECK (duration_minutes IS NULL OR duration_minutes > 0);

        ALTER TABLE billing.invoices ADD
            invoice_no VARCHAR(30) NULL,
            discount_amount DECIMAL(12,2) NOT NULL CONSTRAINT DF_billing_invoices_discount DEFAULT (0),
            tax_amount DECIMAL(12,2) NOT NULL CONSTRAINT DF_billing_invoices_tax DEFAULT (0),
            paid_amount DECIMAL(12,2) NOT NULL CONSTRAINT DF_billing_invoices_paid DEFAULT (0),
            insurance_amount DECIMAL(12,2) NOT NULL CONSTRAINT DF_billing_invoices_insurance DEFAULT (0),
            cancelled_at DATETIME2 NULL,
            cancel_reason NVARCHAR(500) NULL;

        UPDATE billing.invoices
        SET invoice_no = 'INV' + RIGHT(REPLICATE('0',10) + CONVERT(VARCHAR(20),id),10)
        WHERE invoice_no IS NULL;

        ALTER TABLE billing.invoices ALTER COLUMN invoice_no VARCHAR(30) NOT NULL;
        CREATE UNIQUE INDEX UX_billing_invoices_no ON billing.invoices(invoice_no);

        -- Backfill so da thu tu cac payments cu (truoc migration chua co refund).
        UPDATE i SET paid_amount = COALESCE(p.paid,0)
        FROM billing.invoices AS i
        OUTER APPLY (
            SELECT SUM(amount) AS paid
            FROM billing.payments AS p
            WHERE p.invoice_id = i.id
        ) AS p;

        -- Ban goc chua co Cancelled; v1 them 3=Cancelled cho API huy hoa don.
        UPDATE billing.invoices
        SET status = CASE
            WHEN total_amount > 0 AND paid_amount >= total_amount THEN 2
            WHEN paid_amount > 0 THEN 1
            ELSE 0
        END;

        ALTER TABLE billing.invoices ADD
            CONSTRAINT CK_billing_invoices_amounts CHECK (
                total_amount >= 0 AND discount_amount >= 0
                AND tax_amount >= 0 AND paid_amount >= 0
                AND insurance_amount >= 0
                AND discount_amount + insurance_amount <= total_amount + tax_amount
                AND paid_amount <= total_amount + tax_amount - discount_amount - insurance_amount
            ),
            CONSTRAINT CK_billing_invoices_status CHECK (status BETWEEN 0 AND 3),
            CONSTRAINT CK_billing_invoices_cancelled CHECK (
                (status = 3 AND cancelled_at IS NOT NULL)
                OR (status <> 3 AND cancelled_at IS NULL)
            );

        -- Doi FK dich vu: danh muc -> lan chi dinh thuc te.
        -- Preflight da chan moi dong service_id cu de khong mat mapping.
        ALTER TABLE billing.invoice_items
            DROP CONSTRAINT FK_billing_invoice_items_services;

        ALTER TABLE billing.invoice_items
            DROP COLUMN service_id;

        ALTER TABLE billing.invoice_items ADD
            medical_record_service_id INT NULL,
            discount_amount DECIMAL(12,2) NOT NULL CONSTRAINT DF_billing_invoice_items_discount DEFAULT (0);

        ALTER TABLE billing.invoice_items ADD
            CONSTRAINT FK_billing_invoice_items_order FOREIGN KEY (medical_record_service_id)
                REFERENCES clinical.medical_record_services(id);

        -- Backfill snapshot ten neu dong thuoc cu chua co description.
        UPDATE ii SET description = m.name
        FROM billing.invoice_items AS ii
        JOIN clinical.medicines AS m ON m.id = ii.medicine_id
        WHERE ii.description IS NULL;

        ALTER TABLE billing.invoice_items ADD
            CONSTRAINT CK_billing_invoice_items_one_source CHECK (
                (medical_record_service_id IS NOT NULL AND medicine_id IS NULL)
                OR (medical_record_service_id IS NULL AND medicine_id IS NOT NULL)
            ),
            CONSTRAINT CK_billing_invoice_items_amounts CHECK (
                quantity > 0 AND unit_price >= 0 AND discount_amount >= 0
                AND amount >= 0
                AND discount_amount <= quantity * unit_price
                AND amount = quantity * unit_price - discount_amount
            );

        ALTER TABLE billing.payments ADD
            reference_code VARCHAR(100) NULL,
            received_by INT NULL,
            is_refund BIT NOT NULL CONSTRAINT DF_billing_payments_is_refund DEFAULT (0);

        ALTER TABLE billing.payments ADD
            CONSTRAINT FK_billing_payments_received_by FOREIGN KEY (received_by) REFERENCES auth.users(id),
            CONSTRAINT CK_billing_payments_method CHECK (method BETWEEN 0 AND 3),
            CONSTRAINT CK_billing_payments_amount CHECK (
                (is_refund = 0 AND amount > 0)
                OR (is_refund = 1 AND amount < 0)
            );

        -- Insurance policy: de xuat toi thieu theo quan he Patient 1-N Policy.
        -- Chua seed muc huong hay quy tac BHYT thuc te.
        CREATE TABLE billing.insurance_policies (
            id INT IDENTITY(1,1) NOT NULL,
            patient_id INT NOT NULL,
            policy_number VARCHAR(50) NOT NULL,
            provider_name NVARCHAR(255) NULL,
            coverage_percent DECIMAL(5,2) NULL,
            route_type TINYINT NULL,
            valid_from DATE NULL,
            valid_to DATE NULL,
            is_active BIT NOT NULL CONSTRAINT DF_billing_insurance_active DEFAULT (1),
            created_at DATETIME2 NOT NULL CONSTRAINT DF_billing_insurance_created_at DEFAULT (SYSUTCDATETIME()),
            updated_at DATETIME2 NULL,
            CONSTRAINT PK_billing_insurance_policies PRIMARY KEY (id),
            CONSTRAINT FK_billing_insurance_patients FOREIGN KEY (patient_id) REFERENCES scheduling.patients(id),
            CONSTRAINT CK_billing_insurance_coverage CHECK (
                coverage_percent IS NULL OR coverage_percent BETWEEN 0 AND 100
            ),
            CONSTRAINT CK_billing_insurance_dates CHECK (
                valid_from IS NULL OR valid_to IS NULL OR valid_to >= valid_from
            )
        );

        CREATE INDEX IX_billing_insurance_patient
            ON billing.insurance_policies(patient_id);

        -- ========================================================
        -- 5. KHOA SUA BENH AN DA CHOT
        -- ========================================================
        EXEC(N'
CREATE TRIGGER clinical.trg_medical_records_no_edit_finalized
ON clinical.medical_records
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted WHERE status = 1)
        THROW 50020, N''Benh an da Finalized: khong duoc sua/xoa truc tiep.'', 1;
END
');

        -- Luu y: trigger nay bao ve header medical_records.
        -- Application van phai quan ly viec sua prescription/items/orders
        -- trong transaction chot benh an, va ghi audit khi truy cap/sua.

        -- ========================================================
        -- 6. GHI NHAN PHIEN BAN VA KET THUC
        -- ========================================================
        EXEC sys.sp_addextendedproperty
            @name = N'ClinicSchemaVersion', @value = N'003';

        COMMIT TRANSACTION;
        PRINT N'Da nang cap schema tu 19 len 29 bang.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- Kiem tra danh sach 29 bang (khong tinh system tables).
SELECT s.name AS schema_name, t.name AS table_name
FROM sys.tables AS t
JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE s.name IN (N'auth',N'scheduling',N'clinical',N'billing')
ORDER BY s.name,t.name;

SELECT COUNT(*) AS total_tables
FROM sys.tables AS t
JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE s.name IN (N'auth',N'scheduling',N'clinical',N'billing');
GO
