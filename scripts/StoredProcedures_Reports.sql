-- ====================================================================
-- STORED PROCEDURES FOR OPTIMIZED REPORT GENERATION
-- Performance optimized for fast data retrieval with proper indexing
-- ====================================================================

-- ====================
-- DEPARTMENT REPORTS
-- ====================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_GetDepartmentReport')
    DROP PROCEDURE sp_GetDepartmentReport;
GO

CREATE PROCEDURE sp_GetDepartmentReport
    @StartDate DATETIME,
    @EndDate DATETIME,
    @DepartmentId NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StartTimeMs BIGINT = DATEDIFF(MILLISECOND, '1970-01-01', GETUTCDATE());

    SELECT 
        s.Id AS StaffId,
        s.FirstName + ' ' + s.LastName AS StaffName,
        s.Department,
        COUNT(DISTINCT CASE WHEN sa.Status = 'Present' THEN sa.Id END) AS PresentDays,
        COUNT(DISTINCT CASE WHEN sa.Status = 'Absent' THEN sa.Id END) AS AbsentDays,
        COUNT(DISTINCT CASE WHEN sa.Status = 'Leave' THEN sa.Id END) AS LeaveDays,
        COUNT(DISTINCT lr.Id) AS ApprovedLeaves,
        CAST(COUNT(DISTINCT CASE WHEN sa.Status = 'Present' THEN sa.Id END) AS DECIMAL(5,2)) / 
            NULLIF(COUNT(DISTINCT sa.Id), 0) * 100 AS AttendancePercentage
    FROM Staff s
    LEFT JOIN StaffAttendance sa ON s.Id = sa.StaffId 
        AND sa.AttendanceDate >= @StartDate 
        AND sa.AttendanceDate <= @EndDate
    LEFT JOIN LeaveRequest lr ON s.Id = lr.StaffId 
        AND lr.Status = 'Approved' 
        AND lr.StartDate >= @StartDate 
        AND lr.EndDate <= @EndDate
    WHERE (@DepartmentId IS NULL OR s.Id = @DepartmentId)
    GROUP BY s.Id, s.FirstName, s.LastName, s.Department
    ORDER BY s.FirstName, s.LastName;

    SET @StartTimeMs = DATEDIFF(MILLISECOND, '1970-01-01', GETUTCDATE()) - @StartTimeMs;
    SELECT @StartTimeMs AS ExecutionTimeMs;
END;
GO

-- ====================
-- FINANCIAL REPORTS
-- ====================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_GetFinancialReport')
    DROP PROCEDURE sp_GetFinancialReport;
GO

CREATE PROCEDURE sp_GetFinancialReport
    @StartDate DATETIME,
    @EndDate DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StartTimeMs BIGINT = DATEDIFF(MILLISECOND, '1970-01-01', GETUTCDATE());

    SELECT 
        CONVERT(DATE, pr.CreatedDate) AS TransactionDate,
        'Payroll' AS TransactionType,
        COUNT(*) AS TransactionCount,
        SUM(pr.NetSalary) AS Amount
    FROM PayrollRecords pr
    WHERE pr.CreatedDate >= @StartDate 
        AND pr.CreatedDate <= @EndDate 
        AND pr.Status = 'Paid'
    GROUP BY CONVERT(DATE, pr.CreatedDate)

    UNION ALL

    SELECT 
        CONVERT(DATE, b.BillDate) AS TransactionDate,
        'Bills' AS TransactionType,
        COUNT(*) AS TransactionCount,
        SUM(b.TotalAmount) AS Amount
    FROM Bills b
    WHERE b.BillDate >= @StartDate 
        AND b.BillDate <= @EndDate
    GROUP BY CONVERT(DATE, b.BillDate)

    UNION ALL

    SELECT 
        CONVERT(DATE, p.PaymentDate) AS TransactionDate,
        'Payments' AS TransactionType,
        COUNT(*) AS TransactionCount,
        SUM(p.Amount) AS Amount
    FROM Payments p
    WHERE p.PaymentDate >= @StartDate 
        AND p.PaymentDate <= @EndDate
    GROUP BY CONVERT(DATE, p.PaymentDate)
    ORDER BY TransactionDate DESC;

    SET @StartTimeMs = DATEDIFF(MILLISECOND, '1970-01-01', GETUTCDATE()) - @StartTimeMs;
    SELECT @StartTimeMs AS ExecutionTimeMs;
END;
GO

-- ====================
-- OCCUPANCY REPORTS
-- ====================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_GetOccupancyReport')
    DROP PROCEDURE sp_GetOccupancyReport;
GO

