using System.Globalization;
using MedyxHMS.Data;
using MedyxHMS.Extensions;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.EntityFrameworkCore;
using K = MedyxHMS.ViewModels.ReportColumnKind;

namespace MedyxHMS.Services.Implementations
{
    /// <summary>
    /// The hospital's reports, built from live data. Hospital-scoped records (appointments, OPD, IPD, bills,
    /// pharmacy, inventory, OT…) follow the hospital selected in the top bar; group-wide records (lab, radiology,
    /// blood bank, HR, registers) cover the whole group. Empty periods show "no records" – never sample data.
    /// </summary>
    public class ReportEngine : IReportEngine
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private static readonly ReportDefinition[] All =
        {
            new("R1", "Daily Transaction Report", "Finance", "Payments received and refunds on one day (bill payments and pharmacy sales).", ReportFilterKind.SingleDate),
            new("R2", "All Transaction Report", "Finance", "All payments and refunds in the period with daily totals and payment methods.", ReportFilterKind.DateRange),
            new("R3", "Appointment Report", "Clinical", "Appointments in the period by status and doctor.", ReportFilterKind.DateRange),
            new("R4", "OPD Report", "Clinical", "Out-patient visits with consultation fees and payment status.", ReportFilterKind.DateRange),
            new("R5", "IPD Report", "Clinical", "In-patient admissions with ward, bed and length of stay.", ReportFilterKind.DateRange),
            new("R6", "OPD Balance Report", "Finance", "OPD bills of the period with an outstanding balance.", ReportFilterKind.DateRange),
            new("R7", "IPD Balance Report", "Finance", "IPD bills of the period with an outstanding balance.", ReportFilterKind.DateRange),
            new("R8", "OPD Discharged Patient Report", "Clinical", "Out-patients seen and sent home in the period, with diagnosis and treatment.", ReportFilterKind.DateRange),
            new("R9", "IPD Discharged Patient Report", "Clinical", "In-patients discharged in the period with length of stay.", ReportFilterKind.DateRange),
            new("R10", "Pharmacy Balance Report", "Pharmacy", "Pharmacy bills of the period that are not fully paid.", ReportFilterKind.DateRange),
            new("R11", "Expiry Medicine Report", "Pharmacy", "Medicines expired or expiring by the chosen date, with stock value.", ReportFilterKind.ExpiryRange),
            new("R12", "Pathology Patient Report", "Diagnostics", "Laboratory tests ordered in the period with results and status.", ReportFilterKind.DateRange),
            new("R13", "Radiology Patient Report", "Diagnostics", "Radiology examinations ordered in the period with findings and status.", ReportFilterKind.DateRange),
            new("R14", "Operation Theatre (OT) Report", "Clinical", "Operations scheduled in the period by theatre, surgeon and status.", ReportFilterKind.DateRange),
            new("R15", "Blood Issue Report", "Blood Bank", "Blood units issued to patients in the period.", ReportFilterKind.DateRange),
            new("R16", "Blood Component Issue Report", "Blood Bank", "Blood issued in the period by blood group, with the stock on hand.", ReportFilterKind.DateRange),
            new("R17", "Blood Donor Report", "Blood Bank", "Blood stock from donations by blood group, with reserved and minimum levels.", ReportFilterKind.None),
            new("R18", "Live Consultation Report", "Clinical", "Online consultations with patients in the period.", ReportFilterKind.DateRange),
            new("R19", "Live Meeting Report", "Clinical", "All online sessions in the period by platform.", ReportFilterKind.DateRange),
            new("R20", "TPA Report", "Finance", "Insurance (TPA) claims of the period: claimed, approved and settled amounts.", ReportFilterKind.DateRange),
            new("R21", "Income Report", "Finance", "Money received in the period: bill payments and pharmacy sales.", ReportFilterKind.DateRange),
            new("R22", "Income Group Report", "Finance", "Amounts billed in the period by income head.", ReportFilterKind.DateRange),
            new("R23", "Expense Report", "Finance", "Money paid out in the period: vendor payments and salaries.", ReportFilterKind.DateRange),
            new("R24", "Expense Group Report", "Finance", "Expenses of the period by expense head.", ReportFilterKind.DateRange),
            new("R25", "Ambulance Report", "Services", "Ambulance trips in the period with distance and charges.", ReportFilterKind.DateRange),
            new("R26", "Birth Report", "Registers", "Births registered in the period.", ReportFilterKind.DateRange),
            new("R27", "Death Report", "Registers", "Deaths registered in the period.", ReportFilterKind.DateRange),
            new("R28", "Payroll Month Report", "HR", "Salaries of one month per employee.", ReportFilterKind.Month),
            new("R29", "Payroll Report", "HR", "Payroll totals per month in the period.", ReportFilterKind.DateRange),
            new("R30", "Staff Attendance Report", "HR", "Attendance per employee in the period.", ReportFilterKind.DateRange),
            new("R31", "User Log Report", "System", "Sign-ins and sign-outs in the period.", ReportFilterKind.DateRange),
            new("R32", "Patient Login Credential Report", "System", "Patients with a Patient Portal account (passwords are never shown).", ReportFilterKind.None),
            new("R33", "Email / SMS Log Report", "System", "E-mails and text messages sent in the period.", ReportFilterKind.DateRange),
            new("R34", "Inventory Stock Report", "Inventory", "Current stock, reorder levels and stock value of all items.", ReportFilterKind.None),
            new("R35", "Inventory Item Report", "Inventory", "Stock received and issued per item in the period.", ReportFilterKind.DateRange),
            new("R36", "Inventory Issue Report", "Inventory", "Items issued in the period by department and patient.", ReportFilterKind.DateRange),
            new("R38", "Patient Visit Report", "Clinical", "Visits per patient in the period (appointments, OPD, admissions).", ReportFilterKind.DateRange),
            new("R39", "Patient Bill Report", "Finance", "Patient bills of the period with paid amounts and balances.", ReportFilterKind.DateRange),
            new("R40", "Referral Report", "Clinical", "Referrals made in the period.", ReportFilterKind.DateRange),
            new("R41", "Department Report", "Management", "Workload and consultation income per department.", ReportFilterKind.DateRange),
            new("R42", "Financial Report", "Management", "Billed, collected, outstanding and expenses with a monthly breakdown.", ReportFilterKind.DateRange),
            new("R43", "Occupancy Report", "Management", "Bed occupancy per ward on a date, with the 30-day average.", ReportFilterKind.SingleDate),
            new("R44", "Staff Report", "HR", "Attendance records of one employee or all staff in the period.", ReportFilterKind.DateRange),
            new("R50", "Discharge Summary Report", "Clinical", "Discharge reports of patients discharged in the period: condition, final diagnosis and whether the report is completed.", ReportFilterKind.DateRange),
        };

        private readonly ApplicationDbContext _db;
        private readonly IExportService _export;
        private readonly IHospitalContext _hospitalContext;

        public ReportEngine(ApplicationDbContext db, IExportService export, IHospitalContext hospitalContext)
        {
            _db = db;
            _export = export;
            _hospitalContext = hospitalContext;
        }

        /// <summary>One hospital of a multi-hospital group is selected (group-wide figures are then marked).</summary>
        private bool OneHospitalOfGroup => _hospitalContext.FilterEnabled && _hospitalContext.AllHospitals.Count > 1;

        private void NoteGroupWideSalaries(ReportDocument doc)
        {
            if (OneHospitalOfGroup)
                doc.Notes.Add("Salaries are paid for the whole group and are not split per hospital; vendor payments are those of this hospital's purchase bills.");
        }

        public IReadOnlyList<ReportDefinition> Definitions => All;

        /// <summary>The report definitions without an instance (areas and descriptions for the report catalog).</summary>
        public static IReadOnlyList<ReportDefinition> Catalog => All;

        public ReportDefinition? GetDefinition(string? key) =>
            All.FirstOrDefault(d => string.Equals(d.Key, key?.Trim(), StringComparison.OrdinalIgnoreCase));

        public async Task<ReportDocument?> BuildAsync(string? key, ReportParameters p)
        {
            var def = GetDefinition(key);
            if (def == null) return null;

            var doc = new ReportDocument { Key = def.Key, Title = def.Title, Description = def.Description, Category = def.Category, FilterKind = def.Filter };
            var today = DateTime.Now.Date;
            switch (def.Filter)
            {
                case ReportFilterKind.DateRange:
                    var start = (p.StartDate ?? today.AddMonths(-1)).Date;
                    var end = (p.EndDate ?? today).Date;
                    if (end < start) (start, end) = (end, start);
                    doc.StartDate = start;
                    doc.EndDate = end;
                    doc.PeriodText = start == end ? $"Date: {start:dd-MMM-yyyy}" : $"Period: {start.ToString("dd-MMM-yyyy", Inv)} to {end.ToString("dd-MMM-yyyy", Inv)}";
                    break;
                case ReportFilterKind.SingleDate:
                    doc.Date = (p.Date ?? p.EndDate ?? today).Date;
                    doc.PeriodText = "Date: " + doc.Date.Value.ToString("dddd, dd-MMM-yyyy", Inv);
                    break;
                case ReportFilterKind.Month:
                    var m = p.Month ?? p.Date ?? today;
                    doc.Month = new DateTime(m.Year, m.Month, 1);
                    doc.PeriodText = "Month: " + doc.Month.Value.ToString("MMMM yyyy", Inv);
                    break;
                case ReportFilterKind.ExpiryRange:
                    doc.EndDate = (p.EndDate ?? today.AddDays(90)).Date;
                    doc.PeriodText = "Expired or expiring by " + doc.EndDate.Value.ToString("dd-MMM-yyyy", Inv);
                    break;
                default:
                    doc.PeriodText = "As of " + DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt", Inv);
                    break;
            }

            doc.StaffId = string.IsNullOrWhiteSpace(p.StaffId) ? null : p.StaffId.Trim();
            _export.FillLetterhead(doc);

            var from = doc.StartDate ?? today;
            var to = DateRange.EndOfDay(doc.EndDate ?? today);
            switch (def.Key)
            {
                case "R1": await Transactions(doc, doc.Date!.Value, DateRange.EndOfDay(doc.Date.Value), daily: true); break;
                case "R2": await Transactions(doc, from, to, daily: false); break;
                case "R3": await Appointments(doc, from, to); break;
                case "R4": await OpdVisits(doc, from, to, discharged: false); break;
                case "R5": await IpdAdmissions(doc, from, to); break;
                case "R6": await BillBalances(doc, from, to, "OPD"); break;
                case "R7": await BillBalances(doc, from, to, "IPD"); break;
                case "R8": await OpdVisits(doc, from, to, discharged: true); break;
                case "R9": await IpdDischarges(doc, from, to); break;
                case "R10": await PharmacyBalances(doc, from, to); break;
                case "R11": await ExpiringMedicines(doc); break;
                case "R12": await Pathology(doc, from, to); break;
                case "R13": await Radiology(doc, from, to); break;
                case "R14": await OperationTheatre(doc, from, to); break;
                case "R15": await BloodIssues(doc, from, to); break;
                case "R16": await BloodComponents(doc, from, to); break;
                case "R17": await BloodStock(doc); break;
                case "R18": await LiveSessions(doc, from, to, patientsOnly: true); break;
                case "R19": await LiveSessions(doc, from, to, patientsOnly: false); break;
                case "R20": await TpaClaims(doc, from, to); break;
                case "R21": await Income(doc, from, to); break;
                case "R22": await IncomeGroups(doc, from, to); break;
                case "R23": await Expenses(doc, from, to); break;
                case "R24": await ExpenseGroups(doc, from, to); break;
                case "R25": await Ambulance(doc, from, to); break;
                case "R26": await Births(doc, from, to); break;
                case "R27": await Deaths(doc, from, to); break;
                case "R28": await PayrollMonth(doc, doc.Month!.Value); break;
                case "R29": await PayrollPeriod(doc, from, to); break;
                case "R30": await Attendance(doc, from, to); break;
                case "R31": await UserLog(doc, from, to); break;
                case "R32": await PatientAccounts(doc); break;
                case "R33": await NotificationLog(doc, from, to); break;
                case "R34": await InventoryStock(doc); break;
                case "R35": await InventoryItems(doc, from, to); break;
                case "R36": await InventoryIssues(doc, from, to); break;
                case "R38": await PatientVisits(doc, from, to); break;
                case "R39": await PatientBills(doc, from, to); break;
                case "R40": await Referrals(doc, from, to); break;
                case "R41": await Departments(doc, from, to); break;
                case "R42": await Financial(doc, from, to); break;
                case "R43": await Occupancy(doc, doc.Date!.Value); break;
                case "R44": await StaffAttendanceRecords(doc, from, to); break;
                case "R50": await DischargeReports(doc, from, to); break;
            }

            return doc;
        }

        // ── Finance ──────────────────────────────────────────────

        private sealed record MoneyRow(DateTime Date, string Type, string Reference, string Patient, string Method, string Status, string By, decimal Amount);

