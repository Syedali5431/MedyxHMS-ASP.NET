using System.Data;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Services.Implementations
{
    /// <summary>
    /// "Back up now": a full COPY_ONLY backup of the application database to a folder on the database server,
    /// verified with RESTORE VERIFYONLY and recorded in DatabaseBackups. COPY_ONLY leaves any scheduled
    /// backup chain of the database administrator untouched.
    /// </summary>
    public class DatabaseBackupService : IDatabaseBackupService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISecurityPolicyService _policy;
        private readonly IAuditService _audit;
        private readonly ILogger<DatabaseBackupService> _logger;

        public DatabaseBackupService(ApplicationDbContext context, ISecurityPolicyService policy, IAuditService audit, ILogger<DatabaseBackupService> logger)
        {
            _context = context;
            _policy = policy;
            _audit = audit;
            _logger = logger;
        }

        public string DatabaseName => _context.Database.GetDbConnection().Database;

        public async Task<string> GetEffectiveFolderAsync()
        {
            var policy = await _policy.GetPolicyAsync();
            if (!string.IsNullOrWhiteSpace(policy.BackupFolder))
            {
                return policy.BackupFolder.Trim();
            }

            try
            {
                var path = await ScalarAsync("SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))");
                return path?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read the SQL Server default backup folder");
                return string.Empty;
            }
        }

        public async Task<List<DatabaseBackup>> GetHistoryAsync(int take = 50)
        {
            return await _context.DatabaseBackups.AsNoTracking()
                .OrderByDescending(b => b.StartedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<DatabaseBackup> BackupNowAsync(string? createdBy)
        {
            var database = DatabaseName;
            var folder = await GetEffectiveFolderAsync();
            var fileName = $"{database}_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
            var record = new DatabaseBackup
            {
                DatabaseName = database,
                FileName = fileName,
                FilePath = string.IsNullOrWhiteSpace(folder) ? fileName : Path.Combine(folder, fileName),
                StartedAt = DateTime.Now,
                Status = "Running",
                CreatedBy = createdBy ?? string.Empty
            };
            _context.DatabaseBackups.Add(record);
            await _context.SaveChangesAsync();

            try
            {
                if (string.IsNullOrWhiteSpace(folder))
                {
                    throw new InvalidOperationException("No backup folder is configured and SQL Server did not report a default backup folder.");
                }

                // BACKUP/RESTORE accept variables, so the database name and file path are passed as parameters.
                await ExecuteAsync(
                    "BACKUP DATABASE @db TO DISK = @path WITH COPY_ONLY, INIT, CHECKSUM, NAME = @name",
                    new SqlParameter("@db", database),
                    new SqlParameter("@path", record.FilePath),
                    new SqlParameter("@name", $"MedyxHMS backup by {createdBy ?? "system"}"));

                await ExecuteAsync("RESTORE VERIFYONLY FROM DISK = @path WITH CHECKSUM", new SqlParameter("@path", record.FilePath));
                record.Verified = true;

                try
                {
                    var size = await ScalarAsync(
                        "SELECT TOP (1) backup_size FROM msdb.dbo.backupset b JOIN msdb.dbo.backupmediafamily f ON f.media_set_id = b.media_set_id " +
                        "WHERE b.database_name = @db AND f.physical_device_name = @path ORDER BY b.backup_finish_date DESC",
                        new SqlParameter("@db", database), new SqlParameter("@path", record.FilePath));
                    record.SizeBytes = size == null || size is DBNull ? null : Convert.ToInt64(size);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex, "Backup size not available from msdb");
                }

                record.Status = "Succeeded";
                record.Message = "Backup completed and verified.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database backup to {Path} failed", record.FilePath);
                record.Status = "Failed";
                record.Message = ex.Message.Length > 900 ? ex.Message[..900] : ex.Message;
            }

            record.CompletedAt = DateTime.Now;
            _context.DatabaseBackups.Update(record);
            await _context.SaveChangesAsync();
            await _audit.LogActivityAsync(null, record.Status == "Succeeded" ? "DATABASE_BACKUP" : "DATABASE_BACKUP_FAILED", "DatabaseBackup",
                record.Id.ToString(), null, $"{record.FilePath} ({record.Status}) by {createdBy ?? "system"}");
            return record;
        }

        private async Task ExecuteAsync(string sql, params SqlParameter[] parameters)
        {
            var connection = _context.Database.GetDbConnection();
            var opened = connection.State != ConnectionState.Open;
            if (opened) await connection.OpenAsync();
            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.CommandTimeout = 1800; // large databases can take a while
                command.Parameters.AddRange(parameters);
                await command.ExecuteNonQueryAsync();
            }
            finally
            {
                if (opened) await connection.CloseAsync();
            }
        }

        private async Task<object?> ScalarAsync(string sql, params SqlParameter[] parameters)
        {
            var connection = _context.Database.GetDbConnection();
            var opened = connection.State != ConnectionState.Open;
            if (opened) await connection.OpenAsync();
            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Parameters.AddRange(parameters);
                return await command.ExecuteScalarAsync();
            }
            finally
            {
                if (opened) await connection.CloseAsync();
            }
        }
    }
}
