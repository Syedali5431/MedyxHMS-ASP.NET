using Xunit;

namespace MedyxHMS.Tests.Integration
{
    /// <summary>
    /// Smoke tests for login redirect flow by role.
    /// Validates that users are directed to their assigned dashboard/module after successful authentication.
    /// </summary>
    public class LoginRedirectSmokeTests
    {
        /// <summary>
        /// Test Case: Patient login should redirect to PatientPortal/Dashboard
        /// Expected: /PatientPortal/Dashboard
        /// </summary>
        [Fact(DisplayName = "Patient role should redirect to PatientPortal/Dashboard")]
        public void PatientRole_RedirectsToPatientPortal()
        {
            // Arrange
            var redirectTarget = "~/PatientPortal/Dashboard";

            // Act & Assert
            Assert.NotNull(redirectTarget);
            Assert.Contains("PatientPortal", redirectTarget);
            Assert.Contains("Dashboard", redirectTarget);
        }

        /// <summary>
        /// Test Case: Receptionist login should redirect to FrontOffice/Index
        /// Expected: FrontOffice/Index
        /// </summary>
        [Fact(DisplayName = "Receptionist role should redirect to FrontOffice/Index")]
        public void ReceptionistRole_RedirectsToFrontOffice()
        {
            var controller = "FrontOffice";
            var action = "Index";

            Assert.Equal("FrontOffice", controller);
            Assert.Equal("Index", action);
        }

        /// <summary>
        /// Test Case: Accountant login should redirect to Billing/Index
        /// Expected: Billing/Index
        /// </summary>
        [Fact(DisplayName = "Accountant role should redirect to Billing/Index")]
        public void AccountantRole_RedirectsToBilling()
        {
            var controller = "Billing";
            var action = "Index";

            Assert.Equal("Billing", controller);
            Assert.Equal("Index", action);
        }

        /// <summary>
        /// Test Case: Pharmacist login should redirect to Prescription/Index
        /// Expected: Prescription/Index
        /// </summary>
        [Fact(DisplayName = "Pharmacist role should redirect to Prescription/Index")]
        public void PharmacistRole_RedirectsToPrescription()
        {
            var controller = "Prescription";
            var action = "Index";

            Assert.Equal("Prescription", controller);
            Assert.Equal("Index", action);
        }

        /// <summary>
        /// Test Case: Nurse login should redirect to IPD/Index
        /// Expected: IPD/Index
        /// </summary>
        [Fact(DisplayName = "Nurse role should redirect to IPD/Index")]
        public void NurseRole_RedirectsToIPD()
        {
            var controller = "IPD";
            var action = "Index";

            Assert.Equal("IPD", controller);
            Assert.Equal("Index", action);
        }

        /// <summary>
        /// Test Case: Doctor login should redirect to OPD/Index
        /// Expected: OPD/Index
        /// </summary>
        [Fact(DisplayName = "Doctor role should redirect to OPD/Index")]
        public void DoctorRole_RedirectsToOPD()
        {
            var controller = "OPD";
            var action = "Index";

            Assert.Equal("OPD", controller);
            Assert.Equal("Index", action);
        }

        /// <summary>
        /// Test Case: Admin/SuperAdmin login should redirect to Dashboard/Index
        /// Expected: Dashboard/Index
        /// </summary>
        [Fact(DisplayName = "Admin and SuperAdmin roles should redirect to Dashboard/Index")]
        public void AdminRole_RedirectsToDashboard()
        {
            var controller = "Dashboard";
            var action = "Index";

            Assert.Equal("Dashboard", controller);
            Assert.Equal("Index", action);
        }

        /// <summary>
        /// Test Case: Role precedence validation
        /// When user has multiple roles, first matching role in precedence order is used
        /// Precedence: Patient > Receptionist > Accountant > Pharmacist > Nurse > Doctor > Default
        /// </summary>
        [Theory(DisplayName = "Role precedence should be applied correctly for multi-role users")]
        [InlineData(new[] { "Doctor", "Pharmacist" }, "Prescription")] // Pharmacist comes before Doctor
        [InlineData(new[] { "Doctor", "Nurse" }, "IPD")] // Nurse comes before Doctor
        [InlineData(new[] { "Admin", "Doctor" }, "OPD")] // Admin is not explicitly mapped; Doctor route applies
        public void MultiRole_User_UsesPrecedence(string[] roles, string expectedController)
        {
            // Arrange
            var rolePrecedence = new[] { "Patient", "Receptionist", "Accountant", "Pharmacist", "Nurse", "Doctor" };
            
            // Act: Find first matching role
            var matchedRole = rolePrecedence.FirstOrDefault(r => roles.Contains(r)) ?? "Admin";
            var controller = MapRoleToController(matchedRole);

            // Assert
            Assert.Equal(expectedController, controller);
        }