        private async Task<List<MoneyRow>> MoneyInAsync(DateTime from, DateTime to)
        {
            var payments = await _db.Payments.AsNoTracking()
                .Where(x => x.PaymentDate >= from && x.PaymentDate <= to)
                .Select(x => new { x.PaymentDate, x.Bill.BillNumber, x.Bill.Patient.FirstName, x.Bill.Patient.LastName, x.PaymentMethod, x.Status, x.ProcessedBy, x.Amount, x.TransactionId })
                .ToListAsync();
            var pharmacy = await _db.PharmacyBills.AsNoTracking()
                .Where(x => x.BillDate >= from && x.BillDate <= to && x.PaidAmount != 0)
                .Select(x => new { x.BillDate, x.BillNumber, x.Patient.FirstName, x.Patient.LastName, x.PaymentMethod, x.Status, x.CreatedBy, x.PaidAmount })
                .ToListAsync();

            var rows = payments.Select(x =>
            {
                var refund = x.Amount < 0 || string.Equals(x.Status, "Refunded", StringComparison.OrdinalIgnoreCase);
                return new MoneyRow(x.PaymentDate, refund ? "Refund" : "Bill payment", x.BillNumber, $"{x.FirstName} {x.LastName}".Trim(),
                    x.PaymentMethod, x.Status, x.ProcessedBy, refund ? -Math.Abs(x.Amount) : x.Amount);
            }).ToList();
            rows.AddRange(pharmacy.Select(x => new MoneyRow(x.BillDate, "Pharmacy sale", x.BillNumber, $"{x.FirstName} {x.LastName}".Trim(),
                string.IsNullOrWhiteSpace(x.PaymentMethod) ? "Cash" : x.PaymentMethod, x.Status, x.CreatedBy, x.PaidAmount)));
            return rows.OrderBy(r => r.Date).ToList();
        }

        private async Task Transactions(ReportDocument doc, DateTime from, DateTime to, bool daily)
        {
            var rows = await MoneyInAsync(from, to);
            var received = rows.Where(r => r.Amount > 0).Sum(r => r.Amount);
            var refunds = rows.Where(r => r.Amount < 0).Sum(r => -r.Amount);
            doc.Metrics.Add(new ReportMetric("Transactions", rows.Count));
            doc.Metrics.Add(new ReportMetric("Received", received, K.Money, "success"));
            doc.Metrics.Add(new ReportMetric("Refunds", refunds, K.Money, "danger"));
            doc.Metrics.Add(new ReportMetric("Net", received - refunds, K.Money, "primary"));

            doc.Sections.Add(new ReportSection
            {
                Title = "Transactions",
                Columns =
                {
                    new(daily ? "Time" : "Date & time", K.DateTime, 1.3f), new("Type", width: 1), new("Reference", width: 1.1f), new("Patient", width: 1.4f),
                    new("Method", width: 0.9f), new("Status", width: 0.9f), new("Processed by", width: 1), new("Amount", K.Money, 1.1f, true)
                },
                Rows = rows.Select(r => new object?[] { r.Date, r.Type, r.Reference, r.Patient, r.Method, r.Status, r.By, r.Amount }).ToList(),
                ShowTotals = true,
                EmptyText = daily ? "No payments or refunds on this day." : "No payments or refunds in this period."
            });

            doc.Charts.Add(Chart("Received by payment method", rows.Where(r => r.Amount > 0).GroupBy(r => string.IsNullOrWhiteSpace(r.Method) ? "Other" : r.Method), g => g.Sum(r => r.Amount), money: true, type: "doughnut"));
            if (!daily)
            {
                var byDay = rows.GroupBy(r => r.Date.Date).OrderBy(g => g.Key).ToList();
                doc.Charts.Add(new ReportChartData { Title = "Net received per day", IsMoney = true, Labels = byDay.Select(g => g.Key.ToString("dd-MMM", Inv)).ToList(), Values = byDay.Select(g => g.Sum(r => r.Amount)).ToList() });
            }
        }

        private async Task Income(ReportDocument doc, DateTime from, DateTime to)
        {
            var rows = (await MoneyInAsync(from, to)).Where(r => r.Amount > 0 && !string.Equals(r.Status, "Failed", StringComparison.OrdinalIgnoreCase)).ToList();
            doc.Metrics.Add(new ReportMetric("Total income", rows.Sum(r => r.Amount), K.Money, "success"));
            doc.Metrics.Add(new ReportMetric("Bill payments", rows.Where(r => r.Type == "Bill payment").Sum(r => r.Amount), K.Money, "primary"));
            doc.Metrics.Add(new ReportMetric("Pharmacy sales", rows.Where(r => r.Type == "Pharmacy sale").Sum(r => r.Amount), K.Money, "info"));
            doc.Metrics.Add(new ReportMetric("Receipts", rows.Count));
            doc.Sections.Add(new ReportSection
            {
                Title = "Income",
                Columns = { new("Date", K.DateTime, 1.2f), new("Source", width: 1), new("Reference", width: 1.1f), new("Patient", width: 1.5f), new("Method", width: 0.9f), new("Received by", width: 1), new("Amount", K.Money, 1.1f, true) },
                Rows = rows.Select(r => new object?[] { r.Date, r.Type, r.Reference, r.Patient, r.Method, r.By, r.Amount }).ToList(),
                ShowTotals = true
            });
            doc.Charts.Add(Chart("Income by source", rows.GroupBy(r => r.Type), g => g.Sum(r => r.Amount), money: true, type: "doughnut"));
            doc.Charts.Add(Chart("Income by payment method", rows.GroupBy(r => r.Method), g => g.Sum(r => r.Amount), money: true));
        }

        private async Task IncomeGroups(ReportDocument doc, DateTime from, DateTime to)
        {
            var items = await _db.BillItems.AsNoTracking()
                .Where(i => i.Bill.BillDate >= from && i.Bill.BillDate <= to)
                .Select(i => new { i.ItemType, i.ItemName, i.TotalPrice, i.Bill.BillType })
                .ToListAsync();
            var pharmacy = await _db.PharmacyBills.AsNoTracking().Where(b => b.BillDate >= from && b.BillDate <= to).SumAsync(b => (decimal?)b.TotalAmount) ?? 0;
            var pharmacyCount = await _db.PharmacyBills.AsNoTracking().CountAsync(b => b.BillDate >= from && b.BillDate <= to);

            var groups = items.GroupBy(i => IncomeHead(i.ItemType, i.BillType))
                .Select(g => (Head: g.Key, Count: g.Count(), Amount: g.Sum(i => i.TotalPrice)))
                .ToList();
            if (pharmacy != 0 || pharmacyCount > 0) groups.Add(("Pharmacy", pharmacyCount, pharmacy));
            groups = groups.OrderByDescending(g => g.Amount).ToList();
            var total = groups.Sum(g => g.Amount);

            doc.Metrics.Add(new ReportMetric("Total billed", total, K.Money, "success"));
            doc.Metrics.Add(new ReportMetric("Income heads", groups.Count));
            doc.Metrics.Add(new ReportMetric("Billed items", groups.Sum(g => g.Count)));
            doc.Sections.Add(new ReportSection
            {
                Title = "Income by head",
                Columns = { new("Income head", width: 2), new("Items / bills", K.Integer, 1, true), new("Amount billed", K.Money, 1.3f, true), new("Share", K.Percent, 0.8f) },
                Rows = groups.Select(g => new object?[] { g.Head, g.Count, g.Amount, total == 0 ? 0 : g.Amount * 100 / total }).ToList(),
                ShowTotals = true
            });
            doc.Charts.Add(new ReportChartData { Title = "Share of income", Type = "doughnut", IsMoney = true, Labels = groups.Select(g => g.Head).ToList(), Values = groups.Select(g => g.Amount).ToList() });
            doc.Notes.Add("Amounts billed in the period (bill items and pharmacy bills); see the Income Report for money received.");
        }

        private static string IncomeHead(string itemType, string billType)
        {
            var type = string.IsNullOrWhiteSpace(itemType) ? "Other" : itemType.Trim();
            return type switch
            {
                "Bed" => "Room / bed charges",
                "Service" => billType == "IPD" ? "IPD services" : "OPD services",
                _ => type
            };
        }

        private sealed record ExpenseRow(DateTime Date, string Head, string Reference, string Payee, string Method, decimal Amount, string Category);

        private async Task<List<ExpenseRow>> ExpensesAsync(DateTime from, DateTime to)
        {
            var vendor = await _db.VendorPayments.AsNoTracking()
                .Where(v => v.PaymentDate >= from && v.PaymentDate <= to)
                .Select(v => new { v.PaymentDate, v.PurchaseBill.BillNumber, Vendor = v.PurchaseBill.Vendor.Name, v.PurchaseBill.Vendor.Category, v.Method, v.Reference, v.Amount })
                .ToListAsync();
            var salaries = await _db.PayrollRecords.AsNoTracking()
                .Where(r => r.PaymentDate != null && r.PaymentDate >= from && r.PaymentDate <= to && r.Status == "Paid")
                .Select(r => new { r.PaymentDate, r.PayrollMonth, r.Staff.FirstName, r.Staff.LastName, r.Staff.EmployeeId, r.NetSalary })
                .ToListAsync();

            var rows = vendor.Select(v => new ExpenseRow(v.PaymentDate, "Vendor payment", string.IsNullOrWhiteSpace(v.Reference) ? v.BillNumber : $"{v.BillNumber} / {v.Reference}",
                v.Vendor, v.Method, v.Amount, string.IsNullOrWhiteSpace(v.Category) ? "Purchases" : "Purchases – " + v.Category)).ToList();
            rows.AddRange(salaries.Select(s => new ExpenseRow(s.PaymentDate!.Value, "Salary", $"Payroll {s.PayrollMonth:MMM yyyy} · {s.EmployeeId}",
                $"{s.FirstName} {s.LastName}".Trim(), "Bank / cash", s.NetSalary, "Salaries & wages")));
            return rows.OrderBy(r => r.Date).ToList();
        }

        private async Task Expenses(ReportDocument doc, DateTime from, DateTime to)
        {
            var rows = await ExpensesAsync(from, to);
            doc.Metrics.Add(new ReportMetric("Total expenses", rows.Sum(r => r.Amount), K.Money, "danger"));
            doc.Metrics.Add(new ReportMetric("Vendor payments", rows.Where(r => r.Head == "Vendor payment").Sum(r => r.Amount), K.Money, "warning"));
            doc.Metrics.Add(new ReportMetric("Salaries", rows.Where(r => r.Head == "Salary").Sum(r => r.Amount), K.Money, "info"));
            doc.Metrics.Add(new ReportMetric("Payments", rows.Count));
            doc.Sections.Add(new ReportSection
            {
                Title = "Expenses",
                Columns = { new("Date", K.Date), new("Head", width: 1), new("Reference", width: 1.5f), new("Paid to", width: 1.5f), new("Method", width: 0.9f), new("Amount", K.Money, 1.1f, true) },
                Rows = rows.Select(r => new object?[] { r.Date, r.Head, r.Reference, r.Payee, r.Method, r.Amount }).ToList(),
                ShowTotals = true
            });
            doc.Charts.Add(Chart("Expenses by head", rows.GroupBy(r => r.Head), g => g.Sum(r => r.Amount), money: true, type: "doughnut"));
            NoteGroupWideSalaries(doc);
        }

        private async Task ExpenseGroups(ReportDocument doc, DateTime from, DateTime to)
        {
            var rows = await ExpensesAsync(from, to);
            var total = rows.Sum(r => r.Amount);
            var groups = rows.GroupBy(r => r.Category).Select(g => (Head: g.Key, Count: g.Count(), Amount: g.Sum(r => r.Amount))).OrderByDescending(g => g.Amount).ToList();
            doc.Metrics.Add(new ReportMetric("Total expenses", total, K.Money, "danger"));
            doc.Metrics.Add(new ReportMetric("Expense heads", groups.Count));
            doc.Sections.Add(new ReportSection
            {
                Title = "Expenses by head",
                Columns = { new("Expense head", width: 2), new("Payments", K.Integer, 1, true), new("Amount", K.Money, 1.3f, true), new("Share", K.Percent, 0.8f) },
                Rows = groups.Select(g => new object?[] { g.Head, g.Count, g.Amount, total == 0 ? 0 : g.Amount * 100 / total }).ToList(),
                ShowTotals = true
            });
            doc.Charts.Add(new ReportChartData { Title = "Share of expenses", Type = "doughnut", IsMoney = true, Labels = groups.Select(g => g.Head).ToList(), Values = groups.Select(g => g.Amount).ToList() });
            NoteGroupWideSalaries(doc);
        }

