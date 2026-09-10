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


-- ============================================================
-- VERIFY
-- ============================================================

SELECT
    id,
    name,
    description,
    is_system
FROM auth.roles
ORDER BY id;
GO