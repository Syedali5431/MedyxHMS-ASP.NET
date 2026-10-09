namespace MedyxHMS.ViewModels
{
    /// <summary>Receipt printer options (Setup / Settings → Printers).</summary>
    public class ReceiptPrintSettings
    {
        /// <summary>Default paper roll width in mm (80). The width is chosen on the receipt when printing (80 / 58 mm).</summary>
        public int PaperWidth { get; set; } = 80;
        public string CurrencySymbol { get; set; } = DefaultCurrencySymbol;

        /// <summary>Pakistani rupee – used until another symbol is saved in Print Settings.</summary>
        public const string DefaultCurrencySymbol = "PKR";

        /// <summary>"PKR 1,250.00" (a symbol made of letters gets a space) or "$1,250.00".</summary>
        public string FormatMoney(decimal amount)
        {
            var symbol = (CurrencySymbol ?? string.Empty).Trim();
            var number = amount.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
            return symbol.Length == 0 ? number : symbol.Any(char.IsLetter) ? symbol + " " + number : symbol + number;
        }
        public string FooterText { get; set; } = "Thank you. Get well soon!";
        /// <summary>Open the browser print dialog as soon as the receipt opens.</summary>
        public bool AutoPrint { get; set; } = true;
        public bool ShowHospitalAddress { get; set; } = true;

        /// <summary>
        /// Windows: receipts and reports open the browser's print dialog, which prints to the default printer
        /// set in Windows. Network: they are sent straight to a printer from Settings → Printers (LAN / IP).
        /// </summary>
        public string PrintMode { get; set; } = PrintModeWindows;

        public const string PrintModeWindows = "Windows";
        public const string PrintModeNetwork = "Network";

        public bool UsesNetworkPrinters => PrintMode == PrintModeNetwork;
    }

    /// <summary>Header printed at the top of every receipt: the hospital the record belongs to.</summary>
    public class ReceiptHeader
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
    }

    public class ReceiptLine
    {
        public ReceiptLine(string label, string value) { Label = label; Value = value; }
        public string Label { get; }
        public string Value { get; }
    }

    public class ReceiptItem
    {
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
    }

    public class ReceiptTotal
    {
        public ReceiptTotal(string label, decimal amount, bool emphasis = false) { Label = label; Amount = amount; Emphasis = emphasis; }
        public string Label { get; }
        public decimal Amount { get; }
        public bool Emphasis { get; }
    }

    /// <summary>One thermal receipt / slip (bill, payment, pharmacy bill, appointment or OPD slip).</summary>
    public class ThermalReceiptViewModel
    {
        public string Title { get; set; } = string.Empty;
        public ReceiptHeader Header { get; set; } = new();
        public ReceiptPrintSettings Settings { get; set; } = new();
        public int PaperWidth { get; set; } = 80;

        /// <summary>Large number printed under the title (e.g. bill number or token number).</summary>
        public string? Highlight { get; set; }
        public string? HighlightLabel { get; set; }

        public List<ReceiptLine> Lines { get; set; } = new();
        public List<ReceiptItem> Items { get; set; } = new();
        public List<ReceiptTotal> Totals { get; set; } = new();
        public List<ReceiptLine> FooterLines { get; set; } = new();
        public string? Note { get; set; }

        public string PrintedBy { get; set; } = string.Empty;
        public DateTime PrintedAt { get; set; } = DateTime.Now;

        /// <summary>Page to return to from the on-screen toolbar.</summary>
        public string? BackUrl { get; set; }
    }
}
