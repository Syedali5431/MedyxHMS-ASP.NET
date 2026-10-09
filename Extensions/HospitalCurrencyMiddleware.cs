using System.Globalization;
using MedyxHMS.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace MedyxHMS.Extensions
{
    /// <summary>
    /// Shows money in the hospital's currency everywhere. Amounts formatted with "C" used the server culture
    /// (en-US, so "$") while bills and receipts printed another symbol. The symbol now comes from one setting
    /// (Print Settings → currency symbol, default PKR); dates and number formats stay en-US.
    /// </summary>
    public class HospitalCurrencyMiddleware
    {
        private const string CacheKey = "settings:currency-culture";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

        private readonly RequestDelegate _next;

        public HospitalCurrencyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IReceiptPrintService printSettings, IMemoryCache cache)
        {
            if (!context.Request.Path.StartsWithSegments("/lib") && !context.Request.Path.StartsWithSegments("/css") && !context.Request.Path.StartsWithSegments("/js"))
            {
                var culture = await cache.GetOrCreateAsync(CacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                    var symbol = (await printSettings.GetSettingsAsync()).CurrencySymbol;
                    return Build(string.IsNullOrWhiteSpace(symbol) ? MedyxHMS.ViewModels.ReceiptPrintSettings.DefaultCurrencySymbol : symbol.Trim());
                });
                CultureInfo.CurrentCulture = culture!;
            }

            await _next(context);
        }

        /// <summary>Call after the currency symbol setting changes.</summary>
        public static void InvalidateCache(IMemoryCache cache) => cache.Remove(CacheKey);

        private static CultureInfo Build(string symbol)
        {
            var culture = (CultureInfo)CultureInfo.GetCultureInfo("en-US").Clone();
            culture.NumberFormat.CurrencySymbol = symbol;
            // "$1,250.00" / "-$1,250.00"; a symbol made of letters (e.g. "PKR") gets a space: "PKR 1,250.00".
            var spaced = symbol.Any(char.IsLetter);
            culture.NumberFormat.CurrencyPositivePattern = spaced ? 2 : 0;
            culture.NumberFormat.CurrencyNegativePattern = spaced ? 9 : 1;
            return culture;
        }
    }
}