        private async Task BillBalances(ReportDocument doc, DateTime from, DateTime to, string billType)
        {
            var bills = await _db.Bills.AsNoTracking()
                .Where(b => b.BillType == billType && b.BillDate >= from && b.BillDate <= to && b.TotalAmount - b.PaidAmount > 0)
                .OrderBy(b => b.BillDate)
                .Select(b => new { b.BillNumber, b.BillDate, b.DueDate, b.Patient.PatientId, b.Patient.FirstName, b.Patient.LastName, b.Patient.Phone, b.TotalAmount, b.PaidAmount, b.Status })
                .ToListAsync();
            var overdue = bills.Count(b => b.DueDate.Date < DateTime.Now.Date);
            doc.Metrics.Add(new ReportMetric("Bills with balance", bills.Count));
            doc.Metrics.Add(new ReportMetric("Billed", bills.Sum(b => b.TotalAmount), K.Money));
            doc.Metrics.Add(new ReportMetric("Paid", bills.Sum(b => b.PaidAmount), K.Money, "success"));
            doc.Metrics.Add(new ReportMetric("Outstanding", bills.Sum(b => b.TotalAmount - b.PaidAmount), K.Money, "danger"));
            doc.Metrics.Add(new ReportMetric("Overdue bills", overdue, K.Integer, "warning"));
            doc.Sections.Add(new ReportSection
            {
                Title = $"{billType} bills with an outstanding balance",
                Columns =
                {
                    new("Bill No.", width: 1.1f), new("Bill date", K.Date), new("Due date", K.Date), new("Patient ID", width: 0.9f), new("Patient", width: 1.4f), new("Phone", width: 1),
                    new("Total", K.Money, 1, true), new("Paid", K.Money, 1, true), new("Balance", K.Money, 1, true), new("Status", width: 0.9f)
                },
                Rows = bills.Select(b => new object?[] { b.BillNumber, b.BillDate, b.DueDate, b.PatientId, $"{b.FirstName} {b.LastName}", b.Phone, b.TotalAmount, b.PaidAmount, b.TotalAmount - b.PaidAmount, b.Status }).ToList(),
                ShowTotals = true,
                EmptyText = $"No {billType} bills with a balance in this period."
            });
        }

        private async Task PharmacyBalances(ReportDocument doc, DateTime from, DateTime to)
        {
            var bills = await _db.PharmacyBills.AsNoTracking()
                .Where(b => b.BillDate >= from && b.BillDate <= to && b.TotalAmount - b.PaidAmount > 0)
                .OrderBy(b => b.BillDate)
                .Select(b => new { b.BillNumber, b.BillDate, b.Patient.PatientId, b.Patient.FirstName, b.Patient.LastName, b.TotalAmount, b.PaidAmount, b.Status, Items = b.Prescriptions.Count })
                .ToListAsync();
            doc.Metrics.Add(new ReportMetric("Bills with balance", bills.Count));
            doc.Metrics.Add(new ReportMetric("Billed", bills.Sum(b => b.TotalAmount), K.Money));
            doc.Metrics.Add(new ReportMetric("Paid", bills.Sum(b => b.PaidAmount), K.Money, "success"));
            doc.Metrics.Add(new ReportMetric("Outstanding", bills.Sum(b => b.TotalAmount - b.PaidAmount), K.Money, "danger"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Pharmacy bills not fully paid",
                Columns = { new("Bill No.", width: 1.1f), new("Date", K.Date), new("Patient ID"), new("Patient", width: 1.5f), new("Items", K.Integer, 0.6f), new("Total", K.Money, 1, true), new("Paid", K.Money, 1, true), new("Balance", K.Money, 1, true), new("Status") },
                Rows = bills.Select(b => new object?[] { b.BillNumber, b.BillDate, b.PatientId, $"{b.FirstName} {b.LastName}", b.Items, b.TotalAmount, b.PaidAmount, b.TotalAmount - b.PaidAmount, b.Status }).ToList(),
                ShowTotals = true
            });
        }

        private async Task PatientBills(ReportDocument doc, DateTime from, DateTime to)
        {
            var bills = await _db.Bills.AsNoTracking()
                .Where(b => b.BillDate >= from && b.BillDate <= to)
                .OrderBy(b => b.BillDate)
                .Select(b => new { b.BillNumber, b.BillDate, b.BillType, b.Patient.PatientId, b.Patient.FirstName, b.Patient.LastName, b.TotalAmount, b.PaidAmount, b.Status })
                .ToListAsync();
            doc.Metrics.Add(new ReportMetric("Bills", bills.Count));
            doc.Metrics.Add(new ReportMetric("Billed", bills.Sum(b => b.TotalAmount), K.Money));
            doc.Metrics.Add(new ReportMetric("Collected", bills.Sum(b => b.PaidAmount), K.Money, "success"));
            doc.Metrics.Add(new ReportMetric("Balance", bills.Sum(b => b.TotalAmount - b.PaidAmount), K.Money, "danger"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Patient bills",
                Columns = { new("Bill No.", width: 1.1f), new("Date", K.Date), new("Type", width: 0.6f), new("Patient ID"), new("Patient", width: 1.5f), new("Total", K.Money, 1, true), new("Paid", K.Money, 1, true), new("Balance", K.Money, 1, true), new("Status") },
                Rows = bills.Select(b => new object?[] { b.BillNumber, b.BillDate, b.BillType, b.PatientId, $"{b.FirstName} {b.LastName}", b.TotalAmount, b.PaidAmount, b.TotalAmount - b.PaidAmount, b.Status }).ToList(),
                ShowTotals = true
            });
            doc.Charts.Add(Chart("Bills by status", bills.GroupBy(b => b.Status), g => g.Count(), type: "doughnut"));
            doc.Charts.Add(Chart("Billed by type", bills.GroupBy(b => b.BillType), g => g.Sum(b => b.TotalAmount), money: true));
        }

        private async Task TpaClaims(ReportDocument doc, DateTime from, DateTime to)
        {
            var claims = await _db.TpaClaims.AsNoTracking()
                .Where(c => c.ClaimDate >= from && c.ClaimDate <= to)
                .OrderBy(c => c.ClaimDate)
                .Select(c => new { c.ClaimNumber, c.ClaimDate, Tpa = c.TpaProvider.Name, c.Patient.PatientId, c.Patient.FirstName, c.Patient.LastName, c.ClaimedAmount, c.ApprovedAmount, c.SettledAmount, c.Status, c.SettlementDate })
                .ToListAsync();
            doc.Metrics.Add(new ReportMetric("Claims", claims.Count));
            doc.Metrics.Add(new ReportMetric("Claimed", claims.Sum(c => c.ClaimedAmount), K.Money));
            doc.Metrics.Add(new ReportMetric("Approved", claims.Sum(c => c.ApprovedAmount ?? 0), K.Money, "info"));
            doc.Metrics.Add(new ReportMetric("Settled", claims.Sum(c => c.SettledAmount ?? 0), K.Money, "success"));
            doc.Sections.Add(new ReportSection
            {
                Title = "TPA claims",
                Columns = { new("Claim No.", width: 1.1f), new("Date", K.Date), new("TPA", width: 1.3f), new("Patient ID"), new("Patient", width: 1.4f), new("Claimed", K.Money, 1, true), new("Approved", K.Money, 1, true), new("Settled", K.Money, 1, true), new("Status"), new("Settled on", K.Date) },
                Rows = claims.Select(c => new object?[] { c.ClaimNumber, c.ClaimDate, c.Tpa, c.PatientId, $"{c.FirstName} {c.LastName}", c.ClaimedAmount, c.ApprovedAmount ?? 0, c.SettledAmount ?? 0, c.Status, c.SettlementDate }).ToList(),
                ShowTotals = true
            });
            doc.Charts.Add(Chart("Claimed by TPA", claims.GroupBy(c => c.Tpa), g => g.Sum(c => c.ClaimedAmount), money: true));
        }

        private async Task Financial(ReportDocument doc, DateTime from, DateTime to)
        {
            var bills = await _db.Bills.AsNoTracking().Where(b => b.BillDate >= from && b.BillDate <= to)
                .Select(b => new { b.BillDate, b.TotalAmount, b.PaidAmount }).ToListAsync();
            var pharmacyBills = await _db.PharmacyBills.AsNoTracking().Where(b => b.BillDate >= from && b.BillDate <= to)
                .Select(b => new { b.BillDate, b.TotalAmount }).ToListAsync();
            var income = (await MoneyInAsync(from, to)).Where(r => !string.Equals(r.Status, "Failed", StringComparison.OrdinalIgnoreCase)).ToList();
            var expenses = await ExpensesAsync(from, to);

            var billed = bills.Sum(b => b.TotalAmount) + pharmacyBills.Sum(b => b.TotalAmount);
            var collected = income.Sum(r => r.Amount);
            var outstanding = bills.Sum(b => b.TotalAmount - b.PaidAmount);
            var spent = expenses.Sum(e => e.Amount);
            var net = collected - spent;

            doc.Metrics.Add(new ReportMetric("Billed", billed, K.Money));
            doc.Metrics.Add(new ReportMetric("Collected", collected, K.Money, "success"));
            doc.Metrics.Add(new ReportMetric("Outstanding", outstanding, K.Money, "warning"));
            doc.Metrics.Add(new ReportMetric("Expenses", spent, K.Money, "danger"));
            doc.Metrics.Add(new ReportMetric("Net (collected – expenses)", net, K.Money, net >= 0 ? "success" : "danger"));

            decimal Pct(decimal v) => billed == 0 ? 0 : v * 100 / billed;
            doc.Sections.Add(new ReportSection
            {
                Title = "Summary",
                Columns = { new("Item", width: 2.5f), new("Amount", K.Money, 1.2f), new("% of billed", K.Percent, 0.8f) },
                Rows =
                {
                    new object?[] { "Amount billed (patient bills and pharmacy)", billed, billed == 0 ? 0 : 100m },
                    new object?[] { "Money collected (payments, refunds deducted)", collected, Pct(collected) },
                    new object?[] { "Outstanding on bills of the period", outstanding, Pct(outstanding) },
                    new object?[] { "Vendor payments", expenses.Where(e => e.Head == "Vendor payment").Sum(e => e.Amount), null },
                    new object?[] { "Salaries paid", expenses.Where(e => e.Head == "Salary").Sum(e => e.Amount), null },
                    new object?[] { "Net result (collected – expenses)", net, null }
                }
            });

            var months = new List<DateTime>();
            for (var m = new DateTime(from.Year, from.Month, 1); m <= to; m = m.AddMonths(1)) months.Add(m);
            bool InMonth(DateTime d, DateTime m) => d.Year == m.Year && d.Month == m.Month;
            var monthly = months.Select(m => new object?[]
            {
                m.ToString("MMM yyyy", Inv),
                bills.Where(b => InMonth(b.BillDate, m)).Sum(b => b.TotalAmount) + pharmacyBills.Where(b => InMonth(b.BillDate, m)).Sum(b => b.TotalAmount),
                income.Where(r => InMonth(r.Date, m)).Sum(r => r.Amount),
                expenses.Where(e => InMonth(e.Date, m)).Sum(e => e.Amount),
                income.Where(r => InMonth(r.Date, m)).Sum(r => r.Amount) - expenses.Where(e => InMonth(e.Date, m)).Sum(e => e.Amount)
            }).ToList();
            doc.Sections.Add(new ReportSection
            {
                Title = "By month",
                Columns = { new("Month", width: 1.2f), new("Billed", K.Money, 1, true), new("Collected", K.Money, 1, true), new("Expenses", K.Money, 1, true), new("Net", K.Money, 1, true) },
                Rows = monthly,
                ShowTotals = true
            });
            doc.Charts.Add(new ReportChartData { Title = "Collected per month", IsMoney = true, Labels = months.Select(m => m.ToString("MMM yyyy", Inv)).ToList(), Values = monthly.Select(r => (decimal)r[2]!).ToList() });
            doc.Charts.Add(new ReportChartData { Title = "Where the money went", Type = "doughnut", IsMoney = true, Labels = { "Vendor payments", "Salaries" }, Values = { expenses.Where(e => e.Head == "Vendor payment").Sum(e => e.Amount), expenses.Where(e => e.Head == "Salary").Sum(e => e.Amount) } });
            NoteGroupWideSalaries(doc);
        }

        // ── Clinical ─────────────────────────────────────────────

        private async Task Appointments(ReportDocument doc, DateTime from, DateTime to)
        {
            var rows = await _db.Appointments.AsNoTracking()
                .Where(a => a.AppointmentDate >= from && a.AppointmentDate <= to)
                .OrderBy(a => a.AppointmentDate).ThenBy(a => a.AppointmentTime)
                .Select(a => new { a.AppointmentDate, a.AppointmentTime, a.Patient.PatientId, a.Patient.FirstName, a.Patient.LastName, DoctorFirst = a.Doctor.FirstName, DoctorLast = a.Doctor.LastName, a.AppointmentType, a.Priority, a.Status })
                .ToListAsync();
            int Count(params string[] s) => rows.Count(r => s.Contains(r.Status, StringComparer.OrdinalIgnoreCase));
            doc.Metrics.Add(new ReportMetric("Appointments", rows.Count));
            doc.Metrics.Add(new ReportMetric("Completed", Count("Completed"), K.Integer, "success"));
            doc.Metrics.Add(new ReportMetric("Scheduled / confirmed", Count("Scheduled", "Confirmed", "Pending"), K.Integer, "info"));
            doc.Metrics.Add(new ReportMetric("Cancelled", Count("Cancelled"), K.Integer, "danger"));
            doc.Metrics.Add(new ReportMetric("No-show", Count("No-Show", "NoShow", "No Show"), K.Integer, "warning"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Appointments",
                Columns = { new("Date", K.Date), new("Time", width: 0.7f), new("Patient ID"), new("Patient", width: 1.4f), new("Doctor", width: 1.4f), new("Type"), new("Priority", width: 0.8f), new("Status", width: 0.9f) },
                Rows = rows.Select(r => new object?[] { r.AppointmentDate, DateTime.Today.Add(r.AppointmentTime).ToString("hh:mm tt", Inv), r.PatientId, $"{r.FirstName} {r.LastName}", $"Dr. {r.DoctorFirst} {r.DoctorLast}", r.AppointmentType, r.Priority, r.Status }).ToList()
            });
            doc.Charts.Add(Chart("Appointments by status", rows.GroupBy(r => r.Status), g => g.Count(), type: "doughnut"));
            doc.Charts.Add(Chart("Appointments by doctor", rows.GroupBy(r => $"Dr. {r.DoctorFirst} {r.DoctorLast}"), g => g.Count(), top: 10));
        }

