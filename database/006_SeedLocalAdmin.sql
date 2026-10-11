-- Run manually in SSMS against your LOCAL ClinicManagementSystem database.
-- This is an optional development seed, not a deployment migration.
-- Replace PASTE_BCRYPT_HASH_HERE with a BCrypt hash (work factor 12).
-- Keep real passwords and password hashes out of source control.
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'ClinicManagementSystem'
    THROW 50001, N'Select the LOCAL ClinicManagementSystem database first.', 1;

IF CONVERT(INT, SERVERPROPERTY('EngineEdition')) IN (5, 8)
    THROW 50002, N'This development seed must not run on Azure SQL.', 1;

DECLARE @Username VARCHAR(100) = 'admin.local';
DECLARE @Email VARCHAR(255) = 'admin.local@example.com';
DECLARE @PasswordHash VARCHAR(255) = 'PASTE_BCRYPT_HASH_HERE';
DECLARE @UserId INT;
DECLARE @RoleId INT;

IF LEN(@PasswordHash) <> 60 OR LEFT(@PasswordHash, 7) <> '$2a$12$'
    THROW 50003, N'Provide a BCrypt hash generated with work factor 12.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    SELECT @RoleId = id
    FROM auth.roles WITH (UPDLOCK, HOLDLOCK)
    WHERE name = 'Admin';

    IF @RoleId IS NULL
        THROW 50004, N'The Admin role does not exist.', 1;

    SELECT @UserId = id
    FROM auth.users WITH (UPDLOCK, HOLDLOCK)
    WHERE username = @Username;

    IF EXISTS (
        SELECT 1 FROM auth.users WITH (UPDLOCK, HOLDLOCK)
        WHERE email = @Email AND (@UserId IS NULL OR id <> @UserId)
    )
        THROW 50005, N'The seed email belongs to another account.', 1;

    IF @UserId IS NULL
    BEGIN
        INSERT INTO auth.users (
            username, password_hash, email, full_name,
            is_active, is_deleted, email_confirmed, failed_login_count, created_at
        )
        VALUES (
            @Username, @PasswordHash, @Email, N'Admin Local',
            1, 0, 0, 0, SYSUTCDATETIME()
        );

        SET @UserId = CONVERT(INT, SCOPE_IDENTITY());
    END
    ELSE IF EXISTS (
        SELECT 1 FROM auth.users
        WHERE id = @UserId
          AND (email IS NULL OR email <> @Email OR is_active = 0 OR is_deleted = 1)
    )
        THROW 50006, N'The existing account does not match the active local seed account.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM auth.user_roles WITH (UPDLOCK, HOLDLOCK)
        WHERE user_id = @UserId AND role_id = @RoleId
    )
        INSERT INTO auth.user_roles (user_id, role_id, assigned_at)
        VALUES (@UserId, @RoleId, SYSUTCDATETIME());

    COMMIT TRANSACTION;

    SELECT u.id, u.username, u.email, r.name AS role_name
    FROM auth.users u
    JOIN auth.user_roles ur ON ur.user_id = u.id
    JOIN auth.roles r ON r.id = ur.role_id
    WHERE u.id = @UserId;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
