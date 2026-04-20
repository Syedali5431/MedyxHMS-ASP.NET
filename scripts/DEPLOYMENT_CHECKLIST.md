# Database Deployment Checklist

## Overview
This checklist guides deployment of the MedyxHMS database bootstrap scripts to development, staging, and production environments.

---

## Pre-Deployment (All Environments)

### Environment Validation
- [ ] Verify target SQL Server version (2019 or later)
- [ ] Confirm network connectivity to target SQL Server instance
- [ ] Verify credentials and permissions (sa or database creator role required)
- [ ] Check available disk space (minimum 10GB free)
- [ ] Review firewall rules for SQL Server communication
- [ ] Confirm no existing `MedyxHMS` database (or backup existing if upgrading)

### Script Selection
- [ ] Choose appropriate script:
  - `New-Database.sql` - Full schema + baseline seed data (recommended for initial deployments)
  - `New-Database-Empty.sql` - Schema only, for custom/post-deployment seeding

### Backup Strategy
- [ ] If replacing existing database: Create full database backup
- [ ] Store backup in secure, accessible location with timestamp
- [ ] Document backup location and restore procedure

---

## Deployment (LocalDB Development)

### LocalDB Instance Setup
- [ ] Verify LocalDB is installed: `sqllocaldb info MSSQLLocalDB`
- [ ] Start LocalDB: `sqllocaldb start MSSQLLocalDB`

### Script Deployment
```powershell
# Using SSMS
# 1. Open SQL Server Management Studio
# 2. Connect to (localdb)\MSSQLLocalDB
# 3. File > Open > Script File > select New-Database.sql
# 4. Execute (F5)

# OR using sqlcmd (command line)
sqlcmd -S "(localdb)\MSSQLLocalDB" -i "scripts/New-Database.sql"
```

Deployment Tasks:
- [ ] Execute script against LocalDB
- [ ] Verify exit code is 0 (success)
- [ ] Check for warnings in output (non-blocking)

### Post-Deployment Validation (LocalDB)
- [ ] Query table count: `SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo'`
  - Expected: 50+ tables
- [ ] Verify roles created: `SELECT COUNT(*) FROM AspNetRoles` 
  - Expected: 9 roles
- [ ] Verify features created: `SELECT COUNT(*) FROM dbo.Features`
  - Expected: 28 features
- [ ] Verify role-feature mappings: `SELECT COUNT(*) FROM dbo.RoleFeatures`
  - Expected: 95 mappings
- [ ] Verify SuperAdmin user: `SELECT * FROM AspNetUsers WHERE UserName = 'SuperAdmin'`
  - Expected: 1 record
- [ ] Verify utility views exist: `SELECT * FROM INFORMATION_SCHEMA.VIEWS WHERE TABLE_NAME IN ('vw_StaffWithRoles', 'vw_ActiveDoctorShifts')`
  - Expected: 2 views
- [ ] Test stored procedures:
  ```sql
  EXEC usp_GetOperationalCounts;
  EXEC usp_GetUserRoleSummary;
  ```
  - Both should return results without errors

---

## Deployment (Staging SQL Server)

### Pre-Staging Checklist
- [ ] Staging SQL Server hostname/instance documented (e.g., `STAGING-SQL\SQLEXPRESS`)
- [ ] SA credentials obtained and securely stored
- [ ] Staging environment mirrors production topology (single server, cluster, etc.)

### Script Deployment to Staging
```powershell
# Replace values with your staging environment
$ServerName = "STAGING-SQL\SQLEXPRESS"
$SqlUser = "sa"
$SqlPassword = "YourSecurePassword"

sqlcmd -S $ServerName -U $SqlUser -P $SqlPassword -i "scripts/New-Database.sql"
```

Deployment Tasks:
- [ ] Execute script on staging server
- [ ] Monitor deployment progress (typically 30-60 seconds)
- [ ] Verify exit code is 0
- [ ] Document deployment timestamp and operator name

### Post-Deployment Validation (Staging)
- [ ] Run all checks from LocalDB validation section
- [ ] Verify connections from staging app server to database
- [ ] Test network latency: `SELECT @@SERVERNAME, GETDATE()`
- [ ] Check SQL Server logs for errors: 
  ```sql
  EXEC xp_readerrorlog 0, 1
  ```
- [ ] Verify backup/recovery is configured (if required)
- [ ] Test failover capabilities (if using AlwaysOn/clustering)

### Application Testing on Staging
- [ ] Deploy ASP.NET application to staging environment
- [ ] Update connection string to staging database
- [ ] Run smoke tests:
  - [ ] Login as SuperAdmin (username: `SuperAdmin`, default password from seed)
  - [ ] Verify Dashboard loads without errors
  - [ ] Access at least one clinical module (OPD, IPD, Lab, etc.)
  - [ ] Verify role-based access controls (RBAC) functional
  - [ ] Test user creation workflow
  - [ ] Verify audit logging captured user actions

