-- ============================================
-- MedyxHMS — MFA Columns Migration
-- Run this on existing databases to add MFA support
-- Date: 2026-06-24
-- ============================================

-- Add MFA columns to AspNetUsers
ALTER TABLE [dbo].[AspNetUsers] ADD [MFAEnabled] bit NOT NULL DEFAULT 0;
ALTER TABLE [dbo].[AspNetUsers] ADD [MFASecretKey] nvarchar(max) NULL;
ALTER TABLE [dbo].[AspNetUsers] ADD [MFATempSecret] nvarchar(max) NULL;
ALTER TABLE [dbo].[AspNetUsers] ADD [MFARecoveryCodes] nvarchar(max) NULL;

-- Verify columns
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME LIKE 'MFA%';