        private async Task OpdVisits(ReportDocument doc, DateTime from, DateTime to, bool discharged)
        {
            var rows = await _db.OPDVisits.AsNoTracking()
                .Where(v => v.VisitDate >= from && v.VisitDate <= to)
                .OrderBy(v => v.VisitDate)
                .Select(v => new { v.VisitDate, v.Patient.PatientId, v.Patient.FirstName, v.Patient.LastName, DoctorFirst = v.Doctor.FirstName, DoctorLast = v.Doctor.LastName, v.Diagnosis, v.Treatment, v.ConsultationFee, v.PaymentStatus })
                .ToListAsync();
            doc.Metrics.Add(new ReportMetric(discharged ? "Patients seen" : "OPD visits", rows.Count));
            doc.Metrics.Add(new ReportMetric("Different patients", rows.Select(r => r.PatientId).Distinct().Count(), K.Integer, "info"));
            doc.Metrics.Add(new ReportMetric("Consultation fees", rows.Sum(r => r.ConsultationFee), K.Money, "success"));
            doc.Metrics.Add(new ReportMetric("Fees pending", rows.Where(r => !string.Equals(r.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase)).Sum(r => r.ConsultationFee), K.Money, "warning"));
            var section = new ReportSection { Title = discharged ? "Out-patients seen and discharged" : "OPD visits", ShowTotals = true };
            if (discharged)
            {
                section.Columns.AddRange(new ReportColumn[] { new("Date", K.Date), new("Patient ID"), new("Patient", width: 1.3f), new("Doctor", width: 1.3f), new("Diagnosis", width: 1.6f), new("Treatment", width: 1.8f), new("Fee", K.Money, 0.9f, true) });
                section.Rows = rows.Select(r => new object?[] { r.VisitDate, r.PatientId, $"{r.FirstName} {r.LastName}", $"Dr. {r.DoctorFirst} {r.DoctorLast}", r.Diagnosis, r.Treatment, r.ConsultationFee }).ToList();
            }
            else
            {
                section.Columns.AddRange(new ReportColumn[] { new("Date", K.Date), new("Patient ID"), new("Patient", width: 1.4f), new("Doctor", width: 1.4f), new("Diagnosis", width: 1.8f), new("Fee", K.Money, 0.9f, true), new("Payment", width: 0.8f) });
                section.Rows = rows.Select(r => new object?[] { r.VisitDate, r.PatientId, $"{r.FirstName} {r.LastName}", $"Dr. {r.DoctorFirst} {r.DoctorLast}", r.Diagnosis, r.ConsultationFee, r.PaymentStatus }).ToList();
            }

            doc.Sections.Add(section);
            doc.Charts.Add(Chart("Visits by doctor", rows.GroupBy(r => $"Dr. {r.DoctorFirst} {r.DoctorLast}"), g => g.Count(), top: 10));
            if (!discharged) doc.Charts.Add(Chart("Fees by payment status", rows.GroupBy(r => r.PaymentStatus), g => g.Sum(r => r.ConsultationFee), money: true, type: "doughnut"));
        }

        private async Task IpdAdmissions(ReportDocument doc, DateTime from, DateTime to)
        {
            var rows = await _db.IPDAdmissions.AsNoTracking()
                .Where(a => a.AdmissionDate >= from && a.AdmissionDate <= to)
                .OrderBy(a => a.AdmissionDate)
                .Select(a => new
                {
                    a.AdmissionDate, a.DischargeDate, a.Patient.PatientId, a.Patient.FirstName, a.Patient.LastName, DoctorFirst = a.Doctor.FirstName, DoctorLast = a.Doctor.LastName,
                    Ward = a.Bed != null ? a.Bed.Ward.Name : null, BedNumber = a.Bed != null ? a.Bed.BedNumber : null, a.AdmissionType, a.Diagnosis, a.Status, a.DailyCharges
                })
                .ToListAsync();
            var now = DateTime.Now;
            double Days(DateTime admitted, DateTime? discharged) => Math.Max(1, Math.Ceiling(((discharged ?? now) - admitted).TotalDays));
            doc.Metrics.Add(new ReportMetric("Admissions", rows.Count));
            doc.Metrics.Add(new ReportMetric("Still admitted", rows.Count(r => r.Status == "Admitted"), K.Integer, "warning"));
            doc.Metrics.Add(new ReportMetric("Discharged", rows.Count(r => r.Status == "Discharged"), K.Integer, "success"));
            doc.Metrics.Add(new ReportMetric("Average stay (days)", rows.Count == 0 ? 0 : (decimal)rows.Average(r => Days(r.AdmissionDate, r.DischargeDate)), K.Number, "info"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Admissions",
                Columns = { new("Admitted", K.Date), new("Patient ID"), new("Patient", width: 1.3f), new("Doctor", width: 1.3f), new("Ward / bed", width: 1.4f), new("Type", width: 0.8f), new("Diagnosis", width: 1.4f), new("Discharged", K.Date), new("Days", K.Integer, 0.5f), new("Status", width: 0.8f) },
                Rows = rows.Select(r => new object?[] { r.AdmissionDate, r.PatientId, $"{r.FirstName} {r.LastName}", $"Dr. {r.DoctorFirst} {r.DoctorLast}", r.Ward == null ? "-" : $"{r.Ward} / {r.BedNumber}", r.AdmissionType, r.Diagnosis, r.DischargeDate, Days(r.AdmissionDate, r.DischargeDate), r.Status }).ToList()
            });
            doc.Charts.Add(Chart("Admissions by ward", rows.GroupBy(r => r.Ward ?? "No bed"), g => g.Count()));
            doc.Charts.Add(Chart("Admissions by type", rows.GroupBy(r => r.AdmissionType), g => g.Count(), type: "doughnut"));
        }

        private async Task IpdDischarges(ReportDocument doc, DateTime from, DateTime to)
        {
            var rows = await _db.IPDAdmissions.AsNoTracking()
                .Where(a => a.Status == "Discharged" && a.DischargeDate != null && a.DischargeDate >= from && a.DischargeDate <= to)
                .OrderBy(a => a.DischargeDate)
                .Select(a => new
                {
                    a.AdmissionDate, a.DischargeDate, a.Patient.PatientId, a.Patient.FirstName, a.Patient.LastName, DoctorFirst = a.Doctor.FirstName, DoctorLast = a.Doctor.LastName,
                    Ward = a.Bed != null ? a.Bed.Ward.Name : null, BedNumber = a.Bed != null ? a.Bed.BedNumber : null, a.Diagnosis, a.Treatment
                })
                .ToListAsync();
            double Days(DateTime admitted, DateTime discharged) => Math.Max(1, Math.Ceiling((discharged - admitted).TotalDays));
            doc.Metrics.Add(new ReportMetric("Patients discharged", rows.Count, K.Integer, "success"));
            doc.Metrics.Add(new ReportMetric("Average stay (days)", rows.Count == 0 ? 0 : (decimal)rows.Average(r => Days(r.AdmissionDate, r.DischargeDate!.Value)), K.Number, "info"));
            doc.Metrics.Add(new ReportMetric("Longest stay (days)", rows.Count == 0 ? 0 : (decimal)rows.Max(r => Days(r.AdmissionDate, r.DischargeDate!.Value)), K.Integer));
            doc.Sections.Add(new ReportSection
            {
                Title = "Discharged in-patients",
                Columns = { new("Patient ID"), new("Patient", width: 1.3f), new("Doctor", width: 1.3f), new("Ward / bed", width: 1.3f), new("Admitted", K.Date), new("Discharged", K.Date), new("Days", K.Integer, 0.5f), new("Diagnosis", width: 1.5f), new("Treatment", width: 1.7f) },
                Rows = rows.Select(r => new object?[] { r.PatientId, $"{r.FirstName} {r.LastName}", $"Dr. {r.DoctorFirst} {r.DoctorLast}", r.Ward == null ? "-" : $"{r.Ward} / {r.BedNumber}", r.AdmissionDate, r.DischargeDate, Days(r.AdmissionDate, r.DischargeDate!.Value), r.Diagnosis, r.Treatment }).ToList()
            });
        }

        private async Task DischargeReports(ReportDocument doc, DateTime from, DateTime to)
        {
            var rows = await _db.DischargeSummaries.AsNoTracking()
                .Where(s => s.DischargeDate >= from && s.DischargeDate <= to)
                .OrderBy(s => s.DischargeDate)
                .Select(s => new { s.Id, s.AdmissionDate, s.DischargeDate, s.Patient.PatientId, s.Patient.FirstName, s.Patient.LastName, s.ConditionAtDischarge, s.FinalDiagnosis, s.AttendingDoctor, s.Status })
                .ToListAsync();
            var withoutReport = await _db.IPDAdmissions.AsNoTracking()
                .CountAsync(a => a.Status == "Discharged" && a.DischargeDate >= from && a.DischargeDate <= to && !_db.DischargeSummaries.Any(s => s.IPDAdmissionId == a.Id));
            double Days(DateTime admitted, DateTime discharged) => Math.Max(1, Math.Ceiling((discharged - admitted).TotalDays));
            doc.Metrics.Add(new ReportMetric("Discharge reports", rows.Count));
            doc.Metrics.Add(new ReportMetric("Completed", rows.Count(r => r.Status == DischargeSummary.StatusCompleted), K.Integer, "success"));
            doc.Metrics.Add(new ReportMetric("Drafts", rows.Count(r => r.Status != DischargeSummary.StatusCompleted), K.Integer, "warning"));
            doc.Metrics.Add(new ReportMetric("Discharges without a report", withoutReport, K.Integer, withoutReport > 0 ? "danger" : "secondary"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Discharge reports",
                Columns = { new("Report no.", width: 1.1f), new("Patient ID"), new("Patient", width: 1.3f), new("Admitted", K.Date), new("Discharged", K.Date), new("Days", K.Integer, 0.5f), new("Condition"), new("Final diagnosis", width: 1.8f), new("Attending doctor", width: 1.3f), new("Status", width: 0.8f) },
                Rows = rows.Select(r => new object?[] { $"DS-{r.DischargeDate:yyyy}-{r.Id:D6}", r.PatientId, $"{r.FirstName} {r.LastName}", r.AdmissionDate, r.DischargeDate, Days(r.AdmissionDate, r.DischargeDate), r.ConditionAtDischarge, r.FinalDiagnosis, r.AttendingDoctor, r.Status }).ToList()
            });
            doc.Notes.Add("A patient's own discharge report is opened, completed and downloaded under IPD → Discharge Reports (or from the admission).");
        }

        private async Task OperationTheatre(ReportDocument doc, DateTime from, DateTime to)
        {
            var rows = await _db.OTSchedules.AsNoTracking()
                .Where(o => o.ScheduledDate >= from && o.ScheduledDate <= to)
                .OrderBy(o => o.ScheduledDate)
                .Select(o => new
                {
                    o.ScheduledDate, o.Patient.PatientId, o.Patient.FirstName, o.Patient.LastName, o.ProcedureName,
                    Surgeon = o.Surgeon != null ? "Dr. " + o.Surgeon.FirstName + " " + o.Surgeon.LastName : o.SurgeonName,
                    Theatre = o.Theatre != null ? o.Theatre.Name : o.OperationTheatreNumber, o.EstimatedDurationMinutes, o.IsEmergency, o.Status
                })
                .ToListAsync();
            doc.Metrics.Add(new ReportMetric("Operations", rows.Count));
            doc.Metrics.Add(new ReportMetric("Completed", rows.Count(r => r.Status == "Completed"), K.Integer, "success"));
            doc.Metrics.Add(new ReportMetric("Emergency", rows.Count(r => r.IsEmergency), K.Integer, "danger"));
            doc.Metrics.Add(new ReportMetric("Cancelled", rows.Count(r => r.Status == "Cancelled"), K.Integer, "warning"));
            doc.Metrics.Add(new ReportMetric("Theatre time (hours)", rows.Sum(r => r.EstimatedDurationMinutes) / 60m, K.Number, "info"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Operations",
                Columns = { new("Date & time", K.DateTime, 1.2f), new("Patient ID"), new("Patient", width: 1.3f), new("Procedure", width: 1.6f), new("Surgeon", width: 1.3f), new("Theatre"), new("Minutes", K.Integer, 0.6f, true), new("Emergency", width: 0.7f), new("Status", width: 0.8f) },
                Rows = rows.Select(r => new object?[] { r.ScheduledDate, r.PatientId, $"{r.FirstName} {r.LastName}", r.ProcedureName, r.Surgeon, r.Theatre, r.EstimatedDurationMinutes, r.IsEmergency ? "Yes" : "No", r.Status }).ToList(),
                ShowTotals = true
            });
            doc.Charts.Add(Chart("Operations by theatre", rows.GroupBy(r => string.IsNullOrWhiteSpace(r.Theatre) ? "Not set" : r.Theatre), g => g.Count()));
            doc.Charts.Add(Chart("Operations by status", rows.GroupBy(r => r.Status), g => g.Count(), type: "doughnut"));
        }

