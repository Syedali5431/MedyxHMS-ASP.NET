IF DB_ID(N'MedyxHMS') IS NULL
BEGIN
    RAISERROR('Database [MedyxHMS] was not found. Run New-Database.sql first.', 16, 1);
    RETURN;
END;
GO

USE [MedyxHMS];
GO

SET NOCOUNT ON;

DECLARE @Missing TABLE ([TableName] sysname NOT NULL);

INSERT INTO @Missing ([TableName])
SELECT t.[name]
FROM (VALUES
    (N'Patients'),
    (N'Doctors'),
    (N'Appointments'),
    (N'Bills'),
    (N'BillItems'),
    (N'Payments'),
    (N'PublicAppointmentRequests'),
    (N'Staff')
) AS t([name])
WHERE OBJECT_ID(QUOTENAME(N'dbo') + N'.' + QUOTENAME(t.[name]), N'U') IS NULL;

IF EXISTS (SELECT 1 FROM @Missing)
BEGIN
    SELECT [TableName] AS MissingTable FROM @Missing ORDER BY [TableName];
    RAISERROR('Required tables are missing in the selected database. Run database bootstrap scripts first.', 16, 1);
    RETURN;
END;

PRINT '=== Record Counts ===';
SELECT 'Patients' AS [CheckName], COUNT(*) AS [Value] FROM Patients
UNION ALL
SELECT 'Doctors', COUNT(*) FROM Doctors
UNION ALL
SELECT 'Appointments', COUNT(*) FROM Appointments
UNION ALL
SELECT 'Bills', COUNT(*) FROM Bills
UNION ALL
SELECT 'BillItems', COUNT(*) FROM BillItems
UNION ALL
SELECT 'Payments', COUNT(*) FROM Payments
UNION ALL
SELECT 'PublicAppointmentRequests', COUNT(*) FROM PublicAppointmentRequests
UNION ALL
SELECT 'Staff', COUNT(*) FROM Staff;

PRINT '=== Integrity Checks (expected 0) ===';
SELECT 'OrphanAppointmentsMissingPatient' AS [CheckName], COUNT(*) AS [IssueCount]
FROM Appointments a LEFT JOIN Patients p ON p.Id = a.PatientId
WHERE p.Id IS NULL
UNION ALL
SELECT 'OrphanAppointmentsMissingDoctor', COUNT(*)
FROM Appointments a LEFT JOIN Doctors d ON d.Id = a.DoctorId
WHERE d.Id IS NULL
UNION ALL
SELECT 'OrphanBillsMissingPatient', COUNT(*)
FROM Bills b LEFT JOIN Patients p ON p.Id = b.PatientId
WHERE p.Id IS NULL
UNION ALL
SELECT 'OrphanBillItemsMissingBill', COUNT(*)
FROM BillItems bi LEFT JOIN Bills b ON b.Id = bi.BillId
WHERE b.Id IS NULL
UNION ALL
SELECT 'OrphanPaymentsMissingBill', COUNT(*)
FROM Payments p LEFT JOIN Bills b ON b.Id = p.BillId
WHERE b.Id IS NULL
UNION ALL
SELECT 'OrphanPublicRequestsMissingPatient', COUNT(*)
FROM PublicAppointmentRequests r LEFT JOIN Patients p ON p.Id = r.PatientId
WHERE p.Id IS NULL
UNION ALL
SELECT 'OrphanPublicRequestsMissingDoctor', COUNT(*)
FROM PublicAppointmentRequests r LEFT JOIN Doctors d ON d.Id = r.DoctorId
WHERE d.Id IS NULL
UNION ALL
SELECT 'BillsPaidGreaterThanTotal', COUNT(*)
FROM Bills
WHERE PaidAmount > TotalAmount
UNION ALL
SELECT 'BillsNegativePendingAmount', COUNT(*)
FROM Bills
WHERE PendingAmount < 0
UNION ALL
SELECT 'DuplicatePatientIds', COUNT(*)
FROM (
    SELECT PatientId
    FROM Patients
    WHERE PatientId IS NOT NULL AND LTRIM(RTRIM(PatientId)) <> ''
    GROUP BY PatientId
    HAVING COUNT(*) > 1
) x;
