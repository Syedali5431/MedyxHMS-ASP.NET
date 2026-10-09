// Purpose: The Medyx logo on system-generated documents – PDF reports, invoices and lists, Excel exports,
// printed receipts (HTML) and ESC/POS network receipts (pre-rendered raster image).
namespace MedyxHMS.Services.Implementations
{
    public static class Branding
    {
        /// <summary>Logo for pages and printed HTML documents.</summary>
        public const string LogoUrl = "/images/logo.png";

        private static string? _webRootPath;
        private static readonly Lazy<byte[]?> LogoPngBytes = new(() => Load("logo.png"));
        private static readonly Lazy<byte[]?> LogoEscPosBytes = new(() => Load("logo-receipt.escpos"));

        /// <summary>Called once at start-up with the web root (wwwroot) of the running application.</summary>
        public static void Initialize(string? webRootPath) => _webRootPath = webRootPath;

        /// <summary>The logo as PNG (PDF and Excel documents), or null when the file is missing.</summary>
        public static byte[]? LogoPng => LogoPngBytes.Value;

        /// <summary>The logo as an ESC/POS "GS v 0" raster command, 192 dots wide (24 mm), or null when missing.</summary>
        public static byte[]? ReceiptLogoEscPos => LogoEscPosBytes.Value;

        private static byte[]? Load(string fileName)
        {
            var roots = new[]
            {
                _webRootPath,
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                Path.Combine(AppContext.BaseDirectory, "wwwroot")
            };
            foreach (var root in roots)
            {
                if (string.IsNullOrWhiteSpace(root)) continue;
                var file = Path.Combine(root, "images", fileName);
                if (File.Exists(file)) return File.ReadAllBytes(file);
            }
            return null;
        }
    }
}