        private async Task LiveSessions(ReportDocument doc, DateTime from, DateTime to, bool patientsOnly)
        {
            var query = _db.LiveConsultationSessions.AsNoTracking().Where(s => s.ScheduledAt >= from && s.ScheduledAt <= to);
            if (patientsOnly) query = query.Where(s => s.PatientId != null || s.PatientName != "");
            var rows = await query.OrderBy(s => s.ScheduledAt)
                .Select(s => new { s.ScheduledAt, s.PatientName, s.DoctorName, s.DurationMinutes, s.Platform, s.MeetingId, s.Status })
                .ToListAsync();
            doc.Metrics.Add(new ReportMetric(patientsOnly ? "Consultations" : "Sessions", rows.Count));
            doc.Metrics.Add(new ReportMetric("Completed", rows.Count(r => r.Status == "Completed"), K.Integer, "success"));
            doc.Metrics.Add(new ReportMetric("Cancelled", rows.Count(r => r.Status == "Cancelled"), K.Integer, "danger"));
            doc.Metrics.Add(new ReportMetric("Total minutes", rows.Sum(r => r.DurationMinutes), K.Integer, "info"));
            doc.Sections.Add(new ReportSection
            {
                Title = patientsOnly ? "Online consultations" : "Online sessions",
                Columns = { new("Scheduled", K.DateTime, 1.2f), new("Patient", width: 1.3f), new("Doctor", width: 1.3f), new("Platform"), new("Meeting ID"), new("Minutes", K.Integer, 0.6f, true), new("Status", width: 0.8f) },
                Rows = rows.Select(r => new object?[] { r.ScheduledAt, string.IsNullOrWhiteSpace(r.PatientName) ? "-" : r.PatientName, r.DoctorName, r.Platform, r.MeetingId, r.DurationMinutes, r.Status }).ToList(),
                ShowTotals = true
            });
            doc.Charts.Add(Chart("Sessions by platform", rows.GroupBy(r => string.IsNullOrWhiteSpace(r.Platform) ? "Other" : r.Platform), g => g.Count(), type: "doughnut"));
        }

        private async Task PatientVisits(ReportDocument doc, DateTime from, DateTime to)
        {
            var appointments = await _db.Appointments.AsNoTracking().Where(a => a.AppointmentDate >= from && a.AppointmentDate <= to).Select(a => new { a.PatientId, Date = a.AppointmentDate }).ToListAsync();
            var visits = await _db.OPDVisits.AsNoTracking().Where(v => v.VisitDate >= from && v.VisitDate <= to).Select(v => new { v.PatientId, Date = v.VisitDate }).ToListAsync();
            var admissions = await _db.IPDAdmissions.AsNoTracking().Where(a => a.AdmissionDate >= from && a.AdmissionDate <= to).Select(a => new { a.PatientId, Date = a.AdmissionDate }).ToListAsync();
            var ids = appointments.Select(a => a.PatientId).Concat(visits.Select(v => v.PatientId)).Concat(admissions.Select(a => a.PatientId)).Distinct().ToList();
            var patients = await _db.Patients.AsNoTracking().Where(p => ids.Contains(p.Id)).Select(p => new { p.Id, p.PatientId, p.FirstName, p.LastName, p.Phone, p.Gender }).ToDictionaryAsync(p => p.Id);

            var rows = ids.Where(patients.ContainsKey).Select(id =>
            {
                var p = patients[id];
                var a = appointments.Count(x => x.PatientId == id);
                var v = visits.Count(x => x.PatientId == id);
                var i = admissions.Count(x => x.PatientId == id);
                var last = appointments.Where(x => x.PatientId == id).Select(x => x.Date).Concat(visits.Where(x => x.PatientId == id).Select(x => x.Date)).Concat(admissions.Where(x => x.PatientId == id).Select(x => x.Date)).Max();
                return new object?[] { p.PatientId, $"{p.FirstName} {p.LastName}", p.Gender, p.Phone, a, v, i, a + v + i, last };
            }).OrderByDescending(r => (int)r[7]!).ThenBy(r => r[1]).ToList();

            doc.Metrics.Add(new ReportMetric("Patients", rows.Count));
            doc.Metrics.Add(new ReportMetric("Appointments", appointments.Count, K.Integer, "info"));
            doc.Metrics.Add(new ReportMetric("OPD visits", visits.Count, K.Integer, "success"));
            doc.Metrics.Add(new ReportMetric("Admissions", admissions.Count, K.Integer, "warning"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Visits per patient",
                Columns = { new("Patient ID"), new("Patient", width: 1.5f), new("Gender", width: 0.7f), new("Phone"), new("Appointments", K.Integer, 0.8f, true), new("OPD visits", K.Integer, 0.8f, true), new("Admissions", K.Integer, 0.8f, true), new("Total", K.Integer, 0.6f, true), new("Last visit", K.Date) },
                Rows = rows,
                ShowTotals = true
            });
        }

        private async Task Referrals(ReportDocument doc, DateTime from, DateTime to)
        {
            var rows = await _db.Referrals.AsNoTracking()
                .Where(r => r.ReferralDate >= from && r.ReferralDate <= to)
                .OrderBy(r => r.ReferralDate)
                .Select(r => new { r.ReferralDate, r.Patient.PatientId, r.Patient.FirstName, r.Patient.LastName, r.ReferralType, r.ReferredTo, r.ReferralReason, r.TpaProvider, r.ApprovedAmount, r.Status })
                .ToListAsync();
            doc.Metrics.Add(new ReportMetric("Referrals", rows.Count));
            doc.Metrics.Add(new ReportMetric("Approved amount", rows.Sum(r => r.ApprovedAmount ?? 0), K.Money, "success"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Referrals",
                Columns = { new("Date", K.Date), new("Patient ID"), new("Patient", width: 1.3f), new("Type", width: 0.9f), new("Referred to", width: 1.4f), new("Reason", width: 1.6f), new("TPA"), new("Approved", K.Money, 1, true), new("Status", width: 0.8f) },
                Rows = rows.Select(r => new object?[] { r.ReferralDate, r.PatientId, $"{r.FirstName} {r.LastName}", r.ReferralType, r.ReferredTo, r.ReferralReason, string.IsNullOrWhiteSpace(r.TpaProvider) ? "-" : r.TpaProvider, r.ApprovedAmount ?? 0, r.Status }).ToList(),
                ShowTotals = true
            });
            doc.Charts.Add(Chart("Referrals by type", rows.GroupBy(r => r.ReferralType), g => g.Count(), type: "doughnut"));
        }

        // ── Diagnostics & pharmacy ───────────────────────────────

        private async Task Pathology(ReportDocument doc, DateTime from, DateTime to)
        {
            var rows = await _db.LabResults.AsNoTracking()
                .Where(r => r.OrderDate >= from && r.OrderDate <= to)
                .OrderBy(r => r.OrderDate)
                .Select(r => new { r.OrderNumber, r.OrderDate, r.Patient.PatientId, r.Patient.FirstName, r.Patient.LastName, r.LabTest.TestName, r.LabTest.Category, r.ResultValue, r.Unit, r.Status, r.PerformedBy, r.LabTest.Price })
                .ToListAsync();
            doc.Metrics.Add(new ReportMetric("Tests ordered", rows.Count));
            doc.Metrics.Add(new ReportMetric("Completed", rows.Count(r => r.Status == "Completed"), K.Integer, "success"));
            doc.Metrics.Add(new ReportMetric("Pending", rows.Count(r => r.Status != "Completed" && r.Status != "Cancelled"), K.Integer, "warning"));
            doc.Metrics.Add(new ReportMetric("Test value", rows.Sum(r => r.Price), K.Money, "info"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Laboratory tests",
                Columns = { new("Order No.", width: 1.1f), new("Ordered", K.Date), new("Patient ID"), new("Patient", width: 1.3f), new("Test", width: 1.5f), new("Result", width: 1.2f), new("Status", width: 0.8f), new("Performed by"), new("Price", K.Money, 0.9f, true) },
                Rows = rows.Select(r => new object?[] { r.OrderNumber, r.OrderDate, r.PatientId, $"{r.FirstName} {r.LastName}", r.TestName, string.IsNullOrWhiteSpace(r.ResultValue) ? "-" : $"{r.ResultValue} {r.Unit}".Trim(), r.Status, r.PerformedBy, r.Price }).ToList(),
                ShowTotals = true
            });
            doc.Charts.Add(Chart("Most ordered tests", rows.GroupBy(r => r.TestName), g => g.Count(), top: 10));
        }

        private async Task Radiology(ReportDocument doc, DateTime from, DateTime to)
        {
            var rows = await _db.RadiologyResults.AsNoTracking()
                .Where(r => r.OrderDate >= from && r.OrderDate <= to)
                .OrderBy(r => r.OrderDate)
                .Select(r => new { r.OrderNumber, r.OrderDate, r.Patient.PatientId, r.Patient.FirstName, r.Patient.LastName, r.RadiologyTest.TestName, r.Impression, r.Status, r.PerformedBy, r.RadiologyTest.Price })
                .ToListAsync();
            doc.Metrics.Add(new ReportMetric("Examinations", rows.Count));
            doc.Metrics.Add(new ReportMetric("Completed", rows.Count(r => r.Status == "Completed"), K.Integer, "success"));
            doc.Metrics.Add(new ReportMetric("Pending", rows.Count(r => r.Status != "Completed" && r.Status != "Cancelled"), K.Integer, "warning"));
            doc.Metrics.Add(new ReportMetric("Examination value", rows.Sum(r => r.Price), K.Money, "info"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Radiology examinations",
                Columns = { new("Order No.", width: 1.1f), new("Ordered", K.Date), new("Patient ID"), new("Patient", width: 1.3f), new("Examination", width: 1.5f), new("Impression", width: 1.8f), new("Status", width: 0.8f), new("Performed by"), new("Price", K.Money, 0.9f, true) },
                Rows = rows.Select(r => new object?[] { r.OrderNumber, r.OrderDate, r.PatientId, $"{r.FirstName} {r.LastName}", r.TestName, string.IsNullOrWhiteSpace(r.Impression) ? "-" : r.Impression, r.Status, r.PerformedBy, r.Price }).ToList(),
                ShowTotals = true
            });
            doc.Charts.Add(Chart("Examinations by type", rows.GroupBy(r => r.TestName), g => g.Count(), top: 10));
        }

        private async Task ExpiringMedicines(ReportDocument doc)
        {
            var by = doc.EndDate!.Value;
            var today = DateTime.Now.Date;
            var rows = await _db.Medicines.AsNoTracking()
                .Where(m => m.IsActive && m.ExpiryDate <= by)
                .OrderBy(m => m.ExpiryDate)
                .Select(m => new { m.Name, m.GenericName, m.Strength, m.BatchNumber, m.ExpiryDate, m.StockQuantity, m.UnitPrice })
                .ToListAsync();
            var expired = rows.Where(r => r.ExpiryDate.Date < today).ToList();
            doc.Metrics.Add(new ReportMetric("Medicines", rows.Count));
            doc.Metrics.Add(new ReportMetric("Already expired", expired.Count, K.Integer, "danger"));
            doc.Metrics.Add(new ReportMetric("Expiring", rows.Count - expired.Count, K.Integer, "warning"));
            doc.Metrics.Add(new ReportMetric("Stock value at risk", rows.Sum(r => r.StockQuantity * r.UnitPrice), K.Money, "danger"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Expired and expiring medicines",
                Columns = { new("Medicine", width: 1.5f), new("Generic name", width: 1.3f), new("Strength", width: 0.8f), new("Batch"), new("Expiry", K.Date), new("Days left", K.Integer, 0.7f), new("Stock", K.Integer, 0.7f, true), new("Unit price", K.Money, 0.9f), new("Stock value", K.Money, 1, true), new("Status", width: 0.8f) },
                Rows = rows.Select(r =>
                {
                    var days = (int)(r.ExpiryDate.Date - today).TotalDays;
                    return new object?[] { r.Name, r.GenericName, r.Strength, r.BatchNumber, r.ExpiryDate, days, r.StockQuantity, r.UnitPrice, r.StockQuantity * r.UnitPrice, days < 0 ? "Expired" : days <= 30 ? "Expires ≤ 30 days" : "Expiring" };
                }).ToList(),
                ShowTotals = true,
                EmptyText = "No medicines expire by this date."
            });
        }

