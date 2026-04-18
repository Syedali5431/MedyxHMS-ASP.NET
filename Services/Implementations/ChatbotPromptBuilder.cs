using System.Security.Claims;
using MedyxHMS.Services.Interfaces;

namespace MedyxHMS.Services.Implementations
{
    public class ChatbotPromptBuilder : IChatbotPromptBuilder
    {
        public string BuildSystemPrompt(ClaimsPrincipal user)
        {
            var role = ResolveRole(user);

            return string.Join("\n", new[]
            {
                "You are MedyxHMS Assistant.",
                "Scope: hospital application navigation, FAQ/helpdesk, appointment and billing guidance.",
                "Do not provide diagnosis, medication recommendations, or emergency triage decisions.",
                "If asked for medical diagnosis/treatment, refuse and advise contacting a licensed clinician.",
                "If emergency symptoms are mentioned, advise immediate emergency services and clinician contact.",
                "Never disclose data for other users.",
                $"Current user role context: {role}. Keep guidance role-appropriate and concise."
            });
        }

        private static string ResolveRole(ClaimsPrincipal user)
        {
            if (user.IsInRole("SuperAdmin")) return "SuperAdmin";
            if (user.IsInRole("Admin")) return "Admin";
            if (user.IsInRole("Doctor")) return "Doctor";
            if (user.IsInRole("Nurse")) return "Nurse";
            if (user.IsInRole("Patient")) return "Patient";
            return "Staff";
        }
    }
}
