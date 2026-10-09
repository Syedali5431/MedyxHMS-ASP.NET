-- ============================================================
--  MedyxHMS Demo Data Seed Script
--  Target:   SQL Server — database [MedyxHMS]
--  Source:   Adapted from hospitaldemo_db.sql (MariaDB 10.4.27)
--  Coverage: Departments, Doctors, Staff, Patients, Appointments,
--            OPD, IPD, Wards, Beds, Bills, Payments, Pharmacy,
--            Lab, Radiology, Blood Bank, Visitor Logs (Front Office),
--            Staff Attendance, Payroll, OT Schedules, Referrals,
--            the test account "tester" (section 26), three hospitals with
--            their own accounts and recent activity (sections 27-28),
--            inventory with vendors and purchase bills (section 29)
--            and the currency PKR (section 30)
--
--  Usage (runs in the database you connect to – any name):
--    sqlcmd -S .\SQLEXPRESS -E -b -d MedyxHMS -i SeedDemoData.sql
--    — or open in SSMS, select the MedyxHMS database and execute
--  Run New-Database.sql (or New-Database-Empty.sql) first.
--
--  NOTE: Existing rows with the same Id will be skipped (WHERE NOT EXISTS).
--        Intended for a new or demo database; safe to re-run.
--
--  NOT COVERED: birth & death records, quality, equipment – their tables are
--        created by New-Database.sql; add their data in the application.
--
--  Demo passwords: tester = Tester@123!, hospital accounts = Hospital@123!
--        (see scripts/README.md). Remove these accounts on a live system.
-- ============================================================


IF DB_NAME() IN (N'master', N'model', N'msdb', N'tempdb')
BEGIN
    RAISERROR(N'Run this script in the MedyxHMS database (sqlcmd -d MedyxHMS ...), not in %s.', 16, 1, N'a system database');
    SET NOEXEC ON;
END
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

-- Disable FK constraints so demo data can be inserted without user accounts
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
GO

-- Insert a dummy user for FK references (for demo/testing)
INSERT INTO [AspNetUsers] ([Id], [EmployeeId], [FirstName], [LastName], [IsActive], [CreatedDate], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount])
SELECT * FROM (VALUES
  ('demo-user', 'EMP-DEMO-001', 'Demo', 'User', 1, SYSUTCDATETIME(), 'demouser', 'DEMOUSER', 'demo@medyx.local', 'DEMO@MEDYX.LOCAL', 1, 'AQAAAAIAAYagAAAAEMS6w8CTrZcauhNbwdrOImwTUx8Prh5M77Q46lHBV5hXhOKDC32WFC/BuDx8W7hMxw==', 'DUMMYSECURITYSTAMP', 'DUMMYCONCURRENCY', NULL, 0, 0, NULL, 1, 0)
) AS src([Id], [EmployeeId], [FirstName], [LastName], [IsActive], [CreatedDate], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount])
WHERE NOT EXISTS (SELECT 1 FROM [AspNetUsers] u WHERE u.[Id] = 'demo-user');
GO

-- 1. DEPARTMENTS
-- ============================================================
SET IDENTITY_INSERT [Departments] ON;

INSERT INTO [Departments] ([Id],[Name],[Description],[HeadOfDepartment],[IsActive],[CreatedDate])
SELECT * FROM (VALUES
  (1, 'General Medicine',   'General outpatient and inpatient medicine',          'Dr. R. Sharma',    1, '2023-01-01'),
  (2, 'Surgery',            'General & specialty surgical services',              'Dr. P. Kumar',     1, '2023-01-01'),
  (3, 'Pediatrics',         'Medical care for infants, children and adolescents', 'Dr. S. Gupta',     1, '2023-01-01'),
  (4, 'Obstetrics & Gynecology', 'Women''s health, pregnancy and childbirth',     'Dr. A. Singh',     1, '2023-01-01'),
  (5, 'Cardiology',         'Heart and cardiovascular diseases',                  'Dr. V. Patel',     1, '2023-01-01'),
  (6, 'Orthopedics',        'Bone, joint and musculoskeletal conditions',         'Dr. M. Joshi',     1, '2023-01-01'),
  (7, 'ENT',                'Ear, nose and throat disorders',                     'Dr. N. Verma',     1, '2023-01-01'),
  (8, 'Dermatology',        'Skin, hair and nail conditions',                     'Dr. L. Rao',       1, '2023-01-01'),
  (9, 'Ophthalmology',      'Eye care and vision disorders',                      'Dr. K. Mehta',     1, '2023-01-01'),
  (10,'Pathology & Lab',    'Laboratory diagnostics and pathology',               'Dr. D. Nair',      1, '2023-01-01'),
  (11,'Radiology',          'Diagnostic imaging and interventional radiology',    'Dr. T. Iyer',      1, '2023-01-01'),
  (12,'Pharmacy',           'Dispensing and pharmaceutical services',             'Mr. R. Das',       1, '2023-01-01')
) AS src([Id],[Name],[Description],[HeadOfDepartment],[IsActive],[CreatedDate])
WHERE NOT EXISTS (SELECT 1 FROM [Departments] d WHERE d.[Id] = src.[Id]);

SET IDENTITY_INSERT [Departments] OFF;
GO

-- ============================================================
-- 2. DOCTORS
-- ============================================================
SET IDENTITY_INSERT [Doctors] ON;

INSERT INTO [Doctors] ([Id],[EmployeeId],[FirstName],[LastName],[Specialization],[LicenseNumber],[Phone],[Email],[DepartmentId],[IsActive],[CreatedDate])
SELECT * FROM (VALUES
  (1,  'DOC001', 'Rajesh',   'Sharma',   'General Medicine',        'MCI-001234', '9810001001', 'r.sharma@medyx.local',   1,  1, '2023-01-10'),
  (2,  'DOC002', 'Pradeep',  'Kumar',    'General Surgery',         'MCI-001235', '9810001002', 'p.kumar@medyx.local',    2,  1, '2023-01-10'),
  (3,  'DOC003', 'Sunita',   'Gupta',    'Pediatrics',              'MCI-001236', '9810001003', 's.gupta@medyx.local',    3,  1, '2023-01-10'),
  (4,  'DOC004', 'Anita',    'Singh',    'Obstetrics & Gynecology', 'MCI-001237', '9810001004', 'a.singh@medyx.local',    4,  1, '2023-01-10'),
  (5,  'DOC005', 'Vikram',   'Patel',    'Cardiology',              'MCI-001238', '9810001005', 'v.patel@medyx.local',    5,  1, '2023-01-10'),
  (6,  'DOC006', 'Mohan',    'Joshi',    'Orthopedics',             'MCI-001239', '9810001006', 'm.joshi@medyx.local',    6,  1, '2023-01-10'),
  (7,  'DOC007', 'Naresh',   'Verma',    'ENT',                     'MCI-001240', '9810001007', 'n.verma@medyx.local',    7,  1, '2023-01-10'),
  (8,  'DOC008', 'Lalita',   'Rao',      'Dermatology',             'MCI-001241', '9810001008', 'l.rao@medyx.local',      8,  1, '2023-01-10'),
  (9,  'DOC009', 'Kavita',   'Mehta',    'Ophthalmology',           'MCI-001242', '9810001009', 'k.mehta@medyx.local',    9,  1, '2023-01-10'),
  (10, 'DOC010', 'Deepak',   'Nair',     'Pathology',               'MCI-001243', '9810001010', 'd.nair@medyx.local',     10, 1, '2023-01-10')
) AS src([Id],[EmployeeId],[FirstName],[LastName],[Specialization],[LicenseNumber],[Phone],[Email],[DepartmentId],[IsActive],[CreatedDate])
WHERE NOT EXISTS (SELECT 1 FROM [Doctors] d WHERE d.[Id] = src.[Id]);

SET IDENTITY_INSERT [Doctors] OFF;
GO

-- ============================================================
-- 3. STAFF  (need Staff before Appointments — FK DoctorId maps to Doctors but StaffId in other tables references Staff)
-- ============================================================
INSERT INTO [Staff] ([Id],[EmployeeId],[FirstName],[LastName],[Department],[Designation],[DateOfJoining],[Salary],[Phone],[Address],[Email],[About],[IsActive],[CreatedDate])
SELECT * FROM (VALUES
  ('DEMO-STF-001', 'STF001', 'Ritu',   'Sharma', 'General Medicine', 'Receptionist',   '2022-03-01', 22000.00, '9820001001', '12 Hospital Road', 'ritu.s@medyx.local',   '', 1, '2022-03-01'),
  ('DEMO-STF-002', 'STF002', 'Arun',   'Tiwari', 'Pathology & Lab',  'Lab Technician', '2022-03-01', 28000.00, '9820001002', '15 Lab Lane',      'arun.t@medyx.local',   '', 1, '2022-03-01'),
  ('DEMO-STF-003', 'STF003', 'Pooja',  'Kapoor', 'General Medicine', 'Nurse',          '2022-04-01', 25000.00, '9820001003', '8 Nursing Home',   'pooja.k@medyx.local',  '', 1, '2022-04-01'),
  ('DEMO-STF-004', 'STF004', 'Suresh', 'Pandey', 'Pharmacy',         'Pharmacist',     '2022-04-01', 30000.00, '9820001004', '3 Pharmacy Block', 'suresh.p@medyx.local', '', 1, '2022-04-01'),
  ('DEMO-STF-005', 'STF005', 'Meena',  'Dixit',  'Pediatrics',       'Nurse',          '2022-05-01', 25000.00, '9820001005', '7 Ward Street',    'meena.d@medyx.local',  '', 1, '2022-05-01')
) AS src([Id],[EmployeeId],[FirstName],[LastName],[Department],[Designation],[DateOfJoining],[Salary],[Phone],[Address],[Email],[About],[IsActive],[CreatedDate])
WHERE NOT EXISTS (SELECT 1 FROM [Staff] s WHERE s.[Id] = src.[Id]);
GO

-- ============================================================
-- 4. PATIENTS
-- ============================================================
SET IDENTITY_INSERT [Patients] ON;

-- Demo patients have no portal login (UserId NULL); portal accounts are created in the application.
INSERT INTO [Patients] ([Id],[PatientId],[FirstName],[LastName],[Email],[Phone],[DateOfBirth],[Gender],[Address],[City],[State],[Country],[PostalCode],[BloodGroup],[EmergencyContactName],[EmergencyContactPhone],[EmergencyContactRelation],[MedicalHistory],[Allergies],[GuardianName],[GuardianPhone],[MaritalStatus],[Occupation],[UserId],[ProfileImagePath],[IsActive],[CreatedDate],[LastVisitDate])
SELECT * FROM (VALUES
  (1,  'PAT00001', 'Amit',       'Sharma',    'amit.sharma@mail.com',    '9811001001', '1985-06-15', 'Male',   '12 MG Road',         'Delhi',      'Delhi',           'India', '110001', 'B+',  'Priya Sharma',   '9811001099', 'Wife',    'Hypertension',     'Penicillin',  '',              '',           'Married',  'Engineer',  NULL, '', 1, '2024-01-10', '2025-03-15'),
  (2,  'PAT00002', 'Sunita',     'Verma',     'sunita.v@mail.com',       '9811001002', '1990-08-22', 'Female', '45 Civil Lines',      'Mumbai',     'Maharashtra',     'India', '400001', 'A+',  'Raj Verma',      '9811001098', 'Husband', 'Diabetes T2',      'Sulfa',       '',              '',           'Married',  'Teacher',   NULL, '', 1, '2024-01-12', '2025-03-20'),
  (3,  'PAT00003', 'Rohan',      'Singh',     'rohan.s@mail.com',        '9811001003', '2005-03-10', 'Male',   '8 Gandhi Nagar',      'Jaipur',     'Rajasthan',       'India', '302001', 'O+',  'Kiran Singh',    '9811001097', 'Mother',  '',                 '',            'Kiran Singh',  '9811001097', 'Single',   'Student',   NULL, '', 1, '2024-01-15', '2025-02-28'),
  (4,  'PAT00004', 'Priya',      'Gupta',     'priya.g@mail.com',        '9811001004', '1978-11-30', 'Female', '23 Nehru Place',      'Lucknow',    'Uttar Pradesh',   'India', '226001', 'AB+', 'Suresh Gupta',   '9811001096', 'Husband', 'Thyroid disorder', '',            '',              '',           'Married',  'Homemaker', NULL, '', 1, '2024-01-18', '2025-04-01'),
  (5,  'PAT00005', 'Vikram',     'Patel',     'vikram.p@mail.com',       '9811001005', '1965-05-05', 'Male',   '67 Subhash Chowk',   'Ahmedabad',  'Gujarat',         'India', '380001', 'O-',  'Neeta Patel',    '9811001095', 'Wife',    'CAD, Hypertension','Aspirin',     '',              '',           'Married',  'Business',  NULL, '', 1, '2024-01-20', '2025-04-05'),
  (6,  'PAT00006', 'Deepa',      'Nair',      'deepa.n@mail.com',        '9811001006', '1995-07-19', 'Female', '15 West Park',        'Bangalore',  'Karnataka',       'India', '560001', 'B-',  'Rajan Nair',     '9811001094', 'Father',  '',                 '',            '',              '',           'Single',   'Software',  NULL, '', 1, '2024-01-22', '2025-03-10'),
  (7,  'PAT00007', 'Manish',     'Kumar',     'manish.k@mail.com',       '9811001007', '1980-12-01', 'Male',   '3 Race Course Road',  'Chennai',    'Tamil Nadu',      'India', '600001', 'A-',  'Suman Kumar',    '9811001093', 'Wife',    'Asthma',           'NSAIDs',      '',              '',           'Married',  'Driver',    NULL, '', 1, '2024-01-25', '2025-02-20'),
  (8,  'PAT00008', 'Kavita',     'Joshi',     'kavita.j@mail.com',       '9811001008', '1972-09-14', 'Female', '101 Green Park',      'Pune',       'Maharashtra',     'India', '411001', 'AB-', 'Mohan Joshi',    '9811001092', 'Husband', 'Osteoporosis',     '',            '',              '',           'Married',  'Nurse',     NULL, '', 1, '2024-01-28', '2025-04-10'),
  (9,  'PAT00009', 'Arjun',      'Reddy',     'arjun.r@mail.com',        '9811001009', '1999-04-25', 'Male',   '22 Banjara Hills',    'Hyderabad',  'Telangana',       'India', '500001', 'B+',  'Sudha Reddy',    '9811001091', 'Mother',  '',                 '',            '',              '',           'Single',   'Student',   NULL, '', 1, '2024-02-01', '2025-03-28'),
  (10, 'PAT00010', 'Rekha',      'Mishra',    'rekha.m@mail.com',        '9811001010', '1988-02-17', 'Female', '55 Ashok Vihar',      'Kolkata',    'West Bengal',     'India', '700001', 'O+',  'Dinesh Mishra',  '9811001090', 'Husband', 'PCOS',             '',            '',              '',           'Married',  'Accountant',NULL, '', 1, '2024-02-03', '2025-04-02'),
  (11, 'PAT00011', 'Suresh',     'Yadav',     'suresh.y@mail.com',       '9811001011', '1958-10-08', 'Male',   '9 Vikas Puri',        'Delhi',      'Delhi',           'India', '110018', 'A+',  'Kamla Yadav',    '9811001089', 'Wife',    'COPD, Diabetes',   'Iodine',      '',              '',           'Married',  'Retired',   NULL, '', 1, '2024-02-05', '2025-04-08'),
  (12, 'PAT00012', 'Nisha',      'Agarwal',   'nisha.a@mail.com',        '9811001012', '1993-06-30', 'Female', '77 MG Marg',          'Allahabad',  'Uttar Pradesh',   'India', '211001', 'B+',  'Ashok Agarwal',  '9811001088', 'Father',  '',                 '',            '',              '',           'Single',   'Designer',  NULL, '', 1, '2024-02-08', '2025-03-05'),
  (13, 'PAT00013', 'Rahul',      'Saxena',    'rahul.sx@mail.com',       '9811001013', '2015-01-20', 'Male',   '30 Sector 12',        'Noida',      'Uttar Pradesh',   'India', '201301', 'O+',  'Anita Saxena',   '9811001087', 'Mother',  '',                 '',            'Anita Saxena', '9811001087', 'Single',   'Student',   NULL, '', 1, '2024-02-10', '2025-02-15'),
  (14, 'PAT00014', 'Anita',      'Chaudhary', 'anita.c@mail.com',        '9811001014', '1983-08-11', 'Female', '14 Rajpur Road',      'Dehradun',   'Uttarakhand',     'India', '248001', 'A+',  'Ravi Chaudhary', '9811001086', 'Husband', 'Migraine',         '',            '',              '',           'Married',  'Teacher',   NULL, '', 1, '2024-02-12', '2025-03-22'),
  (15, 'PAT00015', 'Balram',     'Singh',     'balram.si@mail.com',      '9811001015', '1950-03-25', 'Male',   '88 Cantonment',       'Kanpur',     'Uttar Pradesh',   'India', '208004', 'B-',  'Geeta Singh',    '9811001085', 'Wife',    'Heart Failure',    'Warfarin',    '',              '',           'Married',  'Retired',   NULL, '', 1, '2024-02-15', '2025-04-12'),
  (16, 'PAT00016', 'Shweta',     'Tiwari',    'shweta.t@mail.com',       '9811001016', '2000-11-11', 'Female', '5 New Colony',        'Bhopal',     'Madhya Pradesh',  'India', '462001', 'AB+', 'Anil Tiwari',    '9811001084', 'Father',  '',                 '',            '',              '',           'Single',   'Student',   NULL, '', 1, '2024-02-18', '2025-03-18'),
  (17, 'PAT00017', 'Dinesh',     'Pandey',    'dinesh.p@mail.com',       '9811001017', '1975-07-04', 'Male',   '18 Tilak Nagar',      'Nagpur',     'Maharashtra',     'India', '440001', 'O-',  'Savita Pandey',  '9811001083', 'Wife',    'Kidney Stones',    '',            '',              '',           'Married',  'Mechanic',  NULL, '', 1, '2024-02-20', '2025-04-03'),
  (18, 'PAT00018', 'Meena',      'Srivastava','meena.sr@mail.com',       '9811001018', '1969-09-28', 'Female', '42 Alambagh',         'Lucknow',    'Uttar Pradesh',   'India', '226005', 'A-',  'Ramesh Srivastava','9811001082','Husband', 'Rheumatoid Arthritis','Methotrexate','',           '',           'Married',  'Homemaker', NULL, '', 1, '2024-02-22', '2025-04-07'),
  (19, 'PAT00019', 'Harish',     'Malhotra',  'harish.m@mail.com',       '9811001019', '1991-04-17', 'Male',   '11 Defence Colony',   'Delhi',      'Delhi',           'India', '110024', 'B+',  'Shashi Malhotra','9811001081', 'Mother',  '',                 '',            '',              '',           'Single',   'CA',        NULL, '', 1, '2024-03-01', '2025-03-30'),
  (20, 'PAT00020', 'Geeta',      'Bhatt',     'geeta.b@mail.com',        '9811001020', '1960-12-22', 'Female', '6 Ram Nagar',         'Varanasi',   'Uttar Pradesh',   'India', '221001', 'O+',  'Suresh Bhatt',   '9811001080', 'Husband', 'Type 1 Diabetes',  'Latex',       '',              '',           'Married',  'Homemaker', NULL, '', 1, '2024-03-05', '2025-04-11')
) AS src([Id],[PatientId],[FirstName],[LastName],[Email],[Phone],[DateOfBirth],[Gender],[Address],[City],[State],[Country],[PostalCode],[BloodGroup],[EmergencyContactName],[EmergencyContactPhone],[EmergencyContactRelation],[MedicalHistory],[Allergies],[GuardianName],[GuardianPhone],[MaritalStatus],[Occupation],[UserId],[ProfileImagePath],[IsActive],[CreatedDate],[LastVisitDate])
WHERE NOT EXISTS (SELECT 1 FROM [Patients] p WHERE p.[Id] = src.[Id]);

SET IDENTITY_INSERT [Patients] OFF;
GO

-- ============================================================
-- 5. APPOINTMENTS
-- ============================================================
SET IDENTITY_INSERT [Appointments] ON;

INSERT INTO [Appointments] ([Id],[PatientId],[DoctorId],[StaffId],[AppointmentDate],[AppointmentTime],[Status],[AppointmentType],[Priority],[Symptoms],[Notes],[CreatedDate],[CreatedBy],[UpdatedDate],[UpdatedBy])
SELECT [Id],[PatientId],[DoctorId],[StaffId],[AppointmentDate],[AppointmentTime],[Status],[AppointmentType],[Priority],[Symptoms],[Notes],[CreatedDate],[CreatedBy],[UpdatedDate],[UpdatedBy] FROM (VALUES
  (1,  1,  1,  1, '1', '2025-04-01', '09:00:00', 'Completed',  'OPD',          'Normal',   'Headache, fever',               '',  '2025-03-28', 'ritu.s', NULL, ''),
  (2,  2,  2,  1, '1', '2025-04-01', '09:30:00', 'Completed',  'Follow-up',    'Normal',   'Diabetes check',                '',  '2025-03-28', 'ritu.s', NULL, ''),
  (3,  3,  3,  3, '3', '2025-04-02', '10:00:00', 'Completed',  'OPD',          'Normal',   'Cough and cold',                '',  '2025-03-29', 'ritu.s', NULL, ''),
  (4,  4,  5,  5, '5', '2025-04-03', '11:00:00', 'Completed',  'Consultation', 'Urgent',   'Chest pain, palpitations',      '',  '2025-03-30', 'ritu.s', NULL, ''),
  (5,  5,  7,  1, '1', '2025-04-03', '14:00:00', 'Completed',  'OPD',          'Normal',   'Breathlessness',                '',  '2025-03-30', 'ritu.s', NULL, ''),
  (6,  6,  8,  6, '6', '2025-04-04', '09:00:00', 'Completed',  'OPD',          'Normal',   'Knee pain',                     '',  '2025-03-31', 'ritu.s', NULL, ''),
  (7,  7,  11, 1, '1', '2025-04-05', '10:30:00', 'Completed',  'Follow-up',    'Normal',   'COPD review',                   '',  '2025-04-01', 'ritu.s', NULL, ''),
  (8,  8,  15, 5, '5', '2025-04-07', '11:00:00', 'Completed',  'Consultation', 'Emergency','Sudden chest pain',             '',  '2025-04-05', 'ritu.s', NULL, ''),
  (9,  9,  4,  1, '1', '2025-04-08', '09:30:00', 'Scheduled',  'Follow-up',    'Normal',   'Thyroid follow-up',             '',  '2025-04-03', 'ritu.s', NULL, ''),
  (10, 10, 6,  8, '8', '2025-04-08', '10:00:00', 'Scheduled',  'OPD',          'Normal',   'Skin rash',                     '',  '2025-04-04', 'ritu.s', NULL, ''),
  (11, 11, 9,  6, '6', '2025-04-09', '09:00:00', 'Scheduled',  'OPD',          'Normal',   'Sports injury - knee',          '',  '2025-04-05', 'ritu.s', NULL, ''),
  (12, 12, 12, 4, '4', '2025-04-09', '11:00:00', 'Scheduled',  'OPD',          'Normal',   'Irregular periods',             '',  '2025-04-05', 'ritu.s', NULL, ''),
  (13, 13, 14, 1, '1', '2025-04-10', '09:00:00', 'Scheduled',  'OPD',          'Normal',   'Migraine episode',              '',  '2025-04-06', 'ritu.s', NULL, ''),
  (14, 14, 17, 1, '1', '2025-04-10', '09:30:00', 'Scheduled',  'Follow-up',    'Normal',   'Kidney stone follow-up',        '',  '2025-04-06', 'ritu.s', NULL, ''),
  (15, 15, 20, 1, '1', '2025-04-11', '10:00:00', 'Scheduled',  'Follow-up',    'Normal',   'Diabetes management',           '',  '2025-04-07', 'ritu.s', NULL, '')
) AS src([Id],[AppointmentId],[PatientId],[DoctorId],[StaffId],[AppointmentDate],[AppointmentTime],[Status],[AppointmentType],[Priority],[Symptoms],[Notes],[CreatedDate],[CreatedBy],[UpdatedDate],[UpdatedBy])
WHERE NOT EXISTS (SELECT 1 FROM [Appointments] a WHERE a.[Id] = src.[Id]);

SET IDENTITY_INSERT [Appointments] OFF;
GO

-- ============================================================
-- 6. OPD VISITS
-- ============================================================
SET IDENTITY_INSERT [OPDVisits] ON;