---

## Deployment (Production)

### Pre-Production Approval
- [ ] Change request approved by IT/Database Team
- [ ] Maintenance window scheduled and communicated
- [ ] Stakeholders notified of planned downtime
- [ ] Disaster recovery plan reviewed
- [ ] Rollback procedure tested and documented

### Production Deployment
```powershell
# High-safety deployment with transaction monitoring
$ServerName = "PROD-SQL\PROD"
$SqlUser = "sa"
$SqlPassword = "YourSecurePassword"
$LogFile = "C:\Logs\MedyxHMS_Deploy_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"

sqlcmd -S $ServerName -U $SqlUser -P $SqlPassword -i "scripts/New-Database.sql" | Tee-Object -FilePath $LogFile
```

Deployment Tasks:
- [ ] Execute during approved maintenance window
- [ ] Real-time monitoring of deployment progress
- [ ] Operator on-call standby for immediate rollback
- [ ] Log all deployment events to central monitoring/audit system
- [ ] Verify exit code is 0

### Post-Production Validation
- [ ] All checks from staging validation section
- [ ] Production backup created immediately post-deployment
- [ ] Backup tested (restore to isolated environment)
- [ ] Monitor database performance for 24 hours:
  - [ ] Check query wait times
  - [ ] Monitor transaction log growth
  - [ ] Verify no blocking queries
- [ ] Application team confirms end-to-end functionality
- [ ] Monitor error logs for any data-related issues

---

## Rollback Procedure

### If Deployment Fails
```sql
-- Immediate rollback: drop database and restore from backup
DROP DATABASE MedyxHMS;

-- Restore from pre-deployment backup
RESTORE DATABASE MedyxHMS 
FROM DISK = 'C:\Backups\MedyxHMS_PreDeployment_backup.bak'
WITH REPLACE, RECOVERY;

-- Verify restoration
SELECT * FROM INFORMATION_SCHEMA.TABLES;
```

Rollback Tasks:
- [ ] Verify backup file exists and is accessible
- [ ] Stop application connections to database
- [ ] Execute DROP DATABASE (if database exists)
- [ ] Execute RESTORE DATABASE from backup
- [ ] Verify database is online and accessible
- [ ] Reconnect application and verify functionality
- [ ] Document rollback completion and root cause

---

## Post-Deployment Documentation

### Deployment Report
Document the following:
- [ ] Deployment date/time
- [ ] Environment (Dev/Staging/Production)
- [ ] Operator/Team name
- [ ] Script used (Full or Empty variant)
- [ ] Duration (start to finish)
- [ ] Exit code and any warnings
- [ ] Issues encountered and resolution
- [ ] Validation checklist results
- [ ] Sign-off by Database Administrator

### Knowledge Base Updates
- [ ] Update wiki/Confluence with environment-specific connection strings
- [ ] Document any performance baselines or tuning applied
- [ ] Record troubleshooting steps for future deployments
- [ ] Archive deployment logs for audit trail

---

## Troubleshooting Guide

### Common Issues & Solutions

#### Issue: "Database already exists"
```sql
-- Drop existing database (USE WITH CAUTION - requires backup!)
DROP DATABASE MedyxHMS;
```

#### Issue: "Login failed for user 'sa'"
- [ ] Verify SQL Server running in Mixed Authentication mode
- [ ] Check SA credentials with DBA team
- [ ] Confirm firewall allows SQL Server port (1433)

#### Issue: "Insufficient disk space"
- [ ] Free up space on SQL Server drive
- [ ] Or modify script to create database on alternate drive:
  ```sql
  ALTER DATABASE MedyxHMS MODIFY FILE (NAME = MedyxHMS, FILENAME = 'D:\SQLData\MedyxHMS.mdf');
  ```

#### Issue: "Seed data INSERTs fail (duplicate key)"
- [ ] Verify no pre-existing data in target database
- [ ] Check for identity seed value conflicts
- [ ] Use `New-Database-Empty.sql` and manually seed

#### Issue: "Script timeout"
- [ ] Increase sqlcmd timeout: `sqlcmd -t 300 ...` (300 seconds)
- [ ] Check SQL Server performance (high CPU/disk I/O)
- [ ] Run on maintenance window with less load

---

## Contact & Support

### Database Team Contacts
- **Database Administrator**: [Name/Email]
- **Network/Infra Team**: [Name/Email]
- **Application Owner**: [Name/Email]

### Script Locations
- **Staging Repository**: `e:\HMS\MedyxHMS-ASPNET\scripts\`
- **Production Repository**: [Production repo location]

### Related Documentation
- [Database Bootstrap Scripts README](README.md)
- [New-Database.sql](New-Database.sql)
- [New-Database-Empty.sql](New-Database-Empty.sql)
- [Main Documentation Index](../docs/INDEX.md)

---

**Last Updated**: 2026-04-20  
**Maintained By**: Database & DevOps Team