CREATE PROCEDURE sp_GetOccupancyReport
    @ReportDate DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartTimeMs BIGINT = DATEDIFF(MILLISECOND, '1970-01-01', GETUTCDATE());

    DECLARE @TotalBeds INT = (SELECT COUNT(*) FROM Beds WHERE IsActive = 1);
    DECLARE @OccupiedBeds INT = (
        SELECT COUNT(*) FROM IPDAdmissions 
        WHERE AdmissionDate <= @ReportDate 
            AND (DischargeDate IS NULL OR DischargeDate >= @ReportDate)
    );

    SELECT 
        @TotalBeds AS TotalBeds,
        @OccupiedBeds AS OccupiedBeds,
        @TotalBeds - @OccupiedBeds AS AvailableBeds,
        CAST(@OccupiedBeds AS DECIMAL(5,2)) / NULLIF(@TotalBeds, 0) * 100 AS OccupancyPercentage,
        w.WardName,
        COUNT(DISTINCT b.Id) AS WardBeds,
        COUNT(DISTINCT CASE WHEN ipa.DischargeDate IS NULL OR ipa.DischargeDate >= @ReportDate THEN ipa.Id END) AS OccupiedInWard
    FROM Beds b
    LEFT JOIN Ward w ON b.WardId = w.Id
    LEFT JOIN IPDAdmissions ipa ON b.Id = ipa.BedId 
        AND ipa.AdmissionDate <= @ReportDate 
        AND (ipa.DischargeDate IS NULL OR ipa.DischargeDate >= @ReportDate)
    WHERE b.IsActive = 1
    GROUP BY w.Id, w.WardName
    ORDER BY w.WardName;

    SET @StartTimeMs = DATEDIFF(MILLISECOND, '1970-01-01', GETUTCDATE()) - @StartTimeMs;
    SELECT @StartTimeMs AS ExecutionTimeMs;
END;
GO

-- ====================
-- PATIENT STATISTICS
-- ====================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_GetPatientStatistics')
    DROP PROCEDURE sp_GetPatientStatistics;
GO

CREATE PROCEDURE sp_GetPatientStatistics
    @StartDate DATETIME,
    @EndDate DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartTimeMs BIGINT = DATEDIFF(MILLISECOND, '1970-01-01', GETUTCDATE());

    SELECT 
        COUNT(*) AS TotalPatients,
        SUM(CASE WHEN Gender = 'Male' THEN 1 ELSE 0 END) AS MalePatients,
        SUM(CASE WHEN Gender = 'Female' THEN 1 ELSE 0 END) AS FemalePatients,
        AVG(DATEDIFF(YEAR, DateOfBirth, GETUTCDATE())) AS AverageAge,
        COUNT(DISTINCT CASE WHEN InsuranceId IS NOT NULL THEN Id END) AS InsuredPatients
    FROM Patient
    WHERE CreatedDate >= @StartDate AND CreatedDate <= @EndDate;

    SET @StartTimeMs = DATEDIFF(MILLISECOND, '1970-01-01', GETUTCDATE()) - @StartTimeMs;
    SELECT @StartTimeMs AS ExecutionTimeMs;
END;
GO

-- ====================
-- APPOINTMENT ANALYTICS
-- ====================

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sp_GetAppointmentAnalytics')
    DROP PROCEDURE sp_GetAppointmentAnalytics;
GO

CREATE PROCEDURE sp_GetAppointmentAnalytics
    @StartDate DATETIME,
    @EndDate DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartTimeMs BIGINT = DATEDIFF(MILLISECOND, '1970-01-01', GETUTCDATE());

    SELECT 
        'Total' AS Metric,
        COUNT(*) AS Value
    FROM Appointment
    WHERE AppointmentDate >= @StartDate AND AppointmentDate <= @EndDate

    UNION ALL

    SELECT 
        CONCAT('Status_', Status),
        COUNT(*)
    FROM Appointment
    WHERE AppointmentDate >= @StartDate AND AppointmentDate <= @EndDate
    GROUP BY Status

    UNION ALL

    SELECT 
        'NoShow_Rate',
        CAST(SUM(CASE WHEN Status = 'NoShow' THEN 1 ELSE 0 END) AS DECIMAL(5,2)) / 
            NULLIF(COUNT(*), 0) * 100
    FROM Appointment
    WHERE AppointmentDate >= @StartDate AND AppointmentDate <= @EndDate;

    SET @StartTimeMs = DATEDIFF(MILLISECOND, '1970-01-01', GETUTCDATE()) - @StartTimeMs;
    SELECT @StartTimeMs AS ExecutionTimeMs;
END;
GO

-- ====================
-- PERFORMANCE INDEXES
-- ====================

-- Create indexes for better report performance
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StaffAttendance_DateRange')
    CREATE NONCLUSTERED INDEX IX_StaffAttendance_DateRange 
    ON StaffAttendance(StaffId, AttendanceDate, Status);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LeaveRequest_DateRange')
    CREATE NONCLUSTERED INDEX IX_LeaveRequest_DateRange 
    ON LeaveRequest(StaffId, StartDate, EndDate, Status);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bills_DateRange')
    CREATE NONCLUSTERED INDEX IX_Bills_DateRange 
    ON Bills(BillDate, Status);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_DateRange')
    CREATE NONCLUSTERED INDEX IX_Payments_DateRange 
    ON Payments(PaymentDate, Status);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Appointment_DateRange')
    CREATE NONCLUSTERED INDEX IX_Appointment_DateRange 
    ON Appointment(AppointmentDate, Status);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IPDAdmissions_DateRange')
    CREATE NONCLUSTERED INDEX IX_IPDAdmissions_DateRange 
    ON IPDAdmissions(AdmissionDate, DischargeDate, BedId);