INSERT INTO [OPDVisits] ([Id],[PatientId],[DoctorId],[VisitDate],[Symptoms],[Diagnosis],[Treatment],[Prescription],[Notes],[ConsultationFee],[PaymentStatus],[CreatedDate],[CreatedBy])
SELECT * FROM (VALUES
  (1,  1,  1, '2025-04-01', 'Headache, fever for 3 days',            'Viral fever',          'Rest, ORS, paracetamol',    'Paracetamol 500mg TDS x 5 days',           '', 300.00, 'Paid',    '2025-04-01', 'r.sharma'),
  (2,  2,  1, '2025-04-01', 'Elevated fasting glucose, fatigue',     'Type 2 Diabetes',      'Metformin, diet counsel',   'Metformin 500mg BD x 30 days',             '', 300.00, 'Paid',    '2025-04-01', 'r.sharma'),
  (3,  3,  3, '2025-04-02', 'Cough, cold, mild fever',               'URTI',                 'Syrup, antihistamine',      'Amoxicillin 250mg TDS x 5 days',           '', 250.00, 'Paid',    '2025-04-02', 's.gupta'),
  (4,  5,  5, '2025-04-03', 'Chest pain, breathlessness on exertion','Stable Angina',        'ECG, nitrate, beta-blocker','Isosorbide 5mg SOS, Atenolol 50mg OD',     '', 500.00, 'Paid',    '2025-04-03', 'v.patel'),
  (5,  7,  1, '2025-04-03', 'Breathlessness, wheezing',              'Bronchial Asthma',     'Salbutamol inhaler, steroid','Salbutamol MDI 2 puffs BD',                '', 300.00, 'Paid',    '2025-04-03', 'r.sharma'),
  (6,  8,  6, '2025-04-04', 'Right knee pain for 2 weeks',           'OA Knee',              'Physiotherapy, NSAIDs',     'Diclofenac 50mg BD after food x 10 days',  '', 400.00, 'Paid',    '2025-04-04', 'm.joshi'),
  (7,  11, 1, '2025-04-05', 'Worsening breathlessness',              'Exacerbation of COPD', 'Nebulisation, IV steroids', 'Budesonide 200mcg BD, salbutamol nebulise', '', 300.00, 'Pending', '2025-04-05', 'r.sharma'),
  (8,  6,  8, '2025-04-08', 'Itchy rash on arms and neck',           'Allergic Dermatitis',  'Cetirizine, calamine lotion','Cetirizine 10mg OD x 7 days',              '', 350.00, 'Paid',    '2025-04-08', 'l.rao'),
  (9,  14, 1, '2025-04-10', 'Severe migraine headache',              'Migraine',             'Sumatriptan SOS, prophylaxis','Sumatriptan 50mg SOS',                     '', 300.00, 'Paid',    '2025-04-10', 'r.sharma'),
  (10, 20, 1, '2025-04-11', 'HbA1c elevated at 9.2%',               'Uncontrolled DM1',     'Insulin adjustment',        'Insulin Glargine 20U HS',                  '', 300.00, 'Paid',    '2025-04-11', 'r.sharma')
) AS src([Id],[PatientId],[DoctorId],[VisitDate],[Symptoms],[Diagnosis],[Treatment],[Prescription],[Notes],[ConsultationFee],[PaymentStatus],[CreatedDate],[CreatedBy])
WHERE NOT EXISTS (SELECT 1 FROM [OPDVisits] o WHERE o.[Id] = src.[Id]);

SET IDENTITY_INSERT [OPDVisits] OFF;
GO

-- ============================================================
-- 7. WARDS
-- ============================================================
SET IDENTITY_INSERT [Wards] ON;

INSERT INTO [Wards] ([Id],[Name],[Description],[TotalBeds],[OccupiedBeds],[IsActive],[CreatedDate])
SELECT * FROM (VALUES
  (1, 'General Male Ward',     'General inpatient ward for male patients',      20, 8, 1, '2023-01-01'),
  (2, 'General Female Ward',   'General inpatient ward for female patients',    20, 6, 1, '2023-01-01'),
  (3, 'ICU',                   'Intensive Care Unit',                           10, 4, 1, '2023-01-01'),
  (4, 'Maternity Ward',        'Labour, delivery and postnatal care',           12, 3, 1, '2023-01-01'),
  (5, 'Pediatric Ward',        'Inpatient care for children under 14',          10, 2, 1, '2023-01-01'),
  (6, 'Private Rooms',         'Single-occupancy private rooms',                15, 5, 1, '2023-01-01')
) AS src([Id],[Name],[Description],[TotalBeds],[OccupiedBeds],[IsActive],[CreatedDate])
WHERE NOT EXISTS (SELECT 1 FROM [Wards] w WHERE w.[Id] = src.[Id]);

SET IDENTITY_INSERT [Wards] OFF;
GO

-- ============================================================
-- 8. BEDS
-- ============================================================
SET IDENTITY_INSERT [Beds] ON;

INSERT INTO [Beds] ([Id],[WardId],[BedNumber],[BedType],[DailyCharges],[Status],[IsActive],[CreatedDate])
SELECT * FROM (VALUES
  (1,  1, 'GM-01', 'General',      500.00,  'Available', 1, '2023-01-01'),
  (2,  1, 'GM-02', 'General',      500.00,  'Occupied',  1, '2023-01-01'),
  (3,  1, 'GM-03', 'General',      500.00,  'Occupied',  1, '2023-01-01'),
  (4,  2, 'GF-01', 'General',      500.00,  'Occupied',  1, '2023-01-01'),
  (5,  2, 'GF-02', 'General',      500.00,  'Available', 1, '2023-01-01'),
  (6,  3, 'ICU-01','ICU',         2500.00,  'Occupied',  1, '2023-01-01'),
  (7,  3, 'ICU-02','ICU',         2500.00,  'Occupied',  1, '2023-01-01'),
  (8,  3, 'ICU-03','ICU',         2500.00,  'Available', 1, '2023-01-01'),
  (9,  4, 'MT-01', 'Semi-private', 1200.00, 'Occupied',  1, '2023-01-01'),
  (10, 6, 'PVT-01','Private',     3000.00,  'Occupied',  1, '2023-01-01'),
  (11, 6, 'PVT-02','Private',     3000.00,  'Available', 1, '2023-01-01')
) AS src([Id],[WardId],[BedNumber],[BedType],[DailyCharges],[Status],[IsActive],[CreatedDate])
WHERE NOT EXISTS (SELECT 1 FROM [Beds] b WHERE b.[Id] = src.[Id]);

SET IDENTITY_INSERT [Beds] OFF;
GO

-- ============================================================
-- 9. IPD ADMISSIONS
-- ============================================================
SET IDENTITY_INSERT [IPDAdmissions] ON;

INSERT INTO [IPDAdmissions] ([Id],[PatientId],[DoctorId],[BedId],[AdmissionDate],[DischargeDate],[AdmissionType],[Diagnosis],[Treatment],[Notes],[Status],[DailyCharges],[CreatedDate],[CreatedBy])
SELECT * FROM (VALUES
  (1, 5,  5, 6,  '2025-03-25', NULL,         'Emergency', 'NSTEMI',             'Angioplasty, heparin drip, CCU care',     '', 'Admitted',   2500.00, '2025-03-25', 'r.sharma'),
  (2, 11, 1, 2,  '2025-03-28', '2025-04-02', 'Emergency', 'COPD Exacerbation',  'Nebulisation, O2 therapy, IV steroids',   '', 'Discharged', 500.00,  '2025-03-28', 'r.sharma'),
  (3, 15, 5, 7,  '2025-04-07', NULL,         'Emergency', 'CHF Exacerbation',   'IV furosemide, monitoring, echo',          '', 'Admitted',   2500.00, '2025-04-07', 'v.patel'),
  (4, 8,  6, 4,  '2025-04-04', '2025-04-08', 'Planned',   'Knee Replacement',   'Right TKR, physio post-op',               '', 'Discharged', 500.00,  '2025-04-04', 'm.joshi'),
  (5, 18, 1, 10, '2025-04-06', NULL,         'Planned',   'RA Flare',           'IV methylprednisolone, biologic review',   '', 'Admitted',   3000.00, '2025-04-06', 'r.sharma')
) AS src([Id],[PatientId],[DoctorId],[BedId],[AdmissionDate],[DischargeDate],[AdmissionType],[Diagnosis],[Treatment],[Notes],[Status],[DailyCharges],[CreatedDate],[CreatedBy])
WHERE NOT EXISTS (SELECT 1 FROM [IPDAdmissions] i WHERE i.[Id] = src.[Id]);

SET IDENTITY_INSERT [IPDAdmissions] OFF;
GO

-- ============================================================
-- 10. BILLS
-- ============================================================
SET IDENTITY_INSERT [Bills] ON;

INSERT INTO [Bills] ([Id],[BillNumber],[PatientId],[AppointmentId],[BillDate],[DueDate],[TotalAmount],[PaidAmount],[PendingAmount],[Status],[BillType],[Notes],[CreatedDate],[UpdatedDate],[CreatedBy])
SELECT * FROM (VALUES
  (1,  'BILL-2025-0001', 1,  1,  '2025-04-01', '2025-04-15', 300.00,   300.00,  0.00,     'Paid',            'OPD',       '', '2025-04-01', NULL, 'ritu.s'),
  (2,  'BILL-2025-0002', 2,  2,  '2025-04-01', '2025-04-15', 300.00,   300.00,  0.00,     'Paid',            'OPD',       '', '2025-04-01', NULL, 'ritu.s'),
  (3,  'BILL-2025-0003', 3,  3,  '2025-04-02', '2025-04-16', 250.00,   250.00,  0.00,     'Paid',            'OPD',       '', '2025-04-02', NULL, 'ritu.s'),
  (4,  'BILL-2025-0004', 5,  4,  '2025-04-03', '2025-04-17', 5500.00,  2000.00, 3500.00,  'Partially Paid',  'IPD',       '', '2025-04-03', NULL, 'ritu.s'),
  (5,  'BILL-2025-0005', 7,  5,  '2025-04-03', '2025-04-17', 300.00,   300.00,  0.00,     'Paid',            'OPD',       '', '2025-04-03', NULL, 'ritu.s'),
  (6,  'BILL-2025-0006', 8,  6,  '2025-04-04', '2025-04-18', 12400.00, 12400.00,0.00,     'Paid',            'IPD',       '', '2025-04-04', NULL, 'ritu.s'),
  (7,  'BILL-2025-0007', 11, 7,  '2025-04-05', '2025-04-19', 300.00,   0.00,    300.00,   'Unpaid',          'OPD',       '', '2025-04-05', NULL, 'ritu.s'),
  (8,  'BILL-2025-0008', 15, 8,  '2025-04-07', '2025-04-21', 7500.00,  0.00,    7500.00,  'Unpaid',          'IPD',       '', '2025-04-07', NULL, 'ritu.s'),
  (9,  'BILL-2025-0009', 6,  10, '2025-04-08', '2025-04-22', 350.00,   350.00,  0.00,     'Paid',            'OPD',       '', '2025-04-08', NULL, 'ritu.s'),
  (10, 'BILL-2025-0010', 18, NULL,'2025-04-06', '2025-04-20', 9000.00,  5000.00, 4000.00,  'Partially Paid',  'IPD',       '', '2025-04-06', NULL, 'ritu.s')
) AS src([Id],[BillNumber],[PatientId],[AppointmentId],[BillDate],[DueDate],[TotalAmount],[PaidAmount],[PendingAmount],[Status],[BillType],[Notes],[CreatedDate],[UpdatedDate],[CreatedBy])
WHERE NOT EXISTS (SELECT 1 FROM [Bills] b WHERE b.[Id] = src.[Id]);

SET IDENTITY_INSERT [Bills] OFF;
GO

-- ============================================================
-- 11. BILL ITEMS
-- ============================================================
SET IDENTITY_INSERT [BillItems] ON;

INSERT INTO [BillItems] ([Id],[BillId],[ItemName],[ItemType],[Quantity],[UnitPrice],[TotalPrice],[Amount],[Description],[CreatedDate])
SELECT * FROM (VALUES
  (1,  1,  'Consultation Fee',           'Service', 1, 300.00,  300.00,  300.00,  '', '2025-04-01'),
  (2,  2,  'Consultation Fee',           'Service', 1, 300.00,  300.00,  300.00,  '', '2025-04-01'),
  (3,  3,  'Consultation Fee',           'Service', 1, 250.00,  250.00,  250.00,  '', '2025-04-02'),
  (4,  4,  'Consultation Fee',           'Service', 1, 500.00,  500.00,  500.00,  '', '2025-04-03'),
  (5,  4,  'ICU Bed Charges (2 days)',   'Bed',     2, 2500.00, 5000.00, 5000.00, '', '2025-04-03'),
  (6,  5,  'Consultation Fee',           'Service', 1, 300.00,  300.00,  300.00,  '', '2025-04-03'),
  (7,  6,  'Consultation Fee',           'Service', 1, 400.00,  400.00,  400.00,  '', '2025-04-04'),
  (8,  6,  'General Bed Charges (4 days)','Bed',    4, 500.00,  2000.00, 2000.00, '', '2025-04-04'),
  (9,  6,  'Theatre Charges',            'Service', 1, 8000.00, 8000.00, 8000.00, '', '2025-04-04'),
  (10, 6,  'Physiotherapy',              'Service', 2, 1000.00, 2000.00, 2000.00, '', '2025-04-04'),
  (11, 7,  'Consultation Fee',           'Service', 1, 300.00,  300.00,  300.00,  '', '2025-04-05'),
  (12, 8,  'Consultation Fee',           'Service', 1, 500.00,  500.00,  500.00,  '', '2025-04-07'),
  (13, 8,  'ICU Bed Charges (3 days)',   'Bed',     3, 2500.00, 7500.00, 7500.00, '', '2025-04-07'),
  (14, 9,  'Consultation Fee',           'Service', 1, 350.00,  350.00,  350.00,  '', '2025-04-08'),
  (15, 10, 'Consultation Fee',           'Service', 1, 300.00,  300.00,  300.00,  '', '2025-04-06'),
  (16, 10, 'Private Room (3 days)',      'Bed',     3, 3000.00, 9000.00, 9000.00, '', '2025-04-06')
) AS src([Id],[BillId],[ItemName],[ItemType],[Quantity],[UnitPrice],[TotalPrice],[Amount],[Description],[CreatedDate])
WHERE NOT EXISTS (SELECT 1 FROM [BillItems] bi WHERE bi.[Id] = src.[Id]);

SET IDENTITY_INSERT [BillItems] OFF;
GO

-- ============================================================
-- 12. PAYMENTS
-- ============================================================
SET IDENTITY_INSERT [Payments] ON;

INSERT INTO [Payments] ([Id],[BillId],[PaymentMethod],[Amount],[TransactionId],[PaymentGateway],[Status],[Notes],[PaymentDate],[ProcessedBy])
SELECT * FROM (VALUES
  (1,  1,  'Cash',  300.00,   'TXN-20250401-001', '', 'Completed', '', '2025-04-01', 'ritu.s'),
  (2,  2,  'Cash',  300.00,   'TXN-20250401-002', '', 'Completed', '', '2025-04-01', 'ritu.s'),
  (3,  3,  'Cash',  250.00,   'TXN-20250402-001', '', 'Completed', '', '2025-04-02', 'ritu.s'),
  (4,  4,  'Card',  2000.00,  'TXN-20250403-001', '', 'Completed', '', '2025-04-03', 'ritu.s'),
  (5,  5,  'Cash',  300.00,   'TXN-20250403-002', '', 'Completed', '', '2025-04-03', 'ritu.s'),
  (6,  6,  'Insurance', 12400.00,'TXN-20250408-001','','Completed','', '2025-04-08', 'ritu.s'),
  (7,  9,  'Cash',  350.00,   'TXN-20250408-002', '', 'Completed', '', '2025-04-08', 'ritu.s'),
  (8,  10, 'Cash',  5000.00,  'TXN-20250406-001', '', 'Completed', '', '2025-04-06', 'ritu.s')
) AS src([Id],[BillId],[PaymentMethod],[Amount],[TransactionId],[PaymentGateway],[Status],[Notes],[PaymentDate],[ProcessedBy])
WHERE NOT EXISTS (SELECT 1 FROM [Payments] p WHERE p.[Id] = src.[Id]);

SET IDENTITY_INSERT [Payments] OFF;
GO

-- ============================================================
-- 13. MEDICINES
-- ============================================================
SET IDENTITY_INSERT [Medicines] ON;

INSERT INTO [Medicines] ([Id],[Name],[GenericName],[Category],[DosageForm],[Strength],[Manufacturer],[UnitPrice],[StockQuantity],[MinStockLevel],[ExpiryDate],[BatchNumber],[IsActive],[CreatedDate])
SELECT * FROM (VALUES
  (1,  'Calpol 500',       'Paracetamol',        'Analgesic/Antipyretic', 'Tablet',  '500mg',   'GSK',        3.50,   2000, 200, '2026-12-31', 'GSK-2024-001', 1, '2024-01-01'),
  (2,  'Glycomet 500',     'Metformin HCl',      'Antidiabetic',          'Tablet',  '500mg',   'USV',        7.00,   1500, 150, '2026-06-30', 'USV-2024-002', 1, '2024-01-01'),
  (3,  'Amoxil 250',       'Amoxicillin',        'Antibiotic',            'Syrup',   '250mg/5ml','Cipla',     22.00,  500,  50,  '2025-09-30', 'CIP-2024-003', 1, '2024-01-01'),
  (4,  'Sorbitrate 5',     'Isosorbide Dinitrate','Antianginal',          'Tablet',  '5mg',     'Abbott',     4.00,   800,  80,  '2026-03-31', 'ABT-2024-004', 1, '2024-01-01'),
  (5,  'Atenolol 50',      'Atenolol',           'Beta-Blocker',          'Tablet',  '50mg',    'Cipla',      2.50,   1200, 120, '2026-12-31', 'CIP-2024-005', 1, '2024-01-01'),
  (6,  'Salbutamol MDI',   'Salbutamol',         'Bronchodilator',        'Inhaler', '100mcg/puff','GSK',    120.00, 200,  20,  '2025-12-31', 'GSK-2024-006', 1, '2024-01-01'),
  (7,  'Voveran 50',       'Diclofenac Sodium',  'NSAID',                 'Tablet',  '50mg',    'Novartis',   5.00,   1000, 100, '2026-03-31', 'NOV-2024-007', 1, '2024-01-01'),
  (8,  'Zyrtec 10',        'Cetirizine HCl',     'Antihistamine',         'Tablet',  '10mg',    'UCB',        6.00,   800,  80,  '2026-12-31', 'UCB-2024-008', 1, '2024-01-01'),
  (9,  'Sumatriptan 50',   'Sumatriptan',        'Antimigraine',          'Tablet',  '50mg',    'Sun Pharma', 45.00,  200,  20,  '2026-06-30', 'SUN-2024-009', 1, '2024-01-01'),
  (10, 'Lantus 10ml',      'Insulin Glargine',   'Insulin',               'Injection','100U/ml','Sanofi',    380.00, 150,  30,  '2025-10-31', 'SNF-2024-010', 1, '2024-01-01'),
  (11, 'Calamine Lotion',  'Calamine',           'Dermatological',        'Lotion',  'Standard','Piramal',   55.00,  300,  30,  '2026-12-31', 'PIR-2024-011', 1, '2024-01-01'),
  (12, 'ORS Sachet',       'Oral Rehydration Salts','Electrolyte',        'Powder',  'Standard','Cipla',      8.00,   500,  50,  '2027-01-31', 'CIP-2024-012', 1, '2024-01-01')
) AS src([Id],[Name],[GenericName],[Category],[DosageForm],[Strength],[Manufacturer],[UnitPrice],[StockQuantity],[MinStockLevel],[ExpiryDate],[BatchNumber],[IsActive],[CreatedDate])
WHERE NOT EXISTS (SELECT 1 FROM [Medicines] m WHERE m.[Id] = src.[Id]);

SET IDENTITY_INSERT [Medicines] OFF;
GO

-- ============================================================
-- 14. PHARMACY BILLS
-- ============================================================
SET IDENTITY_INSERT [PharmacyBills] ON;

INSERT INTO [PharmacyBills] ([Id],[BillNumber],[PatientId],[BillDate],[TotalAmount],[PaidAmount],[Status],[PaymentMethod],[Notes],[CreatedDate],[CreatedBy])
SELECT * FROM (VALUES
  (1, 'RXBILL-2025-0001', 1,  '2025-04-01', 52.50,  52.50,  'Paid',      'Cash', '', '2025-04-01', 'suresh.p'),
  (2, 'RXBILL-2025-0002', 2,  '2025-04-01', 280.00, 280.00, 'Paid',      'Cash', '', '2025-04-01', 'suresh.p'),
  (3, 'RXBILL-2025-0003', 3,  '2025-04-02', 22.00,  22.00,  'Paid',      'Cash', '', '2025-04-02', 'suresh.p'),
  (4, 'RXBILL-2025-0004', 5,  '2025-04-03', 620.00, 620.00, 'Paid',      'Card', '', '2025-04-03', 'suresh.p'),
  (5, 'RXBILL-2025-0005', 7,  '2025-04-03', 120.00, 0.00,   'Pending',   'Cash', '', '2025-04-03', 'suresh.p'),
  (6, 'RXBILL-2025-0006', 8,  '2025-04-04', 50.00,  50.00,  'Paid',      'Cash', '', '2025-04-04', 'suresh.p'),
  (7, 'RXBILL-2025-0007', 6,  '2025-04-08', 42.00,  42.00,  'Paid',      'Cash', '', '2025-04-08', 'suresh.p'),
  (8, 'RXBILL-2025-0008', 20, '2025-04-11', 380.00, 380.00, 'Paid',      'Card', '', '2025-04-11', 'suresh.p')
) AS src([Id],[BillNumber],[PatientId],[BillDate],[TotalAmount],[PaidAmount],[Status],[PaymentMethod],[Notes],[CreatedDate],[CreatedBy])
WHERE NOT EXISTS (SELECT 1 FROM [PharmacyBills] pb WHERE pb.[Id] = src.[Id]);

SET IDENTITY_INSERT [PharmacyBills] OFF;
GO

-- ============================================================
-- 15. PRESCRIPTIONS (Pharmacy bill line items)
-- ============================================================
SET IDENTITY_INSERT [Prescriptions] ON;

INSERT INTO [Prescriptions] ([Id],[PharmacyBillId],[MedicineId],[Dosage],[Frequency],[Duration],[Quantity],[UnitPrice],[TotalPrice],[Instructions],[CreatedDate])
SELECT * FROM (VALUES
  (1,  1, 1,  '500mg', 'TDS',        5,  15, 3.50,  52.50,  'After food',       '2025-04-01'),
  (2,  2, 2,  '500mg', 'BD',         30, 60, 7.00,  420.00, 'After food',       '2025-04-01'),
  (3,  2, 12, 'Standard','TDS',      3,  9,  8.00,  72.00,  'As needed',        '2025-04-01'),
  (4,  3, 3,  '250mg/5ml','TDS',     5,  1,  22.00, 22.00,  'Shake before use', '2025-04-02'),
  (5,  4, 4,  '5mg',  'SOS',         7,  7,  4.00,  28.00,  'Under tongue',     '2025-04-03'),
  (6,  4, 5,  '50mg', 'OD',          30, 30, 2.50,  75.00,  'Morning',          '2025-04-03'),
  (7,  4, 10, '100U/ml','HS',        30, 3,  380.00,1140.00,'SC injection',     '2025-04-03'),
  (8,  5, 6,  '100mcg','BD',         30, 1,  120.00,120.00, '2 puffs BD',       '2025-04-03'),
  (9,  6, 7,  '50mg', 'BD',          10, 20, 5.00,  100.00, 'After food',       '2025-04-04'),
  (10, 7, 8,  '10mg', 'OD',          7,  7,  6.00,  42.00,  'Night',            '2025-04-08'),
  (11, 8, 10, '100U/ml','HS',        30, 1,  380.00,380.00, 'SC injection HS',  '2025-04-11')
) AS src([Id],[PharmacyBillId],[MedicineId],[Dosage],[Frequency],[Duration],[Quantity],[UnitPrice],[TotalPrice],[Instructions],[CreatedDate])
WHERE NOT EXISTS (SELECT 1 FROM [Prescriptions] px WHERE px.[Id] = src.[Id]);

SET IDENTITY_INSERT [Prescriptions] OFF;
GO

-- ============================================================
-- 16. LAB TESTS (catalogue)
-- ============================================================
SET IDENTITY_INSERT [LabTests] ON;

