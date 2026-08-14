-- ============================================================
--  MedyxHMS Supplemental Indexing Script
--  Target: SQL Server — database [MedyxHMS]
--
--  Purpose:
--    New-Database.sql / New-Database-Empty.sql already create one
--    non-clustered index per EF Core foreign key (see IX_* in that
--    script) plus a handful of unique indexes on business keys.
--    Those cover JOIN performance. What they do NOT cover is the
--    date-range and status filtering that every dashboard/report
--    page in this app does on top of those joins — "today's
--    appointments", "this month's bills", "pending lab results",
--    "active admissions", etc. This script adds indexes for that
--    query shape. Safe to run repeatedly — every statement checks
--    sys.indexes first and skips if the index already exists.
--
--  A note on what's deliberately NOT indexed here:
--    Most free-text/status columns in this schema (Status,
--    PatientId business key, Email, Phone, BillNumber, ...) are
--    mapped by EF Core as nvarchar(max), and SQL Server does not
--    allow MAX-length columns as index key columns. Two exceptions
--    that are worth the workaround — Patients.PatientId and
--    Bills.BillNumber are unique, high-traffic point-lookup keys —
--    get a persisted computed column + index below. The rest
--    (Status columns especially) are low-cardinality anyway, so a
--    standalone index on them buys little; the date-range indexes
--    below are the ones that actually matter for those queries.
--
--  Usage:
--    sqlcmd -S "(localdb)\MSSQLLocalDB" -d MedyxHMS -i "scripts/CreateIndexes.sql"
--    sqlcmd -S "YOUR_SERVER\INSTANCE" -E -d MedyxHMS -i "scripts/CreateIndexes.sql"
--    — or open in SSMS, connect to MedyxHMS, and execute (F5).
-- ============================================================

USE [MedyxHMS];
GO

-- Required for indexes on computed columns further down (PatientId_Key, BillNumber_Key)
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ── Helper: create an index only if it doesn't already exist ──────────────
-- (each block below is self-contained so the script can be re-run safely)

