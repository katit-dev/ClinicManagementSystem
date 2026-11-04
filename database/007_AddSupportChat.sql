/* Additive Database First migration. Run explicitly on the chosen database.
   No init, seed, DROP, account creation or automatic startup migration.
   Back up the environment first. Re-running a complete migration is a no-op;
   a partial existing installation is rejected for inspection instead of overwritten. */
SET XACT_ABORT ON;
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
IF @@TRANCOUNT <> 0 THROW 51100, 'Run without an existing transaction.', 1;
IF OBJECT_ID('scheduling.patients','U') IS NULL OR OBJECT_ID('auth.users','U') IS NULL
    THROW 51101, 'Clinic schema is missing. Select the correct database.', 1;
DECLARE @Existing int = (SELECT COUNT(*) FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
    WHERE s.name='support' AND t.name IN ('support_conversations','support_messages','support_conversation_events'));
IF @Existing=3 BEGIN PRINT 'Support schema already exists; verify version/constraints before rollout.'; RETURN; END;
IF @Existing<>0 THROW 51102, 'Partial support schema exists. Inspect before proceeding.', 1;
BEGIN TRY
BEGIN TRANSACTION;
IF SCHEMA_ID('support') IS NULL EXEC('CREATE SCHEMA support');
CREATE TABLE support.support_conversations (
    [id] bigint IDENTITY(1,1) PRIMARY KEY,
    [patient_id] int NOT NULL,
    [patient_user_id] int NOT NULL,
    [subject] nvarchar(150) NOT NULL,
    [status] tinyint NOT NULL,
    [assigned_receptionist_user_id] int NULL,
    [last_message_sequence] bigint NOT NULL,
    [last_event_sequence] bigint NOT NULL,
    [patient_last_read_sequence] bigint NOT NULL,
    [assigned_last_read_sequence] bigint NOT NULL,
    [created_at] datetime2(7) NOT NULL,
    [updated_at] datetime2(7) NOT NULL,
    [last_message_at] datetime2(7) NULL,
    [assigned_at] datetime2(7) NULL,
    [closed_at] datetime2(7) NULL,
    [closed_by_user_id] int NULL,
    [close_reason] tinyint NULL,
    [row_version] rowversion NOT NULL,
    CHECK ([status] IN (0,1,2)),
    CHECK (([status]=0 AND [assigned_receptionist_user_id] IS NULL AND [assigned_at] IS NULL) OR ([status]=1 AND [assigned_receptionist_user_id] IS NOT NULL AND [assigned_at] IS NOT NULL) OR [status]=2),
    CHECK (([status]=2 AND [closed_at] IS NOT NULL AND [closed_by_user_id] IS NOT NULL AND [close_reason] IS NOT NULL AND [close_reason] IN (0,1,2,3)) OR ([status]<2 AND [closed_at] IS NULL AND [closed_by_user_id] IS NULL AND [close_reason] IS NULL)),
    CHECK ([last_message_sequence]>=0 AND [last_event_sequence]>=0 AND [patient_last_read_sequence] BETWEEN 0 AND [last_message_sequence] AND [assigned_last_read_sequence] BETWEEN 0 AND [last_message_sequence]),
    FOREIGN KEY ([patient_id]) REFERENCES scheduling.patients(id),
    FOREIGN KEY ([patient_user_id]) REFERENCES auth.users(id),
    FOREIGN KEY ([assigned_receptionist_user_id]) REFERENCES auth.users(id),
    FOREIGN KEY ([closed_by_user_id]) REFERENCES auth.users(id)
);
CREATE UNIQUE INDEX UX_support_one_open ON support.support_conversations(patient_id) WHERE status < 2;
CREATE INDEX IX_support_patient ON support.support_conversations(patient_user_id,created_at DESC,id DESC);
CREATE INDEX IX_support_queue ON support.support_conversations(status,created_at,id);
CREATE INDEX IX_support_assignee ON support.support_conversations(assigned_receptionist_user_id,status,id);

CREATE TABLE support.support_messages (
    [id] bigint IDENTITY(1,1) PRIMARY KEY,
    [conversation_id] bigint NOT NULL,
    [message_sequence] bigint NOT NULL,
    [sender_user_id] int NULL,
    [sender_side] tinyint NOT NULL,
    [sender_name_snapshot] nvarchar(150) NOT NULL,
    [message_kind] tinyint NOT NULL,
    [content] nvarchar(2000) NOT NULL,
    [client_message_id] uniqueidentifier NOT NULL,
    [sent_at] datetime2(7) NOT NULL,
    CHECK ([message_sequence]>0),
    CHECK (([sender_side] IN (0,1) AND [sender_user_id] IS NOT NULL AND [message_kind]=0) OR ([sender_side]=2 AND [sender_user_id] IS NULL AND [message_kind]=1)),
    CHECK (LEN(LTRIM(RTRIM([content])))>0 AND [client_message_id]<>'00000000-0000-0000-0000-000000000000'),
    FOREIGN KEY ([conversation_id]) REFERENCES support.support_conversations(id),
    FOREIGN KEY ([sender_user_id]) REFERENCES auth.users(id)
);
CREATE UNIQUE INDEX UX_support_message_sequence ON support.support_messages(conversation_id,message_sequence);
CREATE UNIQUE INDEX UX_support_message_retry ON support.support_messages(sender_user_id,client_message_id) WHERE sender_user_id IS NOT NULL;
CREATE UNIQUE INDEX UX_support_auto_ack ON support.support_messages(conversation_id) WHERE message_kind=1;
CREATE INDEX IX_support_unread ON support.support_messages(conversation_id,sender_side,message_sequence);

CREATE TABLE support.support_conversation_events (
    [id] bigint IDENTITY(1,1) PRIMARY KEY,
    [conversation_id] bigint NOT NULL,
    [event_sequence] bigint NOT NULL,
    [event_type] tinyint NOT NULL,
    [actor_user_id] int NOT NULL,
    [from_assignee_user_id] int NULL,
    [to_assignee_user_id] int NULL,
    [message_sequence_at_event] bigint NOT NULL,
    [reason] nvarchar(500) NULL,
    [occurred_at] datetime2(7) NOT NULL,
    CHECK ([event_sequence]>0 AND [message_sequence_at_event]>=0 AND [event_type] BETWEEN 0 AND 4),
    CHECK ([event_type] NOT IN (2,3) OR ([reason] IS NOT NULL AND LEN(LTRIM(RTRIM([reason])))>0)),
    FOREIGN KEY ([conversation_id]) REFERENCES support.support_conversations(id),
    FOREIGN KEY ([actor_user_id]) REFERENCES auth.users(id),
    FOREIGN KEY ([from_assignee_user_id]) REFERENCES auth.users(id),
    FOREIGN KEY ([to_assignee_user_id]) REFERENCES auth.users(id)
);
CREATE UNIQUE INDEX UX_support_event_sequence ON support.support_conversation_events(conversation_id,event_sequence);
COMMIT TRANSACTION;
PRINT 'Support schema installed. Enable module only after deploying API and Web.';
END TRY
BEGIN CATCH
IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
THROW;
END CATCH;
-- The app needs SELECT/INSERT/UPDATE conversations; SELECT/INSERT messages and events.
-- Grant to the existing app database principal explicitly; do not give it DDL permissions.
-- Example: GRANT SELECT, INSERT, UPDATE ON support.support_conversations TO [app_principal];
-- Example: GRANT SELECT, INSERT ON support.support_messages TO [app_principal];
-- Example: GRANT SELECT, INSERT ON support.support_conversation_events TO [app_principal];