INSERT INTO [LabTests] ([Id],[TestName],[TestCode],[Category],[Description],[Price],[NormalRange],[Unit],[PreparationTimeHours],[IsActive],[CreatedDate])
SELECT * FROM (VALUES
  (1,  'Complete Blood Count',      'CBC',        'Hematology',    'Full blood count panel',                           350.00, 'See report', '',      4,  1, '2023-01-01'),
  (2,  'Fasting Blood Sugar',       'FBS',        'Biochemistry',  'Glucose level after 8h fast',                      80.00,  '70–100',    'mg/dL', 2,  1, '2023-01-01'),
  (3,  'HbA1c',                     'HBA1C',      'Biochemistry',  'Glycated haemoglobin — 3-month glucose average',   350.00, '4.0–5.6',   '%',     4,  1, '2023-01-01'),
  (4,  'Lipid Profile',             'LIPID',      'Biochemistry',  'Cholesterol, triglycerides, HDL, LDL',             500.00, 'See report', '',      4,  1, '2023-01-01'),
  (5,  'Thyroid Function Test',     'TFT',        'Endocrinology', 'TSH, T3, T4',                                      600.00, 'See report', '',      6,  1, '2023-01-01'),
  (6,  'Liver Function Test',       'LFT',        'Biochemistry',  'ALT, AST, ALP, bilirubin, albumin',               550.00, 'See report', '',      6,  1, '2023-01-01'),
  (7,  'Renal Function Test',       'RFT',        'Biochemistry',  'Urea, creatinine, uric acid, electrolytes',        450.00, 'See report', '',      4,  1, '2023-01-01'),
  (8,  'Urine Routine',             'URINE-R',    'Microbiology',  'Urine microscopy and culture',                     150.00, 'See report', '',      2,  1, '2023-01-01'),
  (9,  'ECG',                       'ECG',        'Cardiology',    '12-lead electrocardiogram',                        200.00, 'Normal sinus rhythm','',1,1, '2023-01-01'),
  (10, 'Sputum Culture',            'SPUTUM',     'Microbiology',  'Culture and sensitivity for respiratory pathogens',400.00, 'No growth',  '',      48, 1, '2023-01-01')
) AS src([Id],[TestName],[TestCode],[Category],[Description],[Price],[NormalRange],[Unit],[PreparationTimeHours],[IsActive],[CreatedDate])
WHERE NOT EXISTS (SELECT 1 FROM [LabTests] lt WHERE lt.[Id] = src.[Id]);

SET IDENTITY_INSERT [LabTests] OFF;
GO

-- ============================================================
-- 17. LAB RESULTS
-- ============================================================
SET IDENTITY_INSERT [LabResults] ON;

INSERT INTO [LabResults] ([Id],[PatientId],[LabTestId],[OrderNumber],[OrderDate],[ResultDate],[ResultValue],[NormalRange],[Unit],[Interpretation],[Status],[PerformedBy],[VerifiedBy],[Notes],[CreatedDate])
SELECT * FROM (VALUES
  (1,  1,  1,  'LAB-2025-0001', '2025-04-01', '2025-04-01', 'Hb:11.5, WBC:9800, Plt:220000', 'See report','', 'Low Hb — mild anaemia',  'Completed', 'arun.t', 'd.nair', '', '2025-04-01'),
  (2,  2,  2,  'LAB-2025-0002', '2025-04-01', '2025-04-01', '186',                            '70–100',    'mg/dL','High',               'Completed', 'arun.t', 'd.nair', '', '2025-04-01'),
  (3,  2,  3,  'LAB-2025-0003', '2025-04-01', '2025-04-02', '9.2',                            '4.0–5.6',   '%','High — Poor control',    'Completed', 'arun.t', 'd.nair', '', '2025-04-01'),
  (4,  4,  5,  'LAB-2025-0004', '2025-04-08', '2025-04-09', 'TSH:4.8, T3:0.9, T4:7.2',       'See report','', 'Sub-clinical hypothyroid','Completed', 'arun.t', 'd.nair', '', '2025-04-08'),
  (5,  5,  4,  'LAB-2025-0005', '2025-04-03', '2025-04-04', 'TC:242, LDL:165, HDL:38, TG:196','See report','','Dyslipidaemia',          'Completed', 'arun.t', 'd.nair', '', '2025-04-03'),
  (6,  5,  9,  'LAB-2025-0006', '2025-04-03', '2025-04-03', 'ST depression in V4-V6',         'NSR',       '', 'Abnormal',               'Completed', 'arun.t', 'd.nair', '', '2025-04-03'),
  (7,  11, 10, 'LAB-2025-0007', '2025-04-05', '2025-04-07', 'H. influenzae — sensitive to amoxicillin','No growth','','Positive',        'Completed', 'arun.t', 'd.nair', '', '2025-04-05'),
  (8,  17, 7,  'LAB-2025-0008', '2025-04-10', '2025-04-10', 'Creatinine:1.8, Urea:52',        'See report','','High creatinine',        'Completed', 'arun.t', 'd.nair', '', '2025-04-10'),
  (9,  20, 2,  'LAB-2025-0009', '2025-04-11', '2025-04-11', '298',                            '70–100',    'mg/dL','High',               'Completed', 'arun.t', 'd.nair', '', '2025-04-11'),
  (10, 20, 3,  'LAB-2025-0010', '2025-04-11', '2025-04-12', '10.4',                           '4.0–5.6',   '%','High — Poor control',    'Completed', 'arun.t', 'd.nair', '', '2025-04-11')
) AS src([Id],[PatientId],[LabTestId],[OrderNumber],[OrderDate],[ResultDate],[ResultValue],[NormalRange],[Unit],[Interpretation],[Status],[PerformedBy],[VerifiedBy],[Notes],[CreatedDate])
WHERE NOT EXISTS (SELECT 1 FROM [LabResults] lr WHERE lr.[Id] = src.[Id]);

SET IDENTITY_INSERT [LabResults] OFF;
GO

-- ============================================================
-- 18. RADIOLOGY TESTS (catalogue)
-- ============================================================
SET IDENTITY_INSERT [RadiologyTests] ON;

INSERT INTO [RadiologyTests] ([Id],[TestName],[TestCode],[Category],[Description],[Price],[PreparationTimeHours],[SpecialInstructions],[RequiresContrast],[IsActive],[CreatedDate])
SELECT * FROM (VALUES
  (1,  'Chest X-Ray PA',           'CXR-PA',    'X-Ray',       'Postero-anterior chest radiograph',                   300.00, 1, 'Remove metal objects',  0, 1, '2023-01-01'),
  (2,  'X-Ray Knee AP/Lateral',    'XR-KNEE',   'X-Ray',       'Knee joint radiograph',                               350.00, 1, '',                       0, 1, '2023-01-01'),
  (3,  'USG Abdomen & Pelvis',     'USG-ABD',   'Ultrasound',  'Abdominal and pelvic ultrasonography',                600.00, 4, 'Full bladder required',  0, 1, '2023-01-01'),
  (4,  'CT Chest',                 'CT-CHEST',  'CT Scan',     'Computed tomography of chest',                       2500.00, 1, '',                       0, 1, '2023-01-01'),
  (5,  'Echocardiogram',           'ECHO',      'Ultrasound',  '2D and Doppler echocardiography',                    1500.00, 1, '',                       0, 1, '2023-01-01'),
  (6,  'MRI Knee',                 'MRI-KNEE',  'MRI',         'Magnetic resonance imaging of knee joint',           3500.00, 2, 'No metal implants',      0, 1, '2023-01-01'),
  (7,  'CT KUB',                   'CT-KUB',    'CT Scan',     'CT of kidneys, ureters and bladder',                 2000.00, 1, '',                       0, 1, '2023-01-01')
) AS src([Id],[TestName],[TestCode],[Category],[Description],[Price],[PreparationTimeHours],[SpecialInstructions],[RequiresContrast],[IsActive],[CreatedDate])
WHERE NOT EXISTS (SELECT 1 FROM [RadiologyTests] rt WHERE rt.[Id] = src.[Id]);

SET IDENTITY_INSERT [RadiologyTests] OFF;
GO

-- ============================================================
-- 19. RADIOLOGY RESULTS
-- ============================================================
SET IDENTITY_INSERT [RadiologyResults] ON;

INSERT INTO [RadiologyResults] ([Id],[PatientId],[RadiologyTestId],[OrderNumber],[OrderDate],[ResultDate],[Findings],[Impression],[Status],[PerformedBy],[VerifiedBy],[ImagePath],[Notes],[CreatedDate])
SELECT * FROM (VALUES
  (1, 7,  1, 'RAD-2025-0001', '2025-04-03', '2025-04-03', 'Hyperinflated lung fields, flattened diaphragm',        'Features consistent with COPD',        'Completed', 't.iyer', 't.iyer', '', '', '2025-04-03'),
  (2, 5,  5, 'RAD-2025-0002', '2025-04-03', '2025-04-04', 'EF 35%, regional wall motion abnormality inferior wall','LV dysfunction — NSTEMI',              'Completed', 't.iyer', 't.iyer', '', '', '2025-04-03'),
  (3, 8,  2, 'RAD-2025-0003', '2025-04-04', '2025-04-04', 'Joint space narrowing medial compartment, osteophytes',  'OA right knee — Grade 3',              'Completed', 't.iyer', 't.iyer', '', '', '2025-04-04'),
  (4, 11, 1, 'RAD-2025-0004', '2025-04-05', '2025-04-05', 'Hyperinflated lungs, increased peribronchial markings', 'COPD with infective exacerbation',     'Completed', 't.iyer', 't.iyer', '', '', '2025-04-05'),
  (5, 17, 7, 'RAD-2025-0005', '2025-04-10', '2025-04-10', '8mm calculus right ureter at VUJ, mild hydronephrosis', 'Right ureteric calculus with HN',      'Completed', 't.iyer', 't.iyer', '', '', '2025-04-10')
) AS src([Id],[PatientId],[RadiologyTestId],[OrderNumber],[OrderDate],[ResultDate],[Findings],[Impression],[Status],[PerformedBy],[VerifiedBy],[ImagePath],[Notes],[CreatedDate])
WHERE NOT EXISTS (SELECT 1 FROM [RadiologyResults] rr WHERE rr.[Id] = src.[Id]);

SET IDENTITY_INSERT [RadiologyResults] OFF;
GO

-- ============================================================
-- 20. BLOOD INVENTORIES
-- ============================================================
SET IDENTITY_INSERT [BloodInventories] ON;

INSERT INTO [BloodInventories] ([Id],[BloodGroup],[UnitsAvailable],[UnitsReserved],[MinimumLevel],[LastUpdatedDate],[CreatedDate])
SELECT * FROM (VALUES
  (1, 'A+',  25, 3, 5, '2025-04-11', '2023-01-01'),
  (2, 'A-',   8, 1, 5, '2025-04-11', '2023-01-01'),
  (3, 'B+',  30, 4, 5, '2025-04-11', '2023-01-01'),
  (4, 'B-',   6, 0, 5, '2025-04-11', '2023-01-01'),
  (5, 'AB+', 12, 2, 5, '2025-04-11', '2023-01-01'),
  (6, 'AB-',  4, 0, 5, '2025-04-11', '2023-01-01'),
  (7, 'O+',  35, 5, 5, '2025-04-11', '2023-01-01'),
  (8, 'O-',  10, 2, 5, '2025-04-11', '2023-01-01')
) AS src([Id],[BloodGroup],[UnitsAvailable],[UnitsReserved],[MinimumLevel],[LastUpdatedDate],[CreatedDate])
WHERE NOT EXISTS (SELECT 1 FROM [BloodInventories] bi WHERE bi.[Id] = src.[Id]);

SET IDENTITY_INSERT [BloodInventories] OFF;
GO

-- ============================================================
-- 21. VISITOR LOGS (Front Office)
-- ============================================================
SET IDENTITY_INSERT [VisitorLogs] ON;

INSERT INTO [VisitorLogs] ([Id],[VisitorName],[Phone],[Purpose],[PersonToMeet],[VisitDate],[CheckInTime],[CheckOutTime],[Status],[Notes],[CreatedDate])
SELECT * FROM (VALUES
  (1, 'Ramesh Bhatt',   '9300000001', 'Patient enquiry',      'Front Desk',              '2025-04-08', '2025-04-08 09:15:00', '2025-04-08 09:30:00', 'CheckedOut', '', '2025-04-08'),
  (2, 'Sarita Kohli',   '9300000002', 'Bill payment',         'Billing Desk',            '2025-04-08', '2025-04-08 10:05:00', '2025-04-08 10:20:00', 'CheckedOut', '', '2025-04-08'),
  (3, 'Anil Kapoor',    '9300000003', 'Meeting doctor',       'Dr. Vikram Patel',        '2025-04-09', '2025-04-09 11:00:00', '2025-04-09 11:45:00', 'CheckedOut', '', '2025-04-09'),
  (4, 'Neha Sinha',     '9300000004', 'Document collection',  'Records Office',          '2025-04-09', '2025-04-09 14:20:00', NULL,                  'CheckedIn',  '', '2025-04-09'),
  (5, 'Suresh Iyer',    '9300000005', 'Appointment booking',  'Front Desk',              '2025-04-10', '2025-04-10 09:40:00', '2025-04-10 09:55:00', 'CheckedOut', '', '2025-04-10'),
  (6, 'Kavita Rane',    '9300000006', 'Delivery',             'Pharmacy',                '2025-04-10', '2025-04-10 16:10:00', '2025-04-10 16:20:00', 'CheckedOut', '', '2025-04-10'),
  (7, 'Deepak Malhotra','9300000007', 'Patient enquiry',      'Front Desk',              '2025-04-11', '2025-04-11 08:50:00', NULL,                  'CheckedIn',  '', '2025-04-11')
) AS src([Id],[VisitorName],[Phone],[Purpose],[PersonToMeet],[VisitDate],[CheckInTime],[CheckOutTime],[Status],[Notes],[CreatedDate])
WHERE NOT EXISTS (SELECT 1 FROM [VisitorLogs] v WHERE v.[Id] = src.[Id]);

SET IDENTITY_INSERT [VisitorLogs] OFF;
GO

-- ============================================================
-- 22. STAFF ATTENDANCE
-- ============================================================
SET IDENTITY_INSERT [StaffAttendances] ON;

INSERT INTO [StaffAttendances] ([Id],[StaffId],[AttendanceDate],[CheckInTime],[CheckOutTime],[Status],[Notes],[CreatedDate])
SELECT * FROM (VALUES
  (1,  'DEMO-STF-001', '2025-04-07', '2025-04-07 09:00:00', '2025-04-07 18:00:00', 'Present', '', '2025-04-07'),
  (2,  'DEMO-STF-001', '2025-04-08', '2025-04-08 09:05:00', '2025-04-08 18:00:00', 'Present', '', '2025-04-08'),
  (3,  'DEMO-STF-001', '2025-04-09', NULL,                   NULL,                  'Absent',  '', '2025-04-09'),
  (4,  'DEMO-STF-002', '2025-04-07', '2025-04-07 08:55:00', '2025-04-07 17:50:00', 'Present', '', '2025-04-07'),
  (5,  'DEMO-STF-002', '2025-04-08', '2025-04-08 09:00:00', '2025-04-08 13:00:00', 'HalfDay', '', '2025-04-08'),
  (6,  'DEMO-STF-002', '2025-04-09', '2025-04-09 08:58:00', '2025-04-09 17:55:00', 'Present', '', '2025-04-09'),
  (7,  'DEMO-STF-003', '2025-04-07', '2025-04-07 09:10:00', '2025-04-07 18:05:00', 'Present', '', '2025-04-07'),
  (8,  'DEMO-STF-003', '2025-04-08', '2025-04-08 09:02:00', '2025-04-08 18:00:00', 'Present', '', '2025-04-08'),
  (9,  'DEMO-STF-004', '2025-04-07', '2025-04-07 09:00:00', '2025-04-07 17:45:00', 'Present', '', '2025-04-07'),
  (10, 'DEMO-STF-004', '2025-04-08', '2025-04-08 09:00:00', '2025-04-08 17:50:00', 'Present', '', '2025-04-08'),
  (11, 'DEMO-STF-005', '2025-04-07', '2025-04-07 09:20:00', '2025-04-07 18:10:00', 'Present', '', '2025-04-07'),
  (12, 'DEMO-STF-005', '2025-04-08', NULL,                   NULL,                  'Absent',  '', '2025-04-08')
) AS src([Id],[StaffId],[AttendanceDate],[CheckInTime],[CheckOutTime],[Status],[Notes],[CreatedDate])
WHERE NOT EXISTS (SELECT 1 FROM [StaffAttendances] sa WHERE sa.[Id] = src.[Id]);

SET IDENTITY_INSERT [StaffAttendances] OFF;
GO

-- ============================================================
-- 23. PAYROLL RECORDS
-- ============================================================
SET IDENTITY_INSERT [PayrollRecords] ON;

INSERT INTO [PayrollRecords] ([Id],[StaffId],[PayrollMonth],[BasicSalary],[Allowances],[Deductions],[NetSalary],[Status],[PaymentDate],[Notes],[CreatedDate])
SELECT * FROM (VALUES
  (1, 'DEMO-STF-001', '2025-03-01', 22000.00, 2200.00, 1100.00, 23100.00, 'Paid',      '2025-04-05', '', '2025-03-01'),
  (2, 'DEMO-STF-001', '2025-04-01', 22000.00, 2200.00, 1100.00, 23100.00, 'Processed', NULL,         '', '2025-04-01'),
  (3, 'DEMO-STF-002', '2025-03-01', 28000.00, 2800.00, 1400.00, 29400.00, 'Paid',      '2025-04-05', '', '2025-03-01'),
  (4, 'DEMO-STF-002', '2025-04-01', 28000.00, 2800.00, 1400.00, 29400.00, 'Processed', NULL,         '', '2025-04-01'),
  (5, 'DEMO-STF-003', '2025-03-01', 25000.00, 2500.00, 1250.00, 26250.00, 'Paid',      '2025-04-05', '', '2025-03-01'),
  (6, 'DEMO-STF-004', '2025-03-01', 30000.00, 3000.00, 1500.00, 31500.00, 'Paid',      '2025-04-05', '', '2025-03-01'),
  (7, 'DEMO-STF-005', '2025-03-01', 25000.00, 2500.00, 1250.00, 26250.00, 'Paid',      '2025-04-05', '', '2025-03-01')
) AS src([Id],[StaffId],[PayrollMonth],[BasicSalary],[Allowances],[Deductions],[NetSalary],[Status],[PaymentDate],[Notes],[CreatedDate])
WHERE NOT EXISTS (SELECT 1 FROM [PayrollRecords] pr WHERE pr.[Id] = src.[Id]);

SET IDENTITY_INSERT [PayrollRecords] OFF;
GO

-- ============================================================
-- 24. OT SCHEDULES (Operation Theatre)
-- ============================================================
SET IDENTITY_INSERT [OTSchedules] ON;

INSERT INTO [OTSchedules] ([Id],[PatientId],[ProcedureName],[SurgeonName],[ScheduledDate],[EstimatedDurationMinutes],[OperationTheatreNumber],[Status],[Notes],[BillId],[CreatedDate])
SELECT * FROM (VALUES
  (1, 5,  'Cholecystectomy',   'Dr. Pradeep Kumar', '2025-04-06 09:00:00', 90,  'OT-1', 'Completed', '', NULL, '2025-04-05'),
  (2, 8,  'Knee Arthroscopy',  'Dr. Mohan Joshi',    '2025-04-07 10:30:00', 60,  'OT-2', 'Completed', '', NULL, '2025-04-06'),
  (3, 11, 'Hernia Repair',     'Dr. Pradeep Kumar',  '2025-04-14 09:00:00', 75,  'OT-1', 'Scheduled', '', NULL, '2025-04-10'),
  (4, 15, 'Cataract Surgery',  'Dr. Rajesh Sharma',  '2025-04-15 11:00:00', 45,  'OT-3', 'Scheduled', '', NULL, '2025-04-10')
) AS src([Id],[PatientId],[ProcedureName],[SurgeonName],[ScheduledDate],[EstimatedDurationMinutes],[OperationTheatreNumber],[Status],[Notes],[BillId],[CreatedDate])
WHERE NOT EXISTS (SELECT 1 FROM [OTSchedules] ot WHERE ot.[Id] = src.[Id]);

SET IDENTITY_INSERT [OTSchedules] OFF;
GO

-- ============================================================
-- 25. REFERRALS
-- ============================================================
SET IDENTITY_INSERT [Referrals] ON;

INSERT INTO [Referrals] ([Id],[PatientId],[ReferralType],[ReferredTo],[ReferralReason],[ReferralDate],[Status],[TpaProvider],[TpaPolicyNumber],[ApprovedAmount],[Notes],[BillId],[CreatedDate])
SELECT * FROM (VALUES
  (1, 5,  'External', 'City Cardiac Institute',        'Advanced cardiac workup required',  '2025-04-04', 'Approved', '',                      '',              NULL,     '', NULL, '2025-04-04'),
  (2, 11, 'Internal', 'Cardiology Department',         'Specialist consultation',           '2025-04-06', 'Pending',  '',                      '',              NULL,     '', NULL, '2025-04-06'),
  (3, 15, 'TPA',      'MediCare TPA Services',         'Insurance-approved procedure',      '2025-04-07', 'Approved', 'MediCare TPA Services', 'POL-2025-0001', 15000.00, '', NULL, '2025-04-07'),
  (4, 17, 'External', 'Nephrology Department',         'Dialysis planning',                 '2025-04-10', 'Completed','',                      '',              NULL,     '', NULL, '2025-04-10')
) AS src([Id],[PatientId],[ReferralType],[ReferredTo],[ReferralReason],[ReferralDate],[Status],[TpaProvider],[TpaPolicyNumber],[ApprovedAmount],[Notes],[BillId],[CreatedDate])
WHERE NOT EXISTS (SELECT 1 FROM [Referrals] r WHERE r.[Id] = src.[Id]);

SET IDENTITY_INSERT [Referrals] OFF;
GO

-- ============================================================
-- 26. TEST ACCOUNT – for testing the application only
--     User name: tester   Password: Tester@123!
--     Roles: every role except SuperAdmin (Admin, Doctor, Nurse, Staff, Receptionist, Pharmacist,
--            LabTechnician, Pathologist, Radiologist, Accountant). Patient is left out: the Patient
--            Portal needs a patient record – use patient.uat / a registered patient for the portal.
--     Two-step login: not asked – the account is listed under Security & Backups →
--            "Test accounts without two-step login" (setting Security:MfaExemptUserNames).
--     Remove the account and clear that setting on a live system.
-- ============================================================
DECLARE @TesterId nvarchar(128) = (SELECT TOP (1) [Id] FROM [AspNetUsers] WHERE [NormalizedUserName] = N'TESTER');
IF @TesterId IS NULL
BEGIN
    SET @TesterId = N'bdb56c6e-6450-423b-b469-6788aa831987';
    INSERT INTO [AspNetUsers] ([Id],[EmployeeId],[FirstName],[LastName],[IsActive],[CreatedDate],[MFAEnabled],[UserName],[NormalizedUserName],
                               [Email],[NormalizedEmail],[EmailConfirmed],[PasswordHash],[SecurityStamp],[ConcurrencyStamp],
                               [PhoneNumberConfirmed],[TwoFactorEnabled],[LockoutEnabled],[AccessFailedCount])
    VALUES (@TesterId, N'TEST-001', N'Test', N'User', 1, SYSDATETIME(), 0, N'tester', N'TESTER',
            N'tester@hospital.com', N'TESTER@HOSPITAL.COM', 1,
            N'AQAAAAIAAYagAAAAEIN2iyh3bs8pqkbdfHuuOjiz1+7pQQAkLBpG3kDj6t75gS3FBPPkki9AjQ//DWz/3Q==', N'4F05A4CB4018EC22E3750EFD4C3A309F', CONVERT(nvarchar(36), NEWID()),
            0, 0, 1, 0);
END
ELSE
    -- Existing account: keep it usable for testing (active, no two-step login, not locked out).
    UPDATE [AspNetUsers]
    SET [IsActive] = 1, [MFAEnabled] = 0, [MFASecretKey] = NULL, [MFATempSecret] = NULL, [MFARecoveryCodes] = NULL,
        [LockoutEnd] = NULL, [AccessFailedCount] = 0
    WHERE [Id] = @TesterId;

-- Sign-in roles (Identity).
INSERT INTO [AspNetUserRoles] ([UserId],[RoleId])
SELECT @TesterId, r.[Id]
FROM [AspNetRoles] r
WHERE r.[NormalizedName] NOT IN (N'SUPERADMIN', N'PATIENT')
  AND NOT EXISTS (SELECT 1 FROM [AspNetUserRoles] ur WHERE ur.[UserId] = @TesterId AND ur.[RoleId] = r.[Id]);

-- Staff profile and staff roles (the application keeps sign-in roles and staff roles in step).
IF NOT EXISTS (SELECT 1 FROM [Staff] WHERE [Id] = @TesterId)
    INSERT INTO [Staff] ([Id],[EmployeeId],[FirstName],[LastName],[Department],[Designation],[DateOfJoining],[Salary],
                         [Phone],[Address],[Email],[About],[IsActive],[CreatedDate],[UserId])
    VALUES (@TesterId, N'TEST-001', N'Test', N'User', N'Administration', N'Test user', SYSDATETIME(), 0,
            N'', N'', N'tester@hospital.com', N'Test account: every role except SuperAdmin, no two-step login.', 1, SYSDATETIME(), @TesterId);

INSERT INTO [StaffRoles] ([StaffId],[RoleId],[AssignedDate],[AssignedBy])
SELECT @TesterId, r.[Id], SYSDATETIME(), N'SeedDemoData.sql'
FROM [Roles] r
WHERE r.[Name] NOT IN (N'SuperAdmin', N'Patient')
  AND NOT EXISTS (SELECT 1 FROM [StaffRoles] sr WHERE sr.[StaffId] = @TesterId AND sr.[RoleId] = r.[Id]);

