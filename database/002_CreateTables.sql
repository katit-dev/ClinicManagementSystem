-- ClinicManagementSystem - 002_CreateTables.sql
-- Baseline: 19 bang goc theo DBML ban da gui.
-- Chua ap dung cac bo sung P0/P1/P2 cua thay.
-- Khong xoa bang, khong xoa du lieu, khong seed du lieu.
-- Can chay 001_InitDatabase.sql truoc.

USE [ClinicManagementSystem];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'ClinicManagementSystem'
    THROW 50000, N'Khong dung database ClinicManagementSystem.', 1;

IF SCHEMA_ID(N'auth') IS NULL
   OR SCHEMA_ID(N'scheduling') IS NULL
   OR SCHEMA_ID(N'clinical') IS NULL
   OR SCHEMA_ID(N'billing') IS NULL
    THROW 50001, N'Chua du 4 schema. Hay chay 001_InitDatabase.sql truoc.', 1;

-- Chi chay tren baseline chua co 19 bang nay.
-- Neu da co bang, dung lai de tranh ghi de / tao lai cau truc.
IF EXISTS (
    SELECT 1
    FROM sys.tables AS t
    INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
    INNER JOIN (VALUES
        (N'auth', N'users'),
        (N'auth', N'roles'),
        (N'auth', N'user_roles'),
        (N'auth', N'refresh_tokens'),
        (N'scheduling', N'specialties'),
        (N'scheduling', N'doctors'),
        (N'scheduling', N'patients'),
        (N'scheduling', N'doctor_schedules'),
        (N'scheduling', N'doctor_time_off'),
        (N'scheduling', N'appointments'),
        (N'scheduling', N'appointment_status_history'),
        (N'clinical', N'medical_records'),
        (N'clinical', N'medicines'),
        (N'clinical', N'prescriptions'),
        (N'clinical', N'prescription_items'),
        (N'billing', N'services'),
        (N'billing', N'invoices'),
        (N'billing', N'invoice_items'),
        (N'billing', N'payments')
    ) AS expected(schema_name, table_name)
        ON expected.schema_name = s.name AND expected.table_name = t.name
)
    THROW 50002, N'Mot hoac nhieu bang goc da ton tai. Script khong tao lai de bao ve du lieu.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    -- ============================================================
    -- AUTH (4 bang)
    -- ============================================================

    CREATE TABLE [auth].[users] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [username] VARCHAR(100) NOT NULL,
        [password_hash] VARCHAR(255) NOT NULL,
        [email] VARCHAR(255) NULL,
        [full_name] NVARCHAR(255) NULL,
        [phone] VARCHAR(20) NULL,
        [is_active] BIT NOT NULL CONSTRAINT [DF_auth_users_is_active] DEFAULT (1),
        [created_at] DATETIME2 NOT NULL CONSTRAINT [DF_auth_users_created_at] DEFAULT (SYSUTCDATETIME()),
        [updated_at] DATETIME2 NULL,
        CONSTRAINT [PK_auth_users] PRIMARY KEY ([id]),
        CONSTRAINT [UQ_auth_users_username] UNIQUE ([username])
    );

    CREATE TABLE [auth].[roles] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [name] VARCHAR(50) NOT NULL,
        CONSTRAINT [PK_auth_roles] PRIMARY KEY ([id]),
        CONSTRAINT [UQ_auth_roles_name] UNIQUE ([name])
    );

    CREATE TABLE [auth].[user_roles] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [user_id] INT NOT NULL,
        [role_id] INT NOT NULL,
        CONSTRAINT [PK_auth_user_roles] PRIMARY KEY ([id]),
        CONSTRAINT [UQ_user_roles] UNIQUE ([user_id], [role_id]),
        CONSTRAINT [FK_auth_user_roles_users] FOREIGN KEY ([user_id])
            REFERENCES [auth].[users]([id]),
        CONSTRAINT [FK_auth_user_roles_roles] FOREIGN KEY ([role_id])
            REFERENCES [auth].[roles]([id])
    );

    CREATE TABLE [auth].[refresh_tokens] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [user_id] INT NOT NULL,
        [token_hash] CHAR(64) NOT NULL,
        [expires_at] DATETIME2 NOT NULL,
        [revoked_at] DATETIME2 NULL,
        [created_at] DATETIME2 NOT NULL CONSTRAINT [DF_auth_refresh_tokens_created_at] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_auth_refresh_tokens] PRIMARY KEY ([id]),
        CONSTRAINT [UQ_auth_refresh_tokens_token_hash] UNIQUE ([token_hash]),
        CONSTRAINT [FK_auth_refresh_tokens_users] FOREIGN KEY ([user_id])
            REFERENCES [auth].[users]([id])
    );

    -- ============================================================
    -- SCHEDULING (7 bang)
    -- ============================================================

    CREATE TABLE [scheduling].[specialties] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [name] NVARCHAR(255) NOT NULL,
        [description] NVARCHAR(500) NULL,
        [is_active] BIT NOT NULL CONSTRAINT [DF_scheduling_specialties_is_active] DEFAULT (1),
        [created_at] DATETIME2 NOT NULL CONSTRAINT [DF_scheduling_specialties_created_at] DEFAULT (SYSUTCDATETIME()),
        [updated_at] DATETIME2 NULL,
        CONSTRAINT [PK_scheduling_specialties] PRIMARY KEY ([id])
    );

    CREATE TABLE [scheduling].[doctors] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [user_id] INT NOT NULL,
        [specialty_id] INT NOT NULL,
        [full_name] NVARCHAR(255) NOT NULL,
        [title] NVARCHAR(50) NULL,
        [license_number] VARCHAR(50) NULL,
        [phone] VARCHAR(20) NULL,
        [email] VARCHAR(255) NULL,
        [room] VARCHAR(50) NULL,
        [is_active] BIT NOT NULL CONSTRAINT [DF_scheduling_doctors_is_active] DEFAULT (1),
        [created_at] DATETIME2 NOT NULL CONSTRAINT [DF_scheduling_doctors_created_at] DEFAULT (SYSUTCDATETIME()),
        [updated_at] DATETIME2 NULL,
        CONSTRAINT [PK_scheduling_doctors] PRIMARY KEY ([id]),
        CONSTRAINT [UQ_scheduling_doctors_user_id] UNIQUE ([user_id]),
        CONSTRAINT [FK_scheduling_doctors_users] FOREIGN KEY ([user_id])
            REFERENCES [auth].[users]([id]),
        CONSTRAINT [FK_scheduling_doctors_specialties] FOREIGN KEY ([specialty_id])
            REFERENCES [scheduling].[specialties]([id])
    );

    CREATE TABLE [scheduling].[patients] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [user_id] INT NULL,
        [full_name] NVARCHAR(255) NOT NULL,
        [gender] TINYINT NULL,
        [date_of_birth] DATE NULL,
        [phone] VARCHAR(20) NULL,
        [email] VARCHAR(255) NULL,
        [address] NVARCHAR(500) NULL,
        [is_active] BIT NOT NULL CONSTRAINT [DF_scheduling_patients_is_active] DEFAULT (1),
        [created_at] DATETIME2 NOT NULL CONSTRAINT [DF_scheduling_patients_created_at] DEFAULT (SYSUTCDATETIME()),
        [updated_at] DATETIME2 NULL,
        CONSTRAINT [PK_scheduling_patients] PRIMARY KEY ([id]),
        CONSTRAINT [FK_scheduling_patients_users] FOREIGN KEY ([user_id])
            REFERENCES [auth].[users]([id])
    );

    CREATE TABLE [scheduling].[doctor_schedules] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [doctor_id] INT NOT NULL,
        [day_of_week] TINYINT NOT NULL,
        [start_time] TIME NOT NULL,
        [end_time] TIME NOT NULL,
        [slot_minutes] INT NOT NULL CONSTRAINT [DF_scheduling_doctor_schedules_slot_minutes] DEFAULT (30),
        [is_active] BIT NOT NULL CONSTRAINT [DF_scheduling_doctor_schedules_is_active] DEFAULT (1),
        CONSTRAINT [PK_scheduling_doctor_schedules] PRIMARY KEY ([id]),
        CONSTRAINT [FK_scheduling_doctor_schedules_doctors] FOREIGN KEY ([doctor_id])
            REFERENCES [scheduling].[doctors]([id])
    );

    CREATE TABLE [scheduling].[doctor_time_off] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [doctor_id] INT NULL,
        [start_at] DATETIME2 NOT NULL,
        [end_at] DATETIME2 NOT NULL,
        [reason] NVARCHAR(255) NULL,
        [created_at] DATETIME2 NOT NULL CONSTRAINT [DF_scheduling_doctor_time_off_created_at] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_scheduling_doctor_time_off] PRIMARY KEY ([id]),
        CONSTRAINT [FK_scheduling_doctor_time_off_doctors] FOREIGN KEY ([doctor_id])
            REFERENCES [scheduling].[doctors]([id])
    );

    CREATE TABLE [scheduling].[appointments] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [patient_id] INT NOT NULL,
        [doctor_id] INT NOT NULL,
        [start_time] DATETIME2 NOT NULL,
        [end_time] DATETIME2 NOT NULL,
        [status] TINYINT NOT NULL CONSTRAINT [DF_scheduling_appointments_status] DEFAULT (0),
        [reason] NVARCHAR(500) NULL,
        [note] NVARCHAR(500) NULL,
        [created_by] INT NULL,
        [created_at] DATETIME2 NOT NULL CONSTRAINT [DF_scheduling_appointments_created_at] DEFAULT (SYSUTCDATETIME()),
        [updated_at] DATETIME2 NULL,
        CONSTRAINT [PK_scheduling_appointments] PRIMARY KEY ([id]),
        CONSTRAINT [FK_scheduling_appointments_patients] FOREIGN KEY ([patient_id])
            REFERENCES [scheduling].[patients]([id]),
        CONSTRAINT [FK_scheduling_appointments_doctors] FOREIGN KEY ([doctor_id])
            REFERENCES [scheduling].[doctors]([id]),
        CONSTRAINT [FK_scheduling_appointments_users] FOREIGN KEY ([created_by])
            REFERENCES [auth].[users]([id])
    );

    CREATE TABLE [scheduling].[appointment_status_history] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [appointment_id] INT NOT NULL,
        [from_status] TINYINT NULL,
        [to_status] TINYINT NOT NULL,
        [changed_by] INT NULL,
        [reason] NVARCHAR(500) NULL,
        [changed_at] DATETIME2 NOT NULL CONSTRAINT [DF_scheduling_appointment_status_history_changed_at] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_scheduling_appointment_status_history] PRIMARY KEY ([id]),
        CONSTRAINT [FK_scheduling_appointment_status_history_appointments] FOREIGN KEY ([appointment_id])
            REFERENCES [scheduling].[appointments]([id]),
        CONSTRAINT [FK_scheduling_appointment_status_history_users] FOREIGN KEY ([changed_by])
            REFERENCES [auth].[users]([id])
    );

    -- ============================================================
    -- CLINICAL (4 bang)
    -- ============================================================

    CREATE TABLE [clinical].[medical_records] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [appointment_id] INT NOT NULL,
        [patient_id] INT NOT NULL,
        [doctor_id] INT NOT NULL,
        [symptoms] NVARCHAR(4000) NULL,
        [diagnosis] NVARCHAR(4000) NULL,
        [note] NVARCHAR(4000) NULL,
        [created_at] DATETIME2 NOT NULL CONSTRAINT [DF_clinical_medical_records_created_at] DEFAULT (SYSUTCDATETIME()),
        [updated_at] DATETIME2 NULL,
        CONSTRAINT [PK_clinical_medical_records] PRIMARY KEY ([id]),
        CONSTRAINT [UQ_clinical_medical_records_appointment_id] UNIQUE ([appointment_id]),
        CONSTRAINT [FK_clinical_medical_records_appointments] FOREIGN KEY ([appointment_id])
            REFERENCES [scheduling].[appointments]([id]),
        CONSTRAINT [FK_clinical_medical_records_patients] FOREIGN KEY ([patient_id])
            REFERENCES [scheduling].[patients]([id]),
        CONSTRAINT [FK_clinical_medical_records_doctors] FOREIGN KEY ([doctor_id])
            REFERENCES [scheduling].[doctors]([id])
    );

    CREATE TABLE [clinical].[medicines] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [name] NVARCHAR(255) NOT NULL,
        [unit] NVARCHAR(50) NULL,
        [price] DECIMAL(12,2) NOT NULL CONSTRAINT [DF_clinical_medicines_price] DEFAULT (0),
        [stock_quantity] INT NOT NULL CONSTRAINT [DF_clinical_medicines_stock_quantity] DEFAULT (0),
        [expiry_date] DATE NULL,
        [description] NVARCHAR(500) NULL,
        [is_active] BIT NOT NULL CONSTRAINT [DF_clinical_medicines_is_active] DEFAULT (1),
        [created_at] DATETIME2 NOT NULL CONSTRAINT [DF_clinical_medicines_created_at] DEFAULT (SYSUTCDATETIME()),
        [updated_at] DATETIME2 NULL,
        CONSTRAINT [PK_clinical_medicines] PRIMARY KEY ([id])
    );

    CREATE TABLE [clinical].[prescriptions] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [medical_record_id] INT NOT NULL,
        [note] NVARCHAR(500) NULL,
        [created_at] DATETIME2 NOT NULL CONSTRAINT [DF_clinical_prescriptions_created_at] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_clinical_prescriptions] PRIMARY KEY ([id]),
        CONSTRAINT [UQ_clinical_prescriptions_medical_record_id] UNIQUE ([medical_record_id]),
        CONSTRAINT [FK_clinical_prescriptions_medical_records] FOREIGN KEY ([medical_record_id])
            REFERENCES [clinical].[medical_records]([id])
    );

    CREATE TABLE [clinical].[prescription_items] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [prescription_id] INT NOT NULL,
        [medicine_id] INT NOT NULL,
        [quantity] INT NOT NULL CONSTRAINT [DF_clinical_prescription_items_quantity] DEFAULT (1),
        [dosage] NVARCHAR(255) NULL,
        [instruction] NVARCHAR(255) NULL,
        CONSTRAINT [PK_clinical_prescription_items] PRIMARY KEY ([id]),
        CONSTRAINT [FK_clinical_prescription_items_prescriptions] FOREIGN KEY ([prescription_id])
            REFERENCES [clinical].[prescriptions]([id]),
        CONSTRAINT [FK_clinical_prescription_items_medicines] FOREIGN KEY ([medicine_id])
            REFERENCES [clinical].[medicines]([id])
    );

    -- ============================================================
    -- BILLING (4 bang)
    -- ============================================================

    CREATE TABLE [billing].[services] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [name] NVARCHAR(255) NOT NULL,
        [description] NVARCHAR(500) NULL,
        [price] DECIMAL(12,2) NOT NULL CONSTRAINT [DF_billing_services_price] DEFAULT (0),
        [is_active] BIT NOT NULL CONSTRAINT [DF_billing_services_is_active] DEFAULT (1),
        [created_at] DATETIME2 NOT NULL CONSTRAINT [DF_billing_services_created_at] DEFAULT (SYSUTCDATETIME()),
        [updated_at] DATETIME2 NULL,
        CONSTRAINT [PK_billing_services] PRIMARY KEY ([id])
    );

    CREATE TABLE [billing].[invoices] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [patient_id] INT NOT NULL,
        [appointment_id] INT NULL,
        [medical_record_id] INT NULL,
        [patient_name] NVARCHAR(255) NULL,
        [total_amount] DECIMAL(12,2) NOT NULL CONSTRAINT [DF_billing_invoices_total_amount] DEFAULT (0),
        [status] TINYINT NOT NULL CONSTRAINT [DF_billing_invoices_status] DEFAULT (0),
        [created_by] INT NULL,
        [created_at] DATETIME2 NOT NULL CONSTRAINT [DF_billing_invoices_created_at] DEFAULT (SYSUTCDATETIME()),
        [updated_at] DATETIME2 NULL,
        CONSTRAINT [PK_billing_invoices] PRIMARY KEY ([id]),
        CONSTRAINT [FK_billing_invoices_patients] FOREIGN KEY ([patient_id])
            REFERENCES [scheduling].[patients]([id]),
        CONSTRAINT [FK_billing_invoices_appointments] FOREIGN KEY ([appointment_id])
            REFERENCES [scheduling].[appointments]([id]),
        CONSTRAINT [FK_billing_invoices_medical_records] FOREIGN KEY ([medical_record_id])
            REFERENCES [clinical].[medical_records]([id]),
        CONSTRAINT [FK_billing_invoices_users] FOREIGN KEY ([created_by])
            REFERENCES [auth].[users]([id])
    );

    CREATE TABLE [billing].[invoice_items] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [invoice_id] INT NOT NULL,
        [service_id] INT NULL,
        [medicine_id] INT NULL,
        [description] NVARCHAR(255) NULL,
        [quantity] INT NOT NULL CONSTRAINT [DF_billing_invoice_items_quantity] DEFAULT (1),
        [unit_price] DECIMAL(12,2) NOT NULL CONSTRAINT [DF_billing_invoice_items_unit_price] DEFAULT (0),
        [amount] DECIMAL(12,2) NOT NULL CONSTRAINT [DF_billing_invoice_items_amount] DEFAULT (0),
        CONSTRAINT [PK_billing_invoice_items] PRIMARY KEY ([id]),
        CONSTRAINT [FK_billing_invoice_items_invoices] FOREIGN KEY ([invoice_id])
            REFERENCES [billing].[invoices]([id]),
        CONSTRAINT [FK_billing_invoice_items_services] FOREIGN KEY ([service_id])
            REFERENCES [billing].[services]([id]),
        CONSTRAINT [FK_billing_invoice_items_medicines] FOREIGN KEY ([medicine_id])
            REFERENCES [clinical].[medicines]([id])
    );

    CREATE TABLE [billing].[payments] (
        [id] INT IDENTITY(1,1) NOT NULL,
        [invoice_id] INT NOT NULL,
        [amount] DECIMAL(12,2) NOT NULL,
        [method] TINYINT NOT NULL CONSTRAINT [DF_billing_payments_method] DEFAULT (0),
        [paid_at] DATETIME2 NOT NULL CONSTRAINT [DF_billing_payments_paid_at] DEFAULT (SYSUTCDATETIME()),
        [note] NVARCHAR(255) NULL,
        CONSTRAINT [PK_billing_payments] PRIMARY KEY ([id]),
        CONSTRAINT [FK_billing_payments_invoices] FOREIGN KEY ([invoice_id])
            REFERENCES [billing].[invoices]([id])
    );

    -- ============================================================
    -- INDEXES THEO DBML GOC
    -- ============================================================

    -- Filtered unique: chi kiem tra cac gia tri email/user_id/license
    -- khong NULL, dung voi ghi chu trong DBML.
    CREATE UNIQUE INDEX [UX_auth_users_email]
        ON [auth].[users]([email])
        WHERE [email] IS NOT NULL;

    CREATE UNIQUE INDEX [UX_scheduling_doctors_license_number]
        ON [scheduling].[doctors]([license_number])
        WHERE [license_number] IS NOT NULL;

    CREATE UNIQUE INDEX [UX_scheduling_patients_user_id]
        ON [scheduling].[patients]([user_id])
        WHERE [user_id] IS NOT NULL;

    CREATE INDEX [IX_patients_full_name]
        ON [scheduling].[patients]([full_name]);

    CREATE INDEX [IX_patients_phone]
        ON [scheduling].[patients]([phone]);

    CREATE INDEX [IX_appointments_doctor_start]
        ON [scheduling].[appointments]([doctor_id], [start_time]);

    -- Giu dung y dinh filtered unique cua ban goc.
    -- CHUA chan duoc hai khoang gio chong lan co start_time khac nhau.
    CREATE UNIQUE INDEX [UX_appointments_doctor_slot]
        ON [scheduling].[appointments]([doctor_id], [start_time])
        WHERE [status] < 4;

    CREATE INDEX [IX_appointments_patient_id]
        ON [scheduling].[appointments]([patient_id]);

    CREATE INDEX [IX_appointments_start_time]
        ON [scheduling].[appointments]([start_time]);

    CREATE INDEX [IX_invoices_patient_id]
        ON [billing].[invoices]([patient_id]);

    CREATE INDEX [IX_invoices_appointment_id]
        ON [billing].[invoices]([appointment_id]);

    CREATE INDEX [IX_invoices_status_created_at]
        ON [billing].[invoices]([status], [created_at]);

    COMMIT TRANSACTION;
    PRINT N'Da tao xong 19 bang goc.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

-- ================================================================
-- KIEM TRA: phai tra ve 19 bang goc neu database chua co bang khac.
-- ================================================================
SELECT
    s.name AS schema_name,
    t.name AS table_name
FROM sys.tables AS t
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE s.name IN (N'auth', N'scheduling', N'clinical', N'billing')
ORDER BY s.name, t.name;

SELECT COUNT(*) AS total_tables
FROM sys.tables AS t
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE s.name IN (N'auth', N'scheduling', N'clinical', N'billing');
GO
