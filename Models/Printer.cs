using System.ComponentModel.DataAnnotations;

namespace MedyxHMS.Models
{
    /// <summary>
    /// A printer on the network (Settings → Printers): an IP printer (raw TCP port, usually 9100) or a printer
    /// shared by a Windows computer on the LAN (\\COMPUTER\Printer). Used when the print mode is "network
    /// printers"; with the Windows default printer mode the browser's print dialog is used instead.
    /// </summary>
    public class Printer
    {
        public const string TypeReceipt = "Receipt";
        public const string TypeDocument = "Document";
        public const string ConnectionIp = "IP";
        public const string ConnectionLan = "LAN";
        public const string LanguageEscPos = "ESC/POS";
        public const string LanguagePdf = "PDF";
        public const string LanguageText = "Text";

        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Receipt (thermal roll: bills, payments, slips) or Document (A4/A5 sheets: reports).</summary>
        [Required, MaxLength(20)]
        public string PrinterType { get; set; } = TypeReceipt;

        /// <summary>IP (TCP/IP printer, raw port) or LAN (printer shared by a Windows computer).</summary>
        [Required, MaxLength(10)]
        public string ConnectionType { get; set; } = ConnectionIp;

        /// <summary>IP address or host name of an IP printer.</summary>
        [MaxLength(255)]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>Raw printing port of an IP printer (9100 on almost all printers).</summary>
        public int Port { get; set; } = 9100;

        /// <summary>Share path of a LAN printer, e.g. \\RECEPTION-PC\EPSON-TM-T82.</summary>
        [MaxLength(255)]
        public string SharePath { get; set; } = string.Empty;

        /// <summary>80mm / 58mm for receipt printers, A4 / A5 / Letter for document printers.</summary>
        [Required, MaxLength(20)]
        public string PaperSize { get; set; } = "80mm";

        /// <summary>What is sent: ESC/POS (receipt printers), PDF (printers with direct PDF printing) or plain text.</summary>
        [Required, MaxLength(20)]
        public string PrintLanguage { get; set; } = LanguageEscPos;

        public int Copies { get; set; } = 1;

        /// <summary>Receipt printers: cut the paper after each receipt.</summary>
        public bool CutPaper { get; set; } = true;

        /// <summary>Receipt printers: open the cash drawer connected to the printer.</summary>
        public bool OpenCashDrawer { get; set; }

        /// <summary>Hospital the printer belongs to; null = every hospital of the group.</summary>
        public int? HospitalId { get; set; }
        public Hospital? Hospital { get; set; }

        /// <summary>Printer used first for its type (per hospital).</summary>
        public bool IsDefault { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(500)]
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }

        [MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? LastTestedDate { get; set; }

        [MaxLength(500)]
        public string LastTestResult { get; set; } = string.Empty;

        /// <summary>"192.168.1.50:9100" or "\\PC\Printer".</summary>
        public string Address => ConnectionType == ConnectionLan ? SharePath : $"{IpAddress}:{Port}";

        public bool IsReceiptPrinter => PrinterType == TypeReceipt;
    }
}