-- No two-step login for this account (Security & Backups → "Test accounts without two-step login").
IF NOT EXISTS (SELECT 1 FROM [Settings] WHERE [Key] = N'Security:MfaExemptUserNames')
    INSERT INTO [Settings] ([Key],[Value],[Type],[Category],[Description],[IsSystem],[CreatedDate],[ModifiedBy])
    VALUES (N'Security:MfaExemptUserNames', N'tester', N'string', N'Security',
            N'Test accounts that may sign in without two-step login (testing only)', 1, SYSDATETIME(), N'SeedDemoData.sql');
ELSE
    UPDATE [Settings]
    SET [Value] = CASE WHEN LTRIM(RTRIM([Value])) = N'' THEN N'tester' ELSE [Value] + N', tester' END,
        [ModifiedDate] = SYSDATETIME(), [ModifiedBy] = N'SeedDemoData.sql'
    WHERE [Key] = N'Security:MfaExemptUserNames'
      AND (N',' + REPLACE([Value], N' ', N'') + N',') NOT LIKE N'%,tester,%';
GO

-- ============================================================
-- 27. HOSPITALS AND HOSPITAL ACCOUNTS (multi-hospital demo)
--     Main Hospital (MAIN, Lahore – the default), City General Hospital (ISB, Islamabad) and
--     Karachi Care Hospital (KHI, Karachi). Each hospital has its own accounts (password: Hospital@123!):
--       admin.<code>, doctor.<code>, nurse.<code>, reception.<code>, accounts.<code>, pharmacy.<code>, lab.<code>
--       (<code> = main, isb, khi). admin.region is an Admin for Islamabad AND Karachi (switches between them).
--     tester gets all three hospitals and chooses one on the sign-in page. SuperAdmin always sees every hospital.
--     The hospital admins are added to "Test accounts without two-step login" (testing only).
-- ============================================================
DECLARE @Today datetime2 = CAST(CAST(SYSDATETIME() AS date) AS datetime2);
DECLARE @HMain int = COALESCE((SELECT TOP (1) [Id] FROM [Hospitals] WHERE [Code] = N'MAIN'), (SELECT TOP (1) [Id] FROM [Hospitals] WHERE [IsDefault] = 1 ORDER BY [Id]));
DECLARE @HIsb int = (SELECT TOP (1) [Id] FROM [Hospitals] WHERE [Code] = N'ISB');
DECLARE @HKhi int = (SELECT TOP (1) [Id] FROM [Hospitals] WHERE [Code] = N'KHI');

-- Hospitals (the default hospital keeps its name; its empty address is filled in)
IF @HMain IS NULL
BEGIN
    INSERT INTO [Hospitals] ([Code],[Name],[Address],[City],[Phone],[Email],[LicenseNumber],[IsActive],[IsDefault],[CreatedDate])
    VALUES (N'MAIN', N'Main Hospital', N'12 Jail Road, Gulberg', N'Lahore', N'+92 42 3570 1000', N'info@mainhospital.pk', N'PHC-LHR-0001', 1, 1, SYSDATETIME());
    SET @HMain = SCOPE_IDENTITY();
END
ELSE
    UPDATE [Hospitals] SET [Address] = N'12 Jail Road, Gulberg', [City] = N'Lahore', [Phone] = N'+92 42 3570 1000',
                           [Email] = N'info@mainhospital.pk', [LicenseNumber] = N'PHC-LHR-0001', [UpdatedDate] = SYSDATETIME()
    WHERE [Id] = @HMain AND [Address] = N'' AND [City] = N'';

IF @HIsb IS NULL
BEGIN
    INSERT INTO [Hospitals] ([Code],[Name],[Address],[City],[Phone],[Email],[LicenseNumber],[IsActive],[IsDefault],[CreatedDate])
    VALUES (N'ISB', N'City General Hospital', N'Plot 5, G-8 Markaz', N'Islamabad', N'+92 51 225 4000', N'info@citygeneral.pk', N'ICT-HRA-0457', 1, 0, SYSDATETIME());
    SET @HIsb = SCOPE_IDENTITY();
END

IF @HKhi IS NULL
BEGIN
    INSERT INTO [Hospitals] ([Code],[Name],[Address],[City],[Phone],[Email],[LicenseNumber],[IsActive],[IsDefault],[CreatedDate])
    VALUES (N'KHI', N'Karachi Care Hospital', N'22-C Shahrah-e-Faisal', N'Karachi', N'+92 21 3453 2000', N'info@karachicare.pk', N'SHCC-KHI-1123', 1, 0, SYSDATETIME());
    SET @HKhi = SCOPE_IDENTITY();
END

-- Hospital accounts
CREATE TABLE #HospitalUsers ([UserName] nvarchar(256), [EmployeeId] nvarchar(50), [FirstName] nvarchar(100), [LastName] nvarchar(100),
                             [Role] nvarchar(50), [Designation] nvarchar(100), [Department] nvarchar(100), [Phone] nvarchar(30));
INSERT INTO #HospitalUsers VALUES
  (N'admin.main', N'MAIN-ADM-01', N'Ayesha', N'Siddiqui', N'Admin', N'Hospital Administrator', N'Administration', N'+92 310 1000000'),
  (N'doctor.main', N'MAIN-DOC-01', N'Imran', N'Qureshi', N'Doctor', N'Consultant Physician', N'General Medicine', N'+92 311 1007919'),
  (N'nurse.main', N'MAIN-NUR-01', N'Sana', N'Malik', N'Nurse', N'Staff Nurse', N'General Medicine', N'+92 312 1015838'),
  (N'reception.main', N'MAIN-REC-01', N'Bilal', N'Ahmed', N'Receptionist', N'Front Desk Officer', N'Front Office', N'+92 313 1023757'),
  (N'accounts.main', N'MAIN-ACC-01', N'Hina', N'Javed', N'Accountant', N'Accounts Officer', N'Finance', N'+92 314 1031676'),
  (N'pharmacy.main', N'MAIN-PHR-01', N'Usman', N'Tariq', N'Pharmacist', N'Pharmacist', N'Pharmacy', N'+92 315 1039595'),
  (N'lab.main', N'MAIN-LAB-01', N'Zainab', N'Raza', N'LabTechnician', N'Lab Technologist', N'Pathology & Lab', N'+92 316 1047514'),
  (N'admin.isb', N'ISB-ADM-01', N'Kamran', N'Haider', N'Admin', N'Hospital Administrator', N'Administration', N'+92 317 1055433'),
  (N'doctor.isb', N'ISB-DOC-01', N'Farah', N'Naqvi', N'Doctor', N'Consultant Cardiologist', N'Cardiology', N'+92 318 1063352'),
  (N'nurse.isb', N'ISB-NUR-01', N'Rabia', N'Khan', N'Nurse', N'Charge Nurse', N'Cardiology', N'+92 319 1071271'),
  (N'reception.isb', N'ISB-REC-01', N'Asad', N'Mehmood', N'Receptionist', N'Front Desk Officer', N'Front Office', N'+92 320 1079190'),
  (N'accounts.isb', N'ISB-ACC-01', N'Nadia', N'Butt', N'Accountant', N'Accounts Officer', N'Finance', N'+92 321 1087109'),
  (N'pharmacy.isb', N'ISB-PHR-01', N'Faisal', N'Iqbal', N'Pharmacist', N'Pharmacist', N'Pharmacy', N'+92 322 1095028'),
  (N'lab.isb', N'ISB-LAB-01', N'Saad', N'Akhtar', N'LabTechnician', N'Lab Technologist', N'Pathology & Lab', N'+92 323 1102947'),
  (N'admin.khi', N'KHI-ADM-01', N'Shazia', N'Memon', N'Admin', N'Hospital Administrator', N'Administration', N'+92 324 1110866'),
  (N'doctor.khi', N'KHI-DOC-01', N'Arif', N'Shaikh', N'Doctor', N'Consultant Gynaecologist', N'Obstetrics & Gynecology', N'+92 325 1118785'),
  (N'nurse.khi', N'KHI-NUR-01', N'Mehwish', N'Ali', N'Nurse', N'Midwife', N'Obstetrics & Gynecology', N'+92 326 1126704'),
  (N'reception.khi', N'KHI-REC-01', N'Danish', N'Siddiqui', N'Receptionist', N'Front Desk Officer', N'Front Office', N'+92 327 1134623'),
  (N'accounts.khi', N'KHI-ACC-01', N'Sadia', N'Rehman', N'Accountant', N'Accounts Officer', N'Finance', N'+92 328 1142542'),
  (N'pharmacy.khi', N'KHI-PHR-01', N'Waqas', N'Baig', N'Pharmacist', N'Pharmacist', N'Pharmacy', N'+92 329 1150461'),
  (N'lab.khi', N'KHI-LAB-01', N'Noman', N'Ansari', N'LabTechnician', N'Lab Technologist', N'Pathology & Lab', N'+92 330 1158380'),
  (N'admin.region', N'REG-ADM-01', N'Tariq', N'Mahmood', N'Admin', N'Regional Administrator', N'Administration', N'+92 331 1166299');

INSERT INTO [AspNetUsers] ([Id],[EmployeeId],[FirstName],[LastName],[IsActive],[CreatedDate],[MFAEnabled],[UserName],[NormalizedUserName],
                           [Email],[NormalizedEmail],[EmailConfirmed],[PasswordHash],[SecurityStamp],[ConcurrencyStamp],
                           [PhoneNumber],[PhoneNumberConfirmed],[TwoFactorEnabled],[LockoutEnabled],[AccessFailedCount])
SELECT CONVERT(nvarchar(36), NEWID()), h.[EmployeeId], h.[FirstName], h.[LastName], 1, SYSDATETIME(), 0, h.[UserName], UPPER(h.[UserName]),
       h.[UserName] + N'@medyxdemo.pk', UPPER(h.[UserName] + N'@medyxdemo.pk'), 1,
       N'AQAAAAIAAYagAAAAEMITHa26B0r7/2FkpbpCne1y4aZTaRhgnSC/oAh+wyYUjXZF+da/3kSuaafchLYU0w==', UPPER(REPLACE(CONVERT(nvarchar(36), NEWID()), N'-', N'')), CONVERT(nvarchar(36), NEWID()),
       h.[Phone], 0, 0, 1, 0
FROM #HospitalUsers h
WHERE NOT EXISTS (SELECT 1 FROM [AspNetUsers] u WHERE u.[NormalizedUserName] = UPPER(h.[UserName]));

INSERT INTO [AspNetUserRoles] ([UserId],[RoleId])
SELECT u.[Id], r.[Id]
FROM #HospitalUsers h
JOIN [AspNetUsers] u ON u.[NormalizedUserName] = UPPER(h.[UserName])
JOIN [AspNetRoles] r ON r.[NormalizedName] = UPPER(h.[Role])
WHERE NOT EXISTS (SELECT 1 FROM [AspNetUserRoles] ur WHERE ur.[UserId] = u.[Id] AND ur.[RoleId] = r.[Id]);

INSERT INTO [Staff] ([Id],[EmployeeId],[FirstName],[LastName],[Department],[Designation],[DateOfJoining],[Salary],[Phone],[Address],[Email],[About],[IsActive],[CreatedDate],[UserId])
SELECT u.[Id], h.[EmployeeId], h.[FirstName], h.[LastName], h.[Department], h.[Designation], DATEADD(MONTH, -18, @Today),
       CASE h.[Role] WHEN N'Admin' THEN 250000 WHEN N'Doctor' THEN 450000 WHEN N'Nurse' THEN 120000 WHEN N'Pharmacist' THEN 140000
                     WHEN N'LabTechnician' THEN 110000 WHEN N'Accountant' THEN 130000 ELSE 90000 END,
       h.[Phone], N'', u.[Email], N'Demo account (SeedDemoData.sql)', 1, SYSDATETIME(), u.[Id]
FROM #HospitalUsers h
JOIN [AspNetUsers] u ON u.[NormalizedUserName] = UPPER(h.[UserName])
WHERE NOT EXISTS (SELECT 1 FROM [Staff] s WHERE s.[Id] = u.[Id]);

INSERT INTO [StaffRoles] ([StaffId],[RoleId],[AssignedDate],[AssignedBy])
SELECT u.[Id], r.[Id], SYSDATETIME(), N'SeedDemoData.sql'
FROM #HospitalUsers h
JOIN [AspNetUsers] u ON u.[NormalizedUserName] = UPPER(h.[UserName])
JOIN [Roles] r ON r.[Name] = h.[Role]
WHERE NOT EXISTS (SELECT 1 FROM [StaffRoles] sr WHERE sr.[StaffId] = u.[Id] AND sr.[RoleId] = r.[Id]);

-- Hospitals each account works in (default = the hospital pre-selected at sign-in)
CREATE TABLE #Access ([UserName] nvarchar(256), [Code] nvarchar(20), [IsDefault] bit);
INSERT INTO #Access VALUES
  (N'admin.main', N'MAIN', 1),
  (N'doctor.main', N'MAIN', 1),
  (N'nurse.main', N'MAIN', 1),
  (N'reception.main', N'MAIN', 1),
  (N'accounts.main', N'MAIN', 1),
  (N'pharmacy.main', N'MAIN', 1),
  (N'lab.main', N'MAIN', 1),
  (N'admin.isb', N'ISB', 1),
  (N'doctor.isb', N'ISB', 1),
  (N'nurse.isb', N'ISB', 1),
  (N'reception.isb', N'ISB', 1),
  (N'accounts.isb', N'ISB', 1),
  (N'pharmacy.isb', N'ISB', 1),
  (N'lab.isb', N'ISB', 1),
  (N'admin.khi', N'KHI', 1),
  (N'doctor.khi', N'KHI', 1),
  (N'nurse.khi', N'KHI', 1),
  (N'reception.khi', N'KHI', 1),
  (N'accounts.khi', N'KHI', 1),
  (N'pharmacy.khi', N'KHI', 1),
  (N'lab.khi', N'KHI', 1),
  (N'admin.region', N'ISB', 1),
  (N'admin.region', N'KHI', 0),
  (N'tester', N'MAIN', 1),
  (N'tester', N'ISB', 0),
  (N'tester', N'KHI', 0);

INSERT INTO [UserHospitalAccesses] ([UserId],[HospitalId],[IsDefault],[CreatedDate])
SELECT u.[Id], CASE a.[Code] WHEN N'MAIN' THEN @HMain WHEN N'ISB' THEN @HIsb ELSE @HKhi END, a.[IsDefault], SYSDATETIME()
FROM #Access a
JOIN [AspNetUsers] u ON u.[NormalizedUserName] = UPPER(a.[UserName])
WHERE NOT EXISTS (SELECT 1 FROM [UserHospitalAccesses] x
                  WHERE x.[UserId] = u.[Id] AND x.[HospitalId] = CASE a.[Code] WHEN N'MAIN' THEN @HMain WHEN N'ISB' THEN @HIsb ELSE @HKhi END);

-- Hospital admins sign in without two-step login (testing only – remove on a live system)
DECLARE @exempt nvarchar(max) = N'admin.main, admin.isb, admin.khi, admin.region';
IF NOT EXISTS (SELECT 1 FROM [Settings] WHERE [Key] = N'Security:MfaExemptUserNames')
    INSERT INTO [Settings] ([Key],[Value],[Type],[Category],[Description],[IsSystem],[CreatedDate],[ModifiedBy])
    VALUES (N'Security:MfaExemptUserNames', @exempt, N'string', N'Security', N'Test accounts that may sign in without two-step login (testing only)', 1, SYSDATETIME(), N'SeedDemoData.sql');
ELSE
BEGIN
    DECLARE @current nvarchar(max) = (SELECT [Value] FROM [Settings] WHERE [Key] = N'Security:MfaExemptUserNames');
    DECLARE @list nvarchar(max) = ISNULL(@current, N'');
    SELECT @list = @list + CASE WHEN LTRIM(RTRIM(@list)) = N'' THEN N'' ELSE N', ' END + s.[value]
    FROM STRING_SPLIT(@exempt, N',') x CROSS APPLY (SELECT LTRIM(RTRIM(x.[value])) AS [value]) s
    WHERE (N',' + REPLACE(ISNULL(@current, N''), N' ', N'') + N',') NOT LIKE N'%,' + s.[value] + N',%';
    UPDATE [Settings] SET [Value] = @list, [ModifiedDate] = SYSDATETIME(), [ModifiedBy] = N'SeedDemoData.sql'
    WHERE [Key] = N'Security:MfaExemptUserNames' AND [Value] <> @list;
END

DROP TABLE #Access;
DROP TABLE #HospitalUsers;
GO

-- ============================================================
-- 28. HOSPITAL DATA – doctors, patients, wards, beds, appointments, OPD, IPD, bills and pharmacy
--     Recent activity for all three hospitals (dates relative to the day the script runs), so reports and
--     dashboards show figures for the current month. Amounts in PKR. Ids 101+ (existing ids are skipped).
-- ============================================================
DECLARE @Today datetime2 = CAST(CAST(SYSDATETIME() AS date) AS datetime2);
DECLARE @HMain int = COALESCE((SELECT TOP (1) [Id] FROM [Hospitals] WHERE [Code] = N'MAIN'), (SELECT TOP (1) [Id] FROM [Hospitals] WHERE [IsDefault] = 1 ORDER BY [Id]));
DECLARE @HIsb int = (SELECT TOP (1) [Id] FROM [Hospitals] WHERE [Code] = N'ISB');
DECLARE @HKhi int = (SELECT TOP (1) [Id] FROM [Hospitals] WHERE [Code] = N'KHI');

SET IDENTITY_INSERT [Doctors] ON;
INSERT INTO [Doctors] ([Id],[EmployeeId],[FirstName],[LastName],[Specialization],[LicenseNumber],[Phone],[Email],[DepartmentId],[IsActive],[CreatedDate])
SELECT * FROM (VALUES
  (101, N'DOC101', N'Imran', N'Qureshi', N'General Medicine', N'PMDC-52310-P', N'+92 330 1475140', N'doctor.main@medyxdemo.pk', 1, 1, DATEADD(MONTH, -18, @Today)),
  (102, N'DOC102', N'Nadeem', N'Akram', N'General Surgery', N'PMDC-52311-P', N'+92 331 1483059', N'dr.akram@medyxdemo.pk', 2, 1, DATEADD(MONTH, -18, @Today)),
  (103, N'DOC103', N'Farah', N'Naqvi', N'Cardiology', N'PMDC-52312-P', N'+92 332 1490978', N'doctor.isb@medyxdemo.pk', 5, 1, DATEADD(MONTH, -18, @Today)),
  (104, N'DOC104', N'Omar', N'Farooq', N'Orthopedics', N'PMDC-52313-P', N'+92 333 1498897', N'dr.farooq@medyxdemo.pk', 6, 1, DATEADD(MONTH, -18, @Today)),
  (105, N'DOC105', N'Arif', N'Shaikh', N'Obstetrics & Gynecology', N'PMDC-52314-P', N'+92 334 1506816', N'doctor.khi@medyxdemo.pk', 4, 1, DATEADD(MONTH, -18, @Today)),
  (106, N'DOC106', N'Maryam', N'Hashmi', N'Internal Medicine', N'PMDC-52315-P', N'+92 335 1514735', N'dr.hashmi@medyxdemo.pk', 1, 1, DATEADD(MONTH, -18, @Today))
) AS src([Id],[EmployeeId],[FirstName],[LastName],[Specialization],[LicenseNumber],[Phone],[Email],[DepartmentId],[IsActive],[CreatedDate])
WHERE NOT EXISTS (SELECT 1 FROM [Doctors] x WHERE x.[Id] = src.[Id]);
SET IDENTITY_INSERT [Doctors] OFF;

SET IDENTITY_INSERT [Patients] ON;
INSERT INTO [Patients] ([Id],[PatientId],[FirstName],[LastName],[Email],[Phone],[DateOfBirth],[Gender],[Address],[City],[State],[Country],[PostalCode],[BloodGroup],[EmergencyContactName],[EmergencyContactPhone],[EmergencyContactRelation],[MedicalHistory],[Allergies],[GuardianName],[GuardianPhone],[MaritalStatus],[Occupation],[UserId],[ProfileImagePath],[IsActive],[CreatedDate],[LastVisitDate])
SELECT * FROM (VALUES
  (101, N'PAT00101', N'Ali', N'Raza', N'ali.raza@mail.pk', N'+92 310 1633520', N'1982-03-14', N'Male', N'House 12, Street 3', N'Lahore', N'Punjab', N'Pakistan', N'54000', N'B+', N'Family contact', N'+92 310 1950280', N'Relative', N'Hypertension', N'', N'', N'', N'Married', N'', NULL, N'', 1, DATEADD(MONTH, -6, @Today), DATEADD(DAY, -1, @Today)),
  (102, N'PAT00102', N'Fatima', N'Noor', N'fatima.noor@mail.pk', N'+92 311 1641439', N'1990-07-02', N'Female', N'House 19, Street 4', N'Lahore', N'Punjab', N'Pakistan', N'54113', N'O+', N'Family contact', N'+92 311 1958199', N'Relative', N'', N'Penicillin', N'', N'', N'Married', N'', NULL, N'', 1, DATEADD(MONTH, -6, @Today), DATEADD(DAY, -1, @Today)),
  (103, N'PAT00103', N'Hamza', N'Khalid', N'hamza.khalid@mail.pk', N'+92 312 1649358', N'2012-11-20', N'Male', N'House 26, Street 5', N'Lahore', N'Punjab', N'Pakistan', N'54226', N'A+', N'Family contact', N'+92 312 1966118', N'Relative', N'Asthma', N'', N'', N'', N'Single', N'', NULL, N'', 1, DATEADD(MONTH, -6, @Today), DATEADD(DAY, -1, @Today)),
  (104, N'PAT00104', N'Sobia', N'Anwar', N'sobia.anwar@mail.pk', N'+92 313 1657277', N'1975-01-25', N'Female', N'House 33, Street 6', N'Islamabad', N'Islamabad Capital Territory', N'Pakistan', N'54339', N'AB+', N'Family contact', N'+92 313 1974037', N'Relative', N'Osteoporosis', N'', N'', N'', N'Married', N'', NULL, N'', 1, DATEADD(MONTH, -6, @Today), DATEADD(DAY, -1, @Today)),
  (105, N'PAT00105', N'Zeeshan', N'Haider', N'zeeshan.haider@mail.pk', N'+92 314 1665196', N'1968-09-09', N'Male', N'House 40, Street 7', N'Islamabad', N'Islamabad Capital Territory', N'Pakistan', N'54452', N'O-', N'Family contact', N'+92 314 1981956', N'Relative', N'Diabetes T2, IHD', N'Aspirin', N'', N'', N'Married', N'', NULL, N'', 1, DATEADD(MONTH, -6, @Today), DATEADD(DAY, -1, @Today)),
  (106, N'PAT00106', N'Maham', N'Aslam', N'maham.aslam@mail.pk', N'+92 315 1673115', N'1995-05-17', N'Female', N'House 47, Street 8', N'Rawalpindi', N'Punjab', N'Pakistan', N'54565', N'B-', N'Family contact', N'+92 315 1989875', N'Relative', N'', N'', N'', N'', N'Married', N'', NULL, N'', 1, DATEADD(MONTH, -6, @Today), DATEADD(DAY, -1, @Today)),
  (107, N'PAT00107', N'Saleem', N'Shah', N'saleem.shah@mail.pk', N'+92 316 1681034', N'1955-12-01', N'Male', N'House 54, Street 9', N'Karachi', N'Sindh', N'Pakistan', N'54678', N'A-', N'Family contact', N'+92 316 1997794', N'Relative', N'COPD', N'', N'', N'', N'Married', N'', NULL, N'', 1, DATEADD(MONTH, -6, @Today), DATEADD(DAY, -1, @Today)),
  (108, N'PAT00108', N'Nazia', N'Parveen', N'nazia.parveen@mail.pk', N'+92 317 1688953', N'1992-04-11', N'Female', N'House 61, Street 10', N'Karachi', N'Sindh', N'Pakistan', N'54791', N'B+', N'Family contact', N'+92 317 2005713', N'Relative', N'', N'Sulfa', N'', N'', N'Married', N'', NULL, N'', 1, DATEADD(MONTH, -6, @Today), DATEADD(DAY, -1, @Today)),
  (109, N'PAT00109', N'Owais', N'Qadri', N'owais.qadri@mail.pk', N'+92 318 1696872', N'2001-08-30', N'Male', N'House 68, Street 11', N'Karachi', N'Sindh', N'Pakistan', N'54904', N'O+', N'Family contact', N'+92 318 2013632', N'Relative', N'', N'', N'', N'', N'Married', N'', NULL, N'', 1, DATEADD(MONTH, -6, @Today), DATEADD(DAY, -1, @Today)),
  (110, N'PAT00110', N'Rubina', N'Yousaf', N'rubina.yousaf@mail.pk', N'+92 319 1704791', N'1979-02-19', N'Female', N'House 75, Street 12', N'Hyderabad', N'Sindh', N'Pakistan', N'55017', N'AB-', N'Family contact', N'+92 319 2021551', N'Relative', N'Thyroid disorder', N'', N'', N'', N'Married', N'', NULL, N'', 1, DATEADD(MONTH, -6, @Today), DATEADD(DAY, -1, @Today))
) AS src([Id],[PatientId],[FirstName],[LastName],[Email],[Phone],[DateOfBirth],[Gender],[Address],[City],[State],[Country],[PostalCode],[BloodGroup],[EmergencyContactName],[EmergencyContactPhone],[EmergencyContactRelation],[MedicalHistory],[Allergies],[GuardianName],[GuardianPhone],[MaritalStatus],[Occupation],[UserId],[ProfileImagePath],[IsActive],[CreatedDate],[LastVisitDate])
WHERE NOT EXISTS (SELECT 1 FROM [Patients] x WHERE x.[Id] = src.[Id]);
SET IDENTITY_INSERT [Patients] OFF;