        // ── Blood bank ───────────────────────────────────────────

        private async Task BloodIssues(ReportDocument doc, DateTime from, DateTime to)
        {
            var rows = await _db.BloodIssues.AsNoTracking()
                .Where(b => b.IssueDate >= from && b.IssueDate <= to)
                .OrderBy(b => b.IssueDate)
                .Select(b => new { b.IssueDate, b.Patient.PatientId, b.Patient.FirstName, b.Patient.LastName, b.BloodGroup, b.UnitsIssued, b.CrossMatchStatus, b.RequestedBy })
                .ToListAsync();
            doc.Metrics.Add(new ReportMetric("Issues", rows.Count));
            doc.Metrics.Add(new ReportMetric("Units issued", rows.Sum(r => r.UnitsIssued), K.Integer, "danger"));
            doc.Metrics.Add(new ReportMetric("Patients", rows.Select(r => r.PatientId).Distinct().Count(), K.Integer, "info"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Blood issued",
                Columns = { new("Date", K.DateTime, 1.2f), new("Patient ID"), new("Patient", width: 1.4f), new("Blood group", width: 0.8f), new("Units", K.Integer, 0.6f, true), new("Cross-match", width: 0.9f), new("Requested by", width: 1.2f) },
                Rows = rows.Select(r => new object?[] { r.IssueDate, r.PatientId, $"{r.FirstName} {r.LastName}", r.BloodGroup, r.UnitsIssued, r.CrossMatchStatus, r.RequestedBy }).ToList(),
                ShowTotals = true
            });
            doc.Charts.Add(Chart("Units issued by blood group", rows.GroupBy(r => r.BloodGroup), g => g.Sum(r => r.UnitsIssued)));
        }

        private async Task BloodComponents(ReportDocument doc, DateTime from, DateTime to)
        {
            var issues = await _db.BloodIssues.AsNoTracking().Where(b => b.IssueDate >= from && b.IssueDate <= to)
                .Select(b => new { b.BloodGroup, b.UnitsIssued, b.PatientId }).ToListAsync();
            var stock = await _db.BloodInventories.AsNoTracking().Select(b => new { b.BloodGroup, b.UnitsAvailable, b.UnitsReserved }).ToListAsync();
            var groups = issues.Select(i => i.BloodGroup).Concat(stock.Select(s => s.BloodGroup)).Distinct().OrderBy(g => g).ToList();
            doc.Metrics.Add(new ReportMetric("Units issued", issues.Sum(i => i.UnitsIssued), K.Integer, "danger"));
            doc.Metrics.Add(new ReportMetric("Units in stock", stock.Sum(s => s.UnitsAvailable), K.Integer, "success"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Issued and in stock by blood group",
                Columns = { new("Blood group"), new("Issues", K.Integer, 0.8f, true), new("Units issued", K.Integer, 0.9f, true), new("Patients", K.Integer, 0.8f, true), new("Units in stock", K.Integer, 0.9f, true), new("Reserved", K.Integer, 0.8f, true) },
                Rows = groups.Select(g => new object?[]
                {
                    g, issues.Count(i => i.BloodGroup == g), issues.Where(i => i.BloodGroup == g).Sum(i => i.UnitsIssued), issues.Where(i => i.BloodGroup == g).Select(i => i.PatientId).Distinct().Count(),
                    stock.Where(s => s.BloodGroup == g).Sum(s => s.UnitsAvailable), stock.Where(s => s.BloodGroup == g).Sum(s => s.UnitsReserved)
                }).ToList(),
                ShowTotals = true
            });
            doc.Notes.Add("Blood is issued as whole units per blood group; components are recorded with the issue notes.");
        }

        private async Task BloodStock(ReportDocument doc)
        {
            var rows = await _db.BloodInventories.AsNoTracking().OrderBy(b => b.BloodGroup)
                .Select(b => new { b.BloodGroup, b.UnitsAvailable, b.UnitsReserved, b.MinimumLevel, b.LastUpdatedDate }).ToListAsync();
            doc.Metrics.Add(new ReportMetric("Units available", rows.Sum(r => r.UnitsAvailable), K.Integer, "success"));
            doc.Metrics.Add(new ReportMetric("Units reserved", rows.Sum(r => r.UnitsReserved), K.Integer, "warning"));
            doc.Metrics.Add(new ReportMetric("Groups below minimum", rows.Count(r => r.UnitsAvailable - r.UnitsReserved < r.MinimumLevel), K.Integer, "danger"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Blood stock by group",
                Columns = { new("Blood group"), new("Available", K.Integer, 0.8f, true), new("Reserved", K.Integer, 0.8f, true), new("Free", K.Integer, 0.8f, true), new("Minimum level", K.Integer, 0.9f), new("Status"), new("Last updated", K.DateTime, 1.2f) },
                Rows = rows.Select(r => new object?[] { r.BloodGroup, r.UnitsAvailable, r.UnitsReserved, r.UnitsAvailable - r.UnitsReserved, r.MinimumLevel, r.UnitsAvailable - r.UnitsReserved < r.MinimumLevel ? "Below minimum – arrange donors" : "OK", r.LastUpdatedDate }).ToList(),
                ShowTotals = true
            });
            doc.Charts.Add(Chart("Free units by blood group", rows.GroupBy(r => r.BloodGroup), g => g.Sum(r => r.UnitsAvailable - r.UnitsReserved)));
            doc.Notes.Add("Stock is built from donations; individual donors are not registered in the system.");
        }

        // ── Services & registers ─────────────────────────────────

        private async Task Ambulance(ReportDocument doc, DateTime from, DateTime to)
        {
            var rows = await _db.AmbulanceDispatches.AsNoTracking()
                .Where(d => d.DispatchTime >= from && d.DispatchTime <= to)
                .OrderBy(d => d.DispatchTime)
                .Select(d => new { d.DispatchTime, d.AmbulanceVehicle.VehicleNumber, d.AmbulanceVehicle.DriverName, d.PatientName, d.PickupAddress, d.Purpose, d.DistanceKm, d.Charges, d.Status })
                .ToListAsync();
            doc.Metrics.Add(new ReportMetric("Trips", rows.Count));
            doc.Metrics.Add(new ReportMetric("Distance (km)", rows.Sum(r => r.DistanceKm ?? 0), K.Number, "info"));
            doc.Metrics.Add(new ReportMetric("Charges", rows.Sum(r => r.Charges ?? 0), K.Money, "success"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Ambulance trips",
                Columns = { new("Dispatched", K.DateTime, 1.2f), new("Vehicle", width: 0.9f), new("Driver"), new("Patient", width: 1.2f), new("Pick-up", width: 1.5f), new("Purpose"), new("Km", K.Number, 0.6f, true), new("Charges", K.Money, 0.9f, true), new("Status", width: 0.8f) },
                Rows = rows.Select(r => new object?[] { r.DispatchTime, r.VehicleNumber, r.DriverName, r.PatientName, r.PickupAddress, r.Purpose, r.DistanceKm ?? 0, r.Charges ?? 0, r.Status }).ToList(),
                ShowTotals = true
            });
            doc.Charts.Add(Chart("Trips by vehicle", rows.GroupBy(r => r.VehicleNumber), g => g.Count()));
        }

        private async Task Births(ReportDocument doc, DateTime from, DateTime to)
        {
            var rows = await _db.BirthRecords.AsNoTracking()
                .Where(b => b.DateOfBirth >= from && b.DateOfBirth <= to)
                .OrderBy(b => b.DateOfBirth)
                .Select(b => new { b.DateOfBirth, b.TimeOfBirth, b.BabyName, b.Gender, b.WeightKg, b.MotherName, b.FatherName, b.DeliveryType, b.AttendingDoctorName, b.CertificateNumber, b.CertificateIssued })
                .ToListAsync();
            doc.Metrics.Add(new ReportMetric("Births", rows.Count));
            doc.Metrics.Add(new ReportMetric("Boys", rows.Count(r => r.Gender == "Male"), K.Integer, "info"));
            doc.Metrics.Add(new ReportMetric("Girls", rows.Count(r => r.Gender == "Female"), K.Integer, "danger"));
            doc.Metrics.Add(new ReportMetric("Average weight (kg)", rows.Count == 0 ? 0 : rows.Average(r => r.WeightKg), K.Number, "success"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Births",
                Columns = { new("Date", K.Date), new("Time", width: 0.6f), new("Baby", width: 1.2f), new("Gender", width: 0.7f), new("Weight kg", K.Number, 0.7f), new("Mother", width: 1.2f), new("Father", width: 1.2f), new("Delivery", width: 0.9f), new("Doctor", width: 1.2f), new("Certificate") },
                Rows = rows.Select(r => new object?[] { r.DateOfBirth, r.TimeOfBirth, r.BabyName, r.Gender, r.WeightKg, r.MotherName, r.FatherName, r.DeliveryType, r.AttendingDoctorName, r.CertificateIssued ? r.CertificateNumber : "Not issued" }).ToList()
            });
            doc.Charts.Add(Chart("Births by delivery type", rows.GroupBy(r => r.DeliveryType), g => g.Count(), type: "doughnut"));
        }

        private async Task Deaths(ReportDocument doc, DateTime from, DateTime to)
        {
            var rows = await _db.DeathRecords.AsNoTracking()
                .Where(d => d.DateOfDeath >= from && d.DateOfDeath <= to)
                .OrderBy(d => d.DateOfDeath)
                .Select(d => new { d.DateOfDeath, d.TimeOfDeath, d.PatientName, d.Gender, d.CauseOfDeath, d.AttendingDoctorName, d.NextOfKinName, d.NextOfKinContact, d.CertificateNumber, d.CertificateIssued })
                .ToListAsync();
            doc.Metrics.Add(new ReportMetric("Deaths", rows.Count, K.Integer, "secondary"));
            doc.Metrics.Add(new ReportMetric("Certificates issued", rows.Count(r => r.CertificateIssued), K.Integer, "info"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Deaths",
                Columns = { new("Date", K.Date), new("Time", width: 0.6f), new("Name", width: 1.3f), new("Gender", width: 0.7f), new("Cause of death", width: 1.8f), new("Doctor", width: 1.2f), new("Next of kin", width: 1.2f), new("Contact"), new("Certificate") },
                Rows = rows.Select(r => new object?[] { r.DateOfDeath, r.TimeOfDeath, r.PatientName, r.Gender, r.CauseOfDeath, r.AttendingDoctorName, r.NextOfKinName, r.NextOfKinContact, r.CertificateIssued ? r.CertificateNumber : "Not issued" }).ToList()
            });
        }

        // ── HR ───────────────────────────────────────────────────

        private async Task PayrollMonth(ReportDocument doc, DateTime month)
        {
            var next = month.AddMonths(1);
            var rows = await _db.PayrollRecords.AsNoTracking()
                .Where(r => r.PayrollMonth >= month && r.PayrollMonth < next)
                .OrderBy(r => r.Staff.Department).ThenBy(r => r.Staff.FirstName)
                .Select(r => new { r.Staff.EmployeeId, r.Staff.FirstName, r.Staff.LastName, r.Staff.Department, r.Staff.Designation, r.BasicSalary, r.Allowances, r.Deductions, r.NetSalary, r.Status, r.PaymentDate })
                .ToListAsync();
            doc.Metrics.Add(new ReportMetric("Employees", rows.Count));
            doc.Metrics.Add(new ReportMetric("Gross pay", rows.Sum(r => r.BasicSalary + r.Allowances), K.Money, "info"));
            doc.Metrics.Add(new ReportMetric("Deductions", rows.Sum(r => r.Deductions), K.Money, "warning"));
            doc.Metrics.Add(new ReportMetric("Net pay", rows.Sum(r => r.NetSalary), K.Money, "success"));
            doc.Metrics.Add(new ReportMetric("Paid", rows.Count(r => r.Status == "Paid"), K.Integer, "primary"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Salaries",
                Columns = { new("Employee ID"), new("Name", width: 1.4f), new("Department", width: 1.2f), new("Designation", width: 1.1f), new("Basic", K.Money, 1, true), new("Allowances", K.Money, 1, true), new("Deductions", K.Money, 1, true), new("Net pay", K.Money, 1, true), new("Status", width: 0.8f), new("Paid on", K.Date) },
                Rows = rows.Select(r => new object?[] { r.EmployeeId, $"{r.FirstName} {r.LastName}", r.Department, r.Designation, r.BasicSalary, r.Allowances, r.Deductions, r.NetSalary, r.Status, r.PaymentDate }).ToList(),
                ShowTotals = true,
                EmptyText = "No payroll generated for this month."
            });
            doc.Charts.Add(Chart("Net pay by department", rows.GroupBy(r => r.Department), g => g.Sum(r => r.NetSalary), money: true));
        }