-- ============================================================
-- Appointments — doctor schedule / "today's appointments" lookups
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Appointments_AppointmentDate' AND object_id = OBJECT_ID('[Appointments]'))
    CREATE INDEX [IX_Appointments_AppointmentDate] ON [Appointments] ([AppointmentDate]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Appointments_DoctorId_AppointmentDate' AND object_id = OBJECT_ID('[Appointments]'))
    CREATE INDEX [IX_Appointments_DoctorId_AppointmentDate] ON [Appointments] ([DoctorId], [AppointmentDate]);
GO

-- ============================================================
-- OPD Visits — daily visit counts / recent visits
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OPDVisits_VisitDate' AND object_id = OBJECT_ID('[OPDVisits]'))
    CREATE INDEX [IX_OPDVisits_VisitDate] ON [OPDVisits] ([VisitDate]);
GO

-- ============================================================
-- IPD Admissions — active admissions / occupancy / discharge reports
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IPDAdmissions_AdmissionDate' AND object_id = OBJECT_ID('[IPDAdmissions]'))
    CREATE INDEX [IX_IPDAdmissions_AdmissionDate] ON [IPDAdmissions] ([AdmissionDate]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IPDAdmissions_DischargeDate' AND object_id = OBJECT_ID('[IPDAdmissions]'))
    CREATE INDEX [IX_IPDAdmissions_DischargeDate] ON [IPDAdmissions] ([DischargeDate]) WHERE [DischargeDate] IS NOT NULL;
GO

-- ============================================================
-- Bills / Payments — revenue reports, pending-bills dashboard cards
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bills_BillDate' AND object_id = OBJECT_ID('[Bills]'))
    CREATE INDEX [IX_Bills_BillDate] ON [Bills] ([BillDate]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bills_DueDate' AND object_id = OBJECT_ID('[Bills]'))
    CREATE INDEX [IX_Bills_DueDate] ON [Bills] ([DueDate]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_PaymentDate' AND object_id = OBJECT_ID('[Payments]'))
    CREATE INDEX [IX_Payments_PaymentDate] ON [Payments] ([PaymentDate]);
GO

-- ============================================================
-- Pharmacy — dispensing history
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PharmacyBills_BillDate' AND object_id = OBJECT_ID('[PharmacyBills]'))
    CREATE INDEX [IX_PharmacyBills_BillDate] ON [PharmacyBills] ([BillDate]);
GO

-- ============================================================
-- Lab / Radiology — pending-results dashboard cards, turnaround reports
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LabResults_OrderDate' AND object_id = OBJECT_ID('[LabResults]'))
    CREATE INDEX [IX_LabResults_OrderDate] ON [LabResults] ([OrderDate]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RadiologyResults_OrderDate' AND object_id = OBJECT_ID('[RadiologyResults]'))
    CREATE INDEX [IX_RadiologyResults_OrderDate] ON [RadiologyResults] ([OrderDate]);
GO

-- ============================================================
-- Medicines — low-stock alerts (Inventory dashboard)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Medicines_StockQuantity' AND object_id = OBJECT_ID('[Medicines]'))
    CREATE INDEX [IX_Medicines_StockQuantity] ON [Medicines] ([StockQuantity]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Medicines_ExpiryDate' AND object_id = OBJECT_ID('[Medicines]'))
    CREATE INDEX [IX_Medicines_ExpiryDate] ON [Medicines] ([ExpiryDate]);
GO

-- ============================================================
-- Staff Attendance / Front Office / OT / Referrals — date-range dashboard filters
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StaffAttendances_AttendanceDate' AND object_id = OBJECT_ID('[StaffAttendances]'))
    CREATE INDEX [IX_StaffAttendances_AttendanceDate] ON [StaffAttendances] ([AttendanceDate]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_VisitorLogs_VisitDate' AND object_id = OBJECT_ID('[VisitorLogs]'))
    CREATE INDEX [IX_VisitorLogs_VisitDate] ON [VisitorLogs] ([VisitDate]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OTSchedules_ScheduledDate' AND object_id = OBJECT_ID('[OTSchedules]'))
    CREATE INDEX [IX_OTSchedules_ScheduledDate] ON [OTSchedules] ([ScheduledDate]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Referrals_ReferralDate' AND object_id = OBJECT_ID('[Referrals]'))
    CREATE INDEX [IX_Referrals_ReferralDate] ON [Referrals] ([ReferralDate]);
GO

-- ============================================================
-- Audit Logs — Audit module date-range search
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLogs_Timestamp' AND object_id = OBJECT_ID('[AuditLogs]'))
    CREATE INDEX [IX_AuditLogs_Timestamp] ON [AuditLogs] ([Timestamp]);
GO

-- ============================================================
-- Point-lookup business keys (nvarchar(max) columns can't be indexed
-- directly in SQL Server, so a bounded persisted computed column is
-- added first and indexed instead — the base column is untouched).
-- ============================================================
IF COL_LENGTH('[Patients]', 'PatientId_Key') IS NULL
    ALTER TABLE [Patients] ADD [PatientId_Key] AS (CAST([PatientId] AS nvarchar(50))) PERSISTED;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Patients_PatientId_Key' AND object_id = OBJECT_ID('[Patients]'))
    CREATE UNIQUE INDEX [UX_Patients_PatientId_Key] ON [Patients] ([PatientId_Key]);
GO

IF COL_LENGTH('[Bills]', 'BillNumber_Key') IS NULL
    ALTER TABLE [Bills] ADD [BillNumber_Key] AS (CAST([BillNumber] AS nvarchar(50))) PERSISTED;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Bills_BillNumber_Key' AND object_id = OBJECT_ID('[Bills]'))
    CREATE UNIQUE INDEX [UX_Bills_BillNumber_Key] ON [Bills] ([BillNumber_Key]);
GO

PRINT 'MedyxHMS supplemental indexes created successfully.';
GO