SET IDENTITY_INSERT [Wards] ON;
INSERT INTO [Wards] ([Id],[Name],[Description],[TotalBeds],[OccupiedBeds],[IsActive],[CreatedDate],[HospitalId])
SELECT * FROM (VALUES
  (101, N'General Ward – Islamabad', N'General in-patient ward, Block A', 5, 1, 1, DATEADD(MONTH, -12, @Today), @HIsb),
  (102, N'ICU – Islamabad', N'Intensive care unit, Block A', 5, 1, 1, DATEADD(MONTH, -12, @Today), @HIsb),
  (103, N'Private Rooms – Islamabad', N'Single private rooms, Block B', 5, 0, 1, DATEADD(MONTH, -12, @Today), @HIsb),
  (104, N'General Ward – Karachi', N'General in-patient ward, Block A', 5, 0, 1, DATEADD(MONTH, -12, @Today), @HKhi),
  (105, N'Maternity – Karachi', N'Labour, delivery and post-natal care, Block B', 5, 1, 1, DATEADD(MONTH, -12, @Today), @HKhi),
  (106, N'ICU – Karachi', N'Intensive care unit, Block B', 5, 1, 1, DATEADD(MONTH, -12, @Today), @HKhi)
) AS src([Id],[Name],[Description],[TotalBeds],[OccupiedBeds],[IsActive],[CreatedDate],[HospitalId])
WHERE NOT EXISTS (SELECT 1 FROM [Wards] x WHERE x.[Id] = src.[Id]);
SET IDENTITY_INSERT [Wards] OFF;

SET IDENTITY_INSERT [Beds] ON;
INSERT INTO [Beds] ([Id],[WardId],[BedNumber],[BedType],[DailyCharges],[Status],[IsActive],[CreatedDate],[PatientId],[IsIsolation],[RequiresAdminApproval],[LastUpdated],[Block],[Floor],[RoomNumber])
SELECT * FROM (VALUES
  (101, 101, N'ISB-GW-01', N'General', 3500.00, N'Occupied', 1, DATEADD(MONTH, -12, @Today), 104, 0, 0, DATEADD(DAY, -1, @Today), N'A', N'1', N'101'),
  (102, 101, N'ISB-GW-02', N'General', 3500.00, N'Available', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 0, DATEADD(DAY, -1, @Today), N'A', N'1', N'101'),
  (103, 101, N'ISB-GW-03', N'General', 3500.00, N'Cleaning', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 0, DATEADD(DAY, -1, @Today), N'A', N'1', N'102'),
  (104, 101, N'ISB-GW-04', N'General', 3500.00, N'Available', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 0, DATEADD(DAY, -1, @Today), N'A', N'1', N'102'),
  (105, 101, N'ISB-GW-05', N'General', 3500.00, N'Available', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 0, DATEADD(DAY, -1, @Today), N'A', N'1', N'103'),
  (106, 102, N'ISB-ICU-01', N'ICU', 18000.00, N'Occupied', 1, DATEADD(MONTH, -12, @Today), 105, 0, 1, DATEADD(DAY, -1, @Today), N'A', N'2', N'ICU-1'),
  (107, 102, N'ISB-ICU-02', N'ICU', 18000.00, N'Available', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 1, DATEADD(DAY, -1, @Today), N'A', N'2', N'ICU-1'),
  (108, 102, N'ISB-ICU-03', N'ICU', 18000.00, N'Maintenance', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 1, DATEADD(DAY, -1, @Today), N'A', N'2', N'ICU-1'),
  (109, 102, N'ISB-ICU-04', N'ICU', 18000.00, N'Available', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 1, DATEADD(DAY, -1, @Today), N'A', N'2', N'ICU-2'),
  (110, 102, N'ISB-ICU-05', N'ICU', 18000.00, N'Available', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 1, DATEADD(DAY, -1, @Today), N'A', N'2', N'ICU-2'),
  (111, 103, N'ISB-PVT-01', N'Private', 9000.00, N'Available', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 0, DATEADD(DAY, -1, @Today), N'B', N'3', N'301'),
  (112, 103, N'ISB-PVT-02', N'Private', 9000.00, N'Available', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 0, DATEADD(DAY, -1, @Today), N'B', N'3', N'302'),
  (113, 103, N'ISB-PVT-03', N'Private', 9000.00, N'Blocked', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 0, DATEADD(DAY, -1, @Today), N'B', N'3', N'303'),
  (114, 103, N'ISB-PVT-04', N'Private', 9000.00, N'Available', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 0, DATEADD(DAY, -1, @Today), N'B', N'3', N'304'),
  (115, 103, N'ISB-PVT-05', N'Private', 9000.00, N'Available', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 0, DATEADD(DAY, -1, @Today), N'B', N'3', N'305'),
  (116, 104, N'KHI-GW-01', N'General', 3500.00, N'Available', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 0, DATEADD(DAY, -1, @Today), N'A', N'G', N'G-01'),
  (117, 104, N'KHI-GW-02', N'General', 3500.00, N'Available', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 0, DATEADD(DAY, -1, @Today), N'A', N'G', N'G-01'),
  (118, 104, N'KHI-GW-03', N'General', 3500.00, N'Available', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 0, DATEADD(DAY, -1, @Today), N'A', N'G', N'G-01'),
  (119, 104, N'KHI-GW-04', N'General', 3500.00, N'Cleaning', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 0, DATEADD(DAY, -1, @Today), N'A', N'G', N'G-02'),
  (120, 104, N'KHI-GW-05', N'General', 3500.00, N'Available', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 0, DATEADD(DAY, -1, @Today), N'A', N'G', N'G-02'),
  (121, 105, N'KHI-MAT-01', N'Semi-private', 6000.00, N'Occupied', 1, DATEADD(MONTH, -12, @Today), 108, 0, 0, DATEADD(DAY, -1, @Today), N'B', N'1', N'M-11'),
  (122, 105, N'KHI-MAT-02', N'Semi-private', 6000.00, N'Available', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 0, DATEADD(DAY, -1, @Today), N'B', N'1', N'M-11'),
  (123, 105, N'KHI-MAT-03', N'Semi-private', 6000.00, N'Available', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 0, DATEADD(DAY, -1, @Today), N'B', N'1', N'M-12'),
  (124, 105, N'KHI-MAT-04', N'Semi-private', 6000.00, N'Available', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 0, DATEADD(DAY, -1, @Today), N'B', N'1', N'M-12'),
  (125, 105, N'KHI-MAT-05', N'Semi-private', 6000.00, N'Maintenance', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 0, DATEADD(DAY, -1, @Today), N'B', N'1', N'M-13'),
  (126, 106, N'KHI-ICU-01', N'ICU', 18000.00, N'Occupied', 1, DATEADD(MONTH, -12, @Today), 107, 0, 1, DATEADD(DAY, -1, @Today), N'B', N'2', N'ICU'),
  (127, 106, N'KHI-ICU-02', N'ICU', 18000.00, N'Available', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 1, DATEADD(DAY, -1, @Today), N'B', N'2', N'ICU'),
  (128, 106, N'KHI-ICU-03', N'ICU', 18000.00, N'Available', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 1, DATEADD(DAY, -1, @Today), N'B', N'2', N'ICU'),
  (129, 106, N'KHI-ICU-04', N'ICU', 18000.00, N'Blocked', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 1, DATEADD(DAY, -1, @Today), N'B', N'2', N'ICU'),
  (130, 106, N'KHI-ICU-05', N'ICU', 18000.00, N'Available', 1, DATEADD(MONTH, -12, @Today), NULL, 0, 1, DATEADD(DAY, -1, @Today), N'B', N'2', N'ICU')
) AS src([Id],[WardId],[BedNumber],[BedType],[DailyCharges],[Status],[IsActive],[CreatedDate],[PatientId],[IsIsolation],[RequiresAdminApproval],[LastUpdated],[Block],[Floor],[RoomNumber])
WHERE NOT EXISTS (SELECT 1 FROM [Beds] x WHERE x.[Id] = src.[Id]);
SET IDENTITY_INSERT [Beds] OFF;

-- Appointments are booked by each hospital's receptionist.
DECLARE @RecMain nvarchar(450) = (SELECT [Id] FROM [AspNetUsers] WHERE [NormalizedUserName] = N'RECEPTION.MAIN');
DECLARE @RecIsb nvarchar(450) = (SELECT [Id] FROM [AspNetUsers] WHERE [NormalizedUserName] = N'RECEPTION.ISB');
DECLARE @RecKhi nvarchar(450) = (SELECT [Id] FROM [AspNetUsers] WHERE [NormalizedUserName] = N'RECEPTION.KHI');
SET IDENTITY_INSERT [Appointments] ON;
INSERT INTO [Appointments] ([Id],[PatientId],[DoctorId],[StaffId],[AppointmentDate],[AppointmentTime],[Status],[AppointmentType],[Priority],[Symptoms],[Notes],[CreatedDate],[CreatedBy],[UpdatedDate],[UpdatedBy],[HospitalId])
SELECT * FROM (VALUES
  (101, 101, 101, @RecMain, DATEADD(DAY, -14, @Today), CAST(N'09:00:00' AS time), N'Completed', N'OPD', N'Normal', N'Fever and body aches', N'', DATEADD(DAY, -17, @Today), N'reception.main', NULL, N'', @HMain),
  (102, 102, 102, @RecMain, DATEADD(DAY, -10, @Today), CAST(N'09:30:00' AS time), N'Completed', N'Follow-up', N'Normal', N'Follow-up of blood pressure', N'', DATEADD(DAY, -13, @Today), N'reception.main', NULL, N'', @HMain),
  (103, 103, 101, @RecMain, DATEADD(DAY, -7, @Today), CAST(N'10:00:00' AS time), N'Cancelled', N'Consultation', N'Normal', N'Chest discomfort', N'', DATEADD(DAY, -10, @Today), N'reception.main', NULL, N'', @HMain),
  (104, 1, 102, @RecMain, DATEADD(DAY, -5, @Today), CAST(N'10:30:00' AS time), N'Completed', N'OPD', N'Urgent', N'Persistent cough', N'', DATEADD(DAY, -8, @Today), N'reception.main', NULL, N'', @HMain),
  (105, 2, 101, @RecMain, DATEADD(DAY, -3, @Today), CAST(N'11:00:00' AS time), N'No-Show', N'OPD', N'Normal', N'Joint pain', N'', DATEADD(DAY, -6, @Today), N'reception.main', NULL, N'', @HMain),
  (106, 5, 102, @RecMain, DATEADD(DAY, -1, @Today), CAST(N'11:30:00' AS time), N'Completed', N'Follow-up', N'Normal', N'Headache and dizziness', N'', DATEADD(DAY, -4, @Today), N'reception.main', NULL, N'', @HMain),
  (107, 11, 101, @RecMain, DATEADD(DAY, 0, @Today), CAST(N'12:00:00' AS time), N'Scheduled', N'Consultation', N'Normal', N'Abdominal pain', N'', DATEADD(DAY, -3, @Today), N'reception.main', NULL, N'', @HMain),
  (108, 20, 102, @RecMain, DATEADD(DAY, 2, @Today), CAST(N'14:00:00' AS time), N'Scheduled', N'OPD', N'Normal', N'Routine check-up', N'', DATEADD(DAY, -1, @Today), N'reception.main', NULL, N'', @HMain),
  (109, 104, 103, @RecIsb, DATEADD(DAY, -14, @Today), CAST(N'09:00:00' AS time), N'Completed', N'OPD', N'Normal', N'Fever and body aches', N'', DATEADD(DAY, -17, @Today), N'reception.isb', NULL, N'', @HIsb),
  (110, 105, 104, @RecIsb, DATEADD(DAY, -10, @Today), CAST(N'09:30:00' AS time), N'Completed', N'Follow-up', N'Normal', N'Follow-up of blood pressure', N'', DATEADD(DAY, -13, @Today), N'reception.isb', NULL, N'', @HIsb),
  (111, 106, 103, @RecIsb, DATEADD(DAY, -7, @Today), CAST(N'10:00:00' AS time), N'Cancelled', N'Consultation', N'Normal', N'Chest discomfort', N'', DATEADD(DAY, -10, @Today), N'reception.isb', NULL, N'', @HIsb),
  (112, 3, 104, @RecIsb, DATEADD(DAY, -5, @Today), CAST(N'10:30:00' AS time), N'Completed', N'OPD', N'Urgent', N'Persistent cough', N'', DATEADD(DAY, -8, @Today), N'reception.isb', NULL, N'', @HIsb),
  (113, 4, 103, @RecIsb, DATEADD(DAY, -3, @Today), CAST(N'11:00:00' AS time), N'No-Show', N'OPD', N'Normal', N'Joint pain', N'', DATEADD(DAY, -6, @Today), N'reception.isb', NULL, N'', @HIsb),
  (114, 9, 104, @RecIsb, DATEADD(DAY, -1, @Today), CAST(N'11:30:00' AS time), N'Completed', N'Follow-up', N'Normal', N'Headache and dizziness', N'', DATEADD(DAY, -4, @Today), N'reception.isb', NULL, N'', @HIsb),
  (115, 12, 103, @RecIsb, DATEADD(DAY, 0, @Today), CAST(N'12:00:00' AS time), N'Scheduled', N'Consultation', N'Normal', N'Abdominal pain', N'', DATEADD(DAY, -3, @Today), N'reception.isb', NULL, N'', @HIsb),
  (116, 19, 104, @RecIsb, DATEADD(DAY, 2, @Today), CAST(N'14:00:00' AS time), N'Scheduled', N'OPD', N'Normal', N'Routine check-up', N'', DATEADD(DAY, -1, @Today), N'reception.isb', NULL, N'', @HIsb),
  (117, 107, 105, @RecKhi, DATEADD(DAY, -14, @Today), CAST(N'09:00:00' AS time), N'Completed', N'OPD', N'Normal', N'Fever and body aches', N'', DATEADD(DAY, -17, @Today), N'reception.khi', NULL, N'', @HKhi),
  (118, 108, 106, @RecKhi, DATEADD(DAY, -10, @Today), CAST(N'09:30:00' AS time), N'Completed', N'Follow-up', N'Normal', N'Follow-up of blood pressure', N'', DATEADD(DAY, -13, @Today), N'reception.khi', NULL, N'', @HKhi),
  (119, 109, 105, @RecKhi, DATEADD(DAY, -7, @Today), CAST(N'10:00:00' AS time), N'Cancelled', N'Consultation', N'Normal', N'Chest discomfort', N'', DATEADD(DAY, -10, @Today), N'reception.khi', NULL, N'', @HKhi),
  (120, 110, 106, @RecKhi, DATEADD(DAY, -5, @Today), CAST(N'10:30:00' AS time), N'Completed', N'OPD', N'Urgent', N'Persistent cough', N'', DATEADD(DAY, -8, @Today), N'reception.khi', NULL, N'', @HKhi),
  (121, 6, 105, @RecKhi, DATEADD(DAY, -3, @Today), CAST(N'11:00:00' AS time), N'No-Show', N'OPD', N'Normal', N'Joint pain', N'', DATEADD(DAY, -6, @Today), N'reception.khi', NULL, N'', @HKhi),
  (122, 7, 106, @RecKhi, DATEADD(DAY, -1, @Today), CAST(N'11:30:00' AS time), N'Completed', N'Follow-up', N'Normal', N'Headache and dizziness', N'', DATEADD(DAY, -4, @Today), N'reception.khi', NULL, N'', @HKhi),
  (123, 8, 105, @RecKhi, DATEADD(DAY, 0, @Today), CAST(N'12:00:00' AS time), N'Scheduled', N'Consultation', N'Normal', N'Abdominal pain', N'', DATEADD(DAY, -3, @Today), N'reception.khi', NULL, N'', @HKhi),
  (124, 10, 106, @RecKhi, DATEADD(DAY, 2, @Today), CAST(N'14:00:00' AS time), N'Scheduled', N'OPD', N'Normal', N'Routine check-up', N'', DATEADD(DAY, -1, @Today), N'reception.khi', NULL, N'', @HKhi)
) AS src([Id],[PatientId],[DoctorId],[StaffId],[AppointmentDate],[AppointmentTime],[Status],[AppointmentType],[Priority],[Symptoms],[Notes],[CreatedDate],[CreatedBy],[UpdatedDate],[UpdatedBy],[HospitalId])
WHERE NOT EXISTS (SELECT 1 FROM [Appointments] x WHERE x.[Id] = src.[Id]);
SET IDENTITY_INSERT [Appointments] OFF;

SET IDENTITY_INSERT [OPDVisits] ON;
INSERT INTO [OPDVisits] ([Id],[PatientId],[DoctorId],[VisitDate],[Symptoms],[Diagnosis],[Treatment],[Prescription],[Notes],[ConsultationFee],[PaymentStatus],[CreatedDate],[CreatedBy],[HospitalId])
SELECT * FROM (VALUES
  (101, 101, 101, DATEADD(MINUTE, 615, DATEADD(DAY, -14, @Today)), N'Fever and body aches', N'Viral fever', N'Rest, fluids, paracetamol', N'Paracetamol 500 mg TDS x 5 days', N'', 2000.00, N'Paid', DATEADD(MINUTE, 615, DATEADD(DAY, -14, @Today)), N'doctor.main', @HMain),
  (102, 102, 102, DATEADD(MINUTE, 615, DATEADD(DAY, -10, @Today)), N'Follow-up of blood pressure', N'Essential hypertension', N'Medication adjusted', N'Amlodipine 5 mg OD', N'', 2000.00, N'Paid', DATEADD(MINUTE, 615, DATEADD(DAY, -10, @Today)), N'doctor.main', @HMain),
  (103, 1, 101, DATEADD(MINUTE, 615, DATEADD(DAY, -5, @Today)), N'Persistent cough', N'Gastritis', N'Diet advice, PPI', N'Omeprazole 20 mg BD x 14 days', N'', 2500.00, N'Paid', DATEADD(MINUTE, 615, DATEADD(DAY, -5, @Today)), N'doctor.main', @HMain),
  (104, 5, 102, DATEADD(MINUTE, 615, DATEADD(DAY, -1, @Today)), N'Headache and dizziness', N'Acute bronchitis', N'Inhaler, steam', N'Salbutamol MDI 2 puffs BD', N'', 2000.00, N'Paid', DATEADD(MINUTE, 615, DATEADD(DAY, -1, @Today)), N'doctor.main', @HMain),
  (105, 103, 101, DATEADD(MINUTE, 615, DATEADD(DAY, -2, @Today)), N'Chest discomfort', N'Osteoarthritis knee', N'Physiotherapy, NSAID', N'Diclofenac 50 mg BD x 7 days', N'', 2000.00, N'Pending', DATEADD(MINUTE, 615, DATEADD(DAY, -2, @Today)), N'doctor.main', @HMain),
  (106, 11, 102, DATEADD(MINUTE, 615, DATEADD(DAY, -8, @Today)), N'Abdominal pain', N'Migraine', N'Analgesics, triggers explained', N'Sumatriptan 50 mg SOS', N'', 2000.00, N'Paid', DATEADD(MINUTE, 615, DATEADD(DAY, -8, @Today)), N'doctor.main', @HMain),
  (107, 104, 103, DATEADD(MINUTE, 615, DATEADD(DAY, -14, @Today)), N'Fever and body aches', N'Viral fever', N'Rest, fluids, paracetamol', N'Paracetamol 500 mg TDS x 5 days', N'', 3000.00, N'Paid', DATEADD(MINUTE, 615, DATEADD(DAY, -14, @Today)), N'doctor.isb', @HIsb),
  (108, 105, 104, DATEADD(MINUTE, 615, DATEADD(DAY, -10, @Today)), N'Follow-up of blood pressure', N'Essential hypertension', N'Medication adjusted', N'Amlodipine 5 mg OD', N'', 3000.00, N'Paid', DATEADD(MINUTE, 615, DATEADD(DAY, -10, @Today)), N'doctor.isb', @HIsb),
  (109, 3, 103, DATEADD(MINUTE, 615, DATEADD(DAY, -5, @Today)), N'Persistent cough', N'Gastritis', N'Diet advice, PPI', N'Omeprazole 20 mg BD x 14 days', N'', 3500.00, N'Paid', DATEADD(MINUTE, 615, DATEADD(DAY, -5, @Today)), N'doctor.isb', @HIsb),
  (110, 9, 104, DATEADD(MINUTE, 615, DATEADD(DAY, -1, @Today)), N'Headache and dizziness', N'Acute bronchitis', N'Inhaler, steam', N'Salbutamol MDI 2 puffs BD', N'', 3000.00, N'Paid', DATEADD(MINUTE, 615, DATEADD(DAY, -1, @Today)), N'doctor.isb', @HIsb),
  (111, 106, 103, DATEADD(MINUTE, 615, DATEADD(DAY, -2, @Today)), N'Chest discomfort', N'Osteoarthritis knee', N'Physiotherapy, NSAID', N'Diclofenac 50 mg BD x 7 days', N'', 3000.00, N'Pending', DATEADD(MINUTE, 615, DATEADD(DAY, -2, @Today)), N'doctor.isb', @HIsb),
  (112, 12, 104, DATEADD(MINUTE, 615, DATEADD(DAY, -8, @Today)), N'Abdominal pain', N'Migraine', N'Analgesics, triggers explained', N'Sumatriptan 50 mg SOS', N'', 3000.00, N'Paid', DATEADD(MINUTE, 615, DATEADD(DAY, -8, @Today)), N'doctor.isb', @HIsb),
  (113, 107, 105, DATEADD(MINUTE, 615, DATEADD(DAY, -14, @Today)), N'Fever and body aches', N'Viral fever', N'Rest, fluids, paracetamol', N'Paracetamol 500 mg TDS x 5 days', N'', 2500.00, N'Paid', DATEADD(MINUTE, 615, DATEADD(DAY, -14, @Today)), N'doctor.khi', @HKhi),
  (114, 108, 106, DATEADD(MINUTE, 615, DATEADD(DAY, -10, @Today)), N'Follow-up of blood pressure', N'Essential hypertension', N'Medication adjusted', N'Amlodipine 5 mg OD', N'', 2500.00, N'Paid', DATEADD(MINUTE, 615, DATEADD(DAY, -10, @Today)), N'doctor.khi', @HKhi),
  (115, 110, 105, DATEADD(MINUTE, 615, DATEADD(DAY, -5, @Today)), N'Persistent cough', N'Gastritis', N'Diet advice, PPI', N'Omeprazole 20 mg BD x 14 days', N'', 3000.00, N'Paid', DATEADD(MINUTE, 615, DATEADD(DAY, -5, @Today)), N'doctor.khi', @HKhi),
  (116, 7, 106, DATEADD(MINUTE, 615, DATEADD(DAY, -1, @Today)), N'Headache and dizziness', N'Acute bronchitis', N'Inhaler, steam', N'Salbutamol MDI 2 puffs BD', N'', 2500.00, N'Paid', DATEADD(MINUTE, 615, DATEADD(DAY, -1, @Today)), N'doctor.khi', @HKhi),
  (117, 109, 105, DATEADD(MINUTE, 615, DATEADD(DAY, -2, @Today)), N'Chest discomfort', N'Osteoarthritis knee', N'Physiotherapy, NSAID', N'Diclofenac 50 mg BD x 7 days', N'', 2500.00, N'Pending', DATEADD(MINUTE, 615, DATEADD(DAY, -2, @Today)), N'doctor.khi', @HKhi),
  (118, 8, 106, DATEADD(MINUTE, 615, DATEADD(DAY, -8, @Today)), N'Abdominal pain', N'Migraine', N'Analgesics, triggers explained', N'Sumatriptan 50 mg SOS', N'', 2500.00, N'Paid', DATEADD(MINUTE, 615, DATEADD(DAY, -8, @Today)), N'doctor.khi', @HKhi)
) AS src([Id],[PatientId],[DoctorId],[VisitDate],[Symptoms],[Diagnosis],[Treatment],[Prescription],[Notes],[ConsultationFee],[PaymentStatus],[CreatedDate],[CreatedBy],[HospitalId])
WHERE NOT EXISTS (SELECT 1 FROM [OPDVisits] x WHERE x.[Id] = src.[Id]);
SET IDENTITY_INSERT [OPDVisits] OFF;

