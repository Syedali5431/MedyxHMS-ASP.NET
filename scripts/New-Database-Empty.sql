/*
    MedyxHMS – new database, schema only
    Generated 2026-10-06 from the running system (117 tables, 168 indexes, 132 foreign keys, 4 views/procedures).

    What it does
      - Creates the database [MedyxHMS] if it does not exist, then every table, column, index and foreign key
        the application uses.
      - Safe to run again and on an existing (older) MedyxHMS database: only missing tables, columns, indexes
        and keys are added; nothing is dropped or changed. Anything that cannot be added because of the
        existing data is reported with a "Note:" line instead of stopping the script.
      - No data is inserted. The application adds its baseline data (roles, modules, settings, default
        hospital, SuperAdmin account) when it starts for the first time.

    Run (in this order)
      sqlcmd -S <server> -E -b -i New-Database-Empty.sql
      sqlcmd -S <server> -E -b -d MedyxHMS -i StoredProcedures_Reports.sql   (also run by the application at start-up)
      sqlcmd -S <server> -E -b -d MedyxHMS -i CreateIndexes.sql              (reporting indexes, optional)
      sqlcmd -S <server> -E -b -d MedyxHMS -i SeedDemoData.sql               (demo data, optional)
    To use another database name, replace [MedyxHMS] / N'MedyxHMS' in the first two statements.
*/

SET NOCOUNT ON;
GO
IF DB_ID(N'MedyxHMS') IS NULL
BEGIN
    CREATE DATABASE [MedyxHMS];
    PRINT N'Created database MedyxHMS';
END
GO

USE [MedyxHMS];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

/* ============================================================
   Helper procedures (temporary – they exist only while this script runs)
   ============================================================ */
IF OBJECT_ID(N'tempdb..#ExecWithKeyTypes') IS NOT NULL DROP PROCEDURE #ExecWithKeyTypes;
IF OBJECT_ID(N'tempdb..#EnsureColumn') IS NOT NULL DROP PROCEDURE #EnsureColumn;
IF OBJECT_ID(N'tempdb..#EnsureIndex') IS NOT NULL DROP PROCEDURE #EnsureIndex;
IF OBJECT_ID(N'tempdb..#EnsureForeignKey') IS NOT NULL DROP PROCEDURE #EnsureForeignKey;
GO
-- Runs a statement in which {{UserId}}, {{StaffId}} and {{RoleId}} stand for the type of the key they refer to.
-- (nvarchar(128) in databases created by these scripts; nvarchar(450) in databases created by the application itself.)
CREATE PROCEDURE #ExecWithKeyTypes @Sql nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @UserId nvarchar(30) = ISNULL((SELECT CASE WHEN max_length = -1 THEN N'nvarchar(max)' ELSE N'nvarchar(' + CAST(max_length / 2 AS nvarchar(10)) + N')' END
        FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = N'Id'), N'nvarchar(128)');
    DECLARE @StaffId nvarchar(30) = ISNULL((SELECT CASE WHEN max_length = -1 THEN N'nvarchar(max)' ELSE N'nvarchar(' + CAST(max_length / 2 AS nvarchar(10)) + N')' END
        FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Staff]') AND name = N'Id'), @UserId);
    DECLARE @RoleId nvarchar(30) = ISNULL((SELECT CASE WHEN max_length = -1 THEN N'nvarchar(max)' ELSE N'nvarchar(' + CAST(max_length / 2 AS nvarchar(10)) + N')' END
        FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetRoles]') AND name = N'Id'), N'nvarchar(128)');
    SET @Sql = REPLACE(REPLACE(REPLACE(@Sql, N'{{UserId}}', @UserId), N'{{StaffId}}', @StaffId), N'{{RoleId}}', @RoleId);
    EXEC sys.sp_executesql @Sql;
END
GO
-- Adds a column to an existing table when it is missing (NOT NULL columns come with a default for existing rows).
CREATE PROCEDURE #EnsureColumn @Table sysname, @Column sysname, @Definition nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    IF OBJECT_ID(N'[dbo].' + QUOTENAME(@Table), N'U') IS NULL OR COL_LENGTH(N'dbo.' + @Table, @Column) IS NOT NULL RETURN;
    EXEC #ExecWithKeyTypes @Sql = @Definition;
    PRINT N'Added column ' + @Table + N'.' + @Column;
END
GO
-- Creates an index (or unique constraint) unless it, or an index on the same columns, already exists.
CREATE PROCEDURE #EnsureIndex @Table sysname, @Name sysname, @Keys nvarchar(1000), @Ddl nvarchar(max), @IsUnique bit, @Filter nvarchar(max) = N''
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @obj int = OBJECT_ID(N'[dbo].' + QUOTENAME(@Table), N'U');
    IF @obj IS NULL OR EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = @obj AND name = @Name) RETURN;
    IF EXISTS (SELECT 1 FROM sys.indexes x WHERE x.object_id = @obj AND x.is_unique = @IsUnique AND ISNULL(x.filter_definition, N'') = @Filter
               AND (SELECT STRING_AGG(COL_NAME(ic.object_id, ic.column_id), N',') WITHIN GROUP (ORDER BY ic.key_ordinal)
                    FROM sys.index_columns ic WHERE ic.object_id = x.object_id AND ic.index_id = x.index_id AND ic.is_included_column = 0) = @Keys) RETURN;
    IF EXISTS (SELECT 1 FROM STRING_SPLIT(@Keys, N',') k WHERE COL_LENGTH(N'dbo.' + @Table, k.value) IS NULL OR COL_LENGTH(N'dbo.' + @Table, k.value) = -1)
    BEGIN
        PRINT N'Note: index ' + @Name + N' on ' + @Table + N' was not created: a key column is missing or nvarchar(max) in this database.';
        RETURN;
    END
    BEGIN TRY
        EXEC sys.sp_executesql @Ddl;
    END TRY
    BEGIN CATCH
        PRINT N'Note: index ' + @Name + N' on ' + @Table + N' was not created: ' + ERROR_MESSAGE();
    END CATCH
END
GO
-- Adds a foreign key unless it, or a key on the same column to the same table, already exists.
-- Rows already in the table are not re-checked (older data may predate the relationship).
CREATE PROCEDURE #EnsureForeignKey @Table sysname, @Name sysname, @Column sysname, @RefTable sysname, @RefColumn sysname, @Definition nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @obj int = OBJECT_ID(N'[dbo].' + QUOTENAME(@Table), N'U'), @ref int = OBJECT_ID(N'[dbo].' + QUOTENAME(@RefTable), N'U');
    IF @obj IS NULL OR @ref IS NULL OR EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = @obj AND name = @Name) RETURN;
    IF @Column IS NOT NULL AND EXISTS (SELECT 1 FROM sys.foreign_key_columns fkc WHERE fkc.parent_object_id = @obj AND fkc.referenced_object_id = @ref
                                       AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = @Column) RETURN;
    -- A key needs the same column type on both sides (older databases can differ).
    DECLARE @mine nvarchar(100), @theirs nvarchar(100);
    IF @Column IS NOT NULL AND @RefColumn IS NOT NULL
    BEGIN
        SELECT @mine = TYPE_NAME(c.user_type_id) + N'(' + CASE WHEN c.max_length = -1 THEN N'max' ELSE CAST(c.max_length / CASE WHEN TYPE_NAME(c.user_type_id) IN (N'nvarchar', N'nchar') THEN 2 ELSE 1 END AS nvarchar(10)) END + N')'
          FROM sys.columns c WHERE c.object_id = @obj AND c.name = @Column;
        SELECT @theirs = TYPE_NAME(c.user_type_id) + N'(' + CASE WHEN c.max_length = -1 THEN N'max' ELSE CAST(c.max_length / CASE WHEN TYPE_NAME(c.user_type_id) IN (N'nvarchar', N'nchar') THEN 2 ELSE 1 END AS nvarchar(10)) END + N')'
          FROM sys.columns c WHERE c.object_id = @ref AND c.name = @RefColumn;
        IF @mine <> @theirs
        BEGIN
            PRINT N'Note: foreign key ' + @Name + N' on ' + @Table + N' was not created: ' + @Table + N'.' + @Column + N' is ' + @mine
                + N' but ' + @RefTable + N'.' + @RefColumn + N' is ' + @theirs + N' in this database.';
            RETURN;
        END
    END
    DECLARE @sql nvarchar(max) = N'ALTER TABLE [dbo].' + QUOTENAME(@Table)
        + CASE WHEN EXISTS (SELECT 1 FROM sys.partitions WHERE object_id = @obj AND index_id IN (0, 1) AND rows > 0) THEN N' WITH NOCHECK' ELSE N' WITH CHECK' END
        + N' ADD CONSTRAINT ' + QUOTENAME(@Name) + N' ' + @Definition;
    BEGIN TRY
        EXEC sys.sp_executesql @sql;
    END TRY
    BEGIN CATCH
        PRINT N'Note: foreign key ' + @Name + N' on ' + @Table + N' was not created: ' + ERROR_MESSAGE();
    END CATCH
END
GO

/* ============================================================
   1. Tables
   ============================================================ */

IF OBJECT_ID(N'[dbo].[AccountApprovalRequests]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[AccountApprovalRequests] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [RequestedUserId] {{UserId}} NOT NULL,
        [RequestedRole] nvarchar(100) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [RequestedAtUtc] datetime2 NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [ApprovedByUserId] nvarchar(128) NULL,
        [ApprovedAtUtc] datetime2 NULL,
        CONSTRAINT [PK__AccountA__3214EC076795CC69] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table AccountApprovalRequests';
END;
GO

IF OBJECT_ID(N'[dbo].[AmbulanceDispatches]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AmbulanceDispatches] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [AmbulanceVehicleId] int NOT NULL,
        [PatientId] int NULL,
        [PatientName] nvarchar(max) NOT NULL CONSTRAINT [DF__Ambulance__Patie__5C6CB6D7] DEFAULT (''),
        [PickupAddress] nvarchar(max) NOT NULL CONSTRAINT [DF__Ambulance__Picku__5D60DB10] DEFAULT (''),
        [ContactNumber] nvarchar(max) NOT NULL CONSTRAINT [DF__Ambulance__Conta__5E54FF49] DEFAULT (''),
        [Purpose] nvarchar(max) NOT NULL CONSTRAINT [DF__Ambulance__Purpo__5F492382] DEFAULT (''),
        [DispatchTime] datetime2 NOT NULL,
        [ReturnTime] datetime2 NULL,
        [Status] nvarchar(max) NOT NULL CONSTRAINT [DF__Ambulance__Statu__603D47BB] DEFAULT ('Dispatched'),
        [DistanceKm] decimal(18, 2) NULL,
        [Charges] decimal(18, 2) NULL,
        [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF__Ambulance__Notes__61316BF4] DEFAULT (''),
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK__Ambulanc__3214EC07B16E6E46] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table AmbulanceDispatches';
END;
GO

IF OBJECT_ID(N'[dbo].[AmbulanceVehicles]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AmbulanceVehicles] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [VehicleNumber] nvarchar(450) NOT NULL,
        [DriverName] nvarchar(max) NOT NULL CONSTRAINT [DF__Ambulance__Drive__55BFB948] DEFAULT (''),
        [DriverContact] nvarchar(max) NOT NULL CONSTRAINT [DF__Ambulance__Drive__56B3DD81] DEFAULT (''),
        [Model] nvarchar(max) NOT NULL CONSTRAINT [DF__Ambulance__Model__57A801BA] DEFAULT (''),
        [Status] nvarchar(max) NOT NULL CONSTRAINT [DF__Ambulance__Statu__589C25F3] DEFAULT ('Available'),
        [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF__Ambulance__Notes__59904A2C] DEFAULT (''),
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK__Ambulanc__3214EC07CB71C278] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table AmbulanceVehicles';
END;
GO

IF OBJECT_ID(N'[dbo].[Appointments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Appointments] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [PatientId] int NOT NULL,
        [DoctorId] int NOT NULL,
        [StaffId] nvarchar(128) NULL,
        [AppointmentDate] datetime2 NOT NULL,
        [AppointmentTime] time NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [AppointmentType] nvarchar(max) NOT NULL,
        [Priority] nvarchar(max) NOT NULL,
        [Symptoms] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NOT NULL,
        [HospitalId] int NULL,
        CONSTRAINT [PK_Appointments] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table Appointments';
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetRoleClaims]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[AspNetRoleClaims] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [RoleId] {{RoleId}} NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table AspNetRoleClaims';
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetRoles]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetRoles] (
        [Id] nvarchar(128) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table AspNetRoles';
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetUserClaims]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[AspNetUserClaims] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [UserId] {{UserId}} NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table AspNetUserClaims';
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetUserLogins]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[AspNetUserLogins] (
        [LoginProvider] nvarchar(128) NOT NULL,
        [ProviderKey] nvarchar(128) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] {{UserId}} NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY CLUSTERED ([LoginProvider], [ProviderKey])
    );';
    PRINT N'Created table AspNetUserLogins';
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetUserRoles]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[AspNetUserRoles] (
        [UserId] {{UserId}} NOT NULL,
        [RoleId] {{RoleId}} NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY CLUSTERED ([UserId], [RoleId])
    );';
    PRINT N'Created table AspNetUserRoles';
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetUsers]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AspNetUsers] (
        [Id] nvarchar(128) NOT NULL,
        [EmployeeId] nvarchar(max) NOT NULL,
        [FirstName] nvarchar(max) NOT NULL,
        [LastName] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [FirstLoginDate] datetime2 NULL,
        [LastLoginDate] datetime2 NULL,
        [ProfileImage] nvarchar(max) NULL,
        [MFAEnabled] bit NOT NULL CONSTRAINT [DF__AspNetUse__MFAEn__398D8EEE] DEFAULT ((0)),
        [MFASecretKey] nvarchar(max) NULL,
        [MFATempSecret] nvarchar(max) NULL,
        [MFARecoveryCodes] nvarchar(max) NULL,
        [UserName] nvarchar(256) NOT NULL,
        [NormalizedUserName] nvarchar(256) NOT NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        [Gender] nvarchar(20) NULL,
        [DateOfBirth] datetime2 NULL,
        [Address] nvarchar(300) NULL,
        [City] nvarchar(100) NULL,
        [EmergencyContactName] nvarchar(150) NULL,
        [EmergencyContactPhone] nvarchar(30) NULL,
        [About] nvarchar(1000) NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table AspNetUsers';
END;
GO

IF OBJECT_ID(N'[dbo].[AspNetUserTokens]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[AspNetUserTokens] (
        [UserId] {{UserId}} NOT NULL,
        [LoginProvider] nvarchar(128) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY CLUSTERED ([UserId], [LoginProvider], [Name])
    );';
    PRINT N'Created table AspNetUserTokens';
END;
GO

IF OBJECT_ID(N'[dbo].[AuditFindings]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AuditFindings] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [AuditId] int NOT NULL,
        [FindingType] nvarchar(40) NOT NULL,
        [Clause] nvarchar(50) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditFindings] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table AuditFindings';
END;
GO

IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[AuditLogs] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [UserId] {{UserId}} NULL,
        [Action] nvarchar(max) NOT NULL,
        [EntityName] nvarchar(max) NOT NULL,
        [EntityId] nvarchar(max) NOT NULL,
        [OldValues] nvarchar(max) NOT NULL,
        [NewValues] nvarchar(max) NOT NULL,
        [Details] nvarchar(max) NOT NULL,
        [IpAddress] nvarchar(max) NOT NULL,
        [UserAgent] nvarchar(max) NOT NULL,
        [Timestamp] datetime2 NOT NULL,
        [SessionId] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table AuditLogs';
END;
GO

IF OBJECT_ID(N'[dbo].[Beds]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Beds] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [WardId] int NOT NULL,
        [BedNumber] nvarchar(max) NOT NULL,
        [BedType] nvarchar(max) NOT NULL,
        [DailyCharges] decimal(18, 2) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [PatientId] int NULL,
        [IsIsolation] bit NOT NULL CONSTRAINT [DF_Beds_IsIsolation] DEFAULT ((0)),
        [RequiresAdminApproval] bit NOT NULL CONSTRAINT [DF_Beds_RequiresAdminApproval] DEFAULT ((0)),
        [LastUpdated] datetime2 NULL,
        [Block] nvarchar(100) NOT NULL CONSTRAINT [DF_Beds_Block] DEFAULT (''),
        [Floor] nvarchar(50) NOT NULL CONSTRAINT [DF_Beds_Floor] DEFAULT (''),
        [RoomNumber] nvarchar(50) NOT NULL CONSTRAINT [DF_Beds_RoomNumber] DEFAULT (''),
        CONSTRAINT [PK_Beds] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table Beds';
END;
GO

IF OBJECT_ID(N'[dbo].[BillItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BillItems] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [BillId] int NOT NULL,
        [ItemName] nvarchar(max) NOT NULL,
        [ItemType] nvarchar(max) NOT NULL,
        [Quantity] decimal(18, 2) NOT NULL,
        [UnitPrice] decimal(18, 2) NOT NULL,
        [TotalPrice] decimal(18, 2) NOT NULL,
        [Amount] decimal(18, 2) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_BillItems] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table BillItems';
END;
GO

IF OBJECT_ID(N'[dbo].[Bills]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Bills] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [BillNumber] nvarchar(max) NOT NULL,
        [PatientId] int NOT NULL,
        [AppointmentId] int NULL,
        [BillDate] datetime2 NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [TotalAmount] decimal(18, 2) NOT NULL,
        [PaidAmount] decimal(18, 2) NOT NULL,
        [PendingAmount] decimal(18, 2) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [BillType] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [HospitalId] int NULL,
        CONSTRAINT [PK_Bills] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table Bills';
END;
GO

IF OBJECT_ID(N'[dbo].[BirthRecords]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BirthRecords] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [BabyName] nvarchar(max) NOT NULL CONSTRAINT [DF__BirthReco__BabyN__65F62111] DEFAULT (''),
        [Gender] nvarchar(max) NOT NULL CONSTRAINT [DF__BirthReco__Gende__66EA454A] DEFAULT (''),
        [DateOfBirth] datetime2 NOT NULL,
        [TimeOfBirth] nvarchar(max) NOT NULL CONSTRAINT [DF__BirthReco__TimeO__67DE6983] DEFAULT (''),
        [WeightKg] decimal(18, 2) NOT NULL CONSTRAINT [DF__BirthReco__Weigh__68D28DBC] DEFAULT ((0)),
        [MotherName] nvarchar(max) NOT NULL CONSTRAINT [DF__BirthReco__Mothe__69C6B1F5] DEFAULT (''),
        [FatherName] nvarchar(max) NOT NULL CONSTRAINT [DF__BirthReco__Fathe__6ABAD62E] DEFAULT (''),
        [GuardianContact] nvarchar(max) NOT NULL CONSTRAINT [DF__BirthReco__Guard__6BAEFA67] DEFAULT (''),
        [DeliveryType] nvarchar(max) NOT NULL CONSTRAINT [DF__BirthReco__Deliv__6CA31EA0] DEFAULT ('Normal'),
        [AttendingDoctorName] nvarchar(max) NOT NULL CONSTRAINT [DF__BirthReco__Atten__6D9742D9] DEFAULT (''),
        [PatientId] int NULL,
        [CertificateNumber] nvarchar(max) NOT NULL CONSTRAINT [DF__BirthReco__Certi__6E8B6712] DEFAULT (''),
        [CertificateIssued] bit NOT NULL CONSTRAINT [DF__BirthReco__Certi__6F7F8B4B] DEFAULT ((0)),
        [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF__BirthReco__Notes__7073AF84] DEFAULT (''),
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK__BirthRec__3214EC0783E5B718] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table BirthRecords';
END;
GO

IF OBJECT_ID(N'[dbo].[BloodInventories]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BloodInventories] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [BloodGroup] nvarchar(max) NOT NULL,
        [UnitsAvailable] int NOT NULL,
        [UnitsReserved] int NOT NULL,
        [MinimumLevel] int NOT NULL,
        [LastUpdatedDate] datetime2 NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_BloodInventories] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table BloodInventories';
END;
GO

IF OBJECT_ID(N'[dbo].[BloodIssues]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[BloodIssues] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [PatientId] int NOT NULL,
        [BloodGroup] nvarchar(max) NOT NULL,
        [UnitsIssued] int NOT NULL,
        [IssueDate] datetime2 NOT NULL,
        [RequestedBy] nvarchar(max) NOT NULL,
        [CrossMatchStatus] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [BillId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_BloodIssues] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table BloodIssues';
END;
GO

IF OBJECT_ID(N'[dbo].[CapaActions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CapaActions] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [HospitalId] int NULL,
        [IncidentId] int NULL,
        [AuditFindingId] int NULL,
        [ActionType] nvarchar(20) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [OwnerUserId] nvarchar(max) NOT NULL,
        [OwnerName] nvarchar(max) NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [CompletionNotes] nvarchar(max) NOT NULL,
        [CompletedAt] datetime2 NULL,
        [VerifiedBy] nvarchar(max) NOT NULL,
        [VerifiedAt] datetime2 NULL,
        [EffectivenessNotes] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_CapaActions] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table CapaActions';
END;
GO

IF OBJECT_ID(N'[dbo].[CertificateRecords]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[CertificateRecords] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [StaffId] {{StaffId}} NOT NULL,
        [CertificateType] nvarchar(max) NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [IssueDate] datetime2 NOT NULL,
        [GeneratedBy] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_CertificateRecords] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table CertificateRecords';
END;
GO

IF OBJECT_ID(N'[dbo].[ChatbotConsentAudits]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatbotConsentAudits] (
        [Id] bigint IDENTITY(1, 1) NOT NULL,
        [ConsentId] bigint NULL,
        [UserId] nvarchar(450) NULL,
        [Action] nvarchar(30) NOT NULL,
        [ConsentVersion] nvarchar(10) NOT NULL,
        [ConsentStateJson] nvarchar(max) NOT NULL,
        [UserIpAddress] nvarchar(500) NULL,
        [UserAgent] nvarchar(500) NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ChatbotConsentAudits] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table ChatbotConsentAudits';
END;
GO

IF OBJECT_ID(N'[dbo].[ChatbotConsents]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatbotConsents] (
        [Id] bigint IDENTITY(1, 1) NOT NULL,
        [UserId] nvarchar(450) NULL,
        [ConsentVersion] nvarchar(10) NOT NULL,
        [ConsentedToAiProcessing] bit NOT NULL,
        [ConsentedToDataRetention] bit NOT NULL,
        [ConsentedToThirdPartyProcessing] bit NOT NULL,
        [UserIpAddress] nvarchar(50) NULL,
        [UserAgent] nvarchar(500) NULL,
        [ConsentedAtUtc] datetime2 NOT NULL,
        [RevokedAtUtc] datetime2 NULL,
        [IsActive] bit NOT NULL,
        [RevocationReason] nvarchar(500) NULL,
        CONSTRAINT [PK_ChatbotConsents] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table ChatbotConsents';
END;
GO

IF OBJECT_ID(N'[dbo].[ChatbotEventLogs]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatbotEventLogs] (
        [Id] bigint IDENTITY(1, 1) NOT NULL,
        [SessionId] nvarchar(64) NULL,
        [MessageId] bigint NULL,
        [EventType] nvarchar(50) NOT NULL,
        [Severity] nvarchar(20) NOT NULL,
        [Details] nvarchar(2000) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ChatbotEventLogs] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table ChatbotEventLogs';
END;
GO

IF OBJECT_ID(N'[dbo].[ChatEscalations]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatEscalations] (
        [Id] bigint IDENTITY(1, 1) NOT NULL,
        [SessionId] nvarchar(64) NOT NULL,
        [MessageId] bigint NULL,
        [UserId] nvarchar(128) NULL,
        [EscalationType] nvarchar(30) NOT NULL,
        [Reason] nvarchar(1200) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [TargetContact] nvarchar(200) NULL,
        [ResolvedByUserId] nvarchar(128) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [ResolvedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_ChatEscalations] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table ChatEscalations';
END;
GO

IF OBJECT_ID(N'[dbo].[ChatFeedback]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatFeedback] (
        [Id] bigint IDENTITY(1, 1) NOT NULL,
        [SessionId] nvarchar(64) NOT NULL,
        [MessageId] bigint NULL,
        [FeedbackType] nvarchar(20) NOT NULL,
        [Comment] nvarchar(1000) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ChatFeedback] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table ChatFeedback';
END;
GO

IF OBJECT_ID(N'[dbo].[ChatMessages]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatMessages] (
        [Id] bigint IDENTITY(1, 1) NOT NULL,
        [SessionId] nvarchar(64) NOT NULL,
        [SenderType] nvarchar(20) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [ModerationStatus] nvarchar(30) NOT NULL,
        [TokenCount] int NOT NULL,
        [Category] nvarchar(30) NOT NULL,
        CONSTRAINT [PK_ChatMessages] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table ChatMessages';
END;
GO

IF OBJECT_ID(N'[dbo].[ChatSessions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChatSessions] (
        [Id] nvarchar(64) NOT NULL,
        [UserId] nvarchar(128) NULL,
        [UserRole] nvarchar(40) NOT NULL,
        [StartedAtUtc] datetime2 NOT NULL,
        [EndedAtUtc] datetime2 NULL,
        [Status] nvarchar(30) NOT NULL,
        [Channel] nvarchar(20) NOT NULL,
        [IsEscalated] bit NOT NULL,
        [IsUnresolved] bit NOT NULL,
        [PreferredLanguage] nvarchar(12) NOT NULL,
        CONSTRAINT [PK_ChatSessions] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table ChatSessions';
END;
GO

IF OBJECT_ID(N'[dbo].[CmsMenuItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CmsMenuItems] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [Label] nvarchar(100) NOT NULL,
        [Url] nvarchar(300) NULL,
        [CmsPageId] int NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [OpenInNewTab] bit NOT NULL,
        CONSTRAINT [PK_CmsMenuItems] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table CmsMenuItems';
END;
GO

IF OBJECT_ID(N'[dbo].[CmsNotices]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CmsNotices] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Slug] nvarchar(200) NOT NULL,
        [Summary] nvarchar(max) NULL,
        [Content] nvarchar(max) NULL,
        [Type] nvarchar(20) NOT NULL,
        [FeaturedImage] nvarchar(300) NULL,
        [IsActive] bit NOT NULL,
        [PublishedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [CreatedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_CmsNotices] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table CmsNotices';
END;
GO

IF OBJECT_ID(N'[dbo].[CmsPages]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CmsPages] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Slug] nvarchar(200) NOT NULL,
        [Content] nvarchar(max) NULL,
        [MetaDescription] nvarchar(300) NULL,
        [Status] nvarchar(20) NOT NULL,
        [ShowInMenu] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [CreatedBy] nvarchar(100) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [FeaturedImage] nvarchar(300) NULL,
        [FontFamily] nvarchar(100) NULL,
        [FontSizePx] int NULL,
        CONSTRAINT [PK_CmsPages] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table CmsPages';
END;
GO

IF OBJECT_ID(N'[dbo].[ComplaintRecords]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ComplaintRecords] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [ComplainantName] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NOT NULL,
        [Subject] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [ResolutionNotes] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ResolvedDate] datetime2 NULL,
        CONSTRAINT [PK_ComplaintRecords] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table ComplaintRecords';
END;
GO

IF OBJECT_ID(N'[dbo].[ControlledDocuments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ControlledDocuments] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [DocumentNumber] nvarchar(30) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Category] nvarchar(50) NOT NULL,
        [Department] nvarchar(100) NOT NULL,
        [ReviewIntervalMonths] int NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [NextReviewDate] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ControlledDocuments] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table ControlledDocuments';
END;
GO

IF OBJECT_ID(N'[dbo].[DatabaseBackups]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DatabaseBackups] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [DatabaseName] nvarchar(max) NOT NULL,
        [FileName] nvarchar(max) NOT NULL,
        [FilePath] nvarchar(max) NOT NULL,
        [SizeBytes] bigint NULL,
        [StartedAt] datetime2 NOT NULL,
        [CompletedAt] datetime2 NULL,
        [Status] nvarchar(max) NOT NULL,
        [Verified] bit NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_DatabaseBackups] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table DatabaseBackups';
END;
GO

IF OBJECT_ID(N'[dbo].[DeathRecords]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DeathRecords] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [PatientId] int NULL,
        [PatientName] nvarchar(max) NOT NULL CONSTRAINT [DF__DeathReco__Patie__74444068] DEFAULT (''),
        [Gender] nvarchar(max) NOT NULL CONSTRAINT [DF__DeathReco__Gende__753864A1] DEFAULT (''),
        [DateOfDeath] datetime2 NOT NULL,
        [TimeOfDeath] nvarchar(max) NOT NULL CONSTRAINT [DF__DeathReco__TimeO__762C88DA] DEFAULT (''),
        [CauseOfDeath] nvarchar(max) NOT NULL CONSTRAINT [DF__DeathReco__Cause__7720AD13] DEFAULT (''),
        [AttendingDoctorName] nvarchar(max) NOT NULL CONSTRAINT [DF__DeathReco__Atten__7814D14C] DEFAULT (''),
        [NextOfKinName] nvarchar(max) NOT NULL CONSTRAINT [DF__DeathReco__NextO__7908F585] DEFAULT (''),
        [NextOfKinContact] nvarchar(max) NOT NULL CONSTRAINT [DF__DeathReco__NextO__79FD19BE] DEFAULT (''),
        [CertificateNumber] nvarchar(max) NOT NULL CONSTRAINT [DF__DeathReco__Certi__7AF13DF7] DEFAULT (''),
        [CertificateIssued] bit NOT NULL CONSTRAINT [DF__DeathReco__Certi__7BE56230] DEFAULT ((0)),
        [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF__DeathReco__Notes__7CD98669] DEFAULT (''),
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK__DeathRec__3214EC0775D83FE0] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table DeathRecords';
END;
GO

IF OBJECT_ID(N'[dbo].[Departments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Departments] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [HeadOfDepartment] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Departments] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table Departments';
END;
GO

IF OBJECT_ID(N'[dbo].[DischargeSummaries]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DischargeSummaries] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [HospitalId] int NULL,
        [IPDAdmissionId] int NOT NULL,
        [PatientId] int NOT NULL,
        [AdmissionDate] datetime2 NOT NULL,
        [DischargeDate] datetime2 NOT NULL,
        [ConditionAtDischarge] nvarchar(60) NOT NULL,
        [ReasonForAdmission] nvarchar(1000) NOT NULL,
        [FinalDiagnosis] nvarchar(1000) NOT NULL,
        [HospitalCourse] nvarchar(4000) NOT NULL,
        [ProceduresPerformed] nvarchar(2000) NOT NULL,
        [InvestigationsSummary] nvarchar(2000) NOT NULL,
        [DischargeMedications] nvarchar(2000) NOT NULL,
        [AdviceOnDischarge] nvarchar(2000) NOT NULL,
        [FollowUpInstructions] nvarchar(1000) NOT NULL,
        [FollowUpDate] datetime2 NULL,
        [AttendingDoctor] nvarchar(150) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [CreatedByUserId] nvarchar(450) NULL,
        [CreatedByName] nvarchar(150) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [CompletedByName] nvarchar(150) NULL,
        [CompletedAt] datetime2 NULL,
        CONSTRAINT [PK_DischargeSummaries] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table DischargeSummaries';
END;
GO

IF OBJECT_ID(N'[dbo].[DispatchReceiveRecords]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DispatchReceiveRecords] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [RecordType] nvarchar(max) NOT NULL,
        [ReferenceNumber] nvarchar(max) NOT NULL,
        [PartyName] nvarchar(max) NOT NULL,
        [ContactNumber] nvarchar(max) NOT NULL,
        [ContentSummary] nvarchar(max) NOT NULL,
        [RecordDate] datetime2 NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_DispatchReceiveRecords] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table DispatchReceiveRecords';
END;
GO

IF OBJECT_ID(N'[dbo].[Doctors]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Doctors] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [EmployeeId] nvarchar(max) NOT NULL,
        [FirstName] nvarchar(max) NOT NULL,
        [LastName] nvarchar(max) NOT NULL,
        [Specialization] nvarchar(max) NOT NULL,
        [LicenseNumber] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [DepartmentId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Doctors] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table Doctors';
END;
GO

IF OBJECT_ID(N'[dbo].[DoctorShifts]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DoctorShifts] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [DoctorId] int NOT NULL,
        [DayOfWeek] int NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [SlotDurationMinutes] int NOT NULL,
        [MaxPatientsPerSlot] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_DoctorShifts] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table DoctorShifts';
END;
GO

IF OBJECT_ID(N'[dbo].[DocumentVersions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DocumentVersions] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [DocumentId] int NOT NULL,
        [VersionNumber] int NOT NULL,
        [ChangeSummary] nvarchar(max) NOT NULL,
        [StoredFileName] nvarchar(max) NOT NULL,
        [OriginalFileName] nvarchar(max) NOT NULL,
        [FileSize] bigint NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [CreatedByUserId] nvarchar(max) NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [SubmittedAt] datetime2 NULL,
        [ApprovedByUserId] nvarchar(max) NOT NULL,
        [ApprovedBy] nvarchar(max) NOT NULL,
        [ApprovedAt] datetime2 NULL,
        [ApprovalComments] nvarchar(max) NOT NULL,
        [EffectiveDate] datetime2 NULL,
        CONSTRAINT [PK_DocumentVersions] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table DocumentVersions';
END;
GO

IF OBJECT_ID(N'[dbo].[DownloadFiles]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DownloadFiles] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [Title] nvarchar(max) NOT NULL CONSTRAINT [DF__DownloadF__Title__3335971A] DEFAULT (''),
        [Description] nvarchar(max) NOT NULL CONSTRAINT [DF__DownloadF__Descr__3429BB53] DEFAULT (''),
        [Category] nvarchar(max) NOT NULL CONSTRAINT [DF__DownloadF__Categ__351DDF8C] DEFAULT (''),
        [FileName] nvarchar(max) NOT NULL CONSTRAINT [DF__DownloadF__FileN__361203C5] DEFAULT (''),
        [FilePath] nvarchar(max) NOT NULL CONSTRAINT [DF__DownloadF__FileP__370627FE] DEFAULT (''),
        [FileType] nvarchar(max) NOT NULL CONSTRAINT [DF__DownloadF__FileT__37FA4C37] DEFAULT (''),
        [FileSizeBytes] bigint NOT NULL CONSTRAINT [DF__DownloadF__FileS__38EE7070] DEFAULT ((0)),
        [DownloadCount] int NOT NULL CONSTRAINT [DF__DownloadF__Downl__39E294A9] DEFAULT ((0)),
        [UploadedByUserId] nvarchar(128) NOT NULL CONSTRAINT [DF__DownloadF__Uploa__3AD6B8E2] DEFAULT (''),
        [IsPublic] bit NOT NULL CONSTRAINT [DF__DownloadF__IsPub__3BCADD1B] DEFAULT ((0)),
        [IsActive] bit NOT NULL CONSTRAINT [DF__DownloadF__IsAct__3CBF0154] DEFAULT ((1)),
        [UploadedAt] datetime2 NOT NULL,
        CONSTRAINT [PK__Download__3214EC07911AE841] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table DownloadFiles';
END;
GO

IF OBJECT_ID(N'[dbo].[Equipment]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Equipment] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [HospitalId] int NULL,
        [AssetTag] nvarchar(30) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Category] nvarchar(50) NOT NULL,
        [Manufacturer] nvarchar(100) NOT NULL,
        [Model] nvarchar(100) NOT NULL,
        [SerialNumber] nvarchar(100) NOT NULL,
        [Location] nvarchar(150) NOT NULL,
        [Supplier] nvarchar(150) NOT NULL,
        [PurchaseDate] datetime2 NULL,
        [WarrantyExpiry] datetime2 NULL,
        [RiskClass] nvarchar(10) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [MaintenanceIntervalDays] int NOT NULL,
        [CalibrationIntervalDays] int NOT NULL,
        [LastMaintenanceDate] datetime2 NULL,
        [NextMaintenanceDue] datetime2 NULL,
        [LastCalibrationDate] datetime2 NULL,
        [NextCalibrationDue] datetime2 NULL,
        [Notes] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Equipment] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table Equipment';
END;
GO

IF OBJECT_ID(N'[dbo].[EquipmentServiceRecords]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EquipmentServiceRecords] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [EquipmentId] int NOT NULL,
        [ServiceType] nvarchar(30) NOT NULL,
        [ServiceDate] datetime2 NOT NULL,
        [PerformedBy] nvarchar(150) NOT NULL,
        [Result] nvarchar(20) NOT NULL,
        [Cost] decimal(18, 2) NOT NULL,
        [CertificateNumber] nvarchar(100) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_EquipmentServiceRecords] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table EquipmentServiceRecords';
END;
GO

IF OBJECT_ID(N'[dbo].[Features]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Features] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Module] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Features] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table Features';
END;
GO

IF OBJECT_ID(N'[dbo].[GeneratedReports]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[GeneratedReports] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [ReportName] nvarchar(max) NOT NULL,
        [ReportType] nvarchar(128) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [FromDate] datetime2 NOT NULL,
        [ToDate] datetime2 NOT NULL,
        [DepartmentId] int NULL,
        [FilePath] nvarchar(max) NOT NULL,
        [FileFormat] nvarchar(max) NOT NULL,
        [FileSize] bigint NOT NULL,
        [GeneratedBy] {{StaffId}} NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_GeneratedReports] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table GeneratedReports';
END;
GO

IF OBJECT_ID(N'[dbo].[Hospitals]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Hospitals] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [Code] nvarchar(20) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Address] nvarchar(500) NOT NULL CONSTRAINT [DF_Hospitals_Address] DEFAULT (N''),
        [City] nvarchar(100) NOT NULL CONSTRAINT [DF_Hospitals_City] DEFAULT (N''),
        [Phone] nvarchar(50) NOT NULL CONSTRAINT [DF_Hospitals_Phone] DEFAULT (N''),
        [Email] nvarchar(200) NOT NULL CONSTRAINT [DF_Hospitals_Email] DEFAULT (N''),
        [LicenseNumber] nvarchar(100) NOT NULL CONSTRAINT [DF_Hospitals_LicenseNumber] DEFAULT (N''),
        [IsActive] bit NOT NULL CONSTRAINT [DF_Hospitals_IsActive] DEFAULT ((1)),
        [IsDefault] bit NOT NULL CONSTRAINT [DF_Hospitals_IsDefault] DEFAULT ((0)),
        [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Hospitals_CreatedDate] DEFAULT (sysutcdatetime()),
        [UpdatedDate] datetime2 NULL,
        CONSTRAINT [PK_Hospitals] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table Hospitals';
END;
GO

IF OBJECT_ID(N'[dbo].[IdCardRecords]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[IdCardRecords] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [StaffId] {{StaffId}} NOT NULL,
        [CardNumber] nvarchar(128) NOT NULL,
        [IssueDate] datetime2 NOT NULL,
        [ExpiryDate] datetime2 NULL,
        [Status] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_IdCardRecords] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table IdCardRecords';
END;
GO

IF OBJECT_ID(N'[dbo].[InternalAudits]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InternalAudits] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [HospitalId] int NULL,
        [AuditNumber] nvarchar(30) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Scope] nvarchar(200) NOT NULL,
        [Standard] nvarchar(50) NOT NULL,
        [PlannedDate] datetime2 NOT NULL,
        [LeadAuditor] nvarchar(100) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [CompletedDate] datetime2 NULL,
        [Summary] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_InternalAudits] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table InternalAudits';
END;
GO

IF OBJECT_ID(N'[dbo].[InternalMessages]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[InternalMessages] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [SenderId] {{UserId}} NOT NULL,
        [RecipientId] nvarchar(128) NOT NULL CONSTRAINT [DF__InternalM__Recip__13BCEBC1] DEFAULT (''''),
        [Subject] nvarchar(max) NOT NULL CONSTRAINT [DF__InternalM__Subje__14B10FFA] DEFAULT (''''),
        [Body] nvarchar(max) NOT NULL CONSTRAINT [DF__InternalMe__Body__15A53433] DEFAULT (''''),
        [IsRead] bit NOT NULL CONSTRAINT [DF__InternalM__IsRea__1699586C] DEFAULT ((0)),
        [IsBroadcast] bit NOT NULL CONSTRAINT [DF__InternalM__IsBro__178D7CA5] DEFAULT ((0)),
        [ParentMessageId] int NULL,
        [SentAt] datetime2 NOT NULL,
        [ReadAt] datetime2 NULL,
        [IsDeletedBySender] bit NOT NULL CONSTRAINT [DF__InternalM__IsDel__1881A0DE] DEFAULT ((0)),
        [IsDeletedByRecipient] bit NOT NULL CONSTRAINT [DF__InternalM__IsDel__1975C517] DEFAULT ((0)),
        CONSTRAINT [PK__Internal__3214EC072164ED80] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table InternalMessages';
END;
GO

IF OBJECT_ID(N'[dbo].[InventoryItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InventoryItems] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [ItemCode] nvarchar(max) NOT NULL CONSTRAINT [DF__Inventory__ItemC__1E3A7A34] DEFAULT (''),
        [Name] nvarchar(max) NOT NULL CONSTRAINT [DF__InventoryI__Name__1F2E9E6D] DEFAULT (''),
        [Category] nvarchar(max) NOT NULL CONSTRAINT [DF__Inventory__Categ__2022C2A6] DEFAULT (''),
        [Unit] nvarchar(max) NOT NULL CONSTRAINT [DF__InventoryI__Unit__2116E6DF] DEFAULT (''),
        [CurrentStock] decimal(18, 2) NOT NULL CONSTRAINT [DF__Inventory__Curre__220B0B18] DEFAULT ((0)),
        [MinimumStock] decimal(18, 2) NOT NULL CONSTRAINT [DF__Inventory__Minim__22FF2F51] DEFAULT ((0)),
        [ReorderLevel] decimal(18, 2) NOT NULL CONSTRAINT [DF__Inventory__Reord__23F3538A] DEFAULT ((0)),
        [UnitCost] decimal(18, 2) NOT NULL CONSTRAINT [DF__Inventory__UnitC__24E777C3] DEFAULT ((0)),
        [Supplier] nvarchar(max) NOT NULL CONSTRAINT [DF__Inventory__Suppl__25DB9BFC] DEFAULT (''),
        [StorageLocation] nvarchar(max) NOT NULL CONSTRAINT [DF__Inventory__Stora__26CFC035] DEFAULT (''),
        [IsActive] bit NOT NULL CONSTRAINT [DF__Inventory__IsAct__27C3E46E] DEFAULT ((1)),
        [CreatedDate] datetime2 NOT NULL,
        [HospitalId] int NULL,
        [VendorId] int NULL,
        CONSTRAINT [PK__Inventor__3214EC07E8002EEF] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table InventoryItems';
END;
GO

IF OBJECT_ID(N'[dbo].[InventoryTransactions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InventoryTransactions] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [InventoryItemId] int NOT NULL,
        [TransactionType] nvarchar(max) NOT NULL CONSTRAINT [DF__Inventory__Trans__2AA05119] DEFAULT ('IN'),
        [Quantity] decimal(18, 2) NOT NULL CONSTRAINT [DF__Inventory__Quant__2B947552] DEFAULT ((0)),
        [UnitCost] decimal(18, 2) NOT NULL CONSTRAINT [DF__Inventory__UnitC__2C88998B] DEFAULT ((0)),
        [ReferenceNumber] nvarchar(max) NOT NULL CONSTRAINT [DF__Inventory__Refer__2D7CBDC4] DEFAULT (''),
        [Remarks] nvarchar(max) NOT NULL CONSTRAINT [DF__Inventory__Remar__2E70E1FD] DEFAULT (''),
        [PerformedByUserId] nvarchar(128) NOT NULL CONSTRAINT [DF__Inventory__Perfo__2F650636] DEFAULT (''),
        [TransactionDate] datetime2 NOT NULL,
        [Department] nvarchar(100) NOT NULL CONSTRAINT [DF_InventoryTransactions_Department] DEFAULT (N''),
        [PatientId] int NULL,
        [BillId] int NULL,
        [PurchaseBillId] int NULL,
        CONSTRAINT [PK__Inventor__3214EC07CD80E535] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table InventoryTransactions';
END;
GO

IF OBJECT_ID(N'[dbo].[IPDAdmissions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[IPDAdmissions] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [PatientId] int NOT NULL,
        [DoctorId] int NOT NULL,
        [BedId] int NULL,
        [AdmissionDate] datetime2 NOT NULL,
        [DischargeDate] datetime2 NULL,
        [AdmissionType] nvarchar(max) NOT NULL,
        [Diagnosis] nvarchar(max) NOT NULL,
        [Treatment] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [DailyCharges] decimal(18, 2) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [MedicalRecordId] int NULL,
        [HospitalId] int NULL,
        CONSTRAINT [PK_IPDAdmissions] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table IPDAdmissions';
END;
GO

IF OBJECT_ID(N'[dbo].[LabNoteHistories]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[LabNoteHistories] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [LabResultId] int NOT NULL,
        [Notes] nvarchar(4000) NOT NULL CONSTRAINT [DF__LabNoteHi__Notes__477199F1] DEFAULT (''),
        [UpdatedBy] nvarchar(256) NOT NULL CONSTRAINT [DF__LabNoteHi__Updat__4865BE2A] DEFAULT (''),
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK__LabNoteH__3214EC0750264B83] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table LabNoteHistories';
END;
GO

IF OBJECT_ID(N'[dbo].[LabResults]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[LabResults] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [PatientId] int NOT NULL,
        [LabTestId] int NOT NULL,
        [OrderNumber] nvarchar(max) NOT NULL,
        [OrderDate] datetime2 NOT NULL,
        [ResultDate] datetime2 NULL,
        [ResultValue] nvarchar(max) NOT NULL,
        [NormalRange] nvarchar(max) NOT NULL,
        [Unit] nvarchar(max) NOT NULL,
        [Interpretation] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [PerformedBy] nvarchar(max) NOT NULL,
        [VerifiedBy] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [AccessionNumber] nvarchar(30) NULL,
        [SampleType] nvarchar(50) NOT NULL CONSTRAINT [DF_LabResults_SampleType] DEFAULT (N''),
        [SampleStatus] nvarchar(30) NOT NULL CONSTRAINT [DF_LabResults_SampleStatus] DEFAULT (N''),
        [CollectedAt] datetime2 NULL,
        [CollectedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_LabResults_CollectedBy] DEFAULT (N''),
        [ReceivedAt] datetime2 NULL,
        [ReceivedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_LabResults_ReceivedBy] DEFAULT (N''),
        [RejectionReason] nvarchar(max) NOT NULL CONSTRAINT [DF_LabResults_RejectionReason] DEFAULT (N''),
        [ResultEnteredAt] datetime2 NULL,
        [ResultEnteredBy] nvarchar(max) NOT NULL CONSTRAINT [DF_LabResults_ResultEnteredBy] DEFAULT (N''),
        [SignedOffAt] datetime2 NULL,
        [SignedOffBy] nvarchar(max) NOT NULL CONSTRAINT [DF_LabResults_SignedOffBy] DEFAULT (N''),
        [SignedOffByUserId] nvarchar(max) NOT NULL CONSTRAINT [DF_LabResults_SignedOffByUserId] DEFAULT (N''),
        [SignatureHash] nvarchar(max) NOT NULL CONSTRAINT [DF_LabResults_SignatureHash] DEFAULT (N''),
        CONSTRAINT [PK_LabResults] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table LabResults';
END;
GO

IF OBJECT_ID(N'[dbo].[LabSampleEvents]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[LabSampleEvents] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [LabResultId] int NOT NULL,
        [EventType] nvarchar(40) NOT NULL,
        [OccurredAt] datetime2 NOT NULL,
        [UserId] nvarchar(max) NOT NULL,
        [UserName] nvarchar(max) NOT NULL,
        [Details] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_LabSampleEvents] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table LabSampleEvents';
END;
GO

IF OBJECT_ID(N'[dbo].[LabTests]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[LabTests] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [TestName] nvarchar(max) NOT NULL,
        [TestCode] nvarchar(max) NOT NULL,
        [Category] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Price] decimal(18, 2) NOT NULL,
        [NormalRange] nvarchar(max) NOT NULL,
        [Unit] nvarchar(max) NOT NULL,
        [PreparationTimeHours] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_LabTests] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table LabTests';
END;
GO

IF OBJECT_ID(N'[dbo].[Languages]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Languages] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [Code] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [NativeName] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [IsDefault] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Languages] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table Languages';
END;
GO

IF OBJECT_ID(N'[dbo].[LeaveBalances]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[LeaveBalances] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [StaffId] {{StaffId}} NOT NULL,
        [LeaveTypeId] int NOT NULL,
        [Year] int NOT NULL,
        [AllocatedDays] int NOT NULL,
        [UsedDays] int NOT NULL,
        [RemainingDays] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        CONSTRAINT [PK_LeaveBalances] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table LeaveBalances';
END;
GO

IF OBJECT_ID(N'[dbo].[LeaveRequests]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[LeaveRequests] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [StaffId] {{StaffId}} NOT NULL,
        [LeaveTypeId] int NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [TotalDays] int NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        [ApproverId] nvarchar(max) NOT NULL,
        [ApprovedDate] datetime2 NULL,
        [ApproverRemarks] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        CONSTRAINT [PK_LeaveRequests] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table LeaveRequests';
END;
GO

IF OBJECT_ID(N'[dbo].[LeaveTypes]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[LeaveTypes] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [DefaultDaysPerYear] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_LeaveTypes] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table LeaveTypes';
END;
GO

IF OBJECT_ID(N'[dbo].[LicenseAuditLogs]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[LicenseAuditLogs] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [LicenseRecordId] int NOT NULL,
        [ActionType] nvarchar(50) NOT NULL,
        [PerformedByUserId] {{UserId}} NULL,
        [PerformedAtUtc] datetime2 NOT NULL,
        [OldExpiresAtUtc] datetime2 NULL,
        [NewExpiresAtUtc] datetime2 NULL,
        [RenewalTermYears] int NULL,
        [Details] nvarchar(2000) NULL,
        [IpAddress] nvarchar(64) NULL,
        CONSTRAINT [PK_LicenseAuditLogs] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table LicenseAuditLogs';
END;
GO

IF OBJECT_ID(N'[dbo].[LicenseRecords]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[LicenseRecords] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [LicenseReference] nvarchar(100) NOT NULL,
        [IssuedAtUtc] datetime2 NOT NULL,
        [ExpiresAtUtc] datetime2 NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [LastReminderSentAtUtc] datetime2 NULL,
        [LastReminderCycleExpiryUtc] datetime2 NULL,
        [RenewedByUserId] nvarchar(128) NULL,
        [RenewedAtUtc] datetime2 NULL,
        [RenewalTermYears] int NULL,
        [Notes] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        [ProductName] nvarchar(100) NOT NULL CONSTRAINT [DF_LicenseRecords_ProductName_Mig] DEFAULT ('MedyxHMS'),
        [TenantId] nvarchar(150) NOT NULL CONSTRAINT [DF_LicenseRecords_TenantId_Mig] DEFAULT ('UNCONFIGURED'),
        [LicenseGuid] uniqueidentifier NOT NULL CONSTRAINT [DF_LicenseRecords_LicenseGuid_Mig] DEFAULT ('00000000-0000-0000-0000-000000000000'),
        [MaxConcurrentUsers] int NOT NULL CONSTRAINT [DF_LicenseRecords_MaxConcurrentUsers_Mig] DEFAULT ((0)),
        [VerificationKey] nvarchar(64) NOT NULL CONSTRAINT [DF_LicenseRecords_VerificationKey_Mig] DEFAULT (''),
        [LicensedModulesCsv] nvarchar(max) NOT NULL CONSTRAINT [DF_LicenseRecords_LicensedModulesCsv_Mig] DEFAULT (''),
        [PublicKeyModulusHex] nvarchar(max) NOT NULL CONSTRAINT [DF_LicenseRecords_PublicKeyModulusHex_Mig] DEFAULT (''),
        [PublicKeyExponentHex] nvarchar(40) NOT NULL CONSTRAINT [DF_LicenseRecords_PublicKeyExponentHex_Mig] DEFAULT (''),
        [Nonce] nvarchar(120) NOT NULL CONSTRAINT [DF_LicenseRecords_Nonce_Mig] DEFAULT ('N/A'),
        [SignatureAlgorithm] nvarchar(40) NOT NULL CONSTRAINT [DF_LicenseRecords_SignatureAlgorithm_Mig] DEFAULT ('RSA-SHA256'),
        [SignatureHex] nvarchar(max) NOT NULL CONSTRAINT [DF_LicenseRecords_SignatureHex_Mig] DEFAULT (''),
        [EncodedLicenseFile] nvarchar(max) NOT NULL CONSTRAINT [DF_LicenseRecords_EncodedLicenseFile_Mig] DEFAULT (''),
        [CanonicalPayloadJson] nvarchar(max) NOT NULL CONSTRAINT [DF_LicenseRecords_CanonicalPayloadJson_Mig] DEFAULT (''),
        [PayloadSha256Hex] nvarchar(64) NOT NULL CONSTRAINT [DF_LicenseRecords_PayloadSha256Hex_Mig] DEFAULT (''),
        [IsSignatureValid] bit NOT NULL CONSTRAINT [DF_LicenseRecords_IsSignatureValid_Mig] DEFAULT ((0)),
        [LastValidatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_LicenseRecords] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table LicenseRecords';
END;
GO

IF OBJECT_ID(N'[dbo].[LicenseReminderLogs]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[LicenseReminderLogs] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [LicenseRecordId] int NOT NULL,
        [ReminderType] nvarchar(50) NOT NULL,
        [TargetExpiryUtc] datetime2 NOT NULL,
        [TriggeredAtUtc] datetime2 NOT NULL,
        [SentToCount] int NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [ErrorMessage] nvarchar(2000) NULL,
        CONSTRAINT [PK_LicenseReminderLogs] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table LicenseReminderLogs';
END;
GO

IF OBJECT_ID(N'[dbo].[LiveConsultationSessions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[LiveConsultationSessions] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [PatientId] int NULL,
        [DoctorUserId] nvarchar(128) NOT NULL CONSTRAINT [DF__LiveConsu__Docto__3F9B6DFF] DEFAULT (''),
        [DoctorName] nvarchar(max) NOT NULL CONSTRAINT [DF__LiveConsu__Docto__408F9238] DEFAULT (''),
        [PatientName] nvarchar(max) NOT NULL CONSTRAINT [DF__LiveConsu__Patie__4183B671] DEFAULT (''),
        [ScheduledAt] datetime2 NOT NULL,
        [DurationMinutes] int NOT NULL CONSTRAINT [DF__LiveConsu__Durat__4277DAAA] DEFAULT ((30)),
        [Platform] nvarchar(max) NOT NULL CONSTRAINT [DF__LiveConsu__Platf__436BFEE3] DEFAULT ('Zoom'),
        [MeetingLink] nvarchar(max) NOT NULL CONSTRAINT [DF__LiveConsu__Meeti__4460231C] DEFAULT (''),
        [MeetingId] nvarchar(max) NOT NULL CONSTRAINT [DF__LiveConsu__Meeti__45544755] DEFAULT (''),
        [MeetingPassword] nvarchar(max) NOT NULL CONSTRAINT [DF__LiveConsu__Meeti__46486B8E] DEFAULT (''),
        [Status] nvarchar(max) NOT NULL CONSTRAINT [DF__LiveConsu__Statu__473C8FC7] DEFAULT ('Scheduled'),
        [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF__LiveConsu__Notes__4830B400] DEFAULT (''),
        [BillId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK__LiveCons__3214EC07704DF714] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table LiveConsultationSessions';
END;
GO

IF OBJECT_ID(N'[dbo].[MedicalRecords]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[MedicalRecords] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [PatientId] int NOT NULL,
        [RecordType] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [Diagnosis] nvarchar(1000) NOT NULL,
        [Treatment] nvarchar(2000) NOT NULL,
        [Notes] nvarchar(1000) NOT NULL,
        [DoctorName] nvarchar(100) NOT NULL,
        [DoctorId] {{UserId}} NULL,
        [RecordDate] datetime2 NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(max) NOT NULL,
        [SourceType] nvarchar(30) NOT NULL CONSTRAINT [DF_MedicalRecords_SourceType] DEFAULT (N''''),
        [SourceId] int NULL,
        CONSTRAINT [PK_MedicalRecords] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table MedicalRecords';
END;
GO

IF OBJECT_ID(N'[dbo].[Medicines]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Medicines] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [GenericName] nvarchar(max) NOT NULL,
        [Category] nvarchar(max) NOT NULL,
        [DosageForm] nvarchar(max) NOT NULL,
        [Strength] nvarchar(max) NOT NULL,
        [Manufacturer] nvarchar(max) NOT NULL,
        [UnitPrice] decimal(18, 2) NOT NULL,
        [StockQuantity] int NOT NULL,
        [MinStockLevel] int NOT NULL,
        [ExpiryDate] datetime2 NOT NULL,
        [BatchNumber] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Medicines] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table Medicines';
END;
GO

IF OBJECT_ID(N'[dbo].[NotificationDeliveryLogs]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[NotificationDeliveryLogs] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [Channel] nvarchar(20) NOT NULL,
        [Provider] nvarchar(50) NOT NULL,
        [Recipient] nvarchar(200) NOT NULL,
        [Subject] nvarchar(200) NOT NULL,
        [MessageBody] nvarchar(max) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [ProviderResponse] nvarchar(2000) NOT NULL,
        [RelatedEntityType] nvarchar(50) NOT NULL,
        [RelatedEntityId] nvarchar(100) NOT NULL,
        [IsTest] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_NotificationDeliveryLogs] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table NotificationDeliveryLogs';
END;
GO

IF OBJECT_ID(N'[dbo].[OPDVisits]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OPDVisits] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [PatientId] int NOT NULL,
        [DoctorId] int NOT NULL,
        [VisitDate] datetime2 NOT NULL,
        [Symptoms] nvarchar(max) NOT NULL,
        [Diagnosis] nvarchar(max) NOT NULL,
        [Treatment] nvarchar(max) NOT NULL,
        [Prescription] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [ConsultationFee] decimal(18, 2) NOT NULL,
        [PaymentStatus] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [MedicalRecordId] int NULL,
        [HospitalId] int NULL,
        CONSTRAINT [PK_OPDVisits] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table OPDVisits';
END;
GO

IF OBJECT_ID(N'[dbo].[OperationTheatres]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OperationTheatres] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [HospitalId] int NULL,
        [Code] nvarchar(20) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Location] nvarchar(100) NOT NULL,
        [TheatreType] nvarchar(50) NOT NULL,
        [Features] nvarchar(500) NOT NULL,
        [OpensAt] time NOT NULL,
        [ClosesAt] time NOT NULL,
        [Is24Hours] bit NOT NULL,
        [TurnoverMinutes] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_OperationTheatres] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table OperationTheatres';
END;
GO

IF OBJECT_ID(N'[dbo].[OTBlocks]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OTBlocks] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [OperationTheatreId] int NOT NULL,
        [StartsAt] datetime2 NOT NULL,
        [EndsAt] datetime2 NOT NULL,
        [Reason] nvarchar(200) NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_OTBlocks] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table OTBlocks';
END;
GO

IF OBJECT_ID(N'[dbo].[OTSchedules]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OTSchedules] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [PatientId] int NOT NULL,
        [ProcedureName] nvarchar(max) NOT NULL,
        [SurgeonName] nvarchar(max) NOT NULL,
        [ScheduledDate] datetime2 NOT NULL,
        [EstimatedDurationMinutes] int NOT NULL,
        [OperationTheatreNumber] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [BillId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [HospitalId] int NULL,
        [OperationTheatreId] int NULL,
        [SurgeonDoctorId] int NULL,
        [IsEmergency] bit NOT NULL CONSTRAINT [DF_OTSchedules_IsEmergency] DEFAULT ((0)),
        [CancelReason] nvarchar(max) NOT NULL CONSTRAINT [DF_OTSchedules_CancelReason] DEFAULT (N''),
        [StartedAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        CONSTRAINT [PK_OTSchedules] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table OTSchedules';
END;
GO

IF OBJECT_ID(N'[dbo].[PatientDocuments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PatientDocuments] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [PatientId] int NOT NULL,
        [Category] nvarchar(50) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NOT NULL,
        [DocumentDate] datetime2 NULL,
        [OriginalFileName] nvarchar(255) NOT NULL,
        [StoredFileName] nvarchar(100) NOT NULL,
        [ContentType] nvarchar(100) NOT NULL,
        [SizeBytes] bigint NOT NULL,
        [Sha256] nvarchar(64) NOT NULL,
        [UploadedByPatient] bit NOT NULL,
        [SharedWithPatient] bit NOT NULL,
        [UploadedByUserId] nvarchar(max) NOT NULL,
        [UploadedBy] nvarchar(max) NOT NULL,
        [UploadedAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedBy] nvarchar(max) NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeleteReason] nvarchar(300) NOT NULL,
        CONSTRAINT [PK_PatientDocuments] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table PatientDocuments';
END;
GO

IF OBJECT_ID(N'[dbo].[PatientInsurances]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PatientInsurances] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [PatientId] int NOT NULL,
        [ProviderName] nvarchar(100) NOT NULL CONSTRAINT [DF__PatientIn__Provi__39237A9A] DEFAULT (''),
        [PolicyNumber] nvarchar(100) NOT NULL CONSTRAINT [DF__PatientIn__Polic__3A179ED3] DEFAULT (''),
        [InsurancePlan] nvarchar(100) NOT NULL CONSTRAINT [DF__PatientIn__Insur__3B0BC30C] DEFAULT (''),
        [HolderName] nvarchar(100) NOT NULL CONSTRAINT [DF__PatientIn__Holde__3BFFE745] DEFAULT (''),
        [ValidFrom] datetime2 NULL,
        [ValidTo] datetime2 NULL,
        [ContactNumber] nvarchar(100) NOT NULL CONSTRAINT [DF__PatientIn__Conta__3CF40B7E] DEFAULT (''),
        [Notes] nvarchar(1000) NOT NULL CONSTRAINT [DF__PatientIn__Notes__3DE82FB7] DEFAULT (''),
        [IsActive] bit NOT NULL CONSTRAINT [DF__PatientIn__IsAct__3EDC53F0] DEFAULT ((1)),
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK__PatientI__3214EC071C509EBC] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table PatientInsurances';
END;
GO

IF OBJECT_ID(N'[dbo].[Patients]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[Patients] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [PatientId] nvarchar(max) NOT NULL,
        [FirstName] nvarchar(max) NOT NULL,
        [LastName] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NOT NULL,
        [DateOfBirth] datetime2 NOT NULL,
        [Gender] nvarchar(max) NOT NULL,
        [Address] nvarchar(max) NOT NULL,
        [City] nvarchar(max) NOT NULL,
        [State] nvarchar(max) NOT NULL,
        [Country] nvarchar(max) NOT NULL,
        [PostalCode] nvarchar(max) NOT NULL,
        [BloodGroup] nvarchar(max) NOT NULL,
        [EmergencyContactName] nvarchar(max) NOT NULL,
        [EmergencyContactPhone] nvarchar(max) NOT NULL,
        [EmergencyContactRelation] nvarchar(max) NOT NULL,
        [MedicalHistory] nvarchar(max) NOT NULL,
        [Allergies] nvarchar(max) NOT NULL,
        [GuardianName] nvarchar(max) NOT NULL,
        [GuardianPhone] nvarchar(max) NOT NULL,
        [MaritalStatus] nvarchar(max) NOT NULL,
        [Occupation] nvarchar(max) NOT NULL,
        [UserId] {{UserId}} NULL,
        [ProfileImagePath] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastVisitDate] datetime2 NULL,
        [HasInsurance] bit NOT NULL CONSTRAINT [DF_Patients_HasInsurance] DEFAULT ((0)),
        CONSTRAINT [PK_Patients] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table Patients';
END;
GO

IF OBJECT_ID(N'[dbo].[Payments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Payments] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [BillId] int NOT NULL,
        [PaymentMethod] nvarchar(max) NOT NULL,
        [Amount] decimal(18, 2) NOT NULL,
        [TransactionId] nvarchar(max) NOT NULL,
        [PaymentGateway] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [PaymentDate] datetime2 NOT NULL,
        [ProcessedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table Payments';
END;
GO

IF OBJECT_ID(N'[dbo].[PayrollRecords]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[PayrollRecords] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [StaffId] {{StaffId}} NOT NULL,
        [PayrollMonth] datetime2 NOT NULL,
        [BasicSalary] decimal(18, 2) NOT NULL,
        [Allowances] decimal(18, 2) NOT NULL,
        [Deductions] decimal(18, 2) NOT NULL,
        [NetSalary] decimal(18, 2) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [PaymentDate] datetime2 NULL,
        [Notes] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        CONSTRAINT [PK_PayrollRecords] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table PayrollRecords';
END;
GO

IF OBJECT_ID(N'[dbo].[PharmacyBills]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PharmacyBills] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [BillNumber] nvarchar(max) NOT NULL,
        [PatientId] int NOT NULL,
        [BillDate] datetime2 NOT NULL,
        [TotalAmount] decimal(18, 2) NOT NULL,
        [PaidAmount] decimal(18, 2) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [PaymentMethod] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [HospitalId] int NULL,
        CONSTRAINT [PK_PharmacyBills] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table PharmacyBills';
END;
GO

IF OBJECT_ID(N'[dbo].[Prescriptions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Prescriptions] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [PharmacyBillId] int NOT NULL,
        [MedicineId] int NOT NULL,
        [Dosage] nvarchar(max) NOT NULL,
        [Frequency] nvarchar(max) NOT NULL,
        [Duration] int NOT NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(18, 2) NOT NULL,
        [TotalPrice] decimal(18, 2) NOT NULL,
        [Instructions] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Prescriptions] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table Prescriptions';
END;
GO

IF OBJECT_ID(N'[dbo].[Printers]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Printers] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [PrinterType] nvarchar(20) NOT NULL,
        [ConnectionType] nvarchar(10) NOT NULL,
        [IpAddress] nvarchar(255) NOT NULL,
        [Port] int NOT NULL,
        [SharePath] nvarchar(255) NOT NULL,
        [PaperSize] nvarchar(20) NOT NULL,
        [PrintLanguage] nvarchar(20) NOT NULL,
        [Copies] int NOT NULL,
        [CutPaper] bit NOT NULL,
        [OpenCashDrawer] bit NOT NULL,
        [HospitalId] int NULL,
        [IsDefault] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [Notes] nvarchar(500) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        [CreatedBy] nvarchar(256) NOT NULL,
        [LastTestedDate] datetime2 NULL,
        [LastTestResult] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_Printers] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table Printers';
END;
GO

IF OBJECT_ID(N'[dbo].[PublicAppointmentRequests]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PublicAppointmentRequests] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [PatientName] nvarchar(150) NOT NULL,
        [Phone] nvarchar(20) NOT NULL,
        [Email] nvarchar(200) NULL,
        [Gender] nvarchar(10) NULL,
        [Age] nvarchar(10) NULL,
        [PatientId] int NOT NULL,
        [DoctorId] int NOT NULL,
        [PreferredDate] datetime2 NOT NULL,
        [PreferredTime] time NOT NULL,
        [Symptoms] nvarchar(500) NULL,
        [Notes] nvarchar(500) NULL,
        [Status] nvarchar(20) NOT NULL,
        [AdminNotes] nvarchar(300) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IpAddress] nvarchar(45) NULL,
        CONSTRAINT [PK_PublicAppointmentRequests] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table PublicAppointmentRequests';
END;
GO

IF OBJECT_ID(N'[dbo].[PurchaseBillItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PurchaseBillItems] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [PurchaseBillId] int NOT NULL,
        [InventoryItemId] int NULL,
        [Description] nvarchar(200) NOT NULL,
        [Quantity] decimal(18, 2) NOT NULL,
        [UnitCost] decimal(18, 2) NOT NULL,
        [TaxPercent] decimal(18, 2) NOT NULL,
        [LineTotal] decimal(18, 2) NOT NULL,
        [BatchNumber] nvarchar(50) NOT NULL,
        [ExpiryDate] datetime2 NULL,
        CONSTRAINT [PK_PurchaseBillItems] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table PurchaseBillItems';
END;
GO

IF OBJECT_ID(N'[dbo].[PurchaseBills]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PurchaseBills] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [HospitalId] int NULL,
        [BillNumber] nvarchar(30) NOT NULL,
        [VendorId] int NOT NULL,
        [VendorInvoiceNumber] nvarchar(50) NOT NULL,
        [InvoiceDate] datetime2 NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [PaymentStatus] nvarchar(20) NOT NULL,
        [SubTotal] decimal(18, 2) NOT NULL,
        [TaxAmount] decimal(18, 2) NOT NULL,
        [TotalAmount] decimal(18, 2) NOT NULL,
        [PaidAmount] decimal(18, 2) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [CreatedByUserId] nvarchar(max) NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [SubmittedAt] datetime2 NULL,
        [ApprovedBy] nvarchar(max) NOT NULL,
        [ApprovedAt] datetime2 NULL,
        [ApprovalComments] nvarchar(max) NOT NULL,
        [ReceivedBy] nvarchar(max) NOT NULL,
        [ReceivedAt] datetime2 NULL,
        [CancelReason] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_PurchaseBills] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table PurchaseBills';
END;
GO

IF OBJECT_ID(N'[dbo].[QualityIncidents]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[QualityIncidents] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [HospitalId] int NULL,
        [IncidentNumber] nvarchar(30) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Category] nvarchar(50) NOT NULL,
        [Severity] nvarchar(20) NOT NULL,
        [OccurredAt] datetime2 NOT NULL,
        [ReportedAt] datetime2 NOT NULL,
        [Location] nvarchar(150) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [ImmediateAction] nvarchar(max) NOT NULL,
        [PatientId] int NULL,
        [ReportedByUserId] nvarchar(max) NOT NULL,
        [ReportedByName] nvarchar(max) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [RootCause] nvarchar(max) NOT NULL,
        [ClosedAt] datetime2 NULL,
        [ClosedBy] nvarchar(max) NOT NULL,
        [ClosureNotes] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_QualityIncidents] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table QualityIncidents';
END;
GO

IF OBJECT_ID(N'[dbo].[RadiologyResults]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RadiologyResults] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [PatientId] int NOT NULL,
        [RadiologyTestId] int NOT NULL,
        [OrderNumber] nvarchar(max) NOT NULL,
        [OrderDate] datetime2 NOT NULL,
        [ResultDate] datetime2 NULL,
        [Findings] nvarchar(max) NOT NULL,
        [Impression] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [PerformedBy] nvarchar(max) NOT NULL,
        [VerifiedBy] nvarchar(max) NOT NULL,
        [ImagePath] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_RadiologyResults] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table RadiologyResults';
END;
GO

IF OBJECT_ID(N'[dbo].[RadiologyTests]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RadiologyTests] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [TestName] nvarchar(max) NOT NULL,
        [TestCode] nvarchar(max) NOT NULL,
        [Category] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Price] decimal(18, 2) NOT NULL,
        [PreparationTimeHours] int NOT NULL,
        [SpecialInstructions] nvarchar(max) NOT NULL,
        [RequiresContrast] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_RadiologyTests] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table RadiologyTests';
END;
GO

IF OBJECT_ID(N'[dbo].[Referrals]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Referrals] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [PatientId] int NOT NULL,
        [ReferralType] nvarchar(max) NOT NULL,
        [ReferredTo] nvarchar(max) NOT NULL,
        [ReferralReason] nvarchar(max) NOT NULL,
        [ReferralDate] datetime2 NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [TpaProvider] nvarchar(max) NOT NULL,
        [TpaPolicyNumber] nvarchar(max) NOT NULL,
        [ApprovedAmount] decimal(18, 2) NULL,
        [Notes] nvarchar(max) NOT NULL,
        [BillId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Referrals] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table Referrals';
END;
GO

IF OBJECT_ID(N'[dbo].[ReportCharts]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ReportCharts] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [TemplateId] int NOT NULL,
        [Title] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportCha__Title__75035A77] DEFAULT (''),
        [ChartType] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportCha__Chart__75F77EB0] DEFAULT ('bar'),
        [XAxisField] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportCha__XAxis__76EBA2E9] DEFAULT (''),
        [YAxisField] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportCha__YAxis__77DFC722] DEFAULT (''),
        [ShowLegend] bit NOT NULL CONSTRAINT [DF__ReportCha__ShowL__78D3EB5B] DEFAULT ((1)),
        [ShowTooltip] bit NOT NULL CONSTRAINT [DF__ReportCha__ShowT__79C80F94] DEFAULT ((1)),
        [ColorScheme] nvarchar(max) NULL,
        [SortOrder] int NOT NULL CONSTRAINT [DF__ReportCha__SortO__7ABC33CD] DEFAULT ((0)),
        CONSTRAINT [PK__ReportCh__3214EC07BB64704B] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table ReportCharts';
END;
GO

IF OBJECT_ID(N'[dbo].[ReportDesigns]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ReportDesigns] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [TemplateId] int NOT NULL,
        [HeaderText] nvarchar(max) NULL,
        [FooterText] nvarchar(max) NULL,
        [ColorScheme] nvarchar(max) NULL,
        [ShowGridLines] bit NOT NULL CONSTRAINT [DF__ReportDes__ShowG__6D6238AF] DEFAULT ((1)),
        [ShowAlternatingRows] bit NOT NULL CONSTRAINT [DF__ReportDes__ShowA__6E565CE8] DEFAULT ((1)),
        [ShowTotals] bit NOT NULL CONSTRAINT [DF__ReportDes__ShowT__6F4A8121] DEFAULT ((0)),
        [ShowGrouping] bit NOT NULL CONSTRAINT [DF__ReportDes__ShowG__703EA55A] DEFAULT ((0)),
        [PageOrientation] nvarchar(max) NULL,
        [IncludeTimestamp] bit NOT NULL CONSTRAINT [DF__ReportDes__Inclu__7132C993] DEFAULT ((1)),
        [CompanyLogo] nvarchar(max) NULL,
        [CustomCss] nvarchar(max) NULL,
        CONSTRAINT [PK__ReportDe__3214EC071F7BF42D] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table ReportDesigns';
END;
GO

IF OBJECT_ID(N'[dbo].[ReportFields]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ReportFields] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [TemplateId] int NOT NULL,
        [FieldName] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportFie__Field__5B438874] DEFAULT (''),
        [ColumnName] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportFie__Colum__5C37ACAD] DEFAULT (''),
        [DataType] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportFie__DataT__5D2BD0E6] DEFAULT ('string'),
        [DisplayFormat] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportFie__Displ__5E1FF51F] DEFAULT (''),
        [IsVisible] bit NOT NULL CONSTRAINT [DF__ReportFie__IsVis__5F141958] DEFAULT ((1)),
        [IsSortable] bit NOT NULL CONSTRAINT [DF__ReportFie__IsSor__60083D91] DEFAULT ((1)),
        [IsFilterable] bit NOT NULL CONSTRAINT [DF__ReportFie__IsFil__60FC61CA] DEFAULT ((1)),
        [SortOrder] int NOT NULL CONSTRAINT [DF__ReportFie__SortO__61F08603] DEFAULT ((0)),
        [Width] int NULL,
        [Alignment] nvarchar(max) NULL,
        CONSTRAINT [PK__ReportFi__3214EC075538B2D6] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table ReportFields';
END;
GO

IF OBJECT_ID(N'[dbo].[ReportFilters]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ReportFilters] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [TemplateId] int NOT NULL,
        [FilterName] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportFil__Filte__65C116E7] DEFAULT (''),
        [ColumnName] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportFil__Colum__66B53B20] DEFAULT (''),
        [OperatorType] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportFil__Opera__67A95F59] DEFAULT ('equals'),
        [DefaultValue] nvarchar(max) NULL,
        [IsRequired] bit NOT NULL CONSTRAINT [DF__ReportFil__IsReq__689D8392] DEFAULT ((0)),
        [SortOrder] int NOT NULL CONSTRAINT [DF__ReportFil__SortO__6991A7CB] DEFAULT ((0)),
        CONSTRAINT [PK__ReportFi__3214EC07B50C62D5] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table ReportFilters';
END;
GO

IF OBJECT_ID(N'[dbo].[ReportSchedules]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[ReportSchedules] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [ReportName] nvarchar(max) NOT NULL,
        [ReportType] nvarchar(max) NOT NULL,
        [RecurrencePattern] nvarchar(max) NOT NULL,
        [DayOfWeek] int NULL,
        [DayOfMonth] int NULL,
        [TimeOfDay] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [EmailRecipients] nvarchar(max) NOT NULL,
        [CreatedBy] {{StaffId}} NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastRunDate] datetime2 NULL,
        [NextRunDate] datetime2 NULL,
        CONSTRAINT [PK_ReportSchedules] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table ReportSchedules';
END;
GO

IF OBJECT_ID(N'[dbo].[ReportTemplates]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ReportTemplates] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [Name] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportTemp__Name__53A266AC] DEFAULT (''),
        [Description] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportTem__Descr__54968AE5] DEFAULT (''),
        [ReportType] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportTem__Repor__558AAF1E] DEFAULT (''),
        [CreatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportTem__Creat__567ED357] DEFAULT (''),
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(max) NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF__ReportTem__IsAct__5772F790] DEFAULT ((1)),
        [IsDefault] bit NOT NULL CONSTRAINT [DF__ReportTem__IsDef__58671BC9] DEFAULT ((0)),
        CONSTRAINT [PK__ReportTe__3214EC0760A9BA07] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table ReportTemplates';
END;
GO

IF OBJECT_ID(N'[dbo].[RoleFeatures]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RoleFeatures] (
        [RoleId] int NOT NULL,
        [FeatureId] int NOT NULL,
        [CanView] bit NOT NULL,
        [CanAdd] bit NOT NULL,
        [CanEdit] bit NOT NULL,
        [CanDelete] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_RoleFeatures] PRIMARY KEY CLUSTERED ([RoleId], [FeatureId])
    );
    PRINT N'Created table RoleFeatures';
END;
GO

IF OBJECT_ID(N'[dbo].[Roles]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Roles] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table Roles';
END;
GO

IF OBJECT_ID(N'[dbo].[SavedReports]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SavedReports] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [TemplateId] int NOT NULL,
        [Title] nvarchar(max) NOT NULL CONSTRAINT [DF__SavedRepo__Title__7E8CC4B1] DEFAULT (''),
        [CreatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF__SavedRepo__Creat__7F80E8EA] DEFAULT (''),
        [CreatedDate] datetime2 NOT NULL,
        [Description] nvarchar(max) NULL,
        [FilterParams] nvarchar(max) NULL,
        [ReportData] nvarchar(max) NULL,
        [RecordCount] int NOT NULL CONSTRAINT [DF__SavedRepo__Recor__00750D23] DEFAULT ((0)),
        [ExecutionTimeMs] decimal(18, 2) NULL,
        CONSTRAINT [PK__SavedRep__3214EC07331CCCA3] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table SavedReports';
END;
GO

IF OBJECT_ID(N'[dbo].[Settings]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Settings] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [Key] nvarchar(max) NOT NULL,
        [Value] nvarchar(max) NOT NULL,
        [Type] nvarchar(max) NOT NULL,
        [Category] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [IsSystem] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Settings] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table Settings';
END;
GO

IF OBJECT_ID(N'[dbo].[Staff]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[Staff] (
        [Id] {{UserId}} NOT NULL,
        [EmployeeId] nvarchar(max) NOT NULL,
        [FirstName] nvarchar(max) NOT NULL,
        [LastName] nvarchar(max) NOT NULL,
        [Department] nvarchar(max) NOT NULL,
        [Designation] nvarchar(max) NOT NULL,
        [DateOfJoining] datetime2 NOT NULL,
        [Salary] decimal(18, 2) NOT NULL,
        [Phone] nvarchar(max) NOT NULL,
        [Address] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [About] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UserId] {{UserId}} NULL,
        CONSTRAINT [PK_Staff] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table Staff';
END;
GO

IF OBJECT_ID(N'[dbo].[StaffAttendances]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[StaffAttendances] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [StaffId] {{StaffId}} NOT NULL,
        [AttendanceDate] datetime2 NOT NULL,
        [CheckInTime] datetime2 NULL,
        [CheckOutTime] datetime2 NULL,
        [Status] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [UpdatedDate] datetime2 NULL,
        CONSTRAINT [PK_StaffAttendances] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table StaffAttendances';
END;
GO

IF OBJECT_ID(N'[dbo].[StaffRoles]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[StaffRoles] (
        [StaffId] {{StaffId}} NOT NULL,
        [RoleId] int NOT NULL,
        [AssignedDate] datetime2 NOT NULL,
        [AssignedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_StaffRoles] PRIMARY KEY CLUSTERED ([StaffId], [RoleId])
    );';
    PRINT N'Created table StaffRoles';
END;
GO

IF OBJECT_ID(N'[dbo].[SystemModules]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SystemModules] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [Key] nvarchar(50) NOT NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        [Description] nvarchar(300) NULL,
        [Icon] nvarchar(100) NULL,
        [IsGloballyEnabled] bit NOT NULL CONSTRAINT [DF__SystemMod__IsGlo__2DB1C7EE] DEFAULT ((1)),
        [SortOrder] int NOT NULL CONSTRAINT [DF__SystemMod__SortO__2EA5EC27] DEFAULT ((0)),
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        [UpdatedByUserId] nvarchar(450) NULL,
        CONSTRAINT [PK__SystemMo__3214EC07FE3BF154] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table SystemModules';
END;
GO

IF OBJECT_ID(N'[dbo].[SystemNotifications]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[SystemNotifications] (
        [Id] bigint IDENTITY(1, 1) NOT NULL,
        [UserId] {{UserId}} NOT NULL,
        [PatientId] int NULL,
        [Title] nvarchar(200) NOT NULL CONSTRAINT [DF__SystemNot__Title__4C364F0E] DEFAULT (''''),
        [Message] nvarchar(2000) NOT NULL CONSTRAINT [DF__SystemNot__Messa__4D2A7347] DEFAULT (''''),
        [Type] nvarchar(50) NOT NULL CONSTRAINT [DF__SystemNoti__Type__4E1E9780] DEFAULT (''General''),
        [RelatedEntityType] nvarchar(100) NOT NULL CONSTRAINT [DF__SystemNot__Relat__4F12BBB9] DEFAULT (''''),
        [RelatedEntityId] nvarchar(100) NOT NULL CONSTRAINT [DF__SystemNot__Relat__5006DFF2] DEFAULT (''''),
        [IsRead] bit NOT NULL CONSTRAINT [DF__SystemNot__IsRea__50FB042B] DEFAULT ((0)),
        [CreatedAtUtc] datetime2 NOT NULL,
        [ReadAtUtc] datetime2 NULL,
        CONSTRAINT [PK__SystemNo__3214EC07AE76E6DB] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table SystemNotifications';
END;
GO

IF OBJECT_ID(N'[dbo].[TestResults]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[TestResults] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [PatientId] int NOT NULL,
        [TestType] nvarchar(100) NOT NULL,
        [TestName] nvarchar(200) NOT NULL,
        [TestDescription] nvarchar(500) NOT NULL,
        [Result] nvarchar(max) NOT NULL,
        [Unit] nvarchar(50) NOT NULL,
        [ReferenceRange] nvarchar(50) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [PerformedBy] nvarchar(100) NOT NULL,
        [DoctorId] {{UserId}} NOT NULL,
        [TestDate] datetime2 NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(max) NOT NULL,
        [MedicalRecordId] int NULL,
        CONSTRAINT [PK_TestResults] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table TestResults';
END;
GO

IF OBJECT_ID(N'[dbo].[TpaClaims]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TpaClaims] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [TpaProviderId] int NOT NULL,
        [PatientId] int NOT NULL,
        [BillId] int NULL,
        [ClaimNumber] nvarchar(max) NOT NULL CONSTRAINT [DF__TpaClaims__Claim__0B27A5C0] DEFAULT (''),
        [ClaimedAmount] decimal(18, 2) NOT NULL CONSTRAINT [DF__TpaClaims__Claim__0C1BC9F9] DEFAULT ((0)),
        [ApprovedAmount] decimal(18, 2) NULL,
        [SettledAmount] decimal(18, 2) NULL,
        [Status] nvarchar(max) NOT NULL CONSTRAINT [DF__TpaClaims__Statu__0D0FEE32] DEFAULT ('Pending'),
        [ClaimDate] datetime2 NOT NULL,
        [SettlementDate] datetime2 NULL,
        [Remarks] nvarchar(max) NOT NULL CONSTRAINT [DF__TpaClaims__Remar__0E04126B] DEFAULT (''),
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK__TpaClaim__3214EC07B579B0EE] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table TpaClaims';
END;
GO

IF OBJECT_ID(N'[dbo].[TpaProviders]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TpaProviders] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [Name] nvarchar(max) NOT NULL CONSTRAINT [DF__TpaProvide__Name__00AA174D] DEFAULT (''),
        [Code] nvarchar(max) NOT NULL CONSTRAINT [DF__TpaProvide__Code__019E3B86] DEFAULT (''),
        [ContactPerson] nvarchar(max) NOT NULL CONSTRAINT [DF__TpaProvid__Conta__02925FBF] DEFAULT (''),
        [ContactEmail] nvarchar(max) NOT NULL CONSTRAINT [DF__TpaProvid__Conta__038683F8] DEFAULT (''),
        [ContactPhone] nvarchar(max) NOT NULL CONSTRAINT [DF__TpaProvid__Conta__047AA831] DEFAULT (''),
        [Address] nvarchar(max) NOT NULL CONSTRAINT [DF__TpaProvid__Addre__056ECC6A] DEFAULT (''),
        [TpaNetwork] nvarchar(max) NOT NULL CONSTRAINT [DF__TpaProvid__TpaNe__0662F0A3] DEFAULT (''),
        [IsActive] bit NOT NULL CONSTRAINT [DF__TpaProvid__IsAct__075714DC] DEFAULT ((1)),
        [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF__TpaProvid__Notes__084B3915] DEFAULT (''),
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK__TpaProvi__3214EC077067D27A] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table TpaProviders';
END;
GO

IF OBJECT_ID(N'[dbo].[TrainingRecords]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TrainingRecords] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [StaffId] nvarchar(450) NOT NULL,
        [StaffName] nvarchar(max) NOT NULL,
        [StaffDepartment] nvarchar(max) NOT NULL,
        [CourseTitle] nvarchar(200) NOT NULL,
        [Category] nvarchar(50) NOT NULL,
        [Provider] nvarchar(150) NOT NULL,
        [CompletedDate] datetime2 NOT NULL,
        [ExpiryDate] datetime2 NULL,
        [CertificateNumber] nvarchar(100) NOT NULL,
        [Result] nvarchar(50) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_TrainingRecords] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table TrainingRecords';
END;
GO

IF OBJECT_ID(N'[dbo].[Transactions]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Transactions] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [TransactionId] nvarchar(max) NOT NULL,
        [TransactionType] nvarchar(max) NOT NULL,
        [Amount] decimal(18, 2) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [ReferenceNumber] nvarchar(max) NOT NULL,
        [TransactionDate] datetime2 NOT NULL,
        [ProcessedBy] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Transactions] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table Transactions';
END;
GO

IF OBJECT_ID(N'[dbo].[UserActionLogs]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[UserActionLogs] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [UserId] nvarchar(max) NOT NULL,
        [ActionType] nvarchar(max) NOT NULL,
        [Details] nvarchar(max) NOT NULL,
        [IPAddress] nvarchar(max) NOT NULL,
        [LoggedDate] datetime2 NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [StaffId] {{StaffId}} NOT NULL,
        CONSTRAINT [PK_UserActionLogs] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table UserActionLogs';
END;
GO

IF OBJECT_ID(N'[dbo].[UserHospitalAccesses]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[UserHospitalAccesses] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [UserId] {{UserId}} NOT NULL,
        [HospitalId] int NOT NULL,
        [IsDefault] bit NOT NULL CONSTRAINT [DF_UserHospitalAccesses_IsDefault] DEFAULT ((0)),
        [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_UserHospitalAccesses_CreatedDate] DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_UserHospitalAccesses] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table UserHospitalAccesses';
END;
GO

IF OBJECT_ID(N'[dbo].[UserModuleAccesses]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[UserModuleAccesses] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [UserId] {{UserId}} NOT NULL,
        [ModuleId] int NOT NULL,
        [IsEnabled] bit NOT NULL CONSTRAINT [DF__UserModul__IsEna__318258D2] DEFAULT ((1)),
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        [UpdatedByUserId] nvarchar(128) NULL,
        CONSTRAINT [PK__UserModu__3214EC07F8098E1B] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table UserModuleAccesses';
END;
GO

IF OBJECT_ID(N'[dbo].[UserSessions]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[UserSessions] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [UserId] {{UserId}} NOT NULL,
        [SessionId] nvarchar(128) NOT NULL,
        [ActiveRole] nvarchar(50) NOT NULL,
        [IpAddress] nvarchar(64) NULL,
        [UserAgent] nvarchar(512) NULL,
        [LoginAtUtc] datetime2 NOT NULL,
        [LastActivityUtc] datetime2 NOT NULL,
        [LogoutAtUtc] datetime2 NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF__UserSessi__IsAct__29E1370A] DEFAULT ((1)),
        CONSTRAINT [PK__UserSess__3214EC072DA2F971] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table UserSessions';
END;
GO

IF OBJECT_ID(N'[dbo].[UserThemePreferences]', N'U') IS NULL
BEGIN
    EXEC #ExecWithKeyTypes @Sql = N'CREATE TABLE [dbo].[UserThemePreferences] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [UserId] {{UserId}} NOT NULL,
        [ThemeId] nvarchar(50) NOT NULL,
        [PreferenceSince] datetime2 NOT NULL,
        [IsDefault] bit NOT NULL CONSTRAINT [DF__UserTheme__IsDef__062DE679] DEFAULT ((0)),
        CONSTRAINT [PK__UserThem__3214EC075260EFAF] PRIMARY KEY CLUSTERED ([Id])
    );';
    PRINT N'Created table UserThemePreferences';
END;
GO

IF OBJECT_ID(N'[dbo].[VendorPayments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[VendorPayments] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [PurchaseBillId] int NOT NULL,
        [Amount] decimal(18, 2) NOT NULL,
        [PaymentDate] datetime2 NOT NULL,
        [Method] nvarchar(30) NOT NULL,
        [Reference] nvarchar(100) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_VendorPayments] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table VendorPayments';
END;
GO

IF OBJECT_ID(N'[dbo].[Vendors]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Vendors] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [VendorCode] nvarchar(20) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Category] nvarchar(50) NOT NULL,
        [ContactPerson] nvarchar(100) NOT NULL,
        [Phone] nvarchar(50) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [Address] nvarchar(300) NOT NULL,
        [City] nvarchar(100) NOT NULL,
        [TaxNumber] nvarchar(50) NOT NULL,
        [LicenseNumber] nvarchar(100) NOT NULL,
        [PaymentTermsDays] int NOT NULL,
        [BankName] nvarchar(100) NOT NULL,
        [BankAccountName] nvarchar(100) NOT NULL,
        [BankAccountNumber] nvarchar(50) NOT NULL,
        [BankRoutingCode] nvarchar(30) NOT NULL,
        [Rating] int NOT NULL,
        [IsActive] bit NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Vendors] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table Vendors';
END;
GO

IF OBJECT_ID(N'[dbo].[VisitNoteHistories]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[VisitNoteHistories] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [OPDVisitId] int NOT NULL,
        [Notes] nvarchar(4000) NOT NULL CONSTRAINT [DF__VisitNote__Notes__42ACE4D4] DEFAULT (''),
        [UpdatedBy] nvarchar(256) NOT NULL CONSTRAINT [DF__VisitNote__Updat__43A1090D] DEFAULT (''),
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK__VisitNot__3214EC0792F2B869] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table VisitNoteHistories';
END;
GO

IF OBJECT_ID(N'[dbo].[VisitorLogs]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[VisitorLogs] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [VisitorName] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NOT NULL,
        [Purpose] nvarchar(max) NOT NULL,
        [PersonToMeet] nvarchar(max) NOT NULL,
        [VisitDate] datetime2 NOT NULL,
        [CheckInTime] datetime2 NOT NULL,
        [CheckOutTime] datetime2 NULL,
        [Status] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_VisitorLogs] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table VisitorLogs';
END;
GO

IF OBJECT_ID(N'[dbo].[Wards]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Wards] (
        [Id] int IDENTITY(1, 1) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [TotalBeds] int NOT NULL,
        [OccupiedBeds] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [HospitalId] int NULL,
        CONSTRAINT [PK_Wards] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT N'Created table Wards';
END;
GO

/* ============================================================
   2. Columns added since a table was first created (older databases)
   ============================================================ */

-- AccountApprovalRequests
EXEC #EnsureColumn N'AccountApprovalRequests', N'Id', N'ALTER TABLE [dbo].[AccountApprovalRequests] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'AccountApprovalRequests', N'RequestedUserId', N'ALTER TABLE [dbo].[AccountApprovalRequests] ADD [RequestedUserId] {{UserId}} NOT NULL CONSTRAINT [DF_AccountApprovalRequests_RequestedUserId] DEFAULT (N'''');';
EXEC #EnsureColumn N'AccountApprovalRequests', N'RequestedRole', N'ALTER TABLE [dbo].[AccountApprovalRequests] ADD [RequestedRole] nvarchar(100) NOT NULL CONSTRAINT [DF_AccountApprovalRequests_RequestedRole] DEFAULT (N'''');';
EXEC #EnsureColumn N'AccountApprovalRequests', N'Status', N'ALTER TABLE [dbo].[AccountApprovalRequests] ADD [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_AccountApprovalRequests_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'AccountApprovalRequests', N'RequestedAtUtc', N'ALTER TABLE [dbo].[AccountApprovalRequests] ADD [RequestedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_AccountApprovalRequests_RequestedAtUtc] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'AccountApprovalRequests', N'Notes', N'ALTER TABLE [dbo].[AccountApprovalRequests] ADD [Notes] nvarchar(1000) NULL;';
EXEC #EnsureColumn N'AccountApprovalRequests', N'ApprovedByUserId', N'ALTER TABLE [dbo].[AccountApprovalRequests] ADD [ApprovedByUserId] nvarchar(128) NULL;';
EXEC #EnsureColumn N'AccountApprovalRequests', N'ApprovedAtUtc', N'ALTER TABLE [dbo].[AccountApprovalRequests] ADD [ApprovedAtUtc] datetime2 NULL;';
GO

-- AmbulanceDispatches
EXEC #EnsureColumn N'AmbulanceDispatches', N'Id', N'ALTER TABLE [dbo].[AmbulanceDispatches] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'AmbulanceDispatches', N'AmbulanceVehicleId', N'ALTER TABLE [dbo].[AmbulanceDispatches] ADD [AmbulanceVehicleId] int NOT NULL CONSTRAINT [DF_AmbulanceDispatches_AmbulanceVehicleId] DEFAULT (0);';
EXEC #EnsureColumn N'AmbulanceDispatches', N'PatientId', N'ALTER TABLE [dbo].[AmbulanceDispatches] ADD [PatientId] int NULL;';
EXEC #EnsureColumn N'AmbulanceDispatches', N'PatientName', N'ALTER TABLE [dbo].[AmbulanceDispatches] ADD [PatientName] nvarchar(max) NOT NULL CONSTRAINT [DF__Ambulance__Patie__5C6CB6D7] DEFAULT ('''');';
EXEC #EnsureColumn N'AmbulanceDispatches', N'PickupAddress', N'ALTER TABLE [dbo].[AmbulanceDispatches] ADD [PickupAddress] nvarchar(max) NOT NULL CONSTRAINT [DF__Ambulance__Picku__5D60DB10] DEFAULT ('''');';
EXEC #EnsureColumn N'AmbulanceDispatches', N'ContactNumber', N'ALTER TABLE [dbo].[AmbulanceDispatches] ADD [ContactNumber] nvarchar(max) NOT NULL CONSTRAINT [DF__Ambulance__Conta__5E54FF49] DEFAULT ('''');';
EXEC #EnsureColumn N'AmbulanceDispatches', N'Purpose', N'ALTER TABLE [dbo].[AmbulanceDispatches] ADD [Purpose] nvarchar(max) NOT NULL CONSTRAINT [DF__Ambulance__Purpo__5F492382] DEFAULT ('''');';
EXEC #EnsureColumn N'AmbulanceDispatches', N'DispatchTime', N'ALTER TABLE [dbo].[AmbulanceDispatches] ADD [DispatchTime] datetime2 NOT NULL CONSTRAINT [DF_AmbulanceDispatches_DispatchTime] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'AmbulanceDispatches', N'ReturnTime', N'ALTER TABLE [dbo].[AmbulanceDispatches] ADD [ReturnTime] datetime2 NULL;';
EXEC #EnsureColumn N'AmbulanceDispatches', N'Status', N'ALTER TABLE [dbo].[AmbulanceDispatches] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF__Ambulance__Statu__603D47BB] DEFAULT (''Dispatched'');';
EXEC #EnsureColumn N'AmbulanceDispatches', N'DistanceKm', N'ALTER TABLE [dbo].[AmbulanceDispatches] ADD [DistanceKm] decimal(18, 2) NULL;';
EXEC #EnsureColumn N'AmbulanceDispatches', N'Charges', N'ALTER TABLE [dbo].[AmbulanceDispatches] ADD [Charges] decimal(18, 2) NULL;';
EXEC #EnsureColumn N'AmbulanceDispatches', N'Notes', N'ALTER TABLE [dbo].[AmbulanceDispatches] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF__Ambulance__Notes__61316BF4] DEFAULT ('''');';
EXEC #EnsureColumn N'AmbulanceDispatches', N'CreatedDate', N'ALTER TABLE [dbo].[AmbulanceDispatches] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_AmbulanceDispatches_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- AmbulanceVehicles
EXEC #EnsureColumn N'AmbulanceVehicles', N'Id', N'ALTER TABLE [dbo].[AmbulanceVehicles] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'AmbulanceVehicles', N'VehicleNumber', N'ALTER TABLE [dbo].[AmbulanceVehicles] ADD [VehicleNumber] nvarchar(450) NOT NULL CONSTRAINT [DF_AmbulanceVehicles_VehicleNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'AmbulanceVehicles', N'DriverName', N'ALTER TABLE [dbo].[AmbulanceVehicles] ADD [DriverName] nvarchar(max) NOT NULL CONSTRAINT [DF__Ambulance__Drive__55BFB948] DEFAULT ('''');';
EXEC #EnsureColumn N'AmbulanceVehicles', N'DriverContact', N'ALTER TABLE [dbo].[AmbulanceVehicles] ADD [DriverContact] nvarchar(max) NOT NULL CONSTRAINT [DF__Ambulance__Drive__56B3DD81] DEFAULT ('''');';
EXEC #EnsureColumn N'AmbulanceVehicles', N'Model', N'ALTER TABLE [dbo].[AmbulanceVehicles] ADD [Model] nvarchar(max) NOT NULL CONSTRAINT [DF__Ambulance__Model__57A801BA] DEFAULT ('''');';
EXEC #EnsureColumn N'AmbulanceVehicles', N'Status', N'ALTER TABLE [dbo].[AmbulanceVehicles] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF__Ambulance__Statu__589C25F3] DEFAULT (''Available'');';
EXEC #EnsureColumn N'AmbulanceVehicles', N'Notes', N'ALTER TABLE [dbo].[AmbulanceVehicles] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF__Ambulance__Notes__59904A2C] DEFAULT ('''');';
EXEC #EnsureColumn N'AmbulanceVehicles', N'CreatedDate', N'ALTER TABLE [dbo].[AmbulanceVehicles] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_AmbulanceVehicles_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- Appointments
EXEC #EnsureColumn N'Appointments', N'Id', N'ALTER TABLE [dbo].[Appointments] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'Appointments', N'PatientId', N'ALTER TABLE [dbo].[Appointments] ADD [PatientId] int NOT NULL CONSTRAINT [DF_Appointments_PatientId] DEFAULT (0);';
EXEC #EnsureColumn N'Appointments', N'DoctorId', N'ALTER TABLE [dbo].[Appointments] ADD [DoctorId] int NOT NULL CONSTRAINT [DF_Appointments_DoctorId] DEFAULT (0);';
EXEC #EnsureColumn N'Appointments', N'StaffId', N'ALTER TABLE [dbo].[Appointments] ADD [StaffId] nvarchar(128) NULL;';
EXEC #EnsureColumn N'Appointments', N'AppointmentDate', N'ALTER TABLE [dbo].[Appointments] ADD [AppointmentDate] datetime2 NOT NULL CONSTRAINT [DF_Appointments_AppointmentDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'Appointments', N'AppointmentTime', N'ALTER TABLE [dbo].[Appointments] ADD [AppointmentTime] time NOT NULL CONSTRAINT [DF_Appointments_AppointmentTime] DEFAULT (''00:00'');';
EXEC #EnsureColumn N'Appointments', N'Status', N'ALTER TABLE [dbo].[Appointments] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_Appointments_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'Appointments', N'AppointmentType', N'ALTER TABLE [dbo].[Appointments] ADD [AppointmentType] nvarchar(max) NOT NULL CONSTRAINT [DF_Appointments_AppointmentType] DEFAULT (N'''');';
EXEC #EnsureColumn N'Appointments', N'Priority', N'ALTER TABLE [dbo].[Appointments] ADD [Priority] nvarchar(max) NOT NULL CONSTRAINT [DF_Appointments_Priority] DEFAULT (N'''');';
EXEC #EnsureColumn N'Appointments', N'Symptoms', N'ALTER TABLE [dbo].[Appointments] ADD [Symptoms] nvarchar(max) NOT NULL CONSTRAINT [DF_Appointments_Symptoms] DEFAULT (N'''');';
EXEC #EnsureColumn N'Appointments', N'Notes', N'ALTER TABLE [dbo].[Appointments] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF_Appointments_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'Appointments', N'CreatedDate', N'ALTER TABLE [dbo].[Appointments] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Appointments_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'Appointments', N'CreatedBy', N'ALTER TABLE [dbo].[Appointments] ADD [CreatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_Appointments_CreatedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'Appointments', N'UpdatedDate', N'ALTER TABLE [dbo].[Appointments] ADD [UpdatedDate] datetime2 NULL;';
EXEC #EnsureColumn N'Appointments', N'UpdatedBy', N'ALTER TABLE [dbo].[Appointments] ADD [UpdatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_Appointments_UpdatedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'Appointments', N'HospitalId', N'ALTER TABLE [dbo].[Appointments] ADD [HospitalId] int NULL;';
GO

-- AspNetRoleClaims
EXEC #EnsureColumn N'AspNetRoleClaims', N'Id', N'ALTER TABLE [dbo].[AspNetRoleClaims] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'AspNetRoleClaims', N'RoleId', N'ALTER TABLE [dbo].[AspNetRoleClaims] ADD [RoleId] {{RoleId}} NOT NULL CONSTRAINT [DF_AspNetRoleClaims_RoleId] DEFAULT (N'''');';
EXEC #EnsureColumn N'AspNetRoleClaims', N'ClaimType', N'ALTER TABLE [dbo].[AspNetRoleClaims] ADD [ClaimType] nvarchar(max) NULL;';
EXEC #EnsureColumn N'AspNetRoleClaims', N'ClaimValue', N'ALTER TABLE [dbo].[AspNetRoleClaims] ADD [ClaimValue] nvarchar(max) NULL;';
GO

-- AspNetRoles
EXEC #EnsureColumn N'AspNetRoles', N'Id', N'ALTER TABLE [dbo].[AspNetRoles] ADD [Id] nvarchar(128) NOT NULL CONSTRAINT [DF_AspNetRoles_Id] DEFAULT (N'''');';
EXEC #EnsureColumn N'AspNetRoles', N'Name', N'ALTER TABLE [dbo].[AspNetRoles] ADD [Name] nvarchar(256) NULL;';
EXEC #EnsureColumn N'AspNetRoles', N'NormalizedName', N'ALTER TABLE [dbo].[AspNetRoles] ADD [NormalizedName] nvarchar(256) NULL;';
EXEC #EnsureColumn N'AspNetRoles', N'ConcurrencyStamp', N'ALTER TABLE [dbo].[AspNetRoles] ADD [ConcurrencyStamp] nvarchar(max) NULL;';
GO

-- AspNetUserClaims
EXEC #EnsureColumn N'AspNetUserClaims', N'Id', N'ALTER TABLE [dbo].[AspNetUserClaims] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'AspNetUserClaims', N'UserId', N'ALTER TABLE [dbo].[AspNetUserClaims] ADD [UserId] {{UserId}} NOT NULL CONSTRAINT [DF_AspNetUserClaims_UserId] DEFAULT (N'''');';
EXEC #EnsureColumn N'AspNetUserClaims', N'ClaimType', N'ALTER TABLE [dbo].[AspNetUserClaims] ADD [ClaimType] nvarchar(max) NULL;';
EXEC #EnsureColumn N'AspNetUserClaims', N'ClaimValue', N'ALTER TABLE [dbo].[AspNetUserClaims] ADD [ClaimValue] nvarchar(max) NULL;';
GO

-- AspNetUserLogins
EXEC #EnsureColumn N'AspNetUserLogins', N'LoginProvider', N'ALTER TABLE [dbo].[AspNetUserLogins] ADD [LoginProvider] nvarchar(128) NOT NULL CONSTRAINT [DF_AspNetUserLogins_LoginProvider] DEFAULT (N'''');';
EXEC #EnsureColumn N'AspNetUserLogins', N'ProviderKey', N'ALTER TABLE [dbo].[AspNetUserLogins] ADD [ProviderKey] nvarchar(128) NOT NULL CONSTRAINT [DF_AspNetUserLogins_ProviderKey] DEFAULT (N'''');';
EXEC #EnsureColumn N'AspNetUserLogins', N'ProviderDisplayName', N'ALTER TABLE [dbo].[AspNetUserLogins] ADD [ProviderDisplayName] nvarchar(max) NULL;';
EXEC #EnsureColumn N'AspNetUserLogins', N'UserId', N'ALTER TABLE [dbo].[AspNetUserLogins] ADD [UserId] {{UserId}} NOT NULL CONSTRAINT [DF_AspNetUserLogins_UserId] DEFAULT (N'''');';
GO

-- AspNetUserRoles
EXEC #EnsureColumn N'AspNetUserRoles', N'UserId', N'ALTER TABLE [dbo].[AspNetUserRoles] ADD [UserId] {{UserId}} NOT NULL CONSTRAINT [DF_AspNetUserRoles_UserId] DEFAULT (N'''');';
EXEC #EnsureColumn N'AspNetUserRoles', N'RoleId', N'ALTER TABLE [dbo].[AspNetUserRoles] ADD [RoleId] {{RoleId}} NOT NULL CONSTRAINT [DF_AspNetUserRoles_RoleId] DEFAULT (N'''');';
GO

-- AspNetUsers
EXEC #EnsureColumn N'AspNetUsers', N'Id', N'ALTER TABLE [dbo].[AspNetUsers] ADD [Id] nvarchar(128) NOT NULL CONSTRAINT [DF_AspNetUsers_Id] DEFAULT (N'''');';
EXEC #EnsureColumn N'AspNetUsers', N'EmployeeId', N'ALTER TABLE [dbo].[AspNetUsers] ADD [EmployeeId] nvarchar(max) NOT NULL CONSTRAINT [DF_AspNetUsers_EmployeeId] DEFAULT (N'''');';
EXEC #EnsureColumn N'AspNetUsers', N'FirstName', N'ALTER TABLE [dbo].[AspNetUsers] ADD [FirstName] nvarchar(max) NOT NULL CONSTRAINT [DF_AspNetUsers_FirstName] DEFAULT (N'''');';
EXEC #EnsureColumn N'AspNetUsers', N'LastName', N'ALTER TABLE [dbo].[AspNetUsers] ADD [LastName] nvarchar(max) NOT NULL CONSTRAINT [DF_AspNetUsers_LastName] DEFAULT (N'''');';
EXEC #EnsureColumn N'AspNetUsers', N'IsActive', N'ALTER TABLE [dbo].[AspNetUsers] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_AspNetUsers_IsActive] DEFAULT (0);';
EXEC #EnsureColumn N'AspNetUsers', N'CreatedDate', N'ALTER TABLE [dbo].[AspNetUsers] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_AspNetUsers_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'AspNetUsers', N'FirstLoginDate', N'ALTER TABLE [dbo].[AspNetUsers] ADD [FirstLoginDate] datetime2 NULL;';
EXEC #EnsureColumn N'AspNetUsers', N'LastLoginDate', N'ALTER TABLE [dbo].[AspNetUsers] ADD [LastLoginDate] datetime2 NULL;';
EXEC #EnsureColumn N'AspNetUsers', N'ProfileImage', N'ALTER TABLE [dbo].[AspNetUsers] ADD [ProfileImage] nvarchar(max) NULL;';
EXEC #EnsureColumn N'AspNetUsers', N'MFAEnabled', N'ALTER TABLE [dbo].[AspNetUsers] ADD [MFAEnabled] bit NOT NULL CONSTRAINT [DF__AspNetUse__MFAEn__398D8EEE] DEFAULT ((0));';
EXEC #EnsureColumn N'AspNetUsers', N'MFASecretKey', N'ALTER TABLE [dbo].[AspNetUsers] ADD [MFASecretKey] nvarchar(max) NULL;';
EXEC #EnsureColumn N'AspNetUsers', N'MFATempSecret', N'ALTER TABLE [dbo].[AspNetUsers] ADD [MFATempSecret] nvarchar(max) NULL;';
EXEC #EnsureColumn N'AspNetUsers', N'MFARecoveryCodes', N'ALTER TABLE [dbo].[AspNetUsers] ADD [MFARecoveryCodes] nvarchar(max) NULL;';
EXEC #EnsureColumn N'AspNetUsers', N'UserName', N'ALTER TABLE [dbo].[AspNetUsers] ADD [UserName] nvarchar(256) NOT NULL CONSTRAINT [DF_AspNetUsers_UserName] DEFAULT (N'''');';
EXEC #EnsureColumn N'AspNetUsers', N'NormalizedUserName', N'ALTER TABLE [dbo].[AspNetUsers] ADD [NormalizedUserName] nvarchar(256) NOT NULL CONSTRAINT [DF_AspNetUsers_NormalizedUserName] DEFAULT (N'''');';
EXEC #EnsureColumn N'AspNetUsers', N'Email', N'ALTER TABLE [dbo].[AspNetUsers] ADD [Email] nvarchar(256) NULL;';
EXEC #EnsureColumn N'AspNetUsers', N'NormalizedEmail', N'ALTER TABLE [dbo].[AspNetUsers] ADD [NormalizedEmail] nvarchar(256) NULL;';
EXEC #EnsureColumn N'AspNetUsers', N'EmailConfirmed', N'ALTER TABLE [dbo].[AspNetUsers] ADD [EmailConfirmed] bit NOT NULL CONSTRAINT [DF_AspNetUsers_EmailConfirmed] DEFAULT (0);';
EXEC #EnsureColumn N'AspNetUsers', N'PasswordHash', N'ALTER TABLE [dbo].[AspNetUsers] ADD [PasswordHash] nvarchar(max) NULL;';
EXEC #EnsureColumn N'AspNetUsers', N'SecurityStamp', N'ALTER TABLE [dbo].[AspNetUsers] ADD [SecurityStamp] nvarchar(max) NULL;';
EXEC #EnsureColumn N'AspNetUsers', N'ConcurrencyStamp', N'ALTER TABLE [dbo].[AspNetUsers] ADD [ConcurrencyStamp] nvarchar(max) NULL;';
EXEC #EnsureColumn N'AspNetUsers', N'PhoneNumber', N'ALTER TABLE [dbo].[AspNetUsers] ADD [PhoneNumber] nvarchar(max) NULL;';
EXEC #EnsureColumn N'AspNetUsers', N'PhoneNumberConfirmed', N'ALTER TABLE [dbo].[AspNetUsers] ADD [PhoneNumberConfirmed] bit NOT NULL CONSTRAINT [DF_AspNetUsers_PhoneNumberConfirmed] DEFAULT (0);';
EXEC #EnsureColumn N'AspNetUsers', N'TwoFactorEnabled', N'ALTER TABLE [dbo].[AspNetUsers] ADD [TwoFactorEnabled] bit NOT NULL CONSTRAINT [DF_AspNetUsers_TwoFactorEnabled] DEFAULT (0);';
EXEC #EnsureColumn N'AspNetUsers', N'LockoutEnd', N'ALTER TABLE [dbo].[AspNetUsers] ADD [LockoutEnd] datetimeoffset NULL;';
EXEC #EnsureColumn N'AspNetUsers', N'LockoutEnabled', N'ALTER TABLE [dbo].[AspNetUsers] ADD [LockoutEnabled] bit NOT NULL CONSTRAINT [DF_AspNetUsers_LockoutEnabled] DEFAULT (0);';
EXEC #EnsureColumn N'AspNetUsers', N'AccessFailedCount', N'ALTER TABLE [dbo].[AspNetUsers] ADD [AccessFailedCount] int NOT NULL CONSTRAINT [DF_AspNetUsers_AccessFailedCount] DEFAULT (0);';
EXEC #EnsureColumn N'AspNetUsers', N'Gender', N'ALTER TABLE [dbo].[AspNetUsers] ADD [Gender] nvarchar(20) NULL;';
EXEC #EnsureColumn N'AspNetUsers', N'DateOfBirth', N'ALTER TABLE [dbo].[AspNetUsers] ADD [DateOfBirth] datetime2 NULL;';
EXEC #EnsureColumn N'AspNetUsers', N'Address', N'ALTER TABLE [dbo].[AspNetUsers] ADD [Address] nvarchar(300) NULL;';
EXEC #EnsureColumn N'AspNetUsers', N'City', N'ALTER TABLE [dbo].[AspNetUsers] ADD [City] nvarchar(100) NULL;';
EXEC #EnsureColumn N'AspNetUsers', N'EmergencyContactName', N'ALTER TABLE [dbo].[AspNetUsers] ADD [EmergencyContactName] nvarchar(150) NULL;';
EXEC #EnsureColumn N'AspNetUsers', N'EmergencyContactPhone', N'ALTER TABLE [dbo].[AspNetUsers] ADD [EmergencyContactPhone] nvarchar(30) NULL;';
EXEC #EnsureColumn N'AspNetUsers', N'About', N'ALTER TABLE [dbo].[AspNetUsers] ADD [About] nvarchar(1000) NULL;';
GO

-- AspNetUserTokens
EXEC #EnsureColumn N'AspNetUserTokens', N'UserId', N'ALTER TABLE [dbo].[AspNetUserTokens] ADD [UserId] {{UserId}} NOT NULL CONSTRAINT [DF_AspNetUserTokens_UserId] DEFAULT (N'''');';
EXEC #EnsureColumn N'AspNetUserTokens', N'LoginProvider', N'ALTER TABLE [dbo].[AspNetUserTokens] ADD [LoginProvider] nvarchar(128) NOT NULL CONSTRAINT [DF_AspNetUserTokens_LoginProvider] DEFAULT (N'''');';
EXEC #EnsureColumn N'AspNetUserTokens', N'Name', N'ALTER TABLE [dbo].[AspNetUserTokens] ADD [Name] nvarchar(128) NOT NULL CONSTRAINT [DF_AspNetUserTokens_Name] DEFAULT (N'''');';
EXEC #EnsureColumn N'AspNetUserTokens', N'Value', N'ALTER TABLE [dbo].[AspNetUserTokens] ADD [Value] nvarchar(max) NULL;';
GO

-- AuditFindings
EXEC #EnsureColumn N'AuditFindings', N'Id', N'ALTER TABLE [dbo].[AuditFindings] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'AuditFindings', N'AuditId', N'ALTER TABLE [dbo].[AuditFindings] ADD [AuditId] int NOT NULL CONSTRAINT [DF_AuditFindings_AuditId] DEFAULT (0);';
EXEC #EnsureColumn N'AuditFindings', N'FindingType', N'ALTER TABLE [dbo].[AuditFindings] ADD [FindingType] nvarchar(40) NOT NULL CONSTRAINT [DF_AuditFindings_FindingType] DEFAULT (N'''');';
EXEC #EnsureColumn N'AuditFindings', N'Clause', N'ALTER TABLE [dbo].[AuditFindings] ADD [Clause] nvarchar(50) NOT NULL CONSTRAINT [DF_AuditFindings_Clause] DEFAULT (N'''');';
EXEC #EnsureColumn N'AuditFindings', N'Description', N'ALTER TABLE [dbo].[AuditFindings] ADD [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_AuditFindings_Description] DEFAULT (N'''');';
EXEC #EnsureColumn N'AuditFindings', N'Status', N'ALTER TABLE [dbo].[AuditFindings] ADD [Status] nvarchar(20) NOT NULL CONSTRAINT [DF_AuditFindings_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'AuditFindings', N'CreatedAt', N'ALTER TABLE [dbo].[AuditFindings] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_AuditFindings_CreatedAt] DEFAULT (''1900-01-01'');';
GO

-- AuditLogs
EXEC #EnsureColumn N'AuditLogs', N'Id', N'ALTER TABLE [dbo].[AuditLogs] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'AuditLogs', N'UserId', N'ALTER TABLE [dbo].[AuditLogs] ADD [UserId] {{UserId}} NULL;';
EXEC #EnsureColumn N'AuditLogs', N'Action', N'ALTER TABLE [dbo].[AuditLogs] ADD [Action] nvarchar(max) NOT NULL CONSTRAINT [DF_AuditLogs_Action] DEFAULT (N'''');';
EXEC #EnsureColumn N'AuditLogs', N'EntityName', N'ALTER TABLE [dbo].[AuditLogs] ADD [EntityName] nvarchar(max) NOT NULL CONSTRAINT [DF_AuditLogs_EntityName] DEFAULT (N'''');';
EXEC #EnsureColumn N'AuditLogs', N'EntityId', N'ALTER TABLE [dbo].[AuditLogs] ADD [EntityId] nvarchar(max) NOT NULL CONSTRAINT [DF_AuditLogs_EntityId] DEFAULT (N'''');';
EXEC #EnsureColumn N'AuditLogs', N'OldValues', N'ALTER TABLE [dbo].[AuditLogs] ADD [OldValues] nvarchar(max) NOT NULL CONSTRAINT [DF_AuditLogs_OldValues] DEFAULT (N'''');';
EXEC #EnsureColumn N'AuditLogs', N'NewValues', N'ALTER TABLE [dbo].[AuditLogs] ADD [NewValues] nvarchar(max) NOT NULL CONSTRAINT [DF_AuditLogs_NewValues] DEFAULT (N'''');';
EXEC #EnsureColumn N'AuditLogs', N'Details', N'ALTER TABLE [dbo].[AuditLogs] ADD [Details] nvarchar(max) NOT NULL CONSTRAINT [DF_AuditLogs_Details] DEFAULT (N'''');';
EXEC #EnsureColumn N'AuditLogs', N'IpAddress', N'ALTER TABLE [dbo].[AuditLogs] ADD [IpAddress] nvarchar(max) NOT NULL CONSTRAINT [DF_AuditLogs_IpAddress] DEFAULT (N'''');';
EXEC #EnsureColumn N'AuditLogs', N'UserAgent', N'ALTER TABLE [dbo].[AuditLogs] ADD [UserAgent] nvarchar(max) NOT NULL CONSTRAINT [DF_AuditLogs_UserAgent] DEFAULT (N'''');';
EXEC #EnsureColumn N'AuditLogs', N'Timestamp', N'ALTER TABLE [dbo].[AuditLogs] ADD [Timestamp] datetime2 NOT NULL CONSTRAINT [DF_AuditLogs_Timestamp] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'AuditLogs', N'SessionId', N'ALTER TABLE [dbo].[AuditLogs] ADD [SessionId] nvarchar(max) NOT NULL CONSTRAINT [DF_AuditLogs_SessionId] DEFAULT (N'''');';
GO

-- Beds
EXEC #EnsureColumn N'Beds', N'Id', N'ALTER TABLE [dbo].[Beds] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'Beds', N'WardId', N'ALTER TABLE [dbo].[Beds] ADD [WardId] int NOT NULL CONSTRAINT [DF_Beds_WardId] DEFAULT (0);';
EXEC #EnsureColumn N'Beds', N'BedNumber', N'ALTER TABLE [dbo].[Beds] ADD [BedNumber] nvarchar(max) NOT NULL CONSTRAINT [DF_Beds_BedNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'Beds', N'BedType', N'ALTER TABLE [dbo].[Beds] ADD [BedType] nvarchar(max) NOT NULL CONSTRAINT [DF_Beds_BedType] DEFAULT (N'''');';
EXEC #EnsureColumn N'Beds', N'DailyCharges', N'ALTER TABLE [dbo].[Beds] ADD [DailyCharges] decimal(18, 2) NOT NULL CONSTRAINT [DF_Beds_DailyCharges] DEFAULT (0);';
EXEC #EnsureColumn N'Beds', N'Status', N'ALTER TABLE [dbo].[Beds] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_Beds_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'Beds', N'IsActive', N'ALTER TABLE [dbo].[Beds] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_Beds_IsActive] DEFAULT (0);';
EXEC #EnsureColumn N'Beds', N'CreatedDate', N'ALTER TABLE [dbo].[Beds] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Beds_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'Beds', N'PatientId', N'ALTER TABLE [dbo].[Beds] ADD [PatientId] int NULL;';
EXEC #EnsureColumn N'Beds', N'IsIsolation', N'ALTER TABLE [dbo].[Beds] ADD [IsIsolation] bit NOT NULL CONSTRAINT [DF_Beds_IsIsolation] DEFAULT ((0));';
EXEC #EnsureColumn N'Beds', N'RequiresAdminApproval', N'ALTER TABLE [dbo].[Beds] ADD [RequiresAdminApproval] bit NOT NULL CONSTRAINT [DF_Beds_RequiresAdminApproval] DEFAULT ((0));';
EXEC #EnsureColumn N'Beds', N'LastUpdated', N'ALTER TABLE [dbo].[Beds] ADD [LastUpdated] datetime2 NULL;';
EXEC #EnsureColumn N'Beds', N'Block', N'ALTER TABLE [dbo].[Beds] ADD [Block] nvarchar(100) NOT NULL CONSTRAINT [DF_Beds_Block] DEFAULT ('''');';
EXEC #EnsureColumn N'Beds', N'Floor', N'ALTER TABLE [dbo].[Beds] ADD [Floor] nvarchar(50) NOT NULL CONSTRAINT [DF_Beds_Floor] DEFAULT ('''');';
EXEC #EnsureColumn N'Beds', N'RoomNumber', N'ALTER TABLE [dbo].[Beds] ADD [RoomNumber] nvarchar(50) NOT NULL CONSTRAINT [DF_Beds_RoomNumber] DEFAULT ('''');';
GO

-- BillItems
EXEC #EnsureColumn N'BillItems', N'Id', N'ALTER TABLE [dbo].[BillItems] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'BillItems', N'BillId', N'ALTER TABLE [dbo].[BillItems] ADD [BillId] int NOT NULL CONSTRAINT [DF_BillItems_BillId] DEFAULT (0);';
EXEC #EnsureColumn N'BillItems', N'ItemName', N'ALTER TABLE [dbo].[BillItems] ADD [ItemName] nvarchar(max) NOT NULL CONSTRAINT [DF_BillItems_ItemName] DEFAULT (N'''');';
EXEC #EnsureColumn N'BillItems', N'ItemType', N'ALTER TABLE [dbo].[BillItems] ADD [ItemType] nvarchar(max) NOT NULL CONSTRAINT [DF_BillItems_ItemType] DEFAULT (N'''');';
EXEC #EnsureColumn N'BillItems', N'Quantity', N'ALTER TABLE [dbo].[BillItems] ADD [Quantity] decimal(18, 2) NOT NULL CONSTRAINT [DF_BillItems_Quantity] DEFAULT (0);';
EXEC #EnsureColumn N'BillItems', N'UnitPrice', N'ALTER TABLE [dbo].[BillItems] ADD [UnitPrice] decimal(18, 2) NOT NULL CONSTRAINT [DF_BillItems_UnitPrice] DEFAULT (0);';
EXEC #EnsureColumn N'BillItems', N'TotalPrice', N'ALTER TABLE [dbo].[BillItems] ADD [TotalPrice] decimal(18, 2) NOT NULL CONSTRAINT [DF_BillItems_TotalPrice] DEFAULT (0);';
EXEC #EnsureColumn N'BillItems', N'Amount', N'ALTER TABLE [dbo].[BillItems] ADD [Amount] decimal(18, 2) NOT NULL CONSTRAINT [DF_BillItems_Amount] DEFAULT (0);';
EXEC #EnsureColumn N'BillItems', N'Description', N'ALTER TABLE [dbo].[BillItems] ADD [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_BillItems_Description] DEFAULT (N'''');';
EXEC #EnsureColumn N'BillItems', N'CreatedDate', N'ALTER TABLE [dbo].[BillItems] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_BillItems_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- Bills
EXEC #EnsureColumn N'Bills', N'Id', N'ALTER TABLE [dbo].[Bills] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'Bills', N'BillNumber', N'ALTER TABLE [dbo].[Bills] ADD [BillNumber] nvarchar(max) NOT NULL CONSTRAINT [DF_Bills_BillNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'Bills', N'PatientId', N'ALTER TABLE [dbo].[Bills] ADD [PatientId] int NOT NULL CONSTRAINT [DF_Bills_PatientId] DEFAULT (0);';
EXEC #EnsureColumn N'Bills', N'AppointmentId', N'ALTER TABLE [dbo].[Bills] ADD [AppointmentId] int NULL;';
EXEC #EnsureColumn N'Bills', N'BillDate', N'ALTER TABLE [dbo].[Bills] ADD [BillDate] datetime2 NOT NULL CONSTRAINT [DF_Bills_BillDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'Bills', N'DueDate', N'ALTER TABLE [dbo].[Bills] ADD [DueDate] datetime2 NOT NULL CONSTRAINT [DF_Bills_DueDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'Bills', N'TotalAmount', N'ALTER TABLE [dbo].[Bills] ADD [TotalAmount] decimal(18, 2) NOT NULL CONSTRAINT [DF_Bills_TotalAmount] DEFAULT (0);';
EXEC #EnsureColumn N'Bills', N'PaidAmount', N'ALTER TABLE [dbo].[Bills] ADD [PaidAmount] decimal(18, 2) NOT NULL CONSTRAINT [DF_Bills_PaidAmount] DEFAULT (0);';
EXEC #EnsureColumn N'Bills', N'PendingAmount', N'ALTER TABLE [dbo].[Bills] ADD [PendingAmount] decimal(18, 2) NOT NULL CONSTRAINT [DF_Bills_PendingAmount] DEFAULT (0);';
EXEC #EnsureColumn N'Bills', N'Status', N'ALTER TABLE [dbo].[Bills] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_Bills_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'Bills', N'BillType', N'ALTER TABLE [dbo].[Bills] ADD [BillType] nvarchar(max) NOT NULL CONSTRAINT [DF_Bills_BillType] DEFAULT (N'''');';
EXEC #EnsureColumn N'Bills', N'Notes', N'ALTER TABLE [dbo].[Bills] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF_Bills_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'Bills', N'CreatedDate', N'ALTER TABLE [dbo].[Bills] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Bills_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'Bills', N'UpdatedDate', N'ALTER TABLE [dbo].[Bills] ADD [UpdatedDate] datetime2 NULL;';
EXEC #EnsureColumn N'Bills', N'CreatedBy', N'ALTER TABLE [dbo].[Bills] ADD [CreatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_Bills_CreatedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'Bills', N'HospitalId', N'ALTER TABLE [dbo].[Bills] ADD [HospitalId] int NULL;';
GO

-- BirthRecords
EXEC #EnsureColumn N'BirthRecords', N'Id', N'ALTER TABLE [dbo].[BirthRecords] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'BirthRecords', N'BabyName', N'ALTER TABLE [dbo].[BirthRecords] ADD [BabyName] nvarchar(max) NOT NULL CONSTRAINT [DF__BirthReco__BabyN__65F62111] DEFAULT ('''');';
EXEC #EnsureColumn N'BirthRecords', N'Gender', N'ALTER TABLE [dbo].[BirthRecords] ADD [Gender] nvarchar(max) NOT NULL CONSTRAINT [DF__BirthReco__Gende__66EA454A] DEFAULT ('''');';
EXEC #EnsureColumn N'BirthRecords', N'DateOfBirth', N'ALTER TABLE [dbo].[BirthRecords] ADD [DateOfBirth] datetime2 NOT NULL CONSTRAINT [DF_BirthRecords_DateOfBirth] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'BirthRecords', N'TimeOfBirth', N'ALTER TABLE [dbo].[BirthRecords] ADD [TimeOfBirth] nvarchar(max) NOT NULL CONSTRAINT [DF__BirthReco__TimeO__67DE6983] DEFAULT ('''');';
EXEC #EnsureColumn N'BirthRecords', N'WeightKg', N'ALTER TABLE [dbo].[BirthRecords] ADD [WeightKg] decimal(18, 2) NOT NULL CONSTRAINT [DF__BirthReco__Weigh__68D28DBC] DEFAULT ((0));';
EXEC #EnsureColumn N'BirthRecords', N'MotherName', N'ALTER TABLE [dbo].[BirthRecords] ADD [MotherName] nvarchar(max) NOT NULL CONSTRAINT [DF__BirthReco__Mothe__69C6B1F5] DEFAULT ('''');';
EXEC #EnsureColumn N'BirthRecords', N'FatherName', N'ALTER TABLE [dbo].[BirthRecords] ADD [FatherName] nvarchar(max) NOT NULL CONSTRAINT [DF__BirthReco__Fathe__6ABAD62E] DEFAULT ('''');';
EXEC #EnsureColumn N'BirthRecords', N'GuardianContact', N'ALTER TABLE [dbo].[BirthRecords] ADD [GuardianContact] nvarchar(max) NOT NULL CONSTRAINT [DF__BirthReco__Guard__6BAEFA67] DEFAULT ('''');';
EXEC #EnsureColumn N'BirthRecords', N'DeliveryType', N'ALTER TABLE [dbo].[BirthRecords] ADD [DeliveryType] nvarchar(max) NOT NULL CONSTRAINT [DF__BirthReco__Deliv__6CA31EA0] DEFAULT (''Normal'');';
EXEC #EnsureColumn N'BirthRecords', N'AttendingDoctorName', N'ALTER TABLE [dbo].[BirthRecords] ADD [AttendingDoctorName] nvarchar(max) NOT NULL CONSTRAINT [DF__BirthReco__Atten__6D9742D9] DEFAULT ('''');';
EXEC #EnsureColumn N'BirthRecords', N'PatientId', N'ALTER TABLE [dbo].[BirthRecords] ADD [PatientId] int NULL;';
EXEC #EnsureColumn N'BirthRecords', N'CertificateNumber', N'ALTER TABLE [dbo].[BirthRecords] ADD [CertificateNumber] nvarchar(max) NOT NULL CONSTRAINT [DF__BirthReco__Certi__6E8B6712] DEFAULT ('''');';
EXEC #EnsureColumn N'BirthRecords', N'CertificateIssued', N'ALTER TABLE [dbo].[BirthRecords] ADD [CertificateIssued] bit NOT NULL CONSTRAINT [DF__BirthReco__Certi__6F7F8B4B] DEFAULT ((0));';
EXEC #EnsureColumn N'BirthRecords', N'Notes', N'ALTER TABLE [dbo].[BirthRecords] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF__BirthReco__Notes__7073AF84] DEFAULT ('''');';
EXEC #EnsureColumn N'BirthRecords', N'CreatedDate', N'ALTER TABLE [dbo].[BirthRecords] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_BirthRecords_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- BloodInventories
EXEC #EnsureColumn N'BloodInventories', N'Id', N'ALTER TABLE [dbo].[BloodInventories] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'BloodInventories', N'BloodGroup', N'ALTER TABLE [dbo].[BloodInventories] ADD [BloodGroup] nvarchar(max) NOT NULL CONSTRAINT [DF_BloodInventories_BloodGroup] DEFAULT (N'''');';
EXEC #EnsureColumn N'BloodInventories', N'UnitsAvailable', N'ALTER TABLE [dbo].[BloodInventories] ADD [UnitsAvailable] int NOT NULL CONSTRAINT [DF_BloodInventories_UnitsAvailable] DEFAULT (0);';
EXEC #EnsureColumn N'BloodInventories', N'UnitsReserved', N'ALTER TABLE [dbo].[BloodInventories] ADD [UnitsReserved] int NOT NULL CONSTRAINT [DF_BloodInventories_UnitsReserved] DEFAULT (0);';
EXEC #EnsureColumn N'BloodInventories', N'MinimumLevel', N'ALTER TABLE [dbo].[BloodInventories] ADD [MinimumLevel] int NOT NULL CONSTRAINT [DF_BloodInventories_MinimumLevel] DEFAULT (0);';
EXEC #EnsureColumn N'BloodInventories', N'LastUpdatedDate', N'ALTER TABLE [dbo].[BloodInventories] ADD [LastUpdatedDate] datetime2 NOT NULL CONSTRAINT [DF_BloodInventories_LastUpdatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'BloodInventories', N'CreatedDate', N'ALTER TABLE [dbo].[BloodInventories] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_BloodInventories_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- BloodIssues
EXEC #EnsureColumn N'BloodIssues', N'Id', N'ALTER TABLE [dbo].[BloodIssues] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'BloodIssues', N'PatientId', N'ALTER TABLE [dbo].[BloodIssues] ADD [PatientId] int NOT NULL CONSTRAINT [DF_BloodIssues_PatientId] DEFAULT (0);';
EXEC #EnsureColumn N'BloodIssues', N'BloodGroup', N'ALTER TABLE [dbo].[BloodIssues] ADD [BloodGroup] nvarchar(max) NOT NULL CONSTRAINT [DF_BloodIssues_BloodGroup] DEFAULT (N'''');';
EXEC #EnsureColumn N'BloodIssues', N'UnitsIssued', N'ALTER TABLE [dbo].[BloodIssues] ADD [UnitsIssued] int NOT NULL CONSTRAINT [DF_BloodIssues_UnitsIssued] DEFAULT (0);';
EXEC #EnsureColumn N'BloodIssues', N'IssueDate', N'ALTER TABLE [dbo].[BloodIssues] ADD [IssueDate] datetime2 NOT NULL CONSTRAINT [DF_BloodIssues_IssueDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'BloodIssues', N'RequestedBy', N'ALTER TABLE [dbo].[BloodIssues] ADD [RequestedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_BloodIssues_RequestedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'BloodIssues', N'CrossMatchStatus', N'ALTER TABLE [dbo].[BloodIssues] ADD [CrossMatchStatus] nvarchar(max) NOT NULL CONSTRAINT [DF_BloodIssues_CrossMatchStatus] DEFAULT (N'''');';
EXEC #EnsureColumn N'BloodIssues', N'Notes', N'ALTER TABLE [dbo].[BloodIssues] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF_BloodIssues_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'BloodIssues', N'BillId', N'ALTER TABLE [dbo].[BloodIssues] ADD [BillId] int NULL;';
EXEC #EnsureColumn N'BloodIssues', N'CreatedDate', N'ALTER TABLE [dbo].[BloodIssues] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_BloodIssues_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- CapaActions
EXEC #EnsureColumn N'CapaActions', N'Id', N'ALTER TABLE [dbo].[CapaActions] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'CapaActions', N'HospitalId', N'ALTER TABLE [dbo].[CapaActions] ADD [HospitalId] int NULL;';
EXEC #EnsureColumn N'CapaActions', N'IncidentId', N'ALTER TABLE [dbo].[CapaActions] ADD [IncidentId] int NULL;';
EXEC #EnsureColumn N'CapaActions', N'AuditFindingId', N'ALTER TABLE [dbo].[CapaActions] ADD [AuditFindingId] int NULL;';
EXEC #EnsureColumn N'CapaActions', N'ActionType', N'ALTER TABLE [dbo].[CapaActions] ADD [ActionType] nvarchar(20) NOT NULL CONSTRAINT [DF_CapaActions_ActionType] DEFAULT (N'''');';
EXEC #EnsureColumn N'CapaActions', N'Description', N'ALTER TABLE [dbo].[CapaActions] ADD [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_CapaActions_Description] DEFAULT (N'''');';
EXEC #EnsureColumn N'CapaActions', N'OwnerUserId', N'ALTER TABLE [dbo].[CapaActions] ADD [OwnerUserId] nvarchar(max) NOT NULL CONSTRAINT [DF_CapaActions_OwnerUserId] DEFAULT (N'''');';
EXEC #EnsureColumn N'CapaActions', N'OwnerName', N'ALTER TABLE [dbo].[CapaActions] ADD [OwnerName] nvarchar(max) NOT NULL CONSTRAINT [DF_CapaActions_OwnerName] DEFAULT (N'''');';
EXEC #EnsureColumn N'CapaActions', N'DueDate', N'ALTER TABLE [dbo].[CapaActions] ADD [DueDate] datetime2 NOT NULL CONSTRAINT [DF_CapaActions_DueDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'CapaActions', N'Status', N'ALTER TABLE [dbo].[CapaActions] ADD [Status] nvarchar(20) NOT NULL CONSTRAINT [DF_CapaActions_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'CapaActions', N'CompletionNotes', N'ALTER TABLE [dbo].[CapaActions] ADD [CompletionNotes] nvarchar(max) NOT NULL CONSTRAINT [DF_CapaActions_CompletionNotes] DEFAULT (N'''');';
EXEC #EnsureColumn N'CapaActions', N'CompletedAt', N'ALTER TABLE [dbo].[CapaActions] ADD [CompletedAt] datetime2 NULL;';
EXEC #EnsureColumn N'CapaActions', N'VerifiedBy', N'ALTER TABLE [dbo].[CapaActions] ADD [VerifiedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_CapaActions_VerifiedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'CapaActions', N'VerifiedAt', N'ALTER TABLE [dbo].[CapaActions] ADD [VerifiedAt] datetime2 NULL;';
EXEC #EnsureColumn N'CapaActions', N'EffectivenessNotes', N'ALTER TABLE [dbo].[CapaActions] ADD [EffectivenessNotes] nvarchar(max) NOT NULL CONSTRAINT [DF_CapaActions_EffectivenessNotes] DEFAULT (N'''');';
EXEC #EnsureColumn N'CapaActions', N'CreatedAt', N'ALTER TABLE [dbo].[CapaActions] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_CapaActions_CreatedAt] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'CapaActions', N'CreatedBy', N'ALTER TABLE [dbo].[CapaActions] ADD [CreatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_CapaActions_CreatedBy] DEFAULT (N'''');';
GO

-- CertificateRecords
EXEC #EnsureColumn N'CertificateRecords', N'Id', N'ALTER TABLE [dbo].[CertificateRecords] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'CertificateRecords', N'StaffId', N'ALTER TABLE [dbo].[CertificateRecords] ADD [StaffId] {{StaffId}} NOT NULL CONSTRAINT [DF_CertificateRecords_StaffId] DEFAULT (N'''');';
EXEC #EnsureColumn N'CertificateRecords', N'CertificateType', N'ALTER TABLE [dbo].[CertificateRecords] ADD [CertificateType] nvarchar(max) NOT NULL CONSTRAINT [DF_CertificateRecords_CertificateType] DEFAULT (N'''');';
EXEC #EnsureColumn N'CertificateRecords', N'Title', N'ALTER TABLE [dbo].[CertificateRecords] ADD [Title] nvarchar(max) NOT NULL CONSTRAINT [DF_CertificateRecords_Title] DEFAULT (N'''');';
EXEC #EnsureColumn N'CertificateRecords', N'Content', N'ALTER TABLE [dbo].[CertificateRecords] ADD [Content] nvarchar(max) NOT NULL CONSTRAINT [DF_CertificateRecords_Content] DEFAULT (N'''');';
EXEC #EnsureColumn N'CertificateRecords', N'IssueDate', N'ALTER TABLE [dbo].[CertificateRecords] ADD [IssueDate] datetime2 NOT NULL CONSTRAINT [DF_CertificateRecords_IssueDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'CertificateRecords', N'GeneratedBy', N'ALTER TABLE [dbo].[CertificateRecords] ADD [GeneratedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_CertificateRecords_GeneratedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'CertificateRecords', N'CreatedDate', N'ALTER TABLE [dbo].[CertificateRecords] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_CertificateRecords_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- ChatbotConsentAudits
EXEC #EnsureColumn N'ChatbotConsentAudits', N'Id', N'ALTER TABLE [dbo].[ChatbotConsentAudits] ADD [Id] bigint IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'ChatbotConsentAudits', N'ConsentId', N'ALTER TABLE [dbo].[ChatbotConsentAudits] ADD [ConsentId] bigint NULL;';
EXEC #EnsureColumn N'ChatbotConsentAudits', N'UserId', N'ALTER TABLE [dbo].[ChatbotConsentAudits] ADD [UserId] nvarchar(450) NULL;';
EXEC #EnsureColumn N'ChatbotConsentAudits', N'Action', N'ALTER TABLE [dbo].[ChatbotConsentAudits] ADD [Action] nvarchar(30) NOT NULL CONSTRAINT [DF_ChatbotConsentAudits_Action] DEFAULT (N'''');';
EXEC #EnsureColumn N'ChatbotConsentAudits', N'ConsentVersion', N'ALTER TABLE [dbo].[ChatbotConsentAudits] ADD [ConsentVersion] nvarchar(10) NOT NULL CONSTRAINT [DF_ChatbotConsentAudits_ConsentVersion] DEFAULT (N'''');';
EXEC #EnsureColumn N'ChatbotConsentAudits', N'ConsentStateJson', N'ALTER TABLE [dbo].[ChatbotConsentAudits] ADD [ConsentStateJson] nvarchar(max) NOT NULL CONSTRAINT [DF_ChatbotConsentAudits_ConsentStateJson] DEFAULT (N'''');';
EXEC #EnsureColumn N'ChatbotConsentAudits', N'UserIpAddress', N'ALTER TABLE [dbo].[ChatbotConsentAudits] ADD [UserIpAddress] nvarchar(500) NULL;';
EXEC #EnsureColumn N'ChatbotConsentAudits', N'UserAgent', N'ALTER TABLE [dbo].[ChatbotConsentAudits] ADD [UserAgent] nvarchar(500) NULL;';
EXEC #EnsureColumn N'ChatbotConsentAudits', N'Notes', N'ALTER TABLE [dbo].[ChatbotConsentAudits] ADD [Notes] nvarchar(500) NULL;';
EXEC #EnsureColumn N'ChatbotConsentAudits', N'CreatedAtUtc', N'ALTER TABLE [dbo].[ChatbotConsentAudits] ADD [CreatedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_ChatbotConsentAudits_CreatedAtUtc] DEFAULT (''1900-01-01'');';
GO

-- ChatbotConsents
EXEC #EnsureColumn N'ChatbotConsents', N'Id', N'ALTER TABLE [dbo].[ChatbotConsents] ADD [Id] bigint IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'ChatbotConsents', N'UserId', N'ALTER TABLE [dbo].[ChatbotConsents] ADD [UserId] nvarchar(450) NULL;';
EXEC #EnsureColumn N'ChatbotConsents', N'ConsentVersion', N'ALTER TABLE [dbo].[ChatbotConsents] ADD [ConsentVersion] nvarchar(10) NOT NULL CONSTRAINT [DF_ChatbotConsents_ConsentVersion] DEFAULT (N'''');';
EXEC #EnsureColumn N'ChatbotConsents', N'ConsentedToAiProcessing', N'ALTER TABLE [dbo].[ChatbotConsents] ADD [ConsentedToAiProcessing] bit NOT NULL CONSTRAINT [DF_ChatbotConsents_ConsentedToAiProcessing] DEFAULT (0);';
EXEC #EnsureColumn N'ChatbotConsents', N'ConsentedToDataRetention', N'ALTER TABLE [dbo].[ChatbotConsents] ADD [ConsentedToDataRetention] bit NOT NULL CONSTRAINT [DF_ChatbotConsents_ConsentedToDataRetention] DEFAULT (0);';
EXEC #EnsureColumn N'ChatbotConsents', N'ConsentedToThirdPartyProcessing', N'ALTER TABLE [dbo].[ChatbotConsents] ADD [ConsentedToThirdPartyProcessing] bit NOT NULL CONSTRAINT [DF_ChatbotConsents_ConsentedToThirdPartyProcessing] DEFAULT (0);';
EXEC #EnsureColumn N'ChatbotConsents', N'UserIpAddress', N'ALTER TABLE [dbo].[ChatbotConsents] ADD [UserIpAddress] nvarchar(50) NULL;';
EXEC #EnsureColumn N'ChatbotConsents', N'UserAgent', N'ALTER TABLE [dbo].[ChatbotConsents] ADD [UserAgent] nvarchar(500) NULL;';
EXEC #EnsureColumn N'ChatbotConsents', N'ConsentedAtUtc', N'ALTER TABLE [dbo].[ChatbotConsents] ADD [ConsentedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_ChatbotConsents_ConsentedAtUtc] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'ChatbotConsents', N'RevokedAtUtc', N'ALTER TABLE [dbo].[ChatbotConsents] ADD [RevokedAtUtc] datetime2 NULL;';
EXEC #EnsureColumn N'ChatbotConsents', N'IsActive', N'ALTER TABLE [dbo].[ChatbotConsents] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_ChatbotConsents_IsActive] DEFAULT (0);';
EXEC #EnsureColumn N'ChatbotConsents', N'RevocationReason', N'ALTER TABLE [dbo].[ChatbotConsents] ADD [RevocationReason] nvarchar(500) NULL;';
GO

-- ChatbotEventLogs
EXEC #EnsureColumn N'ChatbotEventLogs', N'Id', N'ALTER TABLE [dbo].[ChatbotEventLogs] ADD [Id] bigint IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'ChatbotEventLogs', N'SessionId', N'ALTER TABLE [dbo].[ChatbotEventLogs] ADD [SessionId] nvarchar(64) NULL;';
EXEC #EnsureColumn N'ChatbotEventLogs', N'MessageId', N'ALTER TABLE [dbo].[ChatbotEventLogs] ADD [MessageId] bigint NULL;';
EXEC #EnsureColumn N'ChatbotEventLogs', N'EventType', N'ALTER TABLE [dbo].[ChatbotEventLogs] ADD [EventType] nvarchar(50) NOT NULL CONSTRAINT [DF_ChatbotEventLogs_EventType] DEFAULT (N'''');';
EXEC #EnsureColumn N'ChatbotEventLogs', N'Severity', N'ALTER TABLE [dbo].[ChatbotEventLogs] ADD [Severity] nvarchar(20) NOT NULL CONSTRAINT [DF_ChatbotEventLogs_Severity] DEFAULT (N'''');';
EXEC #EnsureColumn N'ChatbotEventLogs', N'Details', N'ALTER TABLE [dbo].[ChatbotEventLogs] ADD [Details] nvarchar(2000) NOT NULL CONSTRAINT [DF_ChatbotEventLogs_Details] DEFAULT (N'''');';
EXEC #EnsureColumn N'ChatbotEventLogs', N'CreatedAtUtc', N'ALTER TABLE [dbo].[ChatbotEventLogs] ADD [CreatedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_ChatbotEventLogs_CreatedAtUtc] DEFAULT (''1900-01-01'');';
GO

-- ChatEscalations
EXEC #EnsureColumn N'ChatEscalations', N'Id', N'ALTER TABLE [dbo].[ChatEscalations] ADD [Id] bigint IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'ChatEscalations', N'SessionId', N'ALTER TABLE [dbo].[ChatEscalations] ADD [SessionId] nvarchar(64) NOT NULL CONSTRAINT [DF_ChatEscalations_SessionId] DEFAULT (N'''');';
EXEC #EnsureColumn N'ChatEscalations', N'MessageId', N'ALTER TABLE [dbo].[ChatEscalations] ADD [MessageId] bigint NULL;';
EXEC #EnsureColumn N'ChatEscalations', N'UserId', N'ALTER TABLE [dbo].[ChatEscalations] ADD [UserId] nvarchar(128) NULL;';
EXEC #EnsureColumn N'ChatEscalations', N'EscalationType', N'ALTER TABLE [dbo].[ChatEscalations] ADD [EscalationType] nvarchar(30) NOT NULL CONSTRAINT [DF_ChatEscalations_EscalationType] DEFAULT (N'''');';
EXEC #EnsureColumn N'ChatEscalations', N'Reason', N'ALTER TABLE [dbo].[ChatEscalations] ADD [Reason] nvarchar(1200) NOT NULL CONSTRAINT [DF_ChatEscalations_Reason] DEFAULT (N'''');';
EXEC #EnsureColumn N'ChatEscalations', N'Status', N'ALTER TABLE [dbo].[ChatEscalations] ADD [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_ChatEscalations_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'ChatEscalations', N'TargetContact', N'ALTER TABLE [dbo].[ChatEscalations] ADD [TargetContact] nvarchar(200) NULL;';
EXEC #EnsureColumn N'ChatEscalations', N'ResolvedByUserId', N'ALTER TABLE [dbo].[ChatEscalations] ADD [ResolvedByUserId] nvarchar(128) NULL;';
EXEC #EnsureColumn N'ChatEscalations', N'CreatedAtUtc', N'ALTER TABLE [dbo].[ChatEscalations] ADD [CreatedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_ChatEscalations_CreatedAtUtc] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'ChatEscalations', N'ResolvedAtUtc', N'ALTER TABLE [dbo].[ChatEscalations] ADD [ResolvedAtUtc] datetime2 NULL;';
GO

-- ChatFeedback
EXEC #EnsureColumn N'ChatFeedback', N'Id', N'ALTER TABLE [dbo].[ChatFeedback] ADD [Id] bigint IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'ChatFeedback', N'SessionId', N'ALTER TABLE [dbo].[ChatFeedback] ADD [SessionId] nvarchar(64) NOT NULL CONSTRAINT [DF_ChatFeedback_SessionId] DEFAULT (N'''');';
EXEC #EnsureColumn N'ChatFeedback', N'MessageId', N'ALTER TABLE [dbo].[ChatFeedback] ADD [MessageId] bigint NULL;';
EXEC #EnsureColumn N'ChatFeedback', N'FeedbackType', N'ALTER TABLE [dbo].[ChatFeedback] ADD [FeedbackType] nvarchar(20) NOT NULL CONSTRAINT [DF_ChatFeedback_FeedbackType] DEFAULT (N'''');';
EXEC #EnsureColumn N'ChatFeedback', N'Comment', N'ALTER TABLE [dbo].[ChatFeedback] ADD [Comment] nvarchar(1000) NULL;';
EXEC #EnsureColumn N'ChatFeedback', N'CreatedAtUtc', N'ALTER TABLE [dbo].[ChatFeedback] ADD [CreatedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_ChatFeedback_CreatedAtUtc] DEFAULT (''1900-01-01'');';
GO

-- ChatMessages
EXEC #EnsureColumn N'ChatMessages', N'Id', N'ALTER TABLE [dbo].[ChatMessages] ADD [Id] bigint IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'ChatMessages', N'SessionId', N'ALTER TABLE [dbo].[ChatMessages] ADD [SessionId] nvarchar(64) NOT NULL CONSTRAINT [DF_ChatMessages_SessionId] DEFAULT (N'''');';
EXEC #EnsureColumn N'ChatMessages', N'SenderType', N'ALTER TABLE [dbo].[ChatMessages] ADD [SenderType] nvarchar(20) NOT NULL CONSTRAINT [DF_ChatMessages_SenderType] DEFAULT (N'''');';
EXEC #EnsureColumn N'ChatMessages', N'Content', N'ALTER TABLE [dbo].[ChatMessages] ADD [Content] nvarchar(max) NOT NULL CONSTRAINT [DF_ChatMessages_Content] DEFAULT (N'''');';
EXEC #EnsureColumn N'ChatMessages', N'CreatedAtUtc', N'ALTER TABLE [dbo].[ChatMessages] ADD [CreatedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_ChatMessages_CreatedAtUtc] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'ChatMessages', N'ModerationStatus', N'ALTER TABLE [dbo].[ChatMessages] ADD [ModerationStatus] nvarchar(30) NOT NULL CONSTRAINT [DF_ChatMessages_ModerationStatus] DEFAULT (N'''');';
EXEC #EnsureColumn N'ChatMessages', N'TokenCount', N'ALTER TABLE [dbo].[ChatMessages] ADD [TokenCount] int NOT NULL CONSTRAINT [DF_ChatMessages_TokenCount] DEFAULT (0);';
EXEC #EnsureColumn N'ChatMessages', N'Category', N'ALTER TABLE [dbo].[ChatMessages] ADD [Category] nvarchar(30) NOT NULL CONSTRAINT [DF_ChatMessages_Category] DEFAULT (N'''');';
GO

-- ChatSessions
EXEC #EnsureColumn N'ChatSessions', N'Id', N'ALTER TABLE [dbo].[ChatSessions] ADD [Id] nvarchar(64) NOT NULL CONSTRAINT [DF_ChatSessions_Id] DEFAULT (N'''');';
EXEC #EnsureColumn N'ChatSessions', N'UserId', N'ALTER TABLE [dbo].[ChatSessions] ADD [UserId] nvarchar(128) NULL;';
EXEC #EnsureColumn N'ChatSessions', N'UserRole', N'ALTER TABLE [dbo].[ChatSessions] ADD [UserRole] nvarchar(40) NOT NULL CONSTRAINT [DF_ChatSessions_UserRole] DEFAULT (N'''');';
EXEC #EnsureColumn N'ChatSessions', N'StartedAtUtc', N'ALTER TABLE [dbo].[ChatSessions] ADD [StartedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_ChatSessions_StartedAtUtc] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'ChatSessions', N'EndedAtUtc', N'ALTER TABLE [dbo].[ChatSessions] ADD [EndedAtUtc] datetime2 NULL;';
EXEC #EnsureColumn N'ChatSessions', N'Status', N'ALTER TABLE [dbo].[ChatSessions] ADD [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_ChatSessions_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'ChatSessions', N'Channel', N'ALTER TABLE [dbo].[ChatSessions] ADD [Channel] nvarchar(20) NOT NULL CONSTRAINT [DF_ChatSessions_Channel] DEFAULT (N'''');';
EXEC #EnsureColumn N'ChatSessions', N'IsEscalated', N'ALTER TABLE [dbo].[ChatSessions] ADD [IsEscalated] bit NOT NULL CONSTRAINT [DF_ChatSessions_IsEscalated] DEFAULT (0);';
EXEC #EnsureColumn N'ChatSessions', N'IsUnresolved', N'ALTER TABLE [dbo].[ChatSessions] ADD [IsUnresolved] bit NOT NULL CONSTRAINT [DF_ChatSessions_IsUnresolved] DEFAULT (0);';
EXEC #EnsureColumn N'ChatSessions', N'PreferredLanguage', N'ALTER TABLE [dbo].[ChatSessions] ADD [PreferredLanguage] nvarchar(12) NOT NULL CONSTRAINT [DF_ChatSessions_PreferredLanguage] DEFAULT (N'''');';
GO

-- CmsMenuItems
EXEC #EnsureColumn N'CmsMenuItems', N'Id', N'ALTER TABLE [dbo].[CmsMenuItems] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'CmsMenuItems', N'Label', N'ALTER TABLE [dbo].[CmsMenuItems] ADD [Label] nvarchar(100) NOT NULL CONSTRAINT [DF_CmsMenuItems_Label] DEFAULT (N'''');';
EXEC #EnsureColumn N'CmsMenuItems', N'Url', N'ALTER TABLE [dbo].[CmsMenuItems] ADD [Url] nvarchar(300) NULL;';
EXEC #EnsureColumn N'CmsMenuItems', N'CmsPageId', N'ALTER TABLE [dbo].[CmsMenuItems] ADD [CmsPageId] int NULL;';
EXEC #EnsureColumn N'CmsMenuItems', N'SortOrder', N'ALTER TABLE [dbo].[CmsMenuItems] ADD [SortOrder] int NOT NULL CONSTRAINT [DF_CmsMenuItems_SortOrder] DEFAULT (0);';
EXEC #EnsureColumn N'CmsMenuItems', N'IsActive', N'ALTER TABLE [dbo].[CmsMenuItems] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_CmsMenuItems_IsActive] DEFAULT (0);';
EXEC #EnsureColumn N'CmsMenuItems', N'OpenInNewTab', N'ALTER TABLE [dbo].[CmsMenuItems] ADD [OpenInNewTab] bit NOT NULL CONSTRAINT [DF_CmsMenuItems_OpenInNewTab] DEFAULT (0);';
GO

-- CmsNotices
EXEC #EnsureColumn N'CmsNotices', N'Id', N'ALTER TABLE [dbo].[CmsNotices] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'CmsNotices', N'Title', N'ALTER TABLE [dbo].[CmsNotices] ADD [Title] nvarchar(200) NOT NULL CONSTRAINT [DF_CmsNotices_Title] DEFAULT (N'''');';
EXEC #EnsureColumn N'CmsNotices', N'Slug', N'ALTER TABLE [dbo].[CmsNotices] ADD [Slug] nvarchar(200) NOT NULL CONSTRAINT [DF_CmsNotices_Slug] DEFAULT (N'''');';
EXEC #EnsureColumn N'CmsNotices', N'Summary', N'ALTER TABLE [dbo].[CmsNotices] ADD [Summary] nvarchar(max) NULL;';
EXEC #EnsureColumn N'CmsNotices', N'Content', N'ALTER TABLE [dbo].[CmsNotices] ADD [Content] nvarchar(max) NULL;';
EXEC #EnsureColumn N'CmsNotices', N'Type', N'ALTER TABLE [dbo].[CmsNotices] ADD [Type] nvarchar(20) NOT NULL CONSTRAINT [DF_CmsNotices_Type] DEFAULT (N'''');';
EXEC #EnsureColumn N'CmsNotices', N'FeaturedImage', N'ALTER TABLE [dbo].[CmsNotices] ADD [FeaturedImage] nvarchar(300) NULL;';
EXEC #EnsureColumn N'CmsNotices', N'IsActive', N'ALTER TABLE [dbo].[CmsNotices] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_CmsNotices_IsActive] DEFAULT (0);';
EXEC #EnsureColumn N'CmsNotices', N'PublishedAt', N'ALTER TABLE [dbo].[CmsNotices] ADD [PublishedAt] datetime2 NULL;';
EXEC #EnsureColumn N'CmsNotices', N'CreatedAt', N'ALTER TABLE [dbo].[CmsNotices] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_CmsNotices_CreatedAt] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'CmsNotices', N'UpdatedAt', N'ALTER TABLE [dbo].[CmsNotices] ADD [UpdatedAt] datetime2 NULL;';
EXEC #EnsureColumn N'CmsNotices', N'CreatedBy', N'ALTER TABLE [dbo].[CmsNotices] ADD [CreatedBy] nvarchar(100) NULL;';
GO

-- CmsPages
EXEC #EnsureColumn N'CmsPages', N'Id', N'ALTER TABLE [dbo].[CmsPages] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'CmsPages', N'Title', N'ALTER TABLE [dbo].[CmsPages] ADD [Title] nvarchar(200) NOT NULL CONSTRAINT [DF_CmsPages_Title] DEFAULT (N'''');';
EXEC #EnsureColumn N'CmsPages', N'Slug', N'ALTER TABLE [dbo].[CmsPages] ADD [Slug] nvarchar(200) NOT NULL CONSTRAINT [DF_CmsPages_Slug] DEFAULT (N'''');';
EXEC #EnsureColumn N'CmsPages', N'Content', N'ALTER TABLE [dbo].[CmsPages] ADD [Content] nvarchar(max) NULL;';
EXEC #EnsureColumn N'CmsPages', N'MetaDescription', N'ALTER TABLE [dbo].[CmsPages] ADD [MetaDescription] nvarchar(300) NULL;';
EXEC #EnsureColumn N'CmsPages', N'Status', N'ALTER TABLE [dbo].[CmsPages] ADD [Status] nvarchar(20) NOT NULL CONSTRAINT [DF_CmsPages_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'CmsPages', N'ShowInMenu', N'ALTER TABLE [dbo].[CmsPages] ADD [ShowInMenu] bit NOT NULL CONSTRAINT [DF_CmsPages_ShowInMenu] DEFAULT (0);';
EXEC #EnsureColumn N'CmsPages', N'SortOrder', N'ALTER TABLE [dbo].[CmsPages] ADD [SortOrder] int NOT NULL CONSTRAINT [DF_CmsPages_SortOrder] DEFAULT (0);';
EXEC #EnsureColumn N'CmsPages', N'CreatedAt', N'ALTER TABLE [dbo].[CmsPages] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_CmsPages_CreatedAt] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'CmsPages', N'UpdatedAt', N'ALTER TABLE [dbo].[CmsPages] ADD [UpdatedAt] datetime2 NULL;';
EXEC #EnsureColumn N'CmsPages', N'CreatedBy', N'ALTER TABLE [dbo].[CmsPages] ADD [CreatedBy] nvarchar(100) NULL;';
EXEC #EnsureColumn N'CmsPages', N'UpdatedBy', N'ALTER TABLE [dbo].[CmsPages] ADD [UpdatedBy] nvarchar(100) NULL;';
EXEC #EnsureColumn N'CmsPages', N'FeaturedImage', N'ALTER TABLE [dbo].[CmsPages] ADD [FeaturedImage] nvarchar(300) NULL;';
EXEC #EnsureColumn N'CmsPages', N'FontFamily', N'ALTER TABLE [dbo].[CmsPages] ADD [FontFamily] nvarchar(100) NULL;';
EXEC #EnsureColumn N'CmsPages', N'FontSizePx', N'ALTER TABLE [dbo].[CmsPages] ADD [FontSizePx] int NULL;';
GO

-- ComplaintRecords
EXEC #EnsureColumn N'ComplaintRecords', N'Id', N'ALTER TABLE [dbo].[ComplaintRecords] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'ComplaintRecords', N'ComplainantName', N'ALTER TABLE [dbo].[ComplaintRecords] ADD [ComplainantName] nvarchar(max) NOT NULL CONSTRAINT [DF_ComplaintRecords_ComplainantName] DEFAULT (N'''');';
EXEC #EnsureColumn N'ComplaintRecords', N'Phone', N'ALTER TABLE [dbo].[ComplaintRecords] ADD [Phone] nvarchar(max) NOT NULL CONSTRAINT [DF_ComplaintRecords_Phone] DEFAULT (N'''');';
EXEC #EnsureColumn N'ComplaintRecords', N'Subject', N'ALTER TABLE [dbo].[ComplaintRecords] ADD [Subject] nvarchar(max) NOT NULL CONSTRAINT [DF_ComplaintRecords_Subject] DEFAULT (N'''');';
EXEC #EnsureColumn N'ComplaintRecords', N'Description', N'ALTER TABLE [dbo].[ComplaintRecords] ADD [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_ComplaintRecords_Description] DEFAULT (N'''');';
EXEC #EnsureColumn N'ComplaintRecords', N'Status', N'ALTER TABLE [dbo].[ComplaintRecords] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_ComplaintRecords_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'ComplaintRecords', N'ResolutionNotes', N'ALTER TABLE [dbo].[ComplaintRecords] ADD [ResolutionNotes] nvarchar(max) NOT NULL CONSTRAINT [DF_ComplaintRecords_ResolutionNotes] DEFAULT (N'''');';
EXEC #EnsureColumn N'ComplaintRecords', N'CreatedDate', N'ALTER TABLE [dbo].[ComplaintRecords] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_ComplaintRecords_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'ComplaintRecords', N'ResolvedDate', N'ALTER TABLE [dbo].[ComplaintRecords] ADD [ResolvedDate] datetime2 NULL;';
GO

-- ControlledDocuments
EXEC #EnsureColumn N'ControlledDocuments', N'Id', N'ALTER TABLE [dbo].[ControlledDocuments] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'ControlledDocuments', N'DocumentNumber', N'ALTER TABLE [dbo].[ControlledDocuments] ADD [DocumentNumber] nvarchar(30) NOT NULL CONSTRAINT [DF_ControlledDocuments_DocumentNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'ControlledDocuments', N'Title', N'ALTER TABLE [dbo].[ControlledDocuments] ADD [Title] nvarchar(200) NOT NULL CONSTRAINT [DF_ControlledDocuments_Title] DEFAULT (N'''');';
EXEC #EnsureColumn N'ControlledDocuments', N'Category', N'ALTER TABLE [dbo].[ControlledDocuments] ADD [Category] nvarchar(50) NOT NULL CONSTRAINT [DF_ControlledDocuments_Category] DEFAULT (N'''');';
EXEC #EnsureColumn N'ControlledDocuments', N'Department', N'ALTER TABLE [dbo].[ControlledDocuments] ADD [Department] nvarchar(100) NOT NULL CONSTRAINT [DF_ControlledDocuments_Department] DEFAULT (N'''');';
EXEC #EnsureColumn N'ControlledDocuments', N'ReviewIntervalMonths', N'ALTER TABLE [dbo].[ControlledDocuments] ADD [ReviewIntervalMonths] int NOT NULL CONSTRAINT [DF_ControlledDocuments_ReviewIntervalMonths] DEFAULT (0);';
EXEC #EnsureColumn N'ControlledDocuments', N'Status', N'ALTER TABLE [dbo].[ControlledDocuments] ADD [Status] nvarchar(20) NOT NULL CONSTRAINT [DF_ControlledDocuments_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'ControlledDocuments', N'NextReviewDate', N'ALTER TABLE [dbo].[ControlledDocuments] ADD [NextReviewDate] datetime2 NULL;';
EXEC #EnsureColumn N'ControlledDocuments', N'CreatedAt', N'ALTER TABLE [dbo].[ControlledDocuments] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_ControlledDocuments_CreatedAt] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'ControlledDocuments', N'CreatedBy', N'ALTER TABLE [dbo].[ControlledDocuments] ADD [CreatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_ControlledDocuments_CreatedBy] DEFAULT (N'''');';
GO

-- DatabaseBackups
EXEC #EnsureColumn N'DatabaseBackups', N'Id', N'ALTER TABLE [dbo].[DatabaseBackups] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'DatabaseBackups', N'DatabaseName', N'ALTER TABLE [dbo].[DatabaseBackups] ADD [DatabaseName] nvarchar(max) NOT NULL CONSTRAINT [DF_DatabaseBackups_DatabaseName] DEFAULT (N'''');';
EXEC #EnsureColumn N'DatabaseBackups', N'FileName', N'ALTER TABLE [dbo].[DatabaseBackups] ADD [FileName] nvarchar(max) NOT NULL CONSTRAINT [DF_DatabaseBackups_FileName] DEFAULT (N'''');';
EXEC #EnsureColumn N'DatabaseBackups', N'FilePath', N'ALTER TABLE [dbo].[DatabaseBackups] ADD [FilePath] nvarchar(max) NOT NULL CONSTRAINT [DF_DatabaseBackups_FilePath] DEFAULT (N'''');';
EXEC #EnsureColumn N'DatabaseBackups', N'SizeBytes', N'ALTER TABLE [dbo].[DatabaseBackups] ADD [SizeBytes] bigint NULL;';
EXEC #EnsureColumn N'DatabaseBackups', N'StartedAt', N'ALTER TABLE [dbo].[DatabaseBackups] ADD [StartedAt] datetime2 NOT NULL CONSTRAINT [DF_DatabaseBackups_StartedAt] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'DatabaseBackups', N'CompletedAt', N'ALTER TABLE [dbo].[DatabaseBackups] ADD [CompletedAt] datetime2 NULL;';
EXEC #EnsureColumn N'DatabaseBackups', N'Status', N'ALTER TABLE [dbo].[DatabaseBackups] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_DatabaseBackups_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'DatabaseBackups', N'Verified', N'ALTER TABLE [dbo].[DatabaseBackups] ADD [Verified] bit NOT NULL CONSTRAINT [DF_DatabaseBackups_Verified] DEFAULT (0);';
EXEC #EnsureColumn N'DatabaseBackups', N'Message', N'ALTER TABLE [dbo].[DatabaseBackups] ADD [Message] nvarchar(max) NOT NULL CONSTRAINT [DF_DatabaseBackups_Message] DEFAULT (N'''');';
EXEC #EnsureColumn N'DatabaseBackups', N'CreatedBy', N'ALTER TABLE [dbo].[DatabaseBackups] ADD [CreatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_DatabaseBackups_CreatedBy] DEFAULT (N'''');';
GO

-- DeathRecords
EXEC #EnsureColumn N'DeathRecords', N'Id', N'ALTER TABLE [dbo].[DeathRecords] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'DeathRecords', N'PatientId', N'ALTER TABLE [dbo].[DeathRecords] ADD [PatientId] int NULL;';
EXEC #EnsureColumn N'DeathRecords', N'PatientName', N'ALTER TABLE [dbo].[DeathRecords] ADD [PatientName] nvarchar(max) NOT NULL CONSTRAINT [DF__DeathReco__Patie__74444068] DEFAULT ('''');';
EXEC #EnsureColumn N'DeathRecords', N'Gender', N'ALTER TABLE [dbo].[DeathRecords] ADD [Gender] nvarchar(max) NOT NULL CONSTRAINT [DF__DeathReco__Gende__753864A1] DEFAULT ('''');';
EXEC #EnsureColumn N'DeathRecords', N'DateOfDeath', N'ALTER TABLE [dbo].[DeathRecords] ADD [DateOfDeath] datetime2 NOT NULL CONSTRAINT [DF_DeathRecords_DateOfDeath] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'DeathRecords', N'TimeOfDeath', N'ALTER TABLE [dbo].[DeathRecords] ADD [TimeOfDeath] nvarchar(max) NOT NULL CONSTRAINT [DF__DeathReco__TimeO__762C88DA] DEFAULT ('''');';
EXEC #EnsureColumn N'DeathRecords', N'CauseOfDeath', N'ALTER TABLE [dbo].[DeathRecords] ADD [CauseOfDeath] nvarchar(max) NOT NULL CONSTRAINT [DF__DeathReco__Cause__7720AD13] DEFAULT ('''');';
EXEC #EnsureColumn N'DeathRecords', N'AttendingDoctorName', N'ALTER TABLE [dbo].[DeathRecords] ADD [AttendingDoctorName] nvarchar(max) NOT NULL CONSTRAINT [DF__DeathReco__Atten__7814D14C] DEFAULT ('''');';
EXEC #EnsureColumn N'DeathRecords', N'NextOfKinName', N'ALTER TABLE [dbo].[DeathRecords] ADD [NextOfKinName] nvarchar(max) NOT NULL CONSTRAINT [DF__DeathReco__NextO__7908F585] DEFAULT ('''');';
EXEC #EnsureColumn N'DeathRecords', N'NextOfKinContact', N'ALTER TABLE [dbo].[DeathRecords] ADD [NextOfKinContact] nvarchar(max) NOT NULL CONSTRAINT [DF__DeathReco__NextO__79FD19BE] DEFAULT ('''');';
EXEC #EnsureColumn N'DeathRecords', N'CertificateNumber', N'ALTER TABLE [dbo].[DeathRecords] ADD [CertificateNumber] nvarchar(max) NOT NULL CONSTRAINT [DF__DeathReco__Certi__7AF13DF7] DEFAULT ('''');';
EXEC #EnsureColumn N'DeathRecords', N'CertificateIssued', N'ALTER TABLE [dbo].[DeathRecords] ADD [CertificateIssued] bit NOT NULL CONSTRAINT [DF__DeathReco__Certi__7BE56230] DEFAULT ((0));';
EXEC #EnsureColumn N'DeathRecords', N'Notes', N'ALTER TABLE [dbo].[DeathRecords] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF__DeathReco__Notes__7CD98669] DEFAULT ('''');';
EXEC #EnsureColumn N'DeathRecords', N'CreatedDate', N'ALTER TABLE [dbo].[DeathRecords] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_DeathRecords_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- Departments
EXEC #EnsureColumn N'Departments', N'Id', N'ALTER TABLE [dbo].[Departments] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'Departments', N'Name', N'ALTER TABLE [dbo].[Departments] ADD [Name] nvarchar(max) NOT NULL CONSTRAINT [DF_Departments_Name] DEFAULT (N'''');';
EXEC #EnsureColumn N'Departments', N'Description', N'ALTER TABLE [dbo].[Departments] ADD [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_Departments_Description] DEFAULT (N'''');';
EXEC #EnsureColumn N'Departments', N'HeadOfDepartment', N'ALTER TABLE [dbo].[Departments] ADD [HeadOfDepartment] nvarchar(max) NOT NULL CONSTRAINT [DF_Departments_HeadOfDepartment] DEFAULT (N'''');';
EXEC #EnsureColumn N'Departments', N'IsActive', N'ALTER TABLE [dbo].[Departments] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_Departments_IsActive] DEFAULT (0);';
EXEC #EnsureColumn N'Departments', N'CreatedDate', N'ALTER TABLE [dbo].[Departments] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Departments_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- DischargeSummaries
EXEC #EnsureColumn N'DischargeSummaries', N'Id', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'DischargeSummaries', N'HospitalId', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [HospitalId] int NULL;';
EXEC #EnsureColumn N'DischargeSummaries', N'IPDAdmissionId', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [IPDAdmissionId] int NOT NULL CONSTRAINT [DF_DischargeSummaries_IPDAdmissionId] DEFAULT (0);';
EXEC #EnsureColumn N'DischargeSummaries', N'PatientId', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [PatientId] int NOT NULL CONSTRAINT [DF_DischargeSummaries_PatientId] DEFAULT (0);';
EXEC #EnsureColumn N'DischargeSummaries', N'AdmissionDate', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [AdmissionDate] datetime2 NOT NULL CONSTRAINT [DF_DischargeSummaries_AdmissionDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'DischargeSummaries', N'DischargeDate', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [DischargeDate] datetime2 NOT NULL CONSTRAINT [DF_DischargeSummaries_DischargeDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'DischargeSummaries', N'ConditionAtDischarge', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [ConditionAtDischarge] nvarchar(60) NOT NULL CONSTRAINT [DF_DischargeSummaries_ConditionAtDischarge] DEFAULT (N'''');';
EXEC #EnsureColumn N'DischargeSummaries', N'ReasonForAdmission', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [ReasonForAdmission] nvarchar(1000) NOT NULL CONSTRAINT [DF_DischargeSummaries_ReasonForAdmission] DEFAULT (N'''');';
EXEC #EnsureColumn N'DischargeSummaries', N'FinalDiagnosis', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [FinalDiagnosis] nvarchar(1000) NOT NULL CONSTRAINT [DF_DischargeSummaries_FinalDiagnosis] DEFAULT (N'''');';
EXEC #EnsureColumn N'DischargeSummaries', N'HospitalCourse', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [HospitalCourse] nvarchar(4000) NOT NULL CONSTRAINT [DF_DischargeSummaries_HospitalCourse] DEFAULT (N'''');';
EXEC #EnsureColumn N'DischargeSummaries', N'ProceduresPerformed', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [ProceduresPerformed] nvarchar(2000) NOT NULL CONSTRAINT [DF_DischargeSummaries_ProceduresPerformed] DEFAULT (N'''');';
EXEC #EnsureColumn N'DischargeSummaries', N'InvestigationsSummary', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [InvestigationsSummary] nvarchar(2000) NOT NULL CONSTRAINT [DF_DischargeSummaries_InvestigationsSummary] DEFAULT (N'''');';
EXEC #EnsureColumn N'DischargeSummaries', N'DischargeMedications', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [DischargeMedications] nvarchar(2000) NOT NULL CONSTRAINT [DF_DischargeSummaries_DischargeMedications] DEFAULT (N'''');';
EXEC #EnsureColumn N'DischargeSummaries', N'AdviceOnDischarge', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [AdviceOnDischarge] nvarchar(2000) NOT NULL CONSTRAINT [DF_DischargeSummaries_AdviceOnDischarge] DEFAULT (N'''');';
EXEC #EnsureColumn N'DischargeSummaries', N'FollowUpInstructions', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [FollowUpInstructions] nvarchar(1000) NOT NULL CONSTRAINT [DF_DischargeSummaries_FollowUpInstructions] DEFAULT (N'''');';
EXEC #EnsureColumn N'DischargeSummaries', N'FollowUpDate', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [FollowUpDate] datetime2 NULL;';
EXEC #EnsureColumn N'DischargeSummaries', N'AttendingDoctor', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [AttendingDoctor] nvarchar(150) NOT NULL CONSTRAINT [DF_DischargeSummaries_AttendingDoctor] DEFAULT (N'''');';
EXEC #EnsureColumn N'DischargeSummaries', N'Status', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [Status] nvarchar(20) NOT NULL CONSTRAINT [DF_DischargeSummaries_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'DischargeSummaries', N'CreatedByUserId', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [CreatedByUserId] nvarchar(450) NULL;';
EXEC #EnsureColumn N'DischargeSummaries', N'CreatedByName', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [CreatedByName] nvarchar(150) NOT NULL CONSTRAINT [DF_DischargeSummaries_CreatedByName] DEFAULT (N'''');';
EXEC #EnsureColumn N'DischargeSummaries', N'CreatedAt', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_DischargeSummaries_CreatedAt] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'DischargeSummaries', N'UpdatedAt', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [UpdatedAt] datetime2 NULL;';
EXEC #EnsureColumn N'DischargeSummaries', N'CompletedByName', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [CompletedByName] nvarchar(150) NULL;';
EXEC #EnsureColumn N'DischargeSummaries', N'CompletedAt', N'ALTER TABLE [dbo].[DischargeSummaries] ADD [CompletedAt] datetime2 NULL;';
GO

-- DispatchReceiveRecords
EXEC #EnsureColumn N'DispatchReceiveRecords', N'Id', N'ALTER TABLE [dbo].[DispatchReceiveRecords] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'DispatchReceiveRecords', N'RecordType', N'ALTER TABLE [dbo].[DispatchReceiveRecords] ADD [RecordType] nvarchar(max) NOT NULL CONSTRAINT [DF_DispatchReceiveRecords_RecordType] DEFAULT (N'''');';
EXEC #EnsureColumn N'DispatchReceiveRecords', N'ReferenceNumber', N'ALTER TABLE [dbo].[DispatchReceiveRecords] ADD [ReferenceNumber] nvarchar(max) NOT NULL CONSTRAINT [DF_DispatchReceiveRecords_ReferenceNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'DispatchReceiveRecords', N'PartyName', N'ALTER TABLE [dbo].[DispatchReceiveRecords] ADD [PartyName] nvarchar(max) NOT NULL CONSTRAINT [DF_DispatchReceiveRecords_PartyName] DEFAULT (N'''');';
EXEC #EnsureColumn N'DispatchReceiveRecords', N'ContactNumber', N'ALTER TABLE [dbo].[DispatchReceiveRecords] ADD [ContactNumber] nvarchar(max) NOT NULL CONSTRAINT [DF_DispatchReceiveRecords_ContactNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'DispatchReceiveRecords', N'ContentSummary', N'ALTER TABLE [dbo].[DispatchReceiveRecords] ADD [ContentSummary] nvarchar(max) NOT NULL CONSTRAINT [DF_DispatchReceiveRecords_ContentSummary] DEFAULT (N'''');';
EXEC #EnsureColumn N'DispatchReceiveRecords', N'RecordDate', N'ALTER TABLE [dbo].[DispatchReceiveRecords] ADD [RecordDate] datetime2 NOT NULL CONSTRAINT [DF_DispatchReceiveRecords_RecordDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'DispatchReceiveRecords', N'Status', N'ALTER TABLE [dbo].[DispatchReceiveRecords] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_DispatchReceiveRecords_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'DispatchReceiveRecords', N'Notes', N'ALTER TABLE [dbo].[DispatchReceiveRecords] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF_DispatchReceiveRecords_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'DispatchReceiveRecords', N'CreatedDate', N'ALTER TABLE [dbo].[DispatchReceiveRecords] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_DispatchReceiveRecords_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- Doctors
EXEC #EnsureColumn N'Doctors', N'Id', N'ALTER TABLE [dbo].[Doctors] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'Doctors', N'EmployeeId', N'ALTER TABLE [dbo].[Doctors] ADD [EmployeeId] nvarchar(max) NOT NULL CONSTRAINT [DF_Doctors_EmployeeId] DEFAULT (N'''');';
EXEC #EnsureColumn N'Doctors', N'FirstName', N'ALTER TABLE [dbo].[Doctors] ADD [FirstName] nvarchar(max) NOT NULL CONSTRAINT [DF_Doctors_FirstName] DEFAULT (N'''');';
EXEC #EnsureColumn N'Doctors', N'LastName', N'ALTER TABLE [dbo].[Doctors] ADD [LastName] nvarchar(max) NOT NULL CONSTRAINT [DF_Doctors_LastName] DEFAULT (N'''');';
EXEC #EnsureColumn N'Doctors', N'Specialization', N'ALTER TABLE [dbo].[Doctors] ADD [Specialization] nvarchar(max) NOT NULL CONSTRAINT [DF_Doctors_Specialization] DEFAULT (N'''');';
EXEC #EnsureColumn N'Doctors', N'LicenseNumber', N'ALTER TABLE [dbo].[Doctors] ADD [LicenseNumber] nvarchar(max) NOT NULL CONSTRAINT [DF_Doctors_LicenseNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'Doctors', N'Phone', N'ALTER TABLE [dbo].[Doctors] ADD [Phone] nvarchar(max) NOT NULL CONSTRAINT [DF_Doctors_Phone] DEFAULT (N'''');';
EXEC #EnsureColumn N'Doctors', N'Email', N'ALTER TABLE [dbo].[Doctors] ADD [Email] nvarchar(max) NOT NULL CONSTRAINT [DF_Doctors_Email] DEFAULT (N'''');';
EXEC #EnsureColumn N'Doctors', N'DepartmentId', N'ALTER TABLE [dbo].[Doctors] ADD [DepartmentId] int NOT NULL CONSTRAINT [DF_Doctors_DepartmentId] DEFAULT (0);';
EXEC #EnsureColumn N'Doctors', N'IsActive', N'ALTER TABLE [dbo].[Doctors] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_Doctors_IsActive] DEFAULT (0);';
EXEC #EnsureColumn N'Doctors', N'CreatedDate', N'ALTER TABLE [dbo].[Doctors] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Doctors_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- DoctorShifts
EXEC #EnsureColumn N'DoctorShifts', N'Id', N'ALTER TABLE [dbo].[DoctorShifts] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'DoctorShifts', N'DoctorId', N'ALTER TABLE [dbo].[DoctorShifts] ADD [DoctorId] int NOT NULL CONSTRAINT [DF_DoctorShifts_DoctorId] DEFAULT (0);';
EXEC #EnsureColumn N'DoctorShifts', N'DayOfWeek', N'ALTER TABLE [dbo].[DoctorShifts] ADD [DayOfWeek] int NOT NULL CONSTRAINT [DF_DoctorShifts_DayOfWeek] DEFAULT (0);';
EXEC #EnsureColumn N'DoctorShifts', N'StartTime', N'ALTER TABLE [dbo].[DoctorShifts] ADD [StartTime] time NOT NULL CONSTRAINT [DF_DoctorShifts_StartTime] DEFAULT (''00:00'');';
EXEC #EnsureColumn N'DoctorShifts', N'EndTime', N'ALTER TABLE [dbo].[DoctorShifts] ADD [EndTime] time NOT NULL CONSTRAINT [DF_DoctorShifts_EndTime] DEFAULT (''00:00'');';
EXEC #EnsureColumn N'DoctorShifts', N'SlotDurationMinutes', N'ALTER TABLE [dbo].[DoctorShifts] ADD [SlotDurationMinutes] int NOT NULL CONSTRAINT [DF_DoctorShifts_SlotDurationMinutes] DEFAULT (0);';
EXEC #EnsureColumn N'DoctorShifts', N'MaxPatientsPerSlot', N'ALTER TABLE [dbo].[DoctorShifts] ADD [MaxPatientsPerSlot] int NOT NULL CONSTRAINT [DF_DoctorShifts_MaxPatientsPerSlot] DEFAULT (0);';
EXEC #EnsureColumn N'DoctorShifts', N'IsActive', N'ALTER TABLE [dbo].[DoctorShifts] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_DoctorShifts_IsActive] DEFAULT (0);';
GO

-- DocumentVersions
EXEC #EnsureColumn N'DocumentVersions', N'Id', N'ALTER TABLE [dbo].[DocumentVersions] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'DocumentVersions', N'DocumentId', N'ALTER TABLE [dbo].[DocumentVersions] ADD [DocumentId] int NOT NULL CONSTRAINT [DF_DocumentVersions_DocumentId] DEFAULT (0);';
EXEC #EnsureColumn N'DocumentVersions', N'VersionNumber', N'ALTER TABLE [dbo].[DocumentVersions] ADD [VersionNumber] int NOT NULL CONSTRAINT [DF_DocumentVersions_VersionNumber] DEFAULT (0);';
EXEC #EnsureColumn N'DocumentVersions', N'ChangeSummary', N'ALTER TABLE [dbo].[DocumentVersions] ADD [ChangeSummary] nvarchar(max) NOT NULL CONSTRAINT [DF_DocumentVersions_ChangeSummary] DEFAULT (N'''');';
EXEC #EnsureColumn N'DocumentVersions', N'StoredFileName', N'ALTER TABLE [dbo].[DocumentVersions] ADD [StoredFileName] nvarchar(max) NOT NULL CONSTRAINT [DF_DocumentVersions_StoredFileName] DEFAULT (N'''');';
EXEC #EnsureColumn N'DocumentVersions', N'OriginalFileName', N'ALTER TABLE [dbo].[DocumentVersions] ADD [OriginalFileName] nvarchar(max) NOT NULL CONSTRAINT [DF_DocumentVersions_OriginalFileName] DEFAULT (N'''');';
EXEC #EnsureColumn N'DocumentVersions', N'FileSize', N'ALTER TABLE [dbo].[DocumentVersions] ADD [FileSize] bigint NOT NULL CONSTRAINT [DF_DocumentVersions_FileSize] DEFAULT (0);';
EXEC #EnsureColumn N'DocumentVersions', N'Status', N'ALTER TABLE [dbo].[DocumentVersions] ADD [Status] nvarchar(20) NOT NULL CONSTRAINT [DF_DocumentVersions_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'DocumentVersions', N'CreatedByUserId', N'ALTER TABLE [dbo].[DocumentVersions] ADD [CreatedByUserId] nvarchar(max) NOT NULL CONSTRAINT [DF_DocumentVersions_CreatedByUserId] DEFAULT (N'''');';
EXEC #EnsureColumn N'DocumentVersions', N'CreatedBy', N'ALTER TABLE [dbo].[DocumentVersions] ADD [CreatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_DocumentVersions_CreatedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'DocumentVersions', N'CreatedAt', N'ALTER TABLE [dbo].[DocumentVersions] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_DocumentVersions_CreatedAt] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'DocumentVersions', N'SubmittedAt', N'ALTER TABLE [dbo].[DocumentVersions] ADD [SubmittedAt] datetime2 NULL;';
EXEC #EnsureColumn N'DocumentVersions', N'ApprovedByUserId', N'ALTER TABLE [dbo].[DocumentVersions] ADD [ApprovedByUserId] nvarchar(max) NOT NULL CONSTRAINT [DF_DocumentVersions_ApprovedByUserId] DEFAULT (N'''');';
EXEC #EnsureColumn N'DocumentVersions', N'ApprovedBy', N'ALTER TABLE [dbo].[DocumentVersions] ADD [ApprovedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_DocumentVersions_ApprovedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'DocumentVersions', N'ApprovedAt', N'ALTER TABLE [dbo].[DocumentVersions] ADD [ApprovedAt] datetime2 NULL;';
EXEC #EnsureColumn N'DocumentVersions', N'ApprovalComments', N'ALTER TABLE [dbo].[DocumentVersions] ADD [ApprovalComments] nvarchar(max) NOT NULL CONSTRAINT [DF_DocumentVersions_ApprovalComments] DEFAULT (N'''');';
EXEC #EnsureColumn N'DocumentVersions', N'EffectiveDate', N'ALTER TABLE [dbo].[DocumentVersions] ADD [EffectiveDate] datetime2 NULL;';
GO

-- DownloadFiles
EXEC #EnsureColumn N'DownloadFiles', N'Id', N'ALTER TABLE [dbo].[DownloadFiles] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'DownloadFiles', N'Title', N'ALTER TABLE [dbo].[DownloadFiles] ADD [Title] nvarchar(max) NOT NULL CONSTRAINT [DF__DownloadF__Title__3335971A] DEFAULT ('''');';
EXEC #EnsureColumn N'DownloadFiles', N'Description', N'ALTER TABLE [dbo].[DownloadFiles] ADD [Description] nvarchar(max) NOT NULL CONSTRAINT [DF__DownloadF__Descr__3429BB53] DEFAULT ('''');';
EXEC #EnsureColumn N'DownloadFiles', N'Category', N'ALTER TABLE [dbo].[DownloadFiles] ADD [Category] nvarchar(max) NOT NULL CONSTRAINT [DF__DownloadF__Categ__351DDF8C] DEFAULT ('''');';
EXEC #EnsureColumn N'DownloadFiles', N'FileName', N'ALTER TABLE [dbo].[DownloadFiles] ADD [FileName] nvarchar(max) NOT NULL CONSTRAINT [DF__DownloadF__FileN__361203C5] DEFAULT ('''');';
EXEC #EnsureColumn N'DownloadFiles', N'FilePath', N'ALTER TABLE [dbo].[DownloadFiles] ADD [FilePath] nvarchar(max) NOT NULL CONSTRAINT [DF__DownloadF__FileP__370627FE] DEFAULT ('''');';
EXEC #EnsureColumn N'DownloadFiles', N'FileType', N'ALTER TABLE [dbo].[DownloadFiles] ADD [FileType] nvarchar(max) NOT NULL CONSTRAINT [DF__DownloadF__FileT__37FA4C37] DEFAULT ('''');';
EXEC #EnsureColumn N'DownloadFiles', N'FileSizeBytes', N'ALTER TABLE [dbo].[DownloadFiles] ADD [FileSizeBytes] bigint NOT NULL CONSTRAINT [DF__DownloadF__FileS__38EE7070] DEFAULT ((0));';
EXEC #EnsureColumn N'DownloadFiles', N'DownloadCount', N'ALTER TABLE [dbo].[DownloadFiles] ADD [DownloadCount] int NOT NULL CONSTRAINT [DF__DownloadF__Downl__39E294A9] DEFAULT ((0));';
EXEC #EnsureColumn N'DownloadFiles', N'UploadedByUserId', N'ALTER TABLE [dbo].[DownloadFiles] ADD [UploadedByUserId] nvarchar(128) NOT NULL CONSTRAINT [DF__DownloadF__Uploa__3AD6B8E2] DEFAULT ('''');';
EXEC #EnsureColumn N'DownloadFiles', N'IsPublic', N'ALTER TABLE [dbo].[DownloadFiles] ADD [IsPublic] bit NOT NULL CONSTRAINT [DF__DownloadF__IsPub__3BCADD1B] DEFAULT ((0));';
EXEC #EnsureColumn N'DownloadFiles', N'IsActive', N'ALTER TABLE [dbo].[DownloadFiles] ADD [IsActive] bit NOT NULL CONSTRAINT [DF__DownloadF__IsAct__3CBF0154] DEFAULT ((1));';
EXEC #EnsureColumn N'DownloadFiles', N'UploadedAt', N'ALTER TABLE [dbo].[DownloadFiles] ADD [UploadedAt] datetime2 NOT NULL CONSTRAINT [DF_DownloadFiles_UploadedAt] DEFAULT (''1900-01-01'');';
GO

-- Equipment
EXEC #EnsureColumn N'Equipment', N'Id', N'ALTER TABLE [dbo].[Equipment] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'Equipment', N'HospitalId', N'ALTER TABLE [dbo].[Equipment] ADD [HospitalId] int NULL;';
EXEC #EnsureColumn N'Equipment', N'AssetTag', N'ALTER TABLE [dbo].[Equipment] ADD [AssetTag] nvarchar(30) NOT NULL CONSTRAINT [DF_Equipment_AssetTag] DEFAULT (N'''');';
EXEC #EnsureColumn N'Equipment', N'Name', N'ALTER TABLE [dbo].[Equipment] ADD [Name] nvarchar(150) NOT NULL CONSTRAINT [DF_Equipment_Name] DEFAULT (N'''');';
EXEC #EnsureColumn N'Equipment', N'Category', N'ALTER TABLE [dbo].[Equipment] ADD [Category] nvarchar(50) NOT NULL CONSTRAINT [DF_Equipment_Category] DEFAULT (N'''');';
EXEC #EnsureColumn N'Equipment', N'Manufacturer', N'ALTER TABLE [dbo].[Equipment] ADD [Manufacturer] nvarchar(100) NOT NULL CONSTRAINT [DF_Equipment_Manufacturer] DEFAULT (N'''');';
EXEC #EnsureColumn N'Equipment', N'Model', N'ALTER TABLE [dbo].[Equipment] ADD [Model] nvarchar(100) NOT NULL CONSTRAINT [DF_Equipment_Model] DEFAULT (N'''');';
EXEC #EnsureColumn N'Equipment', N'SerialNumber', N'ALTER TABLE [dbo].[Equipment] ADD [SerialNumber] nvarchar(100) NOT NULL CONSTRAINT [DF_Equipment_SerialNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'Equipment', N'Location', N'ALTER TABLE [dbo].[Equipment] ADD [Location] nvarchar(150) NOT NULL CONSTRAINT [DF_Equipment_Location] DEFAULT (N'''');';
EXEC #EnsureColumn N'Equipment', N'Supplier', N'ALTER TABLE [dbo].[Equipment] ADD [Supplier] nvarchar(150) NOT NULL CONSTRAINT [DF_Equipment_Supplier] DEFAULT (N'''');';
EXEC #EnsureColumn N'Equipment', N'PurchaseDate', N'ALTER TABLE [dbo].[Equipment] ADD [PurchaseDate] datetime2 NULL;';
EXEC #EnsureColumn N'Equipment', N'WarrantyExpiry', N'ALTER TABLE [dbo].[Equipment] ADD [WarrantyExpiry] datetime2 NULL;';
EXEC #EnsureColumn N'Equipment', N'RiskClass', N'ALTER TABLE [dbo].[Equipment] ADD [RiskClass] nvarchar(10) NOT NULL CONSTRAINT [DF_Equipment_RiskClass] DEFAULT (N'''');';
EXEC #EnsureColumn N'Equipment', N'Status', N'ALTER TABLE [dbo].[Equipment] ADD [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_Equipment_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'Equipment', N'MaintenanceIntervalDays', N'ALTER TABLE [dbo].[Equipment] ADD [MaintenanceIntervalDays] int NOT NULL CONSTRAINT [DF_Equipment_MaintenanceIntervalDays] DEFAULT (0);';
EXEC #EnsureColumn N'Equipment', N'CalibrationIntervalDays', N'ALTER TABLE [dbo].[Equipment] ADD [CalibrationIntervalDays] int NOT NULL CONSTRAINT [DF_Equipment_CalibrationIntervalDays] DEFAULT (0);';
EXEC #EnsureColumn N'Equipment', N'LastMaintenanceDate', N'ALTER TABLE [dbo].[Equipment] ADD [LastMaintenanceDate] datetime2 NULL;';
EXEC #EnsureColumn N'Equipment', N'NextMaintenanceDue', N'ALTER TABLE [dbo].[Equipment] ADD [NextMaintenanceDue] datetime2 NULL;';
EXEC #EnsureColumn N'Equipment', N'LastCalibrationDate', N'ALTER TABLE [dbo].[Equipment] ADD [LastCalibrationDate] datetime2 NULL;';
EXEC #EnsureColumn N'Equipment', N'NextCalibrationDue', N'ALTER TABLE [dbo].[Equipment] ADD [NextCalibrationDue] datetime2 NULL;';
EXEC #EnsureColumn N'Equipment', N'Notes', N'ALTER TABLE [dbo].[Equipment] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF_Equipment_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'Equipment', N'CreatedAt', N'ALTER TABLE [dbo].[Equipment] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_Equipment_CreatedAt] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'Equipment', N'CreatedBy', N'ALTER TABLE [dbo].[Equipment] ADD [CreatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_Equipment_CreatedBy] DEFAULT (N'''');';
GO

-- EquipmentServiceRecords
EXEC #EnsureColumn N'EquipmentServiceRecords', N'Id', N'ALTER TABLE [dbo].[EquipmentServiceRecords] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'EquipmentServiceRecords', N'EquipmentId', N'ALTER TABLE [dbo].[EquipmentServiceRecords] ADD [EquipmentId] int NOT NULL CONSTRAINT [DF_EquipmentServiceRecords_EquipmentId] DEFAULT (0);';
EXEC #EnsureColumn N'EquipmentServiceRecords', N'ServiceType', N'ALTER TABLE [dbo].[EquipmentServiceRecords] ADD [ServiceType] nvarchar(30) NOT NULL CONSTRAINT [DF_EquipmentServiceRecords_ServiceType] DEFAULT (N'''');';
EXEC #EnsureColumn N'EquipmentServiceRecords', N'ServiceDate', N'ALTER TABLE [dbo].[EquipmentServiceRecords] ADD [ServiceDate] datetime2 NOT NULL CONSTRAINT [DF_EquipmentServiceRecords_ServiceDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'EquipmentServiceRecords', N'PerformedBy', N'ALTER TABLE [dbo].[EquipmentServiceRecords] ADD [PerformedBy] nvarchar(150) NOT NULL CONSTRAINT [DF_EquipmentServiceRecords_PerformedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'EquipmentServiceRecords', N'Result', N'ALTER TABLE [dbo].[EquipmentServiceRecords] ADD [Result] nvarchar(20) NOT NULL CONSTRAINT [DF_EquipmentServiceRecords_Result] DEFAULT (N'''');';
EXEC #EnsureColumn N'EquipmentServiceRecords', N'Cost', N'ALTER TABLE [dbo].[EquipmentServiceRecords] ADD [Cost] decimal(18, 2) NOT NULL CONSTRAINT [DF_EquipmentServiceRecords_Cost] DEFAULT (0);';
EXEC #EnsureColumn N'EquipmentServiceRecords', N'CertificateNumber', N'ALTER TABLE [dbo].[EquipmentServiceRecords] ADD [CertificateNumber] nvarchar(100) NOT NULL CONSTRAINT [DF_EquipmentServiceRecords_CertificateNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'EquipmentServiceRecords', N'Notes', N'ALTER TABLE [dbo].[EquipmentServiceRecords] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF_EquipmentServiceRecords_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'EquipmentServiceRecords', N'CreatedAt', N'ALTER TABLE [dbo].[EquipmentServiceRecords] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_EquipmentServiceRecords_CreatedAt] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'EquipmentServiceRecords', N'CreatedBy', N'ALTER TABLE [dbo].[EquipmentServiceRecords] ADD [CreatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_EquipmentServiceRecords_CreatedBy] DEFAULT (N'''');';
GO

-- Features
EXEC #EnsureColumn N'Features', N'Id', N'ALTER TABLE [dbo].[Features] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'Features', N'Name', N'ALTER TABLE [dbo].[Features] ADD [Name] nvarchar(max) NOT NULL CONSTRAINT [DF_Features_Name] DEFAULT (N'''');';
EXEC #EnsureColumn N'Features', N'Module', N'ALTER TABLE [dbo].[Features] ADD [Module] nvarchar(max) NOT NULL CONSTRAINT [DF_Features_Module] DEFAULT (N'''');';
EXEC #EnsureColumn N'Features', N'Description', N'ALTER TABLE [dbo].[Features] ADD [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_Features_Description] DEFAULT (N'''');';
EXEC #EnsureColumn N'Features', N'IsActive', N'ALTER TABLE [dbo].[Features] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_Features_IsActive] DEFAULT (0);';
EXEC #EnsureColumn N'Features', N'CreatedDate', N'ALTER TABLE [dbo].[Features] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Features_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- GeneratedReports
EXEC #EnsureColumn N'GeneratedReports', N'Id', N'ALTER TABLE [dbo].[GeneratedReports] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'GeneratedReports', N'ReportName', N'ALTER TABLE [dbo].[GeneratedReports] ADD [ReportName] nvarchar(max) NOT NULL CONSTRAINT [DF_GeneratedReports_ReportName] DEFAULT (N'''');';
EXEC #EnsureColumn N'GeneratedReports', N'ReportType', N'ALTER TABLE [dbo].[GeneratedReports] ADD [ReportType] nvarchar(128) NOT NULL CONSTRAINT [DF_GeneratedReports_ReportType] DEFAULT (N'''');';
EXEC #EnsureColumn N'GeneratedReports', N'Description', N'ALTER TABLE [dbo].[GeneratedReports] ADD [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_GeneratedReports_Description] DEFAULT (N'''');';
EXEC #EnsureColumn N'GeneratedReports', N'FromDate', N'ALTER TABLE [dbo].[GeneratedReports] ADD [FromDate] datetime2 NOT NULL CONSTRAINT [DF_GeneratedReports_FromDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'GeneratedReports', N'ToDate', N'ALTER TABLE [dbo].[GeneratedReports] ADD [ToDate] datetime2 NOT NULL CONSTRAINT [DF_GeneratedReports_ToDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'GeneratedReports', N'DepartmentId', N'ALTER TABLE [dbo].[GeneratedReports] ADD [DepartmentId] int NULL;';
EXEC #EnsureColumn N'GeneratedReports', N'FilePath', N'ALTER TABLE [dbo].[GeneratedReports] ADD [FilePath] nvarchar(max) NOT NULL CONSTRAINT [DF_GeneratedReports_FilePath] DEFAULT (N'''');';
EXEC #EnsureColumn N'GeneratedReports', N'FileFormat', N'ALTER TABLE [dbo].[GeneratedReports] ADD [FileFormat] nvarchar(max) NOT NULL CONSTRAINT [DF_GeneratedReports_FileFormat] DEFAULT (N'''');';
EXEC #EnsureColumn N'GeneratedReports', N'FileSize', N'ALTER TABLE [dbo].[GeneratedReports] ADD [FileSize] bigint NOT NULL CONSTRAINT [DF_GeneratedReports_FileSize] DEFAULT (0);';
EXEC #EnsureColumn N'GeneratedReports', N'GeneratedBy', N'ALTER TABLE [dbo].[GeneratedReports] ADD [GeneratedBy] {{StaffId}} NOT NULL CONSTRAINT [DF_GeneratedReports_GeneratedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'GeneratedReports', N'CreatedDate', N'ALTER TABLE [dbo].[GeneratedReports] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_GeneratedReports_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'GeneratedReports', N'Status', N'ALTER TABLE [dbo].[GeneratedReports] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_GeneratedReports_Status] DEFAULT (N'''');';
GO

-- Hospitals
EXEC #EnsureColumn N'Hospitals', N'Id', N'ALTER TABLE [dbo].[Hospitals] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'Hospitals', N'Code', N'ALTER TABLE [dbo].[Hospitals] ADD [Code] nvarchar(20) NOT NULL CONSTRAINT [DF_Hospitals_Code] DEFAULT (N'''');';
EXEC #EnsureColumn N'Hospitals', N'Name', N'ALTER TABLE [dbo].[Hospitals] ADD [Name] nvarchar(200) NOT NULL CONSTRAINT [DF_Hospitals_Name] DEFAULT (N'''');';
EXEC #EnsureColumn N'Hospitals', N'Address', N'ALTER TABLE [dbo].[Hospitals] ADD [Address] nvarchar(500) NOT NULL CONSTRAINT [DF_Hospitals_Address] DEFAULT (N'''');';
EXEC #EnsureColumn N'Hospitals', N'City', N'ALTER TABLE [dbo].[Hospitals] ADD [City] nvarchar(100) NOT NULL CONSTRAINT [DF_Hospitals_City] DEFAULT (N'''');';
EXEC #EnsureColumn N'Hospitals', N'Phone', N'ALTER TABLE [dbo].[Hospitals] ADD [Phone] nvarchar(50) NOT NULL CONSTRAINT [DF_Hospitals_Phone] DEFAULT (N'''');';
EXEC #EnsureColumn N'Hospitals', N'Email', N'ALTER TABLE [dbo].[Hospitals] ADD [Email] nvarchar(200) NOT NULL CONSTRAINT [DF_Hospitals_Email] DEFAULT (N'''');';
EXEC #EnsureColumn N'Hospitals', N'LicenseNumber', N'ALTER TABLE [dbo].[Hospitals] ADD [LicenseNumber] nvarchar(100) NOT NULL CONSTRAINT [DF_Hospitals_LicenseNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'Hospitals', N'IsActive', N'ALTER TABLE [dbo].[Hospitals] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_Hospitals_IsActive] DEFAULT ((1));';
EXEC #EnsureColumn N'Hospitals', N'IsDefault', N'ALTER TABLE [dbo].[Hospitals] ADD [IsDefault] bit NOT NULL CONSTRAINT [DF_Hospitals_IsDefault] DEFAULT ((0));';
EXEC #EnsureColumn N'Hospitals', N'CreatedDate', N'ALTER TABLE [dbo].[Hospitals] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Hospitals_CreatedDate] DEFAULT (sysutcdatetime());';
EXEC #EnsureColumn N'Hospitals', N'UpdatedDate', N'ALTER TABLE [dbo].[Hospitals] ADD [UpdatedDate] datetime2 NULL;';
GO

-- IdCardRecords
EXEC #EnsureColumn N'IdCardRecords', N'Id', N'ALTER TABLE [dbo].[IdCardRecords] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'IdCardRecords', N'StaffId', N'ALTER TABLE [dbo].[IdCardRecords] ADD [StaffId] {{StaffId}} NOT NULL CONSTRAINT [DF_IdCardRecords_StaffId] DEFAULT (N'''');';
EXEC #EnsureColumn N'IdCardRecords', N'CardNumber', N'ALTER TABLE [dbo].[IdCardRecords] ADD [CardNumber] nvarchar(128) NOT NULL CONSTRAINT [DF_IdCardRecords_CardNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'IdCardRecords', N'IssueDate', N'ALTER TABLE [dbo].[IdCardRecords] ADD [IssueDate] datetime2 NOT NULL CONSTRAINT [DF_IdCardRecords_IssueDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'IdCardRecords', N'ExpiryDate', N'ALTER TABLE [dbo].[IdCardRecords] ADD [ExpiryDate] datetime2 NULL;';
EXEC #EnsureColumn N'IdCardRecords', N'Status', N'ALTER TABLE [dbo].[IdCardRecords] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_IdCardRecords_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'IdCardRecords', N'Notes', N'ALTER TABLE [dbo].[IdCardRecords] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF_IdCardRecords_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'IdCardRecords', N'CreatedDate', N'ALTER TABLE [dbo].[IdCardRecords] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_IdCardRecords_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- InternalAudits
EXEC #EnsureColumn N'InternalAudits', N'Id', N'ALTER TABLE [dbo].[InternalAudits] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'InternalAudits', N'HospitalId', N'ALTER TABLE [dbo].[InternalAudits] ADD [HospitalId] int NULL;';
EXEC #EnsureColumn N'InternalAudits', N'AuditNumber', N'ALTER TABLE [dbo].[InternalAudits] ADD [AuditNumber] nvarchar(30) NOT NULL CONSTRAINT [DF_InternalAudits_AuditNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'InternalAudits', N'Title', N'ALTER TABLE [dbo].[InternalAudits] ADD [Title] nvarchar(200) NOT NULL CONSTRAINT [DF_InternalAudits_Title] DEFAULT (N'''');';
EXEC #EnsureColumn N'InternalAudits', N'Scope', N'ALTER TABLE [dbo].[InternalAudits] ADD [Scope] nvarchar(200) NOT NULL CONSTRAINT [DF_InternalAudits_Scope] DEFAULT (N'''');';
EXEC #EnsureColumn N'InternalAudits', N'Standard', N'ALTER TABLE [dbo].[InternalAudits] ADD [Standard] nvarchar(50) NOT NULL CONSTRAINT [DF_InternalAudits_Standard] DEFAULT (N'''');';
EXEC #EnsureColumn N'InternalAudits', N'PlannedDate', N'ALTER TABLE [dbo].[InternalAudits] ADD [PlannedDate] datetime2 NOT NULL CONSTRAINT [DF_InternalAudits_PlannedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'InternalAudits', N'LeadAuditor', N'ALTER TABLE [dbo].[InternalAudits] ADD [LeadAuditor] nvarchar(100) NOT NULL CONSTRAINT [DF_InternalAudits_LeadAuditor] DEFAULT (N'''');';
EXEC #EnsureColumn N'InternalAudits', N'Status', N'ALTER TABLE [dbo].[InternalAudits] ADD [Status] nvarchar(20) NOT NULL CONSTRAINT [DF_InternalAudits_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'InternalAudits', N'CompletedDate', N'ALTER TABLE [dbo].[InternalAudits] ADD [CompletedDate] datetime2 NULL;';
EXEC #EnsureColumn N'InternalAudits', N'Summary', N'ALTER TABLE [dbo].[InternalAudits] ADD [Summary] nvarchar(max) NOT NULL CONSTRAINT [DF_InternalAudits_Summary] DEFAULT (N'''');';
EXEC #EnsureColumn N'InternalAudits', N'CreatedAt', N'ALTER TABLE [dbo].[InternalAudits] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_InternalAudits_CreatedAt] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'InternalAudits', N'CreatedBy', N'ALTER TABLE [dbo].[InternalAudits] ADD [CreatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_InternalAudits_CreatedBy] DEFAULT (N'''');';
GO

-- InternalMessages
EXEC #EnsureColumn N'InternalMessages', N'Id', N'ALTER TABLE [dbo].[InternalMessages] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'InternalMessages', N'SenderId', N'ALTER TABLE [dbo].[InternalMessages] ADD [SenderId] {{UserId}} NOT NULL CONSTRAINT [DF_InternalMessages_SenderId] DEFAULT (N'''');';
EXEC #EnsureColumn N'InternalMessages', N'RecipientId', N'ALTER TABLE [dbo].[InternalMessages] ADD [RecipientId] nvarchar(128) NOT NULL CONSTRAINT [DF__InternalM__Recip__13BCEBC1] DEFAULT ('''');';
EXEC #EnsureColumn N'InternalMessages', N'Subject', N'ALTER TABLE [dbo].[InternalMessages] ADD [Subject] nvarchar(max) NOT NULL CONSTRAINT [DF__InternalM__Subje__14B10FFA] DEFAULT ('''');';
EXEC #EnsureColumn N'InternalMessages', N'Body', N'ALTER TABLE [dbo].[InternalMessages] ADD [Body] nvarchar(max) NOT NULL CONSTRAINT [DF__InternalMe__Body__15A53433] DEFAULT ('''');';
EXEC #EnsureColumn N'InternalMessages', N'IsRead', N'ALTER TABLE [dbo].[InternalMessages] ADD [IsRead] bit NOT NULL CONSTRAINT [DF__InternalM__IsRea__1699586C] DEFAULT ((0));';
EXEC #EnsureColumn N'InternalMessages', N'IsBroadcast', N'ALTER TABLE [dbo].[InternalMessages] ADD [IsBroadcast] bit NOT NULL CONSTRAINT [DF__InternalM__IsBro__178D7CA5] DEFAULT ((0));';
EXEC #EnsureColumn N'InternalMessages', N'ParentMessageId', N'ALTER TABLE [dbo].[InternalMessages] ADD [ParentMessageId] int NULL;';
EXEC #EnsureColumn N'InternalMessages', N'SentAt', N'ALTER TABLE [dbo].[InternalMessages] ADD [SentAt] datetime2 NOT NULL CONSTRAINT [DF_InternalMessages_SentAt] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'InternalMessages', N'ReadAt', N'ALTER TABLE [dbo].[InternalMessages] ADD [ReadAt] datetime2 NULL;';
EXEC #EnsureColumn N'InternalMessages', N'IsDeletedBySender', N'ALTER TABLE [dbo].[InternalMessages] ADD [IsDeletedBySender] bit NOT NULL CONSTRAINT [DF__InternalM__IsDel__1881A0DE] DEFAULT ((0));';
EXEC #EnsureColumn N'InternalMessages', N'IsDeletedByRecipient', N'ALTER TABLE [dbo].[InternalMessages] ADD [IsDeletedByRecipient] bit NOT NULL CONSTRAINT [DF__InternalM__IsDel__1975C517] DEFAULT ((0));';
GO

-- InventoryItems
EXEC #EnsureColumn N'InventoryItems', N'Id', N'ALTER TABLE [dbo].[InventoryItems] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'InventoryItems', N'ItemCode', N'ALTER TABLE [dbo].[InventoryItems] ADD [ItemCode] nvarchar(max) NOT NULL CONSTRAINT [DF__Inventory__ItemC__1E3A7A34] DEFAULT ('''');';
EXEC #EnsureColumn N'InventoryItems', N'Name', N'ALTER TABLE [dbo].[InventoryItems] ADD [Name] nvarchar(max) NOT NULL CONSTRAINT [DF__InventoryI__Name__1F2E9E6D] DEFAULT ('''');';
EXEC #EnsureColumn N'InventoryItems', N'Category', N'ALTER TABLE [dbo].[InventoryItems] ADD [Category] nvarchar(max) NOT NULL CONSTRAINT [DF__Inventory__Categ__2022C2A6] DEFAULT ('''');';
EXEC #EnsureColumn N'InventoryItems', N'Unit', N'ALTER TABLE [dbo].[InventoryItems] ADD [Unit] nvarchar(max) NOT NULL CONSTRAINT [DF__InventoryI__Unit__2116E6DF] DEFAULT ('''');';
EXEC #EnsureColumn N'InventoryItems', N'CurrentStock', N'ALTER TABLE [dbo].[InventoryItems] ADD [CurrentStock] decimal(18, 2) NOT NULL CONSTRAINT [DF__Inventory__Curre__220B0B18] DEFAULT ((0));';
EXEC #EnsureColumn N'InventoryItems', N'MinimumStock', N'ALTER TABLE [dbo].[InventoryItems] ADD [MinimumStock] decimal(18, 2) NOT NULL CONSTRAINT [DF__Inventory__Minim__22FF2F51] DEFAULT ((0));';
EXEC #EnsureColumn N'InventoryItems', N'ReorderLevel', N'ALTER TABLE [dbo].[InventoryItems] ADD [ReorderLevel] decimal(18, 2) NOT NULL CONSTRAINT [DF__Inventory__Reord__23F3538A] DEFAULT ((0));';
EXEC #EnsureColumn N'InventoryItems', N'UnitCost', N'ALTER TABLE [dbo].[InventoryItems] ADD [UnitCost] decimal(18, 2) NOT NULL CONSTRAINT [DF__Inventory__UnitC__24E777C3] DEFAULT ((0));';
EXEC #EnsureColumn N'InventoryItems', N'Supplier', N'ALTER TABLE [dbo].[InventoryItems] ADD [Supplier] nvarchar(max) NOT NULL CONSTRAINT [DF__Inventory__Suppl__25DB9BFC] DEFAULT ('''');';
EXEC #EnsureColumn N'InventoryItems', N'StorageLocation', N'ALTER TABLE [dbo].[InventoryItems] ADD [StorageLocation] nvarchar(max) NOT NULL CONSTRAINT [DF__Inventory__Stora__26CFC035] DEFAULT ('''');';
EXEC #EnsureColumn N'InventoryItems', N'IsActive', N'ALTER TABLE [dbo].[InventoryItems] ADD [IsActive] bit NOT NULL CONSTRAINT [DF__Inventory__IsAct__27C3E46E] DEFAULT ((1));';
EXEC #EnsureColumn N'InventoryItems', N'CreatedDate', N'ALTER TABLE [dbo].[InventoryItems] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_InventoryItems_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'InventoryItems', N'HospitalId', N'ALTER TABLE [dbo].[InventoryItems] ADD [HospitalId] int NULL;';
EXEC #EnsureColumn N'InventoryItems', N'VendorId', N'ALTER TABLE [dbo].[InventoryItems] ADD [VendorId] int NULL;';
GO

-- InventoryTransactions
EXEC #EnsureColumn N'InventoryTransactions', N'Id', N'ALTER TABLE [dbo].[InventoryTransactions] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'InventoryTransactions', N'InventoryItemId', N'ALTER TABLE [dbo].[InventoryTransactions] ADD [InventoryItemId] int NOT NULL CONSTRAINT [DF_InventoryTransactions_InventoryItemId] DEFAULT (0);';
EXEC #EnsureColumn N'InventoryTransactions', N'TransactionType', N'ALTER TABLE [dbo].[InventoryTransactions] ADD [TransactionType] nvarchar(max) NOT NULL CONSTRAINT [DF__Inventory__Trans__2AA05119] DEFAULT (''IN'');';
EXEC #EnsureColumn N'InventoryTransactions', N'Quantity', N'ALTER TABLE [dbo].[InventoryTransactions] ADD [Quantity] decimal(18, 2) NOT NULL CONSTRAINT [DF__Inventory__Quant__2B947552] DEFAULT ((0));';
EXEC #EnsureColumn N'InventoryTransactions', N'UnitCost', N'ALTER TABLE [dbo].[InventoryTransactions] ADD [UnitCost] decimal(18, 2) NOT NULL CONSTRAINT [DF__Inventory__UnitC__2C88998B] DEFAULT ((0));';
EXEC #EnsureColumn N'InventoryTransactions', N'ReferenceNumber', N'ALTER TABLE [dbo].[InventoryTransactions] ADD [ReferenceNumber] nvarchar(max) NOT NULL CONSTRAINT [DF__Inventory__Refer__2D7CBDC4] DEFAULT ('''');';
EXEC #EnsureColumn N'InventoryTransactions', N'Remarks', N'ALTER TABLE [dbo].[InventoryTransactions] ADD [Remarks] nvarchar(max) NOT NULL CONSTRAINT [DF__Inventory__Remar__2E70E1FD] DEFAULT ('''');';
EXEC #EnsureColumn N'InventoryTransactions', N'PerformedByUserId', N'ALTER TABLE [dbo].[InventoryTransactions] ADD [PerformedByUserId] nvarchar(128) NOT NULL CONSTRAINT [DF__Inventory__Perfo__2F650636] DEFAULT ('''');';
EXEC #EnsureColumn N'InventoryTransactions', N'TransactionDate', N'ALTER TABLE [dbo].[InventoryTransactions] ADD [TransactionDate] datetime2 NOT NULL CONSTRAINT [DF_InventoryTransactions_TransactionDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'InventoryTransactions', N'Department', N'ALTER TABLE [dbo].[InventoryTransactions] ADD [Department] nvarchar(100) NOT NULL CONSTRAINT [DF_InventoryTransactions_Department] DEFAULT (N'''');';
EXEC #EnsureColumn N'InventoryTransactions', N'PatientId', N'ALTER TABLE [dbo].[InventoryTransactions] ADD [PatientId] int NULL;';
EXEC #EnsureColumn N'InventoryTransactions', N'BillId', N'ALTER TABLE [dbo].[InventoryTransactions] ADD [BillId] int NULL;';
EXEC #EnsureColumn N'InventoryTransactions', N'PurchaseBillId', N'ALTER TABLE [dbo].[InventoryTransactions] ADD [PurchaseBillId] int NULL;';
GO

-- IPDAdmissions
EXEC #EnsureColumn N'IPDAdmissions', N'Id', N'ALTER TABLE [dbo].[IPDAdmissions] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'IPDAdmissions', N'PatientId', N'ALTER TABLE [dbo].[IPDAdmissions] ADD [PatientId] int NOT NULL CONSTRAINT [DF_IPDAdmissions_PatientId] DEFAULT (0);';
EXEC #EnsureColumn N'IPDAdmissions', N'DoctorId', N'ALTER TABLE [dbo].[IPDAdmissions] ADD [DoctorId] int NOT NULL CONSTRAINT [DF_IPDAdmissions_DoctorId] DEFAULT (0);';
EXEC #EnsureColumn N'IPDAdmissions', N'BedId', N'ALTER TABLE [dbo].[IPDAdmissions] ADD [BedId] int NULL;';
EXEC #EnsureColumn N'IPDAdmissions', N'AdmissionDate', N'ALTER TABLE [dbo].[IPDAdmissions] ADD [AdmissionDate] datetime2 NOT NULL CONSTRAINT [DF_IPDAdmissions_AdmissionDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'IPDAdmissions', N'DischargeDate', N'ALTER TABLE [dbo].[IPDAdmissions] ADD [DischargeDate] datetime2 NULL;';
EXEC #EnsureColumn N'IPDAdmissions', N'AdmissionType', N'ALTER TABLE [dbo].[IPDAdmissions] ADD [AdmissionType] nvarchar(max) NOT NULL CONSTRAINT [DF_IPDAdmissions_AdmissionType] DEFAULT (N'''');';
EXEC #EnsureColumn N'IPDAdmissions', N'Diagnosis', N'ALTER TABLE [dbo].[IPDAdmissions] ADD [Diagnosis] nvarchar(max) NOT NULL CONSTRAINT [DF_IPDAdmissions_Diagnosis] DEFAULT (N'''');';
EXEC #EnsureColumn N'IPDAdmissions', N'Treatment', N'ALTER TABLE [dbo].[IPDAdmissions] ADD [Treatment] nvarchar(max) NOT NULL CONSTRAINT [DF_IPDAdmissions_Treatment] DEFAULT (N'''');';
EXEC #EnsureColumn N'IPDAdmissions', N'Notes', N'ALTER TABLE [dbo].[IPDAdmissions] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF_IPDAdmissions_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'IPDAdmissions', N'Status', N'ALTER TABLE [dbo].[IPDAdmissions] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_IPDAdmissions_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'IPDAdmissions', N'DailyCharges', N'ALTER TABLE [dbo].[IPDAdmissions] ADD [DailyCharges] decimal(18, 2) NOT NULL CONSTRAINT [DF_IPDAdmissions_DailyCharges] DEFAULT (0);';
EXEC #EnsureColumn N'IPDAdmissions', N'CreatedDate', N'ALTER TABLE [dbo].[IPDAdmissions] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_IPDAdmissions_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'IPDAdmissions', N'CreatedBy', N'ALTER TABLE [dbo].[IPDAdmissions] ADD [CreatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_IPDAdmissions_CreatedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'IPDAdmissions', N'MedicalRecordId', N'ALTER TABLE [dbo].[IPDAdmissions] ADD [MedicalRecordId] int NULL;';
EXEC #EnsureColumn N'IPDAdmissions', N'HospitalId', N'ALTER TABLE [dbo].[IPDAdmissions] ADD [HospitalId] int NULL;';
GO

-- LabNoteHistories
EXEC #EnsureColumn N'LabNoteHistories', N'Id', N'ALTER TABLE [dbo].[LabNoteHistories] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'LabNoteHistories', N'LabResultId', N'ALTER TABLE [dbo].[LabNoteHistories] ADD [LabResultId] int NOT NULL CONSTRAINT [DF_LabNoteHistories_LabResultId] DEFAULT (0);';
EXEC #EnsureColumn N'LabNoteHistories', N'Notes', N'ALTER TABLE [dbo].[LabNoteHistories] ADD [Notes] nvarchar(4000) NOT NULL CONSTRAINT [DF__LabNoteHi__Notes__477199F1] DEFAULT ('''');';
EXEC #EnsureColumn N'LabNoteHistories', N'UpdatedBy', N'ALTER TABLE [dbo].[LabNoteHistories] ADD [UpdatedBy] nvarchar(256) NOT NULL CONSTRAINT [DF__LabNoteHi__Updat__4865BE2A] DEFAULT ('''');';
EXEC #EnsureColumn N'LabNoteHistories', N'UpdatedAtUtc', N'ALTER TABLE [dbo].[LabNoteHistories] ADD [UpdatedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_LabNoteHistories_UpdatedAtUtc] DEFAULT (''1900-01-01'');';
GO

-- LabResults
EXEC #EnsureColumn N'LabResults', N'Id', N'ALTER TABLE [dbo].[LabResults] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'LabResults', N'PatientId', N'ALTER TABLE [dbo].[LabResults] ADD [PatientId] int NOT NULL CONSTRAINT [DF_LabResults_PatientId] DEFAULT (0);';
EXEC #EnsureColumn N'LabResults', N'LabTestId', N'ALTER TABLE [dbo].[LabResults] ADD [LabTestId] int NOT NULL CONSTRAINT [DF_LabResults_LabTestId] DEFAULT (0);';
EXEC #EnsureColumn N'LabResults', N'OrderNumber', N'ALTER TABLE [dbo].[LabResults] ADD [OrderNumber] nvarchar(max) NOT NULL CONSTRAINT [DF_LabResults_OrderNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabResults', N'OrderDate', N'ALTER TABLE [dbo].[LabResults] ADD [OrderDate] datetime2 NOT NULL CONSTRAINT [DF_LabResults_OrderDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'LabResults', N'ResultDate', N'ALTER TABLE [dbo].[LabResults] ADD [ResultDate] datetime2 NULL;';
EXEC #EnsureColumn N'LabResults', N'ResultValue', N'ALTER TABLE [dbo].[LabResults] ADD [ResultValue] nvarchar(max) NOT NULL CONSTRAINT [DF_LabResults_ResultValue] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabResults', N'NormalRange', N'ALTER TABLE [dbo].[LabResults] ADD [NormalRange] nvarchar(max) NOT NULL CONSTRAINT [DF_LabResults_NormalRange] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabResults', N'Unit', N'ALTER TABLE [dbo].[LabResults] ADD [Unit] nvarchar(max) NOT NULL CONSTRAINT [DF_LabResults_Unit] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabResults', N'Interpretation', N'ALTER TABLE [dbo].[LabResults] ADD [Interpretation] nvarchar(max) NOT NULL CONSTRAINT [DF_LabResults_Interpretation] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabResults', N'Status', N'ALTER TABLE [dbo].[LabResults] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_LabResults_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabResults', N'PerformedBy', N'ALTER TABLE [dbo].[LabResults] ADD [PerformedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_LabResults_PerformedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabResults', N'VerifiedBy', N'ALTER TABLE [dbo].[LabResults] ADD [VerifiedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_LabResults_VerifiedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabResults', N'Notes', N'ALTER TABLE [dbo].[LabResults] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF_LabResults_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabResults', N'CreatedDate', N'ALTER TABLE [dbo].[LabResults] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_LabResults_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'LabResults', N'AccessionNumber', N'ALTER TABLE [dbo].[LabResults] ADD [AccessionNumber] nvarchar(30) NULL;';
EXEC #EnsureColumn N'LabResults', N'SampleType', N'ALTER TABLE [dbo].[LabResults] ADD [SampleType] nvarchar(50) NOT NULL CONSTRAINT [DF_LabResults_SampleType] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabResults', N'SampleStatus', N'ALTER TABLE [dbo].[LabResults] ADD [SampleStatus] nvarchar(30) NOT NULL CONSTRAINT [DF_LabResults_SampleStatus] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabResults', N'CollectedAt', N'ALTER TABLE [dbo].[LabResults] ADD [CollectedAt] datetime2 NULL;';
EXEC #EnsureColumn N'LabResults', N'CollectedBy', N'ALTER TABLE [dbo].[LabResults] ADD [CollectedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_LabResults_CollectedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabResults', N'ReceivedAt', N'ALTER TABLE [dbo].[LabResults] ADD [ReceivedAt] datetime2 NULL;';
EXEC #EnsureColumn N'LabResults', N'ReceivedBy', N'ALTER TABLE [dbo].[LabResults] ADD [ReceivedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_LabResults_ReceivedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabResults', N'RejectionReason', N'ALTER TABLE [dbo].[LabResults] ADD [RejectionReason] nvarchar(max) NOT NULL CONSTRAINT [DF_LabResults_RejectionReason] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabResults', N'ResultEnteredAt', N'ALTER TABLE [dbo].[LabResults] ADD [ResultEnteredAt] datetime2 NULL;';
EXEC #EnsureColumn N'LabResults', N'ResultEnteredBy', N'ALTER TABLE [dbo].[LabResults] ADD [ResultEnteredBy] nvarchar(max) NOT NULL CONSTRAINT [DF_LabResults_ResultEnteredBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabResults', N'SignedOffAt', N'ALTER TABLE [dbo].[LabResults] ADD [SignedOffAt] datetime2 NULL;';
EXEC #EnsureColumn N'LabResults', N'SignedOffBy', N'ALTER TABLE [dbo].[LabResults] ADD [SignedOffBy] nvarchar(max) NOT NULL CONSTRAINT [DF_LabResults_SignedOffBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabResults', N'SignedOffByUserId', N'ALTER TABLE [dbo].[LabResults] ADD [SignedOffByUserId] nvarchar(max) NOT NULL CONSTRAINT [DF_LabResults_SignedOffByUserId] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabResults', N'SignatureHash', N'ALTER TABLE [dbo].[LabResults] ADD [SignatureHash] nvarchar(max) NOT NULL CONSTRAINT [DF_LabResults_SignatureHash] DEFAULT (N'''');';
GO

-- LabSampleEvents
EXEC #EnsureColumn N'LabSampleEvents', N'Id', N'ALTER TABLE [dbo].[LabSampleEvents] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'LabSampleEvents', N'LabResultId', N'ALTER TABLE [dbo].[LabSampleEvents] ADD [LabResultId] int NOT NULL CONSTRAINT [DF_LabSampleEvents_LabResultId] DEFAULT (0);';
EXEC #EnsureColumn N'LabSampleEvents', N'EventType', N'ALTER TABLE [dbo].[LabSampleEvents] ADD [EventType] nvarchar(40) NOT NULL CONSTRAINT [DF_LabSampleEvents_EventType] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabSampleEvents', N'OccurredAt', N'ALTER TABLE [dbo].[LabSampleEvents] ADD [OccurredAt] datetime2 NOT NULL CONSTRAINT [DF_LabSampleEvents_OccurredAt] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'LabSampleEvents', N'UserId', N'ALTER TABLE [dbo].[LabSampleEvents] ADD [UserId] nvarchar(max) NOT NULL CONSTRAINT [DF_LabSampleEvents_UserId] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabSampleEvents', N'UserName', N'ALTER TABLE [dbo].[LabSampleEvents] ADD [UserName] nvarchar(max) NOT NULL CONSTRAINT [DF_LabSampleEvents_UserName] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabSampleEvents', N'Details', N'ALTER TABLE [dbo].[LabSampleEvents] ADD [Details] nvarchar(max) NOT NULL CONSTRAINT [DF_LabSampleEvents_Details] DEFAULT (N'''');';
GO

-- LabTests
EXEC #EnsureColumn N'LabTests', N'Id', N'ALTER TABLE [dbo].[LabTests] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'LabTests', N'TestName', N'ALTER TABLE [dbo].[LabTests] ADD [TestName] nvarchar(max) NOT NULL CONSTRAINT [DF_LabTests_TestName] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabTests', N'TestCode', N'ALTER TABLE [dbo].[LabTests] ADD [TestCode] nvarchar(max) NOT NULL CONSTRAINT [DF_LabTests_TestCode] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabTests', N'Category', N'ALTER TABLE [dbo].[LabTests] ADD [Category] nvarchar(max) NOT NULL CONSTRAINT [DF_LabTests_Category] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabTests', N'Description', N'ALTER TABLE [dbo].[LabTests] ADD [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_LabTests_Description] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabTests', N'Price', N'ALTER TABLE [dbo].[LabTests] ADD [Price] decimal(18, 2) NOT NULL CONSTRAINT [DF_LabTests_Price] DEFAULT (0);';
EXEC #EnsureColumn N'LabTests', N'NormalRange', N'ALTER TABLE [dbo].[LabTests] ADD [NormalRange] nvarchar(max) NOT NULL CONSTRAINT [DF_LabTests_NormalRange] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabTests', N'Unit', N'ALTER TABLE [dbo].[LabTests] ADD [Unit] nvarchar(max) NOT NULL CONSTRAINT [DF_LabTests_Unit] DEFAULT (N'''');';
EXEC #EnsureColumn N'LabTests', N'PreparationTimeHours', N'ALTER TABLE [dbo].[LabTests] ADD [PreparationTimeHours] int NOT NULL CONSTRAINT [DF_LabTests_PreparationTimeHours] DEFAULT (0);';
EXEC #EnsureColumn N'LabTests', N'IsActive', N'ALTER TABLE [dbo].[LabTests] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_LabTests_IsActive] DEFAULT (0);';
EXEC #EnsureColumn N'LabTests', N'CreatedDate', N'ALTER TABLE [dbo].[LabTests] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_LabTests_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- Languages
EXEC #EnsureColumn N'Languages', N'Id', N'ALTER TABLE [dbo].[Languages] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'Languages', N'Code', N'ALTER TABLE [dbo].[Languages] ADD [Code] nvarchar(max) NOT NULL CONSTRAINT [DF_Languages_Code] DEFAULT (N'''');';
EXEC #EnsureColumn N'Languages', N'Name', N'ALTER TABLE [dbo].[Languages] ADD [Name] nvarchar(max) NOT NULL CONSTRAINT [DF_Languages_Name] DEFAULT (N'''');';
EXEC #EnsureColumn N'Languages', N'NativeName', N'ALTER TABLE [dbo].[Languages] ADD [NativeName] nvarchar(max) NOT NULL CONSTRAINT [DF_Languages_NativeName] DEFAULT (N'''');';
EXEC #EnsureColumn N'Languages', N'IsActive', N'ALTER TABLE [dbo].[Languages] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_Languages_IsActive] DEFAULT (0);';
EXEC #EnsureColumn N'Languages', N'IsDefault', N'ALTER TABLE [dbo].[Languages] ADD [IsDefault] bit NOT NULL CONSTRAINT [DF_Languages_IsDefault] DEFAULT (0);';
EXEC #EnsureColumn N'Languages', N'DisplayOrder', N'ALTER TABLE [dbo].[Languages] ADD [DisplayOrder] int NOT NULL CONSTRAINT [DF_Languages_DisplayOrder] DEFAULT (0);';
EXEC #EnsureColumn N'Languages', N'CreatedDate', N'ALTER TABLE [dbo].[Languages] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Languages_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- LeaveBalances
EXEC #EnsureColumn N'LeaveBalances', N'Id', N'ALTER TABLE [dbo].[LeaveBalances] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'LeaveBalances', N'StaffId', N'ALTER TABLE [dbo].[LeaveBalances] ADD [StaffId] {{StaffId}} NOT NULL CONSTRAINT [DF_LeaveBalances_StaffId] DEFAULT (N'''');';
EXEC #EnsureColumn N'LeaveBalances', N'LeaveTypeId', N'ALTER TABLE [dbo].[LeaveBalances] ADD [LeaveTypeId] int NOT NULL CONSTRAINT [DF_LeaveBalances_LeaveTypeId] DEFAULT (0);';
EXEC #EnsureColumn N'LeaveBalances', N'Year', N'ALTER TABLE [dbo].[LeaveBalances] ADD [Year] int NOT NULL CONSTRAINT [DF_LeaveBalances_Year] DEFAULT (0);';
EXEC #EnsureColumn N'LeaveBalances', N'AllocatedDays', N'ALTER TABLE [dbo].[LeaveBalances] ADD [AllocatedDays] int NOT NULL CONSTRAINT [DF_LeaveBalances_AllocatedDays] DEFAULT (0);';
EXEC #EnsureColumn N'LeaveBalances', N'UsedDays', N'ALTER TABLE [dbo].[LeaveBalances] ADD [UsedDays] int NOT NULL CONSTRAINT [DF_LeaveBalances_UsedDays] DEFAULT (0);';
EXEC #EnsureColumn N'LeaveBalances', N'RemainingDays', N'ALTER TABLE [dbo].[LeaveBalances] ADD [RemainingDays] int NOT NULL CONSTRAINT [DF_LeaveBalances_RemainingDays] DEFAULT (0);';
EXEC #EnsureColumn N'LeaveBalances', N'CreatedDate', N'ALTER TABLE [dbo].[LeaveBalances] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_LeaveBalances_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'LeaveBalances', N'UpdatedDate', N'ALTER TABLE [dbo].[LeaveBalances] ADD [UpdatedDate] datetime2 NULL;';
GO

-- LeaveRequests
EXEC #EnsureColumn N'LeaveRequests', N'Id', N'ALTER TABLE [dbo].[LeaveRequests] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'LeaveRequests', N'StaffId', N'ALTER TABLE [dbo].[LeaveRequests] ADD [StaffId] {{StaffId}} NOT NULL CONSTRAINT [DF_LeaveRequests_StaffId] DEFAULT (N'''');';
EXEC #EnsureColumn N'LeaveRequests', N'LeaveTypeId', N'ALTER TABLE [dbo].[LeaveRequests] ADD [LeaveTypeId] int NOT NULL CONSTRAINT [DF_LeaveRequests_LeaveTypeId] DEFAULT (0);';
EXEC #EnsureColumn N'LeaveRequests', N'StartDate', N'ALTER TABLE [dbo].[LeaveRequests] ADD [StartDate] datetime2 NOT NULL CONSTRAINT [DF_LeaveRequests_StartDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'LeaveRequests', N'EndDate', N'ALTER TABLE [dbo].[LeaveRequests] ADD [EndDate] datetime2 NOT NULL CONSTRAINT [DF_LeaveRequests_EndDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'LeaveRequests', N'TotalDays', N'ALTER TABLE [dbo].[LeaveRequests] ADD [TotalDays] int NOT NULL CONSTRAINT [DF_LeaveRequests_TotalDays] DEFAULT (0);';
EXEC #EnsureColumn N'LeaveRequests', N'Status', N'ALTER TABLE [dbo].[LeaveRequests] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_LeaveRequests_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'LeaveRequests', N'Reason', N'ALTER TABLE [dbo].[LeaveRequests] ADD [Reason] nvarchar(max) NOT NULL CONSTRAINT [DF_LeaveRequests_Reason] DEFAULT (N'''');';
EXEC #EnsureColumn N'LeaveRequests', N'ApproverId', N'ALTER TABLE [dbo].[LeaveRequests] ADD [ApproverId] nvarchar(max) NOT NULL CONSTRAINT [DF_LeaveRequests_ApproverId] DEFAULT (N'''');';
EXEC #EnsureColumn N'LeaveRequests', N'ApprovedDate', N'ALTER TABLE [dbo].[LeaveRequests] ADD [ApprovedDate] datetime2 NULL;';
EXEC #EnsureColumn N'LeaveRequests', N'ApproverRemarks', N'ALTER TABLE [dbo].[LeaveRequests] ADD [ApproverRemarks] nvarchar(max) NOT NULL CONSTRAINT [DF_LeaveRequests_ApproverRemarks] DEFAULT (N'''');';
EXEC #EnsureColumn N'LeaveRequests', N'CreatedDate', N'ALTER TABLE [dbo].[LeaveRequests] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_LeaveRequests_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'LeaveRequests', N'UpdatedDate', N'ALTER TABLE [dbo].[LeaveRequests] ADD [UpdatedDate] datetime2 NULL;';
GO

-- LeaveTypes
EXEC #EnsureColumn N'LeaveTypes', N'Id', N'ALTER TABLE [dbo].[LeaveTypes] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'LeaveTypes', N'Name', N'ALTER TABLE [dbo].[LeaveTypes] ADD [Name] nvarchar(max) NOT NULL CONSTRAINT [DF_LeaveTypes_Name] DEFAULT (N'''');';
EXEC #EnsureColumn N'LeaveTypes', N'Description', N'ALTER TABLE [dbo].[LeaveTypes] ADD [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_LeaveTypes_Description] DEFAULT (N'''');';
EXEC #EnsureColumn N'LeaveTypes', N'DefaultDaysPerYear', N'ALTER TABLE [dbo].[LeaveTypes] ADD [DefaultDaysPerYear] int NOT NULL CONSTRAINT [DF_LeaveTypes_DefaultDaysPerYear] DEFAULT (0);';
EXEC #EnsureColumn N'LeaveTypes', N'IsActive', N'ALTER TABLE [dbo].[LeaveTypes] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_LeaveTypes_IsActive] DEFAULT (0);';
EXEC #EnsureColumn N'LeaveTypes', N'CreatedDate', N'ALTER TABLE [dbo].[LeaveTypes] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_LeaveTypes_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- LicenseAuditLogs
EXEC #EnsureColumn N'LicenseAuditLogs', N'Id', N'ALTER TABLE [dbo].[LicenseAuditLogs] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'LicenseAuditLogs', N'LicenseRecordId', N'ALTER TABLE [dbo].[LicenseAuditLogs] ADD [LicenseRecordId] int NOT NULL CONSTRAINT [DF_LicenseAuditLogs_LicenseRecordId] DEFAULT (0);';
EXEC #EnsureColumn N'LicenseAuditLogs', N'ActionType', N'ALTER TABLE [dbo].[LicenseAuditLogs] ADD [ActionType] nvarchar(50) NOT NULL CONSTRAINT [DF_LicenseAuditLogs_ActionType] DEFAULT (N'''');';
EXEC #EnsureColumn N'LicenseAuditLogs', N'PerformedByUserId', N'ALTER TABLE [dbo].[LicenseAuditLogs] ADD [PerformedByUserId] {{UserId}} NULL;';
EXEC #EnsureColumn N'LicenseAuditLogs', N'PerformedAtUtc', N'ALTER TABLE [dbo].[LicenseAuditLogs] ADD [PerformedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_LicenseAuditLogs_PerformedAtUtc] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'LicenseAuditLogs', N'OldExpiresAtUtc', N'ALTER TABLE [dbo].[LicenseAuditLogs] ADD [OldExpiresAtUtc] datetime2 NULL;';
EXEC #EnsureColumn N'LicenseAuditLogs', N'NewExpiresAtUtc', N'ALTER TABLE [dbo].[LicenseAuditLogs] ADD [NewExpiresAtUtc] datetime2 NULL;';
EXEC #EnsureColumn N'LicenseAuditLogs', N'RenewalTermYears', N'ALTER TABLE [dbo].[LicenseAuditLogs] ADD [RenewalTermYears] int NULL;';
EXEC #EnsureColumn N'LicenseAuditLogs', N'Details', N'ALTER TABLE [dbo].[LicenseAuditLogs] ADD [Details] nvarchar(2000) NULL;';
EXEC #EnsureColumn N'LicenseAuditLogs', N'IpAddress', N'ALTER TABLE [dbo].[LicenseAuditLogs] ADD [IpAddress] nvarchar(64) NULL;';
GO

-- LicenseRecords
EXEC #EnsureColumn N'LicenseRecords', N'Id', N'ALTER TABLE [dbo].[LicenseRecords] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'LicenseRecords', N'LicenseReference', N'ALTER TABLE [dbo].[LicenseRecords] ADD [LicenseReference] nvarchar(100) NOT NULL CONSTRAINT [DF_LicenseRecords_LicenseReference] DEFAULT (N'''');';
EXEC #EnsureColumn N'LicenseRecords', N'IssuedAtUtc', N'ALTER TABLE [dbo].[LicenseRecords] ADD [IssuedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_LicenseRecords_IssuedAtUtc] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'LicenseRecords', N'ExpiresAtUtc', N'ALTER TABLE [dbo].[LicenseRecords] ADD [ExpiresAtUtc] datetime2 NOT NULL CONSTRAINT [DF_LicenseRecords_ExpiresAtUtc] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'LicenseRecords', N'Status', N'ALTER TABLE [dbo].[LicenseRecords] ADD [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_LicenseRecords_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'LicenseRecords', N'LastReminderSentAtUtc', N'ALTER TABLE [dbo].[LicenseRecords] ADD [LastReminderSentAtUtc] datetime2 NULL;';
EXEC #EnsureColumn N'LicenseRecords', N'LastReminderCycleExpiryUtc', N'ALTER TABLE [dbo].[LicenseRecords] ADD [LastReminderCycleExpiryUtc] datetime2 NULL;';
EXEC #EnsureColumn N'LicenseRecords', N'RenewedByUserId', N'ALTER TABLE [dbo].[LicenseRecords] ADD [RenewedByUserId] nvarchar(128) NULL;';
EXEC #EnsureColumn N'LicenseRecords', N'RenewedAtUtc', N'ALTER TABLE [dbo].[LicenseRecords] ADD [RenewedAtUtc] datetime2 NULL;';
EXEC #EnsureColumn N'LicenseRecords', N'RenewalTermYears', N'ALTER TABLE [dbo].[LicenseRecords] ADD [RenewalTermYears] int NULL;';
EXEC #EnsureColumn N'LicenseRecords', N'Notes', N'ALTER TABLE [dbo].[LicenseRecords] ADD [Notes] nvarchar(1000) NULL;';
EXEC #EnsureColumn N'LicenseRecords', N'IsActive', N'ALTER TABLE [dbo].[LicenseRecords] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_LicenseRecords_IsActive] DEFAULT (0);';
EXEC #EnsureColumn N'LicenseRecords', N'CreatedAtUtc', N'ALTER TABLE [dbo].[LicenseRecords] ADD [CreatedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_LicenseRecords_CreatedAtUtc] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'LicenseRecords', N'UpdatedAtUtc', N'ALTER TABLE [dbo].[LicenseRecords] ADD [UpdatedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_LicenseRecords_UpdatedAtUtc] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'LicenseRecords', N'ProductName', N'ALTER TABLE [dbo].[LicenseRecords] ADD [ProductName] nvarchar(100) NOT NULL CONSTRAINT [DF_LicenseRecords_ProductName_Mig] DEFAULT (''MedyxHMS'');';
EXEC #EnsureColumn N'LicenseRecords', N'TenantId', N'ALTER TABLE [dbo].[LicenseRecords] ADD [TenantId] nvarchar(150) NOT NULL CONSTRAINT [DF_LicenseRecords_TenantId_Mig] DEFAULT (''UNCONFIGURED'');';
EXEC #EnsureColumn N'LicenseRecords', N'LicenseGuid', N'ALTER TABLE [dbo].[LicenseRecords] ADD [LicenseGuid] uniqueidentifier NOT NULL CONSTRAINT [DF_LicenseRecords_LicenseGuid_Mig] DEFAULT (''00000000-0000-0000-0000-000000000000'');';
EXEC #EnsureColumn N'LicenseRecords', N'MaxConcurrentUsers', N'ALTER TABLE [dbo].[LicenseRecords] ADD [MaxConcurrentUsers] int NOT NULL CONSTRAINT [DF_LicenseRecords_MaxConcurrentUsers_Mig] DEFAULT ((0));';
EXEC #EnsureColumn N'LicenseRecords', N'VerificationKey', N'ALTER TABLE [dbo].[LicenseRecords] ADD [VerificationKey] nvarchar(64) NOT NULL CONSTRAINT [DF_LicenseRecords_VerificationKey_Mig] DEFAULT ('''');';
EXEC #EnsureColumn N'LicenseRecords', N'LicensedModulesCsv', N'ALTER TABLE [dbo].[LicenseRecords] ADD [LicensedModulesCsv] nvarchar(max) NOT NULL CONSTRAINT [DF_LicenseRecords_LicensedModulesCsv_Mig] DEFAULT ('''');';
EXEC #EnsureColumn N'LicenseRecords', N'PublicKeyModulusHex', N'ALTER TABLE [dbo].[LicenseRecords] ADD [PublicKeyModulusHex] nvarchar(max) NOT NULL CONSTRAINT [DF_LicenseRecords_PublicKeyModulusHex_Mig] DEFAULT ('''');';
EXEC #EnsureColumn N'LicenseRecords', N'PublicKeyExponentHex', N'ALTER TABLE [dbo].[LicenseRecords] ADD [PublicKeyExponentHex] nvarchar(40) NOT NULL CONSTRAINT [DF_LicenseRecords_PublicKeyExponentHex_Mig] DEFAULT ('''');';
EXEC #EnsureColumn N'LicenseRecords', N'Nonce', N'ALTER TABLE [dbo].[LicenseRecords] ADD [Nonce] nvarchar(120) NOT NULL CONSTRAINT [DF_LicenseRecords_Nonce_Mig] DEFAULT (''N/A'');';
EXEC #EnsureColumn N'LicenseRecords', N'SignatureAlgorithm', N'ALTER TABLE [dbo].[LicenseRecords] ADD [SignatureAlgorithm] nvarchar(40) NOT NULL CONSTRAINT [DF_LicenseRecords_SignatureAlgorithm_Mig] DEFAULT (''RSA-SHA256'');';
EXEC #EnsureColumn N'LicenseRecords', N'SignatureHex', N'ALTER TABLE [dbo].[LicenseRecords] ADD [SignatureHex] nvarchar(max) NOT NULL CONSTRAINT [DF_LicenseRecords_SignatureHex_Mig] DEFAULT ('''');';
EXEC #EnsureColumn N'LicenseRecords', N'EncodedLicenseFile', N'ALTER TABLE [dbo].[LicenseRecords] ADD [EncodedLicenseFile] nvarchar(max) NOT NULL CONSTRAINT [DF_LicenseRecords_EncodedLicenseFile_Mig] DEFAULT ('''');';
EXEC #EnsureColumn N'LicenseRecords', N'CanonicalPayloadJson', N'ALTER TABLE [dbo].[LicenseRecords] ADD [CanonicalPayloadJson] nvarchar(max) NOT NULL CONSTRAINT [DF_LicenseRecords_CanonicalPayloadJson_Mig] DEFAULT ('''');';
EXEC #EnsureColumn N'LicenseRecords', N'PayloadSha256Hex', N'ALTER TABLE [dbo].[LicenseRecords] ADD [PayloadSha256Hex] nvarchar(64) NOT NULL CONSTRAINT [DF_LicenseRecords_PayloadSha256Hex_Mig] DEFAULT ('''');';
EXEC #EnsureColumn N'LicenseRecords', N'IsSignatureValid', N'ALTER TABLE [dbo].[LicenseRecords] ADD [IsSignatureValid] bit NOT NULL CONSTRAINT [DF_LicenseRecords_IsSignatureValid_Mig] DEFAULT ((0));';
EXEC #EnsureColumn N'LicenseRecords', N'LastValidatedAtUtc', N'ALTER TABLE [dbo].[LicenseRecords] ADD [LastValidatedAtUtc] datetime2 NULL;';
GO

-- LicenseReminderLogs
EXEC #EnsureColumn N'LicenseReminderLogs', N'Id', N'ALTER TABLE [dbo].[LicenseReminderLogs] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'LicenseReminderLogs', N'LicenseRecordId', N'ALTER TABLE [dbo].[LicenseReminderLogs] ADD [LicenseRecordId] int NOT NULL CONSTRAINT [DF_LicenseReminderLogs_LicenseRecordId] DEFAULT (0);';
EXEC #EnsureColumn N'LicenseReminderLogs', N'ReminderType', N'ALTER TABLE [dbo].[LicenseReminderLogs] ADD [ReminderType] nvarchar(50) NOT NULL CONSTRAINT [DF_LicenseReminderLogs_ReminderType] DEFAULT (N'''');';
EXEC #EnsureColumn N'LicenseReminderLogs', N'TargetExpiryUtc', N'ALTER TABLE [dbo].[LicenseReminderLogs] ADD [TargetExpiryUtc] datetime2 NOT NULL CONSTRAINT [DF_LicenseReminderLogs_TargetExpiryUtc] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'LicenseReminderLogs', N'TriggeredAtUtc', N'ALTER TABLE [dbo].[LicenseReminderLogs] ADD [TriggeredAtUtc] datetime2 NOT NULL CONSTRAINT [DF_LicenseReminderLogs_TriggeredAtUtc] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'LicenseReminderLogs', N'SentToCount', N'ALTER TABLE [dbo].[LicenseReminderLogs] ADD [SentToCount] int NOT NULL CONSTRAINT [DF_LicenseReminderLogs_SentToCount] DEFAULT (0);';
EXEC #EnsureColumn N'LicenseReminderLogs', N'Status', N'ALTER TABLE [dbo].[LicenseReminderLogs] ADD [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_LicenseReminderLogs_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'LicenseReminderLogs', N'ErrorMessage', N'ALTER TABLE [dbo].[LicenseReminderLogs] ADD [ErrorMessage] nvarchar(2000) NULL;';
GO

-- LiveConsultationSessions
EXEC #EnsureColumn N'LiveConsultationSessions', N'Id', N'ALTER TABLE [dbo].[LiveConsultationSessions] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'LiveConsultationSessions', N'PatientId', N'ALTER TABLE [dbo].[LiveConsultationSessions] ADD [PatientId] int NULL;';
EXEC #EnsureColumn N'LiveConsultationSessions', N'DoctorUserId', N'ALTER TABLE [dbo].[LiveConsultationSessions] ADD [DoctorUserId] nvarchar(128) NOT NULL CONSTRAINT [DF__LiveConsu__Docto__3F9B6DFF] DEFAULT ('''');';
EXEC #EnsureColumn N'LiveConsultationSessions', N'DoctorName', N'ALTER TABLE [dbo].[LiveConsultationSessions] ADD [DoctorName] nvarchar(max) NOT NULL CONSTRAINT [DF__LiveConsu__Docto__408F9238] DEFAULT ('''');';
EXEC #EnsureColumn N'LiveConsultationSessions', N'PatientName', N'ALTER TABLE [dbo].[LiveConsultationSessions] ADD [PatientName] nvarchar(max) NOT NULL CONSTRAINT [DF__LiveConsu__Patie__4183B671] DEFAULT ('''');';
EXEC #EnsureColumn N'LiveConsultationSessions', N'ScheduledAt', N'ALTER TABLE [dbo].[LiveConsultationSessions] ADD [ScheduledAt] datetime2 NOT NULL CONSTRAINT [DF_LiveConsultationSessions_ScheduledAt] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'LiveConsultationSessions', N'DurationMinutes', N'ALTER TABLE [dbo].[LiveConsultationSessions] ADD [DurationMinutes] int NOT NULL CONSTRAINT [DF__LiveConsu__Durat__4277DAAA] DEFAULT ((30));';
EXEC #EnsureColumn N'LiveConsultationSessions', N'Platform', N'ALTER TABLE [dbo].[LiveConsultationSessions] ADD [Platform] nvarchar(max) NOT NULL CONSTRAINT [DF__LiveConsu__Platf__436BFEE3] DEFAULT (''Zoom'');';
EXEC #EnsureColumn N'LiveConsultationSessions', N'MeetingLink', N'ALTER TABLE [dbo].[LiveConsultationSessions] ADD [MeetingLink] nvarchar(max) NOT NULL CONSTRAINT [DF__LiveConsu__Meeti__4460231C] DEFAULT ('''');';
EXEC #EnsureColumn N'LiveConsultationSessions', N'MeetingId', N'ALTER TABLE [dbo].[LiveConsultationSessions] ADD [MeetingId] nvarchar(max) NOT NULL CONSTRAINT [DF__LiveConsu__Meeti__45544755] DEFAULT ('''');';
EXEC #EnsureColumn N'LiveConsultationSessions', N'MeetingPassword', N'ALTER TABLE [dbo].[LiveConsultationSessions] ADD [MeetingPassword] nvarchar(max) NOT NULL CONSTRAINT [DF__LiveConsu__Meeti__46486B8E] DEFAULT ('''');';
EXEC #EnsureColumn N'LiveConsultationSessions', N'Status', N'ALTER TABLE [dbo].[LiveConsultationSessions] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF__LiveConsu__Statu__473C8FC7] DEFAULT (''Scheduled'');';
EXEC #EnsureColumn N'LiveConsultationSessions', N'Notes', N'ALTER TABLE [dbo].[LiveConsultationSessions] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF__LiveConsu__Notes__4830B400] DEFAULT ('''');';
EXEC #EnsureColumn N'LiveConsultationSessions', N'BillId', N'ALTER TABLE [dbo].[LiveConsultationSessions] ADD [BillId] int NULL;';
EXEC #EnsureColumn N'LiveConsultationSessions', N'CreatedDate', N'ALTER TABLE [dbo].[LiveConsultationSessions] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_LiveConsultationSessions_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- MedicalRecords
EXEC #EnsureColumn N'MedicalRecords', N'Id', N'ALTER TABLE [dbo].[MedicalRecords] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'MedicalRecords', N'PatientId', N'ALTER TABLE [dbo].[MedicalRecords] ADD [PatientId] int NOT NULL CONSTRAINT [DF_MedicalRecords_PatientId] DEFAULT (0);';
EXEC #EnsureColumn N'MedicalRecords', N'RecordType', N'ALTER TABLE [dbo].[MedicalRecords] ADD [RecordType] nvarchar(100) NOT NULL CONSTRAINT [DF_MedicalRecords_RecordType] DEFAULT (N'''');';
EXEC #EnsureColumn N'MedicalRecords', N'Description', N'ALTER TABLE [dbo].[MedicalRecords] ADD [Description] nvarchar(500) NOT NULL CONSTRAINT [DF_MedicalRecords_Description] DEFAULT (N'''');';
EXEC #EnsureColumn N'MedicalRecords', N'Diagnosis', N'ALTER TABLE [dbo].[MedicalRecords] ADD [Diagnosis] nvarchar(1000) NOT NULL CONSTRAINT [DF_MedicalRecords_Diagnosis] DEFAULT (N'''');';
EXEC #EnsureColumn N'MedicalRecords', N'Treatment', N'ALTER TABLE [dbo].[MedicalRecords] ADD [Treatment] nvarchar(2000) NOT NULL CONSTRAINT [DF_MedicalRecords_Treatment] DEFAULT (N'''');';
EXEC #EnsureColumn N'MedicalRecords', N'Notes', N'ALTER TABLE [dbo].[MedicalRecords] ADD [Notes] nvarchar(1000) NOT NULL CONSTRAINT [DF_MedicalRecords_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'MedicalRecords', N'DoctorName', N'ALTER TABLE [dbo].[MedicalRecords] ADD [DoctorName] nvarchar(100) NOT NULL CONSTRAINT [DF_MedicalRecords_DoctorName] DEFAULT (N'''');';
EXEC #EnsureColumn N'MedicalRecords', N'DoctorId', N'ALTER TABLE [dbo].[MedicalRecords] ADD [DoctorId] {{UserId}} NULL;';
EXEC #EnsureColumn N'MedicalRecords', N'RecordDate', N'ALTER TABLE [dbo].[MedicalRecords] ADD [RecordDate] datetime2 NOT NULL CONSTRAINT [DF_MedicalRecords_RecordDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'MedicalRecords', N'CreatedDate', N'ALTER TABLE [dbo].[MedicalRecords] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_MedicalRecords_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'MedicalRecords', N'CreatedBy', N'ALTER TABLE [dbo].[MedicalRecords] ADD [CreatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_MedicalRecords_CreatedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'MedicalRecords', N'ModifiedDate', N'ALTER TABLE [dbo].[MedicalRecords] ADD [ModifiedDate] datetime2 NULL;';
EXEC #EnsureColumn N'MedicalRecords', N'ModifiedBy', N'ALTER TABLE [dbo].[MedicalRecords] ADD [ModifiedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_MedicalRecords_ModifiedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'MedicalRecords', N'SourceType', N'ALTER TABLE [dbo].[MedicalRecords] ADD [SourceType] nvarchar(30) NOT NULL CONSTRAINT [DF_MedicalRecords_SourceType] DEFAULT (N'''');';
EXEC #EnsureColumn N'MedicalRecords', N'SourceId', N'ALTER TABLE [dbo].[MedicalRecords] ADD [SourceId] int NULL;';
GO

-- Medicines
EXEC #EnsureColumn N'Medicines', N'Id', N'ALTER TABLE [dbo].[Medicines] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'Medicines', N'Name', N'ALTER TABLE [dbo].[Medicines] ADD [Name] nvarchar(max) NOT NULL CONSTRAINT [DF_Medicines_Name] DEFAULT (N'''');';
EXEC #EnsureColumn N'Medicines', N'GenericName', N'ALTER TABLE [dbo].[Medicines] ADD [GenericName] nvarchar(max) NOT NULL CONSTRAINT [DF_Medicines_GenericName] DEFAULT (N'''');';
EXEC #EnsureColumn N'Medicines', N'Category', N'ALTER TABLE [dbo].[Medicines] ADD [Category] nvarchar(max) NOT NULL CONSTRAINT [DF_Medicines_Category] DEFAULT (N'''');';
EXEC #EnsureColumn N'Medicines', N'DosageForm', N'ALTER TABLE [dbo].[Medicines] ADD [DosageForm] nvarchar(max) NOT NULL CONSTRAINT [DF_Medicines_DosageForm] DEFAULT (N'''');';
EXEC #EnsureColumn N'Medicines', N'Strength', N'ALTER TABLE [dbo].[Medicines] ADD [Strength] nvarchar(max) NOT NULL CONSTRAINT [DF_Medicines_Strength] DEFAULT (N'''');';
EXEC #EnsureColumn N'Medicines', N'Manufacturer', N'ALTER TABLE [dbo].[Medicines] ADD [Manufacturer] nvarchar(max) NOT NULL CONSTRAINT [DF_Medicines_Manufacturer] DEFAULT (N'''');';
EXEC #EnsureColumn N'Medicines', N'UnitPrice', N'ALTER TABLE [dbo].[Medicines] ADD [UnitPrice] decimal(18, 2) NOT NULL CONSTRAINT [DF_Medicines_UnitPrice] DEFAULT (0);';
EXEC #EnsureColumn N'Medicines', N'StockQuantity', N'ALTER TABLE [dbo].[Medicines] ADD [StockQuantity] int NOT NULL CONSTRAINT [DF_Medicines_StockQuantity] DEFAULT (0);';
EXEC #EnsureColumn N'Medicines', N'MinStockLevel', N'ALTER TABLE [dbo].[Medicines] ADD [MinStockLevel] int NOT NULL CONSTRAINT [DF_Medicines_MinStockLevel] DEFAULT (0);';
EXEC #EnsureColumn N'Medicines', N'ExpiryDate', N'ALTER TABLE [dbo].[Medicines] ADD [ExpiryDate] datetime2 NOT NULL CONSTRAINT [DF_Medicines_ExpiryDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'Medicines', N'BatchNumber', N'ALTER TABLE [dbo].[Medicines] ADD [BatchNumber] nvarchar(max) NOT NULL CONSTRAINT [DF_Medicines_BatchNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'Medicines', N'IsActive', N'ALTER TABLE [dbo].[Medicines] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_Medicines_IsActive] DEFAULT (0);';
EXEC #EnsureColumn N'Medicines', N'CreatedDate', N'ALTER TABLE [dbo].[Medicines] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Medicines_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- NotificationDeliveryLogs
EXEC #EnsureColumn N'NotificationDeliveryLogs', N'Id', N'ALTER TABLE [dbo].[NotificationDeliveryLogs] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'NotificationDeliveryLogs', N'Channel', N'ALTER TABLE [dbo].[NotificationDeliveryLogs] ADD [Channel] nvarchar(20) NOT NULL CONSTRAINT [DF_NotificationDeliveryLogs_Channel] DEFAULT (N'''');';
EXEC #EnsureColumn N'NotificationDeliveryLogs', N'Provider', N'ALTER TABLE [dbo].[NotificationDeliveryLogs] ADD [Provider] nvarchar(50) NOT NULL CONSTRAINT [DF_NotificationDeliveryLogs_Provider] DEFAULT (N'''');';
EXEC #EnsureColumn N'NotificationDeliveryLogs', N'Recipient', N'ALTER TABLE [dbo].[NotificationDeliveryLogs] ADD [Recipient] nvarchar(200) NOT NULL CONSTRAINT [DF_NotificationDeliveryLogs_Recipient] DEFAULT (N'''');';
EXEC #EnsureColumn N'NotificationDeliveryLogs', N'Subject', N'ALTER TABLE [dbo].[NotificationDeliveryLogs] ADD [Subject] nvarchar(200) NOT NULL CONSTRAINT [DF_NotificationDeliveryLogs_Subject] DEFAULT (N'''');';
EXEC #EnsureColumn N'NotificationDeliveryLogs', N'MessageBody', N'ALTER TABLE [dbo].[NotificationDeliveryLogs] ADD [MessageBody] nvarchar(max) NOT NULL CONSTRAINT [DF_NotificationDeliveryLogs_MessageBody] DEFAULT (N'''');';
EXEC #EnsureColumn N'NotificationDeliveryLogs', N'Status', N'ALTER TABLE [dbo].[NotificationDeliveryLogs] ADD [Status] nvarchar(20) NOT NULL CONSTRAINT [DF_NotificationDeliveryLogs_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'NotificationDeliveryLogs', N'ProviderResponse', N'ALTER TABLE [dbo].[NotificationDeliveryLogs] ADD [ProviderResponse] nvarchar(2000) NOT NULL CONSTRAINT [DF_NotificationDeliveryLogs_ProviderResponse] DEFAULT (N'''');';
EXEC #EnsureColumn N'NotificationDeliveryLogs', N'RelatedEntityType', N'ALTER TABLE [dbo].[NotificationDeliveryLogs] ADD [RelatedEntityType] nvarchar(50) NOT NULL CONSTRAINT [DF_NotificationDeliveryLogs_RelatedEntityType] DEFAULT (N'''');';
EXEC #EnsureColumn N'NotificationDeliveryLogs', N'RelatedEntityId', N'ALTER TABLE [dbo].[NotificationDeliveryLogs] ADD [RelatedEntityId] nvarchar(100) NOT NULL CONSTRAINT [DF_NotificationDeliveryLogs_RelatedEntityId] DEFAULT (N'''');';
EXEC #EnsureColumn N'NotificationDeliveryLogs', N'IsTest', N'ALTER TABLE [dbo].[NotificationDeliveryLogs] ADD [IsTest] bit NOT NULL CONSTRAINT [DF_NotificationDeliveryLogs_IsTest] DEFAULT (0);';
EXEC #EnsureColumn N'NotificationDeliveryLogs', N'CreatedAt', N'ALTER TABLE [dbo].[NotificationDeliveryLogs] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_NotificationDeliveryLogs_CreatedAt] DEFAULT (''1900-01-01'');';
GO

-- OPDVisits
EXEC #EnsureColumn N'OPDVisits', N'Id', N'ALTER TABLE [dbo].[OPDVisits] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'OPDVisits', N'PatientId', N'ALTER TABLE [dbo].[OPDVisits] ADD [PatientId] int NOT NULL CONSTRAINT [DF_OPDVisits_PatientId] DEFAULT (0);';
EXEC #EnsureColumn N'OPDVisits', N'DoctorId', N'ALTER TABLE [dbo].[OPDVisits] ADD [DoctorId] int NOT NULL CONSTRAINT [DF_OPDVisits_DoctorId] DEFAULT (0);';
EXEC #EnsureColumn N'OPDVisits', N'VisitDate', N'ALTER TABLE [dbo].[OPDVisits] ADD [VisitDate] datetime2 NOT NULL CONSTRAINT [DF_OPDVisits_VisitDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'OPDVisits', N'Symptoms', N'ALTER TABLE [dbo].[OPDVisits] ADD [Symptoms] nvarchar(max) NOT NULL CONSTRAINT [DF_OPDVisits_Symptoms] DEFAULT (N'''');';
EXEC #EnsureColumn N'OPDVisits', N'Diagnosis', N'ALTER TABLE [dbo].[OPDVisits] ADD [Diagnosis] nvarchar(max) NOT NULL CONSTRAINT [DF_OPDVisits_Diagnosis] DEFAULT (N'''');';
EXEC #EnsureColumn N'OPDVisits', N'Treatment', N'ALTER TABLE [dbo].[OPDVisits] ADD [Treatment] nvarchar(max) NOT NULL CONSTRAINT [DF_OPDVisits_Treatment] DEFAULT (N'''');';
EXEC #EnsureColumn N'OPDVisits', N'Prescription', N'ALTER TABLE [dbo].[OPDVisits] ADD [Prescription] nvarchar(max) NOT NULL CONSTRAINT [DF_OPDVisits_Prescription] DEFAULT (N'''');';
EXEC #EnsureColumn N'OPDVisits', N'Notes', N'ALTER TABLE [dbo].[OPDVisits] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF_OPDVisits_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'OPDVisits', N'ConsultationFee', N'ALTER TABLE [dbo].[OPDVisits] ADD [ConsultationFee] decimal(18, 2) NOT NULL CONSTRAINT [DF_OPDVisits_ConsultationFee] DEFAULT (0);';
EXEC #EnsureColumn N'OPDVisits', N'PaymentStatus', N'ALTER TABLE [dbo].[OPDVisits] ADD [PaymentStatus] nvarchar(max) NOT NULL CONSTRAINT [DF_OPDVisits_PaymentStatus] DEFAULT (N'''');';
EXEC #EnsureColumn N'OPDVisits', N'CreatedDate', N'ALTER TABLE [dbo].[OPDVisits] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_OPDVisits_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'OPDVisits', N'CreatedBy', N'ALTER TABLE [dbo].[OPDVisits] ADD [CreatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_OPDVisits_CreatedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'OPDVisits', N'MedicalRecordId', N'ALTER TABLE [dbo].[OPDVisits] ADD [MedicalRecordId] int NULL;';
EXEC #EnsureColumn N'OPDVisits', N'HospitalId', N'ALTER TABLE [dbo].[OPDVisits] ADD [HospitalId] int NULL;';
GO

-- OperationTheatres
EXEC #EnsureColumn N'OperationTheatres', N'Id', N'ALTER TABLE [dbo].[OperationTheatres] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'OperationTheatres', N'HospitalId', N'ALTER TABLE [dbo].[OperationTheatres] ADD [HospitalId] int NULL;';
EXEC #EnsureColumn N'OperationTheatres', N'Code', N'ALTER TABLE [dbo].[OperationTheatres] ADD [Code] nvarchar(20) NOT NULL CONSTRAINT [DF_OperationTheatres_Code] DEFAULT (N'''');';
EXEC #EnsureColumn N'OperationTheatres', N'Name', N'ALTER TABLE [dbo].[OperationTheatres] ADD [Name] nvarchar(100) NOT NULL CONSTRAINT [DF_OperationTheatres_Name] DEFAULT (N'''');';
EXEC #EnsureColumn N'OperationTheatres', N'Location', N'ALTER TABLE [dbo].[OperationTheatres] ADD [Location] nvarchar(100) NOT NULL CONSTRAINT [DF_OperationTheatres_Location] DEFAULT (N'''');';
EXEC #EnsureColumn N'OperationTheatres', N'TheatreType', N'ALTER TABLE [dbo].[OperationTheatres] ADD [TheatreType] nvarchar(50) NOT NULL CONSTRAINT [DF_OperationTheatres_TheatreType] DEFAULT (N'''');';
EXEC #EnsureColumn N'OperationTheatres', N'Features', N'ALTER TABLE [dbo].[OperationTheatres] ADD [Features] nvarchar(500) NOT NULL CONSTRAINT [DF_OperationTheatres_Features] DEFAULT (N'''');';
EXEC #EnsureColumn N'OperationTheatres', N'OpensAt', N'ALTER TABLE [dbo].[OperationTheatres] ADD [OpensAt] time NOT NULL CONSTRAINT [DF_OperationTheatres_OpensAt] DEFAULT (''00:00'');';
EXEC #EnsureColumn N'OperationTheatres', N'ClosesAt', N'ALTER TABLE [dbo].[OperationTheatres] ADD [ClosesAt] time NOT NULL CONSTRAINT [DF_OperationTheatres_ClosesAt] DEFAULT (''00:00'');';
EXEC #EnsureColumn N'OperationTheatres', N'Is24Hours', N'ALTER TABLE [dbo].[OperationTheatres] ADD [Is24Hours] bit NOT NULL CONSTRAINT [DF_OperationTheatres_Is24Hours] DEFAULT (0);';
EXEC #EnsureColumn N'OperationTheatres', N'TurnoverMinutes', N'ALTER TABLE [dbo].[OperationTheatres] ADD [TurnoverMinutes] int NOT NULL CONSTRAINT [DF_OperationTheatres_TurnoverMinutes] DEFAULT (0);';
EXEC #EnsureColumn N'OperationTheatres', N'IsActive', N'ALTER TABLE [dbo].[OperationTheatres] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_OperationTheatres_IsActive] DEFAULT (0);';
EXEC #EnsureColumn N'OperationTheatres', N'CreatedDate', N'ALTER TABLE [dbo].[OperationTheatres] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_OperationTheatres_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- OTBlocks
EXEC #EnsureColumn N'OTBlocks', N'Id', N'ALTER TABLE [dbo].[OTBlocks] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'OTBlocks', N'OperationTheatreId', N'ALTER TABLE [dbo].[OTBlocks] ADD [OperationTheatreId] int NOT NULL CONSTRAINT [DF_OTBlocks_OperationTheatreId] DEFAULT (0);';
EXEC #EnsureColumn N'OTBlocks', N'StartsAt', N'ALTER TABLE [dbo].[OTBlocks] ADD [StartsAt] datetime2 NOT NULL CONSTRAINT [DF_OTBlocks_StartsAt] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'OTBlocks', N'EndsAt', N'ALTER TABLE [dbo].[OTBlocks] ADD [EndsAt] datetime2 NOT NULL CONSTRAINT [DF_OTBlocks_EndsAt] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'OTBlocks', N'Reason', N'ALTER TABLE [dbo].[OTBlocks] ADD [Reason] nvarchar(200) NOT NULL CONSTRAINT [DF_OTBlocks_Reason] DEFAULT (N'''');';
EXEC #EnsureColumn N'OTBlocks', N'CreatedBy', N'ALTER TABLE [dbo].[OTBlocks] ADD [CreatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_OTBlocks_CreatedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'OTBlocks', N'CreatedAt', N'ALTER TABLE [dbo].[OTBlocks] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_OTBlocks_CreatedAt] DEFAULT (''1900-01-01'');';
GO

-- OTSchedules
EXEC #EnsureColumn N'OTSchedules', N'Id', N'ALTER TABLE [dbo].[OTSchedules] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'OTSchedules', N'PatientId', N'ALTER TABLE [dbo].[OTSchedules] ADD [PatientId] int NOT NULL CONSTRAINT [DF_OTSchedules_PatientId] DEFAULT (0);';
EXEC #EnsureColumn N'OTSchedules', N'ProcedureName', N'ALTER TABLE [dbo].[OTSchedules] ADD [ProcedureName] nvarchar(max) NOT NULL CONSTRAINT [DF_OTSchedules_ProcedureName] DEFAULT (N'''');';
EXEC #EnsureColumn N'OTSchedules', N'SurgeonName', N'ALTER TABLE [dbo].[OTSchedules] ADD [SurgeonName] nvarchar(max) NOT NULL CONSTRAINT [DF_OTSchedules_SurgeonName] DEFAULT (N'''');';
EXEC #EnsureColumn N'OTSchedules', N'ScheduledDate', N'ALTER TABLE [dbo].[OTSchedules] ADD [ScheduledDate] datetime2 NOT NULL CONSTRAINT [DF_OTSchedules_ScheduledDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'OTSchedules', N'EstimatedDurationMinutes', N'ALTER TABLE [dbo].[OTSchedules] ADD [EstimatedDurationMinutes] int NOT NULL CONSTRAINT [DF_OTSchedules_EstimatedDurationMinutes] DEFAULT (0);';
EXEC #EnsureColumn N'OTSchedules', N'OperationTheatreNumber', N'ALTER TABLE [dbo].[OTSchedules] ADD [OperationTheatreNumber] nvarchar(max) NOT NULL CONSTRAINT [DF_OTSchedules_OperationTheatreNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'OTSchedules', N'Status', N'ALTER TABLE [dbo].[OTSchedules] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_OTSchedules_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'OTSchedules', N'Notes', N'ALTER TABLE [dbo].[OTSchedules] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF_OTSchedules_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'OTSchedules', N'BillId', N'ALTER TABLE [dbo].[OTSchedules] ADD [BillId] int NULL;';
EXEC #EnsureColumn N'OTSchedules', N'CreatedDate', N'ALTER TABLE [dbo].[OTSchedules] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_OTSchedules_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'OTSchedules', N'HospitalId', N'ALTER TABLE [dbo].[OTSchedules] ADD [HospitalId] int NULL;';
EXEC #EnsureColumn N'OTSchedules', N'OperationTheatreId', N'ALTER TABLE [dbo].[OTSchedules] ADD [OperationTheatreId] int NULL;';
EXEC #EnsureColumn N'OTSchedules', N'SurgeonDoctorId', N'ALTER TABLE [dbo].[OTSchedules] ADD [SurgeonDoctorId] int NULL;';
EXEC #EnsureColumn N'OTSchedules', N'IsEmergency', N'ALTER TABLE [dbo].[OTSchedules] ADD [IsEmergency] bit NOT NULL CONSTRAINT [DF_OTSchedules_IsEmergency] DEFAULT ((0));';
EXEC #EnsureColumn N'OTSchedules', N'CancelReason', N'ALTER TABLE [dbo].[OTSchedules] ADD [CancelReason] nvarchar(max) NOT NULL CONSTRAINT [DF_OTSchedules_CancelReason] DEFAULT (N'''');';
EXEC #EnsureColumn N'OTSchedules', N'StartedAt', N'ALTER TABLE [dbo].[OTSchedules] ADD [StartedAt] datetime2 NULL;';
EXEC #EnsureColumn N'OTSchedules', N'CompletedAt', N'ALTER TABLE [dbo].[OTSchedules] ADD [CompletedAt] datetime2 NULL;';
GO

-- PatientDocuments
EXEC #EnsureColumn N'PatientDocuments', N'Id', N'ALTER TABLE [dbo].[PatientDocuments] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'PatientDocuments', N'PatientId', N'ALTER TABLE [dbo].[PatientDocuments] ADD [PatientId] int NOT NULL CONSTRAINT [DF_PatientDocuments_PatientId] DEFAULT (0);';
EXEC #EnsureColumn N'PatientDocuments', N'Category', N'ALTER TABLE [dbo].[PatientDocuments] ADD [Category] nvarchar(50) NOT NULL CONSTRAINT [DF_PatientDocuments_Category] DEFAULT (N'''');';
EXEC #EnsureColumn N'PatientDocuments', N'Title', N'ALTER TABLE [dbo].[PatientDocuments] ADD [Title] nvarchar(200) NOT NULL CONSTRAINT [DF_PatientDocuments_Title] DEFAULT (N'''');';
EXEC #EnsureColumn N'PatientDocuments', N'Description', N'ALTER TABLE [dbo].[PatientDocuments] ADD [Description] nvarchar(1000) NOT NULL CONSTRAINT [DF_PatientDocuments_Description] DEFAULT (N'''');';
EXEC #EnsureColumn N'PatientDocuments', N'DocumentDate', N'ALTER TABLE [dbo].[PatientDocuments] ADD [DocumentDate] datetime2 NULL;';
EXEC #EnsureColumn N'PatientDocuments', N'OriginalFileName', N'ALTER TABLE [dbo].[PatientDocuments] ADD [OriginalFileName] nvarchar(255) NOT NULL CONSTRAINT [DF_PatientDocuments_OriginalFileName] DEFAULT (N'''');';
EXEC #EnsureColumn N'PatientDocuments', N'StoredFileName', N'ALTER TABLE [dbo].[PatientDocuments] ADD [StoredFileName] nvarchar(100) NOT NULL CONSTRAINT [DF_PatientDocuments_StoredFileName] DEFAULT (N'''');';
EXEC #EnsureColumn N'PatientDocuments', N'ContentType', N'ALTER TABLE [dbo].[PatientDocuments] ADD [ContentType] nvarchar(100) NOT NULL CONSTRAINT [DF_PatientDocuments_ContentType] DEFAULT (N'''');';
EXEC #EnsureColumn N'PatientDocuments', N'SizeBytes', N'ALTER TABLE [dbo].[PatientDocuments] ADD [SizeBytes] bigint NOT NULL CONSTRAINT [DF_PatientDocuments_SizeBytes] DEFAULT (0);';
EXEC #EnsureColumn N'PatientDocuments', N'Sha256', N'ALTER TABLE [dbo].[PatientDocuments] ADD [Sha256] nvarchar(64) NOT NULL CONSTRAINT [DF_PatientDocuments_Sha256] DEFAULT (N'''');';
EXEC #EnsureColumn N'PatientDocuments', N'UploadedByPatient', N'ALTER TABLE [dbo].[PatientDocuments] ADD [UploadedByPatient] bit NOT NULL CONSTRAINT [DF_PatientDocuments_UploadedByPatient] DEFAULT (0);';
EXEC #EnsureColumn N'PatientDocuments', N'SharedWithPatient', N'ALTER TABLE [dbo].[PatientDocuments] ADD [SharedWithPatient] bit NOT NULL CONSTRAINT [DF_PatientDocuments_SharedWithPatient] DEFAULT (0);';
EXEC #EnsureColumn N'PatientDocuments', N'UploadedByUserId', N'ALTER TABLE [dbo].[PatientDocuments] ADD [UploadedByUserId] nvarchar(max) NOT NULL CONSTRAINT [DF_PatientDocuments_UploadedByUserId] DEFAULT (N'''');';
EXEC #EnsureColumn N'PatientDocuments', N'UploadedBy', N'ALTER TABLE [dbo].[PatientDocuments] ADD [UploadedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_PatientDocuments_UploadedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'PatientDocuments', N'UploadedAt', N'ALTER TABLE [dbo].[PatientDocuments] ADD [UploadedAt] datetime2 NOT NULL CONSTRAINT [DF_PatientDocuments_UploadedAt] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'PatientDocuments', N'IsDeleted', N'ALTER TABLE [dbo].[PatientDocuments] ADD [IsDeleted] bit NOT NULL CONSTRAINT [DF_PatientDocuments_IsDeleted] DEFAULT (0);';
EXEC #EnsureColumn N'PatientDocuments', N'DeletedBy', N'ALTER TABLE [dbo].[PatientDocuments] ADD [DeletedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_PatientDocuments_DeletedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'PatientDocuments', N'DeletedAt', N'ALTER TABLE [dbo].[PatientDocuments] ADD [DeletedAt] datetime2 NULL;';
EXEC #EnsureColumn N'PatientDocuments', N'DeleteReason', N'ALTER TABLE [dbo].[PatientDocuments] ADD [DeleteReason] nvarchar(300) NOT NULL CONSTRAINT [DF_PatientDocuments_DeleteReason] DEFAULT (N'''');';
GO

-- PatientInsurances
EXEC #EnsureColumn N'PatientInsurances', N'Id', N'ALTER TABLE [dbo].[PatientInsurances] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'PatientInsurances', N'PatientId', N'ALTER TABLE [dbo].[PatientInsurances] ADD [PatientId] int NOT NULL CONSTRAINT [DF_PatientInsurances_PatientId] DEFAULT (0);';
EXEC #EnsureColumn N'PatientInsurances', N'ProviderName', N'ALTER TABLE [dbo].[PatientInsurances] ADD [ProviderName] nvarchar(100) NOT NULL CONSTRAINT [DF__PatientIn__Provi__39237A9A] DEFAULT ('''');';
EXEC #EnsureColumn N'PatientInsurances', N'PolicyNumber', N'ALTER TABLE [dbo].[PatientInsurances] ADD [PolicyNumber] nvarchar(100) NOT NULL CONSTRAINT [DF__PatientIn__Polic__3A179ED3] DEFAULT ('''');';
EXEC #EnsureColumn N'PatientInsurances', N'InsurancePlan', N'ALTER TABLE [dbo].[PatientInsurances] ADD [InsurancePlan] nvarchar(100) NOT NULL CONSTRAINT [DF__PatientIn__Insur__3B0BC30C] DEFAULT ('''');';
EXEC #EnsureColumn N'PatientInsurances', N'HolderName', N'ALTER TABLE [dbo].[PatientInsurances] ADD [HolderName] nvarchar(100) NOT NULL CONSTRAINT [DF__PatientIn__Holde__3BFFE745] DEFAULT ('''');';
EXEC #EnsureColumn N'PatientInsurances', N'ValidFrom', N'ALTER TABLE [dbo].[PatientInsurances] ADD [ValidFrom] datetime2 NULL;';
EXEC #EnsureColumn N'PatientInsurances', N'ValidTo', N'ALTER TABLE [dbo].[PatientInsurances] ADD [ValidTo] datetime2 NULL;';
EXEC #EnsureColumn N'PatientInsurances', N'ContactNumber', N'ALTER TABLE [dbo].[PatientInsurances] ADD [ContactNumber] nvarchar(100) NOT NULL CONSTRAINT [DF__PatientIn__Conta__3CF40B7E] DEFAULT ('''');';
EXEC #EnsureColumn N'PatientInsurances', N'Notes', N'ALTER TABLE [dbo].[PatientInsurances] ADD [Notes] nvarchar(1000) NOT NULL CONSTRAINT [DF__PatientIn__Notes__3DE82FB7] DEFAULT ('''');';
EXEC #EnsureColumn N'PatientInsurances', N'IsActive', N'ALTER TABLE [dbo].[PatientInsurances] ADD [IsActive] bit NOT NULL CONSTRAINT [DF__PatientIn__IsAct__3EDC53F0] DEFAULT ((1));';
EXEC #EnsureColumn N'PatientInsurances', N'CreatedAtUtc', N'ALTER TABLE [dbo].[PatientInsurances] ADD [CreatedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_PatientInsurances_CreatedAtUtc] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'PatientInsurances', N'UpdatedAtUtc', N'ALTER TABLE [dbo].[PatientInsurances] ADD [UpdatedAtUtc] datetime2 NULL;';
GO

-- Patients
EXEC #EnsureColumn N'Patients', N'Id', N'ALTER TABLE [dbo].[Patients] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'Patients', N'PatientId', N'ALTER TABLE [dbo].[Patients] ADD [PatientId] nvarchar(max) NOT NULL CONSTRAINT [DF_Patients_PatientId] DEFAULT (N'''');';
EXEC #EnsureColumn N'Patients', N'FirstName', N'ALTER TABLE [dbo].[Patients] ADD [FirstName] nvarchar(max) NOT NULL CONSTRAINT [DF_Patients_FirstName] DEFAULT (N'''');';
EXEC #EnsureColumn N'Patients', N'LastName', N'ALTER TABLE [dbo].[Patients] ADD [LastName] nvarchar(max) NOT NULL CONSTRAINT [DF_Patients_LastName] DEFAULT (N'''');';
EXEC #EnsureColumn N'Patients', N'Email', N'ALTER TABLE [dbo].[Patients] ADD [Email] nvarchar(max) NOT NULL CONSTRAINT [DF_Patients_Email] DEFAULT (N'''');';
EXEC #EnsureColumn N'Patients', N'Phone', N'ALTER TABLE [dbo].[Patients] ADD [Phone] nvarchar(max) NOT NULL CONSTRAINT [DF_Patients_Phone] DEFAULT (N'''');';
EXEC #EnsureColumn N'Patients', N'DateOfBirth', N'ALTER TABLE [dbo].[Patients] ADD [DateOfBirth] datetime2 NOT NULL CONSTRAINT [DF_Patients_DateOfBirth] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'Patients', N'Gender', N'ALTER TABLE [dbo].[Patients] ADD [Gender] nvarchar(max) NOT NULL CONSTRAINT [DF_Patients_Gender] DEFAULT (N'''');';
EXEC #EnsureColumn N'Patients', N'Address', N'ALTER TABLE [dbo].[Patients] ADD [Address] nvarchar(max) NOT NULL CONSTRAINT [DF_Patients_Address] DEFAULT (N'''');';
EXEC #EnsureColumn N'Patients', N'City', N'ALTER TABLE [dbo].[Patients] ADD [City] nvarchar(max) NOT NULL CONSTRAINT [DF_Patients_City] DEFAULT (N'''');';
EXEC #EnsureColumn N'Patients', N'State', N'ALTER TABLE [dbo].[Patients] ADD [State] nvarchar(max) NOT NULL CONSTRAINT [DF_Patients_State] DEFAULT (N'''');';
EXEC #EnsureColumn N'Patients', N'Country', N'ALTER TABLE [dbo].[Patients] ADD [Country] nvarchar(max) NOT NULL CONSTRAINT [DF_Patients_Country] DEFAULT (N'''');';
EXEC #EnsureColumn N'Patients', N'PostalCode', N'ALTER TABLE [dbo].[Patients] ADD [PostalCode] nvarchar(max) NOT NULL CONSTRAINT [DF_Patients_PostalCode] DEFAULT (N'''');';
EXEC #EnsureColumn N'Patients', N'BloodGroup', N'ALTER TABLE [dbo].[Patients] ADD [BloodGroup] nvarchar(max) NOT NULL CONSTRAINT [DF_Patients_BloodGroup] DEFAULT (N'''');';
EXEC #EnsureColumn N'Patients', N'EmergencyContactName', N'ALTER TABLE [dbo].[Patients] ADD [EmergencyContactName] nvarchar(max) NOT NULL CONSTRAINT [DF_Patients_EmergencyContactName] DEFAULT (N'''');';
EXEC #EnsureColumn N'Patients', N'EmergencyContactPhone', N'ALTER TABLE [dbo].[Patients] ADD [EmergencyContactPhone] nvarchar(max) NOT NULL CONSTRAINT [DF_Patients_EmergencyContactPhone] DEFAULT (N'''');';
EXEC #EnsureColumn N'Patients', N'EmergencyContactRelation', N'ALTER TABLE [dbo].[Patients] ADD [EmergencyContactRelation] nvarchar(max) NOT NULL CONSTRAINT [DF_Patients_EmergencyContactRelation] DEFAULT (N'''');';
EXEC #EnsureColumn N'Patients', N'MedicalHistory', N'ALTER TABLE [dbo].[Patients] ADD [MedicalHistory] nvarchar(max) NOT NULL CONSTRAINT [DF_Patients_MedicalHistory] DEFAULT (N'''');';
EXEC #EnsureColumn N'Patients', N'Allergies', N'ALTER TABLE [dbo].[Patients] ADD [Allergies] nvarchar(max) NOT NULL CONSTRAINT [DF_Patients_Allergies] DEFAULT (N'''');';
EXEC #EnsureColumn N'Patients', N'GuardianName', N'ALTER TABLE [dbo].[Patients] ADD [GuardianName] nvarchar(max) NOT NULL CONSTRAINT [DF_Patients_GuardianName] DEFAULT (N'''');';
EXEC #EnsureColumn N'Patients', N'GuardianPhone', N'ALTER TABLE [dbo].[Patients] ADD [GuardianPhone] nvarchar(max) NOT NULL CONSTRAINT [DF_Patients_GuardianPhone] DEFAULT (N'''');';
EXEC #EnsureColumn N'Patients', N'MaritalStatus', N'ALTER TABLE [dbo].[Patients] ADD [MaritalStatus] nvarchar(max) NOT NULL CONSTRAINT [DF_Patients_MaritalStatus] DEFAULT (N'''');';
EXEC #EnsureColumn N'Patients', N'Occupation', N'ALTER TABLE [dbo].[Patients] ADD [Occupation] nvarchar(max) NOT NULL CONSTRAINT [DF_Patients_Occupation] DEFAULT (N'''');';
EXEC #EnsureColumn N'Patients', N'UserId', N'ALTER TABLE [dbo].[Patients] ADD [UserId] {{UserId}} NULL;';
EXEC #EnsureColumn N'Patients', N'ProfileImagePath', N'ALTER TABLE [dbo].[Patients] ADD [ProfileImagePath] nvarchar(max) NOT NULL CONSTRAINT [DF_Patients_ProfileImagePath] DEFAULT (N'''');';
EXEC #EnsureColumn N'Patients', N'IsActive', N'ALTER TABLE [dbo].[Patients] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_Patients_IsActive] DEFAULT (0);';
EXEC #EnsureColumn N'Patients', N'CreatedDate', N'ALTER TABLE [dbo].[Patients] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Patients_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'Patients', N'LastVisitDate', N'ALTER TABLE [dbo].[Patients] ADD [LastVisitDate] datetime2 NULL;';
EXEC #EnsureColumn N'Patients', N'HasInsurance', N'ALTER TABLE [dbo].[Patients] ADD [HasInsurance] bit NOT NULL CONSTRAINT [DF_Patients_HasInsurance] DEFAULT ((0));';
GO

-- Payments
EXEC #EnsureColumn N'Payments', N'Id', N'ALTER TABLE [dbo].[Payments] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'Payments', N'BillId', N'ALTER TABLE [dbo].[Payments] ADD [BillId] int NOT NULL CONSTRAINT [DF_Payments_BillId] DEFAULT (0);';
EXEC #EnsureColumn N'Payments', N'PaymentMethod', N'ALTER TABLE [dbo].[Payments] ADD [PaymentMethod] nvarchar(max) NOT NULL CONSTRAINT [DF_Payments_PaymentMethod] DEFAULT (N'''');';
EXEC #EnsureColumn N'Payments', N'Amount', N'ALTER TABLE [dbo].[Payments] ADD [Amount] decimal(18, 2) NOT NULL CONSTRAINT [DF_Payments_Amount] DEFAULT (0);';
EXEC #EnsureColumn N'Payments', N'TransactionId', N'ALTER TABLE [dbo].[Payments] ADD [TransactionId] nvarchar(max) NOT NULL CONSTRAINT [DF_Payments_TransactionId] DEFAULT (N'''');';
EXEC #EnsureColumn N'Payments', N'PaymentGateway', N'ALTER TABLE [dbo].[Payments] ADD [PaymentGateway] nvarchar(max) NOT NULL CONSTRAINT [DF_Payments_PaymentGateway] DEFAULT (N'''');';
EXEC #EnsureColumn N'Payments', N'Status', N'ALTER TABLE [dbo].[Payments] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_Payments_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'Payments', N'Notes', N'ALTER TABLE [dbo].[Payments] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF_Payments_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'Payments', N'PaymentDate', N'ALTER TABLE [dbo].[Payments] ADD [PaymentDate] datetime2 NOT NULL CONSTRAINT [DF_Payments_PaymentDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'Payments', N'ProcessedBy', N'ALTER TABLE [dbo].[Payments] ADD [ProcessedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_Payments_ProcessedBy] DEFAULT (N'''');';
GO

-- PayrollRecords
EXEC #EnsureColumn N'PayrollRecords', N'Id', N'ALTER TABLE [dbo].[PayrollRecords] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'PayrollRecords', N'StaffId', N'ALTER TABLE [dbo].[PayrollRecords] ADD [StaffId] {{StaffId}} NOT NULL CONSTRAINT [DF_PayrollRecords_StaffId] DEFAULT (N'''');';
EXEC #EnsureColumn N'PayrollRecords', N'PayrollMonth', N'ALTER TABLE [dbo].[PayrollRecords] ADD [PayrollMonth] datetime2 NOT NULL CONSTRAINT [DF_PayrollRecords_PayrollMonth] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'PayrollRecords', N'BasicSalary', N'ALTER TABLE [dbo].[PayrollRecords] ADD [BasicSalary] decimal(18, 2) NOT NULL CONSTRAINT [DF_PayrollRecords_BasicSalary] DEFAULT (0);';
EXEC #EnsureColumn N'PayrollRecords', N'Allowances', N'ALTER TABLE [dbo].[PayrollRecords] ADD [Allowances] decimal(18, 2) NOT NULL CONSTRAINT [DF_PayrollRecords_Allowances] DEFAULT (0);';
EXEC #EnsureColumn N'PayrollRecords', N'Deductions', N'ALTER TABLE [dbo].[PayrollRecords] ADD [Deductions] decimal(18, 2) NOT NULL CONSTRAINT [DF_PayrollRecords_Deductions] DEFAULT (0);';
EXEC #EnsureColumn N'PayrollRecords', N'NetSalary', N'ALTER TABLE [dbo].[PayrollRecords] ADD [NetSalary] decimal(18, 2) NOT NULL CONSTRAINT [DF_PayrollRecords_NetSalary] DEFAULT (0);';
EXEC #EnsureColumn N'PayrollRecords', N'Status', N'ALTER TABLE [dbo].[PayrollRecords] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_PayrollRecords_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'PayrollRecords', N'PaymentDate', N'ALTER TABLE [dbo].[PayrollRecords] ADD [PaymentDate] datetime2 NULL;';
EXEC #EnsureColumn N'PayrollRecords', N'Notes', N'ALTER TABLE [dbo].[PayrollRecords] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF_PayrollRecords_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'PayrollRecords', N'CreatedDate', N'ALTER TABLE [dbo].[PayrollRecords] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_PayrollRecords_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'PayrollRecords', N'UpdatedDate', N'ALTER TABLE [dbo].[PayrollRecords] ADD [UpdatedDate] datetime2 NULL;';
GO

-- PharmacyBills
EXEC #EnsureColumn N'PharmacyBills', N'Id', N'ALTER TABLE [dbo].[PharmacyBills] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'PharmacyBills', N'BillNumber', N'ALTER TABLE [dbo].[PharmacyBills] ADD [BillNumber] nvarchar(max) NOT NULL CONSTRAINT [DF_PharmacyBills_BillNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'PharmacyBills', N'PatientId', N'ALTER TABLE [dbo].[PharmacyBills] ADD [PatientId] int NOT NULL CONSTRAINT [DF_PharmacyBills_PatientId] DEFAULT (0);';
EXEC #EnsureColumn N'PharmacyBills', N'BillDate', N'ALTER TABLE [dbo].[PharmacyBills] ADD [BillDate] datetime2 NOT NULL CONSTRAINT [DF_PharmacyBills_BillDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'PharmacyBills', N'TotalAmount', N'ALTER TABLE [dbo].[PharmacyBills] ADD [TotalAmount] decimal(18, 2) NOT NULL CONSTRAINT [DF_PharmacyBills_TotalAmount] DEFAULT (0);';
EXEC #EnsureColumn N'PharmacyBills', N'PaidAmount', N'ALTER TABLE [dbo].[PharmacyBills] ADD [PaidAmount] decimal(18, 2) NOT NULL CONSTRAINT [DF_PharmacyBills_PaidAmount] DEFAULT (0);';
EXEC #EnsureColumn N'PharmacyBills', N'Status', N'ALTER TABLE [dbo].[PharmacyBills] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_PharmacyBills_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'PharmacyBills', N'PaymentMethod', N'ALTER TABLE [dbo].[PharmacyBills] ADD [PaymentMethod] nvarchar(max) NOT NULL CONSTRAINT [DF_PharmacyBills_PaymentMethod] DEFAULT (N'''');';
EXEC #EnsureColumn N'PharmacyBills', N'Notes', N'ALTER TABLE [dbo].[PharmacyBills] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF_PharmacyBills_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'PharmacyBills', N'CreatedDate', N'ALTER TABLE [dbo].[PharmacyBills] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_PharmacyBills_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'PharmacyBills', N'CreatedBy', N'ALTER TABLE [dbo].[PharmacyBills] ADD [CreatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_PharmacyBills_CreatedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'PharmacyBills', N'HospitalId', N'ALTER TABLE [dbo].[PharmacyBills] ADD [HospitalId] int NULL;';
GO

-- Prescriptions
EXEC #EnsureColumn N'Prescriptions', N'Id', N'ALTER TABLE [dbo].[Prescriptions] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'Prescriptions', N'PharmacyBillId', N'ALTER TABLE [dbo].[Prescriptions] ADD [PharmacyBillId] int NOT NULL CONSTRAINT [DF_Prescriptions_PharmacyBillId] DEFAULT (0);';
EXEC #EnsureColumn N'Prescriptions', N'MedicineId', N'ALTER TABLE [dbo].[Prescriptions] ADD [MedicineId] int NOT NULL CONSTRAINT [DF_Prescriptions_MedicineId] DEFAULT (0);';
EXEC #EnsureColumn N'Prescriptions', N'Dosage', N'ALTER TABLE [dbo].[Prescriptions] ADD [Dosage] nvarchar(max) NOT NULL CONSTRAINT [DF_Prescriptions_Dosage] DEFAULT (N'''');';
EXEC #EnsureColumn N'Prescriptions', N'Frequency', N'ALTER TABLE [dbo].[Prescriptions] ADD [Frequency] nvarchar(max) NOT NULL CONSTRAINT [DF_Prescriptions_Frequency] DEFAULT (N'''');';
EXEC #EnsureColumn N'Prescriptions', N'Duration', N'ALTER TABLE [dbo].[Prescriptions] ADD [Duration] int NOT NULL CONSTRAINT [DF_Prescriptions_Duration] DEFAULT (0);';
EXEC #EnsureColumn N'Prescriptions', N'Quantity', N'ALTER TABLE [dbo].[Prescriptions] ADD [Quantity] int NOT NULL CONSTRAINT [DF_Prescriptions_Quantity] DEFAULT (0);';
EXEC #EnsureColumn N'Prescriptions', N'UnitPrice', N'ALTER TABLE [dbo].[Prescriptions] ADD [UnitPrice] decimal(18, 2) NOT NULL CONSTRAINT [DF_Prescriptions_UnitPrice] DEFAULT (0);';
EXEC #EnsureColumn N'Prescriptions', N'TotalPrice', N'ALTER TABLE [dbo].[Prescriptions] ADD [TotalPrice] decimal(18, 2) NOT NULL CONSTRAINT [DF_Prescriptions_TotalPrice] DEFAULT (0);';
EXEC #EnsureColumn N'Prescriptions', N'Instructions', N'ALTER TABLE [dbo].[Prescriptions] ADD [Instructions] nvarchar(max) NOT NULL CONSTRAINT [DF_Prescriptions_Instructions] DEFAULT (N'''');';
EXEC #EnsureColumn N'Prescriptions', N'CreatedDate', N'ALTER TABLE [dbo].[Prescriptions] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Prescriptions_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- Printers
EXEC #EnsureColumn N'Printers', N'Id', N'ALTER TABLE [dbo].[Printers] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'Printers', N'Name', N'ALTER TABLE [dbo].[Printers] ADD [Name] nvarchar(100) NOT NULL CONSTRAINT [DF_Printers_Name] DEFAULT (N'''');';
EXEC #EnsureColumn N'Printers', N'PrinterType', N'ALTER TABLE [dbo].[Printers] ADD [PrinterType] nvarchar(20) NOT NULL CONSTRAINT [DF_Printers_PrinterType] DEFAULT (N'''');';
EXEC #EnsureColumn N'Printers', N'ConnectionType', N'ALTER TABLE [dbo].[Printers] ADD [ConnectionType] nvarchar(10) NOT NULL CONSTRAINT [DF_Printers_ConnectionType] DEFAULT (N'''');';
EXEC #EnsureColumn N'Printers', N'IpAddress', N'ALTER TABLE [dbo].[Printers] ADD [IpAddress] nvarchar(255) NOT NULL CONSTRAINT [DF_Printers_IpAddress] DEFAULT (N'''');';
EXEC #EnsureColumn N'Printers', N'Port', N'ALTER TABLE [dbo].[Printers] ADD [Port] int NOT NULL CONSTRAINT [DF_Printers_Port] DEFAULT (0);';
EXEC #EnsureColumn N'Printers', N'SharePath', N'ALTER TABLE [dbo].[Printers] ADD [SharePath] nvarchar(255) NOT NULL CONSTRAINT [DF_Printers_SharePath] DEFAULT (N'''');';
EXEC #EnsureColumn N'Printers', N'PaperSize', N'ALTER TABLE [dbo].[Printers] ADD [PaperSize] nvarchar(20) NOT NULL CONSTRAINT [DF_Printers_PaperSize] DEFAULT (N'''');';
EXEC #EnsureColumn N'Printers', N'PrintLanguage', N'ALTER TABLE [dbo].[Printers] ADD [PrintLanguage] nvarchar(20) NOT NULL CONSTRAINT [DF_Printers_PrintLanguage] DEFAULT (N'''');';
EXEC #EnsureColumn N'Printers', N'Copies', N'ALTER TABLE [dbo].[Printers] ADD [Copies] int NOT NULL CONSTRAINT [DF_Printers_Copies] DEFAULT (0);';
EXEC #EnsureColumn N'Printers', N'CutPaper', N'ALTER TABLE [dbo].[Printers] ADD [CutPaper] bit NOT NULL CONSTRAINT [DF_Printers_CutPaper] DEFAULT (0);';
EXEC #EnsureColumn N'Printers', N'OpenCashDrawer', N'ALTER TABLE [dbo].[Printers] ADD [OpenCashDrawer] bit NOT NULL CONSTRAINT [DF_Printers_OpenCashDrawer] DEFAULT (0);';
EXEC #EnsureColumn N'Printers', N'HospitalId', N'ALTER TABLE [dbo].[Printers] ADD [HospitalId] int NULL;';
EXEC #EnsureColumn N'Printers', N'IsDefault', N'ALTER TABLE [dbo].[Printers] ADD [IsDefault] bit NOT NULL CONSTRAINT [DF_Printers_IsDefault] DEFAULT (0);';
EXEC #EnsureColumn N'Printers', N'IsActive', N'ALTER TABLE [dbo].[Printers] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_Printers_IsActive] DEFAULT (0);';
EXEC #EnsureColumn N'Printers', N'Notes', N'ALTER TABLE [dbo].[Printers] ADD [Notes] nvarchar(500) NOT NULL CONSTRAINT [DF_Printers_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'Printers', N'CreatedDate', N'ALTER TABLE [dbo].[Printers] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Printers_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'Printers', N'UpdatedDate', N'ALTER TABLE [dbo].[Printers] ADD [UpdatedDate] datetime2 NULL;';
EXEC #EnsureColumn N'Printers', N'CreatedBy', N'ALTER TABLE [dbo].[Printers] ADD [CreatedBy] nvarchar(256) NOT NULL CONSTRAINT [DF_Printers_CreatedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'Printers', N'LastTestedDate', N'ALTER TABLE [dbo].[Printers] ADD [LastTestedDate] datetime2 NULL;';
EXEC #EnsureColumn N'Printers', N'LastTestResult', N'ALTER TABLE [dbo].[Printers] ADD [LastTestResult] nvarchar(500) NOT NULL CONSTRAINT [DF_Printers_LastTestResult] DEFAULT (N'''');';
GO

-- PublicAppointmentRequests
EXEC #EnsureColumn N'PublicAppointmentRequests', N'Id', N'ALTER TABLE [dbo].[PublicAppointmentRequests] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'PublicAppointmentRequests', N'PatientName', N'ALTER TABLE [dbo].[PublicAppointmentRequests] ADD [PatientName] nvarchar(150) NOT NULL CONSTRAINT [DF_PublicAppointmentRequests_PatientName] DEFAULT (N'''');';
EXEC #EnsureColumn N'PublicAppointmentRequests', N'Phone', N'ALTER TABLE [dbo].[PublicAppointmentRequests] ADD [Phone] nvarchar(20) NOT NULL CONSTRAINT [DF_PublicAppointmentRequests_Phone] DEFAULT (N'''');';
EXEC #EnsureColumn N'PublicAppointmentRequests', N'Email', N'ALTER TABLE [dbo].[PublicAppointmentRequests] ADD [Email] nvarchar(200) NULL;';
EXEC #EnsureColumn N'PublicAppointmentRequests', N'Gender', N'ALTER TABLE [dbo].[PublicAppointmentRequests] ADD [Gender] nvarchar(10) NULL;';
EXEC #EnsureColumn N'PublicAppointmentRequests', N'Age', N'ALTER TABLE [dbo].[PublicAppointmentRequests] ADD [Age] nvarchar(10) NULL;';
EXEC #EnsureColumn N'PublicAppointmentRequests', N'PatientId', N'ALTER TABLE [dbo].[PublicAppointmentRequests] ADD [PatientId] int NOT NULL CONSTRAINT [DF_PublicAppointmentRequests_PatientId] DEFAULT (0);';
EXEC #EnsureColumn N'PublicAppointmentRequests', N'DoctorId', N'ALTER TABLE [dbo].[PublicAppointmentRequests] ADD [DoctorId] int NOT NULL CONSTRAINT [DF_PublicAppointmentRequests_DoctorId] DEFAULT (0);';
EXEC #EnsureColumn N'PublicAppointmentRequests', N'PreferredDate', N'ALTER TABLE [dbo].[PublicAppointmentRequests] ADD [PreferredDate] datetime2 NOT NULL CONSTRAINT [DF_PublicAppointmentRequests_PreferredDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'PublicAppointmentRequests', N'PreferredTime', N'ALTER TABLE [dbo].[PublicAppointmentRequests] ADD [PreferredTime] time NOT NULL CONSTRAINT [DF_PublicAppointmentRequests_PreferredTime] DEFAULT (''00:00'');';
EXEC #EnsureColumn N'PublicAppointmentRequests', N'Symptoms', N'ALTER TABLE [dbo].[PublicAppointmentRequests] ADD [Symptoms] nvarchar(500) NULL;';
EXEC #EnsureColumn N'PublicAppointmentRequests', N'Notes', N'ALTER TABLE [dbo].[PublicAppointmentRequests] ADD [Notes] nvarchar(500) NULL;';
EXEC #EnsureColumn N'PublicAppointmentRequests', N'Status', N'ALTER TABLE [dbo].[PublicAppointmentRequests] ADD [Status] nvarchar(20) NOT NULL CONSTRAINT [DF_PublicAppointmentRequests_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'PublicAppointmentRequests', N'AdminNotes', N'ALTER TABLE [dbo].[PublicAppointmentRequests] ADD [AdminNotes] nvarchar(300) NULL;';
EXEC #EnsureColumn N'PublicAppointmentRequests', N'CreatedAt', N'ALTER TABLE [dbo].[PublicAppointmentRequests] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_PublicAppointmentRequests_CreatedAt] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'PublicAppointmentRequests', N'UpdatedAt', N'ALTER TABLE [dbo].[PublicAppointmentRequests] ADD [UpdatedAt] datetime2 NULL;';
EXEC #EnsureColumn N'PublicAppointmentRequests', N'IpAddress', N'ALTER TABLE [dbo].[PublicAppointmentRequests] ADD [IpAddress] nvarchar(45) NULL;';
GO

-- PurchaseBillItems
EXEC #EnsureColumn N'PurchaseBillItems', N'Id', N'ALTER TABLE [dbo].[PurchaseBillItems] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'PurchaseBillItems', N'PurchaseBillId', N'ALTER TABLE [dbo].[PurchaseBillItems] ADD [PurchaseBillId] int NOT NULL CONSTRAINT [DF_PurchaseBillItems_PurchaseBillId] DEFAULT (0);';
EXEC #EnsureColumn N'PurchaseBillItems', N'InventoryItemId', N'ALTER TABLE [dbo].[PurchaseBillItems] ADD [InventoryItemId] int NULL;';
EXEC #EnsureColumn N'PurchaseBillItems', N'Description', N'ALTER TABLE [dbo].[PurchaseBillItems] ADD [Description] nvarchar(200) NOT NULL CONSTRAINT [DF_PurchaseBillItems_Description] DEFAULT (N'''');';
EXEC #EnsureColumn N'PurchaseBillItems', N'Quantity', N'ALTER TABLE [dbo].[PurchaseBillItems] ADD [Quantity] decimal(18, 2) NOT NULL CONSTRAINT [DF_PurchaseBillItems_Quantity] DEFAULT (0);';
EXEC #EnsureColumn N'PurchaseBillItems', N'UnitCost', N'ALTER TABLE [dbo].[PurchaseBillItems] ADD [UnitCost] decimal(18, 2) NOT NULL CONSTRAINT [DF_PurchaseBillItems_UnitCost] DEFAULT (0);';
EXEC #EnsureColumn N'PurchaseBillItems', N'TaxPercent', N'ALTER TABLE [dbo].[PurchaseBillItems] ADD [TaxPercent] decimal(18, 2) NOT NULL CONSTRAINT [DF_PurchaseBillItems_TaxPercent] DEFAULT (0);';
EXEC #EnsureColumn N'PurchaseBillItems', N'LineTotal', N'ALTER TABLE [dbo].[PurchaseBillItems] ADD [LineTotal] decimal(18, 2) NOT NULL CONSTRAINT [DF_PurchaseBillItems_LineTotal] DEFAULT (0);';
EXEC #EnsureColumn N'PurchaseBillItems', N'BatchNumber', N'ALTER TABLE [dbo].[PurchaseBillItems] ADD [BatchNumber] nvarchar(50) NOT NULL CONSTRAINT [DF_PurchaseBillItems_BatchNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'PurchaseBillItems', N'ExpiryDate', N'ALTER TABLE [dbo].[PurchaseBillItems] ADD [ExpiryDate] datetime2 NULL;';
GO

-- PurchaseBills
EXEC #EnsureColumn N'PurchaseBills', N'Id', N'ALTER TABLE [dbo].[PurchaseBills] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'PurchaseBills', N'HospitalId', N'ALTER TABLE [dbo].[PurchaseBills] ADD [HospitalId] int NULL;';
EXEC #EnsureColumn N'PurchaseBills', N'BillNumber', N'ALTER TABLE [dbo].[PurchaseBills] ADD [BillNumber] nvarchar(30) NOT NULL CONSTRAINT [DF_PurchaseBills_BillNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'PurchaseBills', N'VendorId', N'ALTER TABLE [dbo].[PurchaseBills] ADD [VendorId] int NOT NULL CONSTRAINT [DF_PurchaseBills_VendorId] DEFAULT (0);';
EXEC #EnsureColumn N'PurchaseBills', N'VendorInvoiceNumber', N'ALTER TABLE [dbo].[PurchaseBills] ADD [VendorInvoiceNumber] nvarchar(50) NOT NULL CONSTRAINT [DF_PurchaseBills_VendorInvoiceNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'PurchaseBills', N'InvoiceDate', N'ALTER TABLE [dbo].[PurchaseBills] ADD [InvoiceDate] datetime2 NOT NULL CONSTRAINT [DF_PurchaseBills_InvoiceDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'PurchaseBills', N'DueDate', N'ALTER TABLE [dbo].[PurchaseBills] ADD [DueDate] datetime2 NOT NULL CONSTRAINT [DF_PurchaseBills_DueDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'PurchaseBills', N'Status', N'ALTER TABLE [dbo].[PurchaseBills] ADD [Status] nvarchar(20) NOT NULL CONSTRAINT [DF_PurchaseBills_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'PurchaseBills', N'PaymentStatus', N'ALTER TABLE [dbo].[PurchaseBills] ADD [PaymentStatus] nvarchar(20) NOT NULL CONSTRAINT [DF_PurchaseBills_PaymentStatus] DEFAULT (N'''');';
EXEC #EnsureColumn N'PurchaseBills', N'SubTotal', N'ALTER TABLE [dbo].[PurchaseBills] ADD [SubTotal] decimal(18, 2) NOT NULL CONSTRAINT [DF_PurchaseBills_SubTotal] DEFAULT (0);';
EXEC #EnsureColumn N'PurchaseBills', N'TaxAmount', N'ALTER TABLE [dbo].[PurchaseBills] ADD [TaxAmount] decimal(18, 2) NOT NULL CONSTRAINT [DF_PurchaseBills_TaxAmount] DEFAULT (0);';
EXEC #EnsureColumn N'PurchaseBills', N'TotalAmount', N'ALTER TABLE [dbo].[PurchaseBills] ADD [TotalAmount] decimal(18, 2) NOT NULL CONSTRAINT [DF_PurchaseBills_TotalAmount] DEFAULT (0);';
EXEC #EnsureColumn N'PurchaseBills', N'PaidAmount', N'ALTER TABLE [dbo].[PurchaseBills] ADD [PaidAmount] decimal(18, 2) NOT NULL CONSTRAINT [DF_PurchaseBills_PaidAmount] DEFAULT (0);';
EXEC #EnsureColumn N'PurchaseBills', N'Notes', N'ALTER TABLE [dbo].[PurchaseBills] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF_PurchaseBills_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'PurchaseBills', N'CreatedByUserId', N'ALTER TABLE [dbo].[PurchaseBills] ADD [CreatedByUserId] nvarchar(max) NOT NULL CONSTRAINT [DF_PurchaseBills_CreatedByUserId] DEFAULT (N'''');';
EXEC #EnsureColumn N'PurchaseBills', N'CreatedBy', N'ALTER TABLE [dbo].[PurchaseBills] ADD [CreatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_PurchaseBills_CreatedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'PurchaseBills', N'CreatedAt', N'ALTER TABLE [dbo].[PurchaseBills] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_PurchaseBills_CreatedAt] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'PurchaseBills', N'SubmittedAt', N'ALTER TABLE [dbo].[PurchaseBills] ADD [SubmittedAt] datetime2 NULL;';
EXEC #EnsureColumn N'PurchaseBills', N'ApprovedBy', N'ALTER TABLE [dbo].[PurchaseBills] ADD [ApprovedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_PurchaseBills_ApprovedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'PurchaseBills', N'ApprovedAt', N'ALTER TABLE [dbo].[PurchaseBills] ADD [ApprovedAt] datetime2 NULL;';
EXEC #EnsureColumn N'PurchaseBills', N'ApprovalComments', N'ALTER TABLE [dbo].[PurchaseBills] ADD [ApprovalComments] nvarchar(max) NOT NULL CONSTRAINT [DF_PurchaseBills_ApprovalComments] DEFAULT (N'''');';
EXEC #EnsureColumn N'PurchaseBills', N'ReceivedBy', N'ALTER TABLE [dbo].[PurchaseBills] ADD [ReceivedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_PurchaseBills_ReceivedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'PurchaseBills', N'ReceivedAt', N'ALTER TABLE [dbo].[PurchaseBills] ADD [ReceivedAt] datetime2 NULL;';
EXEC #EnsureColumn N'PurchaseBills', N'CancelReason', N'ALTER TABLE [dbo].[PurchaseBills] ADD [CancelReason] nvarchar(max) NOT NULL CONSTRAINT [DF_PurchaseBills_CancelReason] DEFAULT (N'''');';
GO

-- QualityIncidents
EXEC #EnsureColumn N'QualityIncidents', N'Id', N'ALTER TABLE [dbo].[QualityIncidents] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'QualityIncidents', N'HospitalId', N'ALTER TABLE [dbo].[QualityIncidents] ADD [HospitalId] int NULL;';
EXEC #EnsureColumn N'QualityIncidents', N'IncidentNumber', N'ALTER TABLE [dbo].[QualityIncidents] ADD [IncidentNumber] nvarchar(30) NOT NULL CONSTRAINT [DF_QualityIncidents_IncidentNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'QualityIncidents', N'Title', N'ALTER TABLE [dbo].[QualityIncidents] ADD [Title] nvarchar(200) NOT NULL CONSTRAINT [DF_QualityIncidents_Title] DEFAULT (N'''');';
EXEC #EnsureColumn N'QualityIncidents', N'Category', N'ALTER TABLE [dbo].[QualityIncidents] ADD [Category] nvarchar(50) NOT NULL CONSTRAINT [DF_QualityIncidents_Category] DEFAULT (N'''');';
EXEC #EnsureColumn N'QualityIncidents', N'Severity', N'ALTER TABLE [dbo].[QualityIncidents] ADD [Severity] nvarchar(20) NOT NULL CONSTRAINT [DF_QualityIncidents_Severity] DEFAULT (N'''');';
EXEC #EnsureColumn N'QualityIncidents', N'OccurredAt', N'ALTER TABLE [dbo].[QualityIncidents] ADD [OccurredAt] datetime2 NOT NULL CONSTRAINT [DF_QualityIncidents_OccurredAt] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'QualityIncidents', N'ReportedAt', N'ALTER TABLE [dbo].[QualityIncidents] ADD [ReportedAt] datetime2 NOT NULL CONSTRAINT [DF_QualityIncidents_ReportedAt] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'QualityIncidents', N'Location', N'ALTER TABLE [dbo].[QualityIncidents] ADD [Location] nvarchar(150) NOT NULL CONSTRAINT [DF_QualityIncidents_Location] DEFAULT (N'''');';
EXEC #EnsureColumn N'QualityIncidents', N'Description', N'ALTER TABLE [dbo].[QualityIncidents] ADD [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_QualityIncidents_Description] DEFAULT (N'''');';
EXEC #EnsureColumn N'QualityIncidents', N'ImmediateAction', N'ALTER TABLE [dbo].[QualityIncidents] ADD [ImmediateAction] nvarchar(max) NOT NULL CONSTRAINT [DF_QualityIncidents_ImmediateAction] DEFAULT (N'''');';
EXEC #EnsureColumn N'QualityIncidents', N'PatientId', N'ALTER TABLE [dbo].[QualityIncidents] ADD [PatientId] int NULL;';
EXEC #EnsureColumn N'QualityIncidents', N'ReportedByUserId', N'ALTER TABLE [dbo].[QualityIncidents] ADD [ReportedByUserId] nvarchar(max) NOT NULL CONSTRAINT [DF_QualityIncidents_ReportedByUserId] DEFAULT (N'''');';
EXEC #EnsureColumn N'QualityIncidents', N'ReportedByName', N'ALTER TABLE [dbo].[QualityIncidents] ADD [ReportedByName] nvarchar(max) NOT NULL CONSTRAINT [DF_QualityIncidents_ReportedByName] DEFAULT (N'''');';
EXEC #EnsureColumn N'QualityIncidents', N'Status', N'ALTER TABLE [dbo].[QualityIncidents] ADD [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_QualityIncidents_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'QualityIncidents', N'RootCause', N'ALTER TABLE [dbo].[QualityIncidents] ADD [RootCause] nvarchar(max) NOT NULL CONSTRAINT [DF_QualityIncidents_RootCause] DEFAULT (N'''');';
EXEC #EnsureColumn N'QualityIncidents', N'ClosedAt', N'ALTER TABLE [dbo].[QualityIncidents] ADD [ClosedAt] datetime2 NULL;';
EXEC #EnsureColumn N'QualityIncidents', N'ClosedBy', N'ALTER TABLE [dbo].[QualityIncidents] ADD [ClosedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_QualityIncidents_ClosedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'QualityIncidents', N'ClosureNotes', N'ALTER TABLE [dbo].[QualityIncidents] ADD [ClosureNotes] nvarchar(max) NOT NULL CONSTRAINT [DF_QualityIncidents_ClosureNotes] DEFAULT (N'''');';
GO

-- RadiologyResults
EXEC #EnsureColumn N'RadiologyResults', N'Id', N'ALTER TABLE [dbo].[RadiologyResults] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'RadiologyResults', N'PatientId', N'ALTER TABLE [dbo].[RadiologyResults] ADD [PatientId] int NOT NULL CONSTRAINT [DF_RadiologyResults_PatientId] DEFAULT (0);';
EXEC #EnsureColumn N'RadiologyResults', N'RadiologyTestId', N'ALTER TABLE [dbo].[RadiologyResults] ADD [RadiologyTestId] int NOT NULL CONSTRAINT [DF_RadiologyResults_RadiologyTestId] DEFAULT (0);';
EXEC #EnsureColumn N'RadiologyResults', N'OrderNumber', N'ALTER TABLE [dbo].[RadiologyResults] ADD [OrderNumber] nvarchar(max) NOT NULL CONSTRAINT [DF_RadiologyResults_OrderNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'RadiologyResults', N'OrderDate', N'ALTER TABLE [dbo].[RadiologyResults] ADD [OrderDate] datetime2 NOT NULL CONSTRAINT [DF_RadiologyResults_OrderDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'RadiologyResults', N'ResultDate', N'ALTER TABLE [dbo].[RadiologyResults] ADD [ResultDate] datetime2 NULL;';
EXEC #EnsureColumn N'RadiologyResults', N'Findings', N'ALTER TABLE [dbo].[RadiologyResults] ADD [Findings] nvarchar(max) NOT NULL CONSTRAINT [DF_RadiologyResults_Findings] DEFAULT (N'''');';
EXEC #EnsureColumn N'RadiologyResults', N'Impression', N'ALTER TABLE [dbo].[RadiologyResults] ADD [Impression] nvarchar(max) NOT NULL CONSTRAINT [DF_RadiologyResults_Impression] DEFAULT (N'''');';
EXEC #EnsureColumn N'RadiologyResults', N'Status', N'ALTER TABLE [dbo].[RadiologyResults] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_RadiologyResults_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'RadiologyResults', N'PerformedBy', N'ALTER TABLE [dbo].[RadiologyResults] ADD [PerformedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_RadiologyResults_PerformedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'RadiologyResults', N'VerifiedBy', N'ALTER TABLE [dbo].[RadiologyResults] ADD [VerifiedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_RadiologyResults_VerifiedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'RadiologyResults', N'ImagePath', N'ALTER TABLE [dbo].[RadiologyResults] ADD [ImagePath] nvarchar(max) NOT NULL CONSTRAINT [DF_RadiologyResults_ImagePath] DEFAULT (N'''');';
EXEC #EnsureColumn N'RadiologyResults', N'Notes', N'ALTER TABLE [dbo].[RadiologyResults] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF_RadiologyResults_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'RadiologyResults', N'CreatedDate', N'ALTER TABLE [dbo].[RadiologyResults] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_RadiologyResults_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- RadiologyTests
EXEC #EnsureColumn N'RadiologyTests', N'Id', N'ALTER TABLE [dbo].[RadiologyTests] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'RadiologyTests', N'TestName', N'ALTER TABLE [dbo].[RadiologyTests] ADD [TestName] nvarchar(max) NOT NULL CONSTRAINT [DF_RadiologyTests_TestName] DEFAULT (N'''');';
EXEC #EnsureColumn N'RadiologyTests', N'TestCode', N'ALTER TABLE [dbo].[RadiologyTests] ADD [TestCode] nvarchar(max) NOT NULL CONSTRAINT [DF_RadiologyTests_TestCode] DEFAULT (N'''');';
EXEC #EnsureColumn N'RadiologyTests', N'Category', N'ALTER TABLE [dbo].[RadiologyTests] ADD [Category] nvarchar(max) NOT NULL CONSTRAINT [DF_RadiologyTests_Category] DEFAULT (N'''');';
EXEC #EnsureColumn N'RadiologyTests', N'Description', N'ALTER TABLE [dbo].[RadiologyTests] ADD [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_RadiologyTests_Description] DEFAULT (N'''');';
EXEC #EnsureColumn N'RadiologyTests', N'Price', N'ALTER TABLE [dbo].[RadiologyTests] ADD [Price] decimal(18, 2) NOT NULL CONSTRAINT [DF_RadiologyTests_Price] DEFAULT (0);';
EXEC #EnsureColumn N'RadiologyTests', N'PreparationTimeHours', N'ALTER TABLE [dbo].[RadiologyTests] ADD [PreparationTimeHours] int NOT NULL CONSTRAINT [DF_RadiologyTests_PreparationTimeHours] DEFAULT (0);';
EXEC #EnsureColumn N'RadiologyTests', N'SpecialInstructions', N'ALTER TABLE [dbo].[RadiologyTests] ADD [SpecialInstructions] nvarchar(max) NOT NULL CONSTRAINT [DF_RadiologyTests_SpecialInstructions] DEFAULT (N'''');';
EXEC #EnsureColumn N'RadiologyTests', N'RequiresContrast', N'ALTER TABLE [dbo].[RadiologyTests] ADD [RequiresContrast] bit NOT NULL CONSTRAINT [DF_RadiologyTests_RequiresContrast] DEFAULT (0);';
EXEC #EnsureColumn N'RadiologyTests', N'IsActive', N'ALTER TABLE [dbo].[RadiologyTests] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_RadiologyTests_IsActive] DEFAULT (0);';
EXEC #EnsureColumn N'RadiologyTests', N'CreatedDate', N'ALTER TABLE [dbo].[RadiologyTests] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_RadiologyTests_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- Referrals
EXEC #EnsureColumn N'Referrals', N'Id', N'ALTER TABLE [dbo].[Referrals] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'Referrals', N'PatientId', N'ALTER TABLE [dbo].[Referrals] ADD [PatientId] int NOT NULL CONSTRAINT [DF_Referrals_PatientId] DEFAULT (0);';
EXEC #EnsureColumn N'Referrals', N'ReferralType', N'ALTER TABLE [dbo].[Referrals] ADD [ReferralType] nvarchar(max) NOT NULL CONSTRAINT [DF_Referrals_ReferralType] DEFAULT (N'''');';
EXEC #EnsureColumn N'Referrals', N'ReferredTo', N'ALTER TABLE [dbo].[Referrals] ADD [ReferredTo] nvarchar(max) NOT NULL CONSTRAINT [DF_Referrals_ReferredTo] DEFAULT (N'''');';
EXEC #EnsureColumn N'Referrals', N'ReferralReason', N'ALTER TABLE [dbo].[Referrals] ADD [ReferralReason] nvarchar(max) NOT NULL CONSTRAINT [DF_Referrals_ReferralReason] DEFAULT (N'''');';
EXEC #EnsureColumn N'Referrals', N'ReferralDate', N'ALTER TABLE [dbo].[Referrals] ADD [ReferralDate] datetime2 NOT NULL CONSTRAINT [DF_Referrals_ReferralDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'Referrals', N'Status', N'ALTER TABLE [dbo].[Referrals] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_Referrals_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'Referrals', N'TpaProvider', N'ALTER TABLE [dbo].[Referrals] ADD [TpaProvider] nvarchar(max) NOT NULL CONSTRAINT [DF_Referrals_TpaProvider] DEFAULT (N'''');';
EXEC #EnsureColumn N'Referrals', N'TpaPolicyNumber', N'ALTER TABLE [dbo].[Referrals] ADD [TpaPolicyNumber] nvarchar(max) NOT NULL CONSTRAINT [DF_Referrals_TpaPolicyNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'Referrals', N'ApprovedAmount', N'ALTER TABLE [dbo].[Referrals] ADD [ApprovedAmount] decimal(18, 2) NULL;';
EXEC #EnsureColumn N'Referrals', N'Notes', N'ALTER TABLE [dbo].[Referrals] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF_Referrals_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'Referrals', N'BillId', N'ALTER TABLE [dbo].[Referrals] ADD [BillId] int NULL;';
EXEC #EnsureColumn N'Referrals', N'CreatedDate', N'ALTER TABLE [dbo].[Referrals] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Referrals_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- ReportCharts
EXEC #EnsureColumn N'ReportCharts', N'Id', N'ALTER TABLE [dbo].[ReportCharts] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'ReportCharts', N'TemplateId', N'ALTER TABLE [dbo].[ReportCharts] ADD [TemplateId] int NOT NULL CONSTRAINT [DF_ReportCharts_TemplateId] DEFAULT (0);';
EXEC #EnsureColumn N'ReportCharts', N'Title', N'ALTER TABLE [dbo].[ReportCharts] ADD [Title] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportCha__Title__75035A77] DEFAULT ('''');';
EXEC #EnsureColumn N'ReportCharts', N'ChartType', N'ALTER TABLE [dbo].[ReportCharts] ADD [ChartType] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportCha__Chart__75F77EB0] DEFAULT (''bar'');';
EXEC #EnsureColumn N'ReportCharts', N'XAxisField', N'ALTER TABLE [dbo].[ReportCharts] ADD [XAxisField] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportCha__XAxis__76EBA2E9] DEFAULT ('''');';
EXEC #EnsureColumn N'ReportCharts', N'YAxisField', N'ALTER TABLE [dbo].[ReportCharts] ADD [YAxisField] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportCha__YAxis__77DFC722] DEFAULT ('''');';
EXEC #EnsureColumn N'ReportCharts', N'ShowLegend', N'ALTER TABLE [dbo].[ReportCharts] ADD [ShowLegend] bit NOT NULL CONSTRAINT [DF__ReportCha__ShowL__78D3EB5B] DEFAULT ((1));';
EXEC #EnsureColumn N'ReportCharts', N'ShowTooltip', N'ALTER TABLE [dbo].[ReportCharts] ADD [ShowTooltip] bit NOT NULL CONSTRAINT [DF__ReportCha__ShowT__79C80F94] DEFAULT ((1));';
EXEC #EnsureColumn N'ReportCharts', N'ColorScheme', N'ALTER TABLE [dbo].[ReportCharts] ADD [ColorScheme] nvarchar(max) NULL;';
EXEC #EnsureColumn N'ReportCharts', N'SortOrder', N'ALTER TABLE [dbo].[ReportCharts] ADD [SortOrder] int NOT NULL CONSTRAINT [DF__ReportCha__SortO__7ABC33CD] DEFAULT ((0));';
GO

-- ReportDesigns
EXEC #EnsureColumn N'ReportDesigns', N'Id', N'ALTER TABLE [dbo].[ReportDesigns] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'ReportDesigns', N'TemplateId', N'ALTER TABLE [dbo].[ReportDesigns] ADD [TemplateId] int NOT NULL CONSTRAINT [DF_ReportDesigns_TemplateId] DEFAULT (0);';
EXEC #EnsureColumn N'ReportDesigns', N'HeaderText', N'ALTER TABLE [dbo].[ReportDesigns] ADD [HeaderText] nvarchar(max) NULL;';
EXEC #EnsureColumn N'ReportDesigns', N'FooterText', N'ALTER TABLE [dbo].[ReportDesigns] ADD [FooterText] nvarchar(max) NULL;';
EXEC #EnsureColumn N'ReportDesigns', N'ColorScheme', N'ALTER TABLE [dbo].[ReportDesigns] ADD [ColorScheme] nvarchar(max) NULL;';
EXEC #EnsureColumn N'ReportDesigns', N'ShowGridLines', N'ALTER TABLE [dbo].[ReportDesigns] ADD [ShowGridLines] bit NOT NULL CONSTRAINT [DF__ReportDes__ShowG__6D6238AF] DEFAULT ((1));';
EXEC #EnsureColumn N'ReportDesigns', N'ShowAlternatingRows', N'ALTER TABLE [dbo].[ReportDesigns] ADD [ShowAlternatingRows] bit NOT NULL CONSTRAINT [DF__ReportDes__ShowA__6E565CE8] DEFAULT ((1));';
EXEC #EnsureColumn N'ReportDesigns', N'ShowTotals', N'ALTER TABLE [dbo].[ReportDesigns] ADD [ShowTotals] bit NOT NULL CONSTRAINT [DF__ReportDes__ShowT__6F4A8121] DEFAULT ((0));';
EXEC #EnsureColumn N'ReportDesigns', N'ShowGrouping', N'ALTER TABLE [dbo].[ReportDesigns] ADD [ShowGrouping] bit NOT NULL CONSTRAINT [DF__ReportDes__ShowG__703EA55A] DEFAULT ((0));';
EXEC #EnsureColumn N'ReportDesigns', N'PageOrientation', N'ALTER TABLE [dbo].[ReportDesigns] ADD [PageOrientation] nvarchar(max) NULL;';
EXEC #EnsureColumn N'ReportDesigns', N'IncludeTimestamp', N'ALTER TABLE [dbo].[ReportDesigns] ADD [IncludeTimestamp] bit NOT NULL CONSTRAINT [DF__ReportDes__Inclu__7132C993] DEFAULT ((1));';
EXEC #EnsureColumn N'ReportDesigns', N'CompanyLogo', N'ALTER TABLE [dbo].[ReportDesigns] ADD [CompanyLogo] nvarchar(max) NULL;';
EXEC #EnsureColumn N'ReportDesigns', N'CustomCss', N'ALTER TABLE [dbo].[ReportDesigns] ADD [CustomCss] nvarchar(max) NULL;';
GO

-- ReportFields
EXEC #EnsureColumn N'ReportFields', N'Id', N'ALTER TABLE [dbo].[ReportFields] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'ReportFields', N'TemplateId', N'ALTER TABLE [dbo].[ReportFields] ADD [TemplateId] int NOT NULL CONSTRAINT [DF_ReportFields_TemplateId] DEFAULT (0);';
EXEC #EnsureColumn N'ReportFields', N'FieldName', N'ALTER TABLE [dbo].[ReportFields] ADD [FieldName] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportFie__Field__5B438874] DEFAULT ('''');';
EXEC #EnsureColumn N'ReportFields', N'ColumnName', N'ALTER TABLE [dbo].[ReportFields] ADD [ColumnName] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportFie__Colum__5C37ACAD] DEFAULT ('''');';
EXEC #EnsureColumn N'ReportFields', N'DataType', N'ALTER TABLE [dbo].[ReportFields] ADD [DataType] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportFie__DataT__5D2BD0E6] DEFAULT (''string'');';
EXEC #EnsureColumn N'ReportFields', N'DisplayFormat', N'ALTER TABLE [dbo].[ReportFields] ADD [DisplayFormat] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportFie__Displ__5E1FF51F] DEFAULT ('''');';
EXEC #EnsureColumn N'ReportFields', N'IsVisible', N'ALTER TABLE [dbo].[ReportFields] ADD [IsVisible] bit NOT NULL CONSTRAINT [DF__ReportFie__IsVis__5F141958] DEFAULT ((1));';
EXEC #EnsureColumn N'ReportFields', N'IsSortable', N'ALTER TABLE [dbo].[ReportFields] ADD [IsSortable] bit NOT NULL CONSTRAINT [DF__ReportFie__IsSor__60083D91] DEFAULT ((1));';
EXEC #EnsureColumn N'ReportFields', N'IsFilterable', N'ALTER TABLE [dbo].[ReportFields] ADD [IsFilterable] bit NOT NULL CONSTRAINT [DF__ReportFie__IsFil__60FC61CA] DEFAULT ((1));';
EXEC #EnsureColumn N'ReportFields', N'SortOrder', N'ALTER TABLE [dbo].[ReportFields] ADD [SortOrder] int NOT NULL CONSTRAINT [DF__ReportFie__SortO__61F08603] DEFAULT ((0));';
EXEC #EnsureColumn N'ReportFields', N'Width', N'ALTER TABLE [dbo].[ReportFields] ADD [Width] int NULL;';
EXEC #EnsureColumn N'ReportFields', N'Alignment', N'ALTER TABLE [dbo].[ReportFields] ADD [Alignment] nvarchar(max) NULL;';
GO

-- ReportFilters
EXEC #EnsureColumn N'ReportFilters', N'Id', N'ALTER TABLE [dbo].[ReportFilters] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'ReportFilters', N'TemplateId', N'ALTER TABLE [dbo].[ReportFilters] ADD [TemplateId] int NOT NULL CONSTRAINT [DF_ReportFilters_TemplateId] DEFAULT (0);';
EXEC #EnsureColumn N'ReportFilters', N'FilterName', N'ALTER TABLE [dbo].[ReportFilters] ADD [FilterName] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportFil__Filte__65C116E7] DEFAULT ('''');';
EXEC #EnsureColumn N'ReportFilters', N'ColumnName', N'ALTER TABLE [dbo].[ReportFilters] ADD [ColumnName] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportFil__Colum__66B53B20] DEFAULT ('''');';
EXEC #EnsureColumn N'ReportFilters', N'OperatorType', N'ALTER TABLE [dbo].[ReportFilters] ADD [OperatorType] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportFil__Opera__67A95F59] DEFAULT (''equals'');';
EXEC #EnsureColumn N'ReportFilters', N'DefaultValue', N'ALTER TABLE [dbo].[ReportFilters] ADD [DefaultValue] nvarchar(max) NULL;';
EXEC #EnsureColumn N'ReportFilters', N'IsRequired', N'ALTER TABLE [dbo].[ReportFilters] ADD [IsRequired] bit NOT NULL CONSTRAINT [DF__ReportFil__IsReq__689D8392] DEFAULT ((0));';
EXEC #EnsureColumn N'ReportFilters', N'SortOrder', N'ALTER TABLE [dbo].[ReportFilters] ADD [SortOrder] int NOT NULL CONSTRAINT [DF__ReportFil__SortO__6991A7CB] DEFAULT ((0));';
GO

-- ReportSchedules
EXEC #EnsureColumn N'ReportSchedules', N'Id', N'ALTER TABLE [dbo].[ReportSchedules] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'ReportSchedules', N'ReportName', N'ALTER TABLE [dbo].[ReportSchedules] ADD [ReportName] nvarchar(max) NOT NULL CONSTRAINT [DF_ReportSchedules_ReportName] DEFAULT (N'''');';
EXEC #EnsureColumn N'ReportSchedules', N'ReportType', N'ALTER TABLE [dbo].[ReportSchedules] ADD [ReportType] nvarchar(max) NOT NULL CONSTRAINT [DF_ReportSchedules_ReportType] DEFAULT (N'''');';
EXEC #EnsureColumn N'ReportSchedules', N'RecurrencePattern', N'ALTER TABLE [dbo].[ReportSchedules] ADD [RecurrencePattern] nvarchar(max) NOT NULL CONSTRAINT [DF_ReportSchedules_RecurrencePattern] DEFAULT (N'''');';
EXEC #EnsureColumn N'ReportSchedules', N'DayOfWeek', N'ALTER TABLE [dbo].[ReportSchedules] ADD [DayOfWeek] int NULL;';
EXEC #EnsureColumn N'ReportSchedules', N'DayOfMonth', N'ALTER TABLE [dbo].[ReportSchedules] ADD [DayOfMonth] int NULL;';
EXEC #EnsureColumn N'ReportSchedules', N'TimeOfDay', N'ALTER TABLE [dbo].[ReportSchedules] ADD [TimeOfDay] nvarchar(max) NOT NULL CONSTRAINT [DF_ReportSchedules_TimeOfDay] DEFAULT (N'''');';
EXEC #EnsureColumn N'ReportSchedules', N'IsActive', N'ALTER TABLE [dbo].[ReportSchedules] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_ReportSchedules_IsActive] DEFAULT (0);';
EXEC #EnsureColumn N'ReportSchedules', N'EmailRecipients', N'ALTER TABLE [dbo].[ReportSchedules] ADD [EmailRecipients] nvarchar(max) NOT NULL CONSTRAINT [DF_ReportSchedules_EmailRecipients] DEFAULT (N'''');';
EXEC #EnsureColumn N'ReportSchedules', N'CreatedBy', N'ALTER TABLE [dbo].[ReportSchedules] ADD [CreatedBy] {{StaffId}} NOT NULL CONSTRAINT [DF_ReportSchedules_CreatedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'ReportSchedules', N'CreatedDate', N'ALTER TABLE [dbo].[ReportSchedules] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_ReportSchedules_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'ReportSchedules', N'LastRunDate', N'ALTER TABLE [dbo].[ReportSchedules] ADD [LastRunDate] datetime2 NULL;';
EXEC #EnsureColumn N'ReportSchedules', N'NextRunDate', N'ALTER TABLE [dbo].[ReportSchedules] ADD [NextRunDate] datetime2 NULL;';
GO

-- ReportTemplates
EXEC #EnsureColumn N'ReportTemplates', N'Id', N'ALTER TABLE [dbo].[ReportTemplates] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'ReportTemplates', N'Name', N'ALTER TABLE [dbo].[ReportTemplates] ADD [Name] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportTemp__Name__53A266AC] DEFAULT ('''');';
EXEC #EnsureColumn N'ReportTemplates', N'Description', N'ALTER TABLE [dbo].[ReportTemplates] ADD [Description] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportTem__Descr__54968AE5] DEFAULT ('''');';
EXEC #EnsureColumn N'ReportTemplates', N'ReportType', N'ALTER TABLE [dbo].[ReportTemplates] ADD [ReportType] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportTem__Repor__558AAF1E] DEFAULT ('''');';
EXEC #EnsureColumn N'ReportTemplates', N'CreatedBy', N'ALTER TABLE [dbo].[ReportTemplates] ADD [CreatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF__ReportTem__Creat__567ED357] DEFAULT ('''');';
EXEC #EnsureColumn N'ReportTemplates', N'CreatedDate', N'ALTER TABLE [dbo].[ReportTemplates] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_ReportTemplates_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'ReportTemplates', N'ModifiedDate', N'ALTER TABLE [dbo].[ReportTemplates] ADD [ModifiedDate] datetime2 NULL;';
EXEC #EnsureColumn N'ReportTemplates', N'ModifiedBy', N'ALTER TABLE [dbo].[ReportTemplates] ADD [ModifiedBy] nvarchar(max) NULL;';
EXEC #EnsureColumn N'ReportTemplates', N'IsActive', N'ALTER TABLE [dbo].[ReportTemplates] ADD [IsActive] bit NOT NULL CONSTRAINT [DF__ReportTem__IsAct__5772F790] DEFAULT ((1));';
EXEC #EnsureColumn N'ReportTemplates', N'IsDefault', N'ALTER TABLE [dbo].[ReportTemplates] ADD [IsDefault] bit NOT NULL CONSTRAINT [DF__ReportTem__IsDef__58671BC9] DEFAULT ((0));';
GO

-- RoleFeatures
EXEC #EnsureColumn N'RoleFeatures', N'RoleId', N'ALTER TABLE [dbo].[RoleFeatures] ADD [RoleId] int NOT NULL CONSTRAINT [DF_RoleFeatures_RoleId] DEFAULT (0);';
EXEC #EnsureColumn N'RoleFeatures', N'FeatureId', N'ALTER TABLE [dbo].[RoleFeatures] ADD [FeatureId] int NOT NULL CONSTRAINT [DF_RoleFeatures_FeatureId] DEFAULT (0);';
EXEC #EnsureColumn N'RoleFeatures', N'CanView', N'ALTER TABLE [dbo].[RoleFeatures] ADD [CanView] bit NOT NULL CONSTRAINT [DF_RoleFeatures_CanView] DEFAULT (0);';
EXEC #EnsureColumn N'RoleFeatures', N'CanAdd', N'ALTER TABLE [dbo].[RoleFeatures] ADD [CanAdd] bit NOT NULL CONSTRAINT [DF_RoleFeatures_CanAdd] DEFAULT (0);';
EXEC #EnsureColumn N'RoleFeatures', N'CanEdit', N'ALTER TABLE [dbo].[RoleFeatures] ADD [CanEdit] bit NOT NULL CONSTRAINT [DF_RoleFeatures_CanEdit] DEFAULT (0);';
EXEC #EnsureColumn N'RoleFeatures', N'CanDelete', N'ALTER TABLE [dbo].[RoleFeatures] ADD [CanDelete] bit NOT NULL CONSTRAINT [DF_RoleFeatures_CanDelete] DEFAULT (0);';
EXEC #EnsureColumn N'RoleFeatures', N'CreatedDate', N'ALTER TABLE [dbo].[RoleFeatures] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_RoleFeatures_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- Roles
EXEC #EnsureColumn N'Roles', N'Id', N'ALTER TABLE [dbo].[Roles] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'Roles', N'Name', N'ALTER TABLE [dbo].[Roles] ADD [Name] nvarchar(max) NOT NULL CONSTRAINT [DF_Roles_Name] DEFAULT (N'''');';
EXEC #EnsureColumn N'Roles', N'Description', N'ALTER TABLE [dbo].[Roles] ADD [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_Roles_Description] DEFAULT (N'''');';
EXEC #EnsureColumn N'Roles', N'IsActive', N'ALTER TABLE [dbo].[Roles] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_Roles_IsActive] DEFAULT (0);';
EXEC #EnsureColumn N'Roles', N'CreatedDate', N'ALTER TABLE [dbo].[Roles] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Roles_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- SavedReports
EXEC #EnsureColumn N'SavedReports', N'Id', N'ALTER TABLE [dbo].[SavedReports] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'SavedReports', N'TemplateId', N'ALTER TABLE [dbo].[SavedReports] ADD [TemplateId] int NOT NULL CONSTRAINT [DF_SavedReports_TemplateId] DEFAULT (0);';
EXEC #EnsureColumn N'SavedReports', N'Title', N'ALTER TABLE [dbo].[SavedReports] ADD [Title] nvarchar(max) NOT NULL CONSTRAINT [DF__SavedRepo__Title__7E8CC4B1] DEFAULT ('''');';
EXEC #EnsureColumn N'SavedReports', N'CreatedBy', N'ALTER TABLE [dbo].[SavedReports] ADD [CreatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF__SavedRepo__Creat__7F80E8EA] DEFAULT ('''');';
EXEC #EnsureColumn N'SavedReports', N'CreatedDate', N'ALTER TABLE [dbo].[SavedReports] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_SavedReports_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'SavedReports', N'Description', N'ALTER TABLE [dbo].[SavedReports] ADD [Description] nvarchar(max) NULL;';
EXEC #EnsureColumn N'SavedReports', N'FilterParams', N'ALTER TABLE [dbo].[SavedReports] ADD [FilterParams] nvarchar(max) NULL;';
EXEC #EnsureColumn N'SavedReports', N'ReportData', N'ALTER TABLE [dbo].[SavedReports] ADD [ReportData] nvarchar(max) NULL;';
EXEC #EnsureColumn N'SavedReports', N'RecordCount', N'ALTER TABLE [dbo].[SavedReports] ADD [RecordCount] int NOT NULL CONSTRAINT [DF__SavedRepo__Recor__00750D23] DEFAULT ((0));';
EXEC #EnsureColumn N'SavedReports', N'ExecutionTimeMs', N'ALTER TABLE [dbo].[SavedReports] ADD [ExecutionTimeMs] decimal(18, 2) NULL;';
GO

-- Settings
EXEC #EnsureColumn N'Settings', N'Id', N'ALTER TABLE [dbo].[Settings] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'Settings', N'Key', N'ALTER TABLE [dbo].[Settings] ADD [Key] nvarchar(max) NOT NULL CONSTRAINT [DF_Settings_Key] DEFAULT (N'''');';
EXEC #EnsureColumn N'Settings', N'Value', N'ALTER TABLE [dbo].[Settings] ADD [Value] nvarchar(max) NOT NULL CONSTRAINT [DF_Settings_Value] DEFAULT (N'''');';
EXEC #EnsureColumn N'Settings', N'Type', N'ALTER TABLE [dbo].[Settings] ADD [Type] nvarchar(max) NOT NULL CONSTRAINT [DF_Settings_Type] DEFAULT (N'''');';
EXEC #EnsureColumn N'Settings', N'Category', N'ALTER TABLE [dbo].[Settings] ADD [Category] nvarchar(max) NOT NULL CONSTRAINT [DF_Settings_Category] DEFAULT (N'''');';
EXEC #EnsureColumn N'Settings', N'Description', N'ALTER TABLE [dbo].[Settings] ADD [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_Settings_Description] DEFAULT (N'''');';
EXEC #EnsureColumn N'Settings', N'IsSystem', N'ALTER TABLE [dbo].[Settings] ADD [IsSystem] bit NOT NULL CONSTRAINT [DF_Settings_IsSystem] DEFAULT (0);';
EXEC #EnsureColumn N'Settings', N'CreatedDate', N'ALTER TABLE [dbo].[Settings] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Settings_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'Settings', N'ModifiedDate', N'ALTER TABLE [dbo].[Settings] ADD [ModifiedDate] datetime2 NULL;';
EXEC #EnsureColumn N'Settings', N'ModifiedBy', N'ALTER TABLE [dbo].[Settings] ADD [ModifiedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_Settings_ModifiedBy] DEFAULT (N'''');';
GO

-- Staff
EXEC #EnsureColumn N'Staff', N'Id', N'ALTER TABLE [dbo].[Staff] ADD [Id] {{UserId}} NOT NULL CONSTRAINT [DF_Staff_Id] DEFAULT (N'''');';
EXEC #EnsureColumn N'Staff', N'EmployeeId', N'ALTER TABLE [dbo].[Staff] ADD [EmployeeId] nvarchar(max) NOT NULL CONSTRAINT [DF_Staff_EmployeeId] DEFAULT (N'''');';
EXEC #EnsureColumn N'Staff', N'FirstName', N'ALTER TABLE [dbo].[Staff] ADD [FirstName] nvarchar(max) NOT NULL CONSTRAINT [DF_Staff_FirstName] DEFAULT (N'''');';
EXEC #EnsureColumn N'Staff', N'LastName', N'ALTER TABLE [dbo].[Staff] ADD [LastName] nvarchar(max) NOT NULL CONSTRAINT [DF_Staff_LastName] DEFAULT (N'''');';
EXEC #EnsureColumn N'Staff', N'Department', N'ALTER TABLE [dbo].[Staff] ADD [Department] nvarchar(max) NOT NULL CONSTRAINT [DF_Staff_Department] DEFAULT (N'''');';
EXEC #EnsureColumn N'Staff', N'Designation', N'ALTER TABLE [dbo].[Staff] ADD [Designation] nvarchar(max) NOT NULL CONSTRAINT [DF_Staff_Designation] DEFAULT (N'''');';
EXEC #EnsureColumn N'Staff', N'DateOfJoining', N'ALTER TABLE [dbo].[Staff] ADD [DateOfJoining] datetime2 NOT NULL CONSTRAINT [DF_Staff_DateOfJoining] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'Staff', N'Salary', N'ALTER TABLE [dbo].[Staff] ADD [Salary] decimal(18, 2) NOT NULL CONSTRAINT [DF_Staff_Salary] DEFAULT (0);';
EXEC #EnsureColumn N'Staff', N'Phone', N'ALTER TABLE [dbo].[Staff] ADD [Phone] nvarchar(max) NOT NULL CONSTRAINT [DF_Staff_Phone] DEFAULT (N'''');';
EXEC #EnsureColumn N'Staff', N'Address', N'ALTER TABLE [dbo].[Staff] ADD [Address] nvarchar(max) NOT NULL CONSTRAINT [DF_Staff_Address] DEFAULT (N'''');';
EXEC #EnsureColumn N'Staff', N'Email', N'ALTER TABLE [dbo].[Staff] ADD [Email] nvarchar(max) NOT NULL CONSTRAINT [DF_Staff_Email] DEFAULT (N'''');';
EXEC #EnsureColumn N'Staff', N'About', N'ALTER TABLE [dbo].[Staff] ADD [About] nvarchar(max) NOT NULL CONSTRAINT [DF_Staff_About] DEFAULT (N'''');';
EXEC #EnsureColumn N'Staff', N'IsActive', N'ALTER TABLE [dbo].[Staff] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_Staff_IsActive] DEFAULT (0);';
EXEC #EnsureColumn N'Staff', N'CreatedDate', N'ALTER TABLE [dbo].[Staff] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Staff_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'Staff', N'UserId', N'ALTER TABLE [dbo].[Staff] ADD [UserId] {{UserId}} NULL;';
GO

-- StaffAttendances
EXEC #EnsureColumn N'StaffAttendances', N'Id', N'ALTER TABLE [dbo].[StaffAttendances] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'StaffAttendances', N'StaffId', N'ALTER TABLE [dbo].[StaffAttendances] ADD [StaffId] {{StaffId}} NOT NULL CONSTRAINT [DF_StaffAttendances_StaffId] DEFAULT (N'''');';
EXEC #EnsureColumn N'StaffAttendances', N'AttendanceDate', N'ALTER TABLE [dbo].[StaffAttendances] ADD [AttendanceDate] datetime2 NOT NULL CONSTRAINT [DF_StaffAttendances_AttendanceDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'StaffAttendances', N'CheckInTime', N'ALTER TABLE [dbo].[StaffAttendances] ADD [CheckInTime] datetime2 NULL;';
EXEC #EnsureColumn N'StaffAttendances', N'CheckOutTime', N'ALTER TABLE [dbo].[StaffAttendances] ADD [CheckOutTime] datetime2 NULL;';
EXEC #EnsureColumn N'StaffAttendances', N'Status', N'ALTER TABLE [dbo].[StaffAttendances] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_StaffAttendances_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'StaffAttendances', N'Notes', N'ALTER TABLE [dbo].[StaffAttendances] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF_StaffAttendances_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'StaffAttendances', N'CreatedDate', N'ALTER TABLE [dbo].[StaffAttendances] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_StaffAttendances_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'StaffAttendances', N'UpdatedDate', N'ALTER TABLE [dbo].[StaffAttendances] ADD [UpdatedDate] datetime2 NULL;';
GO

-- StaffRoles
EXEC #EnsureColumn N'StaffRoles', N'StaffId', N'ALTER TABLE [dbo].[StaffRoles] ADD [StaffId] {{StaffId}} NOT NULL CONSTRAINT [DF_StaffRoles_StaffId] DEFAULT (N'''');';
EXEC #EnsureColumn N'StaffRoles', N'RoleId', N'ALTER TABLE [dbo].[StaffRoles] ADD [RoleId] int NOT NULL CONSTRAINT [DF_StaffRoles_RoleId] DEFAULT (0);';
EXEC #EnsureColumn N'StaffRoles', N'AssignedDate', N'ALTER TABLE [dbo].[StaffRoles] ADD [AssignedDate] datetime2 NOT NULL CONSTRAINT [DF_StaffRoles_AssignedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'StaffRoles', N'AssignedBy', N'ALTER TABLE [dbo].[StaffRoles] ADD [AssignedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_StaffRoles_AssignedBy] DEFAULT (N'''');';
GO

-- SystemModules
EXEC #EnsureColumn N'SystemModules', N'Id', N'ALTER TABLE [dbo].[SystemModules] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'SystemModules', N'Key', N'ALTER TABLE [dbo].[SystemModules] ADD [Key] nvarchar(50) NOT NULL CONSTRAINT [DF_SystemModules_Key] DEFAULT (N'''');';
EXEC #EnsureColumn N'SystemModules', N'DisplayName', N'ALTER TABLE [dbo].[SystemModules] ADD [DisplayName] nvarchar(100) NOT NULL CONSTRAINT [DF_SystemModules_DisplayName] DEFAULT (N'''');';
EXEC #EnsureColumn N'SystemModules', N'Description', N'ALTER TABLE [dbo].[SystemModules] ADD [Description] nvarchar(300) NULL;';
EXEC #EnsureColumn N'SystemModules', N'Icon', N'ALTER TABLE [dbo].[SystemModules] ADD [Icon] nvarchar(100) NULL;';
EXEC #EnsureColumn N'SystemModules', N'IsGloballyEnabled', N'ALTER TABLE [dbo].[SystemModules] ADD [IsGloballyEnabled] bit NOT NULL CONSTRAINT [DF__SystemMod__IsGlo__2DB1C7EE] DEFAULT ((1));';
EXEC #EnsureColumn N'SystemModules', N'SortOrder', N'ALTER TABLE [dbo].[SystemModules] ADD [SortOrder] int NOT NULL CONSTRAINT [DF__SystemMod__SortO__2EA5EC27] DEFAULT ((0));';
EXEC #EnsureColumn N'SystemModules', N'CreatedAtUtc', N'ALTER TABLE [dbo].[SystemModules] ADD [CreatedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_SystemModules_CreatedAtUtc] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'SystemModules', N'UpdatedAtUtc', N'ALTER TABLE [dbo].[SystemModules] ADD [UpdatedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_SystemModules_UpdatedAtUtc] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'SystemModules', N'UpdatedByUserId', N'ALTER TABLE [dbo].[SystemModules] ADD [UpdatedByUserId] nvarchar(450) NULL;';
GO

-- SystemNotifications
EXEC #EnsureColumn N'SystemNotifications', N'Id', N'ALTER TABLE [dbo].[SystemNotifications] ADD [Id] bigint IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'SystemNotifications', N'UserId', N'ALTER TABLE [dbo].[SystemNotifications] ADD [UserId] {{UserId}} NOT NULL CONSTRAINT [DF_SystemNotifications_UserId] DEFAULT (N'''');';
EXEC #EnsureColumn N'SystemNotifications', N'PatientId', N'ALTER TABLE [dbo].[SystemNotifications] ADD [PatientId] int NULL;';
EXEC #EnsureColumn N'SystemNotifications', N'Title', N'ALTER TABLE [dbo].[SystemNotifications] ADD [Title] nvarchar(200) NOT NULL CONSTRAINT [DF__SystemNot__Title__4C364F0E] DEFAULT ('''');';
EXEC #EnsureColumn N'SystemNotifications', N'Message', N'ALTER TABLE [dbo].[SystemNotifications] ADD [Message] nvarchar(2000) NOT NULL CONSTRAINT [DF__SystemNot__Messa__4D2A7347] DEFAULT ('''');';
EXEC #EnsureColumn N'SystemNotifications', N'Type', N'ALTER TABLE [dbo].[SystemNotifications] ADD [Type] nvarchar(50) NOT NULL CONSTRAINT [DF__SystemNoti__Type__4E1E9780] DEFAULT (''General'');';
EXEC #EnsureColumn N'SystemNotifications', N'RelatedEntityType', N'ALTER TABLE [dbo].[SystemNotifications] ADD [RelatedEntityType] nvarchar(100) NOT NULL CONSTRAINT [DF__SystemNot__Relat__4F12BBB9] DEFAULT ('''');';
EXEC #EnsureColumn N'SystemNotifications', N'RelatedEntityId', N'ALTER TABLE [dbo].[SystemNotifications] ADD [RelatedEntityId] nvarchar(100) NOT NULL CONSTRAINT [DF__SystemNot__Relat__5006DFF2] DEFAULT ('''');';
EXEC #EnsureColumn N'SystemNotifications', N'IsRead', N'ALTER TABLE [dbo].[SystemNotifications] ADD [IsRead] bit NOT NULL CONSTRAINT [DF__SystemNot__IsRea__50FB042B] DEFAULT ((0));';
EXEC #EnsureColumn N'SystemNotifications', N'CreatedAtUtc', N'ALTER TABLE [dbo].[SystemNotifications] ADD [CreatedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_SystemNotifications_CreatedAtUtc] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'SystemNotifications', N'ReadAtUtc', N'ALTER TABLE [dbo].[SystemNotifications] ADD [ReadAtUtc] datetime2 NULL;';
GO

-- TestResults
EXEC #EnsureColumn N'TestResults', N'Id', N'ALTER TABLE [dbo].[TestResults] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'TestResults', N'PatientId', N'ALTER TABLE [dbo].[TestResults] ADD [PatientId] int NOT NULL CONSTRAINT [DF_TestResults_PatientId] DEFAULT (0);';
EXEC #EnsureColumn N'TestResults', N'TestType', N'ALTER TABLE [dbo].[TestResults] ADD [TestType] nvarchar(100) NOT NULL CONSTRAINT [DF_TestResults_TestType] DEFAULT (N'''');';
EXEC #EnsureColumn N'TestResults', N'TestName', N'ALTER TABLE [dbo].[TestResults] ADD [TestName] nvarchar(200) NOT NULL CONSTRAINT [DF_TestResults_TestName] DEFAULT (N'''');';
EXEC #EnsureColumn N'TestResults', N'TestDescription', N'ALTER TABLE [dbo].[TestResults] ADD [TestDescription] nvarchar(500) NOT NULL CONSTRAINT [DF_TestResults_TestDescription] DEFAULT (N'''');';
EXEC #EnsureColumn N'TestResults', N'Result', N'ALTER TABLE [dbo].[TestResults] ADD [Result] nvarchar(max) NOT NULL CONSTRAINT [DF_TestResults_Result] DEFAULT (N'''');';
EXEC #EnsureColumn N'TestResults', N'Unit', N'ALTER TABLE [dbo].[TestResults] ADD [Unit] nvarchar(50) NOT NULL CONSTRAINT [DF_TestResults_Unit] DEFAULT (N'''');';
EXEC #EnsureColumn N'TestResults', N'ReferenceRange', N'ALTER TABLE [dbo].[TestResults] ADD [ReferenceRange] nvarchar(50) NOT NULL CONSTRAINT [DF_TestResults_ReferenceRange] DEFAULT (N'''');';
EXEC #EnsureColumn N'TestResults', N'Status', N'ALTER TABLE [dbo].[TestResults] ADD [Status] nvarchar(20) NOT NULL CONSTRAINT [DF_TestResults_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'TestResults', N'PerformedBy', N'ALTER TABLE [dbo].[TestResults] ADD [PerformedBy] nvarchar(100) NOT NULL CONSTRAINT [DF_TestResults_PerformedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'TestResults', N'DoctorId', N'ALTER TABLE [dbo].[TestResults] ADD [DoctorId] {{UserId}} NOT NULL CONSTRAINT [DF_TestResults_DoctorId] DEFAULT (N'''');';
EXEC #EnsureColumn N'TestResults', N'TestDate', N'ALTER TABLE [dbo].[TestResults] ADD [TestDate] datetime2 NOT NULL CONSTRAINT [DF_TestResults_TestDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'TestResults', N'CreatedDate', N'ALTER TABLE [dbo].[TestResults] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_TestResults_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'TestResults', N'CreatedBy', N'ALTER TABLE [dbo].[TestResults] ADD [CreatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_TestResults_CreatedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'TestResults', N'ModifiedDate', N'ALTER TABLE [dbo].[TestResults] ADD [ModifiedDate] datetime2 NULL;';
EXEC #EnsureColumn N'TestResults', N'ModifiedBy', N'ALTER TABLE [dbo].[TestResults] ADD [ModifiedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_TestResults_ModifiedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'TestResults', N'MedicalRecordId', N'ALTER TABLE [dbo].[TestResults] ADD [MedicalRecordId] int NULL;';
GO

-- TpaClaims
EXEC #EnsureColumn N'TpaClaims', N'Id', N'ALTER TABLE [dbo].[TpaClaims] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'TpaClaims', N'TpaProviderId', N'ALTER TABLE [dbo].[TpaClaims] ADD [TpaProviderId] int NOT NULL CONSTRAINT [DF_TpaClaims_TpaProviderId] DEFAULT (0);';
EXEC #EnsureColumn N'TpaClaims', N'PatientId', N'ALTER TABLE [dbo].[TpaClaims] ADD [PatientId] int NOT NULL CONSTRAINT [DF_TpaClaims_PatientId] DEFAULT (0);';
EXEC #EnsureColumn N'TpaClaims', N'BillId', N'ALTER TABLE [dbo].[TpaClaims] ADD [BillId] int NULL;';
EXEC #EnsureColumn N'TpaClaims', N'ClaimNumber', N'ALTER TABLE [dbo].[TpaClaims] ADD [ClaimNumber] nvarchar(max) NOT NULL CONSTRAINT [DF__TpaClaims__Claim__0B27A5C0] DEFAULT ('''');';
EXEC #EnsureColumn N'TpaClaims', N'ClaimedAmount', N'ALTER TABLE [dbo].[TpaClaims] ADD [ClaimedAmount] decimal(18, 2) NOT NULL CONSTRAINT [DF__TpaClaims__Claim__0C1BC9F9] DEFAULT ((0));';
EXEC #EnsureColumn N'TpaClaims', N'ApprovedAmount', N'ALTER TABLE [dbo].[TpaClaims] ADD [ApprovedAmount] decimal(18, 2) NULL;';
EXEC #EnsureColumn N'TpaClaims', N'SettledAmount', N'ALTER TABLE [dbo].[TpaClaims] ADD [SettledAmount] decimal(18, 2) NULL;';
EXEC #EnsureColumn N'TpaClaims', N'Status', N'ALTER TABLE [dbo].[TpaClaims] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF__TpaClaims__Statu__0D0FEE32] DEFAULT (''Pending'');';
EXEC #EnsureColumn N'TpaClaims', N'ClaimDate', N'ALTER TABLE [dbo].[TpaClaims] ADD [ClaimDate] datetime2 NOT NULL CONSTRAINT [DF_TpaClaims_ClaimDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'TpaClaims', N'SettlementDate', N'ALTER TABLE [dbo].[TpaClaims] ADD [SettlementDate] datetime2 NULL;';
EXEC #EnsureColumn N'TpaClaims', N'Remarks', N'ALTER TABLE [dbo].[TpaClaims] ADD [Remarks] nvarchar(max) NOT NULL CONSTRAINT [DF__TpaClaims__Remar__0E04126B] DEFAULT ('''');';
EXEC #EnsureColumn N'TpaClaims', N'CreatedDate', N'ALTER TABLE [dbo].[TpaClaims] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_TpaClaims_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- TpaProviders
EXEC #EnsureColumn N'TpaProviders', N'Id', N'ALTER TABLE [dbo].[TpaProviders] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'TpaProviders', N'Name', N'ALTER TABLE [dbo].[TpaProviders] ADD [Name] nvarchar(max) NOT NULL CONSTRAINT [DF__TpaProvide__Name__00AA174D] DEFAULT ('''');';
EXEC #EnsureColumn N'TpaProviders', N'Code', N'ALTER TABLE [dbo].[TpaProviders] ADD [Code] nvarchar(max) NOT NULL CONSTRAINT [DF__TpaProvide__Code__019E3B86] DEFAULT ('''');';
EXEC #EnsureColumn N'TpaProviders', N'ContactPerson', N'ALTER TABLE [dbo].[TpaProviders] ADD [ContactPerson] nvarchar(max) NOT NULL CONSTRAINT [DF__TpaProvid__Conta__02925FBF] DEFAULT ('''');';
EXEC #EnsureColumn N'TpaProviders', N'ContactEmail', N'ALTER TABLE [dbo].[TpaProviders] ADD [ContactEmail] nvarchar(max) NOT NULL CONSTRAINT [DF__TpaProvid__Conta__038683F8] DEFAULT ('''');';
EXEC #EnsureColumn N'TpaProviders', N'ContactPhone', N'ALTER TABLE [dbo].[TpaProviders] ADD [ContactPhone] nvarchar(max) NOT NULL CONSTRAINT [DF__TpaProvid__Conta__047AA831] DEFAULT ('''');';
EXEC #EnsureColumn N'TpaProviders', N'Address', N'ALTER TABLE [dbo].[TpaProviders] ADD [Address] nvarchar(max) NOT NULL CONSTRAINT [DF__TpaProvid__Addre__056ECC6A] DEFAULT ('''');';
EXEC #EnsureColumn N'TpaProviders', N'TpaNetwork', N'ALTER TABLE [dbo].[TpaProviders] ADD [TpaNetwork] nvarchar(max) NOT NULL CONSTRAINT [DF__TpaProvid__TpaNe__0662F0A3] DEFAULT ('''');';
EXEC #EnsureColumn N'TpaProviders', N'IsActive', N'ALTER TABLE [dbo].[TpaProviders] ADD [IsActive] bit NOT NULL CONSTRAINT [DF__TpaProvid__IsAct__075714DC] DEFAULT ((1));';
EXEC #EnsureColumn N'TpaProviders', N'Notes', N'ALTER TABLE [dbo].[TpaProviders] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF__TpaProvid__Notes__084B3915] DEFAULT ('''');';
EXEC #EnsureColumn N'TpaProviders', N'CreatedDate', N'ALTER TABLE [dbo].[TpaProviders] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_TpaProviders_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- TrainingRecords
EXEC #EnsureColumn N'TrainingRecords', N'Id', N'ALTER TABLE [dbo].[TrainingRecords] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'TrainingRecords', N'StaffId', N'ALTER TABLE [dbo].[TrainingRecords] ADD [StaffId] nvarchar(450) NOT NULL CONSTRAINT [DF_TrainingRecords_StaffId] DEFAULT (N'''');';
EXEC #EnsureColumn N'TrainingRecords', N'StaffName', N'ALTER TABLE [dbo].[TrainingRecords] ADD [StaffName] nvarchar(max) NOT NULL CONSTRAINT [DF_TrainingRecords_StaffName] DEFAULT (N'''');';
EXEC #EnsureColumn N'TrainingRecords', N'StaffDepartment', N'ALTER TABLE [dbo].[TrainingRecords] ADD [StaffDepartment] nvarchar(max) NOT NULL CONSTRAINT [DF_TrainingRecords_StaffDepartment] DEFAULT (N'''');';
EXEC #EnsureColumn N'TrainingRecords', N'CourseTitle', N'ALTER TABLE [dbo].[TrainingRecords] ADD [CourseTitle] nvarchar(200) NOT NULL CONSTRAINT [DF_TrainingRecords_CourseTitle] DEFAULT (N'''');';
EXEC #EnsureColumn N'TrainingRecords', N'Category', N'ALTER TABLE [dbo].[TrainingRecords] ADD [Category] nvarchar(50) NOT NULL CONSTRAINT [DF_TrainingRecords_Category] DEFAULT (N'''');';
EXEC #EnsureColumn N'TrainingRecords', N'Provider', N'ALTER TABLE [dbo].[TrainingRecords] ADD [Provider] nvarchar(150) NOT NULL CONSTRAINT [DF_TrainingRecords_Provider] DEFAULT (N'''');';
EXEC #EnsureColumn N'TrainingRecords', N'CompletedDate', N'ALTER TABLE [dbo].[TrainingRecords] ADD [CompletedDate] datetime2 NOT NULL CONSTRAINT [DF_TrainingRecords_CompletedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'TrainingRecords', N'ExpiryDate', N'ALTER TABLE [dbo].[TrainingRecords] ADD [ExpiryDate] datetime2 NULL;';
EXEC #EnsureColumn N'TrainingRecords', N'CertificateNumber', N'ALTER TABLE [dbo].[TrainingRecords] ADD [CertificateNumber] nvarchar(100) NOT NULL CONSTRAINT [DF_TrainingRecords_CertificateNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'TrainingRecords', N'Result', N'ALTER TABLE [dbo].[TrainingRecords] ADD [Result] nvarchar(50) NOT NULL CONSTRAINT [DF_TrainingRecords_Result] DEFAULT (N'''');';
EXEC #EnsureColumn N'TrainingRecords', N'Notes', N'ALTER TABLE [dbo].[TrainingRecords] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF_TrainingRecords_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'TrainingRecords', N'CreatedAt', N'ALTER TABLE [dbo].[TrainingRecords] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_TrainingRecords_CreatedAt] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'TrainingRecords', N'CreatedBy', N'ALTER TABLE [dbo].[TrainingRecords] ADD [CreatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_TrainingRecords_CreatedBy] DEFAULT (N'''');';
GO

-- Transactions
EXEC #EnsureColumn N'Transactions', N'Id', N'ALTER TABLE [dbo].[Transactions] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'Transactions', N'TransactionId', N'ALTER TABLE [dbo].[Transactions] ADD [TransactionId] nvarchar(max) NOT NULL CONSTRAINT [DF_Transactions_TransactionId] DEFAULT (N'''');';
EXEC #EnsureColumn N'Transactions', N'TransactionType', N'ALTER TABLE [dbo].[Transactions] ADD [TransactionType] nvarchar(max) NOT NULL CONSTRAINT [DF_Transactions_TransactionType] DEFAULT (N'''');';
EXEC #EnsureColumn N'Transactions', N'Amount', N'ALTER TABLE [dbo].[Transactions] ADD [Amount] decimal(18, 2) NOT NULL CONSTRAINT [DF_Transactions_Amount] DEFAULT (0);';
EXEC #EnsureColumn N'Transactions', N'Description', N'ALTER TABLE [dbo].[Transactions] ADD [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_Transactions_Description] DEFAULT (N'''');';
EXEC #EnsureColumn N'Transactions', N'ReferenceNumber', N'ALTER TABLE [dbo].[Transactions] ADD [ReferenceNumber] nvarchar(max) NOT NULL CONSTRAINT [DF_Transactions_ReferenceNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'Transactions', N'TransactionDate', N'ALTER TABLE [dbo].[Transactions] ADD [TransactionDate] datetime2 NOT NULL CONSTRAINT [DF_Transactions_TransactionDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'Transactions', N'ProcessedBy', N'ALTER TABLE [dbo].[Transactions] ADD [ProcessedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_Transactions_ProcessedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'Transactions', N'Status', N'ALTER TABLE [dbo].[Transactions] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_Transactions_Status] DEFAULT (N'''');';
GO

-- UserActionLogs
EXEC #EnsureColumn N'UserActionLogs', N'Id', N'ALTER TABLE [dbo].[UserActionLogs] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'UserActionLogs', N'UserId', N'ALTER TABLE [dbo].[UserActionLogs] ADD [UserId] nvarchar(max) NOT NULL CONSTRAINT [DF_UserActionLogs_UserId] DEFAULT (N'''');';
EXEC #EnsureColumn N'UserActionLogs', N'ActionType', N'ALTER TABLE [dbo].[UserActionLogs] ADD [ActionType] nvarchar(max) NOT NULL CONSTRAINT [DF_UserActionLogs_ActionType] DEFAULT (N'''');';
EXEC #EnsureColumn N'UserActionLogs', N'Details', N'ALTER TABLE [dbo].[UserActionLogs] ADD [Details] nvarchar(max) NOT NULL CONSTRAINT [DF_UserActionLogs_Details] DEFAULT (N'''');';
EXEC #EnsureColumn N'UserActionLogs', N'IPAddress', N'ALTER TABLE [dbo].[UserActionLogs] ADD [IPAddress] nvarchar(max) NOT NULL CONSTRAINT [DF_UserActionLogs_IPAddress] DEFAULT (N'''');';
EXEC #EnsureColumn N'UserActionLogs', N'LoggedDate', N'ALTER TABLE [dbo].[UserActionLogs] ADD [LoggedDate] datetime2 NOT NULL CONSTRAINT [DF_UserActionLogs_LoggedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'UserActionLogs', N'Status', N'ALTER TABLE [dbo].[UserActionLogs] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_UserActionLogs_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'UserActionLogs', N'StaffId', N'ALTER TABLE [dbo].[UserActionLogs] ADD [StaffId] {{StaffId}} NOT NULL CONSTRAINT [DF_UserActionLogs_StaffId] DEFAULT (N'''');';
GO

-- UserHospitalAccesses
EXEC #EnsureColumn N'UserHospitalAccesses', N'Id', N'ALTER TABLE [dbo].[UserHospitalAccesses] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'UserHospitalAccesses', N'UserId', N'ALTER TABLE [dbo].[UserHospitalAccesses] ADD [UserId] {{UserId}} NOT NULL CONSTRAINT [DF_UserHospitalAccesses_UserId] DEFAULT (N'''');';
EXEC #EnsureColumn N'UserHospitalAccesses', N'HospitalId', N'ALTER TABLE [dbo].[UserHospitalAccesses] ADD [HospitalId] int NOT NULL CONSTRAINT [DF_UserHospitalAccesses_HospitalId] DEFAULT (0);';
EXEC #EnsureColumn N'UserHospitalAccesses', N'IsDefault', N'ALTER TABLE [dbo].[UserHospitalAccesses] ADD [IsDefault] bit NOT NULL CONSTRAINT [DF_UserHospitalAccesses_IsDefault] DEFAULT ((0));';
EXEC #EnsureColumn N'UserHospitalAccesses', N'CreatedDate', N'ALTER TABLE [dbo].[UserHospitalAccesses] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_UserHospitalAccesses_CreatedDate] DEFAULT (sysutcdatetime());';
GO

-- UserModuleAccesses
EXEC #EnsureColumn N'UserModuleAccesses', N'Id', N'ALTER TABLE [dbo].[UserModuleAccesses] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'UserModuleAccesses', N'UserId', N'ALTER TABLE [dbo].[UserModuleAccesses] ADD [UserId] {{UserId}} NOT NULL CONSTRAINT [DF_UserModuleAccesses_UserId] DEFAULT (N'''');';
EXEC #EnsureColumn N'UserModuleAccesses', N'ModuleId', N'ALTER TABLE [dbo].[UserModuleAccesses] ADD [ModuleId] int NOT NULL CONSTRAINT [DF_UserModuleAccesses_ModuleId] DEFAULT (0);';
EXEC #EnsureColumn N'UserModuleAccesses', N'IsEnabled', N'ALTER TABLE [dbo].[UserModuleAccesses] ADD [IsEnabled] bit NOT NULL CONSTRAINT [DF__UserModul__IsEna__318258D2] DEFAULT ((1));';
EXEC #EnsureColumn N'UserModuleAccesses', N'CreatedAtUtc', N'ALTER TABLE [dbo].[UserModuleAccesses] ADD [CreatedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_UserModuleAccesses_CreatedAtUtc] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'UserModuleAccesses', N'UpdatedAtUtc', N'ALTER TABLE [dbo].[UserModuleAccesses] ADD [UpdatedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_UserModuleAccesses_UpdatedAtUtc] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'UserModuleAccesses', N'UpdatedByUserId', N'ALTER TABLE [dbo].[UserModuleAccesses] ADD [UpdatedByUserId] nvarchar(128) NULL;';
GO

-- UserSessions
EXEC #EnsureColumn N'UserSessions', N'Id', N'ALTER TABLE [dbo].[UserSessions] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'UserSessions', N'UserId', N'ALTER TABLE [dbo].[UserSessions] ADD [UserId] {{UserId}} NOT NULL CONSTRAINT [DF_UserSessions_UserId] DEFAULT (N'''');';
EXEC #EnsureColumn N'UserSessions', N'SessionId', N'ALTER TABLE [dbo].[UserSessions] ADD [SessionId] nvarchar(128) NOT NULL CONSTRAINT [DF_UserSessions_SessionId] DEFAULT (N'''');';
EXEC #EnsureColumn N'UserSessions', N'ActiveRole', N'ALTER TABLE [dbo].[UserSessions] ADD [ActiveRole] nvarchar(50) NOT NULL CONSTRAINT [DF_UserSessions_ActiveRole] DEFAULT (N'''');';
EXEC #EnsureColumn N'UserSessions', N'IpAddress', N'ALTER TABLE [dbo].[UserSessions] ADD [IpAddress] nvarchar(64) NULL;';
EXEC #EnsureColumn N'UserSessions', N'UserAgent', N'ALTER TABLE [dbo].[UserSessions] ADD [UserAgent] nvarchar(512) NULL;';
EXEC #EnsureColumn N'UserSessions', N'LoginAtUtc', N'ALTER TABLE [dbo].[UserSessions] ADD [LoginAtUtc] datetime2 NOT NULL CONSTRAINT [DF_UserSessions_LoginAtUtc] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'UserSessions', N'LastActivityUtc', N'ALTER TABLE [dbo].[UserSessions] ADD [LastActivityUtc] datetime2 NOT NULL CONSTRAINT [DF_UserSessions_LastActivityUtc] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'UserSessions', N'LogoutAtUtc', N'ALTER TABLE [dbo].[UserSessions] ADD [LogoutAtUtc] datetime2 NULL;';
EXEC #EnsureColumn N'UserSessions', N'IsActive', N'ALTER TABLE [dbo].[UserSessions] ADD [IsActive] bit NOT NULL CONSTRAINT [DF__UserSessi__IsAct__29E1370A] DEFAULT ((1));';
GO

-- UserThemePreferences
EXEC #EnsureColumn N'UserThemePreferences', N'Id', N'ALTER TABLE [dbo].[UserThemePreferences] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'UserThemePreferences', N'UserId', N'ALTER TABLE [dbo].[UserThemePreferences] ADD [UserId] {{UserId}} NOT NULL CONSTRAINT [DF_UserThemePreferences_UserId] DEFAULT (N'''');';
EXEC #EnsureColumn N'UserThemePreferences', N'ThemeId', N'ALTER TABLE [dbo].[UserThemePreferences] ADD [ThemeId] nvarchar(50) NOT NULL CONSTRAINT [DF_UserThemePreferences_ThemeId] DEFAULT (N'''');';
EXEC #EnsureColumn N'UserThemePreferences', N'PreferenceSince', N'ALTER TABLE [dbo].[UserThemePreferences] ADD [PreferenceSince] datetime2 NOT NULL CONSTRAINT [DF_UserThemePreferences_PreferenceSince] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'UserThemePreferences', N'IsDefault', N'ALTER TABLE [dbo].[UserThemePreferences] ADD [IsDefault] bit NOT NULL CONSTRAINT [DF__UserTheme__IsDef__062DE679] DEFAULT ((0));';
GO

-- VendorPayments
EXEC #EnsureColumn N'VendorPayments', N'Id', N'ALTER TABLE [dbo].[VendorPayments] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'VendorPayments', N'PurchaseBillId', N'ALTER TABLE [dbo].[VendorPayments] ADD [PurchaseBillId] int NOT NULL CONSTRAINT [DF_VendorPayments_PurchaseBillId] DEFAULT (0);';
EXEC #EnsureColumn N'VendorPayments', N'Amount', N'ALTER TABLE [dbo].[VendorPayments] ADD [Amount] decimal(18, 2) NOT NULL CONSTRAINT [DF_VendorPayments_Amount] DEFAULT (0);';
EXEC #EnsureColumn N'VendorPayments', N'PaymentDate', N'ALTER TABLE [dbo].[VendorPayments] ADD [PaymentDate] datetime2 NOT NULL CONSTRAINT [DF_VendorPayments_PaymentDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'VendorPayments', N'Method', N'ALTER TABLE [dbo].[VendorPayments] ADD [Method] nvarchar(30) NOT NULL CONSTRAINT [DF_VendorPayments_Method] DEFAULT (N'''');';
EXEC #EnsureColumn N'VendorPayments', N'Reference', N'ALTER TABLE [dbo].[VendorPayments] ADD [Reference] nvarchar(100) NOT NULL CONSTRAINT [DF_VendorPayments_Reference] DEFAULT (N'''');';
EXEC #EnsureColumn N'VendorPayments', N'Notes', N'ALTER TABLE [dbo].[VendorPayments] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF_VendorPayments_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'VendorPayments', N'CreatedBy', N'ALTER TABLE [dbo].[VendorPayments] ADD [CreatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_VendorPayments_CreatedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'VendorPayments', N'CreatedAt', N'ALTER TABLE [dbo].[VendorPayments] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_VendorPayments_CreatedAt] DEFAULT (''1900-01-01'');';
GO

-- Vendors
EXEC #EnsureColumn N'Vendors', N'Id', N'ALTER TABLE [dbo].[Vendors] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'Vendors', N'VendorCode', N'ALTER TABLE [dbo].[Vendors] ADD [VendorCode] nvarchar(20) NOT NULL CONSTRAINT [DF_Vendors_VendorCode] DEFAULT (N'''');';
EXEC #EnsureColumn N'Vendors', N'Name', N'ALTER TABLE [dbo].[Vendors] ADD [Name] nvarchar(150) NOT NULL CONSTRAINT [DF_Vendors_Name] DEFAULT (N'''');';
EXEC #EnsureColumn N'Vendors', N'Category', N'ALTER TABLE [dbo].[Vendors] ADD [Category] nvarchar(50) NOT NULL CONSTRAINT [DF_Vendors_Category] DEFAULT (N'''');';
EXEC #EnsureColumn N'Vendors', N'ContactPerson', N'ALTER TABLE [dbo].[Vendors] ADD [ContactPerson] nvarchar(100) NOT NULL CONSTRAINT [DF_Vendors_ContactPerson] DEFAULT (N'''');';
EXEC #EnsureColumn N'Vendors', N'Phone', N'ALTER TABLE [dbo].[Vendors] ADD [Phone] nvarchar(50) NOT NULL CONSTRAINT [DF_Vendors_Phone] DEFAULT (N'''');';
EXEC #EnsureColumn N'Vendors', N'Email', N'ALTER TABLE [dbo].[Vendors] ADD [Email] nvarchar(150) NOT NULL CONSTRAINT [DF_Vendors_Email] DEFAULT (N'''');';
EXEC #EnsureColumn N'Vendors', N'Address', N'ALTER TABLE [dbo].[Vendors] ADD [Address] nvarchar(300) NOT NULL CONSTRAINT [DF_Vendors_Address] DEFAULT (N'''');';
EXEC #EnsureColumn N'Vendors', N'City', N'ALTER TABLE [dbo].[Vendors] ADD [City] nvarchar(100) NOT NULL CONSTRAINT [DF_Vendors_City] DEFAULT (N'''');';
EXEC #EnsureColumn N'Vendors', N'TaxNumber', N'ALTER TABLE [dbo].[Vendors] ADD [TaxNumber] nvarchar(50) NOT NULL CONSTRAINT [DF_Vendors_TaxNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'Vendors', N'LicenseNumber', N'ALTER TABLE [dbo].[Vendors] ADD [LicenseNumber] nvarchar(100) NOT NULL CONSTRAINT [DF_Vendors_LicenseNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'Vendors', N'PaymentTermsDays', N'ALTER TABLE [dbo].[Vendors] ADD [PaymentTermsDays] int NOT NULL CONSTRAINT [DF_Vendors_PaymentTermsDays] DEFAULT (0);';
EXEC #EnsureColumn N'Vendors', N'BankName', N'ALTER TABLE [dbo].[Vendors] ADD [BankName] nvarchar(100) NOT NULL CONSTRAINT [DF_Vendors_BankName] DEFAULT (N'''');';
EXEC #EnsureColumn N'Vendors', N'BankAccountName', N'ALTER TABLE [dbo].[Vendors] ADD [BankAccountName] nvarchar(100) NOT NULL CONSTRAINT [DF_Vendors_BankAccountName] DEFAULT (N'''');';
EXEC #EnsureColumn N'Vendors', N'BankAccountNumber', N'ALTER TABLE [dbo].[Vendors] ADD [BankAccountNumber] nvarchar(50) NOT NULL CONSTRAINT [DF_Vendors_BankAccountNumber] DEFAULT (N'''');';
EXEC #EnsureColumn N'Vendors', N'BankRoutingCode', N'ALTER TABLE [dbo].[Vendors] ADD [BankRoutingCode] nvarchar(30) NOT NULL CONSTRAINT [DF_Vendors_BankRoutingCode] DEFAULT (N'''');';
EXEC #EnsureColumn N'Vendors', N'Rating', N'ALTER TABLE [dbo].[Vendors] ADD [Rating] int NOT NULL CONSTRAINT [DF_Vendors_Rating] DEFAULT (0);';
EXEC #EnsureColumn N'Vendors', N'IsActive', N'ALTER TABLE [dbo].[Vendors] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_Vendors_IsActive] DEFAULT (0);';
EXEC #EnsureColumn N'Vendors', N'Notes', N'ALTER TABLE [dbo].[Vendors] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF_Vendors_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'Vendors', N'CreatedAt', N'ALTER TABLE [dbo].[Vendors] ADD [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_Vendors_CreatedAt] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'Vendors', N'CreatedBy', N'ALTER TABLE [dbo].[Vendors] ADD [CreatedBy] nvarchar(max) NOT NULL CONSTRAINT [DF_Vendors_CreatedBy] DEFAULT (N'''');';
EXEC #EnsureColumn N'Vendors', N'UpdatedAt', N'ALTER TABLE [dbo].[Vendors] ADD [UpdatedAt] datetime2 NULL;';
GO

-- VisitNoteHistories
EXEC #EnsureColumn N'VisitNoteHistories', N'Id', N'ALTER TABLE [dbo].[VisitNoteHistories] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'VisitNoteHistories', N'OPDVisitId', N'ALTER TABLE [dbo].[VisitNoteHistories] ADD [OPDVisitId] int NOT NULL CONSTRAINT [DF_VisitNoteHistories_OPDVisitId] DEFAULT (0);';
EXEC #EnsureColumn N'VisitNoteHistories', N'Notes', N'ALTER TABLE [dbo].[VisitNoteHistories] ADD [Notes] nvarchar(4000) NOT NULL CONSTRAINT [DF__VisitNote__Notes__42ACE4D4] DEFAULT ('''');';
EXEC #EnsureColumn N'VisitNoteHistories', N'UpdatedBy', N'ALTER TABLE [dbo].[VisitNoteHistories] ADD [UpdatedBy] nvarchar(256) NOT NULL CONSTRAINT [DF__VisitNote__Updat__43A1090D] DEFAULT ('''');';
EXEC #EnsureColumn N'VisitNoteHistories', N'UpdatedAtUtc', N'ALTER TABLE [dbo].[VisitNoteHistories] ADD [UpdatedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_VisitNoteHistories_UpdatedAtUtc] DEFAULT (''1900-01-01'');';
GO

-- VisitorLogs
EXEC #EnsureColumn N'VisitorLogs', N'Id', N'ALTER TABLE [dbo].[VisitorLogs] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'VisitorLogs', N'VisitorName', N'ALTER TABLE [dbo].[VisitorLogs] ADD [VisitorName] nvarchar(max) NOT NULL CONSTRAINT [DF_VisitorLogs_VisitorName] DEFAULT (N'''');';
EXEC #EnsureColumn N'VisitorLogs', N'Phone', N'ALTER TABLE [dbo].[VisitorLogs] ADD [Phone] nvarchar(max) NOT NULL CONSTRAINT [DF_VisitorLogs_Phone] DEFAULT (N'''');';
EXEC #EnsureColumn N'VisitorLogs', N'Purpose', N'ALTER TABLE [dbo].[VisitorLogs] ADD [Purpose] nvarchar(max) NOT NULL CONSTRAINT [DF_VisitorLogs_Purpose] DEFAULT (N'''');';
EXEC #EnsureColumn N'VisitorLogs', N'PersonToMeet', N'ALTER TABLE [dbo].[VisitorLogs] ADD [PersonToMeet] nvarchar(max) NOT NULL CONSTRAINT [DF_VisitorLogs_PersonToMeet] DEFAULT (N'''');';
EXEC #EnsureColumn N'VisitorLogs', N'VisitDate', N'ALTER TABLE [dbo].[VisitorLogs] ADD [VisitDate] datetime2 NOT NULL CONSTRAINT [DF_VisitorLogs_VisitDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'VisitorLogs', N'CheckInTime', N'ALTER TABLE [dbo].[VisitorLogs] ADD [CheckInTime] datetime2 NOT NULL CONSTRAINT [DF_VisitorLogs_CheckInTime] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'VisitorLogs', N'CheckOutTime', N'ALTER TABLE [dbo].[VisitorLogs] ADD [CheckOutTime] datetime2 NULL;';
EXEC #EnsureColumn N'VisitorLogs', N'Status', N'ALTER TABLE [dbo].[VisitorLogs] ADD [Status] nvarchar(max) NOT NULL CONSTRAINT [DF_VisitorLogs_Status] DEFAULT (N'''');';
EXEC #EnsureColumn N'VisitorLogs', N'Notes', N'ALTER TABLE [dbo].[VisitorLogs] ADD [Notes] nvarchar(max) NOT NULL CONSTRAINT [DF_VisitorLogs_Notes] DEFAULT (N'''');';
EXEC #EnsureColumn N'VisitorLogs', N'CreatedDate', N'ALTER TABLE [dbo].[VisitorLogs] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_VisitorLogs_CreatedDate] DEFAULT (''1900-01-01'');';
GO

-- Wards
EXEC #EnsureColumn N'Wards', N'Id', N'ALTER TABLE [dbo].[Wards] ADD [Id] int IDENTITY(1, 1) NOT NULL;';
EXEC #EnsureColumn N'Wards', N'Name', N'ALTER TABLE [dbo].[Wards] ADD [Name] nvarchar(max) NOT NULL CONSTRAINT [DF_Wards_Name] DEFAULT (N'''');';
EXEC #EnsureColumn N'Wards', N'Description', N'ALTER TABLE [dbo].[Wards] ADD [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_Wards_Description] DEFAULT (N'''');';
EXEC #EnsureColumn N'Wards', N'TotalBeds', N'ALTER TABLE [dbo].[Wards] ADD [TotalBeds] int NOT NULL CONSTRAINT [DF_Wards_TotalBeds] DEFAULT (0);';
EXEC #EnsureColumn N'Wards', N'OccupiedBeds', N'ALTER TABLE [dbo].[Wards] ADD [OccupiedBeds] int NOT NULL CONSTRAINT [DF_Wards_OccupiedBeds] DEFAULT (0);';
EXEC #EnsureColumn N'Wards', N'IsActive', N'ALTER TABLE [dbo].[Wards] ADD [IsActive] bit NOT NULL CONSTRAINT [DF_Wards_IsActive] DEFAULT (0);';
EXEC #EnsureColumn N'Wards', N'CreatedDate', N'ALTER TABLE [dbo].[Wards] ADD [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Wards_CreatedDate] DEFAULT (''1900-01-01'');';
EXEC #EnsureColumn N'Wards', N'HospitalId', N'ALTER TABLE [dbo].[Wards] ADD [HospitalId] int NULL;';
GO

/* ============================================================
   3. Indexes and unique constraints
   ============================================================ */

-- AccountApprovalRequests
EXEC #EnsureIndex N'AccountApprovalRequests', N'UX_AccountApprovalRequests_RequestedUserId', N'RequestedUserId', N'CREATE UNIQUE INDEX [UX_AccountApprovalRequests_RequestedUserId] ON [dbo].[AccountApprovalRequests] ([RequestedUserId]);', 1;
EXEC #EnsureIndex N'AccountApprovalRequests', N'IX_AccountApprovalRequests_Status_RequestedAtUtc', N'Status,RequestedAtUtc', N'CREATE INDEX [IX_AccountApprovalRequests_Status_RequestedAtUtc] ON [dbo].[AccountApprovalRequests] ([Status], [RequestedAtUtc]);', 0;
GO

-- AmbulanceDispatches
EXEC #EnsureIndex N'AmbulanceDispatches', N'IX_AmbulanceDispatches_AmbulanceVehicleId', N'AmbulanceVehicleId', N'CREATE INDEX [IX_AmbulanceDispatches_AmbulanceVehicleId] ON [dbo].[AmbulanceDispatches] ([AmbulanceVehicleId]);', 0;
EXEC #EnsureIndex N'AmbulanceDispatches', N'IX_AmbulanceDispatches_PatientId', N'PatientId', N'CREATE INDEX [IX_AmbulanceDispatches_PatientId] ON [dbo].[AmbulanceDispatches] ([PatientId]);', 0;
EXEC #EnsureIndex N'AmbulanceDispatches', N'IX_AmbulanceDispatches_DispatchTime', N'DispatchTime', N'CREATE INDEX [IX_AmbulanceDispatches_DispatchTime] ON [dbo].[AmbulanceDispatches] ([DispatchTime]);', 0;
GO

-- AmbulanceVehicles
EXEC #EnsureIndex N'AmbulanceVehicles', N'IX_AmbulanceVehicles_VehicleNumber', N'VehicleNumber', N'CREATE UNIQUE INDEX [IX_AmbulanceVehicles_VehicleNumber] ON [dbo].[AmbulanceVehicles] ([VehicleNumber]);', 1;
GO

-- Appointments
EXEC #EnsureIndex N'Appointments', N'IX_Appointments_DoctorId', N'DoctorId', N'CREATE INDEX [IX_Appointments_DoctorId] ON [dbo].[Appointments] ([DoctorId]);', 0;
EXEC #EnsureIndex N'Appointments', N'IX_Appointments_PatientId', N'PatientId', N'CREATE INDEX [IX_Appointments_PatientId] ON [dbo].[Appointments] ([PatientId]);', 0;
EXEC #EnsureIndex N'Appointments', N'IX_Appointments_StaffId', N'StaffId', N'CREATE INDEX [IX_Appointments_StaffId] ON [dbo].[Appointments] ([StaffId]);', 0;
EXEC #EnsureIndex N'Appointments', N'IX_Appointment_DateRange', N'AppointmentDate', N'CREATE INDEX [IX_Appointment_DateRange] ON [dbo].[Appointments] ([AppointmentDate]);', 0;
EXEC #EnsureIndex N'Appointments', N'IX_Appointments_HospitalId', N'HospitalId', N'CREATE INDEX [IX_Appointments_HospitalId] ON [dbo].[Appointments] ([HospitalId]);', 0;
GO

-- AspNetRoleClaims
EXEC #EnsureIndex N'AspNetRoleClaims', N'IX_AspNetRoleClaims_RoleId', N'RoleId', N'CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [dbo].[AspNetRoleClaims] ([RoleId]);', 0;
GO

-- AspNetRoles
EXEC #EnsureIndex N'AspNetRoles', N'RoleNameIndex', N'NormalizedName', N'CREATE UNIQUE INDEX [RoleNameIndex] ON [dbo].[AspNetRoles] ([NormalizedName]) WHERE ([NormalizedName] IS NOT NULL);', 1, N'([NormalizedName] IS NOT NULL)';
GO

-- AspNetUserClaims
EXEC #EnsureIndex N'AspNetUserClaims', N'IX_AspNetUserClaims_UserId', N'UserId', N'CREATE INDEX [IX_AspNetUserClaims_UserId] ON [dbo].[AspNetUserClaims] ([UserId]);', 0;
GO

-- AspNetUserLogins
EXEC #EnsureIndex N'AspNetUserLogins', N'IX_AspNetUserLogins_UserId', N'UserId', N'CREATE INDEX [IX_AspNetUserLogins_UserId] ON [dbo].[AspNetUserLogins] ([UserId]);', 0;
GO

-- AspNetUserRoles
EXEC #EnsureIndex N'AspNetUserRoles', N'IX_AspNetUserRoles_RoleId', N'RoleId', N'CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [dbo].[AspNetUserRoles] ([RoleId]);', 0;
GO

-- AspNetUsers
EXEC #EnsureIndex N'AspNetUsers', N'EmailIndex', N'NormalizedEmail', N'CREATE INDEX [EmailIndex] ON [dbo].[AspNetUsers] ([NormalizedEmail]);', 0;
EXEC #EnsureIndex N'AspNetUsers', N'UserNameIndex', N'NormalizedUserName', N'CREATE UNIQUE INDEX [UserNameIndex] ON [dbo].[AspNetUsers] ([NormalizedUserName]) WHERE ([NormalizedUserName] IS NOT NULL);', 1, N'([NormalizedUserName] IS NOT NULL)';
EXEC #EnsureIndex N'AspNetUsers', N'UX_AspNetUsers_UserName', N'UserName', N'CREATE UNIQUE INDEX [UX_AspNetUsers_UserName] ON [dbo].[AspNetUsers] ([UserName]);', 1;
EXEC #EnsureIndex N'AspNetUsers', N'UX_AspNetUsers_Id_UserName', N'Id,UserName', N'CREATE UNIQUE INDEX [UX_AspNetUsers_Id_UserName] ON [dbo].[AspNetUsers] ([Id], [UserName]);', 1;
EXEC #EnsureIndex N'AspNetUsers', N'IX_AspNetUsers_Id_NormalizedUserName', N'Id,NormalizedUserName', N'CREATE UNIQUE INDEX [IX_AspNetUsers_Id_NormalizedUserName] ON [dbo].[AspNetUsers] ([Id], [NormalizedUserName]) WHERE ([NormalizedUserName] IS NOT NULL);', 1, N'([NormalizedUserName] IS NOT NULL)';
GO

-- AuditFindings
EXEC #EnsureIndex N'AuditFindings', N'IX_AuditFindings_AuditId', N'AuditId', N'CREATE INDEX [IX_AuditFindings_AuditId] ON [dbo].[AuditFindings] ([AuditId]);', 0;
GO

-- AuditLogs
EXEC #EnsureIndex N'AuditLogs', N'IX_AuditLogs_UserId', N'UserId', N'CREATE INDEX [IX_AuditLogs_UserId] ON [dbo].[AuditLogs] ([UserId]);', 0;
GO

-- Beds
EXEC #EnsureIndex N'Beds', N'IX_Beds_WardId', N'WardId', N'CREATE INDEX [IX_Beds_WardId] ON [dbo].[Beds] ([WardId]);', 0;
EXEC #EnsureIndex N'Beds', N'IX_Beds_PatientId', N'PatientId', N'CREATE INDEX [IX_Beds_PatientId] ON [dbo].[Beds] ([PatientId]);', 0;
GO

-- BillItems
EXEC #EnsureIndex N'BillItems', N'IX_BillItems_BillId', N'BillId', N'CREATE INDEX [IX_BillItems_BillId] ON [dbo].[BillItems] ([BillId]);', 0;
GO

-- Bills
EXEC #EnsureIndex N'Bills', N'IX_Bills_PatientId', N'PatientId', N'CREATE INDEX [IX_Bills_PatientId] ON [dbo].[Bills] ([PatientId]);', 0;
EXEC #EnsureIndex N'Bills', N'IX_Bills_DateRange', N'BillDate', N'CREATE INDEX [IX_Bills_DateRange] ON [dbo].[Bills] ([BillDate]);', 0;
EXEC #EnsureIndex N'Bills', N'IX_Bills_HospitalId', N'HospitalId', N'CREATE INDEX [IX_Bills_HospitalId] ON [dbo].[Bills] ([HospitalId]);', 0;
GO

-- BirthRecords
EXEC #EnsureIndex N'BirthRecords', N'IX_BirthRecords_PatientId', N'PatientId', N'CREATE INDEX [IX_BirthRecords_PatientId] ON [dbo].[BirthRecords] ([PatientId]);', 0;
GO

-- BloodIssues
EXEC #EnsureIndex N'BloodIssues', N'IX_BloodIssues_BillId', N'BillId', N'CREATE INDEX [IX_BloodIssues_BillId] ON [dbo].[BloodIssues] ([BillId]);', 0;
EXEC #EnsureIndex N'BloodIssues', N'IX_BloodIssues_PatientId', N'PatientId', N'CREATE INDEX [IX_BloodIssues_PatientId] ON [dbo].[BloodIssues] ([PatientId]);', 0;
GO

-- CapaActions
EXEC #EnsureIndex N'CapaActions', N'IX_CapaActions_AuditFindingId', N'AuditFindingId', N'CREATE INDEX [IX_CapaActions_AuditFindingId] ON [dbo].[CapaActions] ([AuditFindingId]);', 0;
EXEC #EnsureIndex N'CapaActions', N'IX_CapaActions_HospitalId', N'HospitalId', N'CREATE INDEX [IX_CapaActions_HospitalId] ON [dbo].[CapaActions] ([HospitalId]);', 0;
EXEC #EnsureIndex N'CapaActions', N'IX_CapaActions_IncidentId', N'IncidentId', N'CREATE INDEX [IX_CapaActions_IncidentId] ON [dbo].[CapaActions] ([IncidentId]);', 0;
GO

-- CertificateRecords
EXEC #EnsureIndex N'CertificateRecords', N'IX_CertificateRecords_StaffId', N'StaffId', N'CREATE INDEX [IX_CertificateRecords_StaffId] ON [dbo].[CertificateRecords] ([StaffId]);', 0;
GO

-- ChatbotEventLogs
EXEC #EnsureIndex N'ChatbotEventLogs', N'IX_ChatbotEventLogs_SessionId_CreatedAtUtc', N'SessionId,CreatedAtUtc', N'CREATE INDEX [IX_ChatbotEventLogs_SessionId_CreatedAtUtc] ON [dbo].[ChatbotEventLogs] ([SessionId], [CreatedAtUtc]);', 0;
GO

-- ChatEscalations
EXEC #EnsureIndex N'ChatEscalations', N'IX_ChatEscalations_MessageId', N'MessageId', N'CREATE INDEX [IX_ChatEscalations_MessageId] ON [dbo].[ChatEscalations] ([MessageId]);', 0;
EXEC #EnsureIndex N'ChatEscalations', N'IX_ChatEscalations_SessionId', N'SessionId', N'CREATE INDEX [IX_ChatEscalations_SessionId] ON [dbo].[ChatEscalations] ([SessionId]);', 0;
EXEC #EnsureIndex N'ChatEscalations', N'IX_ChatEscalations_Status_CreatedAtUtc', N'Status,CreatedAtUtc', N'CREATE INDEX [IX_ChatEscalations_Status_CreatedAtUtc] ON [dbo].[ChatEscalations] ([Status], [CreatedAtUtc]);', 0;
GO

-- ChatFeedback
EXEC #EnsureIndex N'ChatFeedback', N'IX_ChatFeedback_MessageId', N'MessageId', N'CREATE INDEX [IX_ChatFeedback_MessageId] ON [dbo].[ChatFeedback] ([MessageId]);', 0;
EXEC #EnsureIndex N'ChatFeedback', N'IX_ChatFeedback_SessionId_CreatedAtUtc', N'SessionId,CreatedAtUtc', N'CREATE INDEX [IX_ChatFeedback_SessionId_CreatedAtUtc] ON [dbo].[ChatFeedback] ([SessionId], [CreatedAtUtc]);', 0;
GO

-- ChatMessages
EXEC #EnsureIndex N'ChatMessages', N'IX_ChatMessages_SessionId_CreatedAtUtc', N'SessionId,CreatedAtUtc', N'CREATE INDEX [IX_ChatMessages_SessionId_CreatedAtUtc] ON [dbo].[ChatMessages] ([SessionId], [CreatedAtUtc]);', 0;
GO

-- CmsMenuItems
EXEC #EnsureIndex N'CmsMenuItems', N'IX_CmsMenuItems_CmsPageId', N'CmsPageId', N'CREATE INDEX [IX_CmsMenuItems_CmsPageId] ON [dbo].[CmsMenuItems] ([CmsPageId]);', 0;
GO

-- CmsNotices
EXEC #EnsureIndex N'CmsNotices', N'IX_CmsNotices_Slug', N'Slug', N'CREATE UNIQUE INDEX [IX_CmsNotices_Slug] ON [dbo].[CmsNotices] ([Slug]);', 1;
GO

-- CmsPages
EXEC #EnsureIndex N'CmsPages', N'IX_CmsPages_Slug', N'Slug', N'CREATE UNIQUE INDEX [IX_CmsPages_Slug] ON [dbo].[CmsPages] ([Slug]);', 1;
GO

-- ControlledDocuments
EXEC #EnsureIndex N'ControlledDocuments', N'IX_ControlledDocuments_DocumentNumber', N'DocumentNumber', N'CREATE UNIQUE INDEX [IX_ControlledDocuments_DocumentNumber] ON [dbo].[ControlledDocuments] ([DocumentNumber]);', 1;
GO

-- DeathRecords
EXEC #EnsureIndex N'DeathRecords', N'IX_DeathRecords_PatientId', N'PatientId', N'CREATE INDEX [IX_DeathRecords_PatientId] ON [dbo].[DeathRecords] ([PatientId]);', 0;
GO

-- DischargeSummaries
EXEC #EnsureIndex N'DischargeSummaries', N'IX_DischargeSummaries_IPDAdmissionId', N'IPDAdmissionId', N'CREATE UNIQUE INDEX [IX_DischargeSummaries_IPDAdmissionId] ON [dbo].[DischargeSummaries] ([IPDAdmissionId]);', 1;
EXEC #EnsureIndex N'DischargeSummaries', N'IX_DischargeSummaries_PatientId_DischargeDate', N'PatientId,DischargeDate', N'CREATE INDEX [IX_DischargeSummaries_PatientId_DischargeDate] ON [dbo].[DischargeSummaries] ([PatientId], [DischargeDate]);', 0;
EXEC #EnsureIndex N'DischargeSummaries', N'IX_DischargeSummaries_HospitalId', N'HospitalId', N'CREATE INDEX [IX_DischargeSummaries_HospitalId] ON [dbo].[DischargeSummaries] ([HospitalId]);', 0;
GO

-- Doctors
EXEC #EnsureIndex N'Doctors', N'IX_Doctors_DepartmentId', N'DepartmentId', N'CREATE INDEX [IX_Doctors_DepartmentId] ON [dbo].[Doctors] ([DepartmentId]);', 0;
GO

-- DoctorShifts
EXEC #EnsureIndex N'DoctorShifts', N'IX_DoctorShifts_DoctorId', N'DoctorId', N'CREATE INDEX [IX_DoctorShifts_DoctorId] ON [dbo].[DoctorShifts] ([DoctorId]);', 0;
GO

-- DocumentVersions
EXEC #EnsureIndex N'DocumentVersions', N'IX_DocumentVersions_DocumentId_VersionNumber', N'DocumentId,VersionNumber', N'CREATE UNIQUE INDEX [IX_DocumentVersions_DocumentId_VersionNumber] ON [dbo].[DocumentVersions] ([DocumentId], [VersionNumber]);', 1;
GO

-- Equipment
EXEC #EnsureIndex N'Equipment', N'IX_Equipment_AssetTag', N'AssetTag', N'CREATE UNIQUE INDEX [IX_Equipment_AssetTag] ON [dbo].[Equipment] ([AssetTag]);', 1;
EXEC #EnsureIndex N'Equipment', N'IX_Equipment_HospitalId', N'HospitalId', N'CREATE INDEX [IX_Equipment_HospitalId] ON [dbo].[Equipment] ([HospitalId]);', 0;
GO

-- EquipmentServiceRecords
EXEC #EnsureIndex N'EquipmentServiceRecords', N'IX_EquipmentServiceRecords_EquipmentId', N'EquipmentId', N'CREATE INDEX [IX_EquipmentServiceRecords_EquipmentId] ON [dbo].[EquipmentServiceRecords] ([EquipmentId]);', 0;
GO

-- GeneratedReports
EXEC #EnsureIndex N'GeneratedReports', N'IX_GeneratedReports_CreatedDate_ReportType', N'CreatedDate,ReportType', N'CREATE INDEX [IX_GeneratedReports_CreatedDate_ReportType] ON [dbo].[GeneratedReports] ([CreatedDate], [ReportType]);', 0;
EXEC #EnsureIndex N'GeneratedReports', N'IX_GeneratedReports_GeneratedBy', N'GeneratedBy', N'CREATE INDEX [IX_GeneratedReports_GeneratedBy] ON [dbo].[GeneratedReports] ([GeneratedBy]);', 0;
GO

-- Hospitals
EXEC #EnsureIndex N'Hospitals', N'IX_Hospitals_Code', N'Code', N'CREATE UNIQUE INDEX [IX_Hospitals_Code] ON [dbo].[Hospitals] ([Code]);', 1;
GO

-- IdCardRecords
EXEC #EnsureIndex N'IdCardRecords', N'IX_IdCardRecords_CardNumber', N'CardNumber', N'CREATE UNIQUE INDEX [IX_IdCardRecords_CardNumber] ON [dbo].[IdCardRecords] ([CardNumber]);', 1;
EXEC #EnsureIndex N'IdCardRecords', N'IX_IdCardRecords_StaffId', N'StaffId', N'CREATE INDEX [IX_IdCardRecords_StaffId] ON [dbo].[IdCardRecords] ([StaffId]);', 0;
GO

-- InternalAudits
EXEC #EnsureIndex N'InternalAudits', N'IX_InternalAudits_AuditNumber', N'AuditNumber', N'CREATE UNIQUE INDEX [IX_InternalAudits_AuditNumber] ON [dbo].[InternalAudits] ([AuditNumber]);', 1;
EXEC #EnsureIndex N'InternalAudits', N'IX_InternalAudits_HospitalId', N'HospitalId', N'CREATE INDEX [IX_InternalAudits_HospitalId] ON [dbo].[InternalAudits] ([HospitalId]);', 0;
GO

-- InternalMessages
EXEC #EnsureIndex N'InternalMessages', N'IX_InternalMessages_SenderId', N'SenderId', N'CREATE INDEX [IX_InternalMessages_SenderId] ON [dbo].[InternalMessages] ([SenderId]);', 0;
EXEC #EnsureIndex N'InternalMessages', N'IX_InternalMessages_ParentMessageId', N'ParentMessageId', N'CREATE INDEX [IX_InternalMessages_ParentMessageId] ON [dbo].[InternalMessages] ([ParentMessageId]);', 0;
GO

-- InventoryItems
EXEC #EnsureIndex N'InventoryItems', N'IX_InventoryItems_HospitalId', N'HospitalId', N'CREATE INDEX [IX_InventoryItems_HospitalId] ON [dbo].[InventoryItems] ([HospitalId]);', 0;
EXEC #EnsureIndex N'InventoryItems', N'IX_InventoryItems_VendorId', N'VendorId', N'CREATE INDEX [IX_InventoryItems_VendorId] ON [dbo].[InventoryItems] ([VendorId]);', 0;
GO

-- InventoryTransactions
EXEC #EnsureIndex N'InventoryTransactions', N'IX_InventoryTransactions_InventoryItemId', N'InventoryItemId', N'CREATE INDEX [IX_InventoryTransactions_InventoryItemId] ON [dbo].[InventoryTransactions] ([InventoryItemId]);', 0;
GO

-- IPDAdmissions
EXEC #EnsureIndex N'IPDAdmissions', N'IX_IPDAdmissions_BedId', N'BedId', N'CREATE INDEX [IX_IPDAdmissions_BedId] ON [dbo].[IPDAdmissions] ([BedId]);', 0;
EXEC #EnsureIndex N'IPDAdmissions', N'IX_IPDAdmissions_DoctorId', N'DoctorId', N'CREATE INDEX [IX_IPDAdmissions_DoctorId] ON [dbo].[IPDAdmissions] ([DoctorId]);', 0;
EXEC #EnsureIndex N'IPDAdmissions', N'IX_IPDAdmissions_MedicalRecordId', N'MedicalRecordId', N'CREATE INDEX [IX_IPDAdmissions_MedicalRecordId] ON [dbo].[IPDAdmissions] ([MedicalRecordId]);', 0;
EXEC #EnsureIndex N'IPDAdmissions', N'IX_IPDAdmissions_PatientId', N'PatientId', N'CREATE INDEX [IX_IPDAdmissions_PatientId] ON [dbo].[IPDAdmissions] ([PatientId]);', 0;
EXEC #EnsureIndex N'IPDAdmissions', N'IX_IPDAdmissions_DateRange', N'AdmissionDate,DischargeDate,BedId', N'CREATE INDEX [IX_IPDAdmissions_DateRange] ON [dbo].[IPDAdmissions] ([AdmissionDate], [DischargeDate], [BedId]);', 0;
EXEC #EnsureIndex N'IPDAdmissions', N'IX_IPDAdmissions_HospitalId', N'HospitalId', N'CREATE INDEX [IX_IPDAdmissions_HospitalId] ON [dbo].[IPDAdmissions] ([HospitalId]);', 0;
GO

-- LabNoteHistories
EXEC #EnsureIndex N'LabNoteHistories', N'IX_LabNoteHistories_LabResultId_UpdatedAtUtc', N'LabResultId,UpdatedAtUtc', N'CREATE INDEX [IX_LabNoteHistories_LabResultId_UpdatedAtUtc] ON [dbo].[LabNoteHistories] ([LabResultId], [UpdatedAtUtc]);', 0;
GO

-- LabResults
EXEC #EnsureIndex N'LabResults', N'IX_LabResults_LabTestId', N'LabTestId', N'CREATE INDEX [IX_LabResults_LabTestId] ON [dbo].[LabResults] ([LabTestId]);', 0;
EXEC #EnsureIndex N'LabResults', N'IX_LabResults_PatientId', N'PatientId', N'CREATE INDEX [IX_LabResults_PatientId] ON [dbo].[LabResults] ([PatientId]);', 0;
EXEC #EnsureIndex N'LabResults', N'IX_LabResults_AccessionNumber', N'AccessionNumber', N'CREATE UNIQUE INDEX [IX_LabResults_AccessionNumber] ON [dbo].[LabResults] ([AccessionNumber]) WHERE ([AccessionNumber] IS NOT NULL);', 1, N'([AccessionNumber] IS NOT NULL)';
GO

-- LabSampleEvents
EXEC #EnsureIndex N'LabSampleEvents', N'IX_LabSampleEvents_LabResultId', N'LabResultId', N'CREATE INDEX [IX_LabSampleEvents_LabResultId] ON [dbo].[LabSampleEvents] ([LabResultId]);', 0;
GO

-- LeaveBalances
EXEC #EnsureIndex N'LeaveBalances', N'IX_LeaveBalances_LeaveTypeId', N'LeaveTypeId', N'CREATE INDEX [IX_LeaveBalances_LeaveTypeId] ON [dbo].[LeaveBalances] ([LeaveTypeId]);', 0;
EXEC #EnsureIndex N'LeaveBalances', N'IX_LeaveBalances_StaffId_LeaveTypeId_Year', N'StaffId,LeaveTypeId,Year', N'CREATE UNIQUE INDEX [IX_LeaveBalances_StaffId_LeaveTypeId_Year] ON [dbo].[LeaveBalances] ([StaffId], [LeaveTypeId], [Year]);', 1;
GO

-- LeaveRequests
EXEC #EnsureIndex N'LeaveRequests', N'IX_LeaveRequests_LeaveTypeId', N'LeaveTypeId', N'CREATE INDEX [IX_LeaveRequests_LeaveTypeId] ON [dbo].[LeaveRequests] ([LeaveTypeId]);', 0;
EXEC #EnsureIndex N'LeaveRequests', N'IX_LeaveRequests_StaffId', N'StaffId', N'CREATE INDEX [IX_LeaveRequests_StaffId] ON [dbo].[LeaveRequests] ([StaffId]);', 0;
EXEC #EnsureIndex N'LeaveRequests', N'IX_LeaveRequest_DateRange', N'StaffId,StartDate,EndDate', N'CREATE INDEX [IX_LeaveRequest_DateRange] ON [dbo].[LeaveRequests] ([StaffId], [StartDate], [EndDate]);', 0;
GO

-- LicenseAuditLogs
EXEC #EnsureIndex N'LicenseAuditLogs', N'IX_LicenseAuditLogs_LicenseRecordId_PerformedAtUtc', N'LicenseRecordId,PerformedAtUtc', N'CREATE INDEX [IX_LicenseAuditLogs_LicenseRecordId_PerformedAtUtc] ON [dbo].[LicenseAuditLogs] ([LicenseRecordId], [PerformedAtUtc]);', 0;
EXEC #EnsureIndex N'LicenseAuditLogs', N'IX_LicenseAuditLogs_PerformedByUserId', N'PerformedByUserId', N'CREATE INDEX [IX_LicenseAuditLogs_PerformedByUserId] ON [dbo].[LicenseAuditLogs] ([PerformedByUserId]);', 0;
GO

-- LicenseRecords
EXEC #EnsureIndex N'LicenseRecords', N'IX_LicenseRecords_IsActive_ExpiresAtUtc', N'IsActive,ExpiresAtUtc', N'CREATE INDEX [IX_LicenseRecords_IsActive_ExpiresAtUtc] ON [dbo].[LicenseRecords] ([IsActive], [ExpiresAtUtc]);', 0;
GO

-- LicenseReminderLogs
EXEC #EnsureIndex N'LicenseReminderLogs', N'IX_LicenseReminderLogs_LicenseRecordId_TargetExpiryUtc_TriggeredAtUtc', N'LicenseRecordId,TargetExpiryUtc,TriggeredAtUtc', N'CREATE INDEX [IX_LicenseReminderLogs_LicenseRecordId_TargetExpiryUtc_TriggeredAtUtc] ON [dbo].[LicenseReminderLogs] ([LicenseRecordId], [TargetExpiryUtc], [TriggeredAtUtc]);', 0;
GO

-- LiveConsultationSessions
EXEC #EnsureIndex N'LiveConsultationSessions', N'IX_LiveConsultationSessions_PatientId', N'PatientId', N'CREATE INDEX [IX_LiveConsultationSessions_PatientId] ON [dbo].[LiveConsultationSessions] ([PatientId]);', 0;
EXEC #EnsureIndex N'LiveConsultationSessions', N'IX_LiveConsultationSessions_BillId', N'BillId', N'CREATE INDEX [IX_LiveConsultationSessions_BillId] ON [dbo].[LiveConsultationSessions] ([BillId]);', 0;
EXEC #EnsureIndex N'LiveConsultationSessions', N'IX_LiveConsultationSessions_ScheduledAt', N'ScheduledAt', N'CREATE INDEX [IX_LiveConsultationSessions_ScheduledAt] ON [dbo].[LiveConsultationSessions] ([ScheduledAt]);', 0;
GO

-- MedicalRecords
EXEC #EnsureIndex N'MedicalRecords', N'IX_MedicalRecords_DoctorId', N'DoctorId', N'CREATE INDEX [IX_MedicalRecords_DoctorId] ON [dbo].[MedicalRecords] ([DoctorId]);', 0;
EXEC #EnsureIndex N'MedicalRecords', N'IX_MedicalRecords_PatientId', N'PatientId', N'CREATE INDEX [IX_MedicalRecords_PatientId] ON [dbo].[MedicalRecords] ([PatientId]);', 0;
EXEC #EnsureIndex N'MedicalRecords', N'IX_MedicalRecords_SourceType_SourceId', N'SourceType,SourceId', N'CREATE UNIQUE INDEX [IX_MedicalRecords_SourceType_SourceId] ON [dbo].[MedicalRecords] ([SourceType], [SourceId]) WHERE ([SourceId] IS NOT NULL);', 1, N'([SourceId] IS NOT NULL)';
GO

-- NotificationDeliveryLogs
EXEC #EnsureIndex N'NotificationDeliveryLogs', N'IX_NotificationDeliveryLogs_CreatedAt_Channel_Status', N'CreatedAt,Channel,Status', N'CREATE INDEX [IX_NotificationDeliveryLogs_CreatedAt_Channel_Status] ON [dbo].[NotificationDeliveryLogs] ([CreatedAt], [Channel], [Status]);', 0;
GO

-- OPDVisits
EXEC #EnsureIndex N'OPDVisits', N'IX_OPDVisits_DoctorId', N'DoctorId', N'CREATE INDEX [IX_OPDVisits_DoctorId] ON [dbo].[OPDVisits] ([DoctorId]);', 0;
EXEC #EnsureIndex N'OPDVisits', N'IX_OPDVisits_MedicalRecordId', N'MedicalRecordId', N'CREATE INDEX [IX_OPDVisits_MedicalRecordId] ON [dbo].[OPDVisits] ([MedicalRecordId]);', 0;
EXEC #EnsureIndex N'OPDVisits', N'IX_OPDVisits_PatientId', N'PatientId', N'CREATE INDEX [IX_OPDVisits_PatientId] ON [dbo].[OPDVisits] ([PatientId]);', 0;
EXEC #EnsureIndex N'OPDVisits', N'IX_OPDVisits_HospitalId', N'HospitalId', N'CREATE INDEX [IX_OPDVisits_HospitalId] ON [dbo].[OPDVisits] ([HospitalId]);', 0;
GO

-- OperationTheatres
EXEC #EnsureIndex N'OperationTheatres', N'IX_OperationTheatres_HospitalId', N'HospitalId', N'CREATE INDEX [IX_OperationTheatres_HospitalId] ON [dbo].[OperationTheatres] ([HospitalId]);', 0;
EXEC #EnsureIndex N'OperationTheatres', N'IX_OperationTheatres_HospitalId_Code', N'HospitalId,Code', N'CREATE UNIQUE INDEX [IX_OperationTheatres_HospitalId_Code] ON [dbo].[OperationTheatres] ([HospitalId], [Code]) WHERE ([HospitalId] IS NOT NULL);', 1, N'([HospitalId] IS NOT NULL)';
GO

-- OTBlocks
EXEC #EnsureIndex N'OTBlocks', N'IX_OTBlocks_OperationTheatreId_StartsAt', N'OperationTheatreId,StartsAt', N'CREATE INDEX [IX_OTBlocks_OperationTheatreId_StartsAt] ON [dbo].[OTBlocks] ([OperationTheatreId], [StartsAt]);', 0;
GO

-- OTSchedules
EXEC #EnsureIndex N'OTSchedules', N'IX_OTSchedules_BillId', N'BillId', N'CREATE INDEX [IX_OTSchedules_BillId] ON [dbo].[OTSchedules] ([BillId]);', 0;
EXEC #EnsureIndex N'OTSchedules', N'IX_OTSchedules_PatientId', N'PatientId', N'CREATE INDEX [IX_OTSchedules_PatientId] ON [dbo].[OTSchedules] ([PatientId]);', 0;
EXEC #EnsureIndex N'OTSchedules', N'IX_OTSchedules_HospitalId', N'HospitalId', N'CREATE INDEX [IX_OTSchedules_HospitalId] ON [dbo].[OTSchedules] ([HospitalId]);', 0;
EXEC #EnsureIndex N'OTSchedules', N'IX_OTSchedules_OperationTheatreId_ScheduledDate', N'OperationTheatreId,ScheduledDate', N'CREATE INDEX [IX_OTSchedules_OperationTheatreId_ScheduledDate] ON [dbo].[OTSchedules] ([OperationTheatreId], [ScheduledDate]);', 0;
EXEC #EnsureIndex N'OTSchedules', N'IX_OTSchedules_SurgeonDoctorId', N'SurgeonDoctorId', N'CREATE INDEX [IX_OTSchedules_SurgeonDoctorId] ON [dbo].[OTSchedules] ([SurgeonDoctorId]);', 0;
GO

-- PatientDocuments
EXEC #EnsureIndex N'PatientDocuments', N'IX_PatientDocuments_PatientId_IsDeleted', N'PatientId,IsDeleted', N'CREATE INDEX [IX_PatientDocuments_PatientId_IsDeleted] ON [dbo].[PatientDocuments] ([PatientId], [IsDeleted]);', 0;
GO

-- PatientInsurances
EXEC #EnsureIndex N'PatientInsurances', N'IX_PatientInsurances_PatientId_IsActive_ValidTo', N'PatientId,IsActive,ValidTo', N'CREATE INDEX [IX_PatientInsurances_PatientId_IsActive_ValidTo] ON [dbo].[PatientInsurances] ([PatientId], [IsActive], [ValidTo]);', 0;
GO

-- Patients
EXEC #EnsureIndex N'Patients', N'IX_Patients_UserId', N'UserId', N'CREATE INDEX [IX_Patients_UserId] ON [dbo].[Patients] ([UserId]);', 0;
GO

-- Payments
EXEC #EnsureIndex N'Payments', N'IX_Payments_BillId', N'BillId', N'CREATE INDEX [IX_Payments_BillId] ON [dbo].[Payments] ([BillId]);', 0;
EXEC #EnsureIndex N'Payments', N'IX_Payments_DateRange', N'PaymentDate', N'CREATE INDEX [IX_Payments_DateRange] ON [dbo].[Payments] ([PaymentDate]);', 0;
GO

-- PayrollRecords
EXEC #EnsureIndex N'PayrollRecords', N'IX_PayrollRecords_StaffId_PayrollMonth', N'StaffId,PayrollMonth', N'CREATE UNIQUE INDEX [IX_PayrollRecords_StaffId_PayrollMonth] ON [dbo].[PayrollRecords] ([StaffId], [PayrollMonth]);', 1;
GO

-- PharmacyBills
EXEC #EnsureIndex N'PharmacyBills', N'IX_PharmacyBills_PatientId', N'PatientId', N'CREATE INDEX [IX_PharmacyBills_PatientId] ON [dbo].[PharmacyBills] ([PatientId]);', 0;
EXEC #EnsureIndex N'PharmacyBills', N'IX_PharmacyBills_HospitalId', N'HospitalId', N'CREATE INDEX [IX_PharmacyBills_HospitalId] ON [dbo].[PharmacyBills] ([HospitalId]);', 0;
GO

-- Prescriptions
EXEC #EnsureIndex N'Prescriptions', N'IX_Prescriptions_MedicineId', N'MedicineId', N'CREATE INDEX [IX_Prescriptions_MedicineId] ON [dbo].[Prescriptions] ([MedicineId]);', 0;
EXEC #EnsureIndex N'Prescriptions', N'IX_Prescriptions_PharmacyBillId', N'PharmacyBillId', N'CREATE INDEX [IX_Prescriptions_PharmacyBillId] ON [dbo].[Prescriptions] ([PharmacyBillId]);', 0;
GO

-- Printers
EXEC #EnsureIndex N'Printers', N'IX_Printers_HospitalId', N'HospitalId', N'CREATE INDEX [IX_Printers_HospitalId] ON [dbo].[Printers] ([HospitalId]);', 0;
GO

-- PublicAppointmentRequests
EXEC #EnsureIndex N'PublicAppointmentRequests', N'IX_PublicAppointmentRequests_DoctorId', N'DoctorId', N'CREATE INDEX [IX_PublicAppointmentRequests_DoctorId] ON [dbo].[PublicAppointmentRequests] ([DoctorId]);', 0;
EXEC #EnsureIndex N'PublicAppointmentRequests', N'IX_PublicAppointmentRequests_PatientId', N'PatientId', N'CREATE INDEX [IX_PublicAppointmentRequests_PatientId] ON [dbo].[PublicAppointmentRequests] ([PatientId]);', 0;
GO

-- PurchaseBillItems
EXEC #EnsureIndex N'PurchaseBillItems', N'IX_PurchaseBillItems_InventoryItemId', N'InventoryItemId', N'CREATE INDEX [IX_PurchaseBillItems_InventoryItemId] ON [dbo].[PurchaseBillItems] ([InventoryItemId]);', 0;
EXEC #EnsureIndex N'PurchaseBillItems', N'IX_PurchaseBillItems_PurchaseBillId', N'PurchaseBillId', N'CREATE INDEX [IX_PurchaseBillItems_PurchaseBillId] ON [dbo].[PurchaseBillItems] ([PurchaseBillId]);', 0;
GO

-- PurchaseBills
EXEC #EnsureIndex N'PurchaseBills', N'IX_PurchaseBills_BillNumber', N'BillNumber', N'CREATE UNIQUE INDEX [IX_PurchaseBills_BillNumber] ON [dbo].[PurchaseBills] ([BillNumber]);', 1;
EXEC #EnsureIndex N'PurchaseBills', N'IX_PurchaseBills_HospitalId', N'HospitalId', N'CREATE INDEX [IX_PurchaseBills_HospitalId] ON [dbo].[PurchaseBills] ([HospitalId]);', 0;
EXEC #EnsureIndex N'PurchaseBills', N'IX_PurchaseBills_VendorId_VendorInvoiceNumber', N'VendorId,VendorInvoiceNumber', N'CREATE INDEX [IX_PurchaseBills_VendorId_VendorInvoiceNumber] ON [dbo].[PurchaseBills] ([VendorId], [VendorInvoiceNumber]);', 0;
GO

-- QualityIncidents
EXEC #EnsureIndex N'QualityIncidents', N'IX_QualityIncidents_IncidentNumber', N'IncidentNumber', N'CREATE UNIQUE INDEX [IX_QualityIncidents_IncidentNumber] ON [dbo].[QualityIncidents] ([IncidentNumber]);', 1;
EXEC #EnsureIndex N'QualityIncidents', N'IX_QualityIncidents_HospitalId', N'HospitalId', N'CREATE INDEX [IX_QualityIncidents_HospitalId] ON [dbo].[QualityIncidents] ([HospitalId]);', 0;
EXEC #EnsureIndex N'QualityIncidents', N'IX_QualityIncidents_PatientId', N'PatientId', N'CREATE INDEX [IX_QualityIncidents_PatientId] ON [dbo].[QualityIncidents] ([PatientId]);', 0;
GO

-- RadiologyResults
EXEC #EnsureIndex N'RadiologyResults', N'IX_RadiologyResults_PatientId', N'PatientId', N'CREATE INDEX [IX_RadiologyResults_PatientId] ON [dbo].[RadiologyResults] ([PatientId]);', 0;
EXEC #EnsureIndex N'RadiologyResults', N'IX_RadiologyResults_RadiologyTestId', N'RadiologyTestId', N'CREATE INDEX [IX_RadiologyResults_RadiologyTestId] ON [dbo].[RadiologyResults] ([RadiologyTestId]);', 0;
GO

-- Referrals
EXEC #EnsureIndex N'Referrals', N'IX_Referrals_BillId', N'BillId', N'CREATE INDEX [IX_Referrals_BillId] ON [dbo].[Referrals] ([BillId]);', 0;
EXEC #EnsureIndex N'Referrals', N'IX_Referrals_PatientId', N'PatientId', N'CREATE INDEX [IX_Referrals_PatientId] ON [dbo].[Referrals] ([PatientId]);', 0;
GO

-- ReportCharts
EXEC #EnsureIndex N'ReportCharts', N'IX_ReportCharts_TemplateId', N'TemplateId', N'CREATE INDEX [IX_ReportCharts_TemplateId] ON [dbo].[ReportCharts] ([TemplateId]);', 0;
GO

-- ReportDesigns
EXEC #EnsureIndex N'ReportDesigns', N'IX_ReportDesigns_TemplateId', N'TemplateId', N'CREATE UNIQUE INDEX [IX_ReportDesigns_TemplateId] ON [dbo].[ReportDesigns] ([TemplateId]);', 1;
GO

-- ReportFields
EXEC #EnsureIndex N'ReportFields', N'IX_ReportFields_TemplateId', N'TemplateId', N'CREATE INDEX [IX_ReportFields_TemplateId] ON [dbo].[ReportFields] ([TemplateId]);', 0;
GO

-- ReportFilters
EXEC #EnsureIndex N'ReportFilters', N'IX_ReportFilters_TemplateId', N'TemplateId', N'CREATE INDEX [IX_ReportFilters_TemplateId] ON [dbo].[ReportFilters] ([TemplateId]);', 0;
GO

-- ReportSchedules
EXEC #EnsureIndex N'ReportSchedules', N'IX_ReportSchedules_CreatedBy', N'CreatedBy', N'CREATE INDEX [IX_ReportSchedules_CreatedBy] ON [dbo].[ReportSchedules] ([CreatedBy]);', 0;
EXEC #EnsureIndex N'ReportSchedules', N'IX_ReportSchedules_IsActive_NextRunDate', N'IsActive,NextRunDate', N'CREATE INDEX [IX_ReportSchedules_IsActive_NextRunDate] ON [dbo].[ReportSchedules] ([IsActive], [NextRunDate]);', 0;
GO

-- RoleFeatures
EXEC #EnsureIndex N'RoleFeatures', N'IX_RoleFeatures_FeatureId', N'FeatureId', N'CREATE INDEX [IX_RoleFeatures_FeatureId] ON [dbo].[RoleFeatures] ([FeatureId]);', 0;
GO

-- SavedReports
EXEC #EnsureIndex N'SavedReports', N'IX_SavedReports_TemplateId', N'TemplateId', N'CREATE INDEX [IX_SavedReports_TemplateId] ON [dbo].[SavedReports] ([TemplateId]);', 0;
GO

-- Staff
EXEC #EnsureIndex N'Staff', N'IX_Staff_UserId', N'UserId', N'CREATE INDEX [IX_Staff_UserId] ON [dbo].[Staff] ([UserId]);', 0;
GO

-- StaffAttendances
EXEC #EnsureIndex N'StaffAttendances', N'IX_StaffAttendances_StaffId_AttendanceDate', N'StaffId,AttendanceDate', N'CREATE UNIQUE INDEX [IX_StaffAttendances_StaffId_AttendanceDate] ON [dbo].[StaffAttendances] ([StaffId], [AttendanceDate]);', 1;
EXEC #EnsureIndex N'StaffAttendances', N'IX_StaffAttendance_DateRange', N'StaffId,AttendanceDate', N'CREATE INDEX [IX_StaffAttendance_DateRange] ON [dbo].[StaffAttendances] ([StaffId], [AttendanceDate]);', 0;
GO

-- StaffRoles
EXEC #EnsureIndex N'StaffRoles', N'IX_StaffRoles_RoleId', N'RoleId', N'CREATE INDEX [IX_StaffRoles_RoleId] ON [dbo].[StaffRoles] ([RoleId]);', 0;
GO

-- SystemModules
EXEC #EnsureIndex N'SystemModules', N'UX_SystemModules_Key', N'Key', N'CREATE UNIQUE INDEX [UX_SystemModules_Key] ON [dbo].[SystemModules] ([Key]);', 1;
GO

-- SystemNotifications
EXEC #EnsureIndex N'SystemNotifications', N'IX_SystemNotifications_UserId_IsRead_CreatedAtUtc', N'UserId,IsRead,CreatedAtUtc', N'CREATE INDEX [IX_SystemNotifications_UserId_IsRead_CreatedAtUtc] ON [dbo].[SystemNotifications] ([UserId], [IsRead], [CreatedAtUtc]);', 0;
EXEC #EnsureIndex N'SystemNotifications', N'IX_SystemNotifications_PatientId', N'PatientId', N'CREATE INDEX [IX_SystemNotifications_PatientId] ON [dbo].[SystemNotifications] ([PatientId]);', 0;
GO

-- TestResults
EXEC #EnsureIndex N'TestResults', N'IX_TestResults_DoctorId', N'DoctorId', N'CREATE INDEX [IX_TestResults_DoctorId] ON [dbo].[TestResults] ([DoctorId]);', 0;
EXEC #EnsureIndex N'TestResults', N'IX_TestResults_MedicalRecordId', N'MedicalRecordId', N'CREATE INDEX [IX_TestResults_MedicalRecordId] ON [dbo].[TestResults] ([MedicalRecordId]);', 0;
EXEC #EnsureIndex N'TestResults', N'IX_TestResults_PatientId', N'PatientId', N'CREATE INDEX [IX_TestResults_PatientId] ON [dbo].[TestResults] ([PatientId]);', 0;
GO

-- TpaClaims
EXEC #EnsureIndex N'TpaClaims', N'IX_TpaClaims_TpaProviderId', N'TpaProviderId', N'CREATE INDEX [IX_TpaClaims_TpaProviderId] ON [dbo].[TpaClaims] ([TpaProviderId]);', 0;
EXEC #EnsureIndex N'TpaClaims', N'IX_TpaClaims_PatientId', N'PatientId', N'CREATE INDEX [IX_TpaClaims_PatientId] ON [dbo].[TpaClaims] ([PatientId]);', 0;
EXEC #EnsureIndex N'TpaClaims', N'IX_TpaClaims_BillId', N'BillId', N'CREATE INDEX [IX_TpaClaims_BillId] ON [dbo].[TpaClaims] ([BillId]);', 0;
GO

-- TrainingRecords
EXEC #EnsureIndex N'TrainingRecords', N'IX_TrainingRecords_StaffId', N'StaffId', N'CREATE INDEX [IX_TrainingRecords_StaffId] ON [dbo].[TrainingRecords] ([StaffId]);', 0;
GO

-- UserActionLogs
EXEC #EnsureIndex N'UserActionLogs', N'IX_UserActionLogs_StaffId', N'StaffId', N'CREATE INDEX [IX_UserActionLogs_StaffId] ON [dbo].[UserActionLogs] ([StaffId]);', 0;
GO

-- UserHospitalAccesses
EXEC #EnsureIndex N'UserHospitalAccesses', N'IX_UserHospitalAccesses_UserId_HospitalId', N'UserId,HospitalId', N'CREATE UNIQUE INDEX [IX_UserHospitalAccesses_UserId_HospitalId] ON [dbo].[UserHospitalAccesses] ([UserId], [HospitalId]);', 1;
EXEC #EnsureIndex N'UserHospitalAccesses', N'IX_UserHospitalAccesses_HospitalId', N'HospitalId', N'CREATE INDEX [IX_UserHospitalAccesses_HospitalId] ON [dbo].[UserHospitalAccesses] ([HospitalId]);', 0;
GO

-- UserModuleAccesses
EXEC #EnsureIndex N'UserModuleAccesses', N'UX_UserModuleAccesses_UserId_ModuleId', N'UserId,ModuleId', N'CREATE UNIQUE INDEX [UX_UserModuleAccesses_UserId_ModuleId] ON [dbo].[UserModuleAccesses] ([UserId], [ModuleId]);', 1;
EXEC #EnsureIndex N'UserModuleAccesses', N'IX_UserModuleAccesses_ModuleId', N'ModuleId', N'CREATE INDEX [IX_UserModuleAccesses_ModuleId] ON [dbo].[UserModuleAccesses] ([ModuleId]);', 0;
GO

-- UserSessions
EXEC #EnsureIndex N'UserSessions', N'UX_UserSessions_SessionId', N'SessionId', N'CREATE UNIQUE INDEX [UX_UserSessions_SessionId] ON [dbo].[UserSessions] ([SessionId]);', 1;
EXEC #EnsureIndex N'UserSessions', N'IX_UserSessions_IsActive_LastActivityUtc_ActiveRole', N'IsActive,LastActivityUtc,ActiveRole', N'CREATE INDEX [IX_UserSessions_IsActive_LastActivityUtc_ActiveRole] ON [dbo].[UserSessions] ([IsActive], [LastActivityUtc], [ActiveRole]);', 0;
EXEC #EnsureIndex N'UserSessions', N'IX_UserSessions_UserId_IsActive_LastActivityUtc', N'UserId,IsActive,LastActivityUtc', N'CREATE INDEX [IX_UserSessions_UserId_IsActive_LastActivityUtc] ON [dbo].[UserSessions] ([UserId], [IsActive], [LastActivityUtc]);', 0;
GO

-- UserThemePreferences
EXEC #EnsureIndex N'UserThemePreferences', N'UX_UserThemePreferences_UserId', N'UserId', N'CREATE UNIQUE INDEX [UX_UserThemePreferences_UserId] ON [dbo].[UserThemePreferences] ([UserId]);', 1;
GO

-- VendorPayments
EXEC #EnsureIndex N'VendorPayments', N'IX_VendorPayments_PurchaseBillId', N'PurchaseBillId', N'CREATE INDEX [IX_VendorPayments_PurchaseBillId] ON [dbo].[VendorPayments] ([PurchaseBillId]);', 0;
GO

-- Vendors
EXEC #EnsureIndex N'Vendors', N'IX_Vendors_VendorCode', N'VendorCode', N'CREATE UNIQUE INDEX [IX_Vendors_VendorCode] ON [dbo].[Vendors] ([VendorCode]);', 1;
GO

-- VisitNoteHistories
EXEC #EnsureIndex N'VisitNoteHistories', N'IX_VisitNoteHistories_OPDVisitId_UpdatedAtUtc', N'OPDVisitId,UpdatedAtUtc', N'CREATE INDEX [IX_VisitNoteHistories_OPDVisitId_UpdatedAtUtc] ON [dbo].[VisitNoteHistories] ([OPDVisitId], [UpdatedAtUtc]);', 0;
GO

-- Wards
EXEC #EnsureIndex N'Wards', N'IX_Wards_HospitalId', N'HospitalId', N'CREATE INDEX [IX_Wards_HospitalId] ON [dbo].[Wards] ([HospitalId]);', 0;
GO

/* ============================================================
   4. Foreign keys
   ============================================================ */

-- AccountApprovalRequests
EXEC #EnsureForeignKey N'AccountApprovalRequests', N'FK_AccountApprovalRequests_AspNetUsers_RequestedUserId', N'RequestedUserId', N'AspNetUsers', N'Id', N'FOREIGN KEY ([RequestedUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE';
GO

-- AmbulanceDispatches
EXEC #EnsureForeignKey N'AmbulanceDispatches', N'FK_AmbulanceDispatches_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id])';
EXEC #EnsureForeignKey N'AmbulanceDispatches', N'FK_AmbulanceDispatches_AmbulanceVehicles_AmbulanceVehicleId', N'AmbulanceVehicleId', N'AmbulanceVehicles', N'Id', N'FOREIGN KEY ([AmbulanceVehicleId]) REFERENCES [dbo].[AmbulanceVehicles] ([Id]) ON DELETE CASCADE';
GO

-- Appointments
EXEC #EnsureForeignKey N'Appointments', N'FK_Appointments_Hospitals_HospitalId', N'HospitalId', N'Hospitals', N'Id', N'FOREIGN KEY ([HospitalId]) REFERENCES [dbo].[Hospitals] ([Id])';
EXEC #EnsureForeignKey N'Appointments', N'FK_Appointments_Doctors_DoctorId', N'DoctorId', N'Doctors', N'Id', N'FOREIGN KEY ([DoctorId]) REFERENCES [dbo].[Doctors] ([Id]) ON DELETE CASCADE';
EXEC #EnsureForeignKey N'Appointments', N'FK_Appointments_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id]) ON DELETE CASCADE';
GO

-- AspNetRoleClaims
EXEC #EnsureForeignKey N'AspNetRoleClaims', N'FK_AspNetRoleClaims_AspNetRoles_RoleId', N'RoleId', N'AspNetRoles', N'Id', N'FOREIGN KEY ([RoleId]) REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE CASCADE';
GO

-- AspNetUserClaims
EXEC #EnsureForeignKey N'AspNetUserClaims', N'FK_AspNetUserClaims_AspNetUsers_UserId', N'UserId', N'AspNetUsers', N'Id', N'FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE';
GO

-- AspNetUserLogins
EXEC #EnsureForeignKey N'AspNetUserLogins', N'FK_AspNetUserLogins_AspNetUsers_UserId', N'UserId', N'AspNetUsers', N'Id', N'FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE';
GO

-- AspNetUserRoles
EXEC #EnsureForeignKey N'AspNetUserRoles', N'FK_AspNetUserRoles_AspNetUsers_UserId', N'UserId', N'AspNetUsers', N'Id', N'FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE';
EXEC #EnsureForeignKey N'AspNetUserRoles', N'FK_AspNetUserRoles_AspNetRoles_RoleId', N'RoleId', N'AspNetRoles', N'Id', N'FOREIGN KEY ([RoleId]) REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE CASCADE';
GO

-- AspNetUserTokens
EXEC #EnsureForeignKey N'AspNetUserTokens', N'FK_AspNetUserTokens_AspNetUsers_UserId', N'UserId', N'AspNetUsers', N'Id', N'FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE';
GO

-- AuditFindings
EXEC #EnsureForeignKey N'AuditFindings', N'FK_AuditFindings_InternalAudits_AuditId', N'AuditId', N'InternalAudits', N'Id', N'FOREIGN KEY ([AuditId]) REFERENCES [dbo].[InternalAudits] ([Id]) ON DELETE CASCADE';
GO

-- AuditLogs
EXEC #EnsureForeignKey N'AuditLogs', N'FK_AuditLogs_AspNetUsers_UserId', N'UserId', N'AspNetUsers', N'Id', N'FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE';
GO

-- Beds
EXEC #EnsureForeignKey N'Beds', N'FK_Beds_Wards_WardId', N'WardId', N'Wards', N'Id', N'FOREIGN KEY ([WardId]) REFERENCES [dbo].[Wards] ([Id]) ON DELETE CASCADE';
EXEC #EnsureForeignKey N'Beds', N'FK_Beds_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id]) ON DELETE SET NULL';
GO

-- BillItems
EXEC #EnsureForeignKey N'BillItems', N'FK_BillItems_Bills_BillId', N'BillId', N'Bills', N'Id', N'FOREIGN KEY ([BillId]) REFERENCES [dbo].[Bills] ([Id]) ON DELETE CASCADE';
GO

-- Bills
EXEC #EnsureForeignKey N'Bills', N'FK_Bills_Hospitals_HospitalId', N'HospitalId', N'Hospitals', N'Id', N'FOREIGN KEY ([HospitalId]) REFERENCES [dbo].[Hospitals] ([Id])';
EXEC #EnsureForeignKey N'Bills', N'FK_Bills_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id]) ON DELETE CASCADE';
GO

-- BirthRecords
EXEC #EnsureForeignKey N'BirthRecords', N'FK_BirthRecords_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id])';
GO

-- BloodIssues
EXEC #EnsureForeignKey N'BloodIssues', N'FK_BloodIssues_Bills_BillId', N'BillId', N'Bills', N'Id', N'FOREIGN KEY ([BillId]) REFERENCES [dbo].[Bills] ([Id])';
EXEC #EnsureForeignKey N'BloodIssues', N'FK_BloodIssues_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id])';
GO

-- CapaActions
EXEC #EnsureForeignKey N'CapaActions', N'FK_CapaActions_AuditFindings_AuditFindingId', N'AuditFindingId', N'AuditFindings', N'Id', N'FOREIGN KEY ([AuditFindingId]) REFERENCES [dbo].[AuditFindings] ([Id])';
EXEC #EnsureForeignKey N'CapaActions', N'FK_CapaActions_Hospitals_HospitalId', N'HospitalId', N'Hospitals', N'Id', N'FOREIGN KEY ([HospitalId]) REFERENCES [dbo].[Hospitals] ([Id])';
EXEC #EnsureForeignKey N'CapaActions', N'FK_CapaActions_QualityIncidents_IncidentId', N'IncidentId', N'QualityIncidents', N'Id', N'FOREIGN KEY ([IncidentId]) REFERENCES [dbo].[QualityIncidents] ([Id])';
GO

-- CertificateRecords
EXEC #EnsureForeignKey N'CertificateRecords', N'FK_CertificateRecords_Staff_StaffId', N'StaffId', N'Staff', N'Id', N'FOREIGN KEY ([StaffId]) REFERENCES [dbo].[Staff] ([Id])';
GO

-- ChatEscalations
EXEC #EnsureForeignKey N'ChatEscalations', N'FK_ChatEscalations_ChatMessages_MessageId', N'MessageId', N'ChatMessages', N'Id', N'FOREIGN KEY ([MessageId]) REFERENCES [dbo].[ChatMessages] ([Id])';
EXEC #EnsureForeignKey N'ChatEscalations', N'FK_ChatEscalations_ChatSessions_SessionId', N'SessionId', N'ChatSessions', N'Id', N'FOREIGN KEY ([SessionId]) REFERENCES [dbo].[ChatSessions] ([Id]) ON DELETE CASCADE';
GO

-- ChatFeedback
EXEC #EnsureForeignKey N'ChatFeedback', N'FK_ChatFeedback_ChatMessages_MessageId', N'MessageId', N'ChatMessages', N'Id', N'FOREIGN KEY ([MessageId]) REFERENCES [dbo].[ChatMessages] ([Id])';
EXEC #EnsureForeignKey N'ChatFeedback', N'FK_ChatFeedback_ChatSessions_SessionId', N'SessionId', N'ChatSessions', N'Id', N'FOREIGN KEY ([SessionId]) REFERENCES [dbo].[ChatSessions] ([Id]) ON DELETE CASCADE';
GO

-- ChatMessages
EXEC #EnsureForeignKey N'ChatMessages', N'FK_ChatMessages_ChatSessions_SessionId', N'SessionId', N'ChatSessions', N'Id', N'FOREIGN KEY ([SessionId]) REFERENCES [dbo].[ChatSessions] ([Id]) ON DELETE CASCADE';
GO

-- CmsMenuItems
EXEC #EnsureForeignKey N'CmsMenuItems', N'FK_CmsMenuItems_CmsPages_CmsPageId', N'CmsPageId', N'CmsPages', N'Id', N'FOREIGN KEY ([CmsPageId]) REFERENCES [dbo].[CmsPages] ([Id]) ON DELETE SET NULL';
GO

-- DeathRecords
EXEC #EnsureForeignKey N'DeathRecords', N'FK_DeathRecords_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id])';
GO

-- DischargeSummaries
EXEC #EnsureForeignKey N'DischargeSummaries', N'FK_DischargeSummaries_Hospitals_HospitalId', N'HospitalId', N'Hospitals', N'Id', N'FOREIGN KEY ([HospitalId]) REFERENCES [dbo].[Hospitals] ([Id])';
EXEC #EnsureForeignKey N'DischargeSummaries', N'FK_DischargeSummaries_IPDAdmissions_IPDAdmissionId', N'IPDAdmissionId', N'IPDAdmissions', N'Id', N'FOREIGN KEY ([IPDAdmissionId]) REFERENCES [dbo].[IPDAdmissions] ([Id])';
EXEC #EnsureForeignKey N'DischargeSummaries', N'FK_DischargeSummaries_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id])';
GO

-- Doctors
EXEC #EnsureForeignKey N'Doctors', N'FK_Doctors_Departments_DepartmentId', N'DepartmentId', N'Departments', N'Id', N'FOREIGN KEY ([DepartmentId]) REFERENCES [dbo].[Departments] ([Id]) ON DELETE CASCADE';
GO

-- DoctorShifts
EXEC #EnsureForeignKey N'DoctorShifts', N'FK_DoctorShifts_Doctors_DoctorId', N'DoctorId', N'Doctors', N'Id', N'FOREIGN KEY ([DoctorId]) REFERENCES [dbo].[Doctors] ([Id]) ON DELETE CASCADE';
GO

-- DocumentVersions
EXEC #EnsureForeignKey N'DocumentVersions', N'FK_DocumentVersions_ControlledDocuments_DocumentId', N'DocumentId', N'ControlledDocuments', N'Id', N'FOREIGN KEY ([DocumentId]) REFERENCES [dbo].[ControlledDocuments] ([Id]) ON DELETE CASCADE';
GO

-- Equipment
EXEC #EnsureForeignKey N'Equipment', N'FK_Equipment_Hospitals_HospitalId', N'HospitalId', N'Hospitals', N'Id', N'FOREIGN KEY ([HospitalId]) REFERENCES [dbo].[Hospitals] ([Id])';
GO

-- EquipmentServiceRecords
EXEC #EnsureForeignKey N'EquipmentServiceRecords', N'FK_EquipmentServiceRecords_Equipment_EquipmentId', N'EquipmentId', N'Equipment', N'Id', N'FOREIGN KEY ([EquipmentId]) REFERENCES [dbo].[Equipment] ([Id]) ON DELETE CASCADE';
GO

-- GeneratedReports
EXEC #EnsureForeignKey N'GeneratedReports', N'FK_GeneratedReports_Staff_GeneratedBy', N'GeneratedBy', N'Staff', N'Id', N'FOREIGN KEY ([GeneratedBy]) REFERENCES [dbo].[Staff] ([Id])';
GO

-- IdCardRecords
EXEC #EnsureForeignKey N'IdCardRecords', N'FK_IdCardRecords_Staff_StaffId', N'StaffId', N'Staff', N'Id', N'FOREIGN KEY ([StaffId]) REFERENCES [dbo].[Staff] ([Id])';
GO

-- InternalAudits
EXEC #EnsureForeignKey N'InternalAudits', N'FK_InternalAudits_Hospitals_HospitalId', N'HospitalId', N'Hospitals', N'Id', N'FOREIGN KEY ([HospitalId]) REFERENCES [dbo].[Hospitals] ([Id])';
GO

-- InternalMessages
EXEC #EnsureForeignKey N'InternalMessages', N'FK_InternalMessages_AspNetUsers_SenderId', N'SenderId', N'AspNetUsers', N'Id', N'FOREIGN KEY ([SenderId]) REFERENCES [dbo].[AspNetUsers] ([Id])';
EXEC #EnsureForeignKey N'InternalMessages', N'FK_InternalMessages_InternalMessages_ParentMessageId', N'ParentMessageId', N'InternalMessages', N'Id', N'FOREIGN KEY ([ParentMessageId]) REFERENCES [dbo].[InternalMessages] ([Id])';
GO

-- InventoryItems
EXEC #EnsureForeignKey N'InventoryItems', N'FK_InventoryItems_Vendors_VendorId', N'VendorId', N'Vendors', N'Id', N'FOREIGN KEY ([VendorId]) REFERENCES [dbo].[Vendors] ([Id])';
EXEC #EnsureForeignKey N'InventoryItems', N'FK_InventoryItems_Hospitals_HospitalId', N'HospitalId', N'Hospitals', N'Id', N'FOREIGN KEY ([HospitalId]) REFERENCES [dbo].[Hospitals] ([Id])';
GO

-- InventoryTransactions
EXEC #EnsureForeignKey N'InventoryTransactions', N'FK_InventoryTransactions_InventoryItems_InventoryItemId', N'InventoryItemId', N'InventoryItems', N'Id', N'FOREIGN KEY ([InventoryItemId]) REFERENCES [dbo].[InventoryItems] ([Id]) ON DELETE CASCADE';
GO

-- IPDAdmissions
EXEC #EnsureForeignKey N'IPDAdmissions', N'FK_IPDAdmissions_Hospitals_HospitalId', N'HospitalId', N'Hospitals', N'Id', N'FOREIGN KEY ([HospitalId]) REFERENCES [dbo].[Hospitals] ([Id])';
EXEC #EnsureForeignKey N'IPDAdmissions', N'FK_IPDAdmissions_MedicalRecords_MedicalRecordId', N'MedicalRecordId', N'MedicalRecords', N'Id', N'FOREIGN KEY ([MedicalRecordId]) REFERENCES [dbo].[MedicalRecords] ([Id])';
EXEC #EnsureForeignKey N'IPDAdmissions', N'FK_IPDAdmissions_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id])';
EXEC #EnsureForeignKey N'IPDAdmissions', N'FK_IPDAdmissions_Beds_BedId', N'BedId', N'Beds', N'Id', N'FOREIGN KEY ([BedId]) REFERENCES [dbo].[Beds] ([Id])';
EXEC #EnsureForeignKey N'IPDAdmissions', N'FK_IPDAdmissions_Doctors_DoctorId', N'DoctorId', N'Doctors', N'Id', N'FOREIGN KEY ([DoctorId]) REFERENCES [dbo].[Doctors] ([Id]) ON DELETE CASCADE';
GO

-- LabNoteHistories
EXEC #EnsureForeignKey N'LabNoteHistories', N'FK_LabNoteHistories_LabResults_LabResultId', N'LabResultId', N'LabResults', N'Id', N'FOREIGN KEY ([LabResultId]) REFERENCES [dbo].[LabResults] ([Id]) ON DELETE CASCADE';
GO

-- LabResults
EXEC #EnsureForeignKey N'LabResults', N'FK_LabResults_LabTests_LabTestId', N'LabTestId', N'LabTests', N'Id', N'FOREIGN KEY ([LabTestId]) REFERENCES [dbo].[LabTests] ([Id]) ON DELETE CASCADE';
EXEC #EnsureForeignKey N'LabResults', N'FK_LabResults_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id]) ON DELETE CASCADE';
GO

-- LabSampleEvents
EXEC #EnsureForeignKey N'LabSampleEvents', N'FK_LabSampleEvents_LabResults_LabResultId', N'LabResultId', N'LabResults', N'Id', N'FOREIGN KEY ([LabResultId]) REFERENCES [dbo].[LabResults] ([Id]) ON DELETE CASCADE';
GO

-- LeaveBalances
EXEC #EnsureForeignKey N'LeaveBalances', N'FK_LeaveBalances_LeaveTypes_LeaveTypeId', N'LeaveTypeId', N'LeaveTypes', N'Id', N'FOREIGN KEY ([LeaveTypeId]) REFERENCES [dbo].[LeaveTypes] ([Id])';
EXEC #EnsureForeignKey N'LeaveBalances', N'FK_LeaveBalances_Staff_StaffId', N'StaffId', N'Staff', N'Id', N'FOREIGN KEY ([StaffId]) REFERENCES [dbo].[Staff] ([Id])';
GO

-- LeaveRequests
EXEC #EnsureForeignKey N'LeaveRequests', N'FK_LeaveRequests_LeaveTypes_LeaveTypeId', N'LeaveTypeId', N'LeaveTypes', N'Id', N'FOREIGN KEY ([LeaveTypeId]) REFERENCES [dbo].[LeaveTypes] ([Id])';
EXEC #EnsureForeignKey N'LeaveRequests', N'FK_LeaveRequests_Staff_StaffId', N'StaffId', N'Staff', N'Id', N'FOREIGN KEY ([StaffId]) REFERENCES [dbo].[Staff] ([Id])';
GO

-- LicenseAuditLogs
EXEC #EnsureForeignKey N'LicenseAuditLogs', N'FK_LicenseAuditLogs_LicenseRecords_LicenseRecordId', N'LicenseRecordId', N'LicenseRecords', N'Id', N'FOREIGN KEY ([LicenseRecordId]) REFERENCES [dbo].[LicenseRecords] ([Id]) ON DELETE CASCADE';
EXEC #EnsureForeignKey N'LicenseAuditLogs', N'FK_LicenseAuditLogs_AspNetUsers_PerformedByUserId', N'PerformedByUserId', N'AspNetUsers', N'Id', N'FOREIGN KEY ([PerformedByUserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE SET NULL';
GO

-- LicenseReminderLogs
EXEC #EnsureForeignKey N'LicenseReminderLogs', N'FK_LicenseReminderLogs_LicenseRecords_LicenseRecordId', N'LicenseRecordId', N'LicenseRecords', N'Id', N'FOREIGN KEY ([LicenseRecordId]) REFERENCES [dbo].[LicenseRecords] ([Id]) ON DELETE CASCADE';
GO

-- LiveConsultationSessions
EXEC #EnsureForeignKey N'LiveConsultationSessions', N'FK_LiveConsultationSessions_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id])';
EXEC #EnsureForeignKey N'LiveConsultationSessions', N'FK_LiveConsultationSessions_Bills_BillId', N'BillId', N'Bills', N'Id', N'FOREIGN KEY ([BillId]) REFERENCES [dbo].[Bills] ([Id])';
GO

-- MedicalRecords
EXEC #EnsureForeignKey N'MedicalRecords', N'FK_MedicalRecords_AspNetUsers_DoctorId', N'DoctorId', N'AspNetUsers', N'Id', N'FOREIGN KEY ([DoctorId]) REFERENCES [dbo].[AspNetUsers] ([Id])';
EXEC #EnsureForeignKey N'MedicalRecords', N'FK_MedicalRecords_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id])';
GO

-- OPDVisits
EXEC #EnsureForeignKey N'OPDVisits', N'FK_OPDVisits_Hospitals_HospitalId', N'HospitalId', N'Hospitals', N'Id', N'FOREIGN KEY ([HospitalId]) REFERENCES [dbo].[Hospitals] ([Id])';
EXEC #EnsureForeignKey N'OPDVisits', N'FK_OPDVisits_MedicalRecords_MedicalRecordId', N'MedicalRecordId', N'MedicalRecords', N'Id', N'FOREIGN KEY ([MedicalRecordId]) REFERENCES [dbo].[MedicalRecords] ([Id])';
EXEC #EnsureForeignKey N'OPDVisits', N'FK_OPDVisits_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id]) ON DELETE CASCADE';
EXEC #EnsureForeignKey N'OPDVisits', N'FK_OPDVisits_Doctors_DoctorId', N'DoctorId', N'Doctors', N'Id', N'FOREIGN KEY ([DoctorId]) REFERENCES [dbo].[Doctors] ([Id]) ON DELETE CASCADE';
GO

-- OperationTheatres
EXEC #EnsureForeignKey N'OperationTheatres', N'FK_OperationTheatres_Hospitals_HospitalId', N'HospitalId', N'Hospitals', N'Id', N'FOREIGN KEY ([HospitalId]) REFERENCES [dbo].[Hospitals] ([Id])';
GO

-- OTBlocks
EXEC #EnsureForeignKey N'OTBlocks', N'FK_OTBlocks_OperationTheatres_OperationTheatreId', N'OperationTheatreId', N'OperationTheatres', N'Id', N'FOREIGN KEY ([OperationTheatreId]) REFERENCES [dbo].[OperationTheatres] ([Id]) ON DELETE CASCADE';
GO

-- OTSchedules
EXEC #EnsureForeignKey N'OTSchedules', N'FK_OTSchedules_Hospitals_HospitalId', N'HospitalId', N'Hospitals', N'Id', N'FOREIGN KEY ([HospitalId]) REFERENCES [dbo].[Hospitals] ([Id])';
EXEC #EnsureForeignKey N'OTSchedules', N'FK_OTSchedules_Bills_BillId', N'BillId', N'Bills', N'Id', N'FOREIGN KEY ([BillId]) REFERENCES [dbo].[Bills] ([Id])';
EXEC #EnsureForeignKey N'OTSchedules', N'FK_OTSchedules_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id])';
EXEC #EnsureForeignKey N'OTSchedules', N'FK_OTSchedules_OperationTheatres_OperationTheatreId', N'OperationTheatreId', N'OperationTheatres', N'Id', N'FOREIGN KEY ([OperationTheatreId]) REFERENCES [dbo].[OperationTheatres] ([Id])';
EXEC #EnsureForeignKey N'OTSchedules', N'FK_OTSchedules_Doctors_SurgeonDoctorId', N'SurgeonDoctorId', N'Doctors', N'Id', N'FOREIGN KEY ([SurgeonDoctorId]) REFERENCES [dbo].[Doctors] ([Id])';
GO

-- PatientDocuments
EXEC #EnsureForeignKey N'PatientDocuments', N'FK_PatientDocuments_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id])';
GO

-- PatientInsurances
EXEC #EnsureForeignKey N'PatientInsurances', N'FK_PatientInsurances_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id]) ON DELETE CASCADE';
GO

-- Patients
EXEC #EnsureForeignKey N'Patients', N'FK_Patients_AspNetUsers_UserId', N'UserId', N'AspNetUsers', N'Id', N'FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE';
GO

-- Payments
EXEC #EnsureForeignKey N'Payments', N'FK_Payments_Bills_BillId', N'BillId', N'Bills', N'Id', N'FOREIGN KEY ([BillId]) REFERENCES [dbo].[Bills] ([Id]) ON DELETE CASCADE';
GO

-- PayrollRecords
EXEC #EnsureForeignKey N'PayrollRecords', N'FK_PayrollRecords_Staff_StaffId', N'StaffId', N'Staff', N'Id', N'FOREIGN KEY ([StaffId]) REFERENCES [dbo].[Staff] ([Id])';
GO

-- PharmacyBills
EXEC #EnsureForeignKey N'PharmacyBills', N'FK_PharmacyBills_Hospitals_HospitalId', N'HospitalId', N'Hospitals', N'Id', N'FOREIGN KEY ([HospitalId]) REFERENCES [dbo].[Hospitals] ([Id])';
EXEC #EnsureForeignKey N'PharmacyBills', N'FK_PharmacyBills_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id]) ON DELETE CASCADE';
GO

-- Prescriptions
EXEC #EnsureForeignKey N'Prescriptions', N'FK_Prescriptions_Medicines_MedicineId', N'MedicineId', N'Medicines', N'Id', N'FOREIGN KEY ([MedicineId]) REFERENCES [dbo].[Medicines] ([Id]) ON DELETE CASCADE';
EXEC #EnsureForeignKey N'Prescriptions', N'FK_Prescriptions_PharmacyBills_PharmacyBillId', N'PharmacyBillId', N'PharmacyBills', N'Id', N'FOREIGN KEY ([PharmacyBillId]) REFERENCES [dbo].[PharmacyBills] ([Id]) ON DELETE CASCADE';
GO

-- Printers
EXEC #EnsureForeignKey N'Printers', N'FK_Printers_Hospitals_HospitalId', N'HospitalId', N'Hospitals', N'Id', N'FOREIGN KEY ([HospitalId]) REFERENCES [dbo].[Hospitals] ([Id])';
GO

-- PublicAppointmentRequests
EXEC #EnsureForeignKey N'PublicAppointmentRequests', N'FK_PublicAppointmentRequests_Doctors_DoctorId', N'DoctorId', N'Doctors', N'Id', N'FOREIGN KEY ([DoctorId]) REFERENCES [dbo].[Doctors] ([Id])';
EXEC #EnsureForeignKey N'PublicAppointmentRequests', N'FK_PublicAppointmentRequests_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id])';
GO

-- PurchaseBillItems
EXEC #EnsureForeignKey N'PurchaseBillItems', N'FK_PurchaseBillItems_InventoryItems_InventoryItemId', N'InventoryItemId', N'InventoryItems', N'Id', N'FOREIGN KEY ([InventoryItemId]) REFERENCES [dbo].[InventoryItems] ([Id])';
EXEC #EnsureForeignKey N'PurchaseBillItems', N'FK_PurchaseBillItems_PurchaseBills_PurchaseBillId', N'PurchaseBillId', N'PurchaseBills', N'Id', N'FOREIGN KEY ([PurchaseBillId]) REFERENCES [dbo].[PurchaseBills] ([Id]) ON DELETE CASCADE';
GO

-- PurchaseBills
EXEC #EnsureForeignKey N'PurchaseBills', N'FK_PurchaseBills_Hospitals_HospitalId', N'HospitalId', N'Hospitals', N'Id', N'FOREIGN KEY ([HospitalId]) REFERENCES [dbo].[Hospitals] ([Id])';
EXEC #EnsureForeignKey N'PurchaseBills', N'FK_PurchaseBills_Vendors_VendorId', N'VendorId', N'Vendors', N'Id', N'FOREIGN KEY ([VendorId]) REFERENCES [dbo].[Vendors] ([Id])';
GO

-- QualityIncidents
EXEC #EnsureForeignKey N'QualityIncidents', N'FK_QualityIncidents_Hospitals_HospitalId', N'HospitalId', N'Hospitals', N'Id', N'FOREIGN KEY ([HospitalId]) REFERENCES [dbo].[Hospitals] ([Id])';
EXEC #EnsureForeignKey N'QualityIncidents', N'FK_QualityIncidents_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id])';
GO

-- RadiologyResults
EXEC #EnsureForeignKey N'RadiologyResults', N'FK_RadiologyResults_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id]) ON DELETE CASCADE';
EXEC #EnsureForeignKey N'RadiologyResults', N'FK_RadiologyResults_RadiologyTests_RadiologyTestId', N'RadiologyTestId', N'RadiologyTests', N'Id', N'FOREIGN KEY ([RadiologyTestId]) REFERENCES [dbo].[RadiologyTests] ([Id]) ON DELETE CASCADE';
GO

-- Referrals
EXEC #EnsureForeignKey N'Referrals', N'FK_Referrals_Bills_BillId', N'BillId', N'Bills', N'Id', N'FOREIGN KEY ([BillId]) REFERENCES [dbo].[Bills] ([Id])';
EXEC #EnsureForeignKey N'Referrals', N'FK_Referrals_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id])';
GO

-- ReportCharts
EXEC #EnsureForeignKey N'ReportCharts', N'FK_ReportCharts_ReportTemplates_TemplateId', N'TemplateId', N'ReportTemplates', N'Id', N'FOREIGN KEY ([TemplateId]) REFERENCES [dbo].[ReportTemplates] ([Id]) ON DELETE CASCADE';
GO

-- ReportDesigns
EXEC #EnsureForeignKey N'ReportDesigns', N'FK_ReportDesigns_ReportTemplates_TemplateId', N'TemplateId', N'ReportTemplates', N'Id', N'FOREIGN KEY ([TemplateId]) REFERENCES [dbo].[ReportTemplates] ([Id]) ON DELETE CASCADE';
GO

-- ReportFields
EXEC #EnsureForeignKey N'ReportFields', N'FK_ReportFields_ReportTemplates_TemplateId', N'TemplateId', N'ReportTemplates', N'Id', N'FOREIGN KEY ([TemplateId]) REFERENCES [dbo].[ReportTemplates] ([Id]) ON DELETE CASCADE';
GO

-- ReportFilters
EXEC #EnsureForeignKey N'ReportFilters', N'FK_ReportFilters_ReportTemplates_TemplateId', N'TemplateId', N'ReportTemplates', N'Id', N'FOREIGN KEY ([TemplateId]) REFERENCES [dbo].[ReportTemplates] ([Id]) ON DELETE CASCADE';
GO

-- ReportSchedules
EXEC #EnsureForeignKey N'ReportSchedules', N'FK_ReportSchedules_Staff_CreatedBy', N'CreatedBy', N'Staff', N'Id', N'FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[Staff] ([Id])';
GO

-- RoleFeatures
EXEC #EnsureForeignKey N'RoleFeatures', N'FK_RoleFeatures_Features_FeatureId', N'FeatureId', N'Features', N'Id', N'FOREIGN KEY ([FeatureId]) REFERENCES [dbo].[Features] ([Id]) ON DELETE CASCADE';
EXEC #EnsureForeignKey N'RoleFeatures', N'FK_RoleFeatures_Roles_RoleId', N'RoleId', N'Roles', N'Id', N'FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([Id]) ON DELETE CASCADE';
GO

-- SavedReports
EXEC #EnsureForeignKey N'SavedReports', N'FK_SavedReports_ReportTemplates_TemplateId', N'TemplateId', N'ReportTemplates', N'Id', N'FOREIGN KEY ([TemplateId]) REFERENCES [dbo].[ReportTemplates] ([Id]) ON DELETE CASCADE';
GO

-- Staff
EXEC #EnsureForeignKey N'Staff', N'FK_Staff_AspNetUsers_UserId', N'UserId', N'AspNetUsers', N'Id', N'FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id])';
GO

-- StaffAttendances
EXEC #EnsureForeignKey N'StaffAttendances', N'FK_StaffAttendances_Staff_StaffId', N'StaffId', N'Staff', N'Id', N'FOREIGN KEY ([StaffId]) REFERENCES [dbo].[Staff] ([Id])';
GO

-- StaffRoles
EXEC #EnsureForeignKey N'StaffRoles', N'FK_StaffRoles_Roles_RoleId', N'RoleId', N'Roles', N'Id', N'FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([Id]) ON DELETE CASCADE';
EXEC #EnsureForeignKey N'StaffRoles', N'FK_StaffRoles_Staff_StaffId', N'StaffId', N'Staff', N'Id', N'FOREIGN KEY ([StaffId]) REFERENCES [dbo].[Staff] ([Id]) ON DELETE CASCADE';
GO

-- SystemNotifications
EXEC #EnsureForeignKey N'SystemNotifications', N'FK_SystemNotifications_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id])';
EXEC #EnsureForeignKey N'SystemNotifications', N'FK_SystemNotifications_AspNetUsers_UserId', N'UserId', N'AspNetUsers', N'Id', N'FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE';
GO

-- TestResults
EXEC #EnsureForeignKey N'TestResults', N'FK_TestResults_MedicalRecords_MedicalRecordId', N'MedicalRecordId', N'MedicalRecords', N'Id', N'FOREIGN KEY ([MedicalRecordId]) REFERENCES [dbo].[MedicalRecords] ([Id])';
EXEC #EnsureForeignKey N'TestResults', N'FK_TestResults_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id])';
EXEC #EnsureForeignKey N'TestResults', N'FK_TestResults_AspNetUsers_DoctorId', N'DoctorId', N'AspNetUsers', N'Id', N'FOREIGN KEY ([DoctorId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE';
GO

-- TpaClaims
EXEC #EnsureForeignKey N'TpaClaims', N'FK_TpaClaims_Bills_BillId', N'BillId', N'Bills', N'Id', N'FOREIGN KEY ([BillId]) REFERENCES [dbo].[Bills] ([Id])';
EXEC #EnsureForeignKey N'TpaClaims', N'FK_TpaClaims_TpaProviders_TpaProviderId', N'TpaProviderId', N'TpaProviders', N'Id', N'FOREIGN KEY ([TpaProviderId]) REFERENCES [dbo].[TpaProviders] ([Id]) ON DELETE CASCADE';
EXEC #EnsureForeignKey N'TpaClaims', N'FK_TpaClaims_Patients_PatientId', N'PatientId', N'Patients', N'Id', N'FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Patients] ([Id]) ON DELETE CASCADE';
GO

-- UserActionLogs
EXEC #EnsureForeignKey N'UserActionLogs', N'FK_UserActionLogs_Staff_StaffId', N'StaffId', N'Staff', N'Id', N'FOREIGN KEY ([StaffId]) REFERENCES [dbo].[Staff] ([Id]) ON DELETE CASCADE';
GO

-- UserHospitalAccesses
EXEC #EnsureForeignKey N'UserHospitalAccesses', N'FK_UserHospitalAccesses_Hospitals_HospitalId', N'HospitalId', N'Hospitals', N'Id', N'FOREIGN KEY ([HospitalId]) REFERENCES [dbo].[Hospitals] ([Id]) ON DELETE CASCADE';
EXEC #EnsureForeignKey N'UserHospitalAccesses', N'FK_UserHospitalAccesses_AspNetUsers_UserId', N'UserId', N'AspNetUsers', N'Id', N'FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE';
GO

-- UserModuleAccesses
EXEC #EnsureForeignKey N'UserModuleAccesses', N'FK_UserModuleAccesses_AspNetUsers_UserId', N'UserId', N'AspNetUsers', N'Id', N'FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE';
EXEC #EnsureForeignKey N'UserModuleAccesses', N'FK_UserModuleAccesses_SystemModules_ModuleId', N'ModuleId', N'SystemModules', N'Id', N'FOREIGN KEY ([ModuleId]) REFERENCES [dbo].[SystemModules] ([Id]) ON DELETE CASCADE';
GO

-- UserSessions
EXEC #EnsureForeignKey N'UserSessions', N'FK_UserSessions_AspNetUsers_UserId', N'UserId', N'AspNetUsers', N'Id', N'FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE';
GO

-- UserThemePreferences
EXEC #EnsureForeignKey N'UserThemePreferences', N'FK_UserThemePreferences_AspNetUsers_UserId', N'UserId', N'AspNetUsers', N'Id', N'FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE';
GO

-- VendorPayments
EXEC #EnsureForeignKey N'VendorPayments', N'FK_VendorPayments_PurchaseBills_PurchaseBillId', N'PurchaseBillId', N'PurchaseBills', N'Id', N'FOREIGN KEY ([PurchaseBillId]) REFERENCES [dbo].[PurchaseBills] ([Id]) ON DELETE CASCADE';
GO

-- VisitNoteHistories
EXEC #EnsureForeignKey N'VisitNoteHistories', N'FK_VisitNoteHistories_OPDVisits_OPDVisitId', N'OPDVisitId', N'OPDVisits', N'Id', N'FOREIGN KEY ([OPDVisitId]) REFERENCES [dbo].[OPDVisits] ([Id]) ON DELETE CASCADE';
GO

-- Wards
EXEC #EnsureForeignKey N'Wards', N'FK_Wards_Hospitals_HospitalId', N'HospitalId', N'Hospitals', N'Id', N'FOREIGN KEY ([HospitalId]) REFERENCES [dbo].[Hospitals] ([Id])';
GO

/* ============================================================
   6. Views and procedures (report procedures: StoredProcedures_Reports.sql)
   ============================================================ */
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_GetOperationalCounts]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT N'Patients' AS [Entity], COUNT_BIG(1) AS [RecordCount] FROM [dbo].[Patients]
    UNION ALL
    SELECT N'Doctors', COUNT_BIG(1) FROM [dbo].[Doctors]
    UNION ALL
    SELECT N'Appointments', COUNT_BIG(1) FROM [dbo].[Appointments]
    UNION ALL
    SELECT N'Bills', COUNT_BIG(1) FROM [dbo].[Bills]
    UNION ALL
    SELECT N'Staff', COUNT_BIG(1) FROM [dbo].[Staff];
END
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_GetUserRoleSummary]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.[Id] AS [UserId],
        u.[Email],
        u.[FirstName],
        u.[LastName],
        ar.[Name] AS [IdentityRole],
        r.[Name] AS [ApplicationRole],
        u.[IsActive]
    FROM [dbo].[AspNetUsers] u
    LEFT JOIN [dbo].[AspNetUserRoles] aur ON aur.[UserId] = u.[Id]
    LEFT JOIN [dbo].[AspNetRoles] ar ON ar.[Id] = aur.[RoleId]
    LEFT JOIN [dbo].[Staff] s ON s.[Id] = u.[Id]
    LEFT JOIN [dbo].[StaffRoles] sr ON sr.[StaffId] = s.[Id]
    LEFT JOIN [dbo].[Roles] r ON r.[Id] = sr.[RoleId]
    ORDER BY u.[Email];
END
GO

CREATE OR ALTER VIEW [dbo].[vw_ActiveDoctorShifts]
AS
SELECT
    d.[Id] AS [DoctorId],
    d.[FirstName],
    d.[LastName],
    d.[Specialization],
    ds.[DayOfWeek],
    ds.[StartTime],
    ds.[EndTime],
    ds.[SlotDurationMinutes],
    ds.[MaxPatientsPerSlot]
FROM [dbo].[Doctors] d
INNER JOIN [dbo].[DoctorShifts] ds ON ds.[DoctorId] = d.[Id]
WHERE d.[IsActive] = 1
  AND ds.[IsActive] = 1;
GO

CREATE OR ALTER VIEW [dbo].[vw_StaffWithRoles]
AS
SELECT
    s.[Id] AS [StaffId],
    s.[EmployeeId],
    s.[FirstName],
    s.[LastName],
    s.[Department],
    s.[Designation],
    r.[Name] AS [RoleName],
    s.[Email],
    s.[IsActive]
FROM [dbo].[Staff] s
LEFT JOIN [dbo].[StaffRoles] sr ON sr.[StaffId] = s.[Id]
LEFT JOIN [dbo].[Roles] r ON r.[Id] = sr.[RoleId];
GO

DROP PROCEDURE #EnsureForeignKey;
DROP PROCEDURE #EnsureIndex;
DROP PROCEDURE #EnsureColumn;
DROP PROCEDURE #ExecWithKeyTypes;
GO
PRINT N'MedyxHMS database script completed.';
GO
