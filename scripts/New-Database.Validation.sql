/*
    MedyxHMS New Database Bootstrap Script
    - Base schema generated from ApplicationDbContext
    - Baseline seed data (users, roles, features/modules, settings)
    - Utility views and stored procedures
*/

IF DB_ID(N'MedyxHMS_Validation_20260422') IS NULL
BEGIN
    CREATE DATABASE [MedyxHMS_Validation_20260422];
END
GO

USE [MedyxHMS_Validation_20260422];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(128) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(128) NOT NULL,
    [EmployeeId] nvarchar(max) NOT NULL,
    [FirstName] nvarchar(max) NOT NULL,
    [LastName] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [FirstLoginDate] datetime2 NULL,
    [LastLoginDate] datetime2 NULL,
    [ProfileImage] nvarchar(max) NULL,
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
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [BloodInventories] (
    [Id] int NOT NULL IDENTITY,
    [BloodGroup] nvarchar(max) NOT NULL,
    [UnitsAvailable] int NOT NULL,
    [UnitsReserved] int NOT NULL,
    [MinimumLevel] int NOT NULL,
    [LastUpdatedDate] datetime2 NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_BloodInventories] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ChatbotEventLogs] (
    [Id] bigint NOT NULL IDENTITY,
    [SessionId] nvarchar(64) NULL,
    [MessageId] bigint NULL,
    [EventType] nvarchar(50) NOT NULL,
    [Severity] nvarchar(20) NOT NULL,
    [Details] nvarchar(2000) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_ChatbotEventLogs] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ChatSessions] (
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
    CONSTRAINT [PK_ChatSessions] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [CmsNotices] (
    [Id] int NOT NULL IDENTITY,
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
    CONSTRAINT [PK_CmsNotices] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [CmsPages] (
    [Id] int NOT NULL IDENTITY,
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
    CONSTRAINT [PK_CmsPages] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ComplaintRecords] (
    [Id] int NOT NULL IDENTITY,
    [ComplainantName] nvarchar(max) NOT NULL,
    [Phone] nvarchar(max) NOT NULL,
    [Subject] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [ResolutionNotes] nvarchar(max) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ResolvedDate] datetime2 NULL,
    CONSTRAINT [PK_ComplaintRecords] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Departments] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [HeadOfDepartment] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Departments] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [DispatchReceiveRecords] (
    [Id] int NOT NULL IDENTITY,
    [RecordType] nvarchar(max) NOT NULL,
    [ReferenceNumber] nvarchar(max) NOT NULL,
    [PartyName] nvarchar(max) NOT NULL,
    [ContactNumber] nvarchar(max) NOT NULL,
    [ContentSummary] nvarchar(max) NOT NULL,
    [RecordDate] datetime2 NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [Notes] nvarchar(max) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_DispatchReceiveRecords] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Features] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Module] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Features] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [LabTests] (
    [Id] int NOT NULL IDENTITY,
    [TestName] nvarchar(max) NOT NULL,
    [TestCode] nvarchar(max) NOT NULL,
    [Category] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [NormalRange] nvarchar(max) NOT NULL,
    [Unit] nvarchar(max) NOT NULL,
    [PreparationTimeHours] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_LabTests] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Languages] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(max) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [NativeName] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [IsDefault] bit NOT NULL,
    [DisplayOrder] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Languages] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [LeaveTypes] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [DefaultDaysPerYear] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_LeaveTypes] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [LicenseRecords] (
    [Id] int NOT NULL IDENTITY,
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
    CONSTRAINT [PK_LicenseRecords] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Medicines] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [GenericName] nvarchar(max) NOT NULL,
    [Category] nvarchar(max) NOT NULL,
    [DosageForm] nvarchar(max) NOT NULL,
    [Strength] nvarchar(max) NOT NULL,
    [Manufacturer] nvarchar(max) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [StockQuantity] int NOT NULL,
    [MinStockLevel] int NOT NULL,
    [ExpiryDate] datetime2 NOT NULL,
    [BatchNumber] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Medicines] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [NotificationDeliveryLogs] (
    [Id] int NOT NULL IDENTITY,
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
    CONSTRAINT [PK_NotificationDeliveryLogs] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [RadiologyTests] (
    [Id] int NOT NULL IDENTITY,
    [TestName] nvarchar(max) NOT NULL,
    [TestCode] nvarchar(max) NOT NULL,
    [Category] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [PreparationTimeHours] int NOT NULL,
    [SpecialInstructions] nvarchar(max) NOT NULL,
    [RequiresContrast] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_RadiologyTests] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Roles] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Settings] (
    [Id] int NOT NULL IDENTITY,
    [Key] nvarchar(max) NOT NULL,
    [Value] nvarchar(max) NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [Category] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [IsSystem] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Settings] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Transactions] (
    [Id] int NOT NULL IDENTITY,
    [TransactionId] nvarchar(max) NOT NULL,
    [TransactionType] nvarchar(max) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [ReferenceNumber] nvarchar(max) NOT NULL,
    [TransactionDate] datetime2 NOT NULL,
    [ProcessedBy] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Transactions] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [VisitorLogs] (
    [Id] int NOT NULL IDENTITY,
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
    CONSTRAINT [PK_VisitorLogs] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Wards] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [TotalBeds] int NOT NULL,
    [OccupiedBeds] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Wards] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(128) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(128) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(128) NOT NULL,
    [ProviderKey] nvarchar(128) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(128) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(128) NOT NULL,
    [RoleId] nvarchar(128) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(128) NOT NULL,
    [LoginProvider] nvarchar(128) NOT NULL,
    [Name] nvarchar(128) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AuditLogs] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(128) NOT NULL,
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
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AuditLogs_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Patients] (
    [Id] int NOT NULL IDENTITY,
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
    [UserId] nvarchar(128) NOT NULL,
    [ProfileImagePath] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [LastVisitDate] datetime2 NULL,
    CONSTRAINT [PK_Patients] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Patients_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Staff] (
    [Id] nvarchar(128) NOT NULL,
    [EmployeeId] nvarchar(max) NOT NULL,
    [FirstName] nvarchar(max) NOT NULL,
    [LastName] nvarchar(max) NOT NULL,
    [Department] nvarchar(max) NOT NULL,
    [Designation] nvarchar(max) NOT NULL,
    [DateOfJoining] datetime2 NOT NULL,
    [Salary] decimal(18,2) NOT NULL,
    [Phone] nvarchar(max) NOT NULL,
    [Address] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [About] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UserId] nvarchar(128) NULL,
    CONSTRAINT [PK_Staff] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Staff_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id])
);
GO


CREATE TABLE [ChatMessages] (
    [Id] bigint NOT NULL IDENTITY,
    [SessionId] nvarchar(64) NOT NULL,
    [SenderType] nvarchar(20) NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ModerationStatus] nvarchar(30) NOT NULL,
    [TokenCount] int NOT NULL,
    [Category] nvarchar(30) NOT NULL,
    CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ChatMessages_ChatSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [ChatSessions] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [CmsMenuItems] (
    [Id] int NOT NULL IDENTITY,
    [Label] nvarchar(100) NOT NULL,
    [Url] nvarchar(300) NULL,
    [CmsPageId] int NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [OpenInNewTab] bit NOT NULL,
    CONSTRAINT [PK_CmsMenuItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CmsMenuItems_CmsPages_CmsPageId] FOREIGN KEY ([CmsPageId]) REFERENCES [CmsPages] ([Id]) ON DELETE SET NULL
);
GO