SET IDENTITY_INSERT [IPDAdmissions] ON;
INSERT INTO [IPDAdmissions] ([Id],[PatientId],[DoctorId],[BedId],[AdmissionDate],[DischargeDate],[AdmissionType],[Diagnosis],[Treatment],[Notes],[Status],[DailyCharges],[CreatedDate],[CreatedBy],[HospitalId])
SELECT * FROM (VALUES
  (101, 105, 103, 106, DATEADD(MINUTE, 510, DATEADD(DAY, -3, @Today)), NULL, N'Emergency', N'Acute myocardial infarction', N'Primary PCI, dual antiplatelets, CCU monitoring', N'', N'Admitted', 18000.00, DATEADD(MINUTE, 510, DATEADD(DAY, -3, @Today)), N'doctor.isb', @HIsb),
  (102, 104, 104, 101, DATEADD(MINUTE, 510, DATEADD(DAY, -6, @Today)), NULL, N'Planned', N'Fracture neck of femur', N'ORIF done, physiotherapy', N'', N'Admitted', 3500.00, DATEADD(MINUTE, 510, DATEADD(DAY, -6, @Today)), N'doctor.isb', @HIsb),
  (103, 106, 103, 103, DATEADD(MINUTE, 510, DATEADD(DAY, -12, @Today)), DATEADD(MINUTE, 720, DATEADD(DAY, -8, @Today)), N'Emergency', N'Chest pain – ACS ruled out', N'Serial troponins, ECG, observation', N'', N'Discharged', 3500.00, DATEADD(MINUTE, 510, DATEADD(DAY, -12, @Today)), N'doctor.isb', @HIsb),
  (104, 108, 105, 121, DATEADD(MINUTE, 510, DATEADD(DAY, -2, @Today)), NULL, N'Planned', N'Elective caesarean section', N'Lower segment C-section, post-op care', N'', N'Admitted', 6000.00, DATEADD(MINUTE, 510, DATEADD(DAY, -2, @Today)), N'doctor.khi', @HKhi),
  (105, 107, 106, 126, DATEADD(MINUTE, 510, DATEADD(DAY, -1, @Today)), NULL, N'Emergency', N'COPD exacerbation', N'Nebulisation, IV steroids, oxygen', N'', N'Admitted', 18000.00, DATEADD(MINUTE, 510, DATEADD(DAY, -1, @Today)), N'doctor.khi', @HKhi),
  (106, 109, 106, 119, DATEADD(MINUTE, 510, DATEADD(DAY, -9, @Today)), DATEADD(MINUTE, 720, DATEADD(DAY, -6, @Today)), N'Emergency', N'Dengue fever', N'IV fluids, platelet monitoring', N'', N'Discharged', 3500.00, DATEADD(MINUTE, 510, DATEADD(DAY, -9, @Today)), N'doctor.khi', @HKhi),
  (107, 101, 101, 1, DATEADD(MINUTE, 510, DATEADD(DAY, -10, @Today)), DATEADD(MINUTE, 720, DATEADD(DAY, -7, @Today)), N'Emergency', N'Hypertensive urgency', N'IV labetalol, medication review', N'', N'Discharged', 500.00, DATEADD(MINUTE, 510, DATEADD(DAY, -10, @Today)), N'doctor.main', @HMain)
) AS src([Id],[PatientId],[DoctorId],[BedId],[AdmissionDate],[DischargeDate],[AdmissionType],[Diagnosis],[Treatment],[Notes],[Status],[DailyCharges],[CreatedDate],[CreatedBy],[HospitalId])
WHERE NOT EXISTS (SELECT 1 FROM [IPDAdmissions] x WHERE x.[Id] = src.[Id]);
SET IDENTITY_INSERT [IPDAdmissions] OFF;

SET IDENTITY_INSERT [Bills] ON;
INSERT INTO [Bills] ([Id],[BillNumber],[PatientId],[AppointmentId],[BillDate],[DueDate],[TotalAmount],[PaidAmount],[PendingAmount],[Status],[BillType],[Notes],[CreatedDate],[UpdatedDate],[CreatedBy],[HospitalId])
SELECT * FROM (VALUES
  (101, N'BILL-MAIN-0101', 101, NULL, DATEADD(DAY, -14, @Today), DATEADD(DAY, 0, @Today), 2000.00, 2000.00, 0.00, N'Paid', N'OPD', N'', DATEADD(DAY, -14, @Today), NULL, N'accounts.main', @HMain),
  (102, N'BILL-MAIN-0102', 102, NULL, DATEADD(DAY, -10, @Today), DATEADD(DAY, 4, @Today), 2000.00, 2000.00, 0.00, N'Paid', N'OPD', N'', DATEADD(DAY, -10, @Today), NULL, N'accounts.main', @HMain),
  (103, N'BILL-MAIN-0103', 1, NULL, DATEADD(DAY, -5, @Today), DATEADD(DAY, 9, @Today), 2500.00, 2500.00, 0.00, N'Paid', N'OPD', N'', DATEADD(DAY, -5, @Today), NULL, N'accounts.main', @HMain),
  (104, N'BILL-MAIN-0104', 5, NULL, DATEADD(DAY, -1, @Today), DATEADD(DAY, 13, @Today), 2000.00, 2000.00, 0.00, N'Paid', N'OPD', N'', DATEADD(DAY, -1, @Today), NULL, N'accounts.main', @HMain),
  (105, N'BILL-MAIN-0105', 103, NULL, DATEADD(DAY, -2, @Today), DATEADD(DAY, 12, @Today), 2000.00, 0.00, 2000.00, N'Unpaid', N'OPD', N'', DATEADD(DAY, -2, @Today), NULL, N'accounts.main', @HMain),
  (106, N'BILL-MAIN-0106', 11, NULL, DATEADD(DAY, -8, @Today), DATEADD(DAY, 6, @Today), 2000.00, 2000.00, 0.00, N'Paid', N'OPD', N'', DATEADD(DAY, -8, @Today), NULL, N'accounts.main', @HMain),
  (107, N'BILL-ISB-0107', 104, NULL, DATEADD(DAY, -14, @Today), DATEADD(DAY, 0, @Today), 3000.00, 3000.00, 0.00, N'Paid', N'OPD', N'', DATEADD(DAY, -14, @Today), NULL, N'accounts.isb', @HIsb),
  (108, N'BILL-ISB-0108', 105, NULL, DATEADD(DAY, -10, @Today), DATEADD(DAY, 4, @Today), 3000.00, 3000.00, 0.00, N'Paid', N'OPD', N'', DATEADD(DAY, -10, @Today), NULL, N'accounts.isb', @HIsb),
  (109, N'BILL-ISB-0109', 3, NULL, DATEADD(DAY, -5, @Today), DATEADD(DAY, 9, @Today), 3500.00, 3500.00, 0.00, N'Paid', N'OPD', N'', DATEADD(DAY, -5, @Today), NULL, N'accounts.isb', @HIsb),
  (110, N'BILL-ISB-0110', 9, NULL, DATEADD(DAY, -1, @Today), DATEADD(DAY, 13, @Today), 3000.00, 3000.00, 0.00, N'Paid', N'OPD', N'', DATEADD(DAY, -1, @Today), NULL, N'accounts.isb', @HIsb),
  (111, N'BILL-ISB-0111', 106, NULL, DATEADD(DAY, -2, @Today), DATEADD(DAY, 12, @Today), 3000.00, 0.00, 3000.00, N'Unpaid', N'OPD', N'', DATEADD(DAY, -2, @Today), NULL, N'accounts.isb', @HIsb),
  (112, N'BILL-ISB-0112', 12, NULL, DATEADD(DAY, -8, @Today), DATEADD(DAY, 6, @Today), 3000.00, 3000.00, 0.00, N'Paid', N'OPD', N'', DATEADD(DAY, -8, @Today), NULL, N'accounts.isb', @HIsb),
  (113, N'BILL-KHI-0113', 107, NULL, DATEADD(DAY, -14, @Today), DATEADD(DAY, 0, @Today), 2500.00, 2500.00, 0.00, N'Paid', N'OPD', N'', DATEADD(DAY, -14, @Today), NULL, N'accounts.khi', @HKhi),
  (114, N'BILL-KHI-0114', 108, NULL, DATEADD(DAY, -10, @Today), DATEADD(DAY, 4, @Today), 2500.00, 2500.00, 0.00, N'Paid', N'OPD', N'', DATEADD(DAY, -10, @Today), NULL, N'accounts.khi', @HKhi),
  (115, N'BILL-KHI-0115', 110, NULL, DATEADD(DAY, -5, @Today), DATEADD(DAY, 9, @Today), 3000.00, 3000.00, 0.00, N'Paid', N'OPD', N'', DATEADD(DAY, -5, @Today), NULL, N'accounts.khi', @HKhi),
  (116, N'BILL-KHI-0116', 7, NULL, DATEADD(DAY, -1, @Today), DATEADD(DAY, 13, @Today), 2500.00, 2500.00, 0.00, N'Paid', N'OPD', N'', DATEADD(DAY, -1, @Today), NULL, N'accounts.khi', @HKhi),
  (117, N'BILL-KHI-0117', 109, NULL, DATEADD(DAY, -2, @Today), DATEADD(DAY, 12, @Today), 2500.00, 0.00, 2500.00, N'Unpaid', N'OPD', N'', DATEADD(DAY, -2, @Today), NULL, N'accounts.khi', @HKhi),
  (118, N'BILL-KHI-0118', 8, NULL, DATEADD(DAY, -8, @Today), DATEADD(DAY, 6, @Today), 2500.00, 2500.00, 0.00, N'Paid', N'OPD', N'', DATEADD(DAY, -8, @Today), NULL, N'accounts.khi', @HKhi),
  (119, N'BILL-ISB-IPD-101', 105, NULL, DATEADD(DAY, -3, @Today), DATEADD(DAY, 11, @Today), 408000.00, 204000.00, 204000.00, N'Partially Paid', N'IPD', N'', DATEADD(DAY, -3, @Today), NULL, N'accounts.isb', @HIsb),
  (120, N'BILL-ISB-IPD-102', 104, NULL, DATEADD(DAY, -6, @Today), DATEADD(DAY, 8, @Today), 110000.00, 55000.00, 55000.00, N'Partially Paid', N'IPD', N'', DATEADD(DAY, -6, @Today), NULL, N'accounts.isb', @HIsb),
  (121, N'BILL-ISB-IPD-103', 106, NULL, DATEADD(DAY, -12, @Today), DATEADD(DAY, 2, @Today), 18000.00, 18000.00, 0.00, N'Paid', N'IPD', N'', DATEADD(DAY, -12, @Today), NULL, N'accounts.isb', @HIsb),
  (122, N'BILL-KHI-IPD-104', 108, NULL, DATEADD(DAY, -2, @Today), DATEADD(DAY, 12, @Today), 110500.00, 55000.00, 55500.00, N'Partially Paid', N'IPD', N'', DATEADD(DAY, -2, @Today), NULL, N'accounts.khi', @HKhi),
  (123, N'BILL-KHI-IPD-105', 107, NULL, DATEADD(DAY, -1, @Today), DATEADD(DAY, 13, @Today), 21500.00, 0.00, 21500.00, N'Unpaid', N'IPD', N'', DATEADD(DAY, -1, @Today), NULL, N'accounts.khi', @HKhi),
  (124, N'BILL-KHI-IPD-106', 109, NULL, DATEADD(DAY, -9, @Today), DATEADD(DAY, 5, @Today), 14000.00, 14000.00, 0.00, N'Paid', N'IPD', N'', DATEADD(DAY, -9, @Today), NULL, N'accounts.khi', @HKhi),
  (125, N'BILL-MAIN-IPD-107', 101, NULL, DATEADD(DAY, -10, @Today), DATEADD(DAY, 4, @Today), 4500.00, 4500.00, 0.00, N'Paid', N'IPD', N'', DATEADD(DAY, -10, @Today), NULL, N'accounts.main', @HMain)
) AS src([Id],[BillNumber],[PatientId],[AppointmentId],[BillDate],[DueDate],[TotalAmount],[PaidAmount],[PendingAmount],[Status],[BillType],[Notes],[CreatedDate],[UpdatedDate],[CreatedBy],[HospitalId])
WHERE NOT EXISTS (SELECT 1 FROM [Bills] x WHERE x.[Id] = src.[Id] OR x.[BillNumber] = src.[BillNumber]);
SET IDENTITY_INSERT [Bills] OFF;

SET IDENTITY_INSERT [BillItems] ON;
INSERT INTO [BillItems] ([Id],[BillId],[ItemName],[ItemType],[Quantity],[UnitPrice],[TotalPrice],[Amount],[Description],[CreatedDate])
SELECT * FROM (VALUES
  (101, 101, N'Consultation Fee', N'Service', 1, 2000.00, 2000.00, 2000.00, N'', DATEADD(DAY, -14, @Today)),
  (102, 102, N'Consultation Fee', N'Service', 1, 2000.00, 2000.00, 2000.00, N'', DATEADD(DAY, -10, @Today)),
  (103, 103, N'Consultation Fee', N'Service', 1, 2500.00, 2500.00, 2500.00, N'', DATEADD(DAY, -5, @Today)),
  (104, 104, N'Consultation Fee', N'Service', 1, 2000.00, 2000.00, 2000.00, N'', DATEADD(DAY, -1, @Today)),
  (105, 105, N'Consultation Fee', N'Service', 1, 2000.00, 2000.00, 2000.00, N'', DATEADD(DAY, -2, @Today)),
  (106, 106, N'Consultation Fee', N'Service', 1, 2000.00, 2000.00, 2000.00, N'', DATEADD(DAY, -8, @Today)),
  (107, 107, N'Consultation Fee', N'Service', 1, 3000.00, 3000.00, 3000.00, N'', DATEADD(DAY, -14, @Today)),
  (108, 108, N'Consultation Fee', N'Service', 1, 3000.00, 3000.00, 3000.00, N'', DATEADD(DAY, -10, @Today)),
  (109, 109, N'Consultation Fee', N'Service', 1, 3500.00, 3500.00, 3500.00, N'', DATEADD(DAY, -5, @Today)),
  (110, 110, N'Consultation Fee', N'Service', 1, 3000.00, 3000.00, 3000.00, N'', DATEADD(DAY, -1, @Today)),
  (111, 111, N'Consultation Fee', N'Service', 1, 3000.00, 3000.00, 3000.00, N'', DATEADD(DAY, -2, @Today)),
  (112, 112, N'Consultation Fee', N'Service', 1, 3000.00, 3000.00, 3000.00, N'', DATEADD(DAY, -8, @Today)),
  (113, 113, N'Consultation Fee', N'Service', 1, 2500.00, 2500.00, 2500.00, N'', DATEADD(DAY, -14, @Today)),
  (114, 114, N'Consultation Fee', N'Service', 1, 2500.00, 2500.00, 2500.00, N'', DATEADD(DAY, -10, @Today)),
  (115, 115, N'Consultation Fee', N'Service', 1, 3000.00, 3000.00, 3000.00, N'', DATEADD(DAY, -5, @Today)),
  (116, 116, N'Consultation Fee', N'Service', 1, 2500.00, 2500.00, 2500.00, N'', DATEADD(DAY, -1, @Today)),
  (117, 117, N'Consultation Fee', N'Service', 1, 2500.00, 2500.00, 2500.00, N'', DATEADD(DAY, -2, @Today)),
  (118, 118, N'Consultation Fee', N'Service', 1, 2500.00, 2500.00, 2500.00, N'', DATEADD(DAY, -8, @Today)),
  (119, 119, N'Admission consultation', N'Service', 1, 4000.00, 4000.00, 4000.00, N'', DATEADD(DAY, -3, @Today)),
  (120, 119, N'Bed charges (3 days)', N'Bed', 3, 18000.00, 54000.00, 54000.00, N'', DATEADD(DAY, -3, @Today)),
  (121, 119, N'Primary PCI with stent', N'Service', 1, 350000.00, 350000.00, 350000.00, N'', DATEADD(DAY, -3, @Today)),
  (122, 120, N'Admission consultation', N'Service', 1, 4000.00, 4000.00, 4000.00, N'', DATEADD(DAY, -6, @Today)),
  (123, 120, N'Bed charges (6 days)', N'Bed', 6, 3500.00, 21000.00, 21000.00, N'', DATEADD(DAY, -6, @Today)),
  (124, 120, N'Orthopaedic surgery (ORIF)', N'Service', 1, 85000.00, 85000.00, 85000.00, N'', DATEADD(DAY, -6, @Today)),
  (125, 121, N'Admission consultation', N'Service', 1, 4000.00, 4000.00, 4000.00, N'', DATEADD(DAY, -12, @Today)),
  (126, 121, N'Bed charges (4 days)', N'Bed', 4, 3500.00, 14000.00, 14000.00, N'', DATEADD(DAY, -12, @Today)),
  (127, 122, N'Admission consultation', N'Service', 1, 3500.00, 3500.00, 3500.00, N'', DATEADD(DAY, -2, @Today)),
  (128, 122, N'Bed charges (2 days)', N'Bed', 2, 6000.00, 12000.00, 12000.00, N'', DATEADD(DAY, -2, @Today)),
  (129, 122, N'Caesarean section', N'Service', 1, 95000.00, 95000.00, 95000.00, N'', DATEADD(DAY, -2, @Today)),
  (130, 123, N'Admission consultation', N'Service', 1, 3500.00, 3500.00, 3500.00, N'', DATEADD(DAY, -1, @Today)),
  (131, 123, N'Bed charges (1 day)', N'Bed', 1, 18000.00, 18000.00, 18000.00, N'', DATEADD(DAY, -1, @Today)),
  (132, 124, N'Admission consultation', N'Service', 1, 3500.00, 3500.00, 3500.00, N'', DATEADD(DAY, -9, @Today)),
  (133, 124, N'Bed charges (3 days)', N'Bed', 3, 3500.00, 10500.00, 10500.00, N'', DATEADD(DAY, -9, @Today)),
  (134, 125, N'Admission consultation', N'Service', 1, 3000.00, 3000.00, 3000.00, N'', DATEADD(DAY, -10, @Today)),
  (135, 125, N'Bed charges (3 days)', N'Bed', 3, 500.00, 1500.00, 1500.00, N'', DATEADD(DAY, -10, @Today))
) AS src([Id],[BillId],[ItemName],[ItemType],[Quantity],[UnitPrice],[TotalPrice],[Amount],[Description],[CreatedDate])
WHERE NOT EXISTS (SELECT 1 FROM [BillItems] x WHERE x.[Id] = src.[Id]) AND EXISTS (SELECT 1 FROM [Bills] b WHERE b.[Id] = src.[BillId]);
SET IDENTITY_INSERT [BillItems] OFF;

SET IDENTITY_INSERT [Payments] ON;
INSERT INTO [Payments] ([Id],[BillId],[PaymentMethod],[Amount],[TransactionId],[PaymentGateway],[Status],[Notes],[PaymentDate],[ProcessedBy])
SELECT * FROM (VALUES
  (101, 101, N'Cash', 2000.00, N'TXN-MAIN-101', N'', N'Completed', N'', DATEADD(MINUTE, 680, DATEADD(DAY, -14, @Today)), N'accounts.main'),
  (102, 102, N'Card', 2000.00, N'TXN-MAIN-102', N'', N'Completed', N'', DATEADD(MINUTE, 680, DATEADD(DAY, -10, @Today)), N'accounts.main'),
  (103, 103, N'Cash', 2500.00, N'TXN-MAIN-103', N'', N'Completed', N'', DATEADD(MINUTE, 680, DATEADD(DAY, -5, @Today)), N'accounts.main'),
  (104, 104, N'Cash', 2000.00, N'TXN-MAIN-104', N'', N'Completed', N'', DATEADD(MINUTE, 680, DATEADD(DAY, -1, @Today)), N'accounts.main'),
  (105, 106, N'Cash', 2000.00, N'TXN-MAIN-106', N'', N'Completed', N'', DATEADD(MINUTE, 680, DATEADD(DAY, -8, @Today)), N'accounts.main'),
  (106, 107, N'Cash', 3000.00, N'TXN-ISB-107', N'', N'Completed', N'', DATEADD(MINUTE, 680, DATEADD(DAY, -14, @Today)), N'accounts.isb'),
  (107, 108, N'Card', 3000.00, N'TXN-ISB-108', N'', N'Completed', N'', DATEADD(MINUTE, 680, DATEADD(DAY, -10, @Today)), N'accounts.isb'),
  (108, 109, N'Cash', 3500.00, N'TXN-ISB-109', N'', N'Completed', N'', DATEADD(MINUTE, 680, DATEADD(DAY, -5, @Today)), N'accounts.isb'),
  (109, 110, N'Cash', 3000.00, N'TXN-ISB-110', N'', N'Completed', N'', DATEADD(MINUTE, 680, DATEADD(DAY, -1, @Today)), N'accounts.isb'),
  (110, 112, N'Cash', 3000.00, N'TXN-ISB-112', N'', N'Completed', N'', DATEADD(MINUTE, 680, DATEADD(DAY, -8, @Today)), N'accounts.isb'),
  (111, 113, N'Cash', 2500.00, N'TXN-KHI-113', N'', N'Completed', N'', DATEADD(MINUTE, 680, DATEADD(DAY, -14, @Today)), N'accounts.khi'),
  (112, 114, N'Card', 2500.00, N'TXN-KHI-114', N'', N'Completed', N'', DATEADD(MINUTE, 680, DATEADD(DAY, -10, @Today)), N'accounts.khi'),
  (113, 115, N'Cash', 3000.00, N'TXN-KHI-115', N'', N'Completed', N'', DATEADD(MINUTE, 680, DATEADD(DAY, -5, @Today)), N'accounts.khi'),
  (114, 116, N'Cash', 2500.00, N'TXN-KHI-116', N'', N'Completed', N'', DATEADD(MINUTE, 680, DATEADD(DAY, -1, @Today)), N'accounts.khi'),
  (115, 118, N'Cash', 2500.00, N'TXN-KHI-118', N'', N'Completed', N'', DATEADD(MINUTE, 680, DATEADD(DAY, -8, @Today)), N'accounts.khi'),
  (116, 119, N'Card', 204000.00, N'TXN-ISB-IPD-101', N'', N'Completed', N'', DATEADD(MINUTE, 680, DATEADD(DAY, -3, @Today)), N'accounts.isb'),
  (117, 120, N'Card', 55000.00, N'TXN-ISB-IPD-102', N'', N'Completed', N'', DATEADD(MINUTE, 680, DATEADD(DAY, -6, @Today)), N'accounts.isb'),
  (118, 121, N'Insurance', 18000.00, N'TXN-ISB-IPD-103', N'', N'Completed', N'', DATEADD(MINUTE, 680, DATEADD(DAY, -8, @Today)), N'accounts.isb'),
  (119, 122, N'Card', 55000.00, N'TXN-KHI-IPD-104', N'', N'Completed', N'', DATEADD(MINUTE, 680, DATEADD(DAY, -2, @Today)), N'accounts.khi'),
  (120, 124, N'Card', 14000.00, N'TXN-KHI-IPD-106', N'', N'Completed', N'', DATEADD(MINUTE, 680, DATEADD(DAY, -6, @Today)), N'accounts.khi'),
  (121, 125, N'Card', 4500.00, N'TXN-MAIN-IPD-107', N'', N'Completed', N'', DATEADD(MINUTE, 680, DATEADD(DAY, -7, @Today)), N'accounts.main')
) AS src([Id],[BillId],[PaymentMethod],[Amount],[TransactionId],[PaymentGateway],[Status],[Notes],[PaymentDate],[ProcessedBy])
WHERE NOT EXISTS (SELECT 1 FROM [Payments] x WHERE x.[Id] = src.[Id]) AND EXISTS (SELECT 1 FROM [Bills] b WHERE b.[Id] = src.[BillId]);
SET IDENTITY_INSERT [Payments] OFF;

