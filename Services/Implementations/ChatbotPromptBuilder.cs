using System.Security.Claims;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;

namespace MedyxHMS.Services.Implementations
{
    public class ChatbotPromptBuilder : IChatbotPromptBuilder
    {
        public string BuildSystemPrompt(ClaimsPrincipal user, ChatKnowledgeContext context)
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
                "Use only provided grounded context when answering workflow and policy questions.",
                "If context is insufficient, clearly say you do not have enough verified information.",
                "Add a final line in this format: Sources: source-1; source-2.",
                $"Current user role context: {role}. Keep guidance role-appropriate and concise.",
                "Grounded Context:",
                context.SystemContext
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
