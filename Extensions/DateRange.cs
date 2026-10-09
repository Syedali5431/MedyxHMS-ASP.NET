namespace MedyxHMS.Extensions
{
    /// <summary>Helpers for date-range filters.</summary>
    public static class DateRange
    {
        /// <summary>
        /// An end date picked in a filter is midnight; a "to" date includes that whole day.
        /// A value with a time of day is kept as it is.
        /// </summary>
        public static DateTime EndOfDay(DateTime end) =>
            end.TimeOfDay == TimeSpan.Zero ? end.Date.AddDays(1).AddTicks(-1) : end;
    }
}