        private async Task PayrollPeriod(ReportDocument doc, DateTime from, DateTime to)
        {
            var start = new DateTime(from.Year, from.Month, 1);
            var rows = await _db.PayrollRecords.AsNoTracking()
                .Where(r => r.PayrollMonth >= start && r.PayrollMonth <= to)
                .Select(r => new { r.PayrollMonth, r.StaffId, r.BasicSalary, r.Allowances, r.Deductions, r.NetSalary, r.Status })
                .ToListAsync();
            var months = rows.GroupBy(r => new DateTime(r.PayrollMonth.Year, r.PayrollMonth.Month, 1)).OrderBy(g => g.Key).ToList();
            doc.Metrics.Add(new ReportMetric("Months", months.Count));
            doc.Metrics.Add(new ReportMetric("Net pay", rows.Sum(r => r.NetSalary), K.Money, "success"));
            doc.Metrics.Add(new ReportMetric("Deductions", rows.Sum(r => r.Deductions), K.Money, "warning"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Payroll per month",
                Columns = { new("Month", width: 1.1f), new("Employees", K.Integer, 0.8f), new("Basic", K.Money, 1, true), new("Allowances", K.Money, 1, true), new("Deductions", K.Money, 1, true), new("Net pay", K.Money, 1, true), new("Paid", K.Integer, 0.6f, true) },
                Rows = months.Select(g => new object?[] { g.Key.ToString("MMMM yyyy", Inv), g.Select(r => r.StaffId).Distinct().Count(), g.Sum(r => r.BasicSalary), g.Sum(r => r.Allowances), g.Sum(r => r.Deductions), g.Sum(r => r.NetSalary), g.Count(r => r.Status == "Paid") }).ToList(),
                ShowTotals = true
            });
            doc.Charts.Add(new ReportChartData { Title = "Net pay per month", IsMoney = true, Labels = months.Select(g => g.Key.ToString("MMM yyyy", Inv)).ToList(), Values = months.Select(g => g.Sum(r => r.NetSalary)).ToList() });
        }

        private async Task Attendance(ReportDocument doc, DateTime from, DateTime to)
        {
            var rows = await _db.StaffAttendances.AsNoTracking()
                .Where(a => a.AttendanceDate >= from && a.AttendanceDate <= to)
                .Select(a => new { a.StaffId, a.Staff.EmployeeId, a.Staff.FirstName, a.Staff.LastName, a.Staff.Department, a.Status })
                .ToListAsync();
            var people = rows.GroupBy(r => r.StaffId).Select(g =>
            {
                var f = g.First();
                var present = g.Count(r => r.Status == "Present");
                var half = g.Count(r => r.Status == "HalfDay" || r.Status == "Half Day");
                var absent = g.Count(r => r.Status == "Absent");
                var other = g.Count() - present - half - absent;
                var rate = g.Count() == 0 ? 0 : (present + half * 0.5m) * 100 / g.Count();
                return new object?[] { f.EmployeeId, $"{f.FirstName} {f.LastName}", f.Department, present, half, absent, other, g.Count(), rate };
            }).OrderBy(r => r[2]).ThenBy(r => r[1]).ToList();
            doc.Metrics.Add(new ReportMetric("Employees", people.Count));
            doc.Metrics.Add(new ReportMetric("Present days", rows.Count(r => r.Status == "Present"), K.Integer, "success"));
            doc.Metrics.Add(new ReportMetric("Absent days", rows.Count(r => r.Status == "Absent"), K.Integer, "danger"));
            doc.Metrics.Add(new ReportMetric("Attendance rate", rows.Count == 0 ? 0 : (rows.Count(r => r.Status == "Present") + rows.Count(r => r.Status == "HalfDay") * 0.5m) * 100 / rows.Count, K.Percent, "info"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Attendance per employee",
                Columns = { new("Employee ID"), new("Name", width: 1.4f), new("Department", width: 1.2f), new("Present", K.Integer, 0.7f, true), new("Half day", K.Integer, 0.7f, true), new("Absent", K.Integer, 0.7f, true), new("Leave / other", K.Integer, 0.8f, true), new("Days recorded", K.Integer, 0.8f, true), new("Attendance", K.Percent, 0.8f) },
                Rows = people,
                ShowTotals = true
            });
        }

