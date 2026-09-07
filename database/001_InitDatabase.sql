-- 001_InitDatabase.sql
-- Khởi tạo database và các schema

IF DB_ID(N'ClinicManagementSystem') IS NULL
BEGIN
    CREATE DATABASE ClinicManagementSystem;
END
GO

USE ClinicManagementSystem;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.schemas WHERE name = N'auth'
)
    EXEC(N'CREATE SCHEMA auth');
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.schemas WHERE name = N'scheduling'
)
    EXEC(N'CREATE SCHEMA scheduling');
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.schemas WHERE name = N'clinical'
)
    EXEC(N'CREATE SCHEMA clinical');
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.schemas WHERE name = N'billing'
)
    EXEC(N'CREATE SCHEMA billing');
GO