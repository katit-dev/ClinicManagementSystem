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


USE ClinicManagementSystem;
GO


-- ============================================================
-- SEED SPECIALTIES
-- ============================================================

IF NOT EXISTS
(
    SELECT 1
    FROM scheduling.specialties
    WHERE code = 'CARD'
)
BEGIN
    INSERT INTO scheduling.specialties
    (
        name,
        description,
        is_active,
        created_at,
        code
    )
    VALUES
    (
        N'Tim mạch',
        N'Khám và điều trị các bệnh lý về tim mạch.',
        1,
        GETDATE(),
        'CARD'
    );
END;
GO


IF NOT EXISTS
(
    SELECT 1
    FROM scheduling.specialties
    WHERE code = 'GEN'
)
BEGIN
    INSERT INTO scheduling.specialties
    (
        name,
        description,
        is_active,
        created_at,
        code
    )
    VALUES
    (
        N'Nội tổng quát',
        N'Khám và điều trị các bệnh lý nội khoa tổng quát.',
        1,
        GETDATE(),
        'GEN'
    );
END;
GO


IF NOT EXISTS
(
    SELECT 1
    FROM scheduling.specialties
    WHERE code = 'DERM'
)
BEGIN
    INSERT INTO scheduling.specialties
    (
        name,
        description,
        is_active,
        created_at,
        code
    )
    VALUES
    (
        N'Da liễu',
        N'Khám và điều trị các bệnh lý về da.',
        1,
        GETDATE(),
        'DERM'
    );
END;
GO


IF NOT EXISTS
(
    SELECT 1
    FROM scheduling.specialties
    WHERE code = 'PED'
)
BEGIN
    INSERT INTO scheduling.specialties
    (
        name,
        description,
        is_active,
        created_at,
        code
    )
    VALUES
    (
        N'Nhi khoa',
        N'Khám và điều trị bệnh cho trẻ em.',
        1,
        GETDATE(),
        'PED'
    );
END;
GO


-- ============================================================
-- VERIFY
-- ============================================================

SELECT
    id,
    code,
    name,
    description,
    is_active,
    created_at
FROM scheduling.specialties
ORDER BY id;
GO