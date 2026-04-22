using MedyxHMS.Models;

// Purpose: Contains application code for CertificateViewModels and its related runtime behavior.
namespace MedyxHMS.ViewModels
{
    public class CertificateIndexViewModel
    {
        public string StaffIdFilter { get; set; }
        public List<Staff> StaffOptions { get; set; } = new();
        public List<CertificateRecord> Certificates { get; set; } = new();
        public List<IdCardRecord> IdCards { get; set; } = new();
    }

    public class GenerateCertificateViewModel
    {
        public CertificateRecord Certificate { get; set; } = new CertificateRecord { IssueDate = DateTime.Today };
        public List<Staff> StaffOptions { get; set; } = new();
    }

    public class GenerateIdCardViewModel
    {
        public IdCardRecord IdCard { get; set; } = new IdCardRecord { IssueDate = DateTime.Today, Status = "Active" };
        public List<Staff> StaffOptions { get; set; } = new();
    }
}
