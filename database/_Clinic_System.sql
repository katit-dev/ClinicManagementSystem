
/****** Object:  Schema [auth]    Script Date: 09/10/2026 00:32:54 ******/
CREATE SCHEMA [auth]
GO
/****** Object:  Schema [billing]    Script Date: 09/10/2026 00:32:54 ******/
CREATE SCHEMA [billing]
GO
/****** Object:  Schema [clinical]    Script Date: 09/10/2026 00:32:54 ******/
CREATE SCHEMA [clinical]
GO
/****** Object:  Schema [scheduling]    Script Date: 09/10/2026 00:32:54 ******/
CREATE SCHEMA [scheduling]
GO
/****** Object:  Table [auth].[audit_logs]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [auth].[audit_logs](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[user_id] [int] NULL,
	[action] [varchar](100) NOT NULL,
	[entity_name] [varchar](128) NOT NULL,
	[entity_id] [int] NULL,
	[details] [nvarchar](max) NULL,
	[ip_address] [varchar](45) NULL,
	[succeeded] [bit] NOT NULL,
	[occurred_at] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_auth_audit_logs] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [auth].[notifications]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [auth].[notifications](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[appointment_id] [int] NOT NULL,
	[user_id] [int] NULL,
	[channel] [varchar](20) NOT NULL,
	[recipient] [nvarchar](255) NULL,
	[title] [nvarchar](255) NOT NULL,
	[content] [nvarchar](2000) NOT NULL,
	[status] [tinyint] NOT NULL,
	[scheduled_at] [datetime2](7) NULL,
	[sent_at] [datetime2](7) NULL,
	[error_message] [nvarchar](1000) NULL,
	[created_at] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_auth_notifications] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [auth].[password_reset_tokens]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [auth].[password_reset_tokens](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[user_id] [int] NOT NULL,
	[token_hash] [varchar](64) NOT NULL,
	[expires_at] [datetime2](7) NOT NULL,
	[used_at] [datetime2](7) NULL,
	[revoked_at] [datetime2](7) NULL,
	[created_at] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_password_reset_tokens] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [auth].[refresh_tokens]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [auth].[refresh_tokens](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[user_id] [int] NOT NULL,
	[token_hash] [char](64) NOT NULL,
	[expires_at] [datetime2](7) NOT NULL,
	[revoked_at] [datetime2](7) NULL,
	[created_at] [datetime2](7) NOT NULL,
	[device_info] [nvarchar](500) NULL,
	[ip_address] [varchar](45) NULL,
	[replaced_by_token_hash] [char](64) NULL,
 CONSTRAINT [PK_auth_refresh_tokens] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [auth].[roles]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [auth].[roles](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[name] [varchar](50) NOT NULL,
	[description] [nvarchar](255) NULL,
	[is_system] [bit] NOT NULL,
 CONSTRAINT [PK_auth_roles] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [auth].[user_roles]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [auth].[user_roles](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[user_id] [int] NOT NULL,
	[role_id] [int] NOT NULL,
	[assigned_at] [datetime2](7) NOT NULL,
	[assigned_by] [int] NULL,
 CONSTRAINT [PK_auth_user_roles] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [auth].[users]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [auth].[users](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[username] [varchar](100) NOT NULL,
	[password_hash] [varchar](255) NOT NULL,
	[email] [varchar](255) NULL,
	[full_name] [nvarchar](255) NULL,
	[phone] [varchar](20) NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](7) NOT NULL,
	[updated_at] [datetime2](7) NULL,
	[last_login_at] [datetime2](7) NULL,
	[failed_login_count] [int] NOT NULL,
	[lockout_end] [datetime2](7) NULL,
	[email_confirmed] [bit] NOT NULL,
	[avatar_url] [varchar](500) NULL,
	[is_deleted] [bit] NOT NULL,
	[deleted_at] [datetime2](7) NULL,
 CONSTRAINT [PK_auth_users] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [billing].[insurance_policies]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [billing].[insurance_policies](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[patient_id] [int] NOT NULL,
	[policy_number] [varchar](50) NOT NULL,
	[provider_name] [nvarchar](255) NULL,
	[coverage_percent] [decimal](5, 2) NULL,
	[route_type] [tinyint] NULL,
	[valid_from] [date] NULL,
	[valid_to] [date] NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](7) NOT NULL,
	[updated_at] [datetime2](7) NULL,
 CONSTRAINT [PK_billing_insurance_policies] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [billing].[invoice_items]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [billing].[invoice_items](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[invoice_id] [int] NOT NULL,
	[medicine_id] [int] NULL,
	[description] [nvarchar](255) NULL,
	[quantity] [int] NOT NULL,
	[unit_price] [decimal](12, 2) NOT NULL,
	[amount] [decimal](12, 2) NOT NULL,
	[medical_record_service_id] [int] NULL,
	[discount_amount] [decimal](12, 2) NOT NULL,
 CONSTRAINT [PK_billing_invoice_items] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [billing].[invoices]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [billing].[invoices](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[patient_id] [int] NOT NULL,
	[appointment_id] [int] NULL,
	[medical_record_id] [int] NULL,
	[patient_name] [nvarchar](255) NULL,
	[total_amount] [decimal](12, 2) NOT NULL,
	[status] [tinyint] NOT NULL,
	[created_by] [int] NULL,
	[created_at] [datetime2](7) NOT NULL,
	[updated_at] [datetime2](7) NULL,
	[invoice_no] [varchar](30) NOT NULL,
	[discount_amount] [decimal](12, 2) NOT NULL,
	[tax_amount] [decimal](12, 2) NOT NULL,
	[paid_amount] [decimal](12, 2) NOT NULL,
	[insurance_amount] [decimal](12, 2) NOT NULL,
	[cancelled_at] [datetime2](7) NULL,
	[cancel_reason] [nvarchar](500) NULL,
 CONSTRAINT [PK_billing_invoices] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [billing].[payments]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [billing].[payments](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[invoice_id] [int] NOT NULL,
	[amount] [decimal](12, 2) NOT NULL,
	[method] [tinyint] NOT NULL,
	[paid_at] [datetime2](7) NOT NULL,
	[note] [nvarchar](255) NULL,
	[reference_code] [varchar](100) NULL,
	[received_by] [int] NULL,
	[is_refund] [bit] NOT NULL,
 CONSTRAINT [PK_billing_payments] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [billing].[services]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [billing].[services](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[name] [nvarchar](255) NOT NULL,
	[description] [nvarchar](500) NULL,
	[price] [decimal](12, 2) NOT NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](7) NOT NULL,
	[updated_at] [datetime2](7) NULL,
	[code] [varchar](30) NULL,
	[specialty_id] [int] NULL,
	[duration_minutes] [int] NULL,
 CONSTRAINT [PK_billing_services] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [clinical].[attachments]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [clinical].[attachments](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[medical_record_id] [int] NOT NULL,
	[file_url] [nvarchar](1000) NOT NULL,
	[file_type] [varchar](100) NULL,
	[uploaded_at] [datetime2](7) NOT NULL,
	[uploaded_by] [int] NULL,
 CONSTRAINT [PK_clinical_attachments] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [clinical].[lab_results]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [clinical].[lab_results](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[medical_record_service_id] [int] NOT NULL,
	[result_value] [nvarchar](4000) NULL,
	[reference_range] [nvarchar](500) NULL,
	[conclusion] [nvarchar](4000) NULL,
	[resulted_at] [datetime2](7) NULL,
	[recorded_by] [int] NULL,
	[created_at] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_clinical_lab_results] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [clinical].[medical_record_services]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [clinical].[medical_record_services](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[medical_record_id] [int] NOT NULL,
	[service_id] [int] NOT NULL,
	[quantity] [int] NOT NULL,
	[unit_price_snapshot] [decimal](12, 2) NOT NULL,
	[status] [tinyint] NOT NULL,
	[performed_by] [int] NULL,
	[ordered_at] [datetime2](7) NOT NULL,
	[completed_at] [datetime2](7) NULL,
 CONSTRAINT [PK_clinical_medical_record_services] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [clinical].[medical_records]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [clinical].[medical_records](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[appointment_id] [int] NOT NULL,
	[patient_id] [int] NOT NULL,
	[doctor_id] [int] NOT NULL,
	[symptoms] [nvarchar](4000) NULL,
	[diagnosis] [nvarchar](4000) NULL,
	[note] [nvarchar](4000) NULL,
	[created_at] [datetime2](7) NOT NULL,
	[updated_at] [datetime2](7) NULL,
	[icd10_code] [varchar](10) NULL,
	[treatment_plan] [nvarchar](4000) NULL,
	[follow_up_date] [date] NULL,
	[status] [tinyint] NOT NULL,
	[finalized_at] [datetime2](7) NULL,
 CONSTRAINT [PK_clinical_medical_records] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [clinical].[medicine_batches]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [clinical].[medicine_batches](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[medicine_id] [int] NOT NULL,
	[batch_no] [varchar](50) NOT NULL,
	[expiry_date] [date] NOT NULL,
	[quantity] [int] NOT NULL,
	[import_price] [decimal](12, 2) NULL,
 CONSTRAINT [PK_clinical_medicine_batches] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [clinical].[medicine_stock_transactions]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [clinical].[medicine_stock_transactions](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[medicine_id] [int] NOT NULL,
	[batch_id] [int] NOT NULL,
	[type] [tinyint] NOT NULL,
	[quantity] [int] NOT NULL,
	[quantity_before] [int] NOT NULL,
	[quantity_after] [int] NOT NULL,
	[prescription_id] [int] NULL,
	[reversal_of_transaction_id] [int] NULL,
	[created_by] [int] NOT NULL,
	[created_at] [datetime2](7) NOT NULL,
	[note] [nvarchar](500) NULL,
 CONSTRAINT [PK_clinical_medicine_stock_transactions] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [clinical].[medicines]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [clinical].[medicines](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[name] [nvarchar](255) NOT NULL,
	[unit] [nvarchar](50) NULL,
	[price] [decimal](12, 2) NOT NULL,
	[stock_quantity] [int] NOT NULL,
	[description] [nvarchar](500) NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](7) NOT NULL,
	[updated_at] [datetime2](7) NULL,
	[code] [varchar](30) NULL,
	[active_ingredient] [nvarchar](255) NULL,
	[concentration] [nvarchar](100) NULL,
	[cost_price] [decimal](12, 2) NULL,
	[min_stock] [int] NOT NULL,
 CONSTRAINT [PK_clinical_medicines] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [clinical].[patient_allergies]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [clinical].[patient_allergies](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[patient_id] [int] NOT NULL,
	[allergen] [nvarchar](255) NOT NULL,
	[severity] [tinyint] NULL,
	[note] [nvarchar](500) NULL,
 CONSTRAINT [PK_clinical_patient_allergies] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [clinical].[patient_vitals]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [clinical].[patient_vitals](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[medical_record_id] [int] NOT NULL,
	[temperature] [decimal](4, 1) NULL,
	[pulse] [int] NULL,
	[blood_pressure] [varchar](20) NULL,
	[weight] [decimal](6, 2) NULL,
	[height] [decimal](6, 2) NULL,
 CONSTRAINT [PK_clinical_patient_vitals] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [clinical].[prescription_items]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [clinical].[prescription_items](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[prescription_id] [int] NOT NULL,
	[medicine_id] [int] NOT NULL,
	[quantity] [int] NOT NULL,
	[dosage] [nvarchar](255) NULL,
	[instruction] [nvarchar](255) NULL,
	[medicine_name_snapshot] [nvarchar](255) NOT NULL,
	[unit_price_snapshot] [decimal](12, 2) NOT NULL,
	[duration_days] [int] NULL,
	[frequency] [nvarchar](50) NULL,
 CONSTRAINT [PK_clinical_prescription_items] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [clinical].[prescriptions]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [clinical].[prescriptions](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[medical_record_id] [int] NOT NULL,
	[note] [nvarchar](500) NULL,
	[created_at] [datetime2](7) NOT NULL,
	[doctor_id] [int] NOT NULL,
	[status] [tinyint] NOT NULL,
	[dispensed_at] [datetime2](7) NULL,
	[dispensed_by] [int] NULL,
 CONSTRAINT [PK_clinical_prescriptions] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [scheduling].[appointment_status_history]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [scheduling].[appointment_status_history](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[appointment_id] [int] NOT NULL,
	[from_status] [tinyint] NULL,
	[to_status] [tinyint] NOT NULL,
	[changed_by] [int] NULL,
	[reason] [nvarchar](500) NULL,
	[changed_at] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_scheduling_appointment_status_history] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [scheduling].[appointments]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [scheduling].[appointments](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[patient_id] [int] NOT NULL,
	[doctor_id] [int] NOT NULL,
	[start_time] [datetime2](7) NOT NULL,
	[end_time] [datetime2](7) NOT NULL,
	[status] [tinyint] NOT NULL,
	[reason] [nvarchar](500) NULL,
	[note] [nvarchar](500) NULL,
	[created_by] [int] NULL,
	[created_at] [datetime2](7) NOT NULL,
	[updated_at] [datetime2](7) NULL,
	[appointment_code] [varchar](20) NOT NULL,
	[queue_number] [int] NULL,
	[checked_in_at] [datetime2](7) NULL,
	[completed_at] [datetime2](7) NULL,
	[cancel_reason] [nvarchar](500) NULL,
	[source] [tinyint] NOT NULL,
	[fee_snapshot] [decimal](12, 2) NOT NULL,
 CONSTRAINT [PK_scheduling_appointments] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [scheduling].[doctor_schedules]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [scheduling].[doctor_schedules](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[doctor_id] [int] NOT NULL,
	[day_of_week] [tinyint] NOT NULL,
	[start_time] [time](7) NOT NULL,
	[end_time] [time](7) NOT NULL,
	[slot_minutes] [int] NOT NULL,
	[is_active] [bit] NOT NULL,
	[effective_from] [date] NOT NULL,
	[effective_to] [date] NULL,
	[break_start] [time](7) NULL,
	[break_end] [time](7) NULL,
 CONSTRAINT [PK_scheduling_doctor_schedules] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [scheduling].[doctor_time_off]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [scheduling].[doctor_time_off](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[doctor_id] [int] NULL,
	[start_at] [datetime2](7) NOT NULL,
	[end_at] [datetime2](7) NOT NULL,
	[reason] [nvarchar](255) NULL,
	[created_at] [datetime2](7) NOT NULL,
	[type] [tinyint] NULL,
	[is_full_day] [bit] NOT NULL,
	[approved_by] [int] NULL,
 CONSTRAINT [PK_scheduling_doctor_time_off] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [scheduling].[doctors]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [scheduling].[doctors](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[user_id] [int] NOT NULL,
	[specialty_id] [int] NOT NULL,
	[full_name] [nvarchar](255) NOT NULL,
	[title] [nvarchar](50) NULL,
	[license_number] [varchar](50) NULL,
	[phone] [varchar](20) NULL,
	[email] [varchar](255) NULL,
	[room] [varchar](50) NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](7) NOT NULL,
	[updated_at] [datetime2](7) NULL,
	[consultation_fee] [decimal](12, 2) NOT NULL,
	[bio] [nvarchar](2000) NULL,
	[avatar_url] [varchar](500) NULL,
	[experience_years] [int] NULL,
	[max_patients_per_day] [int] NULL,
 CONSTRAINT [PK_scheduling_doctors] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [scheduling].[patients]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [scheduling].[patients](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[user_id] [int] NULL,
	[full_name] [nvarchar](255) NOT NULL,
	[gender] [tinyint] NULL,
	[date_of_birth] [date] NULL,
	[phone] [varchar](20) NULL,
	[email] [varchar](255) NULL,
	[address] [nvarchar](500) NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](7) NOT NULL,
	[updated_at] [datetime2](7) NULL,
	[patient_code] [varchar](20) NOT NULL,
	[national_id] [varchar](30) NULL,
	[insurance_number] [varchar](50) NULL,
	[blood_type] [varchar](10) NULL,
	[emergency_contact_name] [nvarchar](255) NULL,
	[emergency_contact_phone] [varchar](20) NULL,
 CONSTRAINT [PK_scheduling_patients] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [scheduling].[specialties]    Script Date: 09/10/2026 00:32:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [scheduling].[specialties](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[name] [nvarchar](255) NOT NULL,
	[description] [nvarchar](500) NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](7) NOT NULL,
	[updated_at] [datetime2](7) NULL,
	[code] [varchar](20) NULL,
 CONSTRAINT [PK_scheduling_specialties] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET IDENTITY_INSERT [auth].[audit_logs] ON 
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (1, 1, N'VIEW_PATIENT_RECORDS', N'Patient', 8, N'Receptionist viewed medical records of patient BNCDFAACEDA161489CBE.', NULL, 1, CAST(N'2026-09-25T01:56:34.2047109' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (2, 1, N'VIEW_PATIENT_RECORDS', N'Patient', 5, N'Receptionist viewed medical records of patient BN78AD40B9E78942B096.', NULL, 1, CAST(N'2026-09-25T01:56:43.9920440' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (3, 1, N'CREATE_PAYMENT', N'Invoice', 1, N'Thu tiền hóa đơn INV-TEST-0001. Số tiền: 10,000. Phương thức: 3.', NULL, 1, CAST(N'2026-09-27T16:29:57.5174666' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (4, 1, N'CREATE_PAYMENT', N'Invoice', 1, N'Thu tiền hóa đơn INV-TEST-0001. Số tiền: 10,000. Phương thức: 0.', NULL, 1, CAST(N'2026-09-27T23:53:05.3897485' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (5, 1, N'CREATE_PAYMENT', N'Invoice', 1, N'Thu tiền hóa đơn INV-TEST-0001. Số tiền: 70,000. Phương thức: 0.', NULL, 1, CAST(N'2026-09-27T23:58:51.7489824' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (6, 1, N'REFUND_INVOICE', N'Invoice', 1, N'Hoàn tiền hóa đơn INV-TEST-0001. Số tiền: -10,000. Phương thức: 0.', NULL, 1, CAST(N'2026-09-28T00:10:22.2386055' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (7, 1, N'REFUND_INVOICE', N'Invoice', 1, N'Hoàn tiền hóa đơn INV-TEST-0001. Số tiền: -180,000. Phương thức: 0.', NULL, 1, CAST(N'2026-09-28T00:32:52.5450525' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (8, 1, N'CANCEL_INVOICE', N'Invoice', 1, N'Hủy hóa đơn INV-TEST-0001. Lý do: Bệnh nhân không tiếp tục sử dụng dịch vụ.', NULL, 1, CAST(N'2026-09-28T01:26:45.6656109' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (9, 1, N'CREATE_PAYMENT', N'Invoice', 2, N'Thu tiền hóa đơn INV-TEST-CANCEL-0001. Số tiền: 10,000. Phương thức: 0.', NULL, 1, CAST(N'2026-09-28T01:31:19.2116787' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (10, 1, N'CREATE_PAYMENT', N'Invoice', 2, N'Thu tiền hóa đơn INV-TEST-CANCEL-0001. Số tiền: 10,000. Phương thức: 0.', NULL, 1, CAST(N'2026-09-28T11:23:57.9874851' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (11, 1, N'CREATE_PAYMENT', N'Invoice', 2, N'Thu tiền hóa đơn INV-TEST-CANCEL-0001. Số tiền: 230,000. Phương thức: 0.', NULL, 1, CAST(N'2026-09-28T11:24:46.4646206' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (12, 1, N'REFUND_INVOICE', N'Invoice', 2, N'Hoàn tiền hóa đơn INV-TEST-CANCEL-0001. Số tiền: -10,000. Phương thức: 0.', NULL, 1, CAST(N'2026-09-28T11:37:35.6078848' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (13, 1, N'CANCEL_INVOICE', N'Invoice', 5, N'Hủy hóa đơn INV-TEST-CANCEL-20260928-885. Lý do: Khach hang khong su dung dich vu nua', NULL, 1, CAST(N'2026-09-28T12:30:00.9248060' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (14, 1, N'CREATE_PAYMENT', N'Invoice', 2, N'Thu tiền hóa đơn INV-TEST-CANCEL-0001. Số tiền: 10,000. Phương thức: 0.', NULL, 1, CAST(N'2026-09-28T12:55:32.2570527' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (15, 9, N'VIEW_PATIENT_MEDICAL_RECORDS', N'Patient', 5, N'Viewed medical records of patient BN78AD40B9E78942B096.', NULL, 1, CAST(N'2026-09-28T17:22:21.3252545' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (16, 9, N'VIEW_PATIENT_MEDICAL_RECORDS', N'Patient', 5, N'Viewed medical records of patient BN78AD40B9E78942B096.', NULL, 1, CAST(N'2026-09-29T15:10:09.6375949' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (17, 9, N'VIEW_PATIENT_MEDICAL_RECORDS', N'Patient', 5, N'Viewed medical records of patient BN78AD40B9E78942B096.', NULL, 1, CAST(N'2026-09-29T15:11:54.9420235' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (18, 9, N'VIEW_PATIENT_MEDICAL_RECORDS', N'Patient', 5, N'Viewed medical records of patient BN78AD40B9E78942B096.', NULL, 1, CAST(N'2026-09-29T15:15:29.4515821' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (19, 9, N'VIEW_PATIENT_MEDICAL_RECORDS', N'Patient', 5, N'Viewed medical records of patient BN78AD40B9E78942B096.', NULL, 1, CAST(N'2026-09-29T16:00:48.2002580' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (20, 9, N'FINALIZE_MEDICAL_RECORD', N'MedicalRecord', 3, N'Chốt bệnh án #3. Hóa đơn: INV4F490A1874334B739. Tổng tiền: 200,000.', NULL, 1, CAST(N'2026-09-30T10:38:29.0343849' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (21, 9, N'VIEW_PATIENT_MEDICAL_RECORDS', N'Patient', 7, N'Viewed medical records of patient BNEF5D65C382FC4C19B1.', NULL, 1, CAST(N'2026-09-30T07:12:56.0805744' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (22, 9, N'FINALIZE_MEDICAL_RECORD', N'MedicalRecord', 5, N'Chốt bệnh án #5. Hóa đơn: INV875DBA02C6C3424C8. Tổng tiền: 200,000.', NULL, 1, CAST(N'2026-09-30T14:59:10.4148884' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (23, 9, N'FINALIZE_MEDICAL_RECORD', N'MedicalRecord', 8, N'Chốt bệnh án #8. Hóa đơn: INV54E7949239DB4CD68. Tổng tiền: 232,000.', NULL, 1, CAST(N'2026-10-02T01:52:00.0605648' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (24, 9, N'FINALIZE_MEDICAL_RECORD', N'MedicalRecord', 9, N'Chốt bệnh án #9. Hóa đơn: INV07BD514BAA4D46299. Tổng tiền: 200,000.', NULL, 1, CAST(N'2026-10-02T09:09:42.1264415' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (25, 9, N'FINALIZE_MEDICAL_RECORD', N'MedicalRecord', 10, N'Chốt bệnh án #10. Hóa đơn: INVD8B19AAE7A974D31B. Tổng tiền: 220,000.', NULL, 1, CAST(N'2026-10-02T09:14:31.1326808' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (26, 13, N'ADJUST_MEDICINE_STOCK', N'MedicineBatch', 2, N'Điều chỉnh tồn thuốc #1 - lô TEST-PARA-002: 87 → 90. Lý do: Kiểm kê phát hiện dư 5 viên.', NULL, 1, CAST(N'2026-10-02T16:01:59.1955223' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (27, 13, N'ADJUST_MEDICINE_STOCK', N'MedicineBatch', 2, N'Điều chỉnh tồn thuốc #1 - lô TEST-PARA-002: 90 → 85. Lý do: Kiểm kê thực tế thiếu 5 viên.', NULL, 1, CAST(N'2026-10-02T16:02:19.7928421' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (28, 13, N'ADJUST_MEDICINE_STOCK', N'MedicineBatch', 2, N'Điều chỉnh tồn thuốc #1 - lô TEST-PARA-002: 85 → 90. Lý do: Kiểm kê phát hiện dư 5 viên.', NULL, 1, CAST(N'2026-10-02T22:35:27.5129682' AS DateTime2))
GO
INSERT [auth].[audit_logs] ([id], [user_id], [action], [entity_name], [entity_id], [details], [ip_address], [succeeded], [occurred_at]) VALUES (29, 13, N'ADJUST_MEDICINE_STOCK', N'MedicineBatch', 2, N'Điều chỉnh tồn thuốc #1 - lô TEST-PARA-002: 90 → 85. Lý do: Kiểm kê phát hiện thieu 5 viên.', NULL, 1, CAST(N'2026-10-02T22:35:55.5283878' AS DateTime2))
GO
SET IDENTITY_INSERT [auth].[audit_logs] OFF
GO
SET IDENTITY_INSERT [auth].[notifications] ON 
GO
INSERT [auth].[notifications] ([id], [appointment_id], [user_id], [channel], [recipient], [title], [content], [status], [scheduled_at], [sent_at], [error_message], [created_at]) VALUES (1, 2, 4, N'InApp', NULL, N'Nhắc lịch khám', N'Bạn có lịch khám vào 14:30 21/09/2026.', 0, CAST(N'2026-09-20T14:30:00.0000000' AS DateTime2), NULL, NULL, CAST(N'2026-09-19T08:24:31.4284398' AS DateTime2))
GO
INSERT [auth].[notifications] ([id], [appointment_id], [user_id], [channel], [recipient], [title], [content], [status], [scheduled_at], [sent_at], [error_message], [created_at]) VALUES (2, 5, 4, N'InApp', NULL, N'Nhắc lịch khám', N'Bạn có lịch khám vào 08:30 23/09/2026.', 0, CAST(N'2026-09-22T08:30:00.0000000' AS DateTime2), NULL, NULL, CAST(N'2026-09-21T15:13:27.2775125' AS DateTime2))
GO
INSERT [auth].[notifications] ([id], [appointment_id], [user_id], [channel], [recipient], [title], [content], [status], [scheduled_at], [sent_at], [error_message], [created_at]) VALUES (3, 6, 4, N'InApp', NULL, N'Nhắc lịch khám', N'Bạn có lịch khám vào 09:00 22/09/2026.', 0, CAST(N'2026-09-21T09:00:00.0000000' AS DateTime2), NULL, NULL, CAST(N'2026-09-21T15:30:53.2037659' AS DateTime2))
GO
INSERT [auth].[notifications] ([id], [appointment_id], [user_id], [channel], [recipient], [title], [content], [status], [scheduled_at], [sent_at], [error_message], [created_at]) VALUES (4, 7, 4, N'InApp', NULL, N'Nhắc lịch khám', N'Bạn có lịch khám vào 09:30 24/09/2026.', 0, CAST(N'2026-09-23T09:30:00.0000000' AS DateTime2), NULL, NULL, CAST(N'2026-09-21T15:37:28.0846597' AS DateTime2))
GO
INSERT [auth].[notifications] ([id], [appointment_id], [user_id], [channel], [recipient], [title], [content], [status], [scheduled_at], [sent_at], [error_message], [created_at]) VALUES (5, 26, 4, N'InApp', NULL, N'Nhắc lịch khám', N'Bạn có lịch khám vào 16:00 29/09/2026.', 0, CAST(N'2026-09-28T16:00:00.0000000' AS DateTime2), NULL, NULL, CAST(N'2026-09-29T08:20:47.4330350' AS DateTime2))
GO
INSERT [auth].[notifications] ([id], [appointment_id], [user_id], [channel], [recipient], [title], [content], [status], [scheduled_at], [sent_at], [error_message], [created_at]) VALUES (6, 29, 4, N'InApp', NULL, N'Nhắc lịch khám', N'Bạn có lịch khám vào 13:30 01/10/2026.', 0, CAST(N'2026-09-30T13:30:00.0000000' AS DateTime2), NULL, NULL, CAST(N'2026-10-01T05:38:02.2958667' AS DateTime2))
GO
INSERT [auth].[notifications] ([id], [appointment_id], [user_id], [channel], [recipient], [title], [content], [status], [scheduled_at], [sent_at], [error_message], [created_at]) VALUES (7, 30, 4, N'InApp', NULL, N'Nhắc lịch khám', N'Bạn có lịch khám vào 08:00 02/10/2026.', 0, CAST(N'2026-10-01T08:00:00.0000000' AS DateTime2), NULL, NULL, CAST(N'2026-10-01T17:57:03.4062169' AS DateTime2))
GO
INSERT [auth].[notifications] ([id], [appointment_id], [user_id], [channel], [recipient], [title], [content], [status], [scheduled_at], [sent_at], [error_message], [created_at]) VALUES (8, 31, 4, N'InApp', NULL, N'Nhắc lịch khám', N'Bạn có lịch khám vào 09:30 02/10/2026.', 0, CAST(N'2026-10-01T09:30:00.0000000' AS DateTime2), NULL, NULL, CAST(N'2026-10-02T02:05:39.3161404' AS DateTime2))
GO
INSERT [auth].[notifications] ([id], [appointment_id], [user_id], [channel], [recipient], [title], [content], [status], [scheduled_at], [sent_at], [error_message], [created_at]) VALUES (9, 32, 4, N'InApp', NULL, N'Nhắc lịch khám', N'Bạn có lịch khám vào 10:00 02/10/2026.', 0, CAST(N'2026-10-01T10:00:00.0000000' AS DateTime2), NULL, NULL, CAST(N'2026-10-02T02:12:24.3294710' AS DateTime2))
GO
INSERT [auth].[notifications] ([id], [appointment_id], [user_id], [channel], [recipient], [title], [content], [status], [scheduled_at], [sent_at], [error_message], [created_at]) VALUES (10, 30, 9, N'InApp', N'doctor.gen01@clinic.local', N'Báo thiếu thuốc', N'Thuốc "Paracetamol 500mg" trong đơn thuốc #6 đang thiếu. Thuốc hiện không đủ tồn kho, vui lòng đổi thuốc thay thế.', 0, NULL, NULL, NULL, CAST(N'2026-10-02T11:15:37.8806934' AS DateTime2))
GO
INSERT [auth].[notifications] ([id], [appointment_id], [user_id], [channel], [recipient], [title], [content], [status], [scheduled_at], [sent_at], [error_message], [created_at]) VALUES (11, 3, 9, N'InApp', N'doctor.gen01@clinic.local', N'Báo thiếu thuốc', N'Thuốc "Paracetamol 500mg" trong đơn thuốc #4 đang thiếu. Kho không đủ thuốc, cần bác sĩ đổi sang thuốc thay thế.', 0, NULL, NULL, NULL, CAST(N'2026-10-02T14:52:10.2166341' AS DateTime2))
GO
SET IDENTITY_INSERT [auth].[notifications] OFF
GO
SET IDENTITY_INSERT [auth].[password_reset_tokens] ON 
GO
INSERT [auth].[password_reset_tokens] ([id], [user_id], [token_hash], [expires_at], [used_at], [revoked_at], [created_at]) VALUES (1, 3, N'04C020246C8CD8491F0BED01D544CEFE2E801C84D4AB09FA7FF714246A0EA8A6', CAST(N'2026-09-13T09:23:44.7782734' AS DateTime2), NULL, NULL, CAST(N'2026-09-13T09:23:44.7782734' AS DateTime2))
GO
INSERT [auth].[password_reset_tokens] ([id], [user_id], [token_hash], [expires_at], [used_at], [revoked_at], [created_at]) VALUES (2, 3, N'A12CE6A32DDB8840ED9EEF7E75FDAB1146DC930D8916720AC960AED101B8DDC4', CAST(N'2026-09-13T09:40:28.2227503' AS DateTime2), NULL, NULL, CAST(N'2026-09-13T09:40:28.2227503' AS DateTime2))
GO
INSERT [auth].[password_reset_tokens] ([id], [user_id], [token_hash], [expires_at], [used_at], [revoked_at], [created_at]) VALUES (3, 3, N'F4466BA113FB25A18E92F3C19F5B16844F9C36340EE23EFC198AF78383DF028B', CAST(N'2026-09-13T09:43:34.6029428' AS DateTime2), NULL, NULL, CAST(N'2026-09-13T09:43:34.6029428' AS DateTime2))
GO
INSERT [auth].[password_reset_tokens] ([id], [user_id], [token_hash], [expires_at], [used_at], [revoked_at], [created_at]) VALUES (4, 3, N'2CCDF01BD36ADF65EF7FC4EC447C5E9C2A45E02A3C53EFA3AF541F522BD344B6', CAST(N'2026-09-13T10:14:35.7085453' AS DateTime2), NULL, CAST(N'2026-09-13T10:11:02.4258335' AS DateTime2), CAST(N'2026-09-13T10:09:35.7085453' AS DateTime2))
GO
INSERT [auth].[password_reset_tokens] ([id], [user_id], [token_hash], [expires_at], [used_at], [revoked_at], [created_at]) VALUES (5, 3, N'6B0972D92357BB48CDD63F426BB0295A5286B4B9E3B8EB18615DD0EDA6783E38', CAST(N'2026-09-13T10:16:02.4258335' AS DateTime2), NULL, CAST(N'2026-09-13T10:14:19.7752289' AS DateTime2), CAST(N'2026-09-13T10:11:02.4258335' AS DateTime2))
GO
INSERT [auth].[password_reset_tokens] ([id], [user_id], [token_hash], [expires_at], [used_at], [revoked_at], [created_at]) VALUES (6, 3, N'E56F6EEFCEF879E492CCB9FF755C5507C795BC00D231B766737F9594CF73E49A', CAST(N'2026-09-13T10:19:19.7752289' AS DateTime2), NULL, NULL, CAST(N'2026-09-13T10:14:19.7752289' AS DateTime2))
GO
INSERT [auth].[password_reset_tokens] ([id], [user_id], [token_hash], [expires_at], [used_at], [revoked_at], [created_at]) VALUES (7, 3, N'6B2390ADE952F52925A913692613D660E93659F8FA5B611E3BD533757275C0B8', CAST(N'2026-09-13T10:48:36.3705762' AS DateTime2), CAST(N'2026-09-13T10:45:55.8202629' AS DateTime2), NULL, CAST(N'2026-09-13T10:43:36.3705762' AS DateTime2))
GO
INSERT [auth].[password_reset_tokens] ([id], [user_id], [token_hash], [expires_at], [used_at], [revoked_at], [created_at]) VALUES (8, 1, N'4628B6F3E788D066A933604E0D2831FA834A78027D9F110458A7307984004DC4', CAST(N'2026-09-15T07:29:38.0315883' AS DateTime2), CAST(N'2026-09-15T07:25:38.0483560' AS DateTime2), NULL, CAST(N'2026-09-15T07:24:38.0315883' AS DateTime2))
GO
INSERT [auth].[password_reset_tokens] ([id], [user_id], [token_hash], [expires_at], [used_at], [revoked_at], [created_at]) VALUES (9, 3, N'BF49FB8E44E6D6E7B094B1D5A6EED4F4D2E1E33070EBB45359DB79C7B72A95B9', CAST(N'2026-09-16T16:45:45.5812951' AS DateTime2), CAST(N'2026-09-16T16:41:26.7439629' AS DateTime2), NULL, CAST(N'2026-09-16T16:40:45.5812951' AS DateTime2))
GO
INSERT [auth].[password_reset_tokens] ([id], [user_id], [token_hash], [expires_at], [used_at], [revoked_at], [created_at]) VALUES (10, 3, N'90B8270EC4B4F8C674AE379CE52565CA8CF644FD03C4EE271C4E40025FDB7269', CAST(N'2026-09-16T16:46:59.3640791' AS DateTime2), CAST(N'2026-09-16T16:43:25.6500887' AS DateTime2), NULL, CAST(N'2026-09-16T16:41:59.3640791' AS DateTime2))
GO
INSERT [auth].[password_reset_tokens] ([id], [user_id], [token_hash], [expires_at], [used_at], [revoked_at], [created_at]) VALUES (11, 1, N'6003DA18C7F5CD790DA2CBBEA36A876EEC2E115EB96D1F9837A02FD7571EE9CA', CAST(N'2026-09-24T04:37:40.9446274' AS DateTime2), CAST(N'2026-09-24T04:33:09.8198504' AS DateTime2), NULL, CAST(N'2026-09-24T04:32:40.9446274' AS DateTime2))
GO
SET IDENTITY_INSERT [auth].[password_reset_tokens] OFF
GO
SET IDENTITY_INSERT [auth].[refresh_tokens] ON 
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (1, 1, N'C8A5A1C5AFBD0F2EB2F92891B66DD76328820D50DFE7AE6946BB49A3BD3623B7', CAST(N'2026-09-18T17:25:44.0255034' AS DateTime2), NULL, CAST(N'2026-09-11T17:25:44.0255204' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (2, 1, N'B7F9422E2381BBEDEDACF257DD887D998975E06A76676EAC5A3AEC6E0E9CFBA2', CAST(N'2026-09-18T17:37:18.0321896' AS DateTime2), NULL, CAST(N'2026-09-11T17:37:18.0321904' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (3, 1, N'5B742B9684B3EE89DFAD159E4B6B4903935767984FF981F0D1F1877178B6C5E3', CAST(N'2026-09-18T17:39:36.0098675' AS DateTime2), NULL, CAST(N'2026-09-11T17:39:36.0098679' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (4, 3, N'C1BA43A861137FE139D45CCF0EDF888C1D9C46DCEF87A2F232D1EB8E286EA78F', CAST(N'2026-09-18T18:01:40.3658370' AS DateTime2), NULL, CAST(N'2026-09-11T18:01:40.3658375' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (5, 3, N'7273C89E44DC34817B535232E303BA60AD430494A21F6DBBFE810B16BFA226A2', CAST(N'2026-09-19T05:47:04.5852603' AS DateTime2), NULL, CAST(N'2026-09-12T05:47:04.5852813' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (6, 3, N'C8D1547FFD6537F2D4A28FBB39DFC081AB11FCAE3C0A67D930E2769BD176D9EF', CAST(N'2026-09-19T06:40:31.9963481' AS DateTime2), CAST(N'2026-09-12T06:41:18.1055413' AS DateTime2), CAST(N'2026-09-12T06:40:31.9964064' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (7, 3, N'55FAC43BCCBEFA98FB662801DFA0DE7B3F08ABB9FBF9406E70DC1013971108A7', CAST(N'2026-09-19T06:41:18.1055413' AS DateTime2), NULL, CAST(N'2026-09-12T06:41:18.1055413' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (8, 3, N'E63F43CD92542D6F59EC8E4B8D5CAA7B35FD0841190DE9446829D1BCE5E0AC89', CAST(N'2026-09-19T07:00:57.4255095' AS DateTime2), CAST(N'2026-09-12T07:01:09.9986726' AS DateTime2), CAST(N'2026-09-12T07:00:57.4255099' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (9, 3, N'D7ABAA8E2AD306D7B547CE861D696A08590A2EAD668EA263B930CCB7641BDB25', CAST(N'2026-09-19T07:01:09.9986726' AS DateTime2), NULL, CAST(N'2026-09-12T07:01:09.9986726' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (10, 3, N'0ED2586A46BF3470D2EC2838979F44EC9D3542AE4DB5F3ED6810A914E0E3FE42', CAST(N'2026-09-19T07:07:32.4944145' AS DateTime2), CAST(N'2026-09-12T07:09:00.6455238' AS DateTime2), CAST(N'2026-09-12T07:07:32.4944155' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (11, 3, N'BB6F8F47066777E7ACAB51631FC54C540D68A7FB766FD41F870C37C63849040C', CAST(N'2026-09-19T07:09:00.6455238' AS DateTime2), NULL, CAST(N'2026-09-12T07:09:00.6455238' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (12, 3, N'C8630D82CE39A4004DC09DDA9603BA3C6EA4DCE3841562D5D461281854CC9578', CAST(N'2026-09-21T17:59:25.7529017' AS DateTime2), NULL, CAST(N'2026-09-14T17:59:25.7529218' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (13, 3, N'B712C28E2463F6780F42AD8183B7DC828C1D952391C26545150198AFCECB3028', CAST(N'2026-09-21T18:26:46.4836804' AS DateTime2), NULL, CAST(N'2026-09-14T18:26:46.4836966' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (14, 3, N'15C76C9188650CE94010DBEE4A50B80BA492ABD21B0F0DE45FF93469AD9030D8', CAST(N'2026-09-21T18:50:47.1795396' AS DateTime2), NULL, CAST(N'2026-09-14T18:50:47.1795402' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (15, 3, N'B2E4BDC6AA1DFC205AEEEFB46FBF6484C8E0FB26026740BB69256B9FB86B40F5', CAST(N'2026-09-21T18:55:26.3221197' AS DateTime2), NULL, CAST(N'2026-09-14T18:55:26.3221202' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (16, 3, N'82A8E87A6A789E3DB351453C0812323549D3AE9BE807282698BDBAD619F93798', CAST(N'2026-09-21T18:56:21.4022877' AS DateTime2), NULL, CAST(N'2026-09-14T18:56:21.4023038' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (17, 3, N'5F4501465E38E6C4CB095C1EF3616935F4069AA4C21BD55B863CA6898343DBAB', CAST(N'2026-09-21T18:56:47.8470627' AS DateTime2), NULL, CAST(N'2026-09-14T18:56:47.8470632' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (18, 3, N'00CC413410BC96824DC07AFBA4E156EB283CCFB7D8CBE217C4E37FE3F1469169', CAST(N'2026-09-21T18:56:59.4800431' AS DateTime2), NULL, CAST(N'2026-09-14T18:56:59.4800434' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (19, 3, N'060B1D8A8C0D24330B25494F160ED25AF793A4778386C27ED34AF675DDD63179', CAST(N'2026-09-21T19:01:41.3659255' AS DateTime2), NULL, CAST(N'2026-09-14T19:01:41.3659437' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (20, 3, N'9B792BA813872F623A7B2E4F89EBC1CCDBD2EFE8DD82E16B8E108FDB9775165B', CAST(N'2026-09-21T19:05:08.3625931' AS DateTime2), NULL, CAST(N'2026-09-14T19:05:08.3626119' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (21, 3, N'B42246349EAB6234D6054DA8B77AA3842188196E18575D6847E529D79E20AE1B', CAST(N'2026-09-22T06:27:12.1647437' AS DateTime2), NULL, CAST(N'2026-09-15T06:27:12.1647600' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (22, 3, N'C062FDFB49E39CDB170CA227C856714B879286AF5DF29FB7267C9388001F20B8', CAST(N'2026-09-22T06:46:24.4955741' AS DateTime2), NULL, CAST(N'2026-09-15T06:46:24.4955747' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (23, 3, N'7A882D17FF9E985E8432AF6742870DF037A7248CFAF0C5D7EF05346484B57517', CAST(N'2026-09-22T06:53:14.9621206' AS DateTime2), NULL, CAST(N'2026-09-15T06:53:14.9621210' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (24, 3, N'E2880B3FE53512329A7FFE70B258F7768361C0A535F02D660A014F21AA420914', CAST(N'2026-09-22T07:06:45.5685883' AS DateTime2), NULL, CAST(N'2026-09-15T07:06:45.5685887' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (25, 3, N'151DB7FFE298D757165C79ABAFDAA9370F08A597E90CAF2A8BA5D7257E4EFB6A', CAST(N'2026-09-22T07:26:57.2138675' AS DateTime2), NULL, CAST(N'2026-09-15T07:26:57.2138888' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (26, 1, N'76B0FC25124BFEA411294B6CD4137C73A6349AC16F73B78D28CDDD08D1FAD288', CAST(N'2026-09-22T07:27:22.9509364' AS DateTime2), NULL, CAST(N'2026-09-15T07:27:22.9509371' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (27, 3, N'ECD4AB62E59BC25FB0FFEE538B976321F2867B75BFFF8E65904EFF3CB5BE0D1F', CAST(N'2026-09-22T07:28:31.7522478' AS DateTime2), NULL, CAST(N'2026-09-15T07:28:31.7522484' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (28, 3, N'B90696F545E1F8D82DFA32FED34D6F0AE094FD19DDC85221901DAE6C9E0F8349', CAST(N'2026-09-22T07:58:10.0545498' AS DateTime2), NULL, CAST(N'2026-09-15T07:58:10.0545881' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (29, 1, N'3CDD301ABE18993565ED4E9B27DC1A14FC83B9C07005E3BF1BA1683B3A200A18', CAST(N'2026-09-22T08:10:03.7374597' AS DateTime2), NULL, CAST(N'2026-09-15T08:10:03.7374603' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (30, 3, N'DED88E8FCE26E42F5768A036EDA78028B5ACD2AB587B4FDB635C453F93F74AE2', CAST(N'2026-09-22T08:20:17.2628023' AS DateTime2), NULL, CAST(N'2026-09-15T08:20:17.2628027' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (31, 4, N'0036524AF5E2935294C2FA84896B0C63152750D926E602056627AFA1E6AE5181', CAST(N'2026-09-23T06:56:21.1616624' AS DateTime2), NULL, CAST(N'2026-09-16T06:56:21.1617029' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (32, 3, N'FF52F70EA375C52DCC2CCA853BD46F0F27099DD35359EFE49362691B894B3C08', CAST(N'2026-09-23T16:41:38.6169638' AS DateTime2), NULL, CAST(N'2026-09-16T16:41:38.6169826' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (33, 3, N'19EFB358F2B91B4B4920D67295BBFBA870CB702E5E05B6839E47A623DFBC1BC1', CAST(N'2026-09-23T16:43:40.1824253' AS DateTime2), NULL, CAST(N'2026-09-16T16:43:40.1824259' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (34, 3, N'04477FACEBA1FAE1572A89FD69AFE4106CB5F3FD60216607EB29165F7FC4BD5F', CAST(N'2026-09-23T17:11:41.2166608' AS DateTime2), CAST(N'2026-09-16T17:12:44.3452444' AS DateTime2), CAST(N'2026-09-16T17:11:41.2166613' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (35, 3, N'FC9CAFDE455F65E1FF64C73B97A058BC0AE8108457BE2AFBD2B34559316A6EF1', CAST(N'2026-09-23T17:12:44.3452444' AS DateTime2), CAST(N'2026-09-16T17:13:41.4325181' AS DateTime2), CAST(N'2026-09-16T17:12:44.3452444' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (36, 3, N'E621044D9FE3DAF268FE8BE5F85E57774C4C124D7610A87EC33C9848DEE1848B', CAST(N'2026-09-23T17:13:41.4325181' AS DateTime2), CAST(N'2026-09-16T17:13:45.0517863' AS DateTime2), CAST(N'2026-09-16T17:13:41.4325181' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (37, 3, N'E376F85CF32C5769EE22803E6945D8540CE65DD8A0C2D6507CE6C49EE8320127', CAST(N'2026-09-23T17:13:45.0517863' AS DateTime2), CAST(N'2026-09-16T17:13:59.2774368' AS DateTime2), CAST(N'2026-09-16T17:13:45.0517863' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (38, 3, N'A9934A1FB1690328E3347A275B2037FC7E3F3188C0A9F0DC4E2D8231D8756BF2', CAST(N'2026-09-23T17:13:59.2774368' AS DateTime2), CAST(N'2026-09-16T17:14:10.6607021' AS DateTime2), CAST(N'2026-09-16T17:13:59.2774368' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (39, 3, N'54C17019EF323AC3652878BC258186A852E31EAF4F924CCCA78D57A62DE54247', CAST(N'2026-09-23T17:14:10.6607021' AS DateTime2), NULL, CAST(N'2026-09-16T17:14:10.6607021' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (40, 3, N'04F6291DB440F1E5C4DB3522AA9E74F11C07F14E28DDF4D32B7350340E7D0526', CAST(N'2026-09-23T17:18:22.7275371' AS DateTime2), NULL, CAST(N'2026-09-16T17:18:22.7275380' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (41, 4, N'4A07529143094810480398B26E1307FD83BB02317AC5D05B0CEC36252B5FB34C', CAST(N'2026-09-24T05:35:36.4356954' AS DateTime2), NULL, CAST(N'2026-09-17T05:35:36.4357852' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (42, 4, N'F4E7164841D1EF4142A7291A8261381B2F93F80866A6575759FB3FFBCC75B276', CAST(N'2026-09-26T08:18:36.5692402' AS DateTime2), NULL, CAST(N'2026-09-19T08:18:36.5692558' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (43, 4, N'047B184A42ACFD85506AA7F171190FDC0F0278C68BBC5FDCD38D9077F5CF9980', CAST(N'2026-09-26T09:41:05.9795349' AS DateTime2), NULL, CAST(N'2026-09-19T09:41:05.9795533' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (44, 3, N'282ECFF7BFC59A107705636D7626EDFA15EFA8A31AAF482AF78C9D25E09C3EC9', CAST(N'2026-09-26T09:45:00.1154078' AS DateTime2), NULL, CAST(N'2026-09-19T09:45:00.1154087' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (45, 4, N'CE19B26414715A375A2622CF97E8285036CA3E2BE87A5B679CDC58C88083DC7F', CAST(N'2026-09-27T02:02:46.7916333' AS DateTime2), NULL, CAST(N'2026-09-20T02:02:46.7916524' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (46, 4, N'D2AFFDBD3FD3628E9212E41676C200EA0C0C2140339B4267CBD7170100FFD70D', CAST(N'2026-09-27T05:07:56.8795710' AS DateTime2), NULL, CAST(N'2026-09-20T05:07:56.8795888' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (47, 4, N'2B3F77AB730E6F87C614627AB807C19935A5FF1552CD6EED6A66DB4A024AFB6C', CAST(N'2026-09-27T06:41:57.9456153' AS DateTime2), NULL, CAST(N'2026-09-20T06:41:57.9456309' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (48, 4, N'ADC495925748CA04543DC3C25737D17BCC4B5B42FF21DE46EDC3215FCC325BDA', CAST(N'2026-09-27T07:04:02.1210070' AS DateTime2), NULL, CAST(N'2026-09-20T07:04:02.1210482' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (49, 4, N'DAA8892DBC96360D022FF296C5E3668F4E3A4D5A79C45F8B36087E41E6996308', CAST(N'2026-09-27T09:12:47.4211143' AS DateTime2), NULL, CAST(N'2026-09-20T09:12:47.4211296' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (50, 3, N'EF590D75EB549BA8E081DD2CD1DE208ECF0A976DC985E3D6F07B36D06FCBAEDD', CAST(N'2026-09-27T09:13:20.4517408' AS DateTime2), NULL, CAST(N'2026-09-20T09:13:20.4517414' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (51, 4, N'F7219BB8E7C327250CD351CE2E59ADC6AEE648063E26CB699471224E5F6B1EAC', CAST(N'2026-09-27T09:13:36.8947874' AS DateTime2), NULL, CAST(N'2026-09-20T09:13:36.8947879' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (52, 4, N'EFF55BD4FA9EC47E35CF2B387C68D9F4BF56A7676D08D97A4A8A76C71AE2CF14', CAST(N'2026-09-27T10:29:03.3179832' AS DateTime2), NULL, CAST(N'2026-09-20T10:29:03.3180256' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (53, 4, N'D7E050A432BFF71BA013558BA140C170419468D1BDEFE166F38AC1B3BE38B328', CAST(N'2026-09-27T14:40:59.8187547' AS DateTime2), NULL, CAST(N'2026-09-20T14:40:59.8187724' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (54, 6, N'B571033B02EE2B78562D03D836BC545025C0885AADED762FCCD1D61692783F70', CAST(N'2026-09-27T15:16:43.1881875' AS DateTime2), NULL, CAST(N'2026-09-20T15:16:43.1882367' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (55, 4, N'8EE01D4EDE7D8FA2E05266A7F039E8A4E58CEDB27757AE98C7E191A6214B0D09', CAST(N'2026-09-27T15:21:24.3833686' AS DateTime2), NULL, CAST(N'2026-09-20T15:21:24.3833697' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (56, 4, N'3CC934F982040357AC3BE0EC357877B67FFAEF006081694FD3962592A508F7A4', CAST(N'2026-09-27T15:27:40.7047764' AS DateTime2), NULL, CAST(N'2026-09-20T15:27:40.7047922' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (57, 4, N'2496C848D01E0416B276BD4C8CEB452A4B8022E47D1B862142F60528259FBA57', CAST(N'2026-09-28T03:31:05.2769326' AS DateTime2), NULL, CAST(N'2026-09-21T03:31:05.2769513' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (58, 4, N'DBCAFF6C7BC6EAB68201A9CD55FC20C90D653219D95F6C6B985E8581AA7EF761', CAST(N'2026-09-28T03:54:46.1614213' AS DateTime2), NULL, CAST(N'2026-09-21T03:54:46.1614614' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (59, 4, N'5790443EE6D9A12B0C816CBAA7B77F63D357D1E28A1B861E3C305A648338CC16', CAST(N'2026-09-28T07:29:56.3426661' AS DateTime2), NULL, CAST(N'2026-09-21T07:29:56.3427047' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (60, 4, N'F5D3E6DB44CAA230DE5AD6BFBAC152298E49B52326A790B75AFDEFF831E705D8', CAST(N'2026-09-28T14:33:50.3362220' AS DateTime2), NULL, CAST(N'2026-09-21T14:33:50.3362690' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (61, 4, N'02481541CC8F483742D4061BF40DF845DE8CB7865D1CAD73FE0F0CB41F8FCAA4', CAST(N'2026-09-28T15:37:12.3935283' AS DateTime2), NULL, CAST(N'2026-09-21T15:37:12.3935738' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (62, 4, N'CC458C3704395A596534C1D863B3250FF938B523D94E85E0F502E8D75700B819', CAST(N'2026-09-28T17:35:40.2497208' AS DateTime2), NULL, CAST(N'2026-09-21T17:35:40.2497895' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (63, 4, N'7DAE472465F0246F8237A80D55E7A5CC79202A3F726963CAE44BC33BBC79F295', CAST(N'2026-09-28T18:43:37.1729407' AS DateTime2), NULL, CAST(N'2026-09-21T18:43:37.1729909' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (64, 4, N'EC7BA738179F02C99CD996276FBE70F54BECE792261B74F5F1C1195740990484', CAST(N'2026-09-29T05:54:17.4930320' AS DateTime2), NULL, CAST(N'2026-09-22T05:54:17.4930497' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (65, 4, N'DC7215DC152CF5DA82016D99DBF569BE7DCA1E6C49C9F4159D6DF98976BFC6C8', CAST(N'2026-09-29T06:09:36.4790673' AS DateTime2), NULL, CAST(N'2026-09-22T06:09:36.4790837' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (66, 4, N'AC6939106D5FCEBD94D5AA2BBDF14F7CBB12BC7204D6D6586C2A120D9D0DD32B', CAST(N'2026-09-29T07:13:41.8293191' AS DateTime2), NULL, CAST(N'2026-09-22T07:13:41.8293384' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (67, 4, N'41C39D4D32EDD675D98CF4B27983B471E9478BA96C2455F55637E80934E8E41C', CAST(N'2026-09-30T04:18:48.3240917' AS DateTime2), NULL, CAST(N'2026-09-23T04:18:48.3241082' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (68, 4, N'2AF4F2DF17219A47172FAE58AFA379B83FFE98DB095A76BD134A071FA6F7696E', CAST(N'2026-09-30T05:28:58.5323974' AS DateTime2), NULL, CAST(N'2026-09-23T05:28:58.5324136' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (69, 4, N'51872C042091EEE7618C522FE256D8A92D26755830A9D512D1668D85563AF6B3', CAST(N'2026-09-30T06:47:04.8946848' AS DateTime2), NULL, CAST(N'2026-09-23T06:47:04.8947116' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (70, 4, N'6D3C6B2724055A9BBA3BE5D74B0A9B9201CF05FEC99ADDFA27A89C84463910C6', CAST(N'2026-09-30T07:14:12.5951399' AS DateTime2), NULL, CAST(N'2026-09-23T07:14:12.5951578' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (71, 4, N'28AA8ACE2B0A9500658D762C629E2855776BB4CD7BF41443F422D9438EEA7716', CAST(N'2026-09-30T15:45:47.9629389' AS DateTime2), NULL, CAST(N'2026-09-23T15:45:47.9629868' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (72, 4, N'FE0A746D5B249375810353E8223E991519A990C77BB22A70BF6D283E40017CE7', CAST(N'2026-09-30T16:57:27.2918609' AS DateTime2), NULL, CAST(N'2026-09-23T16:57:27.2918802' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (73, 4, N'2AE02ED7EC13323EA5045E5C5DC8742605F56BDDF7693F1B60F6041D82A83135', CAST(N'2026-09-30T17:06:31.2896070' AS DateTime2), NULL, CAST(N'2026-09-23T17:06:31.2897006' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (74, 4, N'0A06A62FA4A440F6C1B9655D2A63511BFF33A94E0584CF54A6EB7A59D45DC0E6', CAST(N'2026-10-01T04:21:52.1713587' AS DateTime2), NULL, CAST(N'2026-09-24T04:21:52.1714021' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (75, 12, N'92AADACC4DE9D22AC6BB671DAB35F0141668131BA299A0567581EA52A380DFB1', CAST(N'2026-10-01T04:24:43.8097777' AS DateTime2), NULL, CAST(N'2026-09-24T04:24:43.8097784' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (76, 1, N'7FB4EAF068A9B0688497050B175141B5D89937048FDC137629BD3EE00A4C0B73', CAST(N'2026-10-01T04:33:27.4593608' AS DateTime2), NULL, CAST(N'2026-09-24T04:33:27.4594079' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (77, 4, N'B59E59D8EB4B5F9F7AADD2F97A7626417CC8DA07AB9F46D95E17360C5482C8A2', CAST(N'2026-10-01T05:45:53.3192063' AS DateTime2), NULL, CAST(N'2026-09-24T05:45:53.3192270' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (78, 4, N'B9B85DF3222B5E719107E4BB66BBBCBF8C56FCDA1005FC07B742AB3BD29D92AC', CAST(N'2026-10-01T06:00:29.2890133' AS DateTime2), NULL, CAST(N'2026-09-24T06:00:29.2890301' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (79, 1, N'AEE3362F46835D11BFE1F21A2666C2C3EEFBD7ECF88204A2AC2396EC76D94AE1', CAST(N'2026-10-01T06:01:13.5912631' AS DateTime2), NULL, CAST(N'2026-09-24T06:01:13.5912641' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (80, 1, N'1E0D3F85322339C52DC31C2DD0EB4C1F0011577491B5A07CC655F50D428E7AEB', CAST(N'2026-10-01T06:21:06.3481527' AS DateTime2), NULL, CAST(N'2026-09-24T06:21:06.3481704' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (81, 1, N'802E7C9061DD9E9F478C29DA788ACFEC1DCE9789FB608687719507584BB630E0', CAST(N'2026-10-01T06:22:28.1154803' AS DateTime2), NULL, CAST(N'2026-09-24T06:22:28.1154810' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (82, 4, N'EF4202BD4D1A6F4CA4DFCD5628F264AA0F65020D2F35CE9D9BC0B8B89B0E9472', CAST(N'2026-10-01T14:00:42.3709753' AS DateTime2), NULL, CAST(N'2026-09-24T14:00:42.3710432' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (83, 1, N'B165C3C160978526C10BC550E4C34C29E12FCE1B899857FEB11827F784B721E7', CAST(N'2026-10-01T14:02:19.3410917' AS DateTime2), NULL, CAST(N'2026-09-24T14:02:19.3410924' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (84, 1, N'5803131FCF460C8798A020A1606E1929CEA34E75109885B83CA1B97B2F7026C1', CAST(N'2026-10-01T15:10:33.8252905' AS DateTime2), NULL, CAST(N'2026-09-24T15:10:33.8253108' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (85, 4, N'130C06C1F2DE29B50A26187F1C35193E3531EF87FB0949ED41A36A2B6E4FD2E3', CAST(N'2026-10-01T16:58:01.8828362' AS DateTime2), NULL, CAST(N'2026-09-24T16:58:01.8828526' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (86, 1, N'6DBDED471FFABD0C2D91E6610F3A9EAF929C2358FCBCE6CC91DB2B2657646D31', CAST(N'2026-10-01T16:58:24.2794240' AS DateTime2), NULL, CAST(N'2026-09-24T16:58:24.2794252' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (87, 4, N'694C9D32E733062810775B8510A0870221D948A10AF3A20054AA768D9EB314E1', CAST(N'2026-10-01T17:24:29.7703570' AS DateTime2), NULL, CAST(N'2026-09-24T17:24:29.7703739' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (88, 1, N'D3A52A0FE873D4DE3FE50F3E832B40949B7A33CDCB562B4DA8FA5F36BD32828F', CAST(N'2026-10-01T17:27:10.2200960' AS DateTime2), NULL, CAST(N'2026-09-24T17:27:10.2200966' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (89, 1, N'4EAC8D785916FEA1CA7B2531740A8494B9DD81BED90932F882DFA732B9D1A788', CAST(N'2026-10-01T17:44:14.0630528' AS DateTime2), NULL, CAST(N'2026-09-24T17:44:14.0630693' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (90, 1, N'DD7452F0BCBBC52CC1CD9AC27CDB23EAEDFF9BE0A5EFBFFF755B4B963095EEFD', CAST(N'2026-10-01T18:04:45.5072795' AS DateTime2), NULL, CAST(N'2026-09-24T18:04:45.5072963' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (91, 1, N'0E798EC2A9E6780C34ADF77A124EC10284C27A629C54C7179103B2923E494214', CAST(N'2026-10-02T01:53:22.2280388' AS DateTime2), NULL, CAST(N'2026-09-25T01:53:22.2280857' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (92, 4, N'25DDE65EB9BA2ED97A245E6104035A08D347743A5D4DDDA08A367D2AD1D93EE3', CAST(N'2026-10-02T01:55:58.0383106' AS DateTime2), NULL, CAST(N'2026-09-25T01:55:58.0383112' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (93, 1, N'FDB44141CBB5FBD39302E9FBEE360DC2545597F25215D476226D5FCDA97AB4CB', CAST(N'2026-10-02T01:56:18.7626938' AS DateTime2), NULL, CAST(N'2026-09-25T01:56:18.7626941' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (94, 1, N'A6101164A4D1A5BAEF7DEB7EE224F8FE2847DF8CFDD5E75F00B2802B42C1F511', CAST(N'2026-10-02T02:40:31.3799581' AS DateTime2), NULL, CAST(N'2026-09-25T02:40:31.3801594' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (95, 1, N'3D726A07C28286D4D1FFB548B0322F6424924950D38EEE50502478E11CD34C39', CAST(N'2026-10-02T03:10:17.2198837' AS DateTime2), NULL, CAST(N'2026-09-25T03:10:17.2199612' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (96, 4, N'CAB18CE4F73CC5C1CC04F6B1CB270055A9CA517935C2CD377754DDAD8829E094', CAST(N'2026-10-02T03:12:37.3316583' AS DateTime2), NULL, CAST(N'2026-09-25T03:12:37.3316590' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (97, 1, N'5A9311C03887B777A74CB656368655A50BFD6B198A0AC932E9343F0D30E7949F', CAST(N'2026-10-02T03:12:56.5840752' AS DateTime2), NULL, CAST(N'2026-09-25T03:12:56.5840758' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (98, 1, N'1BAC4C02C7B2F040BD6954FEED5B05DCC2B5C5CC7F6B6F283ABC55DCFD70F99D', CAST(N'2026-10-02T03:15:15.0762440' AS DateTime2), NULL, CAST(N'2026-09-25T03:15:15.0762633' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (99, 1, N'EB759A018F4A0258A31CB5F1443808AFED9242306AAF894F0DF97DAF75437E5A', CAST(N'2026-10-02T04:28:07.3136241' AS DateTime2), CAST(N'2026-09-25T04:45:29.8132857' AS DateTime2), CAST(N'2026-09-25T04:28:07.3136718' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (100, 1, N'4DF766A3C2DD4646BD882D4FBD502244FA5D5C59C492F074C14E2995B3B39CA3', CAST(N'2026-10-02T04:45:29.8132857' AS DateTime2), NULL, CAST(N'2026-09-25T04:45:29.8132857' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (101, 1, N'93ECA611CAEB50FC65BD7AF1545E858F1D3F9B4D99EAB836F588AE5F9CA3D5D5', CAST(N'2026-10-02T07:59:27.4548792' AS DateTime2), NULL, CAST(N'2026-09-25T07:59:27.4548968' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (102, 1, N'A08409149625BF6D238AF9DDAADD6A51E03B702C3BE306025FDA9405C4B08728', CAST(N'2026-10-02T15:19:27.7007600' AS DateTime2), CAST(N'2026-09-25T16:32:08.6566986' AS DateTime2), CAST(N'2026-09-25T15:19:27.7007882' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (103, 1, N'28E3552E4081A2656E6B3AD7C6C0F33E6FBEC1E82FCA477BA4BFACE4604C8CCC', CAST(N'2026-10-02T16:22:42.8334867' AS DateTime2), NULL, CAST(N'2026-09-25T16:22:42.8335087' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (104, 1, N'D84EFD7F3B39DAFAE9EFA4D955E2A4B11B81759A689E271E0BDF2DAFA738CB52', CAST(N'2026-10-02T16:32:08.6566986' AS DateTime2), NULL, CAST(N'2026-09-25T16:32:08.6566986' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (105, 1, N'AF2B90FF36BE0C44D5F46A7994A5B1034FEAC94FB5AB9C5E72C8F1934BB2C127', CAST(N'2026-10-02T16:48:02.1886642' AS DateTime2), NULL, CAST(N'2026-09-25T16:48:02.1886818' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (106, 1, N'1145DE8FF1600844E779084623A6D87C23D5A585D8D56C0CADCDC4472850FF5E', CAST(N'2026-10-03T03:35:13.5424032' AS DateTime2), NULL, CAST(N'2026-09-26T03:35:13.5424207' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (107, 1, N'1F78A77F5636FE7A490DB86C4BEB480A84863E8668AA8978F57D7AB632F45C1A', CAST(N'2026-10-03T03:44:07.1298844' AS DateTime2), CAST(N'2026-09-26T09:57:01.9203123' AS DateTime2), CAST(N'2026-09-26T03:44:07.1298850' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (108, 1, N'E0FE6F9EC62CB51F303A588C3C68110AF6185A5CB7E0FBF5621F3AED4F21F0F9', CAST(N'2026-10-03T06:04:58.9316604' AS DateTime2), NULL, CAST(N'2026-09-26T06:04:58.9316820' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (109, 1, N'C87CF7833168322429CA937B7F1E124BD998EA864AA24BB1720F5F9EDD33AD38', CAST(N'2026-10-03T06:23:25.8650266' AS DateTime2), NULL, CAST(N'2026-09-26T06:23:25.8650428' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (110, 1, N'C3668FA770D824BBAE0FF97C7AD2E57DBB21D4CEA1DC765BDA1C94879EB6999D', CAST(N'2026-10-03T09:57:01.9203123' AS DateTime2), NULL, CAST(N'2026-09-26T09:57:01.9203123' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (111, 1, N'19F8D2CCE21EC94E904825405E1525FF0B3AEC424362297B0AE75685C32B0EE3', CAST(N'2026-10-03T09:57:18.3494686' AS DateTime2), NULL, CAST(N'2026-09-26T09:57:18.3494693' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (112, 1, N'1ED2BF8AF563943B05F2897C94F7F3543C05BF5D5913307F8D93FDE396EC2AB6', CAST(N'2026-10-03T10:34:33.3827582' AS DateTime2), NULL, CAST(N'2026-09-26T10:34:33.3828091' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (113, 1, N'330D747326A4E02D0C338844E3C6B15DCBAE101F16A20119C76F9780E4FAC0EC', CAST(N'2026-10-03T10:59:19.6911081' AS DateTime2), NULL, CAST(N'2026-09-26T10:59:19.6911255' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (114, 1, N'5A35EAD6F4F39C6B19A8688D029C7C9E82895679AECB58780EB2680290988F86', CAST(N'2026-10-03T16:20:32.5367776' AS DateTime2), NULL, CAST(N'2026-09-26T16:20:32.5367934' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (115, 1, N'C61D53D8311F542B12D8AF4F9C2305E628006CFCFF1D26E0536E9F7341156C55', CAST(N'2026-10-03T16:29:37.5074254' AS DateTime2), CAST(N'2026-09-26T17:52:51.4404911' AS DateTime2), CAST(N'2026-09-26T16:29:37.5074265' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (116, 1, N'F0414123C1DDAF2B46CB00AD5971414082CD63465E186AC7485D338D47BB2B23', CAST(N'2026-10-03T17:47:09.2876480' AS DateTime2), NULL, CAST(N'2026-09-26T17:47:09.2876934' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (117, 1, N'C364BC1F2D5A95AECFB44E993D2E364BD6655DFC8395EAB8C05D85E2043933AB', CAST(N'2026-10-03T17:52:51.4404911' AS DateTime2), NULL, CAST(N'2026-09-26T17:52:51.4404911' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (118, 1, N'D56A8C425A596C36563CDBE700D31C0C10855F388146305480901B966EF2290B', CAST(N'2026-10-04T04:41:06.4732314' AS DateTime2), NULL, CAST(N'2026-09-27T04:41:06.4732521' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (119, 1, N'70EFC9A788960BC741AAFF771617837A81D770995DE25D9BE163D8F42A24EA55', CAST(N'2026-10-04T04:49:36.0769450' AS DateTime2), CAST(N'2026-09-27T07:13:40.4119678' AS DateTime2), CAST(N'2026-09-27T04:49:36.0769457' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (120, 1, N'F32EA8E68425DDB3A29A85A619C98C8E6308EE33E5FF39C982445083B7C96193', CAST(N'2026-10-04T05:41:01.3068040' AS DateTime2), NULL, CAST(N'2026-09-27T05:41:01.3068046' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (121, 1, N'4A779F240EED7FA57E820CEF5A6DB048F72C97C6CE754555FA7247B63DF456B7', CAST(N'2026-10-04T07:13:40.4119678' AS DateTime2), CAST(N'2026-09-27T09:19:26.3093078' AS DateTime2), CAST(N'2026-09-27T07:13:40.4119678' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (122, 1, N'5082E9A7FFE5B455F72EEA8241DFBF8E44FB968B488744B6F6614E323FB9178E', CAST(N'2026-10-04T09:19:26.3093078' AS DateTime2), NULL, CAST(N'2026-09-27T09:19:26.3093078' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (123, 1, N'5EC70579F74D43CF093680C20A3EFA0234FCE96776CFE72C438CAB7D432842BB', CAST(N'2026-10-04T09:20:33.3540540' AS DateTime2), NULL, CAST(N'2026-09-27T09:20:33.3540546' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (124, 1, N'D4E2800C0D5F0DDA16D00B58535D233EDC0D62F4EA8C34BCB5F7CE1CAB63E110', CAST(N'2026-10-04T16:50:58.8061666' AS DateTime2), NULL, CAST(N'2026-09-27T16:50:58.8061856' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (125, 1, N'2ADB5E7834CC0E02D9334E4DE3817322CC53F91B0E300C12E0FA90C4C1945766', CAST(N'2026-10-04T17:09:50.5134972' AS DateTime2), NULL, CAST(N'2026-09-27T17:09:50.5135169' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (126, 1, N'943AAF0E3AEC9613E06E51E9872717F6BC6D25930456A4594C75EBF56BAFEF3C', CAST(N'2026-10-04T18:25:37.1773631' AS DateTime2), NULL, CAST(N'2026-09-27T18:25:37.1773942' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (127, 1, N'3C6B12BEF6B199DE8BA25296E422F106DB8AD7F78BE930014798120D8D6BEAAF', CAST(N'2026-10-05T03:52:05.1639652' AS DateTime2), CAST(N'2026-09-28T05:05:36.2293322' AS DateTime2), CAST(N'2026-09-28T03:52:05.1639980' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (128, 1, N'06C91DF58B81DE758DEF1B794D23F0A88D1D0D041CA7A261E455F8BE2992C13F', CAST(N'2026-10-05T05:05:36.2293322' AS DateTime2), CAST(N'2026-09-28T06:27:26.5866272' AS DateTime2), CAST(N'2026-09-28T05:05:36.2293322' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (129, 1, N'C63DAAD4A60682566BCCD7532E198883510293C30E4EBDB8533B78CC7B4C98BD', CAST(N'2026-10-05T05:57:39.6976722' AS DateTime2), NULL, CAST(N'2026-09-28T05:57:39.6976875' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (130, 1, N'1CDC7BBC4D0DD25C45D54E0E367CF5B89DADC5E2A11996DD15D67DDA5B2E579A', CAST(N'2026-10-05T06:26:01.8232334' AS DateTime2), NULL, CAST(N'2026-09-28T06:26:01.8232489' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (131, 1, N'69F9816C27555A7EB699103D4A4C5030A20625C950C91DA17E9A15B425E324B7', CAST(N'2026-10-05T06:27:26.5866272' AS DateTime2), NULL, CAST(N'2026-09-28T06:27:26.5866272' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (132, 1, N'1A40CC241EC7FD268391D1CABD672A4A11C18E792E3EC9A3C6440FB84BE36184', CAST(N'2026-10-05T07:57:40.2144809' AS DateTime2), NULL, CAST(N'2026-09-28T07:57:40.2144988' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (133, 1, N'4201D8C55F97E73312A1E1D94ABC870300A4010ABC0B66AFBF8B744D77D1ACAE', CAST(N'2026-10-05T08:38:05.1431658' AS DateTime2), NULL, CAST(N'2026-09-28T08:38:05.1431853' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (134, 1, N'4F59795FC5C34613AF33531DCC66290BCA8A2BEEE38B528DB3270197D90806DF', CAST(N'2026-10-05T08:39:53.0323750' AS DateTime2), NULL, CAST(N'2026-09-28T08:39:53.0323754' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (135, 9, N'1EF0850624F6FB4AE0E58FDB766389BD0E0C2C6E3E2500114CBA1FDBAD1009FF', CAST(N'2026-10-05T08:40:27.3146513' AS DateTime2), NULL, CAST(N'2026-09-28T08:40:27.3146518' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (136, 9, N'A02B8FBA8C0F44CF1E3D2E28D9F1240A0755EE2F8746D1ECD58CF75B7326DAF5', CAST(N'2026-10-05T15:23:52.2747679' AS DateTime2), NULL, CAST(N'2026-09-28T15:23:52.2747849' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (137, 9, N'11A02CA17CB7744DE4FBD1B28A27537FB820E95DC7119455F8E401D307DDC9E0', CAST(N'2026-10-05T15:54:00.5622894' AS DateTime2), NULL, CAST(N'2026-09-28T15:54:00.5623107' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (138, 9, N'904556C4CB6F5464B5C5182A7972441508A4E642C40E8C0164E17A5FDD45DA48', CAST(N'2026-10-05T16:00:12.4365412' AS DateTime2), NULL, CAST(N'2026-09-28T16:00:12.4365418' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (139, 9, N'673159918684D5A1F1020365EF7C5CF287703A7FDA432CDE69BA0C26E3AFE2B0', CAST(N'2026-10-05T16:16:36.6266457' AS DateTime2), NULL, CAST(N'2026-09-28T16:16:36.6266660' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (140, 9, N'40459E90EBA461AA7702EE8F790E0F314F41DD3435F81A0B09E4386C26A04FFD', CAST(N'2026-10-05T17:22:01.8047703' AS DateTime2), NULL, CAST(N'2026-09-28T17:22:01.8047871' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (141, 9, N'8CB8FD1592CEBCA8D76991D81232FBF88A44B6E0B9CD79AC60591F8261B765E6', CAST(N'2026-10-06T07:40:54.0746086' AS DateTime2), NULL, CAST(N'2026-09-29T07:40:54.0746251' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (142, 9, N'6D03C978C5ECA99FCD6741202B79BD909E04275343A23D5A284AD72EB19115B4', CAST(N'2026-10-06T08:18:34.8527704' AS DateTime2), NULL, CAST(N'2026-09-29T08:18:34.8528256' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (143, 4, N'51BE6547BE39B0EFDA61E396F05BA56EC4D21D9E6D600AD7273C7DDA06962794', CAST(N'2026-10-06T08:20:27.9604177' AS DateTime2), NULL, CAST(N'2026-09-29T08:20:27.9604187' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (144, 9, N'05E55FD8032B315849366546955BDF7441B8769B1988686EBD758445BA996839', CAST(N'2026-10-06T08:21:09.7860101' AS DateTime2), NULL, CAST(N'2026-09-29T08:21:09.7860108' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (145, 9, N'BE8A5EBABE473B638DAD276A9E6E9856E73B084B59A3FAC25603C0C6AEE09D49', CAST(N'2026-10-06T08:24:32.2776154' AS DateTime2), NULL, CAST(N'2026-09-29T08:24:32.2776164' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (146, 1, N'49B2577834AD0D14DD080155D1A30A9DB72DC819D6C32797E945B1C095218897', CAST(N'2026-10-06T08:31:57.4352959' AS DateTime2), NULL, CAST(N'2026-09-29T08:31:57.4352969' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (147, 9, N'5CAC9001711EE21B32E1B3A162ACA2B57123012629C9A6FF187DC57DC8FC6E39', CAST(N'2026-10-06T08:34:15.6516560' AS DateTime2), NULL, CAST(N'2026-09-29T08:34:15.6516569' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (148, 9, N'D25D213CC3AC75DEC798C70D1DB89B024BCE21B6A7AA631022404CF5FBDDA7E7', CAST(N'2026-10-06T08:37:40.9898751' AS DateTime2), NULL, CAST(N'2026-09-29T08:37:40.9898760' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (149, 9, N'318CCEA016C88E38733F80CC212DE413238E41EEE10477897659F1371C7D7355', CAST(N'2026-10-06T08:47:41.8493722' AS DateTime2), NULL, CAST(N'2026-09-29T08:47:41.8494253' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (150, 9, N'7526EDB90715021007780BE714EC082A4C227A2FB5F536064D3DAA07DD6099D5', CAST(N'2026-10-06T13:24:23.2647905' AS DateTime2), CAST(N'2026-09-29T14:37:12.2164692' AS DateTime2), CAST(N'2026-09-29T13:24:23.2648391' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (151, 1, N'52EBAE1FEA809E052D419A91D4C6CF486F733C0C713E8AF201F0A2CACE625184', CAST(N'2026-10-06T14:08:19.3596544' AS DateTime2), CAST(N'2026-09-29T16:42:49.6036473' AS DateTime2), CAST(N'2026-09-29T14:08:19.3596849' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (152, 9, N'CD865DF840FD1D03D44018995AF9214648ED28C90A7EB24E1E3E7A365D997827', CAST(N'2026-10-06T14:37:12.2164692' AS DateTime2), CAST(N'2026-09-29T15:41:04.2728794' AS DateTime2), CAST(N'2026-09-29T14:37:12.2164692' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (153, 9, N'8D5DCB599DCE1EC0C866821AAE15DD27E0DD39EE8F573E8105291840CD567E81', CAST(N'2026-10-06T15:41:04.2728794' AS DateTime2), NULL, CAST(N'2026-09-29T15:41:04.2728794' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (154, 3, N'A7C288B06AA5D67C4F79E60FF3A877F1DDE5DB8FCCF94A4BD3678221B1A7A727', CAST(N'2026-10-06T16:10:18.9008384' AS DateTime2), NULL, CAST(N'2026-09-29T16:10:18.9008585' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (155, 1, N'26B8DF09367C9ACC4B2078380C23591518238E99441750D295AB0C9AB769FC14', CAST(N'2026-10-06T16:10:33.4068274' AS DateTime2), NULL, CAST(N'2026-09-29T16:10:33.4068297' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (156, 9, N'266ACC1A1FA38FD198B58F71091F0549E03361FF01011A4F929C6C0705BC3A1A', CAST(N'2026-10-06T16:10:53.1027854' AS DateTime2), NULL, CAST(N'2026-09-29T16:10:53.1027861' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (157, 1, N'761A16532ADF54348B2C5D51850C61325F0B5AFB09918B637F3E17203F394759', CAST(N'2026-10-06T16:12:44.0218352' AS DateTime2), NULL, CAST(N'2026-09-29T16:12:44.0218360' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (158, 9, N'1BF8FA73CF813161F5A154C07A1E09B5B143CA752313EE34F28919660B323761', CAST(N'2026-10-06T16:14:28.4598392' AS DateTime2), NULL, CAST(N'2026-09-29T16:14:28.4598406' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (159, 1, N'C3B2D5AF8BA9B9039C61F905BC275ED56EA1A2B35D5AE9B0B1B2BEC20CEBAE5B', CAST(N'2026-10-06T16:42:49.6036473' AS DateTime2), NULL, CAST(N'2026-09-29T16:42:49.6036473' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (160, 9, N'2540A1632A3DD209217808E4A51A94CF296EC3A2E1E672ADCB4A2823B9E233D5', CAST(N'2026-10-06T17:16:52.8528049' AS DateTime2), NULL, CAST(N'2026-09-29T17:16:52.8528266' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (161, 9, N'250CCB54F312B7C36C034A8EC5A23702B7459C597F54650FA78C14551141036B', CAST(N'2026-10-07T03:38:10.1949359' AS DateTime2), NULL, CAST(N'2026-09-30T03:38:10.1949520' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (162, 9, N'5BE123D4D494ED0DECB48CB2006EEB7D63EC8E6B3F79E634E9CBE00FF6493B41', CAST(N'2026-10-07T04:41:05.4129408' AS DateTime2), NULL, CAST(N'2026-09-30T04:41:05.4129593' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (163, 9, N'50D34B89EF7F6E81B66C9B9B7B3387F1B79745894F2D727D27D778CCB5E081E0', CAST(N'2026-10-07T05:15:59.5964304' AS DateTime2), NULL, CAST(N'2026-09-30T05:15:59.5964558' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (164, 9, N'9DE1C2618185BB30C99702165BD52AA8A1B821FD2115265E19F73FF8029BD582', CAST(N'2026-10-07T06:59:25.3478795' AS DateTime2), NULL, CAST(N'2026-09-30T06:59:25.3478968' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (165, 1, N'6FABE85C1A6C2A9D8FC47F73C6ED8E1500581204DF8DA534E8FA1ADA3459F004', CAST(N'2026-10-07T07:01:31.4940769' AS DateTime2), NULL, CAST(N'2026-09-30T07:01:31.4940778' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (166, 9, N'6C60D9B99D8D74A8B8213B9052637FF8E3790FA6FECF4E6A7CC3233328CE8740', CAST(N'2026-10-07T07:11:09.4946653' AS DateTime2), NULL, CAST(N'2026-09-30T07:11:09.4946660' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (167, 1, N'421883B6CA2A98EB1BA3167D6CE83F98AEF9573D6F87521399A39FB34D4B7CF0', CAST(N'2026-10-07T07:11:23.7769553' AS DateTime2), NULL, CAST(N'2026-09-30T07:11:23.7769560' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (168, 9, N'A3A1C77FB09E353B0C24D86142D5974F3C3917F60139F11B7B92E4AF880ED4E2', CAST(N'2026-10-07T07:12:41.3328852' AS DateTime2), CAST(N'2026-09-30T08:20:05.8299041' AS DateTime2), CAST(N'2026-09-30T07:12:41.3328863' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (169, 1, N'008A199C19B299C098CF9434193AF622F6F5FD3CE9AED4C12D653B1E81C8F536', CAST(N'2026-10-07T07:16:07.6566174' AS DateTime2), NULL, CAST(N'2026-09-30T07:16:07.6566190' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (170, 1, N'28527303C3208507EF705668D47273DF4BED62FD477D9602575179D854CD6735', CAST(N'2026-10-07T08:12:42.7225538' AS DateTime2), NULL, CAST(N'2026-09-30T08:12:42.7226085' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (171, 1, N'E42D72932784C2162E150412CD0A4EEEFCC923465BF1858B46CB0B3999B50DD6', CAST(N'2026-10-07T08:12:58.9875336' AS DateTime2), NULL, CAST(N'2026-09-30T08:12:58.9875344' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (172, 1, N'C68492270A21A6AF05835666A3420DE9B1CE17892D035DB0FFD6CF48940218D8', CAST(N'2026-10-07T08:18:02.1550800' AS DateTime2), NULL, CAST(N'2026-09-30T08:18:02.1550835' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (173, 1, N'61FBF97E214B9DE82C31FF9565DB71F449A2FB06FAD28367A4ADFE9FB672694E', CAST(N'2026-10-07T08:18:10.0976271' AS DateTime2), NULL, CAST(N'2026-09-30T08:18:10.0976280' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (174, 9, N'E4DA10874E8CCA3B9081D36FE3CF47BC4C6619DB9877D6044E7FC201666B1902', CAST(N'2026-10-07T08:20:05.8299041' AS DateTime2), NULL, CAST(N'2026-09-30T08:20:05.8299041' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (175, 9, N'0B5F79529E3F46749FEE8B405243042186E20CCAADBE063256D7A9E612C58EE7', CAST(N'2026-10-07T09:48:23.4132460' AS DateTime2), NULL, CAST(N'2026-09-30T09:48:23.4132610' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (176, 9, N'892F30C3F28D69454196B540911128D51F2FBAFF8BFE9EF78922740B72C4CED9', CAST(N'2026-10-07T10:17:40.4700920' AS DateTime2), NULL, CAST(N'2026-09-30T10:17:40.4701075' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (177, 9, N'E21D3B9E209910628F4538E0200724225949D1770BCDD3B8C5BFA987D0692390', CAST(N'2026-10-07T15:27:56.1523309' AS DateTime2), NULL, CAST(N'2026-09-30T15:27:56.1523501' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (178, 9, N'9AAAF2BC49C1BA93334DB3174FC66CA2DD08343C6CCC7A1767B2D308ACDB0AF2', CAST(N'2026-10-07T16:24:10.2226873' AS DateTime2), NULL, CAST(N'2026-09-30T16:24:10.2227066' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (179, 9, N'F4B046916ACFC33405448EDCE840D65A2F40B468431BEB5B886292E0DDEC7509', CAST(N'2026-10-07T18:15:24.5149935' AS DateTime2), NULL, CAST(N'2026-09-30T18:15:24.5150168' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (180, 9, N'B0532C116D41BA8AB4B3183793C3D7E6FEBDC8F5729BBF449198757872AC0F7D', CAST(N'2026-10-07T18:21:14.2879026' AS DateTime2), NULL, CAST(N'2026-09-30T18:21:14.2879183' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (181, 9, N'035B1A41FE401D6AD5E1AB501A07475315AFC3347590F28DAA5A446636E7A740', CAST(N'2026-10-07T18:59:08.5908459' AS DateTime2), NULL, CAST(N'2026-09-30T18:59:08.5908644' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (182, 9, N'466CC250EA5F11D8E2D3CB20C8C6E1DCD0C877FD1A7C93B10578E87BB89C431B', CAST(N'2026-10-08T03:07:01.8911058' AS DateTime2), NULL, CAST(N'2026-10-01T03:07:01.8911267' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (183, 9, N'84E7DF835C766574C48D8B6E9C080C7F69649A584683996973DADF0DDEA6C9BE', CAST(N'2026-10-08T03:14:26.8462890' AS DateTime2), NULL, CAST(N'2026-10-01T03:14:26.8462897' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (184, 9, N'5C79CAA4622EDE00EF93F76D2645CBE1FC1440155E3EAD0F26430617AEDEF302', CAST(N'2026-10-08T03:40:16.9946742' AS DateTime2), NULL, CAST(N'2026-10-01T03:40:16.9946979' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (185, 9, N'A584545F7F13585FBDF498029AE3E68FE8C0B1D154FF6C597ADD4A652DF8F75F', CAST(N'2026-10-08T04:23:43.7903440' AS DateTime2), NULL, CAST(N'2026-10-01T04:23:43.7903603' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (186, 9, N'D5E31E7FCAD2E2E173EBB55E77169FCDA7BE9264DBB73D7C555737BD012DF730', CAST(N'2026-10-08T04:34:09.0829180' AS DateTime2), NULL, CAST(N'2026-10-01T04:34:09.0829334' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (187, 9, N'7DA26EC9ACF8CD122BF909B1B4CB07FE6A2EA448AF9C0F651FB800CEBCC8F1B3', CAST(N'2026-10-08T05:37:00.5542661' AS DateTime2), NULL, CAST(N'2026-10-01T05:37:00.5542848' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (188, 4, N'D85885810FF8066801294D6D26787D0DF1B7BADFE2BA466883E5CBAAFDA83B05', CAST(N'2026-10-08T05:37:29.3466532' AS DateTime2), NULL, CAST(N'2026-10-01T05:37:29.3466538' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (189, 1, N'F790F9D2F69713E2892A0AD4C20305272F7902618C20CED7A9DC7A30960D978C', CAST(N'2026-10-08T05:38:26.8226460' AS DateTime2), NULL, CAST(N'2026-10-01T05:38:26.8226466' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (190, 9, N'53912606EA2DCB41A962C81B548FA888E129DE4DB687BA926A34275F7675149F', CAST(N'2026-10-08T05:57:47.7447072' AS DateTime2), NULL, CAST(N'2026-10-01T05:57:47.7447269' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (191, 9, N'25FEC62C4A9469F2396D26DCD5C14FB9346304FA9B234CFDE33BC7C384425A1B', CAST(N'2026-10-08T06:12:15.5660219' AS DateTime2), CAST(N'2026-10-01T08:00:54.1383107' AS DateTime2), CAST(N'2026-10-01T06:12:15.5660644' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (192, 9, N'B56726CFE45931D602529C99C71ECAB2E44440B92D6D4B16B2BED2B7E1717AD4', CAST(N'2026-10-08T07:27:36.1815944' AS DateTime2), NULL, CAST(N'2026-10-01T07:27:36.1816106' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (193, 9, N'0A11FDDC6362A31F01FDBEAEC1682271D57B199EE7045D534114805CCF84E089', CAST(N'2026-10-08T08:00:54.1349546' AS DateTime2), NULL, CAST(N'2026-10-01T08:00:54.1349546' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (194, 9, N'E1D1EB0D4EF7F43EFA8B73BD6604660B6917F558AECF1B5046DBA292805656A0', CAST(N'2026-10-08T08:00:54.1383107' AS DateTime2), NULL, CAST(N'2026-10-01T08:00:54.1383107' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (195, 9, N'AC5E12F2952AD595B3DD9D0AE9544180085D484DB3F9DABAF01608D88D563E7B', CAST(N'2026-10-08T15:18:01.0654254' AS DateTime2), NULL, CAST(N'2026-10-01T15:18:01.0654453' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (196, 9, N'CCBEC82D3880625F5AA1ADD7ECA7FF34E00CF6091D05A91728540FCAEE2894AB', CAST(N'2026-10-08T15:21:53.5839353' AS DateTime2), CAST(N'2026-10-01T16:22:45.5957344' AS DateTime2), CAST(N'2026-10-01T15:21:53.5839786' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (197, 9, N'66807B7851E986109534A4F09697C7646BFF2BAA9D24DB591F48EB395700E862', CAST(N'2026-10-08T16:22:45.5957344' AS DateTime2), NULL, CAST(N'2026-10-01T16:22:45.5957344' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (198, 9, N'E15C58E991663AA8E7D4757674A94AB48478599C2F9C033FAB3B427144675FA9', CAST(N'2026-10-08T17:56:17.5268718' AS DateTime2), NULL, CAST(N'2026-10-01T17:56:17.5268952' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (199, 4, N'1FF26EB85738F64B8B8068D0C9775702F984A55F87BCED128AB8AA222A13B6C2', CAST(N'2026-10-08T17:56:48.1947186' AS DateTime2), NULL, CAST(N'2026-10-01T17:56:48.1947191' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (200, 1, N'6A97733AB22E07569DA3E9A7D4E132EC36F78183151B9480832906B2808318FD', CAST(N'2026-10-08T17:57:32.1087454' AS DateTime2), NULL, CAST(N'2026-10-01T17:57:32.1087463' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (201, 9, N'99D14A2C42571270BBB1A22C1E29328697EC2C2A71B794A210541AFD24CF30C5', CAST(N'2026-10-08T18:52:53.5835041' AS DateTime2), NULL, CAST(N'2026-10-01T18:52:53.5835208' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (202, 9, N'769D3CA6EE9D0A1272DBA41A583B01AF5525E2722C1DC95B40279EABBBBF1B39', CAST(N'2026-10-09T01:56:48.7391638' AS DateTime2), NULL, CAST(N'2026-10-02T01:56:48.7391816' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (203, 4, N'73C5C56F6E79EF10D8309155CB7EE78E8444CCE36932ECDE340585F894AD416C', CAST(N'2026-10-09T02:05:21.5830371' AS DateTime2), NULL, CAST(N'2026-10-02T02:05:21.5830379' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (204, 1, N'A59CEFC1FF288BA6686F82580A4912C81B3DDFC83AAECED3BFCF3264531B23AC', CAST(N'2026-10-09T02:05:49.3905042' AS DateTime2), NULL, CAST(N'2026-10-02T02:05:49.3905052' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (205, 9, N'A2CC001BCBB01201950483EEEF4721DD0E96229A6FE7AC6E8C6A0E53440EBFC4', CAST(N'2026-10-09T02:06:18.9485009' AS DateTime2), NULL, CAST(N'2026-10-02T02:06:18.9485017' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (206, 4, N'A95BDFDAF3C7ADAE35F6F076989CEC0371D69E487DF4A20CA24DC99F66CBBBF8', CAST(N'2026-10-09T02:12:12.1121052' AS DateTime2), NULL, CAST(N'2026-10-02T02:12:12.1121508' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (207, 1, N'0B2ABA9FEBB9BF989CD035CC6C27EACDED64D6C7CA10F6C0D80BE43F24178C2B', CAST(N'2026-10-09T02:12:35.0865833' AS DateTime2), NULL, CAST(N'2026-10-02T02:12:35.0865849' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (208, 9, N'E8F727AC9F652DEEFDE538D9FAE38DAAB0320755AB60FBC3A95D8E1EDD124DD4', CAST(N'2026-10-09T02:12:56.2255394' AS DateTime2), CAST(N'2026-10-02T06:37:19.1773977' AS DateTime2), CAST(N'2026-10-02T02:12:56.2255403' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (209, 9, N'B517899449A93D68E5CBA67E6997828B1313C463CA226CAC26B9D7F095066ADE', CAST(N'2026-10-09T03:23:21.0217170' AS DateTime2), NULL, CAST(N'2026-10-02T03:23:21.0217347' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (210, 13, N'043FCB480E8C8BF9CFD83F7A1914E5406BB7DBD96E2155AEF86A21AF5FE827BB', CAST(N'2026-10-09T03:23:53.6069220' AS DateTime2), NULL, CAST(N'2026-10-02T03:23:53.6069226' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (211, 13, N'B45E95DCB01046E1E244EA9EC960CE2F6903D361F5CA0C7A0ED4A18B48A18545', CAST(N'2026-10-09T04:11:58.7910372' AS DateTime2), NULL, CAST(N'2026-10-02T04:11:58.7910537' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (212, 13, N'611F952AEA9BBD92ACC8115D5A65CFD14AA95A9E73D7E684F0F97ABE055381DE', CAST(N'2026-10-09T06:08:16.0941489' AS DateTime2), NULL, CAST(N'2026-10-02T06:08:16.0941698' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (213, 13, N'741A6082D469B234125E1B3D94AC1582C72A3DA07A4CED0B8CB96A2896BAC86F', CAST(N'2026-10-09T06:19:46.8928195' AS DateTime2), NULL, CAST(N'2026-10-02T06:19:46.8928393' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (214, 9, N'25CBD0E5CC4E71743F93A78D32C14477598FB46B2DFCEEC875A8E8291250F5ED', CAST(N'2026-10-09T06:37:19.1773977' AS DateTime2), NULL, CAST(N'2026-10-02T06:37:19.1773977' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (215, 13, N'F961433E1BB88A9A50F9A1098A3D9BDC0D246B8B6B7582F673282C430CBAE6CF', CAST(N'2026-10-09T06:37:25.5751719' AS DateTime2), NULL, CAST(N'2026-10-02T06:37:25.5751725' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (216, 13, N'48BEA0FE10235B125B88519BFC4F4CB65F551FFFD2577658E5A152C0A60F6C20', CAST(N'2026-10-09T06:37:32.3912430' AS DateTime2), NULL, CAST(N'2026-10-02T06:37:32.3912438' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (217, 13, N'F4B4FF53E9234FFFDF91D7BDF607B099480F4CADA518E468CE3F38CA40D71D0F', CAST(N'2026-10-09T06:39:27.5736919' AS DateTime2), NULL, CAST(N'2026-10-02T06:39:27.5736931' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (218, 13, N'67F7FA0BBA7F7838E7622DE384E7618E67FD37F3F178E322AF2583E14FD7B7F8', CAST(N'2026-10-09T06:39:30.2949197' AS DateTime2), NULL, CAST(N'2026-10-02T06:39:30.2949202' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (219, 13, N'EE9B0DFCC8727DED43073E9FB3582B806E8307FC43273E8F9851841DD112854D', CAST(N'2026-10-09T06:40:03.6169104' AS DateTime2), NULL, CAST(N'2026-10-02T06:40:03.6169750' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (220, 13, N'3A1E846004484ECD2A6C958BA43E5BA2B1F0E4B2EE626230CB530465B2C482B9', CAST(N'2026-10-09T06:40:53.2524791' AS DateTime2), NULL, CAST(N'2026-10-02T06:40:53.2524800' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (221, 13, N'4A2EB8B40E0CC846FC28BCFA14F882A61E5B0E0BC60DB8E25D3015A19459AEE1', CAST(N'2026-10-09T06:42:16.3866623' AS DateTime2), NULL, CAST(N'2026-10-02T06:42:16.3866634' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (222, 13, N'6A0167696281865EADEFA20E5E1D0F4517414C6A9BA2E497B7D9B653F50F21C2', CAST(N'2026-10-09T06:42:21.3845687' AS DateTime2), NULL, CAST(N'2026-10-02T06:42:21.3845695' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (223, 13, N'A2B0B28C7CDB07D0F83E595CBE9AE1102264003A71E320C0AFC6D9162B21C74C', CAST(N'2026-10-09T06:42:23.5209042' AS DateTime2), NULL, CAST(N'2026-10-02T06:42:23.5209072' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (224, 13, N'4A3F62D14BE13E246A3E6CC0157779890AF908A9608D4FA949277518E46BDCD2', CAST(N'2026-10-09T06:43:02.8129768' AS DateTime2), NULL, CAST(N'2026-10-02T06:43:02.8130284' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (225, 13, N'57FC8010DFFD6756CF2FBEA5BD9F18D5051E514FA2566B3D5F4BB88AF0664F63', CAST(N'2026-10-09T06:43:44.8534147' AS DateTime2), NULL, CAST(N'2026-10-02T06:43:44.8534154' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (226, 13, N'AF7895272CEB6F6F0872C30459E956BF3F0A8AE32B21C733230D182AE539C234', CAST(N'2026-10-09T06:45:22.3088729' AS DateTime2), NULL, CAST(N'2026-10-02T06:45:22.3088738' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (227, 13, N'1D9B04CEC68F047D87F7CD6E2CF6D06460B0DC5AC7EC5B3AB96E723977E2377F', CAST(N'2026-10-09T06:52:11.3821706' AS DateTime2), CAST(N'2026-10-02T07:54:32.5231095' AS DateTime2), CAST(N'2026-10-02T06:52:11.3821714' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (228, 13, N'5EE0CAE58AC4B940E832D673982E970A419E549305138C76C495F2DEF0C74115', CAST(N'2026-10-09T07:06:33.6057024' AS DateTime2), NULL, CAST(N'2026-10-02T07:06:33.6057228' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (229, 13, N'B6DE8C0184FDD513E90641A17ED082FE43E7297308DE1B0A0C01AC3A5FBDBD62', CAST(N'2026-10-09T07:54:32.5231095' AS DateTime2), NULL, CAST(N'2026-10-02T07:54:32.5231095' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (230, 9, N'906E5A8630BE57130FFBE19BCA2BAFC5ABC0DEE5509CFED6BD21D76A0594E716', CAST(N'2026-10-09T07:55:20.2093416' AS DateTime2), NULL, CAST(N'2026-10-02T07:55:20.2093424' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (231, 9, N'9CFD94DCD7067B88E54E56DC18529ED10AC769409BF37350FA498369442DD382', CAST(N'2026-10-09T08:11:32.3957989' AS DateTime2), NULL, CAST(N'2026-10-02T08:11:32.3958196' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (232, 13, N'455F89786C44C0BE33E91845437041FF4AAD6D7CDBA00B08736A6C86AAB76A03', CAST(N'2026-10-09T08:58:42.6118681' AS DateTime2), NULL, CAST(N'2026-10-02T08:58:42.6118901' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (233, 13, N'FFA74B9956181CE9DC91F340FC46ED3C0C1A518EDDC90987B4E828B89CC047EA', CAST(N'2026-10-09T09:20:33.8939332' AS DateTime2), NULL, CAST(N'2026-10-02T09:20:33.8939560' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (234, 13, N'9CC9B5CB77A32433EED8971F0722F413DD20F063FA0568D0F79D8D474900EC71', CAST(N'2026-10-09T15:34:18.1237331' AS DateTime2), NULL, CAST(N'2026-10-02T15:34:18.1237818' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (235, 13, N'B4070B85B1B9FE38C6AB9896F262600F4C7D10A7784E8E6A88A60F9223CD2F77', CAST(N'2026-10-09T15:50:22.9319582' AS DateTime2), CAST(N'2026-10-02T16:54:25.0438206' AS DateTime2), CAST(N'2026-10-02T15:50:22.9320216' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (236, 13, N'019B213EEAC6E03BAB5A22678BD49AB23044B45ACD1AD9F997BACEA500DB6FE0', CAST(N'2026-10-09T16:48:47.6797406' AS DateTime2), NULL, CAST(N'2026-10-02T16:48:47.6797597' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (237, 13, N'DA09AE0286F2D6A4F78F1DDA85F89069658EC1100E655899CA91D775895B5C58', CAST(N'2026-10-09T16:54:25.0438206' AS DateTime2), NULL, CAST(N'2026-10-02T16:54:25.0438206' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (238, 13, N'FA155D5369B423BEA6632B432CBCC5DBD3A80E675F34AE8BEA08698784FD0026', CAST(N'2026-10-09T17:07:38.4927104' AS DateTime2), NULL, CAST(N'2026-10-02T17:07:38.4927561' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (239, 13, N'CD38C1856000C1A300FED781D7BE0783AEF75FD00C6354091A182819A9EA5921', CAST(N'2026-10-09T17:24:31.5575928' AS DateTime2), NULL, CAST(N'2026-10-02T17:24:31.5576107' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (240, 13, N'BCBEC7FC2D172E95E5492A2FB70DEDDF96991AD9124C54EAB781A0EF96E875A1', CAST(N'2026-10-09T17:43:16.4581224' AS DateTime2), NULL, CAST(N'2026-10-02T17:43:16.4581695' AS DateTime2), NULL, NULL, NULL)
GO
INSERT [auth].[refresh_tokens] ([id], [user_id], [token_hash], [expires_at], [revoked_at], [created_at], [device_info], [ip_address], [replaced_by_token_hash]) VALUES (241, 13, N'97175985E729F07DB39305AE22D88CE27096062026BF3867DE3F7C72AF63893C', CAST(N'2026-10-09T17:55:33.4046134' AS DateTime2), NULL, CAST(N'2026-10-02T17:55:33.4046319' AS DateTime2), NULL, NULL, NULL)
GO
SET IDENTITY_INSERT [auth].[refresh_tokens] OFF
GO
SET IDENTITY_INSERT [auth].[roles] ON 
GO
INSERT [auth].[roles] ([id], [name], [description], [is_system]) VALUES (1, N'Admin', N'Quản trị viên', 1)
GO
INSERT [auth].[roles] ([id], [name], [description], [is_system]) VALUES (2, N'Receptionist', N'Lễ tân', 1)
GO
INSERT [auth].[roles] ([id], [name], [description], [is_system]) VALUES (3, N'Doctor', N'Bác sĩ', 1)
GO
INSERT [auth].[roles] ([id], [name], [description], [is_system]) VALUES (4, N'Patient', N'Bệnh nhân', 1)
GO
INSERT [auth].[roles] ([id], [name], [description], [is_system]) VALUES (5, N'Pharmacist', N'Dược sĩ', 1)
GO
SET IDENTITY_INSERT [auth].[roles] OFF
GO
SET IDENTITY_INSERT [auth].[user_roles] ON 
GO
INSERT [auth].[user_roles] ([id], [user_id], [role_id], [assigned_at], [assigned_by]) VALUES (2, 2, 4, CAST(N'2026-09-10T08:52:06.0449393' AS DateTime2), NULL)
GO
INSERT [auth].[user_roles] ([id], [user_id], [role_id], [assigned_at], [assigned_by]) VALUES (3, 3, 3, CAST(N'2026-09-11T17:43:30.0713341' AS DateTime2), NULL)
GO
INSERT [auth].[user_roles] ([id], [user_id], [role_id], [assigned_at], [assigned_by]) VALUES (4, 4, 4, CAST(N'2026-09-16T06:54:03.4027152' AS DateTime2), NULL)
GO
INSERT [auth].[user_roles] ([id], [user_id], [role_id], [assigned_at], [assigned_by]) VALUES (5, 5, 4, CAST(N'2026-09-16T07:08:47.5443033' AS DateTime2), NULL)
GO
INSERT [auth].[user_roles] ([id], [user_id], [role_id], [assigned_at], [assigned_by]) VALUES (6, 6, 4, CAST(N'2026-09-16T07:13:29.0709607' AS DateTime2), NULL)
GO
INSERT [auth].[user_roles] ([id], [user_id], [role_id], [assigned_at], [assigned_by]) VALUES (7, 7, 3, CAST(N'2026-09-17T22:34:00.5733333' AS DateTime2), NULL)
GO
INSERT [auth].[user_roles] ([id], [user_id], [role_id], [assigned_at], [assigned_by]) VALUES (8, 8, 3, CAST(N'2026-09-17T22:34:00.5766667' AS DateTime2), NULL)
GO
INSERT [auth].[user_roles] ([id], [user_id], [role_id], [assigned_at], [assigned_by]) VALUES (9, 9, 3, CAST(N'2026-09-17T22:34:00.5800000' AS DateTime2), NULL)
GO
INSERT [auth].[user_roles] ([id], [user_id], [role_id], [assigned_at], [assigned_by]) VALUES (10, 10, 3, CAST(N'2026-09-17T22:34:00.5800000' AS DateTime2), NULL)
GO
INSERT [auth].[user_roles] ([id], [user_id], [role_id], [assigned_at], [assigned_by]) VALUES (11, 11, 3, CAST(N'2026-09-17T22:34:00.5800000' AS DateTime2), NULL)
GO
INSERT [auth].[user_roles] ([id], [user_id], [role_id], [assigned_at], [assigned_by]) VALUES (12, 12, 4, CAST(N'2026-09-24T04:24:25.5785571' AS DateTime2), NULL)
GO
INSERT [auth].[user_roles] ([id], [user_id], [role_id], [assigned_at], [assigned_by]) VALUES (13, 1, 2, CAST(N'2026-09-24T05:52:29.4610175' AS DateTime2), NULL)
GO
INSERT [auth].[user_roles] ([id], [user_id], [role_id], [assigned_at], [assigned_by]) VALUES (14, 13, 5, CAST(N'2026-10-02T10:00:03.6800000' AS DateTime2), NULL)
GO
SET IDENTITY_INSERT [auth].[user_roles] OFF
GO
SET IDENTITY_INSERT [auth].[users] ON 
GO
INSERT [auth].[users] ([id], [username], [password_hash], [email], [full_name], [phone], [is_active], [created_at], [updated_at], [last_login_at], [failed_login_count], [lockout_end], [email_confirmed], [avatar_url], [is_deleted], [deleted_at]) VALUES (1, N'0901111111', N'$2a$12$OpSzqB8Ra8P29dvMFPWZXeTtmiqe.MusBGuuWvTNOBBWDY76.LxTO', N'lieunguyen10122004@gmail.com', N'Anh Nguyen', N'0901111111', 1, CAST(N'2026-09-10T08:50:11.5845637' AS DateTime2), CAST(N'2026-09-24T04:33:09.8198504' AS DateTime2), CAST(N'2026-10-02T02:12:35.0887032' AS DateTime2), 0, NULL, 0, NULL, 0, NULL)
GO
INSERT [auth].[users] ([id], [username], [password_hash], [email], [full_name], [phone], [is_active], [created_at], [updated_at], [last_login_at], [failed_login_count], [lockout_end], [email_confirmed], [avatar_url], [is_deleted], [deleted_at]) VALUES (2, N'0902222222', N'$2a$12$MKssp6sohv2WoIgdjFbsJu0DXM5tK/moBLovAwEAYyIrGj5BF1a12', N'taikhoang@test.com', N'Trần Tài Khoản', N'0902222222', 1, CAST(N'2026-09-10T08:52:06.0449393' AS DateTime2), NULL, NULL, 0, NULL, 0, NULL, 0, NULL)
GO
INSERT [auth].[users] ([id], [username], [password_hash], [email], [full_name], [phone], [is_active], [created_at], [updated_at], [last_login_at], [failed_login_count], [lockout_end], [email_confirmed], [avatar_url], [is_deleted], [deleted_at]) VALUES (3, N'0903333333', N'$2a$12$NvNo3E.FV0t8crxYfkE3V.HJoYjs4.LZHT929ig8mEs7hsDY.Dm2a', N'khanhvy0946265560@gmail.com', N'Nguyễn Văn Bác Sĩ', N'0903333333', 1, CAST(N'2026-09-11T17:43:30.0713341' AS DateTime2), CAST(N'2026-09-16T16:43:25.6500887' AS DateTime2), CAST(N'2026-09-29T16:10:18.9375639' AS DateTime2), 0, NULL, 0, NULL, 0, NULL)
GO
INSERT [auth].[users] ([id], [username], [password_hash], [email], [full_name], [phone], [is_active], [created_at], [updated_at], [last_login_at], [failed_login_count], [lockout_end], [email_confirmed], [avatar_url], [is_deleted], [deleted_at]) VALUES (4, N'0909999999', N'$2a$12$PS4BkNOqskav1G/Hq3MRMuGTGcEESWgREUEjPoV2JHm1F4zCVfb7G', N'nguyenplase184463@fpt.edu.vn', N'Lieu Nguyen', N'0909999999', 1, CAST(N'2026-09-16T06:54:03.4027152' AS DateTime2), NULL, CAST(N'2026-10-02T02:12:12.1714138' AS DateTime2), 0, NULL, 0, NULL, 0, NULL)
GO
INSERT [auth].[users] ([id], [username], [password_hash], [email], [full_name], [phone], [is_active], [created_at], [updated_at], [last_login_at], [failed_login_count], [lockout_end], [email_confirmed], [avatar_url], [is_deleted], [deleted_at]) VALUES (5, N'0901234567', N'$2a$12$Pmu2K6wC4LMO6n5md6L6rOTewN8hzYCJizy7YIH7eVM1UD/dttB.C', N'nguyenvantest@gmail.com', N'Nguyễn Văn Test', N'0901234567', 1, CAST(N'2026-09-16T07:08:47.5443033' AS DateTime2), NULL, NULL, 0, NULL, 0, NULL, 0, NULL)
GO
INSERT [auth].[users] ([id], [username], [password_hash], [email], [full_name], [phone], [is_active], [created_at], [updated_at], [last_login_at], [failed_login_count], [lockout_end], [email_confirmed], [avatar_url], [is_deleted], [deleted_at]) VALUES (6, N'0902222223', N'$2a$12$FCU8RBdXLnzObiYiMAnt2.eeHA3H1YfZCn8uRRZ70j3lv6rB5E1Qu', N'vytran0908@gmail.com', N'Vy Tran', N'0902222223', 1, CAST(N'2026-09-16T07:13:29.0709607' AS DateTime2), NULL, CAST(N'2026-09-20T15:16:43.2554624' AS DateTime2), 0, NULL, 0, NULL, 0, NULL)
GO
INSERT [auth].[users] ([id], [username], [password_hash], [email], [full_name], [phone], [is_active], [created_at], [updated_at], [last_login_at], [failed_login_count], [lockout_end], [email_confirmed], [avatar_url], [is_deleted], [deleted_at]) VALUES (7, N'doctor.card01', N'$2a$12$NvNo3E.FV0t8crxYfkE3V.HJoYjs4.LZHT929ig8mEs7hsDY.Dm2a', N'doctor.card01@clinic.local', N'Nguyễn Minh Khang', N'0910000001', 1, CAST(N'2026-09-17T22:34:00.5733333' AS DateTime2), NULL, NULL, 0, NULL, 1, NULL, 0, NULL)
GO
INSERT [auth].[users] ([id], [username], [password_hash], [email], [full_name], [phone], [is_active], [created_at], [updated_at], [last_login_at], [failed_login_count], [lockout_end], [email_confirmed], [avatar_url], [is_deleted], [deleted_at]) VALUES (8, N'doctor.card02', N'$2a$12$NvNo3E.FV0t8crxYfkE3V.HJoYjs4.LZHT929ig8mEs7hsDY.Dm2a', N'doctor.card02@clinic.local', N'Trần Hoàng Nam', N'0910000002', 1, CAST(N'2026-09-17T22:34:00.5766667' AS DateTime2), NULL, NULL, 0, NULL, 1, NULL, 0, NULL)
GO
INSERT [auth].[users] ([id], [username], [password_hash], [email], [full_name], [phone], [is_active], [created_at], [updated_at], [last_login_at], [failed_login_count], [lockout_end], [email_confirmed], [avatar_url], [is_deleted], [deleted_at]) VALUES (9, N'doctor.gen01', N'$2a$12$NvNo3E.FV0t8crxYfkE3V.HJoYjs4.LZHT929ig8mEs7hsDY.Dm2a', N'doctor.gen01@clinic.local', N'Lê Thu Hà', N'0910000003', 1, CAST(N'2026-09-17T22:34:00.5800000' AS DateTime2), NULL, CAST(N'2026-10-02T08:11:32.4305907' AS DateTime2), 0, NULL, 1, NULL, 0, NULL)
GO
INSERT [auth].[users] ([id], [username], [password_hash], [email], [full_name], [phone], [is_active], [created_at], [updated_at], [last_login_at], [failed_login_count], [lockout_end], [email_confirmed], [avatar_url], [is_deleted], [deleted_at]) VALUES (10, N'doctor.derm01', N'$2a$12$NvNo3E.FV0t8crxYfkE3V.HJoYjs4.LZHT929ig8mEs7hsDY.Dm2a', N'doctor.derm01@clinic.local', N'Phạm Ngọc Lan', N'0910000004', 1, CAST(N'2026-09-17T22:34:00.5800000' AS DateTime2), NULL, NULL, 0, NULL, 1, NULL, 0, NULL)
GO
INSERT [auth].[users] ([id], [username], [password_hash], [email], [full_name], [phone], [is_active], [created_at], [updated_at], [last_login_at], [failed_login_count], [lockout_end], [email_confirmed], [avatar_url], [is_deleted], [deleted_at]) VALUES (11, N'doctor.noi02', N'$2a$12$NvNo3E.FV0t8crxYfkE3V.HJoYjs4.LZHT929ig8mEs7hsDY.Dm2a', N'doctor.noi02@clinic.local', N'Vũ Quốc Huy', N'0910000005', 1, CAST(N'2026-09-17T22:34:00.5800000' AS DateTime2), NULL, NULL, 0, NULL, 1, NULL, 0, NULL)
GO
INSERT [auth].[users] ([id], [username], [password_hash], [email], [full_name], [phone], [is_active], [created_at], [updated_at], [last_login_at], [failed_login_count], [lockout_end], [email_confirmed], [avatar_url], [is_deleted], [deleted_at]) VALUES (12, N'0123454321', N'$2a$12$nO1ZcDd7aFJ5DA2LlfY14eS/8hW1AMYb1f.DmhWNg5YHUriu2rgj.', N'nguyenlieu10122004@gmail.com', N'Anh Nguyen', N'0123454321', 1, CAST(N'2026-09-24T04:24:25.5785571' AS DateTime2), NULL, CAST(N'2026-09-24T04:24:43.8112036' AS DateTime2), 0, NULL, 0, NULL, 0, NULL)
GO
INSERT [auth].[users] ([id], [username], [password_hash], [email], [full_name], [phone], [is_active], [created_at], [updated_at], [last_login_at], [failed_login_count], [lockout_end], [email_confirmed], [avatar_url], [is_deleted], [deleted_at]) VALUES (13, N'pharmacist01', N'$2a$12$NvNo3E.FV0t8crxYfkE3V.HJoYjs4.LZHT929ig8mEs7hsDY.Dm2a', N'pharmacist01@clinic.local', N'Dược sĩ Test', N'0910000010', 1, CAST(N'2026-10-02T10:00:03.6800000' AS DateTime2), NULL, CAST(N'2026-10-02T17:55:33.4453879' AS DateTime2), 0, NULL, 1, NULL, 0, NULL)
GO
SET IDENTITY_INSERT [auth].[users] OFF
GO
SET IDENTITY_INSERT [billing].[invoice_items] ON 
GO
INSERT [billing].[invoice_items] ([id], [invoice_id], [medicine_id], [description], [quantity], [unit_price], [amount], [medical_record_service_id], [discount_amount]) VALUES (4, 4, NULL, N'Xét nghiệm công thức máu', 1, CAST(150000.00 AS Decimal(12, 2)), CAST(150000.00 AS Decimal(12, 2)), 1, CAST(0.00 AS Decimal(12, 2)))
GO
INSERT [billing].[invoice_items] ([id], [invoice_id], [medicine_id], [description], [quantity], [unit_price], [amount], [medical_record_service_id], [discount_amount]) VALUES (5, 4, 1, N'Paracetamol 500mg', 20, CAST(2000.00 AS Decimal(12, 2)), CAST(40000.00 AS Decimal(12, 2)), NULL, CAST(0.00 AS Decimal(12, 2)))
GO
INSERT [billing].[invoice_items] ([id], [invoice_id], [medicine_id], [description], [quantity], [unit_price], [amount], [medical_record_service_id], [discount_amount]) VALUES (6, 8, 1, N'Paracetamol 500mg', 8, CAST(2000.00 AS Decimal(12, 2)), CAST(16000.00 AS Decimal(12, 2)), NULL, CAST(0.00 AS Decimal(12, 2)))
GO
INSERT [billing].[invoice_items] ([id], [invoice_id], [medicine_id], [description], [quantity], [unit_price], [amount], [medical_record_service_id], [discount_amount]) VALUES (7, 8, 1, N'Paracetamol 500mg', 8, CAST(2000.00 AS Decimal(12, 2)), CAST(16000.00 AS Decimal(12, 2)), NULL, CAST(0.00 AS Decimal(12, 2)))
GO
INSERT [billing].[invoice_items] ([id], [invoice_id], [medicine_id], [description], [quantity], [unit_price], [amount], [medical_record_service_id], [discount_amount]) VALUES (8, 10, 1, N'Paracetamol 500mg', 10, CAST(2000.00 AS Decimal(12, 2)), CAST(20000.00 AS Decimal(12, 2)), NULL, CAST(0.00 AS Decimal(12, 2)))
GO
SET IDENTITY_INSERT [billing].[invoice_items] OFF
GO
SET IDENTITY_INSERT [billing].[invoices] ON 
GO
INSERT [billing].[invoices] ([id], [patient_id], [appointment_id], [medical_record_id], [patient_name], [total_amount], [status], [created_by], [created_at], [updated_at], [invoice_no], [discount_amount], [tax_amount], [paid_amount], [insurance_amount], [cancelled_at], [cancel_reason]) VALUES (1, 5, 2, 1, N'Lieu Nguyen', CAST(190000.00 AS Decimal(12, 2)), 3, NULL, CAST(N'2026-09-23T16:54:27.0360996' AS DateTime2), CAST(N'2026-09-28T01:26:45.6652985' AS DateTime2), N'INV-TEST-0001', CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), CAST(N'2026-09-28T01:26:45.6651930' AS DateTime2), N'Bệnh nhân không tiếp tục sử dụng dịch vụ.')
GO
INSERT [billing].[invoices] ([id], [patient_id], [appointment_id], [medical_record_id], [patient_name], [total_amount], [status], [created_by], [created_at], [updated_at], [invoice_no], [discount_amount], [tax_amount], [paid_amount], [insurance_amount], [cancelled_at], [cancel_reason]) VALUES (2, 5, NULL, NULL, N'Lieu Nguyen', CAST(250000.00 AS Decimal(12, 2)), 2, NULL, CAST(N'2026-09-28T01:30:49.4600000' AS DateTime2), CAST(N'2026-09-28T12:55:32.2037230' AS DateTime2), N'INV-TEST-CANCEL-0001', CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), CAST(250000.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), NULL, NULL)
GO
INSERT [billing].[invoices] ([id], [patient_id], [appointment_id], [medical_record_id], [patient_name], [total_amount], [status], [created_by], [created_at], [updated_at], [invoice_no], [discount_amount], [tax_amount], [paid_amount], [insurance_amount], [cancelled_at], [cancel_reason]) VALUES (4, 5, NULL, NULL, N'Lieu Nguyen', CAST(190000.00 AS Decimal(12, 2)), 0, 1, CAST(N'2026-09-28T12:17:30.6100000' AS DateTime2), NULL, N'INV-TEST-CANCEL-20260928-473', CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), NULL, NULL)
GO
INSERT [billing].[invoices] ([id], [patient_id], [appointment_id], [medical_record_id], [patient_name], [total_amount], [status], [created_by], [created_at], [updated_at], [invoice_no], [discount_amount], [tax_amount], [paid_amount], [insurance_amount], [cancelled_at], [cancel_reason]) VALUES (5, 5, NULL, NULL, N'Lieu Nguyen', CAST(100000.00 AS Decimal(12, 2)), 3, 1, CAST(N'2026-09-28T12:29:31.8166667' AS DateTime2), CAST(N'2026-09-28T12:30:00.9246478' AS DateTime2), N'INV-TEST-CANCEL-20260928-885', CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), CAST(N'2026-09-28T12:30:00.9245835' AS DateTime2), N'Khach hang khong su dung dich vu nua')
GO
INSERT [billing].[invoices] ([id], [patient_id], [appointment_id], [medical_record_id], [patient_name], [total_amount], [status], [created_by], [created_at], [updated_at], [invoice_no], [discount_amount], [tax_amount], [paid_amount], [insurance_amount], [cancelled_at], [cancel_reason]) VALUES (6, 1, 9, 3, N'Nguyễn Văn Test', CAST(200000.00 AS Decimal(12, 2)), 0, 9, CAST(N'2026-09-30T10:38:29.0343849' AS DateTime2), NULL, N'INV4F490A1874334B739', CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), NULL, NULL)
GO
INSERT [billing].[invoices] ([id], [patient_id], [appointment_id], [medical_record_id], [patient_name], [total_amount], [status], [created_by], [created_at], [updated_at], [invoice_no], [discount_amount], [tax_amount], [paid_amount], [insurance_amount], [cancelled_at], [cancel_reason]) VALUES (7, 7, 27, 5, N'Anh Nguyen', CAST(200000.00 AS Decimal(12, 2)), 0, 9, CAST(N'2026-09-30T14:59:10.4148884' AS DateTime2), NULL, N'INV875DBA02C6C3424C8', CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), NULL, NULL)
GO
INSERT [billing].[invoices] ([id], [patient_id], [appointment_id], [medical_record_id], [patient_name], [total_amount], [status], [created_by], [created_at], [updated_at], [invoice_no], [discount_amount], [tax_amount], [paid_amount], [insurance_amount], [cancelled_at], [cancel_reason]) VALUES (8, 5, 30, 8, N'Lieu Nguyen', CAST(232000.00 AS Decimal(12, 2)), 0, 9, CAST(N'2026-10-02T01:52:00.0605648' AS DateTime2), NULL, N'INV54E7949239DB4CD68', CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), NULL, NULL)
GO
INSERT [billing].[invoices] ([id], [patient_id], [appointment_id], [medical_record_id], [patient_name], [total_amount], [status], [created_by], [created_at], [updated_at], [invoice_no], [discount_amount], [tax_amount], [paid_amount], [insurance_amount], [cancelled_at], [cancel_reason]) VALUES (9, 5, 31, 9, N'Lieu Nguyen', CAST(200000.00 AS Decimal(12, 2)), 0, 9, CAST(N'2026-10-02T09:09:42.1264415' AS DateTime2), NULL, N'INV07BD514BAA4D46299', CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), NULL, NULL)
GO
INSERT [billing].[invoices] ([id], [patient_id], [appointment_id], [medical_record_id], [patient_name], [total_amount], [status], [created_by], [created_at], [updated_at], [invoice_no], [discount_amount], [tax_amount], [paid_amount], [insurance_amount], [cancelled_at], [cancel_reason]) VALUES (10, 5, 32, 10, N'Lieu Nguyen', CAST(220000.00 AS Decimal(12, 2)), 0, 9, CAST(N'2026-10-02T09:14:31.1326808' AS DateTime2), NULL, N'INVD8B19AAE7A974D31B', CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), CAST(0.00 AS Decimal(12, 2)), NULL, NULL)
GO
SET IDENTITY_INSERT [billing].[invoices] OFF
GO
SET IDENTITY_INSERT [billing].[payments] ON 
GO
INSERT [billing].[payments] ([id], [invoice_id], [amount], [method], [paid_at], [note], [reference_code], [received_by], [is_refund]) VALUES (1, 1, CAST(100000.00 AS Decimal(12, 2)), 2, CAST(N'2026-09-23T16:54:27.0608324' AS DateTime2), N'Thanh toán thử nghiệm', N'BANK-TEST-001', NULL, 0)
GO
INSERT [billing].[payments] ([id], [invoice_id], [amount], [method], [paid_at], [note], [reference_code], [received_by], [is_refund]) VALUES (2, 1, CAST(10000.00 AS Decimal(12, 2)), 3, CAST(N'2026-09-27T16:29:57.5031165' AS DateTime2), N'Thu tiền lần 2', NULL, 1, 0)
GO
INSERT [billing].[payments] ([id], [invoice_id], [amount], [method], [paid_at], [note], [reference_code], [received_by], [is_refund]) VALUES (3, 1, CAST(10000.00 AS Decimal(12, 2)), 0, CAST(N'2026-09-27T23:53:05.3730531' AS DateTime2), N'Test thu tien', N'TEST-PAY-001', 1, 0)
GO
INSERT [billing].[payments] ([id], [invoice_id], [amount], [method], [paid_at], [note], [reference_code], [received_by], [is_refund]) VALUES (4, 1, CAST(70000.00 AS Decimal(12, 2)), 0, CAST(N'2026-09-27T23:58:51.7482453' AS DateTime2), N'Test thanh toan du', N'TEST-PAY-FULL', 1, 0)
GO
INSERT [billing].[payments] ([id], [invoice_id], [amount], [method], [paid_at], [note], [reference_code], [received_by], [is_refund]) VALUES (5, 1, CAST(-10000.00 AS Decimal(12, 2)), 0, CAST(N'2026-09-28T00:10:22.2220959' AS DateTime2), N'Test hoan tien', N'TEST-REFUND-001', 1, 1)
GO
INSERT [billing].[payments] ([id], [invoice_id], [amount], [method], [paid_at], [note], [reference_code], [received_by], [is_refund]) VALUES (6, 1, CAST(-180000.00 AS Decimal(12, 2)), 0, CAST(N'2026-09-28T00:32:52.5440318' AS DateTime2), N'Test refund toan bo', N'TEST-REFUND-FULL', 1, 1)
GO
INSERT [billing].[payments] ([id], [invoice_id], [amount], [method], [paid_at], [note], [reference_code], [received_by], [is_refund]) VALUES (7, 2, CAST(10000.00 AS Decimal(12, 2)), 0, CAST(N'2026-09-28T01:31:19.1941957' AS DateTime2), N'Test cancel invoice', N'TEST-CANCEL', 1, 0)
GO
INSERT [billing].[payments] ([id], [invoice_id], [amount], [method], [paid_at], [note], [reference_code], [received_by], [is_refund]) VALUES (8, 2, CAST(10000.00 AS Decimal(12, 2)), 0, CAST(N'2026-09-28T11:23:57.9169863' AS DateTime2), N'Thu tiền test frontend', N'FE-TEST-001', 1, 0)
GO
INSERT [billing].[payments] ([id], [invoice_id], [amount], [method], [paid_at], [note], [reference_code], [received_by], [is_refund]) VALUES (9, 2, CAST(230000.00 AS Decimal(12, 2)), 0, CAST(N'2026-09-28T11:24:46.4635554' AS DateTime2), N'Thu du tien', NULL, 1, 0)
GO
INSERT [billing].[payments] ([id], [invoice_id], [amount], [method], [paid_at], [note], [reference_code], [received_by], [is_refund]) VALUES (10, 2, CAST(-10000.00 AS Decimal(12, 2)), 0, CAST(N'2026-09-28T11:37:35.5414838' AS DateTime2), N'Test refund frontend', N'REFUND-FE-001', 1, 1)
GO
INSERT [billing].[payments] ([id], [invoice_id], [amount], [method], [paid_at], [note], [reference_code], [received_by], [is_refund]) VALUES (11, 2, CAST(10000.00 AS Decimal(12, 2)), 0, CAST(N'2026-09-28T12:55:32.2034700' AS DateTime2), NULL, NULL, 1, 0)
GO
SET IDENTITY_INSERT [billing].[payments] OFF
GO
SET IDENTITY_INSERT [billing].[services] ON 
GO
INSERT [billing].[services] ([id], [name], [description], [price], [is_active], [created_at], [updated_at], [code], [specialty_id], [duration_minutes]) VALUES (1, N'Xét nghiệm công thức máu', N'Dịch vụ xét nghiệm dùng để test VC-05.', CAST(150000.00 AS Decimal(12, 2)), 1, CAST(N'2026-09-23T05:49:52.9140029' AS DateTime2), NULL, N'LAB-CBC', NULL, 30)
GO
INSERT [billing].[services] ([id], [name], [description], [price], [is_active], [created_at], [updated_at], [code], [specialty_id], [duration_minutes]) VALUES (2, N'Xét nghiệm đường huyết', N'Xét nghiệm kiểm tra nồng độ glucose trong máu.', CAST(100000.00 AS Decimal(12, 2)), 1, CAST(N'2026-10-01T14:30:05.2466667' AS DateTime2), NULL, N'LAB-GLU', NULL, 30)
GO
INSERT [billing].[services] ([id], [name], [description], [price], [is_active], [created_at], [updated_at], [code], [specialty_id], [duration_minutes]) VALUES (3, N'Xét nghiệm chức năng gan', N'Xét nghiệm đánh giá chức năng gan.', CAST(250000.00 AS Decimal(12, 2)), 1, CAST(N'2026-10-01T14:30:05.2466667' AS DateTime2), NULL, N'LAB-LFT', NULL, 45)
GO
INSERT [billing].[services] ([id], [name], [description], [price], [is_active], [created_at], [updated_at], [code], [specialty_id], [duration_minutes]) VALUES (4, N'X-quang ngực thẳng', N'Chụp X-quang ngực thẳng.', CAST(180000.00 AS Decimal(12, 2)), 1, CAST(N'2026-10-01T14:30:05.2466667' AS DateTime2), NULL, N'IMG-CXR', NULL, 30)
GO
INSERT [billing].[services] ([id], [name], [description], [price], [is_active], [created_at], [updated_at], [code], [specialty_id], [duration_minutes]) VALUES (5, N'Siêu âm bụng tổng quát', N'Siêu âm kiểm tra các cơ quan trong ổ bụng.', CAST(250000.00 AS Decimal(12, 2)), 1, CAST(N'2026-10-01T14:30:05.2466667' AS DateTime2), NULL, N'IMG-USAB', NULL, 30)
GO
INSERT [billing].[services] ([id], [name], [description], [price], [is_active], [created_at], [updated_at], [code], [specialty_id], [duration_minutes]) VALUES (6, N'Điện tâm đồ', N'Đo và ghi lại hoạt động điện của tim.', CAST(120000.00 AS Decimal(12, 2)), 1, CAST(N'2026-10-01T14:30:05.2500000' AS DateTime2), NULL, N'CARD-ECG', NULL, 15)
GO
INSERT [billing].[services] ([id], [name], [description], [price], [is_active], [created_at], [updated_at], [code], [specialty_id], [duration_minutes]) VALUES (7, N'Dịch vụ test inactive', N'Dùng để kiểm tra API không trả về service đã ngừng hoạt động.', CAST(50000.00 AS Decimal(12, 2)), 0, CAST(N'2026-10-01T14:30:05.2500000' AS DateTime2), NULL, N'LAB-INACTIVE', NULL, 15)
GO
SET IDENTITY_INSERT [billing].[services] OFF
GO
SET IDENTITY_INSERT [clinical].[attachments] ON 
GO
INSERT [clinical].[attachments] ([id], [medical_record_id], [file_url], [file_type], [uploaded_at], [uploaded_by]) VALUES (1, 1, N'test-files/lab-result-cbc.pdf', N'application/pdf', CAST(N'2026-09-23T06:21:59.2655605' AS DateTime2), NULL)
GO
INSERT [clinical].[attachments] ([id], [medical_record_id], [file_url], [file_type], [uploaded_at], [uploaded_by]) VALUES (2, 2, N'/uploads/medical-records/2/ca4cf5bfed3a4ccaadd81a4880842f54.pdf', N'application/pdf', CAST(N'2026-09-30T18:16:52.0263813' AS DateTime2), 9)
GO
INSERT [clinical].[attachments] ([id], [medical_record_id], [file_url], [file_type], [uploaded_at], [uploaded_by]) VALUES (3, 2, N'/uploads/medical-records/2/f51696a191f34a55a85a165359348ad2.pdf', N'application/pdf', CAST(N'2026-09-30T18:17:27.1279378' AS DateTime2), 9)
GO
INSERT [clinical].[attachments] ([id], [medical_record_id], [file_url], [file_type], [uploaded_at], [uploaded_by]) VALUES (4, 2, N'/uploads/medical-records/2/a48d03c17c3340f19571cb0f507f0735.pdf', N'application/pdf', CAST(N'2026-09-30T18:22:17.9547683' AS DateTime2), 9)
GO
INSERT [clinical].[attachments] ([id], [medical_record_id], [file_url], [file_type], [uploaded_at], [uploaded_by]) VALUES (5, 7, N'/uploads/medical-records/7/e43ee5e624d24758b802695f9b2b582c.png', N'image/png', CAST(N'2026-10-01T16:21:03.3858279' AS DateTime2), 9)
GO
INSERT [clinical].[attachments] ([id], [medical_record_id], [file_url], [file_type], [uploaded_at], [uploaded_by]) VALUES (6, 7, N'/uploads/medical-records/7/84afca2bcd3e4de795edf08bd2df453f.png', N'image/png', CAST(N'2026-10-01T16:25:25.2846233' AS DateTime2), 9)
GO
SET IDENTITY_INSERT [clinical].[attachments] OFF
GO
SET IDENTITY_INSERT [clinical].[lab_results] ON 
GO
INSERT [clinical].[lab_results] ([id], [medical_record_service_id], [result_value], [reference_range], [conclusion], [resulted_at], [recorded_by], [created_at]) VALUES (1, 1, N'HGB 13.2 g/dL', N'12 - 16 g/dL', N'Kết quả trong giới hạn bình thường.', CAST(N'2026-09-23T13:18:50.2405892' AS DateTime2), NULL, CAST(N'2026-09-23T06:18:50.2405892' AS DateTime2))
GO
INSERT [clinical].[lab_results] ([id], [medical_record_service_id], [result_value], [reference_range], [conclusion], [resulted_at], [recorded_by], [created_at]) VALUES (2, 3, N'HGB 13.2 - WBC 7.4', N'HGB 12-16, WBC 4-10', N'Các chỉ số trong giới hạn bình thường.', CAST(N'2026-09-30T22:30:00.0000000' AS DateTime2), NULL, CAST(N'2026-09-30T16:41:55.5418820' AS DateTime2))
GO
INSERT [clinical].[lab_results] ([id], [medical_record_service_id], [result_value], [reference_range], [conclusion], [resulted_at], [recorded_by], [created_at]) VALUES (3, 5, N'Hồng cầu và bạch cầu trong giới hạn bình thường.', N'4.0 - 10.0', N'Chưa ghi nhận bất thường.', CAST(N'2026-10-01T22:22:12.0458733' AS DateTime2), NULL, CAST(N'2026-10-01T15:22:39.5914532' AS DateTime2))
GO
SET IDENTITY_INSERT [clinical].[lab_results] OFF
GO
SET IDENTITY_INSERT [clinical].[medical_record_services] ON 
GO
INSERT [clinical].[medical_record_services] ([id], [medical_record_id], [service_id], [quantity], [unit_price_snapshot], [status], [performed_by], [ordered_at], [completed_at]) VALUES (1, 1, 1, 1, CAST(150000.00 AS Decimal(12, 2)), 0, NULL, CAST(N'2026-09-23T06:07:35.1681257' AS DateTime2), NULL)
GO
INSERT [clinical].[medical_record_services] ([id], [medical_record_id], [service_id], [quantity], [unit_price_snapshot], [status], [performed_by], [ordered_at], [completed_at]) VALUES (2, 4, 1, 1, CAST(150000.00 AS Decimal(12, 2)), 3, NULL, CAST(N'2026-09-30T10:18:46.0267456' AS DateTime2), NULL)
GO
INSERT [clinical].[medical_record_services] ([id], [medical_record_id], [service_id], [quantity], [unit_price_snapshot], [status], [performed_by], [ordered_at], [completed_at]) VALUES (3, 4, 1, 1, CAST(150000.00 AS Decimal(12, 2)), 2, 9, CAST(N'2026-09-30T16:41:41.2462423' AS DateTime2), CAST(N'2026-09-30T16:41:55.5090787' AS DateTime2))
GO
INSERT [clinical].[medical_record_services] ([id], [medical_record_id], [service_id], [quantity], [unit_price_snapshot], [status], [performed_by], [ordered_at], [completed_at]) VALUES (4, 7, 1, 1, CAST(150000.00 AS Decimal(12, 2)), 3, NULL, CAST(N'2026-10-01T06:42:34.0298068' AS DateTime2), NULL)
GO
INSERT [clinical].[medical_record_services] ([id], [medical_record_id], [service_id], [quantity], [unit_price_snapshot], [status], [performed_by], [ordered_at], [completed_at]) VALUES (5, 7, 5, 1, CAST(250000.00 AS Decimal(12, 2)), 2, 9, CAST(N'2026-10-01T08:19:05.5844036' AS DateTime2), CAST(N'2026-10-01T15:22:39.5031244' AS DateTime2))
GO
SET IDENTITY_INSERT [clinical].[medical_record_services] OFF
GO
SET IDENTITY_INSERT [clinical].[medical_records] ON 
GO
INSERT [clinical].[medical_records] ([id], [appointment_id], [patient_id], [doctor_id], [symptoms], [diagnosis], [note], [created_at], [updated_at], [icd10_code], [treatment_plan], [follow_up_date], [status], [finalized_at]) VALUES (1, 2, 5, 5, N'Đau đầu, chóng mặt', N'Đau đầu do căng thẳng', N'Theo dõi thêm nếu triệu chứng kéo dài', CAST(N'2026-09-20T13:38:23.6400000' AS DateTime2), NULL, N'R51', N'Nghỉ ngơi, uống đủ nước và theo dõi triệu chứng', CAST(N'2026-09-27' AS Date), 1, CAST(N'2026-09-20T13:38:23.6400000' AS DateTime2))
GO
INSERT [clinical].[medical_records] ([id], [appointment_id], [patient_id], [doctor_id], [symptoms], [diagnosis], [note], [created_at], [updated_at], [icd10_code], [treatment_plan], [follow_up_date], [status], [finalized_at]) VALUES (2, 3, 5, 5, N'Ho, đau họng nhẹ', N'Đang chờ bác sĩ hoàn tất chẩn đoán', N'Bệnh án đang ở trạng thái nháp để test', CAST(N'2026-09-20T13:47:31.9866667' AS DateTime2), NULL, NULL, N'Tiếp tục theo dõi', NULL, 0, NULL)
GO
INSERT [clinical].[medical_records] ([id], [appointment_id], [patient_id], [doctor_id], [symptoms], [diagnosis], [note], [created_at], [updated_at], [icd10_code], [treatment_plan], [follow_up_date], [status], [finalized_at]) VALUES (3, 9, 1, 5, N'Test lưu nháp', N'Test diagnosis', N'Test note', CAST(N'2026-09-28T23:00:42.7823420' AS DateTime2), CAST(N'2026-09-30T10:38:29.0343849' AS DateTime2), N'R51', N'Test treatment', NULL, 1, CAST(N'2026-09-30T10:38:29.0343849' AS DateTime2))
GO
INSERT [clinical].[medical_records] ([id], [appointment_id], [patient_id], [doctor_id], [symptoms], [diagnosis], [note], [created_at], [updated_at], [icd10_code], [treatment_plan], [follow_up_date], [status], [finalized_at]) VALUES (4, 26, 5, 5, NULL, NULL, NULL, CAST(N'2026-09-29T21:43:00.0825037' AS DateTime2), NULL, NULL, NULL, NULL, 0, NULL)
GO
INSERT [clinical].[medical_records] ([id], [appointment_id], [patient_id], [doctor_id], [symptoms], [diagnosis], [note], [created_at], [updated_at], [icd10_code], [treatment_plan], [follow_up_date], [status], [finalized_at]) VALUES (5, 27, 7, 5, N'Đau đầu', N'Viêm họng', N'Theo dõi thêm', CAST(N'2026-09-30T14:12:58.4989473' AS DateTime2), CAST(N'2026-09-30T14:59:10.4148884' AS DateTime2), N'R51', N'Nghỉ ngơi, uống đủ nước', CAST(N'2026-10-07' AS Date), 1, CAST(N'2026-09-30T14:59:10.4148884' AS DateTime2))
GO
INSERT [clinical].[medical_records] ([id], [appointment_id], [patient_id], [doctor_id], [symptoms], [diagnosis], [note], [created_at], [updated_at], [icd10_code], [treatment_plan], [follow_up_date], [status], [finalized_at]) VALUES (6, 28, 7, 5, NULL, NULL, NULL, CAST(N'2026-09-30T15:20:10.4185440' AS DateTime2), NULL, NULL, NULL, NULL, 0, NULL)
GO
INSERT [clinical].[medical_records] ([id], [appointment_id], [patient_id], [doctor_id], [symptoms], [diagnosis], [note], [created_at], [updated_at], [icd10_code], [treatment_plan], [follow_up_date], [status], [finalized_at]) VALUES (7, 29, 5, 5, N'Đau bụng thuờng xuyên', N'Rối loạn tiêu hóa', NULL, CAST(N'2026-10-01T12:38:43.0960480' AS DateTime2), NULL, N'123', NULL, NULL, 0, NULL)
GO
INSERT [clinical].[medical_records] ([id], [appointment_id], [patient_id], [doctor_id], [symptoms], [diagnosis], [note], [created_at], [updated_at], [icd10_code], [treatment_plan], [follow_up_date], [status], [finalized_at]) VALUES (8, 30, 5, 5, N'Đau đầu thường xuyên', N'Đau đầu kiểu căng thẳng', N'Theo dõi triệu chứng đau đầu, chưa ghi nhận dấu hiệu bất thường khác.', CAST(N'2026-10-02T00:57:45.8653483' AS DateTime2), CAST(N'2026-10-02T01:52:00.0605648' AS DateTime2), N'G44.2', N'Nghỉ ngơi đầy đủ, uống đủ nước, hạn chế căng thẳng và thời gian sử dụng thiết bị điện tử. Theo dõi mức độ đau đầu và tái khám nếu triệu chứng không cải thiện.', CAST(N'2026-10-08' AS Date), 1, CAST(N'2026-10-02T01:52:00.0605648' AS DateTime2))
GO
INSERT [clinical].[medical_records] ([id], [appointment_id], [patient_id], [doctor_id], [symptoms], [diagnosis], [note], [created_at], [updated_at], [icd10_code], [treatment_plan], [follow_up_date], [status], [finalized_at]) VALUES (9, 31, 5, 5, N'Dau bung', N'Dau da day', N'Can theo doi them', CAST(N'2026-10-02T09:07:39.7283785' AS DateTime2), CAST(N'2026-10-02T09:09:42.1264415' AS DateTime2), N'DD0220', N'Uong thuoc va nghi ngoi', NULL, 1, CAST(N'2026-10-02T09:09:42.1264415' AS DateTime2))
GO
INSERT [clinical].[medical_records] ([id], [appointment_id], [patient_id], [doctor_id], [symptoms], [diagnosis], [note], [created_at], [updated_at], [icd10_code], [treatment_plan], [follow_up_date], [status], [finalized_at]) VALUES (10, 32, 5, 5, NULL, N'Dau bung', NULL, CAST(N'2026-10-02T09:12:59.1814326' AS DateTime2), CAST(N'2026-10-02T09:14:31.1326808' AS DateTime2), NULL, NULL, NULL, 1, CAST(N'2026-10-02T09:14:31.1326808' AS DateTime2))
GO
SET IDENTITY_INSERT [clinical].[medical_records] OFF
GO
SET IDENTITY_INSERT [clinical].[medicine_batches] ON 
GO
INSERT [clinical].[medicine_batches] ([id], [medicine_id], [batch_no], [expiry_date], [quantity], [import_price]) VALUES (1, 1, N'TEST-PARA-001', CAST(N'2027-09-30' AS Date), 0, CAST(1000.00 AS Decimal(12, 2)))
GO
INSERT [clinical].[medicine_batches] ([id], [medicine_id], [batch_no], [expiry_date], [quantity], [import_price]) VALUES (2, 1, N'TEST-PARA-002', CAST(N'2027-09-30' AS Date), 85, CAST(1000.00 AS Decimal(12, 2)))
GO
INSERT [clinical].[medicine_batches] ([id], [medicine_id], [batch_no], [expiry_date], [quantity], [import_price]) VALUES (3, 1, N'TEST-RECEIPT-001', CAST(N'2027-12-30' AS Date), 100, CAST(1500.00 AS Decimal(12, 2)))
GO
INSERT [clinical].[medicine_batches] ([id], [medicine_id], [batch_no], [expiry_date], [quantity], [import_price]) VALUES (4, 1, N'test', CAST(N'2027-10-02' AS Date), 10, CAST(150.00 AS Decimal(12, 2)))
GO
INSERT [clinical].[medicine_batches] ([id], [medicine_id], [batch_no], [expiry_date], [quantity], [import_price]) VALUES (5, 1, N'TEST-UI-001', CAST(N'2027-10-03' AS Date), 100, CAST(1500.00 AS Decimal(12, 2)))
GO
INSERT [clinical].[medicine_batches] ([id], [medicine_id], [batch_no], [expiry_date], [quantity], [import_price]) VALUES (6, 1, N'DFfd', CAST(N'2027-10-03' AS Date), 100, CAST(1500.00 AS Decimal(12, 2)))
GO
INSERT [clinical].[medicine_batches] ([id], [medicine_id], [batch_no], [expiry_date], [quantity], [import_price]) VALUES (7, 1, N'SAFASF', CAST(N'2027-10-03' AS Date), 345, CAST(344.96 AS Decimal(12, 2)))
GO
INSERT [clinical].[medicine_batches] ([id], [medicine_id], [batch_no], [expiry_date], [quantity], [import_price]) VALUES (8, 1, N'L2409', CAST(N'2027-10-03' AS Date), 100, CAST(1500.00 AS Decimal(12, 2)))
GO
SET IDENTITY_INSERT [clinical].[medicine_batches] OFF
GO
SET IDENTITY_INSERT [clinical].[medicine_stock_transactions] ON 
GO
INSERT [clinical].[medicine_stock_transactions] ([id], [medicine_id], [batch_id], [type], [quantity], [quantity_before], [quantity_after], [prescription_id], [reversal_of_transaction_id], [created_by], [created_at], [note]) VALUES (1, 1, 1, 1, 5, 5, 0, 7, NULL, 13, CAST(N'2026-10-02T10:29:09.6133179' AS DateTime2), N'Phát thuốc theo đơn #7.')
GO
INSERT [clinical].[medicine_stock_transactions] ([id], [medicine_id], [batch_id], [type], [quantity], [quantity_before], [quantity_after], [prescription_id], [reversal_of_transaction_id], [created_by], [created_at], [note]) VALUES (2, 1, 2, 1, 5, 100, 95, 7, NULL, 13, CAST(N'2026-10-02T10:29:09.6346306' AS DateTime2), N'Phát thuốc theo đơn #7.')
GO
INSERT [clinical].[medicine_stock_transactions] ([id], [medicine_id], [batch_id], [type], [quantity], [quantity_before], [quantity_after], [prescription_id], [reversal_of_transaction_id], [created_by], [created_at], [note]) VALUES (3, 1, 2, 1, 8, 95, 87, 6, NULL, 13, CAST(N'2026-10-02T14:23:45.7686595' AS DateTime2), N'Phát thuốc theo đơn #6.')
GO
INSERT [clinical].[medicine_stock_transactions] ([id], [medicine_id], [batch_id], [type], [quantity], [quantity_before], [quantity_after], [prescription_id], [reversal_of_transaction_id], [created_by], [created_at], [note]) VALUES (4, 1, 2, 2, 3, 87, 90, NULL, NULL, 13, CAST(N'2026-10-02T16:01:59.1955223' AS DateTime2), N'Kiểm kê phát hiện dư 5 viên.')
GO
INSERT [clinical].[medicine_stock_transactions] ([id], [medicine_id], [batch_id], [type], [quantity], [quantity_before], [quantity_after], [prescription_id], [reversal_of_transaction_id], [created_by], [created_at], [note]) VALUES (5, 1, 2, 2, 5, 90, 85, NULL, NULL, 13, CAST(N'2026-10-02T16:02:19.7928421' AS DateTime2), N'Kiểm kê thực tế thiếu 5 viên.')
GO
INSERT [clinical].[medicine_stock_transactions] ([id], [medicine_id], [batch_id], [type], [quantity], [quantity_before], [quantity_after], [prescription_id], [reversal_of_transaction_id], [created_by], [created_at], [note]) VALUES (6, 1, 2, 2, 5, 85, 90, NULL, NULL, 13, CAST(N'2026-10-02T22:35:27.5129682' AS DateTime2), N'Kiểm kê phát hiện dư 5 viên.')
GO
INSERT [clinical].[medicine_stock_transactions] ([id], [medicine_id], [batch_id], [type], [quantity], [quantity_before], [quantity_after], [prescription_id], [reversal_of_transaction_id], [created_by], [created_at], [note]) VALUES (7, 1, 2, 2, 5, 90, 85, NULL, NULL, 13, CAST(N'2026-10-02T22:35:55.5283878' AS DateTime2), N'Kiểm kê phát hiện thieu 5 viên.')
GO
INSERT [clinical].[medicine_stock_transactions] ([id], [medicine_id], [batch_id], [type], [quantity], [quantity_before], [quantity_after], [prescription_id], [reversal_of_transaction_id], [created_by], [created_at], [note]) VALUES (8, 1, 3, 0, 100, 0, 100, NULL, NULL, 13, CAST(N'2026-10-02T23:49:04.9361274' AS DateTime2), N'Nhập thuốc | NCC: Công ty Dược Hậu Giang | Số HĐ: HD-DHG-8891 | Lô: TEST-RECEIPT-001')
GO
INSERT [clinical].[medicine_stock_transactions] ([id], [medicine_id], [batch_id], [type], [quantity], [quantity_before], [quantity_after], [prescription_id], [reversal_of_transaction_id], [created_by], [created_at], [note]) VALUES (9, 1, 4, 0, 10, 0, 10, NULL, NULL, 13, CAST(N'2026-10-03T00:26:11.7487194' AS DateTime2), N'Nhập thuốc | NCC: Nha cung cap abc | Số HĐ: GSS | Lô: test')
GO
INSERT [clinical].[medicine_stock_transactions] ([id], [medicine_id], [batch_id], [type], [quantity], [quantity_before], [quantity_after], [prescription_id], [reversal_of_transaction_id], [created_by], [created_at], [note]) VALUES (10, 1, 5, 0, 100, 0, 100, NULL, NULL, 13, CAST(N'2026-10-03T00:28:42.9025673' AS DateTime2), N'Nhập thuốc | NCC: Công ty Dược Hậu Giang | Số HĐ: HD-DHG-8891 | Lô: TEST-UI-001')
GO
INSERT [clinical].[medicine_stock_transactions] ([id], [medicine_id], [batch_id], [type], [quantity], [quantity_before], [quantity_after], [prescription_id], [reversal_of_transaction_id], [created_by], [created_at], [note]) VALUES (11, 1, 6, 0, 100, 0, 100, NULL, NULL, 13, CAST(N'2026-10-03T00:29:37.6869097' AS DateTime2), N'Nhập thuốc | NCC: asdf | Số HĐ: asf | Lô: DFfd')
GO
INSERT [clinical].[medicine_stock_transactions] ([id], [medicine_id], [batch_id], [type], [quantity], [quantity_before], [quantity_after], [prescription_id], [reversal_of_transaction_id], [created_by], [created_at], [note]) VALUES (12, 1, 7, 0, 345, 0, 345, NULL, NULL, 13, CAST(N'2026-10-03T00:43:50.2657337' AS DateTime2), N'Nhập thuốc | NCC: afa | Số HĐ: DSFDSF | Lô: SAFASF')
GO
INSERT [clinical].[medicine_stock_transactions] ([id], [medicine_id], [batch_id], [type], [quantity], [quantity_before], [quantity_after], [prescription_id], [reversal_of_transaction_id], [created_by], [created_at], [note]) VALUES (13, 1, 8, 0, 100, 0, 100, NULL, NULL, 13, CAST(N'2026-10-03T01:10:28.1843810' AS DateTime2), N'Nhập thuốc | NCC: Nha cung cap Lieu Lieu | Số HĐ: HD-HD83 | Lô: L2409')
GO
SET IDENTITY_INSERT [clinical].[medicine_stock_transactions] OFF
GO
SET IDENTITY_INSERT [clinical].[medicines] ON 
GO
INSERT [clinical].[medicines] ([id], [name], [unit], [price], [stock_quantity], [description], [is_active], [created_at], [updated_at], [code], [active_ingredient], [concentration], [cost_price], [min_stock]) VALUES (1, N'Paracetamol 500mg', N'Viên', CAST(2000.00 AS Decimal(12, 2)), 840, N'Thuốc giảm đau, hạ sốt dùng để test VC-05.', 1, CAST(N'2026-09-23T05:39:20.6480532' AS DateTime2), CAST(N'2026-10-03T01:10:28.1842707' AS DateTime2), N'MED-PARA-500', N'Paracetamol', N'500mg', CAST(1000.00 AS Decimal(12, 2)), 10)
GO
SET IDENTITY_INSERT [clinical].[medicines] OFF
GO
SET IDENTITY_INSERT [clinical].[patient_allergies] ON 
GO
INSERT [clinical].[patient_allergies] ([id], [patient_id], [allergen], [severity], [note]) VALUES (1, 8, N'Paracetamol', 2, N'Từng nổi mề đay sau khi sử dụng.')
GO
INSERT [clinical].[patient_allergies] ([id], [patient_id], [allergen], [severity], [note]) VALUES (4, 7, N'Paracetamol', 2, N'Chóng mặt khi sử dụng')
GO
INSERT [clinical].[patient_allergies] ([id], [patient_id], [allergen], [severity], [note]) VALUES (5, 7, N'Penicillin ', 1, NULL)
GO
SET IDENTITY_INSERT [clinical].[patient_allergies] OFF
GO
SET IDENTITY_INSERT [clinical].[patient_vitals] ON 
GO
INSERT [clinical].[patient_vitals] ([id], [medical_record_id], [temperature], [pulse], [blood_pressure], [weight], [height]) VALUES (1, 3, CAST(37.0 AS Decimal(4, 1)), 80, N'125/80', CAST(55.00 AS Decimal(6, 2)), CAST(165.00 AS Decimal(6, 2)))
GO
INSERT [clinical].[patient_vitals] ([id], [medical_record_id], [temperature], [pulse], [blood_pressure], [weight], [height]) VALUES (2, 5, CAST(36.8 AS Decimal(4, 1)), 80, NULL, CAST(53.00 AS Decimal(6, 2)), CAST(160.00 AS Decimal(6, 2)))
GO
INSERT [clinical].[patient_vitals] ([id], [medical_record_id], [temperature], [pulse], [blood_pressure], [weight], [height]) VALUES (3, 7, CAST(36.0 AS Decimal(4, 1)), 80, NULL, CAST(80.00 AS Decimal(6, 2)), CAST(75.00 AS Decimal(6, 2)))
GO
INSERT [clinical].[patient_vitals] ([id], [medical_record_id], [temperature], [pulse], [blood_pressure], [weight], [height]) VALUES (4, 8, CAST(36.0 AS Decimal(4, 1)), 80, NULL, CAST(50.00 AS Decimal(6, 2)), CAST(165.00 AS Decimal(6, 2)))
GO
INSERT [clinical].[patient_vitals] ([id], [medical_record_id], [temperature], [pulse], [blood_pressure], [weight], [height]) VALUES (5, 9, CAST(36.0 AS Decimal(4, 1)), 880, NULL, CAST(70.00 AS Decimal(6, 2)), CAST(170.00 AS Decimal(6, 2)))
GO
SET IDENTITY_INSERT [clinical].[patient_vitals] OFF
GO
SET IDENTITY_INSERT [clinical].[prescription_items] ON 
GO
INSERT [clinical].[prescription_items] ([id], [prescription_id], [medicine_id], [quantity], [dosage], [instruction], [medicine_name_snapshot], [unit_price_snapshot], [duration_days], [frequency]) VALUES (4, 4, 1, 100, N'1 viên/lần', N'Uống sau ăn', N'Paracetamol 500mg', CAST(2000.00 AS Decimal(12, 2)), 5, NULL)
GO
INSERT [clinical].[prescription_items] ([id], [prescription_id], [medicine_id], [quantity], [dosage], [instruction], [medicine_name_snapshot], [unit_price_snapshot], [duration_days], [frequency]) VALUES (6, 6, 1, 8, N'1 viên/ 1 lần', N'Uống sau ăn', N'Paracetamol 500mg', CAST(2000.00 AS Decimal(12, 2)), 10, NULL)
GO
INSERT [clinical].[prescription_items] ([id], [prescription_id], [medicine_id], [quantity], [dosage], [instruction], [medicine_name_snapshot], [unit_price_snapshot], [duration_days], [frequency]) VALUES (8, 7, 1, 10, N'1 viên/ 1 lần', N'Uống sau ăn', N'Paracetamol 500mg', CAST(2000.00 AS Decimal(12, 2)), 10, NULL)
GO
SET IDENTITY_INSERT [clinical].[prescription_items] OFF
GO
SET IDENTITY_INSERT [clinical].[prescriptions] ON 
GO
INSERT [clinical].[prescriptions] ([id], [medical_record_id], [note], [created_at], [doctor_id], [status], [dispensed_at], [dispensed_by]) VALUES (4, 2, N'Test allergy', CAST(N'2026-10-01T11:34:39.1503198' AS DateTime2), 5, 1, NULL, NULL)
GO
INSERT [clinical].[prescriptions] ([id], [medical_record_id], [note], [created_at], [doctor_id], [status], [dispensed_at], [dispensed_by]) VALUES (6, 8, NULL, CAST(N'2026-10-02T01:46:31.5212148' AS DateTime2), 5, 2, CAST(N'2026-10-02T14:23:45.8118714' AS DateTime2), 13)
GO
INSERT [clinical].[prescriptions] ([id], [medical_record_id], [note], [created_at], [doctor_id], [status], [dispensed_at], [dispensed_by]) VALUES (7, 10, NULL, CAST(N'2026-10-02T09:14:19.6736728' AS DateTime2), 5, 2, CAST(N'2026-10-02T10:29:09.6348606' AS DateTime2), 13)
GO
SET IDENTITY_INSERT [clinical].[prescriptions] OFF
GO
SET IDENTITY_INSERT [scheduling].[appointment_status_history] ON 
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (1, 2, NULL, 0, NULL, NULL, CAST(N'2026-09-19T08:24:31.4284398' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (2, 2, 0, 4, 4, N'trung lich', CAST(N'2026-09-20T09:04:10.9421755' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (3, 2, 1, 4, 4, N'trung lich', CAST(N'2026-09-20T09:14:42.0095566' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (4, 5, NULL, 0, NULL, NULL, CAST(N'2026-09-21T15:13:27.2775125' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (5, 6, NULL, 0, NULL, NULL, CAST(N'2026-09-21T15:30:53.2037659' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (6, 7, NULL, 0, NULL, NULL, CAST(N'2026-09-21T15:37:28.0846597' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (7, 3, 0, 4, 4, N'Toi co viec dot xuat', CAST(N'2026-09-22T01:44:13.5468697' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (8, 5, 0, 2, 1, NULL, CAST(N'2026-09-24T13:03:45.5035736' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (9, 4, 1, 2, 1, NULL, CAST(N'2026-09-24T13:04:47.5450999' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (10, 4, 1, 5, 1, N'string', CAST(N'2026-09-24T13:31:32.2256830' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (11, 4, 1, 5, 1, N'string', CAST(N'2026-09-24T13:32:06.9956146' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (12, 5, 0, 5, 1, N'Bệnh nhân không đến.', CAST(N'2026-09-24T22:19:47.7072224' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (13, 4, 0, 2, 1, NULL, CAST(N'2026-09-24T22:20:53.0731020' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (14, 4, 1, 5, 1, N'Bệnh nhân không đến.', CAST(N'2026-09-24T22:43:34.3337492' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (15, 5, 1, 2, 1, NULL, CAST(N'2026-09-24T23:04:45.9717518' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (16, 8, NULL, 0, 1, NULL, CAST(N'2026-09-26T06:24:45.8119134' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (17, 9, NULL, 0, 1, NULL, CAST(N'2026-09-26T17:48:14.1814673' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (18, 10, NULL, 0, 1, NULL, CAST(N'2026-09-26T17:52:51.4790030' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (19, 11, NULL, 0, 1, NULL, CAST(N'2026-09-26T18:05:12.0534494' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (20, 20, 0, 2, 1, NULL, CAST(N'2026-09-27T12:04:25.8174835' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (21, 21, 0, 2, 1, NULL, CAST(N'2026-09-27T12:04:29.9989941' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (22, 25, 0, 2, 1, NULL, CAST(N'2026-09-27T12:04:33.4277687' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (23, 9, 0, 2, 1, NULL, CAST(N'2026-09-28T14:00:40.7135528' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (24, 26, NULL, 0, NULL, NULL, CAST(N'2026-09-29T08:20:47.4330350' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (25, 26, 0, 2, 1, NULL, CAST(N'2026-09-29T15:32:05.4959885' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (26, 9, 2, 3, 9, N'Chốt bệnh án.', CAST(N'2026-09-30T10:38:29.0343849' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (27, 27, NULL, 0, 1, NULL, CAST(N'2026-09-30T07:10:51.7397648' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (28, 8, 0, 5, 1, N'Bệnh nhân không đến.', CAST(N'2026-09-30T14:12:08.4357075' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (29, 27, 0, 2, 1, NULL, CAST(N'2026-09-30T14:12:11.4504627' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (30, 27, 2, 3, 9, N'Chốt bệnh án.', CAST(N'2026-09-30T14:59:10.4148884' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (31, 28, NULL, 0, 1, NULL, CAST(N'2026-09-30T08:18:29.1529172' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (32, 28, 0, 2, 1, NULL, CAST(N'2026-09-30T15:19:58.4812938' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (33, 29, NULL, 0, NULL, NULL, CAST(N'2026-10-01T05:38:02.2958667' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (34, 29, 0, 2, 1, NULL, CAST(N'2026-10-01T12:38:31.0007760' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (35, 30, NULL, 0, NULL, NULL, CAST(N'2026-10-01T17:57:03.4062169' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (36, 30, 0, 2, 1, NULL, CAST(N'2026-10-02T00:57:36.5485631' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (37, 30, 2, 3, 9, N'Chốt bệnh án.', CAST(N'2026-10-02T01:52:00.0605648' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (38, 31, NULL, 0, NULL, NULL, CAST(N'2026-10-02T02:05:39.3161404' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (39, 31, 0, 2, 1, NULL, CAST(N'2026-10-02T09:05:57.2943797' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (40, 31, 2, 3, 9, N'Chốt bệnh án.', CAST(N'2026-10-02T09:09:42.1264415' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (41, 32, NULL, 0, NULL, NULL, CAST(N'2026-10-02T02:12:24.3294710' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (42, 32, 0, 2, 1, NULL, CAST(N'2026-10-02T09:12:43.6456221' AS DateTime2))
GO
INSERT [scheduling].[appointment_status_history] ([id], [appointment_id], [from_status], [to_status], [changed_by], [reason], [changed_at]) VALUES (43, 32, 2, 3, 9, N'Chốt bệnh án.', CAST(N'2026-10-02T09:14:31.1326808' AS DateTime2))
GO
SET IDENTITY_INSERT [scheduling].[appointment_status_history] OFF
GO
SET IDENTITY_INSERT [scheduling].[appointments] ON 
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (1, 1, 5, CAST(N'2026-09-21T10:30:00.0000000' AS DateTime2), CAST(N'2026-09-21T11:00:00.0000000' AS DateTime2), 0, N'Test booked slot', NULL, NULL, CAST(N'2026-09-18T23:02:52.1166667' AS DateTime2), NULL, N'TEST-20260918230252', NULL, NULL, NULL, NULL, 0, CAST(200000.00 AS Decimal(12, 2)))
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (2, 5, 5, CAST(N'2026-09-22T11:30:00.0000000' AS DateTime2), CAST(N'2026-09-22T12:00:00.0000000' AS DateTime2), 3, N'string', NULL, NULL, CAST(N'2026-09-19T08:24:31.4284398' AS DateTime2), CAST(N'2026-09-20T12:30:47.0933098' AS DateTime2), N'APT4CCC4E7CEA0D4E2BB', NULL, NULL, NULL, N'trung lich', 0, CAST(200000.00 AS Decimal(12, 2)))
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (3, 5, 5, CAST(N'2026-09-23T14:30:00.0000000' AS DateTime2), CAST(N'2026-09-23T15:00:00.0000000' AS DateTime2), 4, NULL, NULL, NULL, CAST(N'2026-09-20T05:21:27.5066893' AS DateTime2), CAST(N'2026-09-22T01:44:13.5468697' AS DateTime2), N'APT4CCC4E7CEA0D4E2AA', NULL, NULL, NULL, N'Toi co viec dot xuat', 0, CAST(200000.00 AS Decimal(12, 2)))
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (4, 5, 5, CAST(N'2026-09-24T13:30:00.0000000' AS DateTime2), CAST(N'2026-09-24T14:00:00.0000000' AS DateTime2), 5, NULL, NULL, NULL, CAST(N'2026-09-20T06:50:18.4214494' AS DateTime2), CAST(N'2026-09-24T22:43:34.3337492' AS DateTime2), N'APT4CCC4E7CEA0D4E2CC', 3, CAST(N'2026-09-24T22:20:53.0731020' AS DateTime2), NULL, NULL, 0, CAST(200000.00 AS Decimal(12, 2)))
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (5, 5, 5, CAST(N'2026-09-24T21:30:00.0000000' AS DateTime2), CAST(N'2026-09-24T22:00:00.0000000' AS DateTime2), 2, N'Đau đầu kéo dài 3 ngày', NULL, NULL, CAST(N'2026-09-21T15:13:27.2775125' AS DateTime2), CAST(N'2026-09-24T23:04:45.9717518' AS DateTime2), N'APT71875382F7CB47F4A', 4, CAST(N'2026-09-24T23:04:45.9717518' AS DateTime2), NULL, NULL, 0, CAST(200000.00 AS Decimal(12, 2)))
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (6, 5, 5, CAST(N'2026-09-22T09:00:00.0000000' AS DateTime2), CAST(N'2026-09-22T09:30:00.0000000' AS DateTime2), 0, NULL, NULL, NULL, CAST(N'2026-09-21T15:30:53.2037659' AS DateTime2), NULL, N'APTD05BE0D7215142B49', NULL, NULL, NULL, NULL, 0, CAST(200000.00 AS Decimal(12, 2)))
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (7, 5, 5, CAST(N'2026-09-25T10:00:00.0000000' AS DateTime2), CAST(N'2026-09-25T10:30:00.0000000' AS DateTime2), 0, N'Dau bung', NULL, NULL, CAST(N'2026-09-21T15:37:28.0846597' AS DateTime2), CAST(N'2026-09-22T13:40:44.8296064' AS DateTime2), N'APT41A4A210C3754148A', NULL, NULL, NULL, NULL, 0, CAST(200000.00 AS Decimal(12, 2)))
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (8, 1, 5, CAST(N'2026-09-30T08:00:00.0000000' AS DateTime2), CAST(N'2026-09-30T08:30:00.0000000' AS DateTime2), 5, N'Khám tổng quát', NULL, 1, CAST(N'2026-09-26T06:24:45.8119134' AS DateTime2), CAST(N'2026-09-30T14:12:08.4357075' AS DateTime2), N'APT239D663F223244B6A', NULL, NULL, NULL, NULL, 2, CAST(200000.00 AS Decimal(12, 2)))
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (9, 1, 5, CAST(N'2026-09-28T08:00:00.0000000' AS DateTime2), CAST(N'2026-09-28T08:30:00.0000000' AS DateTime2), 3, N'Khám tổng quát', NULL, 1, CAST(N'2026-09-26T17:48:14.1814673' AS DateTime2), CAST(N'2026-09-30T10:38:29.0343849' AS DateTime2), N'APT50719E50F4C545F6B', 1, CAST(N'2026-09-28T14:00:40.7135528' AS DateTime2), NULL, NULL, 1, CAST(200000.00 AS Decimal(12, 2)))
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (10, 7, 5, CAST(N'2026-09-28T16:30:00.0000000' AS DateTime2), CAST(N'2026-09-28T17:00:00.0000000' AS DateTime2), 0, N'Nhuc dau', NULL, 1, CAST(N'2026-09-26T17:52:51.4790030' AS DateTime2), NULL, N'APTE40CAF9BFB65482CB', NULL, NULL, NULL, NULL, 1, CAST(200000.00 AS Decimal(12, 2)))
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (11, 7, 5, CAST(N'2026-09-28T15:00:00.0000000' AS DateTime2), CAST(N'2026-09-28T15:30:00.0000000' AS DateTime2), 0, N'', NULL, 1, CAST(N'2026-09-26T18:05:12.0534494' AS DateTime2), NULL, N'APT76497B2B6E6C4D2EB', NULL, NULL, NULL, NULL, 1, CAST(200000.00 AS Decimal(12, 2)))
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (20, 1, 5, CAST(N'2026-09-27T08:00:00.0000000' AS DateTime2), CAST(N'2026-09-27T08:30:00.0000000' AS DateTime2), 2, N'Khám tổng quát', NULL, NULL, CAST(N'2026-09-27T12:03:09.4233333' AS DateTime2), CAST(N'2026-09-27T14:15:21.3247751' AS DateTime2), N'TEST27-P01', 6, CAST(N'2026-09-27T12:04:25.8174835' AS DateTime2), NULL, NULL, 1, CAST(200000.00 AS Decimal(12, 2)))
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (21, 2, 5, CAST(N'2026-09-27T08:30:00.0000000' AS DateTime2), CAST(N'2026-09-27T09:00:00.0000000' AS DateTime2), 2, N'Đau đầu', NULL, NULL, CAST(N'2026-09-27T12:03:09.4233333' AS DateTime2), CAST(N'2026-09-27T12:04:29.9989941' AS DateTime2), N'TEST27-P02', 2, CAST(N'2026-09-27T12:04:29.9989941' AS DateTime2), NULL, NULL, 1, CAST(200000.00 AS Decimal(12, 2)))
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (22, 3, 5, CAST(N'2026-09-27T09:00:00.0000000' AS DateTime2), CAST(N'2026-09-27T09:30:00.0000000' AS DateTime2), 0, N'Đau bụng', NULL, NULL, CAST(N'2026-09-27T12:03:09.4233333' AS DateTime2), NULL, N'TEST27-P03', NULL, NULL, NULL, NULL, 1, CAST(200000.00 AS Decimal(12, 2)))
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (23, 5, 5, CAST(N'2026-09-27T09:30:00.0000000' AS DateTime2), CAST(N'2026-09-27T10:00:00.0000000' AS DateTime2), 0, N'Khám định kỳ', NULL, NULL, CAST(N'2026-09-27T12:03:09.4233333' AS DateTime2), NULL, N'TEST27-P05', NULL, NULL, NULL, NULL, 1, CAST(200000.00 AS Decimal(12, 2)))
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (24, 6, 5, CAST(N'2026-09-27T10:00:00.0000000' AS DateTime2), CAST(N'2026-09-27T10:30:00.0000000' AS DateTime2), 0, N'Ho và đau họng', NULL, NULL, CAST(N'2026-09-27T12:03:09.4233333' AS DateTime2), NULL, N'TEST27-P06', NULL, NULL, NULL, NULL, 1, CAST(200000.00 AS Decimal(12, 2)))
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (25, 7, 5, CAST(N'2026-09-27T10:30:00.0000000' AS DateTime2), CAST(N'2026-09-27T11:00:00.0000000' AS DateTime2), 2, N'Đau lưng', NULL, NULL, CAST(N'2026-09-27T12:03:09.4233333' AS DateTime2), CAST(N'2026-09-27T14:43:52.5064741' AS DateTime2), N'TEST27-P07', 7, CAST(N'2026-09-27T12:04:33.4277687' AS DateTime2), NULL, NULL, 1, CAST(200000.00 AS Decimal(12, 2)))
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (26, 5, 5, CAST(N'2026-09-29T16:00:00.0000000' AS DateTime2), CAST(N'2026-09-29T16:30:00.0000000' AS DateTime2), 2, NULL, NULL, NULL, CAST(N'2026-09-29T08:20:47.4330350' AS DateTime2), CAST(N'2026-09-29T15:32:05.4959885' AS DateTime2), N'APT96FA04C2CA9045CF9', 1, CAST(N'2026-09-29T15:32:05.4959885' AS DateTime2), NULL, NULL, 0, CAST(200000.00 AS Decimal(12, 2)))
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (27, 7, 5, CAST(N'2026-09-30T14:30:00.0000000' AS DateTime2), CAST(N'2026-09-30T15:00:00.0000000' AS DateTime2), 3, N'Dau dau', NULL, 1, CAST(N'2026-09-30T07:10:51.7397648' AS DateTime2), CAST(N'2026-09-30T14:59:10.4148884' AS DateTime2), N'APT283741DA742C41508', 1, CAST(N'2026-09-30T14:12:11.4504627' AS DateTime2), NULL, NULL, 1, CAST(200000.00 AS Decimal(12, 2)))
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (28, 7, 5, CAST(N'2026-09-30T15:30:00.0000000' AS DateTime2), CAST(N'2026-09-30T16:00:00.0000000' AS DateTime2), 2, N'', NULL, 1, CAST(N'2026-09-30T08:18:29.1529172' AS DateTime2), CAST(N'2026-09-30T15:19:58.4812938' AS DateTime2), N'APT59212AAAB91B4E6FA', 2, CAST(N'2026-09-30T15:19:58.4812938' AS DateTime2), NULL, NULL, 1, CAST(200000.00 AS Decimal(12, 2)))
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (29, 5, 5, CAST(N'2026-10-01T13:30:00.0000000' AS DateTime2), CAST(N'2026-10-01T14:00:00.0000000' AS DateTime2), 2, N'Đau bụng', NULL, NULL, CAST(N'2026-10-01T05:38:02.2958667' AS DateTime2), CAST(N'2026-10-01T12:38:31.0007760' AS DateTime2), N'APT178D6087B0BE4A258', 1, CAST(N'2026-10-01T12:38:31.0007760' AS DateTime2), NULL, NULL, 0, CAST(200000.00 AS Decimal(12, 2)))
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (30, 5, 5, CAST(N'2026-10-02T08:00:00.0000000' AS DateTime2), CAST(N'2026-10-02T08:30:00.0000000' AS DateTime2), 3, NULL, NULL, NULL, CAST(N'2026-10-01T17:57:03.4062169' AS DateTime2), CAST(N'2026-10-02T01:52:00.0605648' AS DateTime2), N'APTA47E5E58E56A450C9', 1, CAST(N'2026-10-02T00:57:36.5485631' AS DateTime2), NULL, NULL, 0, CAST(200000.00 AS Decimal(12, 2)))
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (31, 5, 5, CAST(N'2026-10-02T09:30:00.0000000' AS DateTime2), CAST(N'2026-10-02T10:00:00.0000000' AS DateTime2), 3, N'Dau bung', NULL, NULL, CAST(N'2026-10-02T02:05:39.3161404' AS DateTime2), CAST(N'2026-10-02T09:09:42.1264415' AS DateTime2), N'APT7AD6F5BB0C664E13A', 2, CAST(N'2026-10-02T09:05:57.2943797' AS DateTime2), NULL, NULL, 0, CAST(200000.00 AS Decimal(12, 2)))
GO
INSERT [scheduling].[appointments] ([id], [patient_id], [doctor_id], [start_time], [end_time], [status], [reason], [note], [created_by], [created_at], [updated_at], [appointment_code], [queue_number], [checked_in_at], [completed_at], [cancel_reason], [source], [fee_snapshot]) VALUES (32, 5, 5, CAST(N'2026-10-02T10:00:00.0000000' AS DateTime2), CAST(N'2026-10-02T10:30:00.0000000' AS DateTime2), 3, NULL, NULL, NULL, CAST(N'2026-10-02T02:12:24.3294710' AS DateTime2), CAST(N'2026-10-02T09:14:31.1326808' AS DateTime2), N'APT970970CC571B40558', 3, CAST(N'2026-10-02T09:12:43.6456221' AS DateTime2), NULL, NULL, 0, CAST(200000.00 AS Decimal(12, 2)))
GO
SET IDENTITY_INSERT [scheduling].[appointments] OFF
GO
SET IDENTITY_INSERT [scheduling].[doctor_schedules] ON 
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (1, 3, 1, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (2, 3, 2, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (3, 3, 3, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (4, 3, 4, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (5, 3, 5, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (6, 4, 1, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (7, 4, 2, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (8, 4, 3, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (9, 4, 4, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (10, 4, 5, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (11, 5, 1, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (12, 5, 2, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (13, 5, 3, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (14, 5, 4, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (15, 5, 5, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (16, 6, 1, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (17, 6, 2, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (18, 6, 3, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (19, 6, 4, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (20, 6, 5, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (21, 6, 6, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (22, 7, 1, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (23, 7, 2, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (24, 7, 3, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (25, 7, 4, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (26, 7, 5, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-17' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (27, 5, 2, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-26' AS Date), NULL, CAST(N'12:00:00' AS Time), CAST(N'13:30:00' AS Time))
GO
INSERT [scheduling].[doctor_schedules] ([id], [doctor_id], [day_of_week], [start_time], [end_time], [slot_minutes], [is_active], [effective_from], [effective_to], [break_start], [break_end]) VALUES (28, 5, 3, CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time), 30, 1, CAST(N'2026-09-30' AS Date), NULL, NULL, NULL)
GO
SET IDENTITY_INSERT [scheduling].[doctor_schedules] OFF
GO
SET IDENTITY_INSERT [scheduling].[doctor_time_off] ON 
GO
INSERT [scheduling].[doctor_time_off] ([id], [doctor_id], [start_at], [end_at], [reason], [created_at], [type], [is_full_day], [approved_by]) VALUES (1, 5, CAST(N'2026-09-21T09:00:00.0000000' AS DateTime2), CAST(N'2026-09-21T10:00:00.0000000' AS DateTime2), N'Test DoctorTimeOff', CAST(N'2026-09-18T21:29:57.2800000' AS DateTime2), NULL, 0, NULL)
GO
SET IDENTITY_INSERT [scheduling].[doctor_time_off] OFF
GO
SET IDENTITY_INSERT [scheduling].[doctors] ON 
GO
INSERT [scheduling].[doctors] ([id], [user_id], [specialty_id], [full_name], [title], [license_number], [phone], [email], [room], [is_active], [created_at], [updated_at], [consultation_fee], [bio], [avatar_url], [experience_years], [max_patients_per_day]) VALUES (2, 3, 1, N'Trần Nguyễn Khánh Vy', N'ThS.BS', N'LIC-NOI-001', N'0903333333', N'khanhvy0946265560@gmail.com', N'P.101', 1, CAST(N'2026-09-11T17:49:11.2506853' AS DateTime2), NULL, CAST(200000.00 AS Decimal(12, 2)), N'Bác sĩ chuyên khoa Nội khoa.', NULL, 8, 30)
GO
INSERT [scheduling].[doctors] ([id], [user_id], [specialty_id], [full_name], [title], [license_number], [phone], [email], [room], [is_active], [created_at], [updated_at], [consultation_fee], [bio], [avatar_url], [experience_years], [max_patients_per_day]) VALUES (3, 7, 2, N'Nguyễn Minh Khang', N'TS.BS', N'LIC-CARD-001', N'0910000001', N'doctor.card01@clinic.local', N'P.201', 1, CAST(N'2026-09-17T22:34:00.5766667' AS DateTime2), NULL, CAST(300000.00 AS Decimal(12, 2)), N'Bác sĩ chuyên khoa Tim mạch.', NULL, 12, 30)
GO
INSERT [scheduling].[doctors] ([id], [user_id], [specialty_id], [full_name], [title], [license_number], [phone], [email], [room], [is_active], [created_at], [updated_at], [consultation_fee], [bio], [avatar_url], [experience_years], [max_patients_per_day]) VALUES (4, 8, 2, N'Trần Hoàng Nam', N'BS.CKI', N'LIC-CARD-002', N'0910000002', N'doctor.card02@clinic.local', N'P.202', 1, CAST(N'2026-09-17T22:34:00.5800000' AS DateTime2), NULL, CAST(250000.00 AS Decimal(12, 2)), N'Bác sĩ chuyên khoa Tim mạch.', NULL, 8, 25)
GO
INSERT [scheduling].[doctors] ([id], [user_id], [specialty_id], [full_name], [title], [license_number], [phone], [email], [room], [is_active], [created_at], [updated_at], [consultation_fee], [bio], [avatar_url], [experience_years], [max_patients_per_day]) VALUES (5, 9, 3, N'Lê Thu Hà', N'BS.CKI', N'LIC-GEN-001', N'0910000003', N'doctor.gen01@clinic.local', N'P.203', 1, CAST(N'2026-09-17T22:34:00.5800000' AS DateTime2), NULL, CAST(200000.00 AS Decimal(12, 2)), N'Bác sĩ chuyên khoa Nội tổng quát.', NULL, 7, 30)
GO
INSERT [scheduling].[doctors] ([id], [user_id], [specialty_id], [full_name], [title], [license_number], [phone], [email], [room], [is_active], [created_at], [updated_at], [consultation_fee], [bio], [avatar_url], [experience_years], [max_patients_per_day]) VALUES (6, 10, 4, N'Phạm Ngọc Lan', N'ThS.BS', N'LIC-DERM-001', N'0910000004', N'doctor.derm01@clinic.local', N'P.301', 1, CAST(N'2026-09-17T22:34:00.5800000' AS DateTime2), NULL, CAST(250000.00 AS Decimal(12, 2)), N'Bác sĩ chuyên khoa Da liễu.', NULL, 9, 25)
GO
INSERT [scheduling].[doctors] ([id], [user_id], [specialty_id], [full_name], [title], [license_number], [phone], [email], [room], [is_active], [created_at], [updated_at], [consultation_fee], [bio], [avatar_url], [experience_years], [max_patients_per_day]) VALUES (7, 11, 1, N'Vũ Quốc Huy', N'BS.CKI', N'LIC-NOI-002', N'0910000005', N'doctor.noi02@clinic.local', N'P.102', 1, CAST(N'2026-09-17T22:34:00.5800000' AS DateTime2), NULL, CAST(200000.00 AS Decimal(12, 2)), N'Bác sĩ chuyên khoa Nội khoa.', NULL, 6, 30)
GO
SET IDENTITY_INSERT [scheduling].[doctors] OFF
GO
SET IDENTITY_INSERT [scheduling].[patients] ON 
GO
INSERT [scheduling].[patients] ([id], [user_id], [full_name], [gender], [date_of_birth], [phone], [email], [address], [is_active], [created_at], [updated_at], [patient_code], [national_id], [insurance_number], [blood_type], [emergency_contact_name], [emergency_contact_phone]) VALUES (1, 5, N'Nguyễn Văn Test', NULL, CAST(N'2000-05-10' AS Date), N'0901234567', N'nguyenvantest@gmail.com', NULL, 1, CAST(N'2026-09-10T06:43:24.9175395' AS DateTime2), CAST(N'2026-09-16T07:08:47.5443033' AS DateTime2), N'BNTEST000000000001', NULL, NULL, NULL, NULL, NULL)
GO
INSERT [scheduling].[patients] ([id], [user_id], [full_name], [gender], [date_of_birth], [phone], [email], [address], [is_active], [created_at], [updated_at], [patient_code], [national_id], [insurance_number], [blood_type], [emergency_contact_name], [emergency_contact_phone]) VALUES (2, 1, N'Nguyễn Văn Mới', 0, CAST(N'2002-05-10' AS Date), N'0901111111', N'newpatient2@test.com', NULL, 1, CAST(N'2026-09-10T08:50:11.5845637' AS DateTime2), NULL, N'BNBD71779A872E4B938E', NULL, NULL, NULL, NULL, NULL)
GO
INSERT [scheduling].[patients] ([id], [user_id], [full_name], [gender], [date_of_birth], [phone], [email], [address], [is_active], [created_at], [updated_at], [patient_code], [national_id], [insurance_number], [blood_type], [emergency_contact_name], [emergency_contact_phone]) VALUES (3, 2, N'Trần Tài Khoản', NULL, CAST(N'1995-08-20' AS Date), N'0902222222', NULL, NULL, 1, CAST(N'2026-09-10T08:51:47.2848904' AS DateTime2), CAST(N'2026-09-10T08:52:06.0449393' AS DateTime2), N'BNTESTVANGLAI00001', NULL, NULL, NULL, NULL, NULL)
GO
INSERT [scheduling].[patients] ([id], [user_id], [full_name], [gender], [date_of_birth], [phone], [email], [address], [is_active], [created_at], [updated_at], [patient_code], [national_id], [insurance_number], [blood_type], [emergency_contact_name], [emergency_contact_phone]) VALUES (5, 4, N'Lieu Nguyen', NULL, CAST(N'2004-12-10' AS Date), N'0909999999', N'nguyenplase184463@fpt.edu.vn', NULL, 1, CAST(N'2026-09-16T06:54:03.4027152' AS DateTime2), NULL, N'BN78AD40B9E78942B096', NULL, NULL, NULL, NULL, NULL)
GO
INSERT [scheduling].[patients] ([id], [user_id], [full_name], [gender], [date_of_birth], [phone], [email], [address], [is_active], [created_at], [updated_at], [patient_code], [national_id], [insurance_number], [blood_type], [emergency_contact_name], [emergency_contact_phone]) VALUES (6, 6, N'Vy Tran', NULL, CAST(N'2007-12-10' AS Date), N'0902222223', N'vytran0908@gmail.com', NULL, 1, CAST(N'2026-09-16T07:13:29.0709607' AS DateTime2), NULL, N'BNE5AA3A16E9574BD99C', NULL, NULL, NULL, NULL, NULL)
GO
INSERT [scheduling].[patients] ([id], [user_id], [full_name], [gender], [date_of_birth], [phone], [email], [address], [is_active], [created_at], [updated_at], [patient_code], [national_id], [insurance_number], [blood_type], [emergency_contact_name], [emergency_contact_phone]) VALUES (7, 12, N'Anh Nguyen', NULL, CAST(N'2004-12-10' AS Date), N'0123454321', N'nguyenlieu10122004@gmail.com', NULL, 1, CAST(N'2026-09-24T04:24:25.5785571' AS DateTime2), NULL, N'BNEF5D65C382FC4C19B1', NULL, NULL, NULL, NULL, NULL)
GO
INSERT [scheduling].[patients] ([id], [user_id], [full_name], [gender], [date_of_birth], [phone], [email], [address], [is_active], [created_at], [updated_at], [patient_code], [national_id], [insurance_number], [blood_type], [emergency_contact_name], [emergency_contact_phone]) VALUES (8, NULL, N'Nguyễn Văn Test', 1, CAST(N'2000-05-20' AS Date), N'0909999001', N'testpatient@gmail.com', N'TP. Hồ Chí Minh', 1, CAST(N'2026-09-24T17:29:47.5581240' AS DateTime2), NULL, N'BNCDFAACEDA161489CBE', N'079200001234', N'DN4012345678901', N'O+', NULL, NULL)
GO
INSERT [scheduling].[patients] ([id], [user_id], [full_name], [gender], [date_of_birth], [phone], [email], [address], [is_active], [created_at], [updated_at], [patient_code], [national_id], [insurance_number], [blood_type], [emergency_contact_name], [emergency_contact_phone]) VALUES (9, NULL, N'Phan Duy', 1, CAST(N'2000-09-26' AS Date), N'0909999000', N'phanduy00gmail.com', NULL, 1, CAST(N'2026-09-25T04:32:36.1451251' AS DateTime2), NULL, N'BNA9702A9840874EB0AE', NULL, NULL, NULL, NULL, NULL)
GO
INSERT [scheduling].[patients] ([id], [user_id], [full_name], [gender], [date_of_birth], [phone], [email], [address], [is_active], [created_at], [updated_at], [patient_code], [national_id], [insurance_number], [blood_type], [emergency_contact_name], [emergency_contact_phone]) VALUES (10, NULL, N'Nguyen Van Test', NULL, NULL, N'0900000000', NULL, NULL, 1, CAST(N'2026-09-26T06:05:17.6188486' AS DateTime2), NULL, N'BN933E9B8DF3184076AF', NULL, NULL, NULL, NULL, NULL)
GO
INSERT [scheduling].[patients] ([id], [user_id], [full_name], [gender], [date_of_birth], [phone], [email], [address], [is_active], [created_at], [updated_at], [patient_code], [national_id], [insurance_number], [blood_type], [emergency_contact_name], [emergency_contact_phone]) VALUES (11, NULL, N'Phan Lan Anh', NULL, NULL, N'0123456789', NULL, NULL, 1, CAST(N'2026-09-26T18:20:05.8089070' AS DateTime2), NULL, N'BNF6BEA59504074BB2AA', NULL, NULL, NULL, NULL, NULL)
GO
SET IDENTITY_INSERT [scheduling].[patients] OFF
GO
SET IDENTITY_INSERT [scheduling].[specialties] ON 
GO
INSERT [scheduling].[specialties] ([id], [name], [description], [is_active], [created_at], [updated_at], [code]) VALUES (1, N'Nội khoa', NULL, 1, CAST(N'2026-09-11T17:47:34.0683612' AS DateTime2), NULL, N'NOI')
GO
INSERT [scheduling].[specialties] ([id], [name], [description], [is_active], [created_at], [updated_at], [code]) VALUES (2, N'Tim mạch', N'Khám và điều trị các bệnh lý về tim mạch.', 1, CAST(N'2026-09-17T22:15:34.8666667' AS DateTime2), NULL, N'CARD')
GO
INSERT [scheduling].[specialties] ([id], [name], [description], [is_active], [created_at], [updated_at], [code]) VALUES (3, N'Nội tổng quát', N'Khám và điều trị các bệnh lý nội khoa tổng quát.', 1, CAST(N'2026-09-17T22:15:34.8833333' AS DateTime2), NULL, N'GEN')
GO
INSERT [scheduling].[specialties] ([id], [name], [description], [is_active], [created_at], [updated_at], [code]) VALUES (4, N'Da liễu', N'Khám và điều trị các bệnh lý về da.', 1, CAST(N'2026-09-17T22:15:34.8866667' AS DateTime2), NULL, N'DERM')
GO
INSERT [scheduling].[specialties] ([id], [name], [description], [is_active], [created_at], [updated_at], [code]) VALUES (5, N'Nhi khoa', N'Khám và điều trị bệnh cho trẻ em.', 1, CAST(N'2026-09-17T22:15:34.8900000' AS DateTime2), NULL, N'PED')
GO
SET IDENTITY_INSERT [scheduling].[specialties] OFF
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_auth_audit_logs_entity]    Script Date: 09/10/2026 00:32:54 ******/
CREATE NONCLUSTERED INDEX [IX_auth_audit_logs_entity] ON [auth].[audit_logs]
(
	[entity_name] ASC,
	[entity_id] ASC,
	[occurred_at] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_auth_audit_logs_user_time]    Script Date: 09/10/2026 00:32:54 ******/
