using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Data;

namespace MedyxHMS.Services.Implementations
{
    /// <summary>
    /// Service for managing custom report templates with caching support.
    /// Enables admins to create, edit, and delete customizable report definitions.
    /// </summary>
    public class ReportTemplateService : IReportTemplateService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ReportTemplateService> _logger;

        public ReportTemplateService(
            ApplicationDbContext context,
            ICacheService cacheService,
            ILogger<ReportTemplateService> logger)
        {
            _context = context;
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>Gets all report templates with caching (30 minutes).</summary>
        public async Task<List<ReportTemplate>> GetAllTemplatesAsync()
        {
            const string cacheKey = "templates:all";
            return await _cacheService.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    _logger.LogInformation("Loading all report templates from database");
                    return await _context.ReportTemplates
                        .Include(t => t.Fields)
                        .Include(t => t.Filters)
                        .Include(t => t.Design)
                        .Include(t => t.Charts)
                        .OrderBy(t => t.Name)
                        .ToListAsync();
                },
                durationMinutes: 30
            ) ?? new List<ReportTemplate>();
        }

        /// <summary>Gets a specific template by ID with caching (60 minutes).</summary>
        public async Task<ReportTemplate?> GetTemplateByIdAsync(int templateId)
        {
            var cacheKey = $"template:{templateId}";
            var template = await _cacheService.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    _logger.LogInformation("Loading template {TemplateId} from database", templateId);
                    return await _context.ReportTemplates
                        .Include(t => t.Fields)
                        .Include(t => t.Filters)
                        .Include(t => t.Design)
                        .Include(t => t.Charts)
                        .FirstOrDefaultAsync(t => t.Id == templateId);
                },
                durationMinutes: 60
            );
            return template;
        }

        /// <summary>Gets templates by report type.</summary>
        public async Task<List<ReportTemplate>> GetTemplatesByTypeAsync(string reportType)
        {
            var cacheKey = $"templates:type:{reportType}";
            return await _cacheService.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    _logger.LogInformation("Loading templates for type {ReportType}", reportType);
                    return await _context.ReportTemplates
                        .Where(t => t.ReportType == reportType)
                        .Include(t => t.Fields)
                        .Include(t => t.Filters)
                        .Include(t => t.Design)
                        .Include(t => t.Charts)
                        .OrderBy(t => t.Name)
                        .ToListAsync();
                },
                durationMinutes: 45
            ) ?? new List<ReportTemplate>();
        }

        /// <summary>Creates a new report template and invalidates cache.</summary>
        public async Task<ReportTemplate> CreateTemplateAsync(ReportTemplate template)
        {
            try
            {
                _logger.LogInformation("Creating new template: {TemplateName}", template.Name);
                
                _context.ReportTemplates.Add(template);
                await _context.SaveChangesAsync();

                // Invalidate cache
                await _cacheService.InvalidatePrefixAsync("template:");
                await _cacheService.InvalidatePrefixAsync("templates:");

                _logger.LogInformation("Template created successfully with ID {TemplateId}", template.Id);
                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating template {TemplateName}", template.Name);
                throw;
            }
        }

        /// <summary>Updates an existing report template and invalidates cache.</summary>
        public async Task<ReportTemplate> UpdateTemplateAsync(ReportTemplate template)
        {
            try
            {
                _logger.LogInformation("Updating template {TemplateId}: {TemplateName}", template.Id, template.Name);

                _context.ReportTemplates.Update(template);
                await _context.SaveChangesAsync();

                // Invalidate cache
                await _cacheService.RemoveAsync($"template:{template.Id}");
                await _cacheService.InvalidatePrefixAsync("templates:");

                _logger.LogInformation("Template {TemplateId} updated successfully", template.Id);
                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating template {TemplateId}", template.Id);
                throw;
            }
        }

        /// <summary>Deletes a report template and invalidates cache.</summary>
        public async Task<bool> DeleteTemplateAsync(int templateId)
        {
            try
            {
                _logger.LogInformation("Deleting template {TemplateId}", templateId);

                var template = await _context.ReportTemplates.FindAsync(templateId);
                if (template == null)
                {
                    _logger.LogWarning("Template {TemplateId} not found for deletion", templateId);
                    return false;
                }

                _context.ReportTemplates.Remove(template);
                await _context.SaveChangesAsync();

                // Invalidate cache
                await _cacheService.RemoveAsync($"template:{templateId}");
                await _cacheService.InvalidatePrefixAsync("templates:");

                _logger.LogInformation("Template {TemplateId} deleted successfully", templateId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template {TemplateId}", templateId);
                throw;
            }
        }

        /// <summary>Adds a field to a report template.</summary>
        public async Task<ReportField> AddFieldAsync(int templateId, ReportField field)
        {
            try
            {
                _logger.LogInformation("Adding field {FieldName} to template {TemplateId}", field.FieldName, templateId);

                field.TemplateId = templateId;
                _context.ReportFields.Add(field);
                await _context.SaveChangesAsync();

                // Invalidate cache
                await _cacheService.RemoveAsync($"template:{templateId}");

                return field;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding field to template {TemplateId}", templateId);
                throw;
            }
        }

        /// <summary>Removes a field from a report template.</summary>
        public async Task<bool> RemoveFieldAsync(int fieldId)
        {
            try
            {
                _logger.LogInformation("Removing field {FieldId}", fieldId);

                var field = await _context.ReportFields.FindAsync(fieldId);
                if (field == null)
                {
                    _logger.LogWarning("Field {FieldId} not found", fieldId);
                    return false;
                }

                _context.ReportFields.Remove(field);
                await _context.SaveChangesAsync();

                // Invalidate template cache
                if (field.TemplateId > 0)
                {
                    await _cacheService.RemoveAsync($"template:{field.TemplateId}");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing field {FieldId}", fieldId);
                throw;
            }
        }

        /// <summary>Executes a saved report with performance tracking.</summary>
        public async Task<ReportExecutionResult> ExecuteSavedReportAsync(int templateId, Dictionary<string, object>? parameters = null)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("Executing template {TemplateId}", templateId);

                var template = await GetTemplateByIdAsync(templateId);
                if (template == null)
                {
                    _logger.LogWarning("Template {TemplateId} not found for execution", templateId);
                    return new ReportExecutionResult
                    {
                        Success = false,
                        Message = "Template not found",
                        ExecutionTimeMs = sw.ElapsedMilliseconds
                    };
                }

                var cacheKey = $"template-exec:{templateId}";
                var cachedData = await _cacheService.GetAsync<List<Dictionary<string, object>>>(cacheKey);
                if (cachedData != null)
                {
                    return new ReportExecutionResult
                    {
                        Success = true,
                        Message = "Report executed successfully (cache)",
                        Data = cachedData,
                        Fields = template.Fields,
                        Charts = template.Charts,
                        TotalRecords = cachedData.Count,
                        ExecutionTimeMs = sw.ElapsedMilliseconds
                    };
                }

                var rows = await ExecuteTemplateStoredProcedureAsync(template, parameters);
                await _cacheService.SetAsync(cacheKey, rows, 10);

                return new ReportExecutionResult
                {
                    Success = true,
                    Message = "Report executed successfully",
                    Data = rows,
                    Fields = template.Fields,
                    Charts = template.Charts,
                    TotalRecords = rows.Count,
                    ExecutionTimeMs = sw.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing template {TemplateId}", templateId);
                return new ReportExecutionResult
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    ExecutionTimeMs = sw.ElapsedMilliseconds
                };
            }
            finally
            {
                sw.Stop();
            }
        }

        /// <summary>Gets predefined report types available in the system.</summary>
        public async Task<List<string>> GetAvailableReportTypesAsync()
        {
            return await Task.FromResult(new List<string>
            {
                "Department",
                "Financial",
                "Occupancy",
                "Patient",
                "Appointment",
                "Payroll",
                "Attendance",
                "Lab",
                "Radiology",
                "Pharmacy",
                "LegacyPHP"
            });
        }

        /// <summary>Clones an existing template.</summary>
        public async Task<ReportTemplate> CloneTemplateAsync(int templateId, string newName)
        {
            try
            {
                _logger.LogInformation("Cloning template {TemplateId} as {NewName}", templateId, newName);

                var template = await GetTemplateByIdAsync(templateId);
                if (template == null)
                {
                    throw new InvalidOperationException($"Template {templateId} not found");
                }

                // Create new template with cloned data
                var newTemplate = new ReportTemplate
                {
                    Name = newName,
                    ReportType = template.ReportType,
                    Description = $"Clone of {template.Name}",
                    CreatedBy = template.CreatedBy,
                    CreatedDate = DateTime.UtcNow,
                    Fields = template.Fields?.Select(f => new ReportField
                    {
                        FieldName = f.FieldName,
                        ColumnName = f.ColumnName,
                        DataType = f.DataType,
                        DisplayFormat = f.DisplayFormat,
                        IsVisible = f.IsVisible,
                        IsSortable = f.IsSortable,
                        IsFilterable = f.IsFilterable,
                        SortOrder = f.SortOrder,
                        Width = f.Width,
                        Alignment = f.Alignment
                    }).ToList() ?? new List<ReportField>(),
                    Filters = template.Filters?.Select(f => new ReportFilter
                    {
                        FilterName = f.FilterName,
                        ColumnName = f.ColumnName,
                        OperatorType = f.OperatorType,
                        DefaultValue = f.DefaultValue,
                        IsRequired = f.IsRequired,
                        SortOrder = f.SortOrder
                    }).ToList() ?? new List<ReportFilter>(),
                    Charts = template.Charts?.Select(c => new ReportChart
                    {
                        Title = c.Title,
                        ChartType = c.ChartType,
                        XAxisField = c.XAxisField,
                        YAxisField = c.YAxisField,
                        ShowLegend = c.ShowLegend,
                        ShowTooltip = c.ShowTooltip,
                        ColorScheme = c.ColorScheme,
                        SortOrder = c.SortOrder
                    }).ToList() ?? new List<ReportChart>()
                };

                return await CreateTemplateAsync(newTemplate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cloning template {TemplateId}", templateId);
                throw;
            }
        }

        private async Task<List<Dictionary<string, object>>> ExecuteTemplateStoredProcedureAsync(
            ReportTemplate template,
            Dictionary<string, object>? parameters)
        {
            var now = DateTime.UtcNow;
            var startDate = ResolveDate(parameters, "startDate", now.AddMonths(-1));
            var endDate = ResolveDate(parameters, "endDate", now);

            var procedure = template.ReportType.ToLowerInvariant() switch
            {
                "department" => "sp_GetDepartmentReport",
                "financial" => "sp_GetFinancialReport",
                "occupancy" => "sp_GetOccupancyReport",
                "patient" => "sp_GetPatientStatistics",
                "appointment" => "sp_GetAppointmentAnalytics",
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(procedure))
            {
                return new List<Dictionary<string, object>>();
            }

            var sqlParameters = new List<SqlParameter>();
            if (procedure == "sp_GetOccupancyReport")
            {
                sqlParameters.Add(new SqlParameter("@ReportDate", endDate));
            }
            else
            {
                sqlParameters.Add(new SqlParameter("@StartDate", startDate));
                sqlParameters.Add(new SqlParameter("@EndDate", endDate));
            }

            if (procedure == "sp_GetDepartmentReport")
            {
                var departmentId = parameters != null && parameters.TryGetValue("departmentId", out var d)
                    ? d?.ToString()
                    : null;
                sqlParameters.Add(new SqlParameter("@DepartmentId", string.IsNullOrWhiteSpace(departmentId) ? (object)DBNull.Value : departmentId));
            }

            return await ExecuteStoredProcedureAsync(procedure, sqlParameters);
        }

        private async Task<List<Dictionary<string, object>>> ExecuteStoredProcedureAsync(
            string procedureName,
            List<SqlParameter> parameters)
        {
            var rows = new List<Dictionary<string, object>>();
            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await using var command = connection.CreateCommand();
            command.CommandText = procedureName;
            command.CommandType = CommandType.StoredProcedure;
            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? string.Empty : reader.GetValue(i);
                }
                rows.Add(row);
            }

            return rows;
        }

        private static DateTime ResolveDate(Dictionary<string, object>? parameters, string key, DateTime fallback)
        {
            if (parameters != null && parameters.TryGetValue(key, out var value) && value != null)
            {
                if (value is DateTime dt)
                {
                    return dt;
                }

                if (DateTime.TryParse(value.ToString(), out var parsed))
                {
                    return parsed;
                }
            }

            return fallback;
        }
    }
}
