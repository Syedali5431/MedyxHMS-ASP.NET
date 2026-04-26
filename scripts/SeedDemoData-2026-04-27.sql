/*
    MedyxHMS Demo Data Seed Script (2026-04-27)
    - Use after running New-Database-2026-04-27.sql
    - Inserts demo data for Departments, Doctors, Staff, Patients, Appointments, Beds, etc.
*/

USE [MedyxHMS];
GO

-- Disable FK constraints for demo data insert
EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
GO

-- Example: Departments
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

-- Add similar demo data inserts for Doctors, Staff, Patients, Beds, Appointments, etc.
-- Copy structure from previous SeedDemoData.sql as needed.

-- Re-enable FK constraints
EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
GO
