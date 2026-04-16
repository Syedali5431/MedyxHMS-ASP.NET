using MedyxHMS.Data;
using MedyxHMS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Services.Implementations
{
    public class DatabaseInitializer
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DatabaseInitializer(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task InitializeAsync()
        {
            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();

            // Seed roles and features
            await SeedRolesAndFeaturesAsync();

            // Seed SuperAdmin user
            await SeedSuperAdminUserAsync();
        }

        private async Task SeedRolesAndFeaturesAsync()
        {
            // Define roles
            var roles = new[]
            {
                "SuperAdmin",
                "Admin",
                "Doctor",
                "Nurse",
                "Staff",
                "Receptionist",
                "Pharmacist",
                "LabTechnician",
                "Radiologist"
            };

            // Define features/permissions
            var features = new[]
            {
                // Patient Management
                new { Name = "ViewPatients", Module = "Patient", Description = "View patient records" },
                new { Name = "AddPatients", Module = "Patient", Description = "Add new patients" },
                new { Name = "EditPatients", Module = "Patient", Description = "Edit patient information" },
                new { Name = "DeletePatients", Module = "Patient", Description = "Delete patient records" },

                // Appointments
                new { Name = "ViewAppointments", Module = "Appointment", Description = "View appointments" },
                new { Name = "AddAppointments", Module = "Appointment", Description = "Schedule appointments" },
                new { Name = "EditAppointments", Module = "Appointment", Description = "Edit appointments" },
                new { Name = "DeleteAppointments", Module = "Appointment", Description = "Cancel appointments" },

                // Billing
                new { Name = "ViewBills", Module = "Billing", Description = "View bills and invoices" },
                new { Name = "AddBills", Module = "Billing", Description = "Create bills" },
                new { Name = "EditBills", Module = "Billing", Description = "Edit bills" },
                new { Name = "DeleteBills", Module = "Billing", Description = "Delete bills" },
                new { Name = "ProcessPayments", Module = "Billing", Description = "Process payments" },

                // OPD/IPD
                new { Name = "ViewOPDVisits", Module = "OPD", Description = "View OPD visits" },
                new { Name = "AddOPDVisits", Module = "OPD", Description = "Add OPD visits" },
                new { Name = "ViewIPDAdmissions", Module = "IPD", Description = "View IPD admissions" },
                new { Name = "AddIPDAdmissions", Module = "IPD", Description = "Add IPD admissions" },

                // Pharmacy
                new { Name = "ViewMedicines", Module = "Pharmacy", Description = "View medicines" },
                new { Name = "AddMedicines", Module = "Pharmacy", Description = "Add medicines" },
                new { Name = "DispenseMedicines", Module = "Pharmacy", Description = "Dispense medicines" },

                // Lab & Radiology
                new { Name = "ViewLabTests", Module = "Lab", Description = "View lab tests" },
                new { Name = "AddLabTests", Module = "Lab", Description = "Order lab tests" },
                new { Name = "ViewRadiologyTests", Module = "Radiology", Description = "View radiology tests" },
                new { Name = "AddRadiologyTests", Module = "Radiology", Description = "Order radiology tests" },

                // Administration
                new { Name = "ManageUsers", Module = "Admin", Description = "Manage user accounts" },
                new { Name = "ManageRoles", Module = "Admin", Description = "Manage roles and permissions" },
                new { Name = "ViewReports", Module = "Reports", Description = "View reports" },
                new { Name = "ManageSettings", Module = "Admin", Description = "Manage system settings" }
            };

            // Seed features
            foreach (var featureData in features)
            {
                if (!await _context.Features.AnyAsync(f => f.Name == featureData.Name))
                {
                    var feature = new Feature
                    {
                        Name = featureData.Name,
                        Module = featureData.Module,
                        Description = featureData.Description,
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.Features.Add(feature);
                }
            }

            // Seed roles
            foreach (var roleName in roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }

                // Create corresponding Role entity for RBAC
                if (!await _context.Roles.AnyAsync(r => r.Name == roleName))
                {
                    var role = new Role
                    {
                        Name = roleName,
                        Description = $"{roleName} role",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.Roles.Add(role);
                    await _context.SaveChangesAsync();

                    // Assign permissions based on role
                    await AssignRolePermissionsAsync(role.Id, roleName);
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task AssignRolePermissionsAsync(int roleId, string roleName)
        {
            var permissions = roleName switch
            {
                "SuperAdmin" => new[] {
                    "ViewPatients", "AddPatients", "EditPatients", "DeletePatients",
                    "ViewAppointments", "AddAppointments", "EditAppointments", "DeleteAppointments",
                    "ViewBills", "AddBills", "EditBills", "DeleteBills", "ProcessPayments",
                    "ViewOPDVisits", "AddOPDVisits", "ViewIPDAdmissions", "AddIPDAdmissions",
                    "ViewMedicines", "AddMedicines", "DispenseMedicines",
                    "ViewLabTests", "AddLabTests", "ViewRadiologyTests", "AddRadiologyTests",
                    "ManageUsers", "ManageRoles", "ViewReports", "ManageSettings"
                },
                "Admin" => new[] {
                    "ViewPatients", "AddPatients", "EditPatients",
                    "ViewAppointments", "AddAppointments", "EditAppointments",
                    "ViewBills", "AddBills", "EditBills", "ProcessPayments",
                    "ViewOPDVisits", "AddOPDVisits", "ViewIPDAdmissions", "AddIPDAdmissions",
                    "ViewMedicines", "AddMedicines", "DispenseMedicines",
                    "ViewLabTests", "AddLabTests", "ViewRadiologyTests", "AddRadiologyTests",
                    "ManageUsers", "ViewReports", "ManageSettings"
                },
                "Doctor" => new[] {
                    "ViewPatients", "EditPatients",
                    "ViewAppointments", "AddAppointments", "EditAppointments",
                    "ViewOPDVisits", "AddOPDVisits", "ViewIPDAdmissions", "AddIPDAdmissions",
                    "ViewLabTests", "AddLabTests", "ViewRadiologyTests", "AddRadiologyTests"
                },
                "Nurse" => new[] {
                    "ViewPatients",
                    "ViewAppointments",
                    "ViewOPDVisits", "AddOPDVisits", "ViewIPDAdmissions", "AddIPDAdmissions",
                    "ViewMedicines", "DispenseMedicines"
                },
                "Staff" => new[] {
                    "ViewPatients",
                    "ViewAppointments", "AddAppointments",
                    "ViewBills"
                },
                "Receptionist" => new[] {
                    "ViewPatients", "AddPatients", "EditPatients",
                    "ViewAppointments", "AddAppointments", "EditAppointments",
                    "ViewBills", "AddBills"
                },
                "Pharmacist" => new[] {
                    "ViewPatients",
                    "ViewMedicines", "AddMedicines", "DispenseMedicines"
                },
                "LabTechnician" => new[] {
                    "ViewPatients",
                    "ViewLabTests", "AddLabTests"
                },
                "Radiologist" => new[] {
                    "ViewPatients",
                    "ViewRadiologyTests", "AddRadiologyTests"
                },
                _ => Array.Empty<string>()
            };

            foreach (var permission in permissions)
            {
                var feature = await _context.Features.FirstOrDefaultAsync(f => f.Name == permission);
                if (feature != null)
                {
                    var roleFeature = new RoleFeature
                    {
                        RoleId = roleId,
                        FeatureId = feature.Id,
                        CanView = true,
                        CanAdd = permission.Contains("Add"),
                        CanEdit = permission.Contains("Edit"),
                        CanDelete = permission.Contains("Delete"),
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.RoleFeatures.Add(roleFeature);
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedSuperAdminUserAsync()
        {
            const string superAdminEmail = "superadmin@hospital.com";
            const string superAdminEmployeeId = "SUPER001";

            var superAdminUser = await _userManager.FindByEmailAsync(superAdminEmail);
            if (superAdminUser == null)
            {
                superAdminUser = new ApplicationUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    EmailConfirmed = true,
                    EmployeeId = superAdminEmployeeId,
                    FirstName = "Super",
                    LastName = "Admin",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(superAdminUser, "SuperAdmin@123!");
                if (result.Succeeded)
                {
                    // Assign SuperAdmin role
                    var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
                    if (superAdminRole != null)
                    {
                        var staffRole = new StaffRole
                        {
                            StaffId = superAdminUser.Id,
                            RoleId = superAdminRole.Id,
                            AssignedDate = DateTime.UtcNow,
                            AssignedBy = "System"
                        };
                        _context.StaffRoles.Add(staffRole);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }
    }
}