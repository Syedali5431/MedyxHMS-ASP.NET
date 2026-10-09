-- ============================================
-- MedyxHMS — MFA Columns Migration
-- Adds the MFA columns to AspNetUsers in databases created before MFA support (June 2026).
-- Safe to run more than once: columns that already exist are skipped.
-- New-Database.sql / New-Database-Empty.sql already contain these columns (and add them to older
-- databases too), so this script is only needed when you update an old database by hand.
--
-- Usage: sqlcmd -S <server> -E -b -d MedyxHMS -i MFA-Migration.sql
-- ============================================

IF DB_NAME() IN (N'master', N'model', N'msdb', N'tempdb')
BEGIN
    RAISERROR(N'Run this script in the MedyxHMS database (sqlcmd -d MedyxHMS ...), not in %s.', 16, 1, N'a system database');
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.AspNetUsers', N'MFAEnabled') IS NULL
    ALTER TABLE [dbo].[AspNetUsers] ADD [MFAEnabled] bit NOT NULL CONSTRAINT [DF_AspNetUsers_MFAEnabled] DEFAULT 0;
IF COL_LENGTH(N'dbo.AspNetUsers', N'MFASecretKey') IS NULL
    ALTER TABLE [dbo].[AspNetUsers] ADD [MFASecretKey] nvarchar(max) NULL;
IF COL_LENGTH(N'dbo.AspNetUsers', N'MFATempSecret') IS NULL
    ALTER TABLE [dbo].[AspNetUsers] ADD [MFATempSecret] nvarchar(max) NULL;
IF COL_LENGTH(N'dbo.AspNetUsers', N'MFARecoveryCodes') IS NULL
    ALTER TABLE [dbo].[AspNetUsers] ADD [MFARecoveryCodes] nvarchar(max) NULL;
GO

-- Verify columns
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME LIKE 'MFA%';
GO

-- Ends the stop set above when the script was run in a system database.
SET NOEXEC OFF;
GO