SET IDENTITY_INSERT [PharmacyBills] ON;
INSERT INTO [PharmacyBills] ([Id],[BillNumber],[PatientId],[BillDate],[TotalAmount],[PaidAmount],[Status],[PaymentMethod],[Notes],[CreatedDate],[CreatedBy],[HospitalId])
SELECT * FROM (VALUES
  (101, N'RX-MAIN-0101', 101, DATEADD(MINUTE, 760, DATEADD(DAY, -13, @Today)), 100.50, 100.50, N'Paid', N'Cash', N'', DATEADD(MINUTE, 760, DATEADD(DAY, -13, @Today)), N'pharmacy.main', @HMain),
  (102, N'RX-MAIN-0102', 102, DATEADD(MINUTE, 760, DATEADD(DAY, -4, @Today)), 495.00, 495.00, N'Paid', N'Cash', N'', DATEADD(MINUTE, 760, DATEADD(DAY, -4, @Today)), N'pharmacy.main', @HMain),
  (103, N'RX-MAIN-0103', 1, DATEADD(MINUTE, 760, DATEADD(DAY, -1, @Today)), 250.00, 250.00, N'Paid', N'Card', N'', DATEADD(MINUTE, 760, DATEADD(DAY, -1, @Today)), N'pharmacy.main', @HMain),
  (104, N'RX-ISB-0104', 104, DATEADD(MINUTE, 760, DATEADD(DAY, -13, @Today)), 100.50, 100.50, N'Paid', N'Cash', N'', DATEADD(MINUTE, 760, DATEADD(DAY, -13, @Today)), N'pharmacy.isb', @HIsb),
  (105, N'RX-ISB-0105', 105, DATEADD(MINUTE, 760, DATEADD(DAY, -4, @Today)), 495.00, 495.00, N'Paid', N'Cash', N'', DATEADD(MINUTE, 760, DATEADD(DAY, -4, @Today)), N'pharmacy.isb', @HIsb),
  (106, N'RX-ISB-0106', 3, DATEADD(MINUTE, 760, DATEADD(DAY, -1, @Today)), 250.00, 250.00, N'Paid', N'Card', N'', DATEADD(MINUTE, 760, DATEADD(DAY, -1, @Today)), N'pharmacy.isb', @HIsb),
  (107, N'RX-KHI-0107', 107, DATEADD(MINUTE, 760, DATEADD(DAY, -13, @Today)), 100.50, 100.50, N'Paid', N'Cash', N'', DATEADD(MINUTE, 760, DATEADD(DAY, -13, @Today)), N'pharmacy.khi', @HKhi),
  (108, N'RX-KHI-0108', 108, DATEADD(MINUTE, 760, DATEADD(DAY, -4, @Today)), 495.00, 0.00, N'Pending', N'Cash', N'', DATEADD(MINUTE, 760, DATEADD(DAY, -4, @Today)), N'pharmacy.khi', @HKhi),
  (109, N'RX-KHI-0109', 110, DATEADD(MINUTE, 760, DATEADD(DAY, -1, @Today)), 250.00, 250.00, N'Paid', N'Card', N'', DATEADD(MINUTE, 760, DATEADD(DAY, -1, @Today)), N'pharmacy.khi', @HKhi)
) AS src([Id],[BillNumber],[PatientId],[BillDate],[TotalAmount],[PaidAmount],[Status],[PaymentMethod],[Notes],[CreatedDate],[CreatedBy],[HospitalId])
WHERE NOT EXISTS (SELECT 1 FROM [PharmacyBills] x WHERE x.[Id] = src.[Id]);
SET IDENTITY_INSERT [PharmacyBills] OFF;

SET IDENTITY_INSERT [Prescriptions] ON;
INSERT INTO [Prescriptions] ([Id],[PharmacyBillId],[MedicineId],[Dosage],[Frequency],[Duration],[Quantity],[UnitPrice],[TotalPrice],[Instructions],[CreatedDate])
SELECT * FROM (VALUES
  (101, 101, 1, N'As directed', N'BD', 7, 15, 3.50, 52.50, N'After food', DATEADD(DAY, -13, @Today)),
  (102, 101, 12, N'As directed', N'BD', 7, 6, 8.00, 48.00, N'After food', DATEADD(DAY, -13, @Today)),
  (103, 102, 2, N'As directed', N'BD', 7, 60, 7.00, 420.00, N'After food', DATEADD(DAY, -4, @Today)),
  (104, 102, 5, N'As directed', N'BD', 7, 30, 2.50, 75.00, N'After food', DATEADD(DAY, -4, @Today)),
  (105, 103, 8, N'As directed', N'BD', 7, 10, 6.00, 60.00, N'After food', DATEADD(DAY, -1, @Today)),
  (106, 103, 7, N'As directed', N'BD', 7, 14, 5.00, 70.00, N'After food', DATEADD(DAY, -1, @Today)),
  (107, 103, 6, N'As directed', N'BD', 7, 1, 120.00, 120.00, N'After food', DATEADD(DAY, -1, @Today)),
  (108, 104, 1, N'As directed', N'BD', 7, 15, 3.50, 52.50, N'After food', DATEADD(DAY, -13, @Today)),
  (109, 104, 12, N'As directed', N'BD', 7, 6, 8.00, 48.00, N'After food', DATEADD(DAY, -13, @Today)),
  (110, 105, 2, N'As directed', N'BD', 7, 60, 7.00, 420.00, N'After food', DATEADD(DAY, -4, @Today)),
  (111, 105, 5, N'As directed', N'BD', 7, 30, 2.50, 75.00, N'After food', DATEADD(DAY, -4, @Today)),
  (112, 106, 8, N'As directed', N'BD', 7, 10, 6.00, 60.00, N'After food', DATEADD(DAY, -1, @Today)),
  (113, 106, 7, N'As directed', N'BD', 7, 14, 5.00, 70.00, N'After food', DATEADD(DAY, -1, @Today)),
  (114, 106, 6, N'As directed', N'BD', 7, 1, 120.00, 120.00, N'After food', DATEADD(DAY, -1, @Today)),
  (115, 107, 1, N'As directed', N'BD', 7, 15, 3.50, 52.50, N'After food', DATEADD(DAY, -13, @Today)),
  (116, 107, 12, N'As directed', N'BD', 7, 6, 8.00, 48.00, N'After food', DATEADD(DAY, -13, @Today)),
  (117, 108, 2, N'As directed', N'BD', 7, 60, 7.00, 420.00, N'After food', DATEADD(DAY, -4, @Today)),
  (118, 108, 5, N'As directed', N'BD', 7, 30, 2.50, 75.00, N'After food', DATEADD(DAY, -4, @Today)),
  (119, 109, 8, N'As directed', N'BD', 7, 10, 6.00, 60.00, N'After food', DATEADD(DAY, -1, @Today)),
  (120, 109, 7, N'As directed', N'BD', 7, 14, 5.00, 70.00, N'After food', DATEADD(DAY, -1, @Today)),
  (121, 109, 6, N'As directed', N'BD', 7, 1, 120.00, 120.00, N'After food', DATEADD(DAY, -1, @Today))
) AS src([Id],[PharmacyBillId],[MedicineId],[Dosage],[Frequency],[Duration],[Quantity],[UnitPrice],[TotalPrice],[Instructions],[CreatedDate])
WHERE NOT EXISTS (SELECT 1 FROM [Prescriptions] x WHERE x.[Id] = src.[Id]) AND EXISTS (SELECT 1 FROM [PharmacyBills] b WHERE b.[Id] = src.[PharmacyBillId]);
SET IDENTITY_INSERT [Prescriptions] OFF;
GO

-- ============================================================
-- 29. INVENTORY – vendors, stock items per hospital, stock movements and purchase bills
--     Each hospital has its own store (12 items) with opening stock, issues to departments/patients and a
--     received purchase bill; a second purchase bill is approved and waiting for delivery. Some items are
--     below the reorder level or out of stock so the stock reports show every status. Amounts in PKR.
-- ============================================================
DECLARE @Today datetime2 = CAST(CAST(SYSDATETIME() AS date) AS datetime2);
DECLARE @HMain int = COALESCE((SELECT TOP (1) [Id] FROM [Hospitals] WHERE [Code] = N'MAIN'), (SELECT TOP (1) [Id] FROM [Hospitals] WHERE [IsDefault] = 1 ORDER BY [Id]));
DECLARE @HIsb int = (SELECT TOP (1) [Id] FROM [Hospitals] WHERE [Code] = N'ISB');
DECLARE @HKhi int = (SELECT TOP (1) [Id] FROM [Hospitals] WHERE [Code] = N'KHI');

SET IDENTITY_INSERT [Vendors] ON;
INSERT INTO [Vendors] ([Id],[VendorCode],[Name],[Category],[ContactPerson],[Phone],[Email],[Address],[City],[TaxNumber],[LicenseNumber],[PaymentTermsDays],[BankName],[BankAccountName],[BankAccountNumber],[BankRoutingCode],[Rating],[IsActive],[Notes],[CreatedAt],[CreatedBy],[UpdatedAt])
SELECT * FROM (VALUES
  (101, N'V-PHARMA-01', N'Getz Pharma Distribution', N'Pharmaceuticals', N'Adeel Ahmed', N'+92 330 2108660', N'v-pharma-01@vendor.pk', N'Office 10, Trade Centre', N'Karachi', N'NTN-4410020', N'DRAP-8800', 30, N'Meezan Bank', N'Getz Pharma Distribution', N'PK36MEZN00010045600', N'MEZNPKKA', 5, 1, N'Demo vendor', DATEADD(MONTH, -12, @Today), N'SeedDemoData.sql', NULL),
  (102, N'V-SURG-01', N'Medi-Surge Supplies', N'Surgical consumables', N'Rashid Mehmood', N'+92 331 2116579', N'v-surg-01@vendor.pk', N'Office 11, Trade Centre', N'Lahore', N'NTN-4410057', N'DRAP-8801', 30, N'Meezan Bank', N'Medi-Surge Supplies', N'PK37MEZN00010046513', N'MEZNPKKA', 4, 1, N'Demo vendor', DATEADD(MONTH, -12, @Today), N'SeedDemoData.sql', NULL),
  (103, N'V-LAB-01', N'Lab Science Pakistan', N'Laboratory supplies', N'Saima Gul', N'+92 332 2124498', N'v-lab-01@vendor.pk', N'Office 12, Trade Centre', N'Islamabad', N'NTN-4410094', N'DRAP-8802', 45, N'Meezan Bank', N'Lab Science Pakistan', N'PK38MEZN00010047426', N'MEZNPKKA', 4, 1, N'Demo vendor', DATEADD(MONTH, -12, @Today), N'SeedDemoData.sql', NULL),
  (104, N'V-HOUSE-01', N'CleanCare Linen & Hygiene', N'Housekeeping', N'Javed Akhtar', N'+92 333 2132417', N'v-house-01@vendor.pk', N'Office 13, Trade Centre', N'Lahore', N'NTN-4410131', N'DRAP-8803', 15, N'Meezan Bank', N'CleanCare Linen & Hygiene', N'PK39MEZN00010048339', N'MEZNPKKA', 3, 1, N'Demo vendor', DATEADD(MONTH, -12, @Today), N'SeedDemoData.sql', NULL),
  (105, N'V-OFFICE-01', N'Office World Traders', N'Stationery & printing', N'Kashif Ali', N'+92 334 2140336', N'v-office-01@vendor.pk', N'Office 14, Trade Centre', N'Karachi', N'NTN-4410168', N'DRAP-8804', 15, N'Meezan Bank', N'Office World Traders', N'PK40MEZN00010049252', N'MEZNPKKA', 4, 1, N'Demo vendor', DATEADD(MONTH, -12, @Today), N'SeedDemoData.sql', NULL)
) AS src([Id],[VendorCode],[Name],[Category],[ContactPerson],[Phone],[Email],[Address],[City],[TaxNumber],[LicenseNumber],[PaymentTermsDays],[BankName],[BankAccountName],[BankAccountNumber],[BankRoutingCode],[Rating],[IsActive],[Notes],[CreatedAt],[CreatedBy],[UpdatedAt])
WHERE NOT EXISTS (SELECT 1 FROM [Vendors] x WHERE x.[Id] = src.[Id] OR x.[VendorCode] = src.[VendorCode]);
SET IDENTITY_INSERT [Vendors] OFF;

SET IDENTITY_INSERT [InventoryItems] ON;
INSERT INTO [InventoryItems] ([Id],[ItemCode],[Name],[Category],[Unit],[CurrentStock],[MinimumStock],[ReorderLevel],[UnitCost],[Supplier],[StorageLocation],[IsActive],[CreatedDate],[HospitalId],[VendorId])
SELECT * FROM (VALUES
  (101, N'MAIN-GLV', N'Surgical gloves (box of 100)', N'Surgical consumables', N'box', 40, 5, 10, 1450.00, N'Medi-Surge Supplies', N'Main store, rack 6', 1, DATEADD(DAY, -40, @Today), @HMain, 102),
  (102, N'MAIN-MSK', N'Face masks 3-ply (box of 50)', N'Surgical consumables', N'box', 25, 8, 15, 650.00, N'Medi-Surge Supplies', N'Main store, rack 1', 1, DATEADD(DAY, -40, @Today), @HMain, 102),
  (103, N'MAIN-SYR', N'Syringes 5 ml (box of 100)', N'Surgical consumables', N'box', 29, 4, 8, 1800.00, N'Medi-Surge Supplies', N'Main store, rack 2', 1, DATEADD(DAY, -40, @Today), @HMain, 102),
  (104, N'MAIN-CAN', N'IV cannula 20G', N'Surgical consumables', N'pcs', 390, 50, 100, 95.00, N'Medi-Surge Supplies', N'Main store, rack 3', 1, DATEADD(DAY, -40, @Today), @HMain, 102),
  (105, N'MAIN-GAU', N'Gauze swabs (pack of 100)', N'Surgical consumables', N'pack', 34, 10, 20, 520.00, N'Medi-Surge Supplies', N'Main store, rack 4', 1, DATEADD(DAY, -40, @Today), @HMain, 102),
  (106, N'MAIN-SAN', N'Hand sanitizer 500 ml', N'Housekeeping', N'bottle', 17, 6, 12, 780.00, N'CleanCare Linen & Hygiene', N'Main store, rack 5', 1, DATEADD(DAY, -40, @Today), @HMain, 104),
  (107, N'MAIN-SHT', N'Bed sheets', N'Linen', N'pcs', 15, 10, 20, 1650.00, N'CleanCare Linen & Hygiene', N'Linen room', 1, DATEADD(DAY, -40, @Today), @HMain, 104),
  (108, N'MAIN-TUB', N'Blood collection tubes EDTA (box of 100)', N'Laboratory supplies', N'box', 4, 3, 5, 2400.00, N'Lab Science Pakistan', N'Main store, rack 1', 1, DATEADD(DAY, -40, @Today), @HMain, 103),
  (109, N'MAIN-SWB', N'Alcohol swabs (box of 200)', N'Surgical consumables', N'box', 16, 5, 10, 450.00, N'Medi-Surge Supplies', N'Main store, rack 2', 1, DATEADD(DAY, -40, @Today), @HMain, 102),
  (110, N'MAIN-PPR', N'A4 paper (ream)', N'Stationery', N'ream', 22, 5, 10, 1350.00, N'Office World Traders', N'Admin store', 1, DATEADD(DAY, -40, @Today), @HMain, 105),
  (111, N'MAIN-ROL', N'Thermal receipt roll 80 mm', N'Stationery', N'roll', 50, 15, 30, 120.00, N'Office World Traders', N'Admin store', 1, DATEADD(DAY, -40, @Today), @HMain, 105),
  (112, N'MAIN-OXY', N'Oxygen mask (adult)', N'Surgical consumables', N'pcs', 30, 10, 25, 380.00, N'Medi-Surge Supplies', N'Main store, rack 5', 1, DATEADD(DAY, -40, @Today), @HMain, 102),
  (113, N'ISB-GLV', N'Surgical gloves (box of 100)', N'Surgical consumables', N'box', 40, 5, 10, 1450.00, N'Medi-Surge Supplies', N'Main store, rack 6', 1, DATEADD(DAY, -40, @Today), @HIsb, 102),
  (114, N'ISB-MSK', N'Face masks 3-ply (box of 50)', N'Surgical consumables', N'box', 25, 8, 15, 650.00, N'Medi-Surge Supplies', N'Main store, rack 1', 1, DATEADD(DAY, -40, @Today), @HIsb, 102),
  (115, N'ISB-SYR', N'Syringes 5 ml (box of 100)', N'Surgical consumables', N'box', 29, 4, 8, 1800.00, N'Medi-Surge Supplies', N'Main store, rack 2', 1, DATEADD(DAY, -40, @Today), @HIsb, 102),
  (116, N'ISB-CAN', N'IV cannula 20G', N'Surgical consumables', N'pcs', 390, 50, 100, 95.00, N'Medi-Surge Supplies', N'Main store, rack 3', 1, DATEADD(DAY, -40, @Today), @HIsb, 102),
  (117, N'ISB-GAU', N'Gauze swabs (pack of 100)', N'Surgical consumables', N'pack', 34, 10, 20, 520.00, N'Medi-Surge Supplies', N'Main store, rack 4', 1, DATEADD(DAY, -40, @Today), @HIsb, 102),
  (118, N'ISB-SAN', N'Hand sanitizer 500 ml', N'Housekeeping', N'bottle', 17, 6, 12, 780.00, N'CleanCare Linen & Hygiene', N'Main store, rack 5', 1, DATEADD(DAY, -40, @Today), @HIsb, 104),
  (119, N'ISB-SHT', N'Bed sheets', N'Linen', N'pcs', 60, 10, 20, 1650.00, N'CleanCare Linen & Hygiene', N'Linen room', 1, DATEADD(DAY, -40, @Today), @HIsb, 104),
  (120, N'ISB-TUB', N'Blood collection tubes EDTA (box of 100)', N'Laboratory supplies', N'box', 0, 3, 5, 2400.00, N'Lab Science Pakistan', N'Main store, rack 1', 1, DATEADD(DAY, -40, @Today), @HIsb, 103),
  (121, N'ISB-SWB', N'Alcohol swabs (box of 200)', N'Surgical consumables', N'box', 16, 5, 10, 450.00, N'Medi-Surge Supplies', N'Main store, rack 2', 1, DATEADD(DAY, -40, @Today), @HIsb, 102),
  (122, N'ISB-PPR', N'A4 paper (ream)', N'Stationery', N'ream', 22, 5, 10, 1350.00, N'Office World Traders', N'Admin store', 1, DATEADD(DAY, -40, @Today), @HIsb, 105),
  (123, N'ISB-ROL', N'Thermal receipt roll 80 mm', N'Stationery', N'roll', 50, 15, 30, 120.00, N'Office World Traders', N'Admin store', 1, DATEADD(DAY, -40, @Today), @HIsb, 105),
  (124, N'ISB-OXY', N'Oxygen mask (adult)', N'Surgical consumables', N'pcs', 30, 10, 25, 380.00, N'Medi-Surge Supplies', N'Main store, rack 5', 1, DATEADD(DAY, -40, @Today), @HIsb, 102),
  (125, N'KHI-GLV', N'Surgical gloves (box of 100)', N'Surgical consumables', N'box', 23, 5, 10, 1450.00, N'Medi-Surge Supplies', N'Main store, rack 6', 1, DATEADD(DAY, -40, @Today), @HKhi, 102),
  (126, N'KHI-MSK', N'Face masks 3-ply (box of 50)', N'Surgical consumables', N'box', 25, 8, 15, 650.00, N'Medi-Surge Supplies', N'Main store, rack 1', 1, DATEADD(DAY, -40, @Today), @HKhi, 102),
  (127, N'KHI-SYR', N'Syringes 5 ml (box of 100)', N'Surgical consumables', N'box', 29, 4, 8, 1800.00, N'Medi-Surge Supplies', N'Main store, rack 2', 1, DATEADD(DAY, -40, @Today), @HKhi, 102),
  (128, N'KHI-CAN', N'IV cannula 20G', N'Surgical consumables', N'pcs', 390, 50, 100, 95.00, N'Medi-Surge Supplies', N'Main store, rack 3', 1, DATEADD(DAY, -40, @Today), @HKhi, 102),
  (129, N'KHI-GAU', N'Gauze swabs (pack of 100)', N'Surgical consumables', N'pack', 34, 10, 20, 520.00, N'Medi-Surge Supplies', N'Main store, rack 4', 1, DATEADD(DAY, -40, @Today), @HKhi, 102),
  (130, N'KHI-SAN', N'Hand sanitizer 500 ml', N'Housekeeping', N'bottle', 17, 6, 12, 780.00, N'CleanCare Linen & Hygiene', N'Main store, rack 5', 1, DATEADD(DAY, -40, @Today), @HKhi, 104),
  (131, N'KHI-SHT', N'Bed sheets', N'Linen', N'pcs', 60, 10, 20, 1650.00, N'CleanCare Linen & Hygiene', N'Linen room', 1, DATEADD(DAY, -40, @Today), @HKhi, 104),
  (132, N'KHI-TUB', N'Blood collection tubes EDTA (box of 100)', N'Laboratory supplies', N'box', 4, 3, 5, 2400.00, N'Lab Science Pakistan', N'Main store, rack 1', 1, DATEADD(DAY, -40, @Today), @HKhi, 103),
  (133, N'KHI-SWB', N'Alcohol swabs (box of 200)', N'Surgical consumables', N'box', 16, 5, 10, 450.00, N'Medi-Surge Supplies', N'Main store, rack 2', 1, DATEADD(DAY, -40, @Today), @HKhi, 102),
  (134, N'KHI-PPR', N'A4 paper (ream)', N'Stationery', N'ream', 22, 5, 10, 1350.00, N'Office World Traders', N'Admin store', 1, DATEADD(DAY, -40, @Today), @HKhi, 105),
  (135, N'KHI-ROL', N'Thermal receipt roll 80 mm', N'Stationery', N'roll', 10, 15, 30, 120.00, N'Office World Traders', N'Admin store', 1, DATEADD(DAY, -40, @Today), @HKhi, 105),
  (136, N'KHI-OXY', N'Oxygen mask (adult)', N'Surgical consumables', N'pcs', 30, 10, 25, 380.00, N'Medi-Surge Supplies', N'Main store, rack 5', 1, DATEADD(DAY, -40, @Today), @HKhi, 102)
) AS src([Id],[ItemCode],[Name],[Category],[Unit],[CurrentStock],[MinimumStock],[ReorderLevel],[UnitCost],[Supplier],[StorageLocation],[IsActive],[CreatedDate],[HospitalId],[VendorId])
WHERE NOT EXISTS (SELECT 1 FROM [InventoryItems] x WHERE x.[Id] = src.[Id]);
SET IDENTITY_INSERT [InventoryItems] OFF;

DECLARE @Store nvarchar(450) = (SELECT TOP (1) [Id] FROM [AspNetUsers] WHERE [NormalizedUserName] IN (N'PHARMACY.MAIN', N'TESTER') ORDER BY [NormalizedUserName]);
SET IDENTITY_INSERT [PurchaseBills] ON;
INSERT INTO [PurchaseBills] ([Id],[HospitalId],[BillNumber],[VendorId],[VendorInvoiceNumber],[InvoiceDate],[DueDate],[Status],[PaymentStatus],[SubTotal],[TaxAmount],[TotalAmount],[PaidAmount],[Notes],[CreatedByUserId],[CreatedBy],[CreatedAt],[SubmittedAt],[ApprovedBy],[ApprovedAt],[ApprovalComments],[ReceivedBy],[ReceivedAt],[CancelReason])
SELECT * FROM (VALUES
  (101, @HMain, N'PB-MAIN-0001', 102, N'INV-MS-4501', DATEADD(DAY, -13, @Today), DATEADD(DAY, 4, @Today), N'Received', N'Paid', 66000.00, 0.00, 66000.00, 66000.00, N'Demo purchase', ISNULL(@Store, N''), N'admin.main', DATEADD(DAY, -13, @Today), DATEADD(DAY, -13, @Today), N'admin.main', DATEADD(DAY, -13, @Today), N'Approved', N'pharmacy.main', DATEADD(DAY, -12, @Today), N''),
  (102, @HMain, N'PB-MAIN-0002', 105, N'INV-OW-7802', DATEADD(DAY, -2, @Today), DATEADD(DAY, 11, @Today), N'Approved', N'Unpaid', 39000.00, 0.00, 39000.00, 0.00, N'Demo purchase', ISNULL(@Store, N''), N'admin.main', DATEADD(DAY, -2, @Today), DATEADD(DAY, -2, @Today), N'admin.main', DATEADD(DAY, -2, @Today), N'Approved', N'', NULL, N''),
  (103, @HIsb, N'PB-ISB-0001', 102, N'INV-MS-4503', DATEADD(DAY, -13, @Today), DATEADD(DAY, 4, @Today), N'Received', N'Paid', 66000.00, 0.00, 66000.00, 66000.00, N'Demo purchase', ISNULL(@Store, N''), N'admin.isb', DATEADD(DAY, -13, @Today), DATEADD(DAY, -13, @Today), N'admin.isb', DATEADD(DAY, -13, @Today), N'Approved', N'pharmacy.isb', DATEADD(DAY, -12, @Today), N''),
  (104, @HIsb, N'PB-ISB-0002', 105, N'INV-OW-7804', DATEADD(DAY, -2, @Today), DATEADD(DAY, 11, @Today), N'Approved', N'Unpaid', 39000.00, 0.00, 39000.00, 0.00, N'Demo purchase', ISNULL(@Store, N''), N'admin.isb', DATEADD(DAY, -2, @Today), DATEADD(DAY, -2, @Today), N'admin.isb', DATEADD(DAY, -2, @Today), N'Approved', N'', NULL, N''),
  (105, @HKhi, N'PB-KHI-0001', 102, N'INV-MS-4505', DATEADD(DAY, -13, @Today), DATEADD(DAY, 4, @Today), N'Received', N'Partially paid', 66000.00, 0.00, 66000.00, 33000.00, N'Demo purchase', ISNULL(@Store, N''), N'admin.khi', DATEADD(DAY, -13, @Today), DATEADD(DAY, -13, @Today), N'admin.khi', DATEADD(DAY, -13, @Today), N'Approved', N'pharmacy.khi', DATEADD(DAY, -12, @Today), N''),
  (106, @HKhi, N'PB-KHI-0002', 105, N'INV-OW-7806', DATEADD(DAY, -2, @Today), DATEADD(DAY, 11, @Today), N'Approved', N'Unpaid', 39000.00, 0.00, 39000.00, 0.00, N'Demo purchase', ISNULL(@Store, N''), N'admin.khi', DATEADD(DAY, -2, @Today), DATEADD(DAY, -2, @Today), N'admin.khi', DATEADD(DAY, -2, @Today), N'Approved', N'', NULL, N'')
) AS src([Id],[HospitalId],[BillNumber],[VendorId],[VendorInvoiceNumber],[InvoiceDate],[DueDate],[Status],[PaymentStatus],[SubTotal],[TaxAmount],[TotalAmount],[PaidAmount],[Notes],[CreatedByUserId],[CreatedBy],[CreatedAt],[SubmittedAt],[ApprovedBy],[ApprovedAt],[ApprovalComments],[ReceivedBy],[ReceivedAt],[CancelReason])
WHERE NOT EXISTS (SELECT 1 FROM [PurchaseBills] x WHERE x.[Id] = src.[Id] OR x.[BillNumber] = src.[BillNumber]);
SET IDENTITY_INSERT [PurchaseBills] OFF;

