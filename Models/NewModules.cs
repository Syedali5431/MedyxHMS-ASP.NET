// Models for M10 Ambulance, M12 Birth/Death, M15 TPA, M17 Messaging,
// M18 Inventory, M19 Download Center, M22 Live Consultation

namespace MedyxHMS.Models
{
    // ── M10 · Ambulance ──────────────────────────────────────────
    public class AmbulanceVehicle
    {
        public int Id { get; set; }
        public string VehicleNumber { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string DriverContact { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Status { get; set; } = "Available"; // Available, Dispatched, Maintenance
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class AmbulanceDispatch
    {
        public int Id { get; set; }
        public int AmbulanceVehicleId { get; set; }
        public int? PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PickupAddress { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty; // Emergency, Transfer, Discharge
        public DateTime DispatchTime { get; set; } = DateTime.UtcNow;
        public DateTime? ReturnTime { get; set; }
        public string Status { get; set; } = "Dispatched"; // Dispatched, Returned, Cancelled
        public decimal? DistanceKm { get; set; }
        public decimal? Charges { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public AmbulanceVehicle AmbulanceVehicle { get; set; } = null!;
        public Patient? Patient { get; set; }
    }

    // ── M12 · Birth / Death Records ──────────────────────────────
    public class BirthRecord
    {
        public int Id { get; set; }
        public string BabyName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;     // Male, Female, Other
        public DateTime DateOfBirth { get; set; }
        public string TimeOfBirth { get; set; } = string.Empty;
        public decimal WeightKg { get; set; }
        public string MotherName { get; set; } = string.Empty;
        public string FatherName { get; set; } = string.Empty;
        public string GuardianContact { get; set; } = string.Empty;
        public string DeliveryType { get; set; } = "Normal";   // Normal, C-Section
        public string AttendingDoctorName { get; set; } = string.Empty;
        public int? PatientId { get; set; }     // mother's patient record
        public string CertificateNumber { get; set; } = string.Empty;
        public bool CertificateIssued { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public Patient? Patient { get; set; }
    }

    public class DeathRecord
    {
        public int Id { get; set; }
        public int? PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime DateOfDeath { get; set; }
        public string TimeOfDeath { get; set; } = string.Empty;
        public string CauseOfDeath { get; set; } = string.Empty;
        public string AttendingDoctorName { get; set; } = string.Empty;
        public string NextOfKinName { get; set; } = string.Empty;
        public string NextOfKinContact { get; set; } = string.Empty;
        public string CertificateNumber { get; set; } = string.Empty;
        public bool CertificateIssued { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public Patient? Patient { get; set; }
    }

    // ── M15 · TPA (Third Party Administrator) ───────────────────
    public class TpaProvider
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string TpaNetwork { get; set; } = string.Empty;   // Insurance company name
        public bool IsActive { get; set; } = true;
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public ICollection<TpaClaim> Claims { get; set; } = new List<TpaClaim>();
    }

    public class TpaClaim
    {
        public int Id { get; set; }
        public int TpaProviderId { get; set; }
        public int PatientId { get; set; }
        public int? BillId { get; set; }
        public string ClaimNumber { get; set; } = string.Empty;
        public decimal ClaimedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public decimal? SettledAmount { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Settled
        public DateTime ClaimDate { get; set; } = DateTime.UtcNow;
        public DateTime? SettlementDate { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public TpaProvider TpaProvider { get; set; } = null!;
        public Patient Patient { get; set; } = null!;
        public Bill? Bill { get; set; }
    }

    // ── M17 · Internal Messaging ─────────────────────────────────
    public class InternalMessage
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;    // ApplicationUser.Id
        public string RecipientId { get; set; } = string.Empty; // ApplicationUser.Id (empty = broadcast)
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public bool IsBroadcast { get; set; }
        public int? ParentMessageId { get; set; }               // for replies
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
        public bool IsDeletedBySender { get; set; }
        public bool IsDeletedByRecipient { get; set; }

        public InternalMessage? ParentMessage { get; set; }
    }

    // ── M18 · Inventory ──────────────────────────────────────────
    public class InventoryItem
    {
        public int Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;   // Consumable, Equipment, Drug, Linen
        public string Unit { get; set; } = string.Empty;       // Pieces, Box, Kg, Litre
        public decimal CurrentStock { get; set; }
        public decimal MinimumStock { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal UnitCost { get; set; }
        public string Supplier { get; set; } = string.Empty;
        public string StorageLocation { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
    }

    public class InventoryTransaction
    {
        public int Id { get; set; }
        public int InventoryItemId { get; set; }
        public string TransactionType { get; set; } = "IN";   // IN, OUT, Adjustment
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string PerformedByUserId { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        public InventoryItem InventoryItem { get; set; } = null!;
    }

    // ── M19 · Download Center ────────────────────────────────────
    public class DownloadFile
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;  // Form, Policy, SOP, Report
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;  // pdf, docx, xlsx
        public long FileSizeBytes { get; set; }
        public int DownloadCount { get; set; }
        public string UploadedByUserId { get; set; } = string.Empty;
        public bool IsPublic { get; set; }                     // true = all staff; false = admin+
        public bool IsActive { get; set; } = true;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }

    // ── M22 · Live Consultation ──────────────────────────────────
    public class LiveConsultationSession
    {
        public int Id { get; set; }
        public int? PatientId { get; set; }
        public string DoctorUserId { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public DateTime ScheduledAt { get; set; }
        public int DurationMinutes { get; set; } = 30;
        public string Platform { get; set; } = "Zoom";         // Zoom, Teams, GoogleMeet, Custom
        public string MeetingLink { get; set; } = string.Empty;
        public string MeetingId { get; set; } = string.Empty;
        public string MeetingPassword { get; set; } = string.Empty;
        public string Status { get; set; } = "Scheduled";      // Scheduled, InProgress, Completed, Cancelled
        public string Notes { get; set; } = string.Empty;
        public int? BillId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public Patient? Patient { get; set; }
        public Bill? Bill { get; set; }
    }
}