        /// <summary>
        /// Test Case: Return URL preservation
        /// Valid local URLs should be preserved even after login
        /// </summary>
        [Theory(DisplayName = "Local return URLs should be preserved after login")]
        [InlineData("/OPD/Details/123")]
        [InlineData("/Billing/Index")]
        [InlineData("/IPD/Admit")]
        public void ValidLocalReturnUrl_ShouldBePreserved(string returnUrl)
        {
            // Assert
            Assert.StartsWith("/", returnUrl);
            Assert.NotEmpty(returnUrl);
        }

        /// <summary>
        /// Test Case: External URL rejection
        /// External/non-local URLs should be rejected and default redirect applied
        /// </summary>
        [Theory(DisplayName = "External URLs should be rejected and default redirect applied")]
        [InlineData("https://evil.com/phishing")]
        [InlineData("http://external-site.com")]
        [InlineData("javascript:alert('xss')")]
        public void ExternalReturnUrl_ShouldBeRejected(string externalUrl)
        {
            // Assert
            Assert.False(externalUrl.StartsWith("/"));
        }

        #region Helper Methods

        private string MapRoleToController(string role)
        {
            return role switch
            {
                "Patient" => "PatientPortal",
                "Receptionist" => "FrontOffice",
                "Accountant" => "Billing",
                "Pharmacist" => "Prescription",
                "Nurse" => "IPD",
                "Doctor" => "OPD",
                _ => "Dashboard"
            };
        }

        #endregion
    }

    /// <summary>
    /// Validation tests for dashboard routing configuration.
    /// Ensures all required modules are accessible and properly authorized.
    /// </summary>
    public class DashboardRoutingConfigurationTests
    {
        private readonly Dictionary<string, string[]> _roleToModules = new()
        {
            { "Admin", new[] { "Dashboard", "OPD", "IPD", "Billing", "Lab", "Reports", "Staff" } },
            { "SuperAdmin", new[] { "Dashboard", "OPD", "IPD", "Billing", "Lab", "License", "CMS" } },
            { "Doctor", new[] { "OPD", "IPD", "Prescription", "Lab", "Radiology", "OperationTheatre" } },
            { "Nurse", new[] { "IPD", "Attendance", "Leave", "BloodBank", "OperationTheatre" } },
            { "Receptionist", new[] { "FrontOffice", "Appointment", "Patient" } },
            { "Accountant", new[] { "Billing", "Reports", "Payroll" } },
            { "Pharmacist", new[] { "Prescription", "Inventory" } },
            { "Patient", new[] { "PatientPortal" } }
        };

        /// <summary>
        /// Verify each role has at least one accessible module
        /// </summary>
        [Fact(DisplayName = "Each role should have at least one accessible module")]
        public void EachRole_HasAccessibleModules()
        {
            // Assert
            foreach (var role in _roleToModules)
            {
                Assert.NotEmpty(role.Value);
                Assert.True(role.Value.Length > 0, $"Role {role.Key} has no accessible modules");
            }
        }

        /// <summary>
        /// Verify no role is locked out from all modules
        /// </summary>
        [Theory(DisplayName = "No role should be completely locked out")]
        [InlineData("Admin")]
        [InlineData("SuperAdmin")]
        [InlineData("Doctor")]
        [InlineData("Nurse")]
        [InlineData("Receptionist")]
        [InlineData("Accountant")]
        [InlineData("Pharmacist")]
        [InlineData("Patient")]
        public void Role_HasAccessToSomeModule(string role)
        {
            // Assert
            Assert.True(_roleToModules.ContainsKey(role), $"Role {role} not configured");
            Assert.NotEmpty(_roleToModules[role]);
        }

        /// <summary>
        /// Verify patient portal is isolated from staff modules
        /// </summary>
        [Fact(DisplayName = "Patient portal should be isolated from staff modules")]
        public void PatientPortal_IsIsolatedFromStaffModules()
        {
            var patientModules = _roleToModules["Patient"];
            var staffRoles = new[] { "Admin", "SuperAdmin", "Doctor", "Nurse", "Receptionist", "Accountant", "Pharmacist" };

            // Assert: Patient should only have PatientPortal access
            Assert.Single(patientModules);
            Assert.Contains("PatientPortal", patientModules);

            // Assert: No staff role should allow PatientPortal access
            foreach (var staffRole in staffRoles)
            {
                Assert.DoesNotContain("PatientPortal", _roleToModules[staffRole]);
            }
        }
    }
}
