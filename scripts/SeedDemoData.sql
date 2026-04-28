-- ============================================================
--  MedyxHMS Demo Data Seed Script
--  Target:   SQL Server — database [MedyxHMS]
--  Source:   Adapted from hospitaldemo_db.sql (MariaDB 10.4.27)
--  Coverage: Departments, Doctors, Staff, Patients, Appointments,
--            OPD, IPD, Wards, Beds, Bills, Payments, Pharmacy,
--            Lab, Radiology, Blood Bank
--
--  Usage:
--    sqlcmd -S .\SQLEXPRESS -d MedyxHMS -i SeedDemoData.sql
--    — or open in SSMS and execute against MedyxHMS
--
--  NOTE: Existing rows with the same Id will be skipped (WHERE NOT EXISTS).
--        Run this script once on a clean or empty database.
-- ============================================================


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

INSERT INTO [Patients] ([Id],[PatientId],[FirstName],[LastName],[Email],[Phone],[DateOfBirth],[Gender],[Address],[City],[State],[Country],[PostalCode],[BloodGroup],[EmergencyContactName],[EmergencyContactPhone],[EmergencyContactRelation],[MedicalHistory],[Allergies],[GuardianName],[GuardianPhone],[MaritalStatus],[Occupation],[UserId],[ProfileImagePath],[IsActive],[CreatedDate],[LastVisitDate])
SELECT * FROM (VALUES
  (1,  'PAT00001', 'Amit',       'Sharma',    'amit.sharma@mail.com',    '9811001001', '1985-06-15', 'Male',   '12 MG Road',         'Delhi',      'Delhi',           'India', '110001', 'B+',  'Priya Sharma',   '9811001099', 'Wife',    'Hypertension',     'Penicillin',  '',              '',           'Married',  'Engineer',  'demo-patient', '', 1, '2024-01-10', '2025-03-15'),
  (2,  'PAT00002', 'Sunita',     'Verma',     'sunita.v@mail.com',       '9811001002', '1990-08-22', 'Female', '45 Civil Lines',      'Mumbai',     'Maharashtra',     'India', '400001', 'A+',  'Raj Verma',      '9811001098', 'Husband', 'Diabetes T2',      'Sulfa',       '',              '',           'Married',  'Teacher',   'demo-patient', '', 1, '2024-01-12', '2025-03-20'),
  (3,  'PAT00003', 'Rohan',      'Singh',     'rohan.s@mail.com',        '9811001003', '2005-03-10', 'Male',   '8 Gandhi Nagar',      'Jaipur',     'Rajasthan',       'India', '302001', 'O+',  'Kiran Singh',    '9811001097', 'Mother',  '',                 '',            'Kiran Singh',  '9811001097', 'Single',   'Student',   'demo-patient', '', 1, '2024-01-15', '2025-02-28'),
  (4,  'PAT00004', 'Priya',      'Gupta',     'priya.g@mail.com',        '9811001004', '1978-11-30', 'Female', '23 Nehru Place',      'Lucknow',    'Uttar Pradesh',   'India', '226001', 'AB+', 'Suresh Gupta',   '9811001096', 'Husband', 'Thyroid disorder', '',            '',              '',           'Married',  'Homemaker', 'demo-patient', '', 1, '2024-01-18', '2025-04-01'),
  (5,  'PAT00005', 'Vikram',     'Patel',     'vikram.p@mail.com',       '9811001005', '1965-05-05', 'Male',   '67 Subhash Chowk',   'Ahmedabad',  'Gujarat',         'India', '380001', 'O-',  'Neeta Patel',    '9811001095', 'Wife',    'CAD, Hypertension','Aspirin',     '',              '',           'Married',  'Business',  'demo-patient', '', 1, '2024-01-20', '2025-04-05'),
  (6,  'PAT00006', 'Deepa',      'Nair',      'deepa.n@mail.com',        '9811001006', '1995-07-19', 'Female', '15 West Park',        'Bangalore',  'Karnataka',       'India', '560001', 'B-',  'Rajan Nair',     '9811001094', 'Father',  '',                 '',            '',              '',           'Single',   'Software',  'demo-patient', '', 1, '2024-01-22', '2025-03-10'),
  (7,  'PAT00007', 'Manish',     'Kumar',     'manish.k@mail.com',       '9811001007', '1980-12-01', 'Male',   '3 Race Course Road',  'Chennai',    'Tamil Nadu',      'India', '600001', 'A-',  'Suman Kumar',    '9811001093', 'Wife',    'Asthma',           'NSAIDs',      '',              '',           'Married',  'Driver',    'demo-patient', '', 1, '2024-01-25', '2025-02-20'),
  (8,  'PAT00008', 'Kavita',     'Joshi',     'kavita.j@mail.com',       '9811001008', '1972-09-14', 'Female', '101 Green Park',      'Pune',       'Maharashtra',     'India', '411001', 'AB-', 'Mohan Joshi',    '9811001092', 'Husband', 'Osteoporosis',     '',            '',              '',           'Married',  'Nurse',     'demo-patient', '', 1, '2024-01-28', '2025-04-10'),
  (9,  'PAT00009', 'Arjun',      'Reddy',     'arjun.r@mail.com',        '9811001009', '1999-04-25', 'Male',   '22 Banjara Hills',    'Hyderabad',  'Telangana',       'India', '500001', 'B+',  'Sudha Reddy',    '9811001091', 'Mother',  '',                 '',            '',              '',           'Single',   'Student',   'demo-patient', '', 1, '2024-02-01', '2025-03-28'),
  (10, 'PAT00010', 'Rekha',      'Mishra',    'rekha.m@mail.com',        '9811001010', '1988-02-17', 'Female', '55 Ashok Vihar',      'Kolkata',    'West Bengal',     'India', '700001', 'O+',  'Dinesh Mishra',  '9811001090', 'Husband', 'PCOS',             '',            '',              '',           'Married',  'Accountant','demo-patient', '', 1, '2024-02-03', '2025-04-02'),
  (11, 'PAT00011', 'Suresh',     'Yadav',     'suresh.y@mail.com',       '9811001011', '1958-10-08', 'Male',   '9 Vikas Puri',        'Delhi',      'Delhi',           'India', '110018', 'A+',  'Kamla Yadav',    '9811001089', 'Wife',    'COPD, Diabetes',   'Iodine',      '',              '',           'Married',  'Retired',   'demo-patient', '', 1, '2024-02-05', '2025-04-08'),
  (12, 'PAT00012', 'Nisha',      'Agarwal',   'nisha.a@mail.com',        '9811001012', '1993-06-30', 'Female', '77 MG Marg',          'Allahabad',  'Uttar Pradesh',   'India', '211001', 'B+',  'Ashok Agarwal',  '9811001088', 'Father',  '',                 '',            '',              '',           'Single',   'Designer',  'demo-patient', '', 1, '2024-02-08', '2025-03-05'),
  (13, 'PAT00013', 'Rahul',      'Saxena',    'rahul.sx@mail.com',       '9811001013', '2015-01-20', 'Male',   '30 Sector 12',        'Noida',      'Uttar Pradesh',   'India', '201301', 'O+',  'Anita Saxena',   '9811001087', 'Mother',  '',                 '',            'Anita Saxena', '9811001087', 'Single',   'Student',   'demo-patient', '', 1, '2024-02-10', '2025-02-15'),
  (14, 'PAT00014', 'Anita',      'Chaudhary', 'anita.c@mail.com',        '9811001014', '1983-08-11', 'Female', '14 Rajpur Road',      'Dehradun',   'Uttarakhand',     'India', '248001', 'A+',  'Ravi Chaudhary', '9811001086', 'Husband', 'Migraine',         '',            '',              '',           'Married',  'Teacher',   'demo-patient', '', 1, '2024-02-12', '2025-03-22'),
  (15, 'PAT00015', 'Balram',     'Singh',     'balram.si@mail.com',      '9811001015', '1950-03-25', 'Male',   '88 Cantonment',       'Kanpur',     'Uttar Pradesh',   'India', '208004', 'B-',  'Geeta Singh',    '9811001085', 'Wife',    'Heart Failure',    'Warfarin',    '',              '',           'Married',  'Retired',   'demo-patient', '', 1, '2024-02-15', '2025-04-12'),
  (16, 'PAT00016', 'Shweta',     'Tiwari',    'shweta.t@mail.com',       '9811001016', '2000-11-11', 'Female', '5 New Colony',        'Bhopal',     'Madhya Pradesh',  'India', '462001', 'AB+', 'Anil Tiwari',    '9811001084', 'Father',  '',                 '',            '',              '',           'Single',   'Student',   'demo-patient', '', 1, '2024-02-18', '2025-03-18'),
  (17, 'PAT00017', 'Dinesh',     'Pandey',    'dinesh.p@mail.com',       '9811001017', '1975-07-04', 'Male',   '18 Tilak Nagar',      'Nagpur',     'Maharashtra',     'India', '440001', 'O-',  'Savita Pandey',  '9811001083', 'Wife',    'Kidney Stones',    '',            '',              '',           'Married',  'Mechanic',  'demo-patient', '', 1, '2024-02-20', '2025-04-03'),
  (18, 'PAT00018', 'Meena',      'Srivastava','meena.sr@mail.com',       '9811001018', '1969-09-28', 'Female', '42 Alambagh',         'Lucknow',    'Uttar Pradesh',   'India', '226005', 'A-',  'Ramesh Srivastava','9811001082','Husband', 'Rheumatoid Arthritis','Methotrexate','',           '',           'Married',  'Homemaker', 'demo-patient', '', 1, '2024-02-22', '2025-04-07'),
  (19, 'PAT00019', 'Harish',     'Malhotra',  'harish.m@mail.com',       '9811001019', '1991-04-17', 'Male',   '11 Defence Colony',   'Delhi',      'Delhi',           'India', '110024', 'B+',  'Shashi Malhotra','9811001081', 'Mother',  '',                 '',            '',              '',           'Single',   'CA',        'demo-patient', '', 1, '2024-03-01', '2025-03-30'),
  (20, 'PAT00020', 'Geeta',      'Bhatt',     'geeta.b@mail.com',        '9811001020', '1960-12-22', 'Female', '6 Ram Nagar',         'Varanasi',   'Uttar Pradesh',   'India', '221001', 'O+',  'Suresh Bhatt',   '9811001080', 'Husband', 'Type 1 Diabetes',  'Latex',       '',              '',           'Married',  'Homemaker', 'demo-patient', '', 1, '2024-03-05', '2025-04-11')
) AS src([Id],[PatientId],[FirstName],[LastName],[Email],[Phone],[DateOfBirth],[Gender],[Address],[City],[State],[Country],[PostalCode],[BloodGroup],[EmergencyContactName],[EmergencyContactPhone],[EmergencyContactRelation],[MedicalHistory],[Allergies],[GuardianName],[GuardianPhone],[MaritalStatus],[Occupation],[UserId],[ProfileImagePath],[IsActive],[CreatedDate],[LastVisitDate])
WHERE NOT EXISTS (SELECT 1 FROM [Patients] p WHERE p.[Id] = src.[Id]);

SET IDENTITY_INSERT [Patients] OFF;
GO

-- ============================================================
-- 5. APPOINTMENTS
-- ============================================================
SET IDENTITY_INSERT [Appointments] ON;

INSERT INTO [Appointments] ([Id],[AppointmentId],[PatientId],[DoctorId],[StaffId],[AppointmentDate],[AppointmentTime],[Status],[AppointmentType],[Priority],[Symptoms],[Notes],[CreatedDate],[CreatedBy],[UpdatedDate],[UpdatedBy])
SELECT * FROM (VALUES
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
-- Done.
-- ============================================================
PRINT 'MedyxHMS demo data seeded successfully.';
GO

-- Re-enable FK constraints (NOCHECK = re-enable without re-validating existing rows)
EXEC sp_MSforeachtable 'ALTER TABLE ? WITH NOCHECK CHECK CONSTRAINT ALL';
GO