CREATE TABLE [Doctors] (
    [Id] int NOT NULL IDENTITY,
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
    CONSTRAINT [PK_Doctors] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Doctors_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [LicenseAuditLogs] (
    [Id] int NOT NULL IDENTITY,
    [LicenseRecordId] int NOT NULL,
    [ActionType] nvarchar(50) NOT NULL,
    [PerformedByUserId] nvarchar(128) NULL,
    [PerformedAtUtc] datetime2 NOT NULL,
    [OldExpiresAtUtc] datetime2 NULL,
    [NewExpiresAtUtc] datetime2 NULL,
    [RenewalTermYears] int NULL,
    [Details] nvarchar(2000) NULL,
    [IpAddress] nvarchar(64) NULL,
    CONSTRAINT [PK_LicenseAuditLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LicenseAuditLogs_AspNetUsers_PerformedByUserId] FOREIGN KEY ([PerformedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_LicenseAuditLogs_LicenseRecords_LicenseRecordId] FOREIGN KEY ([LicenseRecordId]) REFERENCES [LicenseRecords] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [LicenseReminderLogs] (
    [Id] int NOT NULL IDENTITY,
    [LicenseRecordId] int NOT NULL,
    [ReminderType] nvarchar(50) NOT NULL,
    [TargetExpiryUtc] datetime2 NOT NULL,
    [TriggeredAtUtc] datetime2 NOT NULL,
    [SentToCount] int NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [ErrorMessage] nvarchar(2000) NULL,
    CONSTRAINT [PK_LicenseReminderLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LicenseReminderLogs_LicenseRecords_LicenseRecordId] FOREIGN KEY ([LicenseRecordId]) REFERENCES [LicenseRecords] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [RoleFeatures] (
    [RoleId] int NOT NULL,
    [FeatureId] int NOT NULL,
    [CanView] bit NOT NULL,
    [CanAdd] bit NOT NULL,
    [CanEdit] bit NOT NULL,
    [CanDelete] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_RoleFeatures] PRIMARY KEY ([RoleId], [FeatureId]),
    CONSTRAINT [FK_RoleFeatures_Features_FeatureId] FOREIGN KEY ([FeatureId]) REFERENCES [Features] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RoleFeatures_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Beds] (
    [Id] int NOT NULL IDENTITY,
    [WardId] int NOT NULL,
    [BedNumber] nvarchar(max) NOT NULL,
    [BedType] nvarchar(max) NOT NULL,
    [DailyCharges] decimal(18,2) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Beds] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Beds_Wards_WardId] FOREIGN KEY ([WardId]) REFERENCES [Wards] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Bills] (
    [Id] int NOT NULL IDENTITY,
    [BillNumber] nvarchar(max) NOT NULL,
    [PatientId] int NOT NULL,
    [AppointmentId] int NULL,
    [BillDate] datetime2 NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [PaidAmount] decimal(18,2) NOT NULL,
    [PendingAmount] decimal(18,2) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [BillType] nvarchar(max) NOT NULL,
    [Notes] nvarchar(max) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Bills] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Bills_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [LabResults] (
    [Id] int NOT NULL IDENTITY,
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
    CONSTRAINT [PK_LabResults] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LabResults_LabTests_LabTestId] FOREIGN KEY ([LabTestId]) REFERENCES [LabTests] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LabResults_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [MedicalRecords] (
    [Id] int NOT NULL IDENTITY,
    [PatientId] int NOT NULL,
    [RecordType] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [Diagnosis] nvarchar(1000) NOT NULL,
    [Treatment] nvarchar(2000) NOT NULL,
    [Notes] nvarchar(1000) NOT NULL,
    [DoctorName] nvarchar(100) NOT NULL,
    [DoctorId] nvarchar(128) NOT NULL,
    [RecordDate] datetime2 NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_MedicalRecords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MedicalRecords_AspNetUsers_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_MedicalRecords_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [PharmacyBills] (
    [Id] int NOT NULL IDENTITY,
    [BillNumber] nvarchar(max) NOT NULL,
    [PatientId] int NOT NULL,
    [BillDate] datetime2 NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [PaidAmount] decimal(18,2) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [PaymentMethod] nvarchar(max) NOT NULL,
    [Notes] nvarchar(max) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_PharmacyBills] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PharmacyBills_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [RadiologyResults] (
    [Id] int NOT NULL IDENTITY,
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
    CONSTRAINT [PK_RadiologyResults] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RadiologyResults_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RadiologyResults_RadiologyTests_RadiologyTestId] FOREIGN KEY ([RadiologyTestId]) REFERENCES [RadiologyTests] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [CertificateRecords] (
    [Id] int NOT NULL IDENTITY,
    [StaffId] nvarchar(128) NOT NULL,
    [CertificateType] nvarchar(max) NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [IssueDate] datetime2 NOT NULL,
    [GeneratedBy] nvarchar(max) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_CertificateRecords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CertificateRecords_Staff_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [Staff] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [GeneratedReports] (
    [Id] int NOT NULL IDENTITY,
    [ReportName] nvarchar(max) NOT NULL,
    [ReportType] nvarchar(128) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [FromDate] datetime2 NOT NULL,
    [ToDate] datetime2 NOT NULL,
    [DepartmentId] int NULL,
    [FilePath] nvarchar(max) NOT NULL,
    [FileFormat] nvarchar(max) NOT NULL,
    [FileSize] bigint NOT NULL,
    [GeneratedBy] nvarchar(128) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_GeneratedReports] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GeneratedReports_Staff_GeneratedBy] FOREIGN KEY ([GeneratedBy]) REFERENCES [Staff] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [IdCardRecords] (
    [Id] int NOT NULL IDENTITY,
    [StaffId] nvarchar(128) NOT NULL,
    [CardNumber] nvarchar(128) NOT NULL,
    [IssueDate] datetime2 NOT NULL,
    [ExpiryDate] datetime2 NULL,
    [Status] nvarchar(max) NOT NULL,
    [Notes] nvarchar(max) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_IdCardRecords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_IdCardRecords_Staff_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [Staff] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [LeaveBalances] (
    [Id] int NOT NULL IDENTITY,
    [StaffId] nvarchar(128) NOT NULL,
    [LeaveTypeId] int NOT NULL,
    [Year] int NOT NULL,
    [AllocatedDays] int NOT NULL,
    [UsedDays] int NOT NULL,
    [RemainingDays] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NULL,
    CONSTRAINT [PK_LeaveBalances] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LeaveBalances_LeaveTypes_LeaveTypeId] FOREIGN KEY ([LeaveTypeId]) REFERENCES [LeaveTypes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LeaveBalances_Staff_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [Staff] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [LeaveRequests] (
    [Id] int NOT NULL IDENTITY,
    [StaffId] nvarchar(128) NOT NULL,
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
    CONSTRAINT [PK_LeaveRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LeaveRequests_LeaveTypes_LeaveTypeId] FOREIGN KEY ([LeaveTypeId]) REFERENCES [LeaveTypes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LeaveRequests_Staff_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [Staff] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [PayrollRecords] (
    [Id] int NOT NULL IDENTITY,
    [StaffId] nvarchar(128) NOT NULL,
    [PayrollMonth] datetime2 NOT NULL,
    [BasicSalary] decimal(18,2) NOT NULL,
    [Allowances] decimal(18,2) NOT NULL,
    [Deductions] decimal(18,2) NOT NULL,
    [NetSalary] decimal(18,2) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [PaymentDate] datetime2 NULL,
    [Notes] nvarchar(max) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NULL,
    CONSTRAINT [PK_PayrollRecords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PayrollRecords_Staff_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [Staff] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ReportSchedules] (
    [Id] int NOT NULL IDENTITY,
    [ReportName] nvarchar(max) NOT NULL,
    [ReportType] nvarchar(max) NOT NULL,
    [RecurrencePattern] nvarchar(max) NOT NULL,
    [DayOfWeek] int NULL,
    [DayOfMonth] int NULL,
    [TimeOfDay] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [EmailRecipients] nvarchar(max) NOT NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [LastRunDate] datetime2 NULL,
    [NextRunDate] datetime2 NULL,
    CONSTRAINT [PK_ReportSchedules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReportSchedules_Staff_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Staff] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [StaffAttendances] (
    [Id] int NOT NULL IDENTITY,
    [StaffId] nvarchar(128) NOT NULL,
    [AttendanceDate] datetime2 NOT NULL,
    [CheckInTime] datetime2 NULL,
    [CheckOutTime] datetime2 NULL,
    [Status] nvarchar(max) NOT NULL,
    [Notes] nvarchar(max) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NULL,
    CONSTRAINT [PK_StaffAttendances] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StaffAttendances_Staff_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [Staff] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [StaffRoles] (
    [StaffId] nvarchar(128) NOT NULL,
    [RoleId] int NOT NULL,
    [AssignedDate] datetime2 NOT NULL,
    [AssignedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_StaffRoles] PRIMARY KEY ([StaffId], [RoleId]),
    CONSTRAINT [FK_StaffRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StaffRoles_Staff_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [Staff] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [UserActionLogs] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(max) NOT NULL,
    [ActionType] nvarchar(max) NOT NULL,
    [Details] nvarchar(max) NOT NULL,
    [IPAddress] nvarchar(max) NOT NULL,
    [LoggedDate] datetime2 NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [StaffId] nvarchar(128) NOT NULL,
    CONSTRAINT [PK_UserActionLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserActionLogs_Staff_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [Staff] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [ChatEscalations] (
    [Id] bigint NOT NULL IDENTITY,
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
    CONSTRAINT [PK_ChatEscalations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ChatEscalations_ChatMessages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [ChatMessages] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ChatEscalations_ChatSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [ChatSessions] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [ChatFeedback] (
    [Id] bigint NOT NULL IDENTITY,
    [SessionId] nvarchar(64) NOT NULL,
    [MessageId] bigint NULL,
    [FeedbackType] nvarchar(20) NOT NULL,
    [Comment] nvarchar(1000) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_ChatFeedback] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ChatFeedback_ChatMessages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [ChatMessages] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ChatFeedback_ChatSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [ChatSessions] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Appointments] (
    [Id] int NOT NULL IDENTITY,
    [AppointmentId] int NOT NULL,
    [PatientId] int NOT NULL,
    [DoctorId] int NOT NULL,
    [StaffId] nvarchar(128) NOT NULL,
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
    CONSTRAINT [PK_Appointments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Appointments_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Appointments_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Appointments_Staff_StaffId] FOREIGN KEY ([StaffId]) REFERENCES [Staff] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [DoctorShifts] (
    [Id] int NOT NULL IDENTITY,
    [DoctorId] int NOT NULL,
    [DayOfWeek] int NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [SlotDurationMinutes] int NOT NULL,
    [MaxPatientsPerSlot] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_DoctorShifts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DoctorShifts_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PublicAppointmentRequests] (
    [Id] int NOT NULL IDENTITY,
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
    CONSTRAINT [PK_PublicAppointmentRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PublicAppointmentRequests_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PublicAppointmentRequests_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [BillItems] (
    [Id] int NOT NULL IDENTITY,
    [BillId] int NOT NULL,
    [ItemName] nvarchar(max) NOT NULL,
    [ItemType] nvarchar(max) NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [TotalPrice] decimal(18,2) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_BillItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BillItems_Bills_BillId] FOREIGN KEY ([BillId]) REFERENCES [Bills] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [BloodIssues] (
    [Id] int NOT NULL IDENTITY,
    [PatientId] int NOT NULL,
    [BloodGroup] nvarchar(max) NOT NULL,
    [UnitsIssued] int NOT NULL,
    [IssueDate] datetime2 NOT NULL,
    [RequestedBy] nvarchar(max) NOT NULL,
    [CrossMatchStatus] nvarchar(max) NOT NULL,
    [Notes] nvarchar(max) NOT NULL,
    [BillId] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_BloodIssues] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BloodIssues_Bills_BillId] FOREIGN KEY ([BillId]) REFERENCES [Bills] ([Id]),
    CONSTRAINT [FK_BloodIssues_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [OTSchedules] (
    [Id] int NOT NULL IDENTITY,
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
    CONSTRAINT [PK_OTSchedules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OTSchedules_Bills_BillId] FOREIGN KEY ([BillId]) REFERENCES [Bills] ([Id]),
    CONSTRAINT [FK_OTSchedules_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Payments] (
    [Id] int NOT NULL IDENTITY,
    [BillId] int NOT NULL,
    [PaymentMethod] nvarchar(max) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [TransactionId] nvarchar(max) NOT NULL,
    [PaymentGateway] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [Notes] nvarchar(max) NOT NULL,
    [PaymentDate] datetime2 NOT NULL,
    [ProcessedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Payments_Bills_BillId] FOREIGN KEY ([BillId]) REFERENCES [Bills] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Referrals] (
    [Id] int NOT NULL IDENTITY,
    [PatientId] int NOT NULL,
    [ReferralType] nvarchar(max) NOT NULL,
    [ReferredTo] nvarchar(max) NOT NULL,
    [ReferralReason] nvarchar(max) NOT NULL,
    [ReferralDate] datetime2 NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [TpaProvider] nvarchar(max) NOT NULL,
    [TpaPolicyNumber] nvarchar(max) NOT NULL,
    [ApprovedAmount] decimal(18,2) NULL,
    [Notes] nvarchar(max) NOT NULL,
    [BillId] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Referrals] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Referrals_Bills_BillId] FOREIGN KEY ([BillId]) REFERENCES [Bills] ([Id]),
    CONSTRAINT [FK_Referrals_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [IPDAdmissions] (
    [Id] int NOT NULL IDENTITY,
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
    [DailyCharges] decimal(18,2) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [MedicalRecordId] int NULL,
    CONSTRAINT [PK_IPDAdmissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_IPDAdmissions_Beds_BedId] FOREIGN KEY ([BedId]) REFERENCES [Beds] ([Id]),
    CONSTRAINT [FK_IPDAdmissions_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_IPDAdmissions_MedicalRecords_MedicalRecordId] FOREIGN KEY ([MedicalRecordId]) REFERENCES [MedicalRecords] ([Id]),
    CONSTRAINT [FK_IPDAdmissions_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [OPDVisits] (
    [Id] int NOT NULL IDENTITY,
    [PatientId] int NOT NULL,
    [DoctorId] int NOT NULL,
    [VisitDate] datetime2 NOT NULL,
    [Symptoms] nvarchar(max) NOT NULL,
    [Diagnosis] nvarchar(max) NOT NULL,
    [Treatment] nvarchar(max) NOT NULL,
    [Prescription] nvarchar(max) NOT NULL,
    [Notes] nvarchar(max) NOT NULL,
    [ConsultationFee] decimal(18,2) NOT NULL,
    [PaymentStatus] nvarchar(max) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [MedicalRecordId] int NULL,
    CONSTRAINT [PK_OPDVisits] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OPDVisits_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OPDVisits_MedicalRecords_MedicalRecordId] FOREIGN KEY ([MedicalRecordId]) REFERENCES [MedicalRecords] ([Id]),
    CONSTRAINT [FK_OPDVisits_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [TestResults] (
    [Id] int NOT NULL IDENTITY,
    [PatientId] int NOT NULL,
    [TestType] nvarchar(100) NOT NULL,
    [TestName] nvarchar(200) NOT NULL,
    [TestDescription] nvarchar(500) NOT NULL,
    [Result] nvarchar(max) NOT NULL,
    [Unit] nvarchar(50) NOT NULL,
    [ReferenceRange] nvarchar(50) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [PerformedBy] nvarchar(100) NOT NULL,
    [DoctorId] nvarchar(128) NOT NULL,
    [TestDate] datetime2 NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    [MedicalRecordId] int NULL,
    CONSTRAINT [PK_TestResults] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TestResults_AspNetUsers_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TestResults_MedicalRecords_MedicalRecordId] FOREIGN KEY ([MedicalRecordId]) REFERENCES [MedicalRecords] ([Id]),
    CONSTRAINT [FK_TestResults_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Prescriptions] (
    [Id] int NOT NULL IDENTITY,
    [PharmacyBillId] int NOT NULL,
    [MedicineId] int NOT NULL,
    [Dosage] nvarchar(max) NOT NULL,
    [Frequency] nvarchar(max) NOT NULL,
    [Duration] int NOT NULL,
    [Quantity] int NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [TotalPrice] decimal(18,2) NOT NULL,
    [Instructions] nvarchar(max) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Prescriptions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Prescriptions_Medicines_MedicineId] FOREIGN KEY ([MedicineId]) REFERENCES [Medicines] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Prescriptions_PharmacyBills_PharmacyBillId] FOREIGN KEY ([PharmacyBillId]) REFERENCES [PharmacyBills] ([Id]) ON DELETE CASCADE
);
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccessFailedCount', N'ConcurrencyStamp', N'CreatedDate', N'Email', N'EmailConfirmed', N'EmployeeId', N'FirstLoginDate', N'FirstName', N'IsActive', N'LastLoginDate', N'LastName', N'LockoutEnabled', N'LockoutEnd', N'NormalizedEmail', N'NormalizedUserName', N'PasswordHash', N'PhoneNumber', N'PhoneNumberConfirmed', N'ProfileImage', N'SecurityStamp', N'TwoFactorEnabled', N'UserName') AND [object_id] = OBJECT_ID(N'[AspNetUsers]'))
    SET IDENTITY_INSERT [AspNetUsers] ON;
INSERT INTO [AspNetUsers] ([Id], [AccessFailedCount], [ConcurrencyStamp], [CreatedDate], [Email], [EmailConfirmed], [EmployeeId], [FirstLoginDate], [FirstName], [IsActive], [LastLoginDate], [LastName], [LockoutEnabled], [LockoutEnd], [NormalizedEmail], [NormalizedUserName], [PasswordHash], [PhoneNumber], [PhoneNumberConfirmed], [ProfileImage], [SecurityStamp], [TwoFactorEnabled], [UserName])
VALUES (N'superadmin-user-id', 0, N'c46314b2-b32b-40bc-9f6a-8f86d9226883', '2026-04-20T10:12:33.8154378Z', N'superadmin@hospital.com', CAST(1 AS bit), N'SUPER001', NULL, N'Super', CAST(1 AS bit), NULL, N'Admin', CAST(0 AS bit), NULL, N'SUPERADMIN@HOSPITAL.COM', N'SUPERADMIN', NULL, NULL, CAST(0 AS bit), NULL, N'bd050a67-aef4-4356-8918-3a025db5f98f', CAST(0 AS bit), N'superadmin@hospital.com');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccessFailedCount', N'ConcurrencyStamp', N'CreatedDate', N'Email', N'EmailConfirmed', N'EmployeeId', N'FirstLoginDate', N'FirstName', N'IsActive', N'LastLoginDate', N'LastName', N'LockoutEnabled', N'LockoutEnd', N'NormalizedEmail', N'NormalizedUserName', N'PasswordHash', N'PhoneNumber', N'PhoneNumberConfirmed', N'ProfileImage', N'SecurityStamp', N'TwoFactorEnabled', N'UserName') AND [object_id] = OBJECT_ID(N'[AspNetUsers]'))
    SET IDENTITY_INSERT [AspNetUsers] OFF;
GO


CREATE INDEX [IX_Appointments_DoctorId] ON [Appointments] ([DoctorId]);
GO


CREATE INDEX [IX_Appointments_PatientId] ON [Appointments] ([PatientId]);
GO


CREATE INDEX [IX_Appointments_StaffId] ON [Appointments] ([StaffId]);
GO


CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO


CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO


CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO


CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO


CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO


CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
GO


CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO


CREATE UNIQUE INDEX [UX_AspNetUsers_UserName] ON [AspNetUsers] ([UserName]);
GO


CREATE UNIQUE INDEX [UX_AspNetUsers_Id_UserName] ON [AspNetUsers] ([Id], [UserName]);
GO


CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
GO


CREATE INDEX [IX_Beds_WardId] ON [Beds] ([WardId]);
GO


CREATE INDEX [IX_BillItems_BillId] ON [BillItems] ([BillId]);
GO


CREATE INDEX [IX_Bills_PatientId] ON [Bills] ([PatientId]);
GO


CREATE INDEX [IX_BloodIssues_BillId] ON [BloodIssues] ([BillId]);
GO


CREATE INDEX [IX_BloodIssues_PatientId] ON [BloodIssues] ([PatientId]);
GO


CREATE INDEX [IX_CertificateRecords_StaffId] ON [CertificateRecords] ([StaffId]);
GO


CREATE INDEX [IX_ChatbotEventLogs_SessionId_CreatedAtUtc] ON [ChatbotEventLogs] ([SessionId], [CreatedAtUtc]);
GO


CREATE INDEX [IX_ChatEscalations_MessageId] ON [ChatEscalations] ([MessageId]);
GO


CREATE INDEX [IX_ChatEscalations_SessionId] ON [ChatEscalations] ([SessionId]);
GO


CREATE INDEX [IX_ChatEscalations_Status_CreatedAtUtc] ON [ChatEscalations] ([Status], [CreatedAtUtc]);
GO


CREATE INDEX [IX_ChatFeedback_MessageId] ON [ChatFeedback] ([MessageId]);
GO


CREATE INDEX [IX_ChatFeedback_SessionId_CreatedAtUtc] ON [ChatFeedback] ([SessionId], [CreatedAtUtc]);
GO


CREATE INDEX [IX_ChatMessages_SessionId_CreatedAtUtc] ON [ChatMessages] ([SessionId], [CreatedAtUtc]);
GO


CREATE INDEX [IX_CmsMenuItems_CmsPageId] ON [CmsMenuItems] ([CmsPageId]);
GO


CREATE UNIQUE INDEX [IX_CmsNotices_Slug] ON [CmsNotices] ([Slug]);
GO


CREATE UNIQUE INDEX [IX_CmsPages_Slug] ON [CmsPages] ([Slug]);
GO


CREATE INDEX [IX_Doctors_DepartmentId] ON [Doctors] ([DepartmentId]);
GO


CREATE INDEX [IX_DoctorShifts_DoctorId] ON [DoctorShifts] ([DoctorId]);
GO


CREATE INDEX [IX_GeneratedReports_CreatedDate_ReportType] ON [GeneratedReports] ([CreatedDate], [ReportType]);
GO


CREATE INDEX [IX_GeneratedReports_GeneratedBy] ON [GeneratedReports] ([GeneratedBy]);
GO


CREATE UNIQUE INDEX [IX_IdCardRecords_CardNumber] ON [IdCardRecords] ([CardNumber]);
GO


CREATE INDEX [IX_IdCardRecords_StaffId] ON [IdCardRecords] ([StaffId]);
GO


CREATE INDEX [IX_IPDAdmissions_BedId] ON [IPDAdmissions] ([BedId]);
GO


CREATE INDEX [IX_IPDAdmissions_DoctorId] ON [IPDAdmissions] ([DoctorId]);
GO


CREATE INDEX [IX_IPDAdmissions_MedicalRecordId] ON [IPDAdmissions] ([MedicalRecordId]);
GO


CREATE INDEX [IX_IPDAdmissions_PatientId] ON [IPDAdmissions] ([PatientId]);
GO


CREATE INDEX [IX_LabResults_LabTestId] ON [LabResults] ([LabTestId]);
GO


CREATE INDEX [IX_LabResults_PatientId] ON [LabResults] ([PatientId]);
GO


CREATE INDEX [IX_LeaveBalances_LeaveTypeId] ON [LeaveBalances] ([LeaveTypeId]);
GO


CREATE UNIQUE INDEX [IX_LeaveBalances_StaffId_LeaveTypeId_Year] ON [LeaveBalances] ([StaffId], [LeaveTypeId], [Year]);
GO


CREATE INDEX [IX_LeaveRequests_LeaveTypeId] ON [LeaveRequests] ([LeaveTypeId]);
GO


CREATE INDEX [IX_LeaveRequests_StaffId] ON [LeaveRequests] ([StaffId]);
GO


CREATE INDEX [IX_LicenseAuditLogs_LicenseRecordId_PerformedAtUtc] ON [LicenseAuditLogs] ([LicenseRecordId], [PerformedAtUtc]);
GO


CREATE INDEX [IX_LicenseAuditLogs_PerformedByUserId] ON [LicenseAuditLogs] ([PerformedByUserId]);
GO


CREATE INDEX [IX_LicenseRecords_IsActive_ExpiresAtUtc] ON [LicenseRecords] ([IsActive], [ExpiresAtUtc]);
GO


CREATE INDEX [IX_LicenseReminderLogs_LicenseRecordId_TargetExpiryUtc_TriggeredAtUtc] ON [LicenseReminderLogs] ([LicenseRecordId], [TargetExpiryUtc], [TriggeredAtUtc]);
GO


CREATE INDEX [IX_MedicalRecords_DoctorId] ON [MedicalRecords] ([DoctorId]);
GO


CREATE INDEX [IX_MedicalRecords_PatientId] ON [MedicalRecords] ([PatientId]);
GO


CREATE INDEX [IX_NotificationDeliveryLogs_CreatedAt_Channel_Status] ON [NotificationDeliveryLogs] ([CreatedAt], [Channel], [Status]);
GO


CREATE INDEX [IX_OPDVisits_DoctorId] ON [OPDVisits] ([DoctorId]);
GO


CREATE INDEX [IX_OPDVisits_MedicalRecordId] ON [OPDVisits] ([MedicalRecordId]);
GO


CREATE INDEX [IX_OPDVisits_PatientId] ON [OPDVisits] ([PatientId]);
GO


CREATE INDEX [IX_OTSchedules_BillId] ON [OTSchedules] ([BillId]);
GO


CREATE INDEX [IX_OTSchedules_PatientId] ON [OTSchedules] ([PatientId]);
GO


CREATE INDEX [IX_Patients_UserId] ON [Patients] ([UserId]);
GO


CREATE INDEX [IX_Payments_BillId] ON [Payments] ([BillId]);
GO


CREATE UNIQUE INDEX [IX_PayrollRecords_StaffId_PayrollMonth] ON [PayrollRecords] ([StaffId], [PayrollMonth]);
GO


CREATE INDEX [IX_PharmacyBills_PatientId] ON [PharmacyBills] ([PatientId]);
GO


CREATE INDEX [IX_Prescriptions_MedicineId] ON [Prescriptions] ([MedicineId]);
GO


CREATE INDEX [IX_Prescriptions_PharmacyBillId] ON [Prescriptions] ([PharmacyBillId]);
GO


CREATE INDEX [IX_PublicAppointmentRequests_DoctorId] ON [PublicAppointmentRequests] ([DoctorId]);
GO


CREATE INDEX [IX_PublicAppointmentRequests_PatientId] ON [PublicAppointmentRequests] ([PatientId]);
GO


CREATE INDEX [IX_RadiologyResults_PatientId] ON [RadiologyResults] ([PatientId]);
GO


CREATE INDEX [IX_RadiologyResults_RadiologyTestId] ON [RadiologyResults] ([RadiologyTestId]);
GO


CREATE INDEX [IX_Referrals_BillId] ON [Referrals] ([BillId]);
GO


CREATE INDEX [IX_Referrals_PatientId] ON [Referrals] ([PatientId]);
GO


CREATE INDEX [IX_ReportSchedules_CreatedBy] ON [ReportSchedules] ([CreatedBy]);
GO


CREATE INDEX [IX_ReportSchedules_IsActive_NextRunDate] ON [ReportSchedules] ([IsActive], [NextRunDate]);
GO


CREATE INDEX [IX_RoleFeatures_FeatureId] ON [RoleFeatures] ([FeatureId]);
GO


CREATE INDEX [IX_Staff_UserId] ON [Staff] ([UserId]);
GO


CREATE UNIQUE INDEX [IX_StaffAttendances_StaffId_AttendanceDate] ON [StaffAttendances] ([StaffId], [AttendanceDate]);
GO


CREATE INDEX [IX_StaffRoles_RoleId] ON [StaffRoles] ([RoleId]);
GO


CREATE INDEX [IX_TestResults_DoctorId] ON [TestResults] ([DoctorId]);
GO


CREATE INDEX [IX_TestResults_MedicalRecordId] ON [TestResults] ([MedicalRecordId]);
GO


CREATE INDEX [IX_TestResults_PatientId] ON [TestResults] ([PatientId]);
GO


CREATE INDEX [IX_UserActionLogs_StaffId] ON [UserActionLogs] ([StaffId]);
GO


/* =========================
   Baseline seed data
   ========================= */

DECLARE @UtcNow datetime2 = SYSUTCDATETIME();

/* 1) Identity roles */
IF NOT EXISTS (SELECT 1 FROM [AspNetRoles] WHERE [NormalizedName] = N'SUPERADMIN')
BEGIN
    INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (N'2A1F8A1E-3D8D-4D67-B4DE-67FBE5D4E001', N'SuperAdmin', N'SUPERADMIN', N'8A6D8FEF-35C7-4D49-B429-D66A8A80B001');
END

IF NOT EXISTS (SELECT 1 FROM [AspNetRoles] WHERE [NormalizedName] = N'ADMIN')
BEGIN
    INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (N'2A1F8A1E-3D8D-4D67-B4DE-67FBE5D4E002', N'Admin', N'ADMIN', N'8A6D8FEF-35C7-4D49-B429-D66A8A80B002');
END

IF NOT EXISTS (SELECT 1 FROM [AspNetRoles] WHERE [NormalizedName] = N'DOCTOR')
BEGIN
    INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (N'2A1F8A1E-3D8D-4D67-B4DE-67FBE5D4E003', N'Doctor', N'DOCTOR', N'8A6D8FEF-35C7-4D49-B429-D66A8A80B003');
END

IF NOT EXISTS (SELECT 1 FROM [AspNetRoles] WHERE [NormalizedName] = N'NURSE')
BEGIN
    INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (N'2A1F8A1E-3D8D-4D67-B4DE-67FBE5D4E004', N'Nurse', N'NURSE', N'8A6D8FEF-35C7-4D49-B429-D66A8A80B004');
END

IF NOT EXISTS (SELECT 1 FROM [AspNetRoles] WHERE [NormalizedName] = N'STAFF')
BEGIN
    INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (N'2A1F8A1E-3D8D-4D67-B4DE-67FBE5D4E005', N'Staff', N'STAFF', N'8A6D8FEF-35C7-4D49-B429-D66A8A80B005');
END

IF NOT EXISTS (SELECT 1 FROM [AspNetRoles] WHERE [NormalizedName] = N'RECEPTIONIST')
BEGIN
    INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (N'2A1F8A1E-3D8D-4D67-B4DE-67FBE5D4E006', N'Receptionist', N'RECEPTIONIST', N'8A6D8FEF-35C7-4D49-B429-D66A8A80B006');
END

IF NOT EXISTS (SELECT 1 FROM [AspNetRoles] WHERE [NormalizedName] = N'PHARMACIST')
BEGIN
    INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (N'2A1F8A1E-3D8D-4D67-B4DE-67FBE5D4E007', N'Pharmacist', N'PHARMACIST', N'8A6D8FEF-35C7-4D49-B429-D66A8A80B007');
END

IF NOT EXISTS (SELECT 1 FROM [AspNetRoles] WHERE [NormalizedName] = N'LABTECHNICIAN')
BEGIN
    INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (N'2A1F8A1E-3D8D-4D67-B4DE-67FBE5D4E008', N'LabTechnician', N'LABTECHNICIAN', N'8A6D8FEF-35C7-4D49-B429-D66A8A80B008');
END

IF NOT EXISTS (SELECT 1 FROM [AspNetRoles] WHERE [NormalizedName] = N'RADIOLOGIST')
BEGIN
    INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (N'2A1F8A1E-3D8D-4D67-B4DE-67FBE5D4E009', N'Radiologist', N'RADIOLOGIST', N'8A6D8FEF-35C7-4D49-B429-D66A8A80B009');
END

/* 2) Application roles */
;WITH RoleSeed AS (
    SELECT N'SuperAdmin' AS [Name], N'SuperAdmin role' AS [Description] UNION ALL
    SELECT N'Admin', N'Admin role' UNION ALL
    SELECT N'Doctor', N'Doctor role' UNION ALL
    SELECT N'Nurse', N'Nurse role' UNION ALL
    SELECT N'Staff', N'Staff role' UNION ALL
    SELECT N'Receptionist', N'Receptionist role' UNION ALL
    SELECT N'Pharmacist', N'Pharmacist role' UNION ALL
    SELECT N'LabTechnician', N'LabTechnician role' UNION ALL
    SELECT N'Radiologist', N'Radiologist role'
)
INSERT INTO [Roles] ([Name], [Description], [IsActive], [CreatedDate])
SELECT rs.[Name], rs.[Description], 1, @UtcNow
FROM RoleSeed rs
WHERE NOT EXISTS (
    SELECT 1 FROM [Roles] r WHERE r.[Name] = rs.[Name]
);

/* 3) Features/modules */
;WITH FeatureSeed AS (
    SELECT N'ViewPatients' AS [Name], N'Patient' AS [Module], N'View patient records' AS [Description] UNION ALL
    SELECT N'AddPatients', N'Patient', N'Add new patients' UNION ALL
    SELECT N'EditPatients', N'Patient', N'Edit patient information' UNION ALL
    SELECT N'DeletePatients', N'Patient', N'Delete patient records' UNION ALL
    SELECT N'ViewAppointments', N'Appointment', N'View appointments' UNION ALL
    SELECT N'AddAppointments', N'Appointment', N'Schedule appointments' UNION ALL
    SELECT N'EditAppointments', N'Appointment', N'Edit appointments' UNION ALL
    SELECT N'DeleteAppointments', N'Appointment', N'Cancel appointments' UNION ALL
    SELECT N'ViewBills', N'Billing', N'View bills and invoices' UNION ALL
    SELECT N'AddBills', N'Billing', N'Create bills' UNION ALL
    SELECT N'EditBills', N'Billing', N'Edit bills' UNION ALL
    SELECT N'DeleteBills', N'Billing', N'Delete bills' UNION ALL
    SELECT N'ProcessPayments', N'Billing', N'Process payments' UNION ALL
    SELECT N'ViewOPDVisits', N'OPD', N'View OPD visits' UNION ALL
    SELECT N'AddOPDVisits', N'OPD', N'Add OPD visits' UNION ALL
    SELECT N'ViewIPDAdmissions', N'IPD', N'View IPD admissions' UNION ALL
    SELECT N'AddIPDAdmissions', N'IPD', N'Add IPD admissions' UNION ALL
    SELECT N'ViewMedicines', N'Pharmacy', N'View medicines' UNION ALL
    SELECT N'AddMedicines', N'Pharmacy', N'Add medicines' UNION ALL
    SELECT N'DispenseMedicines', N'Pharmacy', N'Dispense medicines' UNION ALL
    SELECT N'ViewLabTests', N'Lab', N'View lab tests' UNION ALL
    SELECT N'AddLabTests', N'Lab', N'Order lab tests' UNION ALL
    SELECT N'ViewRadiologyTests', N'Radiology', N'View radiology tests' UNION ALL
    SELECT N'AddRadiologyTests', N'Radiology', N'Order radiology tests' UNION ALL
    SELECT N'ManageUsers', N'Admin', N'Manage user accounts' UNION ALL
    SELECT N'ManageRoles', N'Admin', N'Manage roles and permissions' UNION ALL
    SELECT N'ViewReports', N'Reports', N'View reports' UNION ALL
    SELECT N'ManageSettings', N'Admin', N'Manage system settings'
)
INSERT INTO [Features] ([Name], [Module], [Description], [IsActive], [CreatedDate])
SELECT fs.[Name], fs.[Module], fs.[Description], 1, @UtcNow
FROM FeatureSeed fs
WHERE NOT EXISTS (
    SELECT 1 FROM [Features] f WHERE f.[Name] = fs.[Name]
);

/* 4) Role-feature mapping */
;WITH PermissionSeed AS (
    SELECT N'SuperAdmin' AS RoleName, N'ViewPatients' AS FeatureName UNION ALL
    SELECT N'SuperAdmin', N'AddPatients' UNION ALL
    SELECT N'SuperAdmin', N'EditPatients' UNION ALL
    SELECT N'SuperAdmin', N'DeletePatients' UNION ALL
    SELECT N'SuperAdmin', N'ViewAppointments' UNION ALL
    SELECT N'SuperAdmin', N'AddAppointments' UNION ALL
    SELECT N'SuperAdmin', N'EditAppointments' UNION ALL
    SELECT N'SuperAdmin', N'DeleteAppointments' UNION ALL
    SELECT N'SuperAdmin', N'ViewBills' UNION ALL
    SELECT N'SuperAdmin', N'AddBills' UNION ALL
    SELECT N'SuperAdmin', N'EditBills' UNION ALL
    SELECT N'SuperAdmin', N'DeleteBills' UNION ALL
    SELECT N'SuperAdmin', N'ProcessPayments' UNION ALL
    SELECT N'SuperAdmin', N'ViewOPDVisits' UNION ALL
    SELECT N'SuperAdmin', N'AddOPDVisits' UNION ALL
    SELECT N'SuperAdmin', N'ViewIPDAdmissions' UNION ALL
    SELECT N'SuperAdmin', N'AddIPDAdmissions' UNION ALL
    SELECT N'SuperAdmin', N'ViewMedicines' UNION ALL
    SELECT N'SuperAdmin', N'AddMedicines' UNION ALL
    SELECT N'SuperAdmin', N'DispenseMedicines' UNION ALL
    SELECT N'SuperAdmin', N'ViewLabTests' UNION ALL
    SELECT N'SuperAdmin', N'AddLabTests' UNION ALL
    SELECT N'SuperAdmin', N'ViewRadiologyTests' UNION ALL
    SELECT N'SuperAdmin', N'AddRadiologyTests' UNION ALL
    SELECT N'SuperAdmin', N'ManageUsers' UNION ALL
    SELECT N'SuperAdmin', N'ManageRoles' UNION ALL
    SELECT N'SuperAdmin', N'ViewReports' UNION ALL
    SELECT N'SuperAdmin', N'ManageSettings' UNION ALL
    SELECT N'Admin', N'ViewPatients' UNION ALL
    SELECT N'Admin', N'AddPatients' UNION ALL
    SELECT N'Admin', N'EditPatients' UNION ALL
    SELECT N'Admin', N'ViewAppointments' UNION ALL
    SELECT N'Admin', N'AddAppointments' UNION ALL
    SELECT N'Admin', N'EditAppointments' UNION ALL
    SELECT N'Admin', N'ViewBills' UNION ALL
    SELECT N'Admin', N'AddBills' UNION ALL
    SELECT N'Admin', N'EditBills' UNION ALL
    SELECT N'Admin', N'ProcessPayments' UNION ALL
    SELECT N'Admin', N'ViewOPDVisits' UNION ALL
    SELECT N'Admin', N'AddOPDVisits' UNION ALL
    SELECT N'Admin', N'ViewIPDAdmissions' UNION ALL
    SELECT N'Admin', N'AddIPDAdmissions' UNION ALL
    SELECT N'Admin', N'ViewMedicines' UNION ALL
    SELECT N'Admin', N'AddMedicines' UNION ALL
    SELECT N'Admin', N'DispenseMedicines' UNION ALL
    SELECT N'Admin', N'ViewLabTests' UNION ALL
    SELECT N'Admin', N'AddLabTests' UNION ALL
    SELECT N'Admin', N'ViewRadiologyTests' UNION ALL
    SELECT N'Admin', N'AddRadiologyTests' UNION ALL
    SELECT N'Admin', N'ManageUsers' UNION ALL
    SELECT N'Admin', N'ViewReports' UNION ALL
    SELECT N'Admin', N'ManageSettings' UNION ALL
    SELECT N'Doctor', N'ViewPatients' UNION ALL
    SELECT N'Doctor', N'EditPatients' UNION ALL
    SELECT N'Doctor', N'ViewAppointments' UNION ALL
    SELECT N'Doctor', N'AddAppointments' UNION ALL
    SELECT N'Doctor', N'EditAppointments' UNION ALL
    SELECT N'Doctor', N'ViewOPDVisits' UNION ALL
    SELECT N'Doctor', N'AddOPDVisits' UNION ALL
    SELECT N'Doctor', N'ViewIPDAdmissions' UNION ALL
    SELECT N'Doctor', N'AddIPDAdmissions' UNION ALL
    SELECT N'Doctor', N'ViewLabTests' UNION ALL
    SELECT N'Doctor', N'AddLabTests' UNION ALL
    SELECT N'Doctor', N'ViewRadiologyTests' UNION ALL
    SELECT N'Doctor', N'AddRadiologyTests' UNION ALL
    SELECT N'Nurse', N'ViewPatients' UNION ALL
    SELECT N'Nurse', N'ViewAppointments' UNION ALL
    SELECT N'Nurse', N'ViewOPDVisits' UNION ALL
    SELECT N'Nurse', N'AddOPDVisits' UNION ALL
    SELECT N'Nurse', N'ViewIPDAdmissions' UNION ALL
    SELECT N'Nurse', N'AddIPDAdmissions' UNION ALL
    SELECT N'Nurse', N'ViewMedicines' UNION ALL
    SELECT N'Nurse', N'DispenseMedicines' UNION ALL
    SELECT N'Staff', N'ViewPatients' UNION ALL
    SELECT N'Staff', N'ViewAppointments' UNION ALL
    SELECT N'Staff', N'AddAppointments' UNION ALL
    SELECT N'Staff', N'ViewBills' UNION ALL
    SELECT N'Receptionist', N'ViewPatients' UNION ALL
    SELECT N'Receptionist', N'AddPatients' UNION ALL
    SELECT N'Receptionist', N'EditPatients' UNION ALL
    SELECT N'Receptionist', N'ViewAppointments' UNION ALL
    SELECT N'Receptionist', N'AddAppointments' UNION ALL
    SELECT N'Receptionist', N'EditAppointments' UNION ALL
    SELECT N'Receptionist', N'ViewBills' UNION ALL
    SELECT N'Receptionist', N'AddBills' UNION ALL
    SELECT N'Pharmacist', N'ViewPatients' UNION ALL
    SELECT N'Pharmacist', N'ViewMedicines' UNION ALL
    SELECT N'Pharmacist', N'AddMedicines' UNION ALL
    SELECT N'Pharmacist', N'DispenseMedicines' UNION ALL
    SELECT N'LabTechnician', N'ViewPatients' UNION ALL
    SELECT N'LabTechnician', N'ViewLabTests' UNION ALL
    SELECT N'LabTechnician', N'AddLabTests' UNION ALL
    SELECT N'Radiologist', N'ViewPatients' UNION ALL
    SELECT N'Radiologist', N'ViewRadiologyTests' UNION ALL
    SELECT N'Radiologist', N'AddRadiologyTests'
)
INSERT INTO [RoleFeatures] ([RoleId], [FeatureId], [CanView], [CanAdd], [CanEdit], [CanDelete], [CreatedDate])
SELECT r.[Id], f.[Id], 1,
       CASE WHEN ps.FeatureName LIKE N'Add%' THEN 1 ELSE 0 END,
       CASE WHEN ps.FeatureName LIKE N'Edit%' THEN 1 ELSE 0 END,
       CASE WHEN ps.FeatureName LIKE N'Delete%' THEN 1 ELSE 0 END,
       @UtcNow
FROM PermissionSeed ps
INNER JOIN [Roles] r ON r.[Name] = ps.RoleName
INNER JOIN [Features] f ON f.[Name] = ps.FeatureName
WHERE NOT EXISTS (
    SELECT 1
    FROM [RoleFeatures] rf
    WHERE rf.[RoleId] = r.[Id] AND rf.[FeatureId] = f.[Id]
);

/* 5) Default SuperAdmin user + mapping */
IF NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [Email] = N'superadmin@hospital.com')
BEGIN
    INSERT INTO [AspNetUsers] (
        [Id], [EmployeeId], [FirstName], [LastName], [IsActive], [CreatedDate],
        [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed],
        [PasswordHash], [SecurityStamp], [ConcurrencyStamp],
        [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount]
    )
    VALUES (
        N'1', N'SUPER001', N'Super', N'Admin', 1, @UtcNow,
        N'superadmin', N'SUPERADMIN', N'superadmin@hospital.com', N'SUPERADMIN@HOSPITAL.COM', 1,
        N'AQAAAAIAAYagAAAAENmHyolAF/zZx5gv7PnxwRPXwKYJ6dA+0y562EPe7kUseYbGGAsFnGRJGAHxPhaUFw==', N'QHPBKFIVA5XXBP4VFMJV66ZU2JAKU7S7', N'26885701-f8ad-4c53-bde0-398cad55a639',
        NULL, 0, 0, NULL, 1, 0
    );
END
ELSE
BEGIN
    UPDATE [AspNetUsers]
    SET [EmployeeId] = COALESCE(NULLIF([EmployeeId], N''), N'SUPER001'),
        [FirstName] = COALESCE(NULLIF([FirstName], N''), N'Super'),
        [LastName] = COALESCE(NULLIF([LastName], N''), N'Admin'),
        [IsActive] = 1,
        [UserName] = N'superadmin',
        [NormalizedUserName] = N'SUPERADMIN',
        [Email] = N'superadmin@hospital.com',
        [NormalizedEmail] = N'SUPERADMIN@HOSPITAL.COM',
        [EmailConfirmed] = 1,
        [PasswordHash] = COALESCE(NULLIF([PasswordHash], N''), N'AQAAAAIAAYagAAAAENmHyolAF/zZx5gv7PnxwRPXwKYJ6dA+0y562EPe7kUseYbGGAsFnGRJGAHxPhaUFw=='),
        [SecurityStamp] = COALESCE(NULLIF([SecurityStamp], N''), N'QHPBKFIVA5XXBP4VFMJV66ZU2JAKU7S7'),
        [ConcurrencyStamp] = COALESCE(NULLIF([ConcurrencyStamp], N''), N'26885701-f8ad-4c53-bde0-398cad55a639')
    WHERE [Email] = N'superadmin@hospital.com';
END

DECLARE @SuperAdminUserId nvarchar(128);
SELECT TOP (1) @SuperAdminUserId = [Id]
FROM [AspNetUsers]
WHERE [Email] = N'superadmin@hospital.com'
ORDER BY [Id];

IF @SuperAdminUserId IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM [AspNetUserRoles] ur
       INNER JOIN [AspNetRoles] r ON r.[Id] = ur.[RoleId]
       WHERE ur.[UserId] = @SuperAdminUserId
         AND r.[NormalizedName] = N'SUPERADMIN'
   )
BEGIN
    INSERT INTO [AspNetUserRoles] ([UserId], [RoleId])
    SELECT @SuperAdminUserId, r.[Id]
    FROM [AspNetRoles] r
    WHERE r.[NormalizedName] = N'SUPERADMIN';
END

IF @SuperAdminUserId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [Staff] WHERE [Id] = @SuperAdminUserId)
BEGIN
    INSERT INTO [Staff] (
        [Id], [EmployeeId], [FirstName], [LastName], [Department], [Designation],
        [DateOfJoining], [Salary], [Phone], [Address], [Email], [About], [IsActive], [CreatedDate], [UserId]
    )
    VALUES (
        @SuperAdminUserId, N'SUPER001', N'Super', N'Admin', N'Administration', N'SuperAdmin',
        @UtcNow, 0, N'', N'', N'superadmin@hospital.com', N'System generated SuperAdmin staff profile', 1, @UtcNow, @SuperAdminUserId
    );
END

IF @SuperAdminUserId IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM [StaffRoles] sr
       INNER JOIN [Roles] r ON r.[Id] = sr.[RoleId]
       WHERE sr.[StaffId] = @SuperAdminUserId
         AND r.[Name] = N'SuperAdmin'
   )
BEGIN
    INSERT INTO [StaffRoles] ([StaffId], [RoleId], [AssignedDate], [AssignedBy])
    SELECT @SuperAdminUserId, r.[Id], @UtcNow, N'System'
    FROM [Roles] r
    WHERE r.[Name] = N'SuperAdmin';
END

/* 6) Essential settings */
;WITH SettingSeed AS (
    SELECT N'PublicSiteAddress' AS [Key], N'Medyx Hospital, Main Road, Your City' AS [Value], N'string' AS [Type], N'PublicSite' AS [Category], N'Public website address displayed on contact/location pages.' AS [Description] UNION ALL
    SELECT N'PublicSitePhone', N'+000-000-0000', N'string', N'PublicSite', N'Public website contact phone number.' UNION ALL
    SELECT N'PublicSiteEmail', N'info@medyxhospital.com', N'string', N'PublicSite', N'Public website contact email address.' UNION ALL
    SELECT N'PublicSiteMapEmbedUrl', N'', N'string', N'PublicSite', N'Optional Google map embed URL; when empty, map is generated from address.' UNION ALL
    SELECT N'PublicSiteCareersContent', N'We are hiring doctors, nurses, technicians, and support staff. Share your resume using the contact email.', N'string', N'PublicSite', N'Public careers page content.' UNION ALL
    SELECT N'ChatbotEnabled', N'true', N'bool', N'Chatbot', N'Enable or disable chatbot globally.' UNION ALL
    SELECT N'ChatbotModel', N'gpt-4o-mini', N'string', N'Chatbot', N'Configured provider model for chatbot responses.' UNION ALL
    SELECT N'ChatbotTemperature', N'0.2', N'decimal', N'Chatbot', N'Configured model temperature for chatbot responses.' UNION ALL
    SELECT N'ChatbotMaxTokens', N'350', N'int', N'Chatbot', N'Configured max token target for chatbot responses.' UNION ALL
    SELECT N'LicenseReminderSubject', N'MedyxHMS license expires in {DaysRemaining} days', N'string', N'Licensing', N'Reminder email subject template for license expiry notifications.'
)
INSERT INTO [Settings] ([Key], [Value], [Type], [Category], [Description], [IsSystem], [CreatedDate], [ModifiedBy])
SELECT ss.[Key], ss.[Value], ss.[Type], ss.[Category], ss.[Description], 1, @UtcNow, N'System'
FROM SettingSeed ss
WHERE NOT EXISTS (
    SELECT 1 FROM [Settings] s WHERE s.[Key] = ss.[Key]
);
GO


/* =========================
   Utility views
   ========================= */
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


/* =========================
   Utility stored procedures
   ========================= */
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



-- Deleted as per request. See new scripts for latest schema.