SET IDENTITY_INSERT [PurchaseBillItems] ON;
INSERT INTO [PurchaseBillItems] ([Id],[PurchaseBillId],[InventoryItemId],[Description],[Quantity],[UnitCost],[TaxPercent],[LineTotal],[BatchNumber],[ExpiryDate])
SELECT * FROM (VALUES
  (101, 101, 101, N'Surgical gloves (box of 100)', 20, 1450.00, 0, 29000.00, N'B24101', DATEADD(YEAR, 2, @Today)),
  (102, 101, 103, N'Syringes 5 ml (box of 100)', 10, 1800.00, 0, 18000.00, N'B24102', DATEADD(YEAR, 2, @Today)),
  (103, 101, 104, N'IV cannula 20G', 200, 95.00, 0, 19000.00, N'B24103', DATEADD(YEAR, 2, @Today)),
  (104, 102, 110, N'A4 paper (ream)', 20, 1350.00, 0, 27000.00, N'B24104', DATEADD(YEAR, 2, @Today)),
  (105, 102, 111, N'Thermal receipt roll 80 mm', 100, 120.00, 0, 12000.00, N'B24105', DATEADD(YEAR, 2, @Today)),
  (106, 103, 113, N'Surgical gloves (box of 100)', 20, 1450.00, 0, 29000.00, N'B24106', DATEADD(YEAR, 2, @Today)),
  (107, 103, 115, N'Syringes 5 ml (box of 100)', 10, 1800.00, 0, 18000.00, N'B24107', DATEADD(YEAR, 2, @Today)),
  (108, 103, 116, N'IV cannula 20G', 200, 95.00, 0, 19000.00, N'B24108', DATEADD(YEAR, 2, @Today)),
  (109, 104, 122, N'A4 paper (ream)', 20, 1350.00, 0, 27000.00, N'B24109', DATEADD(YEAR, 2, @Today)),
  (110, 104, 123, N'Thermal receipt roll 80 mm', 100, 120.00, 0, 12000.00, N'B24110', DATEADD(YEAR, 2, @Today)),
  (111, 105, 125, N'Surgical gloves (box of 100)', 20, 1450.00, 0, 29000.00, N'B24111', DATEADD(YEAR, 2, @Today)),
  (112, 105, 127, N'Syringes 5 ml (box of 100)', 10, 1800.00, 0, 18000.00, N'B24112', DATEADD(YEAR, 2, @Today)),
  (113, 105, 128, N'IV cannula 20G', 200, 95.00, 0, 19000.00, N'B24113', DATEADD(YEAR, 2, @Today)),
  (114, 106, 134, N'A4 paper (ream)', 20, 1350.00, 0, 27000.00, N'B24114', DATEADD(YEAR, 2, @Today)),
  (115, 106, 135, N'Thermal receipt roll 80 mm', 100, 120.00, 0, 12000.00, N'B24115', DATEADD(YEAR, 2, @Today))
) AS src([Id],[PurchaseBillId],[InventoryItemId],[Description],[Quantity],[UnitCost],[TaxPercent],[LineTotal],[BatchNumber],[ExpiryDate])
WHERE NOT EXISTS (SELECT 1 FROM [PurchaseBillItems] x WHERE x.[Id] = src.[Id]) AND EXISTS (SELECT 1 FROM [PurchaseBills] b WHERE b.[Id] = src.[PurchaseBillId]);
SET IDENTITY_INSERT [PurchaseBillItems] OFF;

SET IDENTITY_INSERT [VendorPayments] ON;
INSERT INTO [VendorPayments] ([Id],[PurchaseBillId],[Amount],[PaymentDate],[Method],[Reference],[Notes],[CreatedBy],[CreatedAt])
SELECT * FROM (VALUES
  (101, 101, 66000.00, DATEADD(MINUTE, 900, DATEADD(DAY, -10, @Today)), N'Bank transfer', N'IBFT-MAIN-101', N'', N'SeedDemoData.sql', DATEADD(MINUTE, 900, DATEADD(DAY, -10, @Today))),
  (102, 103, 66000.00, DATEADD(MINUTE, 900, DATEADD(DAY, -10, @Today)), N'Bank transfer', N'IBFT-ISB-103', N'', N'SeedDemoData.sql', DATEADD(MINUTE, 900, DATEADD(DAY, -10, @Today))),
  (103, 105, 33000.00, DATEADD(MINUTE, 900, DATEADD(DAY, -10, @Today)), N'Bank transfer', N'IBFT-KHI-105', N'', N'SeedDemoData.sql', DATEADD(MINUTE, 900, DATEADD(DAY, -10, @Today)))
) AS src([Id],[PurchaseBillId],[Amount],[PaymentDate],[Method],[Reference],[Notes],[CreatedBy],[CreatedAt])
WHERE NOT EXISTS (SELECT 1 FROM [VendorPayments] x WHERE x.[Id] = src.[Id]) AND EXISTS (SELECT 1 FROM [PurchaseBills] b WHERE b.[Id] = src.[PurchaseBillId]);
SET IDENTITY_INSERT [VendorPayments] OFF;

SET IDENTITY_INSERT [InventoryTransactions] ON;
INSERT INTO [InventoryTransactions] ([Id],[InventoryItemId],[TransactionType],[Quantity],[UnitCost],[ReferenceNumber],[Remarks],[PerformedByUserId],[TransactionDate],[Department],[PatientId],[BillId],[PurchaseBillId])
SELECT * FROM (VALUES
  (101, 101, N'IN', 40, 1450.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (102, 101, N'OUT', 12, 1450.00, N'ISS-MAIN-103', N'Issued to OT', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -9, @Today)), N'OT', NULL, NULL, NULL),
  (103, 101, N'OUT', 8, 1450.00, N'ISS-MAIN-104', N'Issued to Emergency', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -3, @Today)), N'Emergency', NULL, NULL, NULL),
  (104, 102, N'IN', 50, 650.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (105, 102, N'OUT', 15, 650.00, N'ISS-MAIN-106', N'Issued to Wards', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -8, @Today)), N'Wards', NULL, NULL, NULL),
  (106, 102, N'OUT', 10, 650.00, N'ISS-MAIN-107', N'Issued to Front Office', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -2, @Today)), N'Front Office', NULL, NULL, NULL),
  (107, 103, N'IN', 30, 1800.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (108, 103, N'OUT', 6, 1800.00, N'ISS-MAIN-109', N'Issued to Wards', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -7, @Today)), N'Wards', NULL, NULL, NULL),
  (109, 103, N'OUT', 5, 1800.00, N'ISS-MAIN-110', N'Issued to Emergency', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -1, @Today)), N'Emergency', NULL, NULL, NULL),
  (110, 104, N'IN', 400, 95.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (111, 104, N'OUT', 120, 95.00, N'ISS-MAIN-112', N'Issued to Wards', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -6, @Today)), N'Wards', 101, NULL, NULL),
  (112, 104, N'OUT', 90, 95.00, N'ISS-MAIN-113', N'Issued to ICU', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -2, @Today)), N'ICU', NULL, NULL, NULL),
  (113, 105, N'IN', 60, 520.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (114, 105, N'OUT', 14, 520.00, N'ISS-MAIN-115', N'Issued to OT', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -9, @Today)), N'OT', NULL, NULL, NULL),
  (115, 105, N'OUT', 12, 520.00, N'ISS-MAIN-116', N'Issued to Wards', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -4, @Today)), N'Wards', NULL, NULL, NULL),
  (116, 106, N'IN', 36, 780.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (117, 106, N'OUT', 10, 780.00, N'ISS-MAIN-118', N'Issued to Wards', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -10, @Today)), N'Wards', NULL, NULL, NULL),
  (118, 106, N'OUT', 9, 780.00, N'ISS-MAIN-119', N'Issued to Front Office', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -3, @Today)), N'Front Office', NULL, NULL, NULL),
  (119, 107, N'IN', 80, 1650.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (120, 107, N'OUT', 20, 1650.00, N'ISS-MAIN-121', N'Issued to Wards', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -5, @Today)), N'Wards', NULL, NULL, NULL),
  (121, 107, N'OUT', 45, 1650.00, N'ISS-MAIN-122', N'Issued to Wards', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -1, @Today)), N'Wards', NULL, NULL, NULL),
  (122, 108, N'IN', 15, 2400.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (123, 108, N'OUT', 6, 2400.00, N'ISS-MAIN-124', N'Issued to Laboratory', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -8, @Today)), N'Laboratory', NULL, NULL, NULL),
  (124, 108, N'OUT', 5, 2400.00, N'ISS-MAIN-125', N'Issued to Laboratory', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -2, @Today)), N'Laboratory', NULL, NULL, NULL),
  (125, 109, N'IN', 30, 450.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (126, 109, N'OUT', 8, 450.00, N'ISS-MAIN-127', N'Issued to Laboratory', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -6, @Today)), N'Laboratory', NULL, NULL, NULL),
  (127, 109, N'OUT', 6, 450.00, N'ISS-MAIN-128', N'Issued to Wards', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -1, @Today)), N'Wards', NULL, NULL, NULL),
  (128, 110, N'IN', 40, 1350.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (129, 110, N'OUT', 12, 1350.00, N'ISS-MAIN-130', N'Issued to Administration', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -7, @Today)), N'Administration', NULL, NULL, NULL),
  (130, 110, N'OUT', 6, 1350.00, N'ISS-MAIN-131', N'Issued to Front Office', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -2, @Today)), N'Front Office', NULL, NULL, NULL),
  (131, 111, N'IN', 120, 120.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (132, 111, N'OUT', 40, 120.00, N'ISS-MAIN-133', N'Issued to Front Office', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -6, @Today)), N'Front Office', NULL, NULL, NULL),
  (133, 111, N'OUT', 30, 120.00, N'ISS-MAIN-134', N'Issued to Pharmacy', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -1, @Today)), N'Pharmacy', NULL, NULL, NULL),
  (134, 112, N'IN', 60, 380.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (135, 112, N'OUT', 18, 380.00, N'ISS-MAIN-136', N'Issued to ICU', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -4, @Today)), N'ICU', 101, NULL, NULL),
  (136, 112, N'OUT', 12, 380.00, N'ISS-MAIN-137', N'Issued to Emergency', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -1, @Today)), N'Emergency', NULL, NULL, NULL),
  (137, 101, N'IN', 20, 1450.00, N'PB-MAIN-0001', N'Received from purchase bill', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -12, @Today)), N'', NULL, NULL, 101),
  (138, 103, N'IN', 10, 1800.00, N'PB-MAIN-0001', N'Received from purchase bill', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -12, @Today)), N'', NULL, NULL, 101),
  (139, 104, N'IN', 200, 95.00, N'PB-MAIN-0001', N'Received from purchase bill', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -12, @Today)), N'', NULL, NULL, 101),
  (140, 113, N'IN', 40, 1450.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (141, 113, N'OUT', 12, 1450.00, N'ISS-ISB-142', N'Issued to OT', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -9, @Today)), N'OT', NULL, NULL, NULL),
  (142, 113, N'OUT', 8, 1450.00, N'ISS-ISB-143', N'Issued to Emergency', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -3, @Today)), N'Emergency', NULL, NULL, NULL),
  (143, 114, N'IN', 50, 650.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (144, 114, N'OUT', 15, 650.00, N'ISS-ISB-145', N'Issued to Wards', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -8, @Today)), N'Wards', NULL, NULL, NULL),
  (145, 114, N'OUT', 10, 650.00, N'ISS-ISB-146', N'Issued to Front Office', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -2, @Today)), N'Front Office', NULL, NULL, NULL),
  (146, 115, N'IN', 30, 1800.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (147, 115, N'OUT', 6, 1800.00, N'ISS-ISB-148', N'Issued to Wards', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -7, @Today)), N'Wards', NULL, NULL, NULL),
  (148, 115, N'OUT', 5, 1800.00, N'ISS-ISB-149', N'Issued to Emergency', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -1, @Today)), N'Emergency', NULL, NULL, NULL),
  (149, 116, N'IN', 400, 95.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (150, 116, N'OUT', 120, 95.00, N'ISS-ISB-151', N'Issued to Wards', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -6, @Today)), N'Wards', 105, NULL, NULL),
  (151, 116, N'OUT', 90, 95.00, N'ISS-ISB-152', N'Issued to ICU', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -2, @Today)), N'ICU', NULL, NULL, NULL),
  (152, 117, N'IN', 60, 520.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (153, 117, N'OUT', 14, 520.00, N'ISS-ISB-154', N'Issued to OT', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -9, @Today)), N'OT', NULL, NULL, NULL),
  (154, 117, N'OUT', 12, 520.00, N'ISS-ISB-155', N'Issued to Wards', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -4, @Today)), N'Wards', NULL, NULL, NULL),
  (155, 118, N'IN', 36, 780.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (156, 118, N'OUT', 10, 780.00, N'ISS-ISB-157', N'Issued to Wards', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -10, @Today)), N'Wards', NULL, NULL, NULL),
  (157, 118, N'OUT', 9, 780.00, N'ISS-ISB-158', N'Issued to Front Office', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -3, @Today)), N'Front Office', NULL, NULL, NULL),
  (158, 119, N'IN', 80, 1650.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (159, 119, N'OUT', 20, 1650.00, N'ISS-ISB-160', N'Issued to Wards', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -5, @Today)), N'Wards', NULL, NULL, NULL),
  (160, 120, N'IN', 15, 2400.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (161, 120, N'OUT', 6, 2400.00, N'ISS-ISB-162', N'Issued to Laboratory', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -8, @Today)), N'Laboratory', NULL, NULL, NULL),
  (162, 120, N'OUT', 5, 2400.00, N'ISS-ISB-163', N'Issued to Laboratory', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -2, @Today)), N'Laboratory', NULL, NULL, NULL),
  (163, 120, N'OUT', 4, 2400.00, N'ISS-ISB-164', N'Issued to Laboratory', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -1, @Today)), N'Laboratory', NULL, NULL, NULL),
  (164, 121, N'IN', 30, 450.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (165, 121, N'OUT', 8, 450.00, N'ISS-ISB-166', N'Issued to Laboratory', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -6, @Today)), N'Laboratory', NULL, NULL, NULL),
  (166, 121, N'OUT', 6, 450.00, N'ISS-ISB-167', N'Issued to Wards', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -1, @Today)), N'Wards', NULL, NULL, NULL),
  (167, 122, N'IN', 40, 1350.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (168, 122, N'OUT', 12, 1350.00, N'ISS-ISB-169', N'Issued to Administration', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -7, @Today)), N'Administration', NULL, NULL, NULL),
  (169, 122, N'OUT', 6, 1350.00, N'ISS-ISB-170', N'Issued to Front Office', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -2, @Today)), N'Front Office', NULL, NULL, NULL),
  (170, 123, N'IN', 120, 120.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (171, 123, N'OUT', 40, 120.00, N'ISS-ISB-172', N'Issued to Front Office', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -6, @Today)), N'Front Office', NULL, NULL, NULL),
  (172, 123, N'OUT', 30, 120.00, N'ISS-ISB-173', N'Issued to Pharmacy', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -1, @Today)), N'Pharmacy', NULL, NULL, NULL),
  (173, 124, N'IN', 60, 380.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (174, 124, N'OUT', 18, 380.00, N'ISS-ISB-175', N'Issued to ICU', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -4, @Today)), N'ICU', 105, NULL, NULL),
  (175, 124, N'OUT', 12, 380.00, N'ISS-ISB-176', N'Issued to Emergency', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -1, @Today)), N'Emergency', NULL, NULL, NULL),
  (176, 113, N'IN', 20, 1450.00, N'PB-ISB-0001', N'Received from purchase bill', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -12, @Today)), N'', NULL, NULL, 103),
  (177, 115, N'IN', 10, 1800.00, N'PB-ISB-0001', N'Received from purchase bill', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -12, @Today)), N'', NULL, NULL, 103),
  (178, 116, N'IN', 200, 95.00, N'PB-ISB-0001', N'Received from purchase bill', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -12, @Today)), N'', NULL, NULL, 103),
  (179, 125, N'IN', 40, 1450.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (180, 125, N'OUT', 12, 1450.00, N'ISS-KHI-181', N'Issued to OT', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -9, @Today)), N'OT', NULL, NULL, NULL),
  (181, 125, N'OUT', 8, 1450.00, N'ISS-KHI-182', N'Issued to Emergency', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -3, @Today)), N'Emergency', NULL, NULL, NULL),
  (182, 125, N'OUT', 17, 1450.00, N'ISS-KHI-183', N'Issued to OT', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -1, @Today)), N'OT', NULL, NULL, NULL),
  (183, 126, N'IN', 50, 650.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (184, 126, N'OUT', 15, 650.00, N'ISS-KHI-185', N'Issued to Wards', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -8, @Today)), N'Wards', NULL, NULL, NULL),
  (185, 126, N'OUT', 10, 650.00, N'ISS-KHI-186', N'Issued to Front Office', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -2, @Today)), N'Front Office', NULL, NULL, NULL),
  (186, 127, N'IN', 30, 1800.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (187, 127, N'OUT', 6, 1800.00, N'ISS-KHI-188', N'Issued to Wards', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -7, @Today)), N'Wards', NULL, NULL, NULL),
  (188, 127, N'OUT', 5, 1800.00, N'ISS-KHI-189', N'Issued to Emergency', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -1, @Today)), N'Emergency', NULL, NULL, NULL),
  (189, 128, N'IN', 400, 95.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (190, 128, N'OUT', 120, 95.00, N'ISS-KHI-191', N'Issued to Wards', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -6, @Today)), N'Wards', 108, NULL, NULL),
  (191, 128, N'OUT', 90, 95.00, N'ISS-KHI-192', N'Issued to ICU', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -2, @Today)), N'ICU', NULL, NULL, NULL),
  (192, 129, N'IN', 60, 520.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (193, 129, N'OUT', 14, 520.00, N'ISS-KHI-194', N'Issued to OT', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -9, @Today)), N'OT', NULL, NULL, NULL),
  (194, 129, N'OUT', 12, 520.00, N'ISS-KHI-195', N'Issued to Wards', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -4, @Today)), N'Wards', NULL, NULL, NULL),
  (195, 130, N'IN', 36, 780.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (196, 130, N'OUT', 10, 780.00, N'ISS-KHI-197', N'Issued to Wards', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -10, @Today)), N'Wards', NULL, NULL, NULL),
  (197, 130, N'OUT', 9, 780.00, N'ISS-KHI-198', N'Issued to Front Office', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -3, @Today)), N'Front Office', NULL, NULL, NULL),
  (198, 131, N'IN', 80, 1650.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (199, 131, N'OUT', 20, 1650.00, N'ISS-KHI-200', N'Issued to Wards', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -5, @Today)), N'Wards', NULL, NULL, NULL),
  (200, 132, N'IN', 15, 2400.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (201, 132, N'OUT', 6, 2400.00, N'ISS-KHI-202', N'Issued to Laboratory', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -8, @Today)), N'Laboratory', NULL, NULL, NULL),
  (202, 132, N'OUT', 5, 2400.00, N'ISS-KHI-203', N'Issued to Laboratory', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -2, @Today)), N'Laboratory', NULL, NULL, NULL),
  (203, 133, N'IN', 30, 450.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (204, 133, N'OUT', 8, 450.00, N'ISS-KHI-205', N'Issued to Laboratory', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -6, @Today)), N'Laboratory', NULL, NULL, NULL),
  (205, 133, N'OUT', 6, 450.00, N'ISS-KHI-206', N'Issued to Wards', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -1, @Today)), N'Wards', NULL, NULL, NULL),
  (206, 134, N'IN', 40, 1350.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (207, 134, N'OUT', 12, 1350.00, N'ISS-KHI-208', N'Issued to Administration', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -7, @Today)), N'Administration', NULL, NULL, NULL),
  (208, 134, N'OUT', 6, 1350.00, N'ISS-KHI-209', N'Issued to Front Office', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -2, @Today)), N'Front Office', NULL, NULL, NULL),
  (209, 135, N'IN', 120, 120.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (210, 135, N'OUT', 40, 120.00, N'ISS-KHI-211', N'Issued to Front Office', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -6, @Today)), N'Front Office', NULL, NULL, NULL),
  (211, 135, N'OUT', 30, 120.00, N'ISS-KHI-212', N'Issued to Pharmacy', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -1, @Today)), N'Pharmacy', NULL, NULL, NULL),
  (212, 135, N'OUT', 40, 120.00, N'ISS-KHI-213', N'Issued to Front Office', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, 0, @Today)), N'Front Office', NULL, NULL, NULL),
  (213, 136, N'IN', 60, 380.00, N'OPENING', N'Opening stock', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -40, @Today)), N'', NULL, NULL, NULL),
  (214, 136, N'OUT', 18, 380.00, N'ISS-KHI-215', N'Issued to ICU', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -4, @Today)), N'ICU', 108, NULL, NULL),
  (215, 136, N'OUT', 12, 380.00, N'ISS-KHI-216', N'Issued to Emergency', ISNULL(@Store, N''), DATEADD(MINUTE, 810, DATEADD(DAY, -1, @Today)), N'Emergency', NULL, NULL, NULL),
  (216, 125, N'IN', 20, 1450.00, N'PB-KHI-0001', N'Received from purchase bill', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -12, @Today)), N'', NULL, NULL, 105),
  (217, 127, N'IN', 10, 1800.00, N'PB-KHI-0001', N'Received from purchase bill', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -12, @Today)), N'', NULL, NULL, 105),
  (218, 128, N'IN', 200, 95.00, N'PB-KHI-0001', N'Received from purchase bill', ISNULL(@Store, N''), DATEADD(MINUTE, 540, DATEADD(DAY, -12, @Today)), N'', NULL, NULL, 105)
) AS src([Id],[InventoryItemId],[TransactionType],[Quantity],[UnitCost],[ReferenceNumber],[Remarks],[PerformedByUserId],[TransactionDate],[Department],[PatientId],[BillId],[PurchaseBillId])
WHERE NOT EXISTS (SELECT 1 FROM [InventoryTransactions] x WHERE x.[Id] = src.[Id]) AND EXISTS (SELECT 1 FROM [InventoryItems] i WHERE i.[Id] = src.[InventoryItemId]);
SET IDENTITY_INSERT [InventoryTransactions] OFF;
GO

-- ============================================================
-- 30. CURRENCY – Pakistani rupee (PKR)
--     All amounts are shown as "PKR 1,250.00" (Settings → Printers → Currency symbol); online payments in PKR.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM [Settings] WHERE [Key] = N'Printing:CurrencySymbol')
    INSERT INTO [Settings] ([Key],[Value],[Type],[Category],[Description],[IsSystem],[CreatedDate],[ModifiedBy])
    VALUES (N'Printing:CurrencySymbol', N'PKR', N'string', N'Printing', N'Currency symbol shown on all pages and printed on receipts', 0, SYSDATETIME(), N'SeedDemoData.sql');
ELSE
    UPDATE [Settings] SET [Value] = N'PKR', [ModifiedDate] = SYSDATETIME(), [ModifiedBy] = N'SeedDemoData.sql'
    WHERE [Key] = N'Printing:CurrencySymbol' AND [Value] IN (N'', N'₹', N'Rs', N'Rs.', N'INR', N'$', N'USD');

IF NOT EXISTS (SELECT 1 FROM [Settings] WHERE [Key] = N'Payment:Currency')
    INSERT INTO [Settings] ([Key],[Value],[Type],[Category],[Description],[IsSystem],[CreatedDate],[ModifiedBy])
    VALUES (N'Payment:Currency', N'PKR', N'string', N'Payment', N'Currency of online payments', 0, SYSDATETIME(), N'SeedDemoData.sql');
GO

-- ============================================================
-- Done.
-- ============================================================
PRINT 'MedyxHMS demo data seeded successfully.';
GO

-- Re-enable the foreign keys and re-validate the rows so the keys stay trusted; a table whose existing
-- rows do not satisfy a key gets it back without re-validation (and a note).
EXEC sp_MSforeachtable N'BEGIN TRY
    ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL;
END TRY
BEGIN CATCH
    ALTER TABLE ? WITH NOCHECK CHECK CONSTRAINT ALL;
    PRINT N''Note: foreign keys of ? re-enabled without re-validation: '' + ERROR_MESSAGE();
END CATCH';
GO

-- Ends the stop set above when the script was run in a system database.
SET NOEXEC OFF;
GO