CREATE NONCLUSTERED INDEX [IX_auth_audit_logs_user_time] ON [auth].[audit_logs]
(
	[user_id] ASC,
	[occurred_at] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_auth_notifications_appointment]    Script Date: 09/10/2026 00:32:54 ******/
CREATE NONCLUSTERED INDEX [IX_auth_notifications_appointment] ON [auth].[notifications]
(
	[appointment_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_auth_notifications_status_scheduled]    Script Date: 09/10/2026 00:32:54 ******/
CREATE NONCLUSTERED INDEX [IX_auth_notifications_status_scheduled] ON [auth].[notifications]
(
	[status] ASC,
	[scheduled_at] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_password_reset_tokens_token_hash]    Script Date: 09/10/2026 00:32:54 ******/
ALTER TABLE [auth].[password_reset_tokens] ADD  CONSTRAINT [UQ_password_reset_tokens_token_hash] UNIQUE NONCLUSTERED 
(
	[token_hash] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_password_reset_tokens_user_id]    Script Date: 09/10/2026 00:32:54 ******/
CREATE NONCLUSTERED INDEX [IX_password_reset_tokens_user_id] ON [auth].[password_reset_tokens]
(
	[user_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_auth_refresh_tokens_token_hash]    Script Date: 09/10/2026 00:32:54 ******/
ALTER TABLE [auth].[refresh_tokens] ADD  CONSTRAINT [UQ_auth_refresh_tokens_token_hash] UNIQUE NONCLUSTERED 
(
	[token_hash] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_auth_roles_name]    Script Date: 09/10/2026 00:32:54 ******/
ALTER TABLE [auth].[roles] ADD  CONSTRAINT [UQ_auth_roles_name] UNIQUE NONCLUSTERED 
(
	[name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UQ_user_roles]    Script Date: 09/10/2026 00:32:54 ******/
ALTER TABLE [auth].[user_roles] ADD  CONSTRAINT [UQ_user_roles] UNIQUE NONCLUSTERED 
(
	[user_id] ASC,
	[role_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_auth_users_username]    Script Date: 09/10/2026 00:32:54 ******/
ALTER TABLE [auth].[users] ADD  CONSTRAINT [UQ_auth_users_username] UNIQUE NONCLUSTERED 
(
	[username] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_auth_users_email]    Script Date: 09/10/2026 00:32:54 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_auth_users_email] ON [auth].[users]
(
	[email] ASC
)
WHERE ([email] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_billing_insurance_patient]    Script Date: 09/10/2026 00:32:54 ******/
CREATE NONCLUSTERED INDEX [IX_billing_insurance_patient] ON [billing].[insurance_policies]
(
	[patient_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_invoices_appointment_id]    Script Date: 09/10/2026 00:32:54 ******/
CREATE NONCLUSTERED INDEX [IX_invoices_appointment_id] ON [billing].[invoices]
(
	[appointment_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_invoices_patient_id]    Script Date: 09/10/2026 00:32:54 ******/
CREATE NONCLUSTERED INDEX [IX_invoices_patient_id] ON [billing].[invoices]
(
	[patient_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_invoices_status_created_at]    Script Date: 09/10/2026 00:32:54 ******/
CREATE NONCLUSTERED INDEX [IX_invoices_status_created_at] ON [billing].[invoices]
(
	[status] ASC,
	[created_at] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_billing_invoices_no]    Script Date: 09/10/2026 00:32:54 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_billing_invoices_no] ON [billing].[invoices]
(
	[invoice_no] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_billing_services_code]    Script Date: 09/10/2026 00:32:54 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_billing_services_code] ON [billing].[services]
(
	[code] ASC
)
WHERE ([code] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_clinical_attachments_record]    Script Date: 09/10/2026 00:32:54 ******/
CREATE NONCLUSTERED INDEX [IX_clinical_attachments_record] ON [clinical].[attachments]
(
	[medical_record_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UQ_clinical_lab_results_order]    Script Date: 09/10/2026 00:32:54 ******/
ALTER TABLE [clinical].[lab_results] ADD  CONSTRAINT [UQ_clinical_lab_results_order] UNIQUE NONCLUSTERED 
(
	[medical_record_service_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_clinical_mrs_record_status]    Script Date: 09/10/2026 00:32:54 ******/
CREATE NONCLUSTERED INDEX [IX_clinical_mrs_record_status] ON [clinical].[medical_record_services]
(
	[medical_record_id] ASC,
	[status] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UQ_clinical_medical_records_appointment_id]    Script Date: 09/10/2026 00:32:54 ******/
ALTER TABLE [clinical].[medical_records] ADD  CONSTRAINT [UQ_clinical_medical_records_appointment_id] UNIQUE NONCLUSTERED 
(
	[appointment_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UQ_clinical_batches_id_medicine]    Script Date: 09/10/2026 00:32:54 ******/
ALTER TABLE [clinical].[medicine_batches] ADD  CONSTRAINT [UQ_clinical_batches_id_medicine] UNIQUE NONCLUSTERED 
(
	[id] ASC,
	[medicine_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ_clinical_batches_medicine_no]    Script Date: 09/10/2026 00:32:54 ******/
ALTER TABLE [clinical].[medicine_batches] ADD  CONSTRAINT [UQ_clinical_batches_medicine_no] UNIQUE NONCLUSTERED 
(
	[medicine_id] ASC,
	[batch_no] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_clinical_batches_expiry]    Script Date: 09/10/2026 00:32:54 ******/
CREATE NONCLUSTERED INDEX [IX_clinical_batches_expiry] ON [clinical].[medicine_batches]
(
	[expiry_date] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_clinical_stock_tx_batch_time]    Script Date: 09/10/2026 00:32:54 ******/
CREATE NONCLUSTERED INDEX [IX_clinical_stock_tx_batch_time] ON [clinical].[medicine_stock_transactions]
(
	[batch_id] ASC,
	[created_at] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_clinical_stock_tx_prescription]    Script Date: 09/10/2026 00:32:54 ******/
CREATE NONCLUSTERED INDEX [IX_clinical_stock_tx_prescription] ON [clinical].[medicine_stock_transactions]
(
	[prescription_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_clinical_medicines_code]    Script Date: 09/10/2026 00:32:54 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_clinical_medicines_code] ON [clinical].[medicines]
(
	[code] ASC
)
WHERE ([code] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_clinical_patient_allergies_patient]    Script Date: 09/10/2026 00:32:54 ******/
CREATE NONCLUSTERED INDEX [IX_clinical_patient_allergies_patient] ON [clinical].[patient_allergies]
(
	[patient_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UQ_clinical_patient_vitals_record]    Script Date: 09/10/2026 00:32:54 ******/
ALTER TABLE [clinical].[patient_vitals] ADD  CONSTRAINT [UQ_clinical_patient_vitals_record] UNIQUE NONCLUSTERED 
(
	[medical_record_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UQ_clinical_prescriptions_medical_record_id]    Script Date: 09/10/2026 00:32:54 ******/
ALTER TABLE [clinical].[prescriptions] ADD  CONSTRAINT [UQ_clinical_prescriptions_medical_record_id] UNIQUE NONCLUSTERED 
(
	[medical_record_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_appointments_doctor_start]    Script Date: 09/10/2026 00:32:54 ******/
CREATE NONCLUSTERED INDEX [IX_appointments_doctor_start] ON [scheduling].[appointments]
(
	[doctor_id] ASC,
	[start_time] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_appointments_patient_id]    Script Date: 09/10/2026 00:32:54 ******/
CREATE NONCLUSTERED INDEX [IX_appointments_patient_id] ON [scheduling].[appointments]
(
	[patient_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_appointments_start_time]    Script Date: 09/10/2026 00:32:54 ******/
CREATE NONCLUSTERED INDEX [IX_appointments_start_time] ON [scheduling].[appointments]
(
	[start_time] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UX_appointments_doctor_slot]    Script Date: 09/10/2026 00:32:54 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_appointments_doctor_slot] ON [scheduling].[appointments]
(
	[doctor_id] ASC,
	[start_time] ASC
)
WHERE ([status]<(4))
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_scheduling_appointments_code]    Script Date: 09/10/2026 00:32:54 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_scheduling_appointments_code] ON [scheduling].[appointments]
(
	[appointment_code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UX_scheduling_schedules_version]    Script Date: 09/10/2026 00:32:54 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_scheduling_schedules_version] ON [scheduling].[doctor_schedules]
(
	[doctor_id] ASC,
	[day_of_week] ASC,
	[start_time] ASC,
	[effective_from] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UQ_scheduling_doctors_user_id]    Script Date: 09/10/2026 00:32:54 ******/
ALTER TABLE [scheduling].[doctors] ADD  CONSTRAINT [UQ_scheduling_doctors_user_id] UNIQUE NONCLUSTERED 
(
	[user_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_scheduling_doctors_license_number]    Script Date: 09/10/2026 00:32:54 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_scheduling_doctors_license_number] ON [scheduling].[doctors]
(
	[license_number] ASC
)
WHERE ([license_number] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_patients_full_name]    Script Date: 09/10/2026 00:32:54 ******/
CREATE NONCLUSTERED INDEX [IX_patients_full_name] ON [scheduling].[patients]
(
	[full_name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_patients_phone]    Script Date: 09/10/2026 00:32:54 ******/
CREATE NONCLUSTERED INDEX [IX_patients_phone] ON [scheduling].[patients]
(
	[phone] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_scheduling_patients_code]    Script Date: 09/10/2026 00:32:54 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_scheduling_patients_code] ON [scheduling].[patients]
(
	[patient_code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UX_scheduling_patients_user_id]    Script Date: 09/10/2026 00:32:54 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_scheduling_patients_user_id] ON [scheduling].[patients]
(
	[user_id] ASC
)
WHERE ([user_id] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UX_scheduling_specialties_code]    Script Date: 09/10/2026 00:32:54 ******/
CREATE UNIQUE NONCLUSTERED INDEX [UX_scheduling_specialties_code] ON [scheduling].[specialties]
(
	[code] ASC
)
WHERE ([code] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [auth].[audit_logs] ADD  CONSTRAINT [DF_auth_audit_logs_succeeded]  DEFAULT ((1)) FOR [succeeded]
GO
ALTER TABLE [auth].[audit_logs] ADD  CONSTRAINT [DF_auth_audit_logs_occurred_at]  DEFAULT (sysutcdatetime()) FOR [occurred_at]
GO
ALTER TABLE [auth].[notifications] ADD  CONSTRAINT [DF_auth_notifications_status]  DEFAULT ((0)) FOR [status]
GO
ALTER TABLE [auth].[notifications] ADD  CONSTRAINT [DF_auth_notifications_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [auth].[password_reset_tokens] ADD  CONSTRAINT [DF_password_reset_tokens_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [auth].[refresh_tokens] ADD  CONSTRAINT [DF_auth_refresh_tokens_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [auth].[roles] ADD  CONSTRAINT [DF_auth_roles_is_system]  DEFAULT ((0)) FOR [is_system]
GO
ALTER TABLE [auth].[user_roles] ADD  CONSTRAINT [DF_auth_user_roles_assigned_at]  DEFAULT (sysutcdatetime()) FOR [assigned_at]
GO
ALTER TABLE [auth].[users] ADD  CONSTRAINT [DF_auth_users_is_active]  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [auth].[users] ADD  CONSTRAINT [DF_auth_users_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [auth].[users] ADD  CONSTRAINT [DF_auth_users_failed_login_count]  DEFAULT ((0)) FOR [failed_login_count]
GO
ALTER TABLE [auth].[users] ADD  CONSTRAINT [DF_auth_users_email_confirmed]  DEFAULT ((0)) FOR [email_confirmed]
GO
ALTER TABLE [auth].[users] ADD  CONSTRAINT [DF_auth_users_is_deleted]  DEFAULT ((0)) FOR [is_deleted]
GO
ALTER TABLE [billing].[insurance_policies] ADD  CONSTRAINT [DF_billing_insurance_active]  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [billing].[insurance_policies] ADD  CONSTRAINT [DF_billing_insurance_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [billing].[invoice_items] ADD  CONSTRAINT [DF_billing_invoice_items_quantity]  DEFAULT ((1)) FOR [quantity]
GO
ALTER TABLE [billing].[invoice_items] ADD  CONSTRAINT [DF_billing_invoice_items_unit_price]  DEFAULT ((0)) FOR [unit_price]
GO
ALTER TABLE [billing].[invoice_items] ADD  CONSTRAINT [DF_billing_invoice_items_amount]  DEFAULT ((0)) FOR [amount]
GO
ALTER TABLE [billing].[invoice_items] ADD  CONSTRAINT [DF_billing_invoice_items_discount]  DEFAULT ((0)) FOR [discount_amount]
GO
ALTER TABLE [billing].[invoices] ADD  CONSTRAINT [DF_billing_invoices_total_amount]  DEFAULT ((0)) FOR [total_amount]
GO
ALTER TABLE [billing].[invoices] ADD  CONSTRAINT [DF_billing_invoices_status]  DEFAULT ((0)) FOR [status]
GO
ALTER TABLE [billing].[invoices] ADD  CONSTRAINT [DF_billing_invoices_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [billing].[invoices] ADD  CONSTRAINT [DF_billing_invoices_discount]  DEFAULT ((0)) FOR [discount_amount]
GO
ALTER TABLE [billing].[invoices] ADD  CONSTRAINT [DF_billing_invoices_tax]  DEFAULT ((0)) FOR [tax_amount]
GO
ALTER TABLE [billing].[invoices] ADD  CONSTRAINT [DF_billing_invoices_paid]  DEFAULT ((0)) FOR [paid_amount]
GO
ALTER TABLE [billing].[invoices] ADD  CONSTRAINT [DF_billing_invoices_insurance]  DEFAULT ((0)) FOR [insurance_amount]
GO
ALTER TABLE [billing].[payments] ADD  CONSTRAINT [DF_billing_payments_method]  DEFAULT ((0)) FOR [method]
GO
ALTER TABLE [billing].[payments] ADD  CONSTRAINT [DF_billing_payments_paid_at]  DEFAULT (sysutcdatetime()) FOR [paid_at]
GO
ALTER TABLE [billing].[payments] ADD  CONSTRAINT [DF_billing_payments_is_refund]  DEFAULT ((0)) FOR [is_refund]
GO
ALTER TABLE [billing].[services] ADD  CONSTRAINT [DF_billing_services_price]  DEFAULT ((0)) FOR [price]
GO
ALTER TABLE [billing].[services] ADD  CONSTRAINT [DF_billing_services_is_active]  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [billing].[services] ADD  CONSTRAINT [DF_billing_services_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [clinical].[attachments] ADD  CONSTRAINT [DF_clinical_attachments_uploaded_at]  DEFAULT (sysutcdatetime()) FOR [uploaded_at]
GO
ALTER TABLE [clinical].[lab_results] ADD  CONSTRAINT [DF_clinical_lab_results_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [clinical].[medical_record_services] ADD  CONSTRAINT [DF_clinical_mrs_quantity]  DEFAULT ((1)) FOR [quantity]
GO
ALTER TABLE [clinical].[medical_record_services] ADD  CONSTRAINT [DF_clinical_mrs_status]  DEFAULT ((0)) FOR [status]
GO
ALTER TABLE [clinical].[medical_record_services] ADD  CONSTRAINT [DF_clinical_mrs_ordered_at]  DEFAULT (sysutcdatetime()) FOR [ordered_at]
GO
ALTER TABLE [clinical].[medical_records] ADD  CONSTRAINT [DF_clinical_medical_records_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [clinical].[medical_records] ADD  CONSTRAINT [DF_clinical_records_status]  DEFAULT ((0)) FOR [status]
GO
ALTER TABLE [clinical].[medicine_batches] ADD  CONSTRAINT [DF_clinical_batches_quantity]  DEFAULT ((0)) FOR [quantity]
GO
ALTER TABLE [clinical].[medicine_stock_transactions] ADD  CONSTRAINT [DF_clinical_stock_tx_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [clinical].[medicines] ADD  CONSTRAINT [DF_clinical_medicines_price]  DEFAULT ((0)) FOR [price]
GO
ALTER TABLE [clinical].[medicines] ADD  CONSTRAINT [DF_clinical_medicines_stock_quantity]  DEFAULT ((0)) FOR [stock_quantity]
GO
ALTER TABLE [clinical].[medicines] ADD  CONSTRAINT [DF_clinical_medicines_is_active]  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [clinical].[medicines] ADD  CONSTRAINT [DF_clinical_medicines_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [clinical].[medicines] ADD  CONSTRAINT [DF_clinical_medicines_min_stock]  DEFAULT ((0)) FOR [min_stock]
GO
ALTER TABLE [clinical].[prescription_items] ADD  CONSTRAINT [DF_clinical_prescription_items_quantity]  DEFAULT ((1)) FOR [quantity]
GO
ALTER TABLE [clinical].[prescriptions] ADD  CONSTRAINT [DF_clinical_prescriptions_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [clinical].[prescriptions] ADD  CONSTRAINT [DF_clinical_prescriptions_status]  DEFAULT ((0)) FOR [status]
GO
ALTER TABLE [scheduling].[appointment_status_history] ADD  CONSTRAINT [DF_scheduling_appointment_status_history_changed_at]  DEFAULT (sysutcdatetime()) FOR [changed_at]
GO
ALTER TABLE [scheduling].[appointments] ADD  CONSTRAINT [DF_scheduling_appointments_status]  DEFAULT ((0)) FOR [status]
GO
ALTER TABLE [scheduling].[appointments] ADD  CONSTRAINT [DF_scheduling_appointments_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [scheduling].[appointments] ADD  CONSTRAINT [DF_scheduling_appointments_source]  DEFAULT ((0)) FOR [source]
GO
ALTER TABLE [scheduling].[appointments] ADD  CONSTRAINT [DF_scheduling_appointments_fee_snapshot]  DEFAULT ((0)) FOR [fee_snapshot]
GO
ALTER TABLE [scheduling].[doctor_schedules] ADD  CONSTRAINT [DF_scheduling_doctor_schedules_slot_minutes]  DEFAULT ((30)) FOR [slot_minutes]
GO
ALTER TABLE [scheduling].[doctor_schedules] ADD  CONSTRAINT [DF_scheduling_doctor_schedules_is_active]  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [scheduling].[doctor_time_off] ADD  CONSTRAINT [DF_scheduling_doctor_time_off_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [scheduling].[doctor_time_off] ADD  CONSTRAINT [DF_scheduling_time_off_full_day]  DEFAULT ((0)) FOR [is_full_day]
GO
ALTER TABLE [scheduling].[doctors] ADD  CONSTRAINT [DF_scheduling_doctors_is_active]  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [scheduling].[doctors] ADD  CONSTRAINT [DF_scheduling_doctors_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [scheduling].[doctors] ADD  CONSTRAINT [DF_scheduling_doctors_consultation_fee]  DEFAULT ((0)) FOR [consultation_fee]
GO
ALTER TABLE [scheduling].[patients] ADD  CONSTRAINT [DF_scheduling_patients_is_active]  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [scheduling].[patients] ADD  CONSTRAINT [DF_scheduling_patients_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [scheduling].[specialties] ADD  CONSTRAINT [DF_scheduling_specialties_is_active]  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [scheduling].[specialties] ADD  CONSTRAINT [DF_scheduling_specialties_created_at]  DEFAULT (sysutcdatetime()) FOR [created_at]
GO
ALTER TABLE [auth].[audit_logs]  WITH CHECK ADD  CONSTRAINT [FK_auth_audit_logs_users] FOREIGN KEY([user_id])
REFERENCES [auth].[users] ([id])
GO
ALTER TABLE [auth].[audit_logs] CHECK CONSTRAINT [FK_auth_audit_logs_users]
GO
ALTER TABLE [auth].[notifications]  WITH CHECK ADD  CONSTRAINT [FK_auth_notifications_appointments] FOREIGN KEY([appointment_id])
REFERENCES [scheduling].[appointments] ([id])
GO
ALTER TABLE [auth].[notifications] CHECK CONSTRAINT [FK_auth_notifications_appointments]
GO
ALTER TABLE [auth].[notifications]  WITH CHECK ADD  CONSTRAINT [FK_auth_notifications_users] FOREIGN KEY([user_id])
REFERENCES [auth].[users] ([id])
GO
ALTER TABLE [auth].[notifications] CHECK CONSTRAINT [FK_auth_notifications_users]
GO
ALTER TABLE [auth].[password_reset_tokens]  WITH CHECK ADD  CONSTRAINT [FK_password_reset_tokens_users] FOREIGN KEY([user_id])
REFERENCES [auth].[users] ([id])
GO
ALTER TABLE [auth].[password_reset_tokens] CHECK CONSTRAINT [FK_password_reset_tokens_users]
GO
ALTER TABLE [auth].[refresh_tokens]  WITH CHECK ADD  CONSTRAINT [FK_auth_refresh_tokens_users] FOREIGN KEY([user_id])
REFERENCES [auth].[users] ([id])
GO
ALTER TABLE [auth].[refresh_tokens] CHECK CONSTRAINT [FK_auth_refresh_tokens_users]
GO
ALTER TABLE [auth].[user_roles]  WITH CHECK ADD  CONSTRAINT [FK_auth_user_roles_assigned_by] FOREIGN KEY([assigned_by])
REFERENCES [auth].[users] ([id])
GO
ALTER TABLE [auth].[user_roles] CHECK CONSTRAINT [FK_auth_user_roles_assigned_by]
GO
ALTER TABLE [auth].[user_roles]  WITH CHECK ADD  CONSTRAINT [FK_auth_user_roles_roles] FOREIGN KEY([role_id])
REFERENCES [auth].[roles] ([id])
GO
ALTER TABLE [auth].[user_roles] CHECK CONSTRAINT [FK_auth_user_roles_roles]
GO
ALTER TABLE [auth].[user_roles]  WITH CHECK ADD  CONSTRAINT [FK_auth_user_roles_users] FOREIGN KEY([user_id])
REFERENCES [auth].[users] ([id])
GO
ALTER TABLE [auth].[user_roles] CHECK CONSTRAINT [FK_auth_user_roles_users]
GO
ALTER TABLE [billing].[insurance_policies]  WITH CHECK ADD  CONSTRAINT [FK_billing_insurance_patients] FOREIGN KEY([patient_id])
REFERENCES [scheduling].[patients] ([id])
GO
ALTER TABLE [billing].[insurance_policies] CHECK CONSTRAINT [FK_billing_insurance_patients]
GO
ALTER TABLE [billing].[invoice_items]  WITH CHECK ADD  CONSTRAINT [FK_billing_invoice_items_invoices] FOREIGN KEY([invoice_id])
REFERENCES [billing].[invoices] ([id])
GO
ALTER TABLE [billing].[invoice_items] CHECK CONSTRAINT [FK_billing_invoice_items_invoices]
GO
ALTER TABLE [billing].[invoice_items]  WITH CHECK ADD  CONSTRAINT [FK_billing_invoice_items_medicines] FOREIGN KEY([medicine_id])
REFERENCES [clinical].[medicines] ([id])
GO
ALTER TABLE [billing].[invoice_items] CHECK CONSTRAINT [FK_billing_invoice_items_medicines]
GO
ALTER TABLE [billing].[invoice_items]  WITH CHECK ADD  CONSTRAINT [FK_billing_invoice_items_order] FOREIGN KEY([medical_record_service_id])
REFERENCES [clinical].[medical_record_services] ([id])
GO
ALTER TABLE [billing].[invoice_items] CHECK CONSTRAINT [FK_billing_invoice_items_order]
GO
ALTER TABLE [billing].[invoices]  WITH CHECK ADD  CONSTRAINT [FK_billing_invoices_appointments] FOREIGN KEY([appointment_id])
REFERENCES [scheduling].[appointments] ([id])
GO
ALTER TABLE [billing].[invoices] CHECK CONSTRAINT [FK_billing_invoices_appointments]
GO
ALTER TABLE [billing].[invoices]  WITH CHECK ADD  CONSTRAINT [FK_billing_invoices_medical_records] FOREIGN KEY([medical_record_id])
REFERENCES [clinical].[medical_records] ([id])
GO
ALTER TABLE [billing].[invoices] CHECK CONSTRAINT [FK_billing_invoices_medical_records]
GO
ALTER TABLE [billing].[invoices]  WITH CHECK ADD  CONSTRAINT [FK_billing_invoices_patients] FOREIGN KEY([patient_id])
REFERENCES [scheduling].[patients] ([id])
GO
ALTER TABLE [billing].[invoices] CHECK CONSTRAINT [FK_billing_invoices_patients]
GO
ALTER TABLE [billing].[invoices]  WITH CHECK ADD  CONSTRAINT [FK_billing_invoices_users] FOREIGN KEY([created_by])
REFERENCES [auth].[users] ([id])
GO
ALTER TABLE [billing].[invoices] CHECK CONSTRAINT [FK_billing_invoices_users]
GO
ALTER TABLE [billing].[payments]  WITH CHECK ADD  CONSTRAINT [FK_billing_payments_invoices] FOREIGN KEY([invoice_id])
REFERENCES [billing].[invoices] ([id])
GO
ALTER TABLE [billing].[payments] CHECK CONSTRAINT [FK_billing_payments_invoices]
GO
ALTER TABLE [billing].[payments]  WITH CHECK ADD  CONSTRAINT [FK_billing_payments_received_by] FOREIGN KEY([received_by])
REFERENCES [auth].[users] ([id])
GO
ALTER TABLE [billing].[payments] CHECK CONSTRAINT [FK_billing_payments_received_by]
GO
ALTER TABLE [billing].[services]  WITH CHECK ADD  CONSTRAINT [FK_billing_services_specialties] FOREIGN KEY([specialty_id])
REFERENCES [scheduling].[specialties] ([id])
GO
ALTER TABLE [billing].[services] CHECK CONSTRAINT [FK_billing_services_specialties]
GO
ALTER TABLE [clinical].[attachments]  WITH CHECK ADD  CONSTRAINT [FK_clinical_attachments_records] FOREIGN KEY([medical_record_id])
REFERENCES [clinical].[medical_records] ([id])
GO
ALTER TABLE [clinical].[attachments] CHECK CONSTRAINT [FK_clinical_attachments_records]
GO
ALTER TABLE [clinical].[attachments]  WITH CHECK ADD  CONSTRAINT [FK_clinical_attachments_users] FOREIGN KEY([uploaded_by])
REFERENCES [auth].[users] ([id])
GO
ALTER TABLE [clinical].[attachments] CHECK CONSTRAINT [FK_clinical_attachments_users]
GO
ALTER TABLE [clinical].[lab_results]  WITH CHECK ADD  CONSTRAINT [FK_clinical_lab_results_order] FOREIGN KEY([medical_record_service_id])
REFERENCES [clinical].[medical_record_services] ([id])
GO
ALTER TABLE [clinical].[lab_results] CHECK CONSTRAINT [FK_clinical_lab_results_order]
GO
ALTER TABLE [clinical].[lab_results]  WITH CHECK ADD  CONSTRAINT [FK_clinical_lab_results_users] FOREIGN KEY([recorded_by])
REFERENCES [auth].[users] ([id])
GO
ALTER TABLE [clinical].[lab_results] CHECK CONSTRAINT [FK_clinical_lab_results_users]
GO
ALTER TABLE [clinical].[medical_record_services]  WITH CHECK ADD  CONSTRAINT [FK_clinical_mrs_performed_by] FOREIGN KEY([performed_by])
REFERENCES [auth].[users] ([id])
GO
ALTER TABLE [clinical].[medical_record_services] CHECK CONSTRAINT [FK_clinical_mrs_performed_by]
GO
ALTER TABLE [clinical].[medical_record_services]  WITH CHECK ADD  CONSTRAINT [FK_clinical_mrs_records] FOREIGN KEY([medical_record_id])
REFERENCES [clinical].[medical_records] ([id])
GO
ALTER TABLE [clinical].[medical_record_services] CHECK CONSTRAINT [FK_clinical_mrs_records]
GO
ALTER TABLE [clinical].[medical_record_services]  WITH CHECK ADD  CONSTRAINT [FK_clinical_mrs_services] FOREIGN KEY([service_id])
REFERENCES [billing].[services] ([id])
GO
ALTER TABLE [clinical].[medical_record_services] CHECK CONSTRAINT [FK_clinical_mrs_services]
GO
ALTER TABLE [clinical].[medical_records]  WITH CHECK ADD  CONSTRAINT [FK_clinical_medical_records_appointments] FOREIGN KEY([appointment_id])
REFERENCES [scheduling].[appointments] ([id])
GO
ALTER TABLE [clinical].[medical_records] CHECK CONSTRAINT [FK_clinical_medical_records_appointments]
GO
ALTER TABLE [clinical].[medical_records]  WITH CHECK ADD  CONSTRAINT [FK_clinical_medical_records_doctors] FOREIGN KEY([doctor_id])
REFERENCES [scheduling].[doctors] ([id])
GO
ALTER TABLE [clinical].[medical_records] CHECK CONSTRAINT [FK_clinical_medical_records_doctors]
GO
ALTER TABLE [clinical].[medical_records]  WITH CHECK ADD  CONSTRAINT [FK_clinical_medical_records_patients] FOREIGN KEY([patient_id])
REFERENCES [scheduling].[patients] ([id])
GO
ALTER TABLE [clinical].[medical_records] CHECK CONSTRAINT [FK_clinical_medical_records_patients]
GO
ALTER TABLE [clinical].[medicine_batches]  WITH CHECK ADD  CONSTRAINT [FK_clinical_batches_medicines] FOREIGN KEY([medicine_id])
REFERENCES [clinical].[medicines] ([id])
GO
ALTER TABLE [clinical].[medicine_batches] CHECK CONSTRAINT [FK_clinical_batches_medicines]
GO
ALTER TABLE [clinical].[medicine_stock_transactions]  WITH CHECK ADD  CONSTRAINT [FK_clinical_stock_tx_batch_medicine] FOREIGN KEY([batch_id], [medicine_id])
REFERENCES [clinical].[medicine_batches] ([id], [medicine_id])
GO
ALTER TABLE [clinical].[medicine_stock_transactions] CHECK CONSTRAINT [FK_clinical_stock_tx_batch_medicine]
GO
ALTER TABLE [clinical].[medicine_stock_transactions]  WITH CHECK ADD  CONSTRAINT [FK_clinical_stock_tx_medicines] FOREIGN KEY([medicine_id])
REFERENCES [clinical].[medicines] ([id])
GO
ALTER TABLE [clinical].[medicine_stock_transactions] CHECK CONSTRAINT [FK_clinical_stock_tx_medicines]
GO
ALTER TABLE [clinical].[medicine_stock_transactions]  WITH CHECK ADD  CONSTRAINT [FK_clinical_stock_tx_prescriptions] FOREIGN KEY([prescription_id])
REFERENCES [clinical].[prescriptions] ([id])
GO
ALTER TABLE [clinical].[medicine_stock_transactions] CHECK CONSTRAINT [FK_clinical_stock_tx_prescriptions]
GO
ALTER TABLE [clinical].[medicine_stock_transactions]  WITH CHECK ADD  CONSTRAINT [FK_clinical_stock_tx_reversal] FOREIGN KEY([reversal_of_transaction_id])
REFERENCES [clinical].[medicine_stock_transactions] ([id])
GO
ALTER TABLE [clinical].[medicine_stock_transactions] CHECK CONSTRAINT [FK_clinical_stock_tx_reversal]
GO
ALTER TABLE [clinical].[medicine_stock_transactions]  WITH CHECK ADD  CONSTRAINT [FK_clinical_stock_tx_users] FOREIGN KEY([created_by])
REFERENCES [auth].[users] ([id])
GO
ALTER TABLE [clinical].[medicine_stock_transactions] CHECK CONSTRAINT [FK_clinical_stock_tx_users]
GO
ALTER TABLE [clinical].[patient_allergies]  WITH CHECK ADD  CONSTRAINT [FK_clinical_patient_allergies_patients] FOREIGN KEY([patient_id])
REFERENCES [scheduling].[patients] ([id])
GO
ALTER TABLE [clinical].[patient_allergies] CHECK CONSTRAINT [FK_clinical_patient_allergies_patients]
GO
ALTER TABLE [clinical].[patient_vitals]  WITH CHECK ADD  CONSTRAINT [FK_clinical_patient_vitals_records] FOREIGN KEY([medical_record_id])
REFERENCES [clinical].[medical_records] ([id])
GO
ALTER TABLE [clinical].[patient_vitals] CHECK CONSTRAINT [FK_clinical_patient_vitals_records]
GO
ALTER TABLE [clinical].[prescription_items]  WITH CHECK ADD  CONSTRAINT [FK_clinical_prescription_items_medicines] FOREIGN KEY([medicine_id])
REFERENCES [clinical].[medicines] ([id])
GO
ALTER TABLE [clinical].[prescription_items] CHECK CONSTRAINT [FK_clinical_prescription_items_medicines]
GO
ALTER TABLE [clinical].[prescription_items]  WITH CHECK ADD  CONSTRAINT [FK_clinical_prescription_items_prescriptions] FOREIGN KEY([prescription_id])
REFERENCES [clinical].[prescriptions] ([id])
GO
ALTER TABLE [clinical].[prescription_items] CHECK CONSTRAINT [FK_clinical_prescription_items_prescriptions]
GO
ALTER TABLE [clinical].[prescriptions]  WITH CHECK ADD  CONSTRAINT [FK_clinical_prescriptions_dispensed_by] FOREIGN KEY([dispensed_by])
REFERENCES [auth].[users] ([id])
GO
ALTER TABLE [clinical].[prescriptions] CHECK CONSTRAINT [FK_clinical_prescriptions_dispensed_by]
GO
ALTER TABLE [clinical].[prescriptions]  WITH CHECK ADD  CONSTRAINT [FK_clinical_prescriptions_doctors] FOREIGN KEY([doctor_id])
REFERENCES [scheduling].[doctors] ([id])
GO
ALTER TABLE [clinical].[prescriptions] CHECK CONSTRAINT [FK_clinical_prescriptions_doctors]
GO
ALTER TABLE [clinical].[prescriptions]  WITH CHECK ADD  CONSTRAINT [FK_clinical_prescriptions_medical_records] FOREIGN KEY([medical_record_id])
REFERENCES [clinical].[medical_records] ([id])
GO
ALTER TABLE [clinical].[prescriptions] CHECK CONSTRAINT [FK_clinical_prescriptions_medical_records]
GO
ALTER TABLE [scheduling].[appointment_status_history]  WITH CHECK ADD  CONSTRAINT [FK_scheduling_appointment_status_history_appointments] FOREIGN KEY([appointment_id])
REFERENCES [scheduling].[appointments] ([id])
GO
ALTER TABLE [scheduling].[appointment_status_history] CHECK CONSTRAINT [FK_scheduling_appointment_status_history_appointments]
GO
ALTER TABLE [scheduling].[appointment_status_history]  WITH CHECK ADD  CONSTRAINT [FK_scheduling_appointment_status_history_users] FOREIGN KEY([changed_by])
REFERENCES [auth].[users] ([id])
GO
ALTER TABLE [scheduling].[appointment_status_history] CHECK CONSTRAINT [FK_scheduling_appointment_status_history_users]
GO
ALTER TABLE [scheduling].[appointments]  WITH CHECK ADD  CONSTRAINT [FK_scheduling_appointments_doctors] FOREIGN KEY([doctor_id])
REFERENCES [scheduling].[doctors] ([id])
GO
ALTER TABLE [scheduling].[appointments] CHECK CONSTRAINT [FK_scheduling_appointments_doctors]
GO
ALTER TABLE [scheduling].[appointments]  WITH CHECK ADD  CONSTRAINT [FK_scheduling_appointments_patients] FOREIGN KEY([patient_id])
REFERENCES [scheduling].[patients] ([id])
GO
ALTER TABLE [scheduling].[appointments] CHECK CONSTRAINT [FK_scheduling_appointments_patients]
GO
ALTER TABLE [scheduling].[appointments]  WITH CHECK ADD  CONSTRAINT [FK_scheduling_appointments_users] FOREIGN KEY([created_by])
REFERENCES [auth].[users] ([id])
GO
ALTER TABLE [scheduling].[appointments] CHECK CONSTRAINT [FK_scheduling_appointments_users]
GO
ALTER TABLE [scheduling].[doctor_schedules]  WITH CHECK ADD  CONSTRAINT [FK_scheduling_doctor_schedules_doctors] FOREIGN KEY([doctor_id])
REFERENCES [scheduling].[doctors] ([id])
GO
ALTER TABLE [scheduling].[doctor_schedules] CHECK CONSTRAINT [FK_scheduling_doctor_schedules_doctors]
GO
ALTER TABLE [scheduling].[doctor_time_off]  WITH CHECK ADD  CONSTRAINT [FK_scheduling_doctor_time_off_doctors] FOREIGN KEY([doctor_id])
REFERENCES [scheduling].[doctors] ([id])
GO
ALTER TABLE [scheduling].[doctor_time_off] CHECK CONSTRAINT [FK_scheduling_doctor_time_off_doctors]
GO
ALTER TABLE [scheduling].[doctor_time_off]  WITH CHECK ADD  CONSTRAINT [FK_scheduling_time_off_approved_by] FOREIGN KEY([approved_by])
REFERENCES [auth].[users] ([id])
GO
ALTER TABLE [scheduling].[doctor_time_off] CHECK CONSTRAINT [FK_scheduling_time_off_approved_by]
GO
ALTER TABLE [scheduling].[doctors]  WITH CHECK ADD  CONSTRAINT [FK_scheduling_doctors_specialties] FOREIGN KEY([specialty_id])
REFERENCES [scheduling].[specialties] ([id])
GO
ALTER TABLE [scheduling].[doctors] CHECK CONSTRAINT [FK_scheduling_doctors_specialties]
GO
ALTER TABLE [scheduling].[doctors]  WITH CHECK ADD  CONSTRAINT [FK_scheduling_doctors_users] FOREIGN KEY([user_id])
REFERENCES [auth].[users] ([id])
GO
ALTER TABLE [scheduling].[doctors] CHECK CONSTRAINT [FK_scheduling_doctors_users]
GO
ALTER TABLE [scheduling].[patients]  WITH CHECK ADD  CONSTRAINT [FK_scheduling_patients_users] FOREIGN KEY([user_id])
REFERENCES [auth].[users] ([id])
GO
ALTER TABLE [scheduling].[patients] CHECK CONSTRAINT [FK_scheduling_patients_users]
GO
ALTER TABLE [auth].[notifications]  WITH CHECK ADD  CONSTRAINT [CK_auth_notifications_channel] CHECK  (([channel]='InApp' OR [channel]='Zalo' OR [channel]='SMS' OR [channel]='Email'))
GO
ALTER TABLE [auth].[notifications] CHECK CONSTRAINT [CK_auth_notifications_channel]
GO
ALTER TABLE [auth].[notifications]  WITH CHECK ADD  CONSTRAINT [CK_auth_notifications_status] CHECK  (([status]>=(0) AND [status]<=(3)))
GO
ALTER TABLE [auth].[notifications] CHECK CONSTRAINT [CK_auth_notifications_status]
GO
ALTER TABLE [auth].[users]  WITH CHECK ADD  CONSTRAINT [CK_auth_users_failed_login_count] CHECK  (([failed_login_count]>=(0)))
GO
ALTER TABLE [auth].[users] CHECK CONSTRAINT [CK_auth_users_failed_login_count]
GO
ALTER TABLE [billing].[insurance_policies]  WITH CHECK ADD  CONSTRAINT [CK_billing_insurance_coverage] CHECK  (([coverage_percent] IS NULL OR [coverage_percent]>=(0) AND [coverage_percent]<=(100)))
GO
ALTER TABLE [billing].[insurance_policies] CHECK CONSTRAINT [CK_billing_insurance_coverage]
GO
ALTER TABLE [billing].[insurance_policies]  WITH CHECK ADD  CONSTRAINT [CK_billing_insurance_dates] CHECK  (([valid_from] IS NULL OR [valid_to] IS NULL OR [valid_to]>=[valid_from]))
GO
ALTER TABLE [billing].[insurance_policies] CHECK CONSTRAINT [CK_billing_insurance_dates]
GO
ALTER TABLE [billing].[invoice_items]  WITH CHECK ADD  CONSTRAINT [CK_billing_invoice_items_amounts] CHECK  (([quantity]>(0) AND [unit_price]>=(0) AND [discount_amount]>=(0) AND [amount]>=(0) AND [discount_amount]<=[quantity]*[unit_price] AND [amount]=([quantity]*[unit_price]-[discount_amount])))
GO
ALTER TABLE [billing].[invoice_items] CHECK CONSTRAINT [CK_billing_invoice_items_amounts]
GO
ALTER TABLE [billing].[invoice_items]  WITH CHECK ADD  CONSTRAINT [CK_billing_invoice_items_one_source] CHECK  (([medical_record_service_id] IS NOT NULL AND [medicine_id] IS NULL OR [medical_record_service_id] IS NULL AND [medicine_id] IS NOT NULL))
GO
ALTER TABLE [billing].[invoice_items] CHECK CONSTRAINT [CK_billing_invoice_items_one_source]
GO
ALTER TABLE [billing].[invoices]  WITH CHECK ADD  CONSTRAINT [CK_billing_invoices_amounts] CHECK  (([total_amount]>=(0) AND [discount_amount]>=(0) AND [tax_amount]>=(0) AND [paid_amount]>=(0) AND [insurance_amount]>=(0) AND ([discount_amount]+[insurance_amount])<=([total_amount]+[tax_amount]) AND [paid_amount]<=((([total_amount]+[tax_amount])-[discount_amount])-[insurance_amount])))
GO
ALTER TABLE [billing].[invoices] CHECK CONSTRAINT [CK_billing_invoices_amounts]
GO
ALTER TABLE [billing].[invoices]  WITH CHECK ADD  CONSTRAINT [CK_billing_invoices_cancelled] CHECK  (([status]=(3) AND [cancelled_at] IS NOT NULL OR [status]<>(3) AND [cancelled_at] IS NULL))
GO
ALTER TABLE [billing].[invoices] CHECK CONSTRAINT [CK_billing_invoices_cancelled]
GO
ALTER TABLE [billing].[invoices]  WITH CHECK ADD  CONSTRAINT [CK_billing_invoices_status] CHECK  (([status]>=(0) AND [status]<=(3)))
GO
ALTER TABLE [billing].[invoices] CHECK CONSTRAINT [CK_billing_invoices_status]
GO
ALTER TABLE [billing].[payments]  WITH CHECK ADD  CONSTRAINT [CK_billing_payments_amount] CHECK  (([is_refund]=(0) AND [amount]>(0) OR [is_refund]=(1) AND [amount]<(0)))
GO
ALTER TABLE [billing].[payments] CHECK CONSTRAINT [CK_billing_payments_amount]
GO
ALTER TABLE [billing].[payments]  WITH CHECK ADD  CONSTRAINT [CK_billing_payments_method] CHECK  (([method]>=(0) AND [method]<=(3)))
GO
ALTER TABLE [billing].[payments] CHECK CONSTRAINT [CK_billing_payments_method]
GO
ALTER TABLE [billing].[services]  WITH CHECK ADD  CONSTRAINT [CK_billing_services_duration] CHECK  (([duration_minutes] IS NULL OR [duration_minutes]>(0)))
GO
ALTER TABLE [billing].[services] CHECK CONSTRAINT [CK_billing_services_duration]
GO
ALTER TABLE [billing].[services]  WITH CHECK ADD  CONSTRAINT [CK_billing_services_price] CHECK  (([price]>=(0)))
GO
ALTER TABLE [billing].[services] CHECK CONSTRAINT [CK_billing_services_price]
GO
ALTER TABLE [clinical].[medical_record_services]  WITH CHECK ADD  CONSTRAINT [CK_clinical_mrs_price] CHECK  (([unit_price_snapshot]>=(0)))
GO
ALTER TABLE [clinical].[medical_record_services] CHECK CONSTRAINT [CK_clinical_mrs_price]
GO
ALTER TABLE [clinical].[medical_record_services]  WITH CHECK ADD  CONSTRAINT [CK_clinical_mrs_quantity] CHECK  (([quantity]>(0)))
GO
ALTER TABLE [clinical].[medical_record_services] CHECK CONSTRAINT [CK_clinical_mrs_quantity]
GO
ALTER TABLE [clinical].[medical_record_services]  WITH CHECK ADD  CONSTRAINT [CK_clinical_mrs_status] CHECK  (([status]>=(0) AND [status]<=(3)))
GO
ALTER TABLE [clinical].[medical_record_services] CHECK CONSTRAINT [CK_clinical_mrs_status]
GO
ALTER TABLE [clinical].[medical_records]  WITH CHECK ADD  CONSTRAINT [CK_clinical_records_finalized] CHECK  (([status]=(0) AND [finalized_at] IS NULL OR [status]=(1) AND [finalized_at] IS NOT NULL))
GO
ALTER TABLE [clinical].[medical_records] CHECK CONSTRAINT [CK_clinical_records_finalized]
GO
ALTER TABLE [clinical].[medical_records]  WITH CHECK ADD  CONSTRAINT [CK_clinical_records_status] CHECK  (([status]>=(0) AND [status]<=(1)))
GO
ALTER TABLE [clinical].[medical_records] CHECK CONSTRAINT [CK_clinical_records_status]
GO
ALTER TABLE [clinical].[medicine_batches]  WITH CHECK ADD  CONSTRAINT [CK_clinical_batches_price] CHECK  (([import_price] IS NULL OR [import_price]>=(0)))
GO
ALTER TABLE [clinical].[medicine_batches] CHECK CONSTRAINT [CK_clinical_batches_price]
GO
ALTER TABLE [clinical].[medicine_batches]  WITH CHECK ADD  CONSTRAINT [CK_clinical_batches_quantity] CHECK  (([quantity]>=(0)))
GO
ALTER TABLE [clinical].[medicine_batches] CHECK CONSTRAINT [CK_clinical_batches_quantity]
GO
ALTER TABLE [clinical].[medicine_stock_transactions]  WITH CHECK ADD  CONSTRAINT [CK_clinical_stock_tx_balances] CHECK  (([quantity_before]>=(0) AND [quantity_after]>=(0)))
GO
ALTER TABLE [clinical].[medicine_stock_transactions] CHECK CONSTRAINT [CK_clinical_stock_tx_balances]
GO
ALTER TABLE [clinical].[medicine_stock_transactions]  WITH CHECK ADD  CONSTRAINT [CK_clinical_stock_tx_quantity] CHECK  (([quantity]>(0)))
GO
ALTER TABLE [clinical].[medicine_stock_transactions] CHECK CONSTRAINT [CK_clinical_stock_tx_quantity]
GO
ALTER TABLE [clinical].[medicine_stock_transactions]  WITH CHECK ADD  CONSTRAINT [CK_clinical_stock_tx_type] CHECK  (([type]=(0) AND [quantity_after]=([quantity_before]+[quantity]) OR [type]=(1) AND [quantity_before]=([quantity_after]+[quantity]) OR [type]=(2) AND ([quantity_after]=([quantity_before]+[quantity]) OR [quantity_before]=([quantity_after]+[quantity]))))
GO
ALTER TABLE [clinical].[medicine_stock_transactions] CHECK CONSTRAINT [CK_clinical_stock_tx_type]
GO
ALTER TABLE [clinical].[medicines]  WITH CHECK ADD  CONSTRAINT [CK_clinical_medicines_cost] CHECK  (([cost_price] IS NULL OR [cost_price]>=(0)))
GO
ALTER TABLE [clinical].[medicines] CHECK CONSTRAINT [CK_clinical_medicines_cost]
GO
ALTER TABLE [clinical].[medicines]  WITH CHECK ADD  CONSTRAINT [CK_clinical_medicines_price] CHECK  (([price]>=(0)))
GO
ALTER TABLE [clinical].[medicines] CHECK CONSTRAINT [CK_clinical_medicines_price]
GO
ALTER TABLE [clinical].[medicines]  WITH CHECK ADD  CONSTRAINT [CK_clinical_medicines_stock] CHECK  (([stock_quantity]>=(0) AND [min_stock]>=(0)))
GO
ALTER TABLE [clinical].[medicines] CHECK CONSTRAINT [CK_clinical_medicines_stock]
GO
ALTER TABLE [clinical].[patient_vitals]  WITH CHECK ADD  CONSTRAINT [CK_clinical_patient_vitals_values] CHECK  ((([temperature] IS NULL OR [temperature]>=(0)) AND ([pulse] IS NULL OR [pulse]>(0)) AND ([weight] IS NULL OR [weight]>(0)) AND ([height] IS NULL OR [height]>(0))))
GO
ALTER TABLE [clinical].[patient_vitals] CHECK CONSTRAINT [CK_clinical_patient_vitals_values]
GO
ALTER TABLE [clinical].[prescription_items]  WITH CHECK ADD  CONSTRAINT [CK_clinical_prescription_items_duration] CHECK  (([duration_days] IS NULL OR [duration_days]>(0)))
GO
ALTER TABLE [clinical].[prescription_items] CHECK CONSTRAINT [CK_clinical_prescription_items_duration]
GO
ALTER TABLE [clinical].[prescription_items]  WITH CHECK ADD  CONSTRAINT [CK_clinical_prescription_items_price] CHECK  (([unit_price_snapshot]>=(0)))
GO
ALTER TABLE [clinical].[prescription_items] CHECK CONSTRAINT [CK_clinical_prescription_items_price]
GO
ALTER TABLE [clinical].[prescription_items]  WITH CHECK ADD  CONSTRAINT [CK_clinical_prescription_items_quantity] CHECK  (([quantity]>(0)))
GO
ALTER TABLE [clinical].[prescription_items] CHECK CONSTRAINT [CK_clinical_prescription_items_quantity]
GO
ALTER TABLE [clinical].[prescriptions]  WITH CHECK ADD  CONSTRAINT [CK_clinical_prescriptions_dispensed] CHECK  (([status]<>(2) OR [dispensed_at] IS NOT NULL AND [dispensed_by] IS NOT NULL))
GO
ALTER TABLE [clinical].[prescriptions] CHECK CONSTRAINT [CK_clinical_prescriptions_dispensed]
GO
ALTER TABLE [clinical].[prescriptions]  WITH CHECK ADD  CONSTRAINT [CK_clinical_prescriptions_status] CHECK  (([status]>=(0) AND [status]<=(3)))
GO
ALTER TABLE [clinical].[prescriptions] CHECK CONSTRAINT [CK_clinical_prescriptions_status]
GO
ALTER TABLE [scheduling].[appointment_status_history]  WITH CHECK ADD  CONSTRAINT [CK_scheduling_history_from] CHECK  (([from_status] IS NULL OR [from_status]>=(0) AND [from_status]<=(5)))
GO
ALTER TABLE [scheduling].[appointment_status_history] CHECK CONSTRAINT [CK_scheduling_history_from]
GO
ALTER TABLE [scheduling].[appointment_status_history]  WITH CHECK ADD  CONSTRAINT [CK_scheduling_history_to] CHECK  (([to_status]>=(0) AND [to_status]<=(5)))
GO
ALTER TABLE [scheduling].[appointment_status_history] CHECK CONSTRAINT [CK_scheduling_history_to]
GO
ALTER TABLE [scheduling].[appointments]  WITH CHECK ADD  CONSTRAINT [CK_scheduling_appointments_fee] CHECK  (([fee_snapshot]>=(0)))
GO
ALTER TABLE [scheduling].[appointments] CHECK CONSTRAINT [CK_scheduling_appointments_fee]
GO
ALTER TABLE [scheduling].[appointments]  WITH CHECK ADD  CONSTRAINT [CK_scheduling_appointments_queue] CHECK  (([queue_number] IS NULL OR [queue_number]>(0)))
GO
ALTER TABLE [scheduling].[appointments] CHECK CONSTRAINT [CK_scheduling_appointments_queue]
GO
ALTER TABLE [scheduling].[appointments]  WITH CHECK ADD  CONSTRAINT [CK_scheduling_appointments_source] CHECK  (([source]>=(0) AND [source]<=(2)))
GO
ALTER TABLE [scheduling].[appointments] CHECK CONSTRAINT [CK_scheduling_appointments_source]
GO
ALTER TABLE [scheduling].[appointments]  WITH CHECK ADD  CONSTRAINT [CK_scheduling_appointments_status] CHECK  (([status]>=(0) AND [status]<=(5)))
GO
ALTER TABLE [scheduling].[appointments] CHECK CONSTRAINT [CK_scheduling_appointments_status]
GO
ALTER TABLE [scheduling].[appointments]  WITH CHECK ADD  CONSTRAINT [CK_scheduling_appointments_time] CHECK  (([start_time]<[end_time]))
GO
ALTER TABLE [scheduling].[appointments] CHECK CONSTRAINT [CK_scheduling_appointments_time]
GO
ALTER TABLE [scheduling].[doctor_schedules]  WITH CHECK ADD  CONSTRAINT [CK_scheduling_schedules_break] CHECK  (([break_start] IS NULL AND [break_end] IS NULL OR [break_start] IS NOT NULL AND [break_end] IS NOT NULL AND [start_time]<=[break_start] AND [break_start]<[break_end] AND [break_end]<=[end_time]))
GO
ALTER TABLE [scheduling].[doctor_schedules] CHECK CONSTRAINT [CK_scheduling_schedules_break]
GO
ALTER TABLE [scheduling].[doctor_schedules]  WITH CHECK ADD  CONSTRAINT [CK_scheduling_schedules_day] CHECK  (([day_of_week]>=(0) AND [day_of_week]<=(6)))
GO
ALTER TABLE [scheduling].[doctor_schedules] CHECK CONSTRAINT [CK_scheduling_schedules_day]
GO
ALTER TABLE [scheduling].[doctor_schedules]  WITH CHECK ADD  CONSTRAINT [CK_scheduling_schedules_effective] CHECK  (([effective_to] IS NULL OR [effective_to]>=[effective_from]))
GO
ALTER TABLE [scheduling].[doctor_schedules] CHECK CONSTRAINT [CK_scheduling_schedules_effective]
GO
ALTER TABLE [scheduling].[doctor_schedules]  WITH CHECK ADD  CONSTRAINT [CK_scheduling_schedules_slot] CHECK  (([slot_minutes]>(0)))
GO
ALTER TABLE [scheduling].[doctor_schedules] CHECK CONSTRAINT [CK_scheduling_schedules_slot]
GO
ALTER TABLE [scheduling].[doctor_schedules]  WITH CHECK ADD  CONSTRAINT [CK_scheduling_schedules_time] CHECK  (([start_time]<[end_time]))
GO
ALTER TABLE [scheduling].[doctor_schedules] CHECK CONSTRAINT [CK_scheduling_schedules_time]
GO
ALTER TABLE [scheduling].[doctor_time_off]  WITH CHECK ADD  CONSTRAINT [CK_scheduling_time_off_time] CHECK  (([start_at]<[end_at]))
GO
ALTER TABLE [scheduling].[doctor_time_off] CHECK CONSTRAINT [CK_scheduling_time_off_time]
GO
ALTER TABLE [scheduling].[doctors]  WITH CHECK ADD  CONSTRAINT [CK_scheduling_doctors_consultation_fee] CHECK  (([consultation_fee]>=(0)))
GO
ALTER TABLE [scheduling].[doctors] CHECK CONSTRAINT [CK_scheduling_doctors_consultation_fee]
GO
ALTER TABLE [scheduling].[doctors]  WITH CHECK ADD  CONSTRAINT [CK_scheduling_doctors_experience] CHECK  (([experience_years] IS NULL OR [experience_years]>=(0)))
GO
ALTER TABLE [scheduling].[doctors] CHECK CONSTRAINT [CK_scheduling_doctors_experience]
GO
ALTER TABLE [scheduling].[doctors]  WITH CHECK ADD  CONSTRAINT [CK_scheduling_doctors_max_patients] CHECK  (([max_patients_per_day] IS NULL OR [max_patients_per_day]>(0)))
GO
ALTER TABLE [scheduling].[doctors] CHECK CONSTRAINT [CK_scheduling_doctors_max_patients]
GO
ALTER TABLE [scheduling].[patients]  WITH CHECK ADD  CONSTRAINT [CK_scheduling_patients_gender] CHECK  (([gender] IS NULL OR [gender]>=(0) AND [gender]<=(2)))
GO
ALTER TABLE [scheduling].[patients] CHECK CONSTRAINT [CK_scheduling_patients_gender]
GO
EXEC sys.sp_addextendedproperty @name=N'ClinicSchemaVersion', @value=N'003' 
GO
