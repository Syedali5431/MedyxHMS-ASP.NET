// Purpose: Contains application code for ErrorViewModel and its related runtime behavior.
namespace MedyxHMS.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