        private async Task StaffAttendanceRecords(ReportDocument doc, DateTime from, DateTime to)
        {
            doc.StaffOptions = (await _db.Staff.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.FirstName).ThenBy(s => s.LastName)
                    .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName, s.EmployeeId }).ToListAsync())
                .Select(s => new KeyValuePair<string, string>(s.Id, $"{s.Name} ({s.EmployeeId})")).ToList();
            var query = _db.StaffAttendances.AsNoTracking().Where(a => a.AttendanceDate >= from && a.AttendanceDate <= to);
            if (!string.IsNullOrEmpty(doc.StaffId)) query = query.Where(a => a.StaffId == doc.StaffId);
            var rows = await query.OrderBy(a => a.AttendanceDate).ThenBy(a => a.Staff.FirstName)
                .Select(a => new { a.AttendanceDate, a.Staff.EmployeeId, a.Staff.FirstName, a.Staff.LastName, a.Staff.Department, a.Status, a.CheckInTime, a.CheckOutTime })
                .ToListAsync();
            decimal Hours(DateTime? i, DateTime? o) => i.HasValue && o.HasValue && o > i ? Math.Round((decimal)(o.Value - i.Value).TotalHours, 2) : 0;
            var selected = doc.StaffOptions.FirstOrDefault(o => o.Key == doc.StaffId).Value;
            if (!string.IsNullOrEmpty(selected)) doc.PeriodText += " · " + selected;
            doc.Metrics.Add(new ReportMetric("Records", rows.Count));
            doc.Metrics.Add(new ReportMetric("Present", rows.Count(r => r.Status == "Present"), K.Integer, "success"));
            doc.Metrics.Add(new ReportMetric("Absent", rows.Count(r => r.Status == "Absent"), K.Integer, "danger"));
            doc.Metrics.Add(new ReportMetric("Hours worked", rows.Sum(r => Hours(r.CheckInTime, r.CheckOutTime)), K.Number, "info"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Attendance records",
                Columns = { new("Date", K.Date), new("Employee ID"), new("Name", width: 1.4f), new("Department", width: 1.2f), new("Status", width: 0.8f), new("Check-in", width: 0.8f), new("Check-out", width: 0.8f), new("Hours", K.Number, 0.6f, true) },
                Rows = rows.Select(r => new object?[] { r.AttendanceDate, r.EmployeeId, $"{r.FirstName} {r.LastName}", r.Department, r.Status, r.CheckInTime?.ToString("hh:mm tt", Inv) ?? "-", r.CheckOutTime?.ToString("hh:mm tt", Inv) ?? "-", Hours(r.CheckInTime, r.CheckOutTime) }).ToList(),
                ShowTotals = true
            });
        }

        private async Task Departments(ReportDocument doc, DateTime from, DateTime to)
        {
            var departments = await _db.Departments.AsNoTracking().Where(d => d.IsActive).OrderBy(d => d.Name).Select(d => new { d.Id, d.Name, d.HeadOfDepartment }).ToListAsync();
            var doctors = await _db.Doctors.AsNoTracking().Where(d => d.IsActive).Select(d => new { d.Id, d.DepartmentId }).ToListAsync();
            var appointments = await _db.Appointments.AsNoTracking().Where(a => a.AppointmentDate >= from && a.AppointmentDate <= to).Select(a => a.Doctor.DepartmentId).ToListAsync();
            var visits = await _db.OPDVisits.AsNoTracking().Where(v => v.VisitDate >= from && v.VisitDate <= to).Select(v => new { v.Doctor.DepartmentId, v.ConsultationFee }).ToListAsync();
            var admissions = await _db.IPDAdmissions.AsNoTracking().Where(a => a.AdmissionDate >= from && a.AdmissionDate <= to).Select(a => a.Doctor.DepartmentId).ToListAsync();

            var rows = departments.Select(d => new object?[]
            {
                d.Name, d.HeadOfDepartment, doctors.Count(x => x.DepartmentId == d.Id), appointments.Count(x => x == d.Id), visits.Count(x => x.DepartmentId == d.Id),
                admissions.Count(x => x == d.Id), visits.Where(x => x.DepartmentId == d.Id).Sum(x => x.ConsultationFee)
            }).ToList();
            doc.Metrics.Add(new ReportMetric("Departments", departments.Count));
            doc.Metrics.Add(new ReportMetric("Appointments", appointments.Count, K.Integer, "info"));
            doc.Metrics.Add(new ReportMetric("OPD visits", visits.Count, K.Integer, "success"));
            doc.Metrics.Add(new ReportMetric("Admissions", admissions.Count, K.Integer, "warning"));
            doc.Metrics.Add(new ReportMetric("Consultation fees", visits.Sum(v => v.ConsultationFee), K.Money, "primary"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Workload per department",
                Columns = { new("Department", width: 1.5f), new("Head", width: 1.3f), new("Doctors", K.Integer, 0.7f, true), new("Appointments", K.Integer, 0.9f, true), new("OPD visits", K.Integer, 0.8f, true), new("Admissions", K.Integer, 0.8f, true), new("Consultation fees", K.Money, 1.1f, true) },
                Rows = rows,
                ShowTotals = true
            });
            doc.Charts.Add(new ReportChartData { Title = "OPD visits per department", Labels = rows.Where(r => (int)r[4]! > 0).Select(r => (string)r[0]!).ToList(), Values = rows.Where(r => (int)r[4]! > 0).Select(r => (decimal)(int)r[4]!).ToList() });
        }

        private async Task Occupancy(ReportDocument doc, DateTime date)
        {
            var dayEnd = DateRange.EndOfDay(date);
            var beds = await _db.Beds.AsNoTracking().Where(b => b.IsActive).Select(b => new { b.Id, b.WardId, Ward = b.Ward.Name, b.Status }).ToListAsync();
            var windowStart = date.AddDays(-29);
            var stays = await _db.IPDAdmissions.AsNoTracking()
                .Where(a => a.BedId != null && a.AdmissionDate <= dayEnd && (a.DischargeDate == null || a.DischargeDate >= windowStart))
                .Select(a => new { BedId = a.BedId!.Value, a.AdmissionDate, a.DischargeDate })
                .ToListAsync();
            bool OccupiedOn(DateTime day, DateTime admitted, DateTime? discharged) => admitted <= DateRange.EndOfDay(day) && (discharged == null || discharged >= day);
            var occupiedBeds = stays.Where(s => OccupiedOn(date, s.AdmissionDate, s.DischargeDate)).Select(s => s.BedId).ToHashSet();
            var isToday = date == DateTime.Now.Date;

            var wards = beds.GroupBy(b => new { b.WardId, b.Ward }).OrderBy(g => g.Key.Ward).Select(g =>
            {
                var total = g.Count();
                var occupied = isToday ? g.Count(b => b.Status == "Occupied") : g.Count(b => occupiedBeds.Contains(b.Id));
                return new object?[]
                {
                    g.Key.Ward, total, occupied, g.Count(b => b.Status == "Available"), g.Count(b => b.Status == "Cleaning"),
                    g.Count(b => b.Status == "Maintenance" || b.Status == "Blocked"), total == 0 ? 0 : occupied * 100m / total
                };
            }).ToList();

            var totalBeds = beds.Count;
            var occupiedTotal = wards.Sum(w => (int)w[2]!);
            decimal average = 0;
            if (totalBeds > 0)
            {
                var days = Enumerable.Range(0, 30).Select(i => windowStart.AddDays(i)).ToList();
                average = (decimal)days.Average(d => stays.Count(s => OccupiedOn(d, s.AdmissionDate, s.DischargeDate)) * 100.0 / totalBeds);
            }

            doc.Metrics.Add(new ReportMetric("Beds", totalBeds));
            doc.Metrics.Add(new ReportMetric("Occupied", occupiedTotal, K.Integer, "danger"));
            doc.Metrics.Add(new ReportMetric("Occupancy", totalBeds == 0 ? 0 : occupiedTotal * 100m / totalBeds, K.Percent, "warning"));
            doc.Metrics.Add(new ReportMetric("30-day average", average, K.Percent, "info"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Beds per ward",
                Columns = { new("Ward", width: 1.6f), new("Beds", K.Integer, 0.7f, true), new(isToday ? "Occupied" : "Occupied on date", K.Integer, 0.9f, true), new("Available now", K.Integer, 0.9f, true), new("Cleaning now", K.Integer, 0.9f, true), new("Maint./Blocked now", K.Integer, 1, true), new("Occupancy", K.Percent, 0.8f) },
                Rows = wards,
                ShowTotals = true
            });
            doc.Charts.Add(new ReportChartData { Title = "Occupancy per ward (%)", Labels = wards.Select(w => (string)w[0]!).ToList(), Values = wards.Select(w => Math.Round((decimal)w[6]!, 1)).ToList() });
            if (!isToday) doc.Notes.Add("\"Occupied on date\" comes from IPD admissions; the other bed statuses show the situation now.");
        }

        // ── System ───────────────────────────────────────────────

        private async Task UserLog(ReportDocument doc, DateTime from, DateTime to)
        {
            var logs = await _db.AuditLogs.AsNoTracking()
                .Where(a => a.Timestamp >= from && a.Timestamp <= to && (a.Action.StartsWith("LOGIN") || a.Action == "LOGOUT" || a.Action == "HOSPITAL_SELECTED"))
                .OrderByDescending(a => a.Timestamp)
                .Select(a => new { a.Timestamp, a.UserId, a.Action, a.IpAddress, a.NewValues })
                .Take(5000)
                .ToListAsync();
            var ids = logs.Where(l => l.UserId != null).Select(l => l.UserId!).Distinct().ToList();
            var users = await _db.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).Select(u => new { u.Id, u.UserName, u.FirstName, u.LastName }).ToDictionaryAsync(u => u.Id);
            static string Friendly(string action) => action switch
            {
                "LOGIN_SUCCESS" => "Signed in",
                "LOGIN_SUCCESS_MFA" => "Signed in (two-step)",
                "LOGOUT" => "Signed out",
                "HOSPITAL_SELECTED" => "Hospital chosen",
                "LOGIN_FAILED" or "LOGIN_FAILED_USER_NOT_FOUND" => "Failed sign-in",
                "LOGIN_FAILED_LOCKOUT" => "Locked out",
                "LOGIN_BLOCKED_CONCURRENT_LIMIT" => "Blocked (user limit)",
                _ => action
            };
            doc.Metrics.Add(new ReportMetric("Sign-ins", logs.Count(l => l.Action.StartsWith("LOGIN_SUCCESS")), K.Integer, "success"));
            doc.Metrics.Add(new ReportMetric("Failed sign-ins", logs.Count(l => l.Action.StartsWith("LOGIN_FAILED") || l.Action.StartsWith("LOGIN_BLOCKED")), K.Integer, "danger"));
            doc.Metrics.Add(new ReportMetric("Users", ids.Count, K.Integer, "info"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Sign-in log",
                Columns = { new("Time", K.DateTime, 1.2f), new("User", width: 1.1f), new("Name", width: 1.3f), new("Event", width: 1.1f), new("Details", width: 1.6f), new("IP address", width: 1) },
                Rows = logs.Select(l =>
                {
                    var u = l.UserId != null && users.TryGetValue(l.UserId, out var x) ? x : null;
                    return new object?[] { l.Timestamp, u?.UserName ?? "-", u == null ? "-" : $"{u.FirstName} {u.LastName}", Friendly(l.Action), l.Action == "HOSPITAL_SELECTED" || l.Action.StartsWith("LOGIN_FAILED_USER") ? l.NewValues : string.Empty, l.IpAddress };
                }).ToList()
            });
        }

        private async Task PatientAccounts(ReportDocument doc)
        {
            var rows = await (from p in _db.Patients.AsNoTracking()
                              join u in _db.Users.AsNoTracking() on p.UserId equals u.Id
                              orderby p.FirstName, p.LastName
                              select new { p.PatientId, p.FirstName, p.LastName, u.UserName, u.Email, p.Phone, u.IsActive, u.LastLoginDate, u.CreatedDate }).ToListAsync();
            doc.Metrics.Add(new ReportMetric("Portal accounts", rows.Count));
            doc.Metrics.Add(new ReportMetric("Active", rows.Count(r => r.IsActive), K.Integer, "success"));
            doc.Metrics.Add(new ReportMetric("Never signed in", rows.Count(r => r.LastLoginDate == null), K.Integer, "warning"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Patient Portal accounts",
                Columns = { new("Patient ID"), new("Patient", width: 1.4f), new("User name", width: 1.2f), new("E-mail", width: 1.6f), new("Phone"), new("Account", width: 0.7f), new("Last sign-in", K.DateTime, 1.2f), new("Created", K.Date) },
                Rows = rows.Select(r => new object?[] { r.PatientId, $"{r.FirstName} {r.LastName}", r.UserName, r.Email, r.Phone, r.IsActive ? "Active" : "Inactive", r.LastLoginDate, r.CreatedDate }).ToList(),
                EmptyText = "No patients have a Patient Portal account yet."
            });
            doc.Notes.Add("Passwords are stored encrypted and are never shown. Patients reset a forgotten password from the Patient Portal sign-in page.");
        }

        private async Task NotificationLog(ReportDocument doc, DateTime from, DateTime to)
        {
            var rows = await _db.NotificationDeliveryLogs.AsNoTracking()
                .Where(n => n.CreatedAt >= from && n.CreatedAt <= to)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new { n.CreatedAt, n.Channel, n.Provider, n.Recipient, n.Subject, n.Status, n.IsTest })
                .Take(5000)
                .ToListAsync();
            doc.Metrics.Add(new ReportMetric("Messages", rows.Count));
            doc.Metrics.Add(new ReportMetric("Sent", rows.Count(r => r.Status is "Sent" or "Delivered" or "Success"), K.Integer, "success"));
            doc.Metrics.Add(new ReportMetric("Failed", rows.Count(r => r.Status is "Failed" or "Error"), K.Integer, "danger"));
            doc.Sections.Add(new ReportSection
            {
                Title = "E-mails and text messages",
                Columns = { new("Time", K.DateTime, 1.2f), new("Channel", width: 0.7f), new("Provider", width: 0.9f), new("Recipient", width: 1.5f), new("Subject", width: 2), new("Status", width: 0.8f), new("Test", width: 0.5f) },
                Rows = rows.Select(r => new object?[] { r.CreatedAt, r.Channel, r.Provider, r.Recipient, r.Subject, r.Status, r.IsTest ? "Yes" : "" }).ToList()
            });
            doc.Charts.Add(Chart("Messages by channel", rows.GroupBy(r => r.Channel), g => g.Count(), type: "doughnut"));
        }

        // ── Inventory ────────────────────────────────────────────

        private async Task InventoryStock(ReportDocument doc)
        {
            var rows = await _db.InventoryItems.AsNoTracking().Where(i => i.IsActive)
                .OrderBy(i => i.Category).ThenBy(i => i.Name)
                .Select(i => new { i.ItemCode, i.Name, i.Category, i.Unit, i.StorageLocation, i.CurrentStock, i.ReorderLevel, i.MinimumStock, i.UnitCost, Vendor = i.Vendor != null ? i.Vendor.Name : i.Supplier })
                .ToListAsync();
            string Status(decimal stock, decimal reorder, decimal minimum) => stock <= 0 ? "Out of stock" : stock <= Math.Max(reorder, minimum) ? "Reorder" : "OK";
            doc.Metrics.Add(new ReportMetric("Items", rows.Count));
            doc.Metrics.Add(new ReportMetric("Stock value", rows.Sum(r => r.CurrentStock * r.UnitCost), K.Money, "success"));
            doc.Metrics.Add(new ReportMetric("To reorder", rows.Count(r => Status(r.CurrentStock, r.ReorderLevel, r.MinimumStock) == "Reorder"), K.Integer, "warning"));
            doc.Metrics.Add(new ReportMetric("Out of stock", rows.Count(r => r.CurrentStock <= 0), K.Integer, "danger"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Stock on hand",
                Columns = { new("Code", width: 0.8f), new("Item", width: 1.6f), new("Category"), new("Unit", width: 0.6f), new("Location"), new("Supplier", width: 1.1f), new("In stock", K.Number, 0.8f), new("Reorder at", K.Number, 0.8f), new("Unit cost", K.Money, 0.9f), new("Stock value", K.Money, 1, true), new("Status", width: 0.8f) },
                Rows = rows.Select(r => new object?[] { r.ItemCode, r.Name, r.Category, r.Unit, r.StorageLocation, r.Vendor, r.CurrentStock, r.ReorderLevel, r.UnitCost, r.CurrentStock * r.UnitCost, Status(r.CurrentStock, r.ReorderLevel, r.MinimumStock) }).ToList(),
                ShowTotals = true,
                EmptyText = "No inventory items."
            });
            doc.Charts.Add(Chart("Stock value by category", rows.GroupBy(r => r.Category), g => g.Sum(r => r.CurrentStock * r.UnitCost), money: true, type: "doughnut"));
        }

        private async Task InventoryItems(ReportDocument doc, DateTime from, DateTime to)
        {
            var items = await _db.InventoryItems.AsNoTracking().Where(i => i.IsActive).OrderBy(i => i.Name)
                .Select(i => new { i.Id, i.ItemCode, i.Name, i.Category, i.Unit, i.CurrentStock, i.UnitCost }).ToListAsync();
            var moves = await _db.InventoryTransactions.AsNoTracking().Where(t => t.TransactionDate >= from && t.TransactionDate <= to)
                .Select(t => new { t.InventoryItemId, t.TransactionType, t.Quantity, t.UnitCost }).ToListAsync();
            var rows = items.Select(i =>
            {
                var m = moves.Where(x => x.InventoryItemId == i.Id).ToList();
                var received = m.Where(x => x.TransactionType == "IN").Sum(x => x.Quantity);
                var issued = m.Where(x => x.TransactionType == "OUT").Sum(x => x.Quantity);
                return new object?[] { i.ItemCode, i.Name, i.Category, i.Unit, received, issued, m.Count(x => x.TransactionType != "IN" && x.TransactionType != "OUT"), i.CurrentStock, i.UnitCost, i.CurrentStock * i.UnitCost };
            }).ToList();
            doc.Metrics.Add(new ReportMetric("Items", items.Count));
            doc.Metrics.Add(new ReportMetric("Movements", moves.Count, K.Integer, "info"));
            doc.Metrics.Add(new ReportMetric("Value received", moves.Where(x => x.TransactionType == "IN").Sum(x => x.Quantity * x.UnitCost), K.Money, "success"));
            doc.Metrics.Add(new ReportMetric("Value issued", moves.Where(x => x.TransactionType == "OUT").Sum(x => x.Quantity * x.UnitCost), K.Money, "warning"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Movements per item",
                Columns = { new("Code", width: 0.8f), new("Item", width: 1.6f), new("Category"), new("Unit", width: 0.6f), new("Received", K.Number, 0.8f, true), new("Issued", K.Number, 0.8f, true), new("Adjustments", K.Integer, 0.8f, true), new("In stock now", K.Number, 0.9f), new("Unit cost", K.Money, 0.9f), new("Stock value", K.Money, 1, true) },
                Rows = rows,
                ShowTotals = true
            });
        }

        private async Task InventoryIssues(ReportDocument doc, DateTime from, DateTime to)
        {
            var rows = await _db.InventoryTransactions.AsNoTracking()
                .Where(t => t.TransactionType == "OUT" && t.TransactionDate >= from && t.TransactionDate <= to)
                .OrderBy(t => t.TransactionDate)
                .Select(t => new { t.TransactionDate, t.InventoryItem.ItemCode, t.InventoryItem.Name, t.InventoryItem.Category, t.InventoryItem.Unit, t.Quantity, t.UnitCost, t.Department, t.PatientId, t.ReferenceNumber, t.Remarks })
                .ToListAsync();
            var patientIds = rows.Where(r => r.PatientId != null).Select(r => r.PatientId!.Value).Distinct().ToList();
            var patients = await _db.Patients.AsNoTracking().Where(p => patientIds.Contains(p.Id)).Select(p => new { p.Id, Name = p.FirstName + " " + p.LastName, p.PatientId }).ToDictionaryAsync(p => p.Id);
            doc.Metrics.Add(new ReportMetric("Issues", rows.Count));
            doc.Metrics.Add(new ReportMetric("Value issued", rows.Sum(r => r.Quantity * r.UnitCost), K.Money, "warning"));
            doc.Metrics.Add(new ReportMetric("Departments", rows.Select(r => r.Department).Where(d => !string.IsNullOrWhiteSpace(d)).Distinct().Count(), K.Integer, "info"));
            doc.Sections.Add(new ReportSection
            {
                Title = "Items issued",
                Columns = { new("Date", K.DateTime, 1.2f), new("Code", width: 0.8f), new("Item", width: 1.5f), new("Quantity", K.Number, 0.7f), new("Unit", width: 0.6f), new("Department"), new("Patient", width: 1.3f), new("Reference"), new("Value", K.Money, 0.9f, true) },
                Rows = rows.Select(r => new object?[]
                {
                    r.TransactionDate, r.ItemCode, r.Name, r.Quantity, r.Unit, string.IsNullOrWhiteSpace(r.Department) ? "-" : r.Department,
                    r.PatientId != null && patients.TryGetValue(r.PatientId.Value, out var p) ? $"{p.Name} ({p.PatientId})" : "-", r.ReferenceNumber, r.Quantity * r.UnitCost
                }).ToList(),
                ShowTotals = true
            });
            doc.Charts.Add(Chart("Value issued by department", rows.GroupBy(r => string.IsNullOrWhiteSpace(r.Department) ? "Not given" : r.Department), g => g.Sum(r => r.Quantity * r.UnitCost), money: true));
        }

        // ── helpers ──────────────────────────────────────────────

        private static ReportChartData Chart<T>(string title, IEnumerable<IGrouping<string, T>> groups, Func<IGrouping<string, T>, decimal> value, bool money = false, string type = "bar", int top = 12)
        {
            var list = groups.Select(g => (Label: string.IsNullOrWhiteSpace(g.Key) ? "Not set" : g.Key, Value: value(g))).OrderByDescending(x => x.Value).Take(top).ToList();
            return new ReportChartData { Title = title, Type = type, IsMoney = money, Labels = list.Select(x => x.Label).ToList(), Values = list.Select(x => x.Value).ToList() };
        }
    }
}
