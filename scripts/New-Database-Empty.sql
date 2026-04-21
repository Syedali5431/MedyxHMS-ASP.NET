/*
    MedyxHMS New Database Bootstrap Script
    - Base schema generated from ApplicationDbContext
    - Baseline seed data (users, roles, features/modules, settings)
    - Utility views and stored procedures
*/

IF DB_ID(N'MedyxHMS') IS NULL
BEGIN
    CREATE DATABASE [MedyxHMS];
END
GO

USE [MedyxHMS];
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


/* =========================`r`n   Utility views
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



