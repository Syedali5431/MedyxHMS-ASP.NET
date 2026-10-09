using System.Globalization;
using MedyxHMS.Data;
using MedyxHMS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Purpose: Sample records for January to October 2026 in every module (all hospitals of the group), so the
// dashboards, lists and reports of the current year have data: appointments, OPD visits with bills and payments,
// admissions with discharge reports and IPD bills, laboratory and radiology, pharmacy, operations, referrals,
// insurance claims, blood bank, ambulance, births and deaths, front office, HR (leave, payroll, training), quality,
// equipment, live consultations and website appointment requests.
// Runs once (setting SampleData:Year2026) when Seeding:Year2026SampleData or Seeding:DemoData is on; numbers continue
// the system's own numbering (BILLyyyyMMddnnnn, RXBILL-2026-nnnn, Lyymmddnnnn).
namespace MedyxHMS.Services.Implementations
{
    public class Year2026SampleDataSeeder
    {
        public const string MarkerKey = "SampleData:Year2026";
        private const string By = "System";
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly ILogger<Year2026SampleDataSeeder> _logger;
        private readonly Random _rnd = new(2026);

        public Year2026SampleDataSeeder(ApplicationDbContext db, UserManager<ApplicationUser> users, ILogger<Year2026SampleDataSeeder> logger)
        {
            _db = db;
            _users = users;
            _logger = logger;
        }

        public async Task<bool> SeedAsync()
        {
            // Insurance claims have their own marker: they need TPA providers, which older databases may not have had.
            await SeedTpaClaimsOnceAsync();
            // Operation bills and the medicines/investigations on admission bills, once (added after the first release).
            var addCharges = !await _db.Settings.AnyAsync(s => s.Key == ChargesMarkerKey);

            if (await _db.Settings.AnyAsync(s => s.Key == MarkerKey))
            {
                if (addCharges) await SeedChargesOnceAsync();
                return false;
            }

            var hospitals = await _db.Hospitals.Where(h => h.IsActive).OrderBy(h => h.Id).Select(h => h.Id).ToListAsync();
            var patients = await _db.Patients.Where(p => p.IsActive).OrderBy(p => p.Id).ToListAsync();
            var doctors = await _db.Doctors.Where(d => d.IsActive).OrderBy(d => d.Id).ToListAsync();
            if (hospitals.Count == 0 || patients.Count < 5 || doctors.Count == 0)
            {
                _logger.LogInformation("2026 sample data skipped: no hospitals, patients or doctors yet.");
                return false;
            }

            var months = MonthsToSeed();
            var counts = new Dictionary<string, int>();
            void Count(string what, int n) => counts[what] = counts.GetValueOrDefault(what) + n;

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                Count("appointments", await AppointmentsAsync(hospitals, patients, doctors, months));
                Count("OPD visits", await OpdVisitsAsync(hospitals, patients, doctors, months));
                Count("admissions", await AdmissionsAsync(hospitals, patients, doctors, months));
                Count("lab results", await LabAsync(patients, months));
                Count("radiology results", await RadiologyAsync(patients, months));
                Count("pharmacy bills", await PharmacyAsync(hospitals, patients, months));
                Count("operations", await OperationsAsync(hospitals, patients, doctors, months));
                Count("referrals", await ReferralsAsync(patients, months));
                Count("blood issues", await BloodAsync(patients, months));
                Count("ambulance trips", await AmbulanceAsync(patients, months));
                Count("births and deaths", await BirthsAndDeathsAsync(patients, doctors, months));
                Count("front office", await FrontOfficeAsync(months));
                Count("HR records", await HrAsync(months));
                Count("quality records", await QualityAsync(hospitals, patients, months));
                Count("equipment records", await EquipmentAsync(hospitals, months));
                Count("online sessions and requests", await OnlineAsync(patients, doctors, months));

                _db.Settings.Add(new Setting
                {
                    Key = MarkerKey,
                    Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm", Inv),
                    Type = "string",
                    Category = "SampleData",
                    Description = "Sample records for January–October 2026 were added on this date.",
                    IsSystem = true,
                    ModifiedBy = By,
                    ModifiedDate = DateTime.Now
                });
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Adding the 2026 sample data failed; nothing was saved.");
                return false;
            }
            if (addCharges) await SeedChargesOnceAsync();

            _logger.LogInformation("2026 sample data added: {Counts}", string.Join(", ", counts.Select(c => $"{c.Value} {c.Key}")));
            return true;
        }

        // ── calendar ─────────────────────────────────────────────

        /// <summary>January to the current month of 2026 (at most October), days up to yesterday.</summary>
        private static List<DateTime> MonthsToSeed()
        {
            var last = DateTime.Today.Year == 2026 ? Math.Min(DateTime.Today.Month, 10) : 10;
            return Enumerable.Range(1, last).Select(m => new DateTime(2026, m, 1)).ToList();
        }

        /// <summary>The n-th of `count` spread days in the month (9:00–16:00), never today or later.</summary>
        private DateTime Day(DateTime month, int n, int count, int hour = -1)
        {
            var days = DateTime.DaysInMonth(month.Year, month.Month);
            var lastDay = month.Year == DateTime.Today.Year && month.Month == DateTime.Today.Month ? Math.Max(1, DateTime.Today.Day - 1) : days;
            var day = Math.Clamp(1 + (int)((n + 0.5) * lastDay / Math.Max(1, count)), 1, lastDay);
            var h = hour >= 0 ? hour : 9 + _rnd.Next(0, 8);
            return new DateTime(month.Year, month.Month, day, h, _rnd.Next(0, 4) * 15, 0);
        }

        private T Pick<T>(IReadOnlyList<T> list) => list[_rnd.Next(list.Count)];

        private static readonly string[] FirstNames = { "Ahmed", "Fatima", "Bilal", "Ayesha", "Usman", "Sana", "Hamza", "Zainab", "Imran", "Mariam", "Faisal", "Hira", "Kamran", "Nida", "Tariq", "Sadia" };
        private static readonly string[] LastNames = { "Khan", "Ahmed", "Malik", "Hussain", "Qureshi", "Sheikh", "Butt", "Chaudhry", "Raza", "Siddiqui", "Mirza", "Iqbal" };
        private string PersonName() => $"{Pick(FirstNames)} {Pick(LastNames)}";
        private string Phone() => $"03{_rnd.Next(0, 5)}{_rnd.Next(0, 10)}-{_rnd.Next(1000000, 9999999)}";

        // ── number series (continue the system's numbering) ─────

        private Dictionary<string, int>? _billSeq;
        private async Task<string> NextBillNumberAsync(DateTime date)
        {
            if (_billSeq == null)
            {
                _billSeq = new Dictionary<string, int>();
                var existing = await _db.Bills.IgnoreQueryFilters().Where(b => b.BillNumber.StartsWith("BILL2026")).Select(b => b.BillNumber).ToListAsync();
                foreach (var n in existing.Where(n => n.Length >= 16))
                {
                    if (int.TryParse(n.Substring(12), out var seq)) _billSeq[n[..12]] = Math.Max(_billSeq.GetValueOrDefault(n[..12]), seq);
                }
            }
            var prefix = "BILL" + date.ToString("yyyyMMdd", Inv);
            var next = _billSeq.GetValueOrDefault(prefix) + 1;
            _billSeq[prefix] = next;
            return $"{prefix}{next:D4}";
        }

        // ── clinical ─────────────────────────────────────────────

        private static readonly (string Symptoms, string Diagnosis, string Treatment)[] OpdCases =
        {
            ("Fever and body aches for 3 days", "Viral fever", "Paracetamol, fluids and rest"),
            ("Headache, high readings at home", "Essential hypertension", "Amlodipine 5 mg daily; salt restriction"),
            ("Increased thirst and urination", "Type 2 diabetes mellitus", "Metformin; diet counselling; HbA1c in 3 months"),
            ("Cough with sputum for a week", "Acute bronchitis", "Amoxicillin; steam inhalation"),
            ("Lower back pain after lifting", "Mechanical low back pain", "Ibuprofen; physiotherapy exercises"),
            ("Sneezing and itchy eyes", "Allergic rhinitis", "Cetirizine at night"),
            ("Burning upper abdominal pain", "Gastritis", "Omeprazole 20 mg before breakfast"),
            ("Tiredness and pale skin", "Iron deficiency anaemia", "Ferrous sulphate; CBC after 6 weeks"),
            ("Sore throat and fever", "Acute tonsillitis", "Azithromycin; warm saline gargles"),
            ("Knee pain on walking", "Osteoarthritis of the knee", "Analgesics; quadriceps exercises"),
        };

        private async Task<int> AppointmentsAsync(List<int> hospitals, List<Patient> patients, List<Doctor> doctors, List<DateTime> months)
        {
            var list = new List<Appointment>();
            foreach (var month in months)
            foreach (var h in hospitals)
            {
                for (var i = 0; i < 4; i++)
                {
                    var at = Day(month, i, 4);
                    var c = Pick(OpdCases);
                    var roll = _rnd.Next(10);
                    list.Add(new Appointment
                    {
                        HospitalId = h,
                        PatientId = Pick(patients).Id,
                        DoctorId = Pick(doctors).Id,
                        AppointmentDate = at.Date,
                        AppointmentTime = at.TimeOfDay,
                        Status = roll < 7 ? "Completed" : roll < 9 ? "Cancelled" : "No-Show",
                        AppointmentType = roll % 3 == 0 ? "Follow-up" : "Consultation",
                        Priority = roll == 0 ? "Urgent" : "Normal",
                        Symptoms = c.Symptoms,
                        Notes = string.Empty,
                        CreatedDate = at.AddDays(-_rnd.Next(1, 8)),
                        CreatedBy = By,
                        UpdatedBy = By
                    });
                }
            }
            _db.Appointments.AddRange(list);
            await _db.SaveChangesAsync();
            return list.Count;
        }

        private async Task<int> OpdVisitsAsync(List<int> hospitals, List<Patient> patients, List<Doctor> doctors, List<DateTime> months)
        {
            var visits = new List<OPDVisit>();
            foreach (var month in months)
            foreach (var h in hospitals)
            {
                for (var i = 0; i < 4; i++)
                {
                    var at = Day(month, i, 4);
                    var c = Pick(OpdCases);
                    var recent = month.Month >= 9;
                    visits.Add(new OPDVisit
                    {
                        HospitalId = h,
                        PatientId = Pick(patients).Id,
                        DoctorId = Pick(doctors).Id,
                        VisitDate = at,
                        Symptoms = c.Symptoms,
                        Diagnosis = c.Diagnosis,
                        Treatment = c.Treatment,
                        Prescription = c.Treatment,
                        Notes = "Review in 2 weeks if not better",
                        ConsultationFee = new[] { 800m, 1000m, 1200m, 1500m }[_rnd.Next(4)],
                        PaymentStatus = recent && _rnd.Next(4) == 0 ? "Pending" : "Paid",
                        CreatedDate = at,
                        CreatedBy = By
                    });
                }
            }
            _db.OPDVisits.AddRange(visits);
            await _db.SaveChangesAsync();

            // Consultation bills (as the OPD page creates them) with their payments.
            foreach (var v in visits)
            {
                var paid = v.PaymentStatus == "Paid";
                var bill = new Bill
                {
                    HospitalId = v.HospitalId,
                    BillNumber = await NextBillNumberAsync(v.VisitDate),
                    PatientId = v.PatientId,
                    BillDate = v.VisitDate,
                    DueDate = v.VisitDate.Date,
                    TotalAmount = v.ConsultationFee,
                    PaidAmount = paid ? v.ConsultationFee : 0,
                    PendingAmount = paid ? 0 : v.ConsultationFee,
                    Status = paid ? "Paid" : "Unpaid",
                    BillType = "OPD",
                    Notes = $"OPD consultation bill for OPD Visit ID: {v.Id}",
                    CreatedDate = v.VisitDate,
                    CreatedBy = By,
                    BillItems = new List<BillItem>
                    {
                        new()
                        {
                            ItemName = "OPD Consultation", ItemType = "Service", Quantity = 1, UnitPrice = v.ConsultationFee, TotalPrice = v.ConsultationFee,
                            Description = $"Consultation charge for OPD Visit #{v.Id}", CreatedDate = v.VisitDate
                        }
                    }
                };
                if (paid)
                {
                    bill.Payments.Add(new Payment
                    {
                        PaymentMethod = _rnd.Next(3) switch { 0 => "Card", 1 => "Online", _ => "Cash" },
                        Amount = v.ConsultationFee,
                        TransactionId = $"TXN{v.VisitDate:yyyyMMdd}{v.Id:D5}",
                        PaymentGateway = string.Empty,
                        Status = "Completed",
                        Notes = string.Empty,
                        PaymentDate = v.VisitDate.AddMinutes(20),
                        ProcessedBy = By
                    });
                }
                _db.Bills.Add(bill);
            }
            await _db.SaveChangesAsync();
            return visits.Count;
        }

        private static readonly (string Reason, string Diagnosis, string Course, string Medicines, string Advice)[] IpdCases =
        {
            ("Emergency admission – fever, vomiting and dehydration", "Acute gastroenteritis with dehydration", "IV fluids and antiemetics; tolerated oral diet after 24 hours.", "ORS sachets as needed; Omeprazole 20 mg daily x 5 days", "Soft diet and plenty of fluids; return if vomiting recurs."),
            ("Planned admission for surgery", "Acute appendicitis – laparoscopic appendectomy", "Uneventful laparoscopic appendectomy; wound clean, mobilising on day 1.", "Cefixime 400 mg daily x 5 days; Paracetamol 1 g as needed", "Keep the wound dry for 5 days; no heavy lifting for 4 weeks."),
            ("Emergency admission – shortness of breath", "Community-acquired pneumonia", "IV antibiotics and oxygen for 2 days, then oral antibiotics; saturation normal on room air.", "Amoxicillin-clavulanate 625 mg three times daily x 5 days", "Rest at home for a week; chest X-ray after 6 weeks."),
            ("Emergency admission – high blood sugar", "Uncontrolled type 2 diabetes with hyperglycaemia", "Insulin infusion then basal-bolus insulin; diabetes education given.", "Insulin glargine 18 units at night; Metformin 1 g twice daily", "Check blood sugar twice daily; diabetic diet."),
            ("Emergency admission – chest pain", "Unstable angina", "Observed on cardiac monitor; troponin negative; started on antiplatelet and statin.", "Aspirin 75 mg; Clopidogrel 75 mg; Atorvastatin 40 mg; Bisoprolol 2.5 mg", "No smoking; walk 30 minutes daily; cardiology clinic in 2 weeks."),
            ("Planned admission – delivery", "Normal vaginal delivery, healthy baby", "Uncomplicated labour and delivery; mother and baby well.", "Iron and folic acid daily; Paracetamol as needed", "Breastfeeding support; postnatal check after 6 weeks."),
        };

        private async Task<int> AdmissionsAsync(List<int> hospitals, List<Patient> patients, List<Doctor> doctors, List<DateTime> months)
        {
            var beds = await _db.Beds.IgnoreQueryFilters().Include(b => b.Ward).Where(b => b.IsActive).ToListAsync();
            var admissions = new List<(IPDAdmission Admission, int Case)>();
            foreach (var month in months)
            foreach (var h in hospitals)
            {
                var hospitalBeds = beds.Where(b => b.Ward.HospitalId == h).ToList();
                if (hospitalBeds.Count == 0) continue;
                for (var i = 0; i < 2; i++)
                {
                    var admitted = Day(month, i, 2, 10 + i * 3);
                    if (admitted.Date >= DateTime.Today.AddDays(-3)) continue; // only completed stays
                    var stay = _rnd.Next(2, 6);
                    var discharged = admitted.AddDays(stay).Date.AddHours(12).AddMinutes(_rnd.Next(0, 4) * 15);
                    if (discharged >= DateTime.Now) discharged = DateTime.Today.AddDays(-1).AddHours(12);
                    var c = _rnd.Next(IpdCases.Length);
                    var bed = Pick(hospitalBeds);
                    admissions.Add((new IPDAdmission
                    {
                        HospitalId = h,
                        PatientId = Pick(patients).Id,
                        DoctorId = Pick(doctors).Id,
                        BedId = bed.Id,
                        AdmissionDate = admitted,
                        DischargeDate = discharged,
                        AdmissionType = IpdCases[c].Reason.StartsWith("Planned") ? "Planned" : "Emergency",
                        Diagnosis = IpdCases[c].Diagnosis,
                        Treatment = IpdCases[c].Course,
                        Notes = string.Empty,
                        Status = "Discharged",
                        DailyCharges = bed.DailyCharges > 0 ? bed.DailyCharges : 3500m,
                        CreatedDate = admitted,
                        CreatedBy = By
                    }, c));
                }
            }
            _db.IPDAdmissions.AddRange(admissions.Select(a => a.Admission));
            await _db.SaveChangesAsync();

            var doctorNames = doctors.ToDictionary(d => d.Id, d => $"Dr. {d.FirstName} {d.LastName}" + (string.IsNullOrWhiteSpace(d.Specialization) ? "" : $" ({d.Specialization})"));
            foreach (var (a, c) in admissions)
            {
                var days = Math.Max(1, (a.DischargeDate!.Value.Date - a.AdmissionDate.Date).Days);
                var total = a.DailyCharges * days;
                var paid = a.DischargeDate.Value.Month < 9 || _rnd.Next(3) > 0;
                var bill = new Bill
                {
                    HospitalId = a.HospitalId,
                    BillNumber = await NextBillNumberAsync(a.DischargeDate.Value),
                    PatientId = a.PatientId,
                    BillDate = a.DischargeDate.Value,
                    DueDate = a.DischargeDate.Value.Date,
                    TotalAmount = total,
                    PaidAmount = paid ? total : 0,
                    PendingAmount = paid ? 0 : total,
                    Status = paid ? "Paid" : "Unpaid",
                    BillType = "IPD",
                    Notes = $"IPD daily charges bill for IPD Admission ID: {a.Id}",
                    CreatedDate = a.DischargeDate.Value,
                    CreatedBy = By,
                    BillItems = new List<BillItem>
                    {
                        new()
                        {
                            ItemName = "IPD Daily Charges", ItemType = "Service", Quantity = days, UnitPrice = a.DailyCharges, TotalPrice = total,
                            Description = $"Daily charges for {days} day(s), IPD Admission #{a.Id}", CreatedDate = a.DischargeDate.Value
                        }
                    }
                };
                if (paid)
                {
                    bill.Payments.Add(new Payment
                    {
                        PaymentMethod = _rnd.Next(2) == 0 ? "Card" : "Cash", Amount = total, TransactionId = $"TXN{a.DischargeDate:yyyyMMdd}I{a.Id:D5}",
                        PaymentGateway = string.Empty, Status = "Completed", Notes = string.Empty, PaymentDate = a.DischargeDate.Value.AddHours(1), ProcessedBy = By
                    });
                }
                _db.Bills.Add(bill);

                var k = IpdCases[c];
                _db.DischargeSummaries.Add(new DischargeSummary
                {
                    HospitalId = a.HospitalId,
                    IPDAdmissionId = a.Id,
                    PatientId = a.PatientId,
                    AdmissionDate = a.AdmissionDate,
                    DischargeDate = a.DischargeDate.Value,
                    ConditionAtDischarge = _rnd.Next(3) == 0 ? "Recovered" : "Improved",
                    ReasonForAdmission = k.Reason,
                    FinalDiagnosis = k.Diagnosis,
                    HospitalCourse = k.Course,
                    ProceduresPerformed = string.Empty,
                    InvestigationsSummary = "CBC, electrolytes and renal function at admission and before discharge.",
                    DischargeMedications = k.Medicines,
                    AdviceOnDischarge = k.Advice,
                    FollowUpInstructions = "Review in the OPD with this summary.",
                    FollowUpDate = a.DischargeDate.Value.Date.AddDays(7),
                    AttendingDoctor = doctorNames.GetValueOrDefault(a.DoctorId, string.Empty),
                    Status = DischargeSummary.StatusCompleted,
                    CreatedByName = "Ward nurse",
                    CreatedAt = a.DischargeDate.Value,
                    CompletedByName = doctorNames.GetValueOrDefault(a.DoctorId, "Doctor"),
                    CompletedAt = a.DischargeDate.Value.AddHours(2)
                });
            }
            await _db.SaveChangesAsync();
            return admissions.Count;
        }

        private static readonly (string Value, string Interpretation)[] LabValues =
        {
            ("Normal", "Normal"), ("Within reference range", "Normal"), ("Slightly raised", "High"), ("Low", "Low"), ("Normal", "Normal")
        };

        private async Task<int> LabAsync(List<Patient> patients, List<DateTime> months)
        {
            var tests = await _db.LabTests.Where(t => t.IsActive).ToListAsync();
            if (tests.Count == 0) return 0;
            var signer = (await _users.GetUsersInRoleAsync("Pathologist")).FirstOrDefault()
                         ?? (await _users.GetUsersInRoleAsync("Admin")).FirstOrDefault();
            var signerName = signer == null ? "Pathologist" : $"{signer.FirstName} {signer.LastName}".Trim();

            var accession = new Dictionary<string, int>();
            foreach (var a in await _db.LabResults.Where(r => r.AccessionNumber != null && r.AccessionNumber.StartsWith("L26")).Select(r => r.AccessionNumber!).ToListAsync())
            {
                if (a.Length == 11 && int.TryParse(a[7..], out var n)) accession[a[..7]] = Math.Max(accession.GetValueOrDefault(a[..7]), n);
            }

            var list = new List<LabResult>();
            var seq = 0;
            foreach (var month in months)
            {
                for (var i = 0; i < 12; i++)
                {
                    var ordered = Day(month, i, 12, 8 + i % 4);
                    var test = Pick(tests);
                    var (value, interpretation) = Pick(LabValues);
                    var prefix = "L" + ordered.ToString("yyMMdd", Inv);
                    var next = accession.GetValueOrDefault(prefix) + 1;
                    accession[prefix] = next;
                    var signedAt = ordered.AddHours(5);
                    var r = new LabResult
                    {
                        PatientId = Pick(patients).Id,
                        LabTestId = test.Id,
                        OrderNumber = $"LAB-{ordered:yyyyMMddHHmm}{++seq % 60:D2}",
                        OrderDate = ordered,
                        ResultDate = ordered.AddHours(4),
                        ResultValue = value,
                        NormalRange = test.NormalRange,
                        Unit = test.Unit,
                        Interpretation = interpretation,
                        Status = "Completed",
                        PerformedBy = "Lab technician",
                        VerifiedBy = signerName,
                        Notes = string.Empty,
                        CreatedDate = ordered,
                        AccessionNumber = $"{prefix}{next:D4}",
                        SampleType = LabTraceabilityService.DefaultSampleType(test.Category),
                        SampleStatus = "Received",
                        CollectedAt = ordered.AddMinutes(20),
                        CollectedBy = "Phlebotomist",
                        ReceivedAt = ordered.AddMinutes(50),
                        ReceivedBy = "Lab technician",
                        ResultEnteredAt = ordered.AddHours(4),
                        ResultEnteredBy = "Lab technician",
                        SignedOffAt = signedAt,
                        SignedOffBy = signerName,
                        SignedOffByUserId = signer?.Id ?? string.Empty
                    };
                    r.SignatureHash = LabTraceabilityService.ComputeSignature(r, r.SignedOffByUserId, signedAt);
                    list.Add(r);
                }
            }
            _db.LabResults.AddRange(list);
            await _db.SaveChangesAsync();
            return list.Count;
        }

        private static readonly (string Findings, string Impression)[] ScanReports =
        {
            ("Lungs clear. Heart size normal. No pleural effusion.", "Normal chest X-ray."),
            ("Liver, gallbladder, pancreas and kidneys normal in size and echotexture.", "Normal abdominal ultrasound."),
            ("Mild degenerative changes of the lumbar spine.", "Lumbar spondylosis."),
            ("Patchy consolidation in the right lower zone.", "Right lower lobe pneumonia."),
            ("Single live intrauterine pregnancy, normal growth for dates.", "Normal obstetric scan."),
        };

        private async Task<int> RadiologyAsync(List<Patient> patients, List<DateTime> months)
        {
            var tests = await _db.RadiologyTests.Where(t => t.IsActive).ToListAsync();
            if (tests.Count == 0) return 0;
            var list = new List<RadiologyResult>();
            var seq = 0;
            foreach (var month in months)
            {
                for (var i = 0; i < 6; i++)
                {
                    var ordered = Day(month, i, 6, 9 + i % 5);
                    var (findings, impression) = Pick(ScanReports);
                    list.Add(new RadiologyResult
                    {
                        PatientId = Pick(patients).Id,
                        RadiologyTestId = Pick(tests).Id,
                        OrderNumber = $"RAD-{ordered:yyyyMMddHHmm}{++seq % 60:D2}",
                        OrderDate = ordered,
                        ResultDate = ordered.AddHours(3),
                        Findings = findings,
                        Impression = impression,
                        Status = "Completed",
                        PerformedBy = "Radiographer",
                        VerifiedBy = "Consultant radiologist",
                        ImagePath = string.Empty,
                        Notes = string.Empty,
                        CreatedDate = ordered
                    });
                }
            }
            _db.RadiologyResults.AddRange(list);
            await _db.SaveChangesAsync();
            return list.Count;
        }

        private async Task<int> PharmacyAsync(List<int> hospitals, List<Patient> patients, List<DateTime> months)
        {
            var medicines = await _db.Medicines.Where(m => m.IsActive).ToListAsync();
            if (medicines.Count == 0) return 0;
            const string prefix = "RXBILL-2026-";
            var numbers = await _db.PharmacyBills.IgnoreQueryFilters().Where(b => b.BillNumber.StartsWith(prefix)).Select(b => b.BillNumber).ToListAsync();
            var next = numbers.Select(n => int.TryParse(n.Substring(prefix.Length, Math.Min(4, n.Length - prefix.Length)), out var x) ? x : 0).DefaultIfEmpty(0).Max();

            var count = 0;
            foreach (var month in months)
            foreach (var h in hospitals)
            {
                for (var i = 0; i < 3; i++)
                {
                    var at = Day(month, i, 3);
                    var bill = new PharmacyBill
                    {
                        HospitalId = h,
                        BillNumber = $"{prefix}{++next:D4}",
                        PatientId = Pick(patients).Id,
                        BillDate = at,
                        Status = "Paid",
                        PaymentMethod = _rnd.Next(2) == 0 ? "Cash" : "Card",
                        Notes = string.Empty,
                        CreatedDate = at,
                        CreatedBy = By
                    };
                    var lines = new List<Prescription>();
                    foreach (var med in medicines.OrderBy(_ => _rnd.Next()).Take(_rnd.Next(1, 3)))
                    {
                        var qty = new[] { 10, 14, 20, 30 }[_rnd.Next(4)];
                        lines.Add(new Prescription
                        {
                            PharmacyBill = bill, MedicineId = med.Id, Dosage = "1 tablet", Frequency = _rnd.Next(2) == 0 ? "Twice daily" : "Once daily",
                            Duration = qty / 2, Quantity = qty, UnitPrice = med.UnitPrice, TotalPrice = med.UnitPrice * qty, Instructions = "After meals", CreatedDate = at
                        });
                    }
                    bill.TotalAmount = lines.Sum(l => l.TotalPrice);
                    bill.PaidAmount = bill.TotalAmount;
                    _db.PharmacyBills.Add(bill);
                    _db.Prescriptions.AddRange(lines);
                    count++;
                }
            }
            await _db.SaveChangesAsync();
            return count;
        }

        private static readonly string[] Procedures = { "Laparoscopic appendectomy", "Laparoscopic cholecystectomy", "Inguinal hernia repair", "Caesarean section", "Open reduction and internal fixation – radius", "Tonsillectomy", "Cataract extraction with lens implant" };

        private async Task<int> OperationsAsync(List<int> hospitals, List<Patient> patients, List<Doctor> doctors, List<DateTime> months)
        {
            var theatres = await _db.OperationTheatres.IgnoreQueryFilters().Where(t => t.IsActive).ToListAsync();
            var list = new List<OTSchedule>();
            foreach (var month in months)
            foreach (var h in hospitals)
            {
                var theatre = theatres.FirstOrDefault(t => t.HospitalId == h);
                for (var i = 0; i < 2; i++)
                {
                    var at = Day(month, i, 2, 9 + i * 3);
                    var surgeon = Pick(doctors);
                    list.Add(new OTSchedule
                    {
                        HospitalId = h,
                        PatientId = Pick(patients).Id,
                        ProcedureName = Pick(Procedures),
                        SurgeonName = $"Dr. {surgeon.FirstName} {surgeon.LastName}",
                        SurgeonDoctorId = surgeon.Id,
                        ScheduledDate = at,
                        EstimatedDurationMinutes = new[] { 60, 90, 120 }[_rnd.Next(3)],
                        OperationTheatreNumber = theatre?.Name ?? "OT-1",
                        OperationTheatreId = theatre?.Id,
                        Status = _rnd.Next(10) == 0 ? "Cancelled" : "Completed",
                        IsEmergency = _rnd.Next(5) == 0,
                        Notes = string.Empty,
                        CreatedDate = at.AddDays(-3)
                    });
                }
            }
            _db.OTSchedules.AddRange(list);
            await _db.SaveChangesAsync();
            return list.Count;
        }

        private async Task<int> ReferralsAsync(List<Patient> patients, List<DateTime> months)
        {
            var places = new[] { "Shaukat Khanum Memorial Hospital, Lahore", "Aga Khan University Hospital, Karachi", "Pakistan Institute of Medical Sciences, Islamabad", "Punjab Institute of Cardiology, Lahore" };
            var reasons = new[] { "Oncology opinion", "Cardiac catheterisation", "Neurosurgical assessment", "Paediatric cardiology review" };
            var count = 0;
            foreach (var month in months)
            {
                for (var i = 0; i < 2; i++)
                {
                    var at = Day(month, i, 2);
                    _db.Referrals.Add(new Referral
                    {
                        PatientId = Pick(patients).Id, ReferralType = "External", ReferredTo = Pick(places), ReferralReason = Pick(reasons),
                        ReferralDate = at, Status = month.Month >= 9 && i == 1 ? "Pending" : "Completed", TpaProvider = string.Empty, TpaPolicyNumber = string.Empty,
                        Notes = string.Empty, CreatedDate = at
                    });
                    count++;
                }
            }
            await _db.SaveChangesAsync();
            return count;
        }

        public const string ChargesMarkerKey = "SampleData:Year2026:Charges";

        /// <summary>
        /// Bills for the 2026 sample operations (surgery, anaesthesia, theatre consumables) and the medicines,
        /// investigations and doctor's visits on the 2026 sample admission bills – once.
        /// </summary>
        private async Task SeedChargesOnceAsync()
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var ops = await _db.OTSchedules.IgnoreQueryFilters()
                    .Where(o => o.ScheduledDate.Year == 2026 && o.Status == "Completed" && o.BillId == null && o.CreatedDate.Year == 2026)
                    .ToListAsync();
                foreach (var o in ops)
                {
                    var surgery = _rnd.Next(12, 30) * 10000m;
                    var anaesthesia = _rnd.Next(25, 45) * 1000m;
                    var consumables = _rnd.Next(15, 40) * 1000m;
                    var total = surgery + anaesthesia + consumables;
                    var billDate = o.ScheduledDate.AddHours(4);
                    var paid = billDate.Month < 9 || _rnd.Next(3) > 0;
                    var bill = new Bill
                    {
                        HospitalId = o.HospitalId, BillNumber = await NextBillNumberAsync(billDate), PatientId = o.PatientId, BillDate = billDate, DueDate = billDate.Date.AddDays(10),
                        TotalAmount = total, PaidAmount = paid ? total : 0, PendingAmount = paid ? 0 : total, Status = paid ? "Paid" : "Unpaid", BillType = "OT",
                        Notes = $"OT booking for {o.ProcedureName}", CreatedDate = billDate, CreatedBy = By,
                        BillItems = new List<BillItem>
                        {
                            new() { ItemName = $"Surgeon's fee – {o.ProcedureName}", ItemType = "OT", Quantity = 1, UnitPrice = surgery, TotalPrice = surgery, Description = $"{o.OperationTheatreNumber}, {o.EstimatedDurationMinutes} min", CreatedDate = billDate },
                            new() { ItemName = "Anaesthesia", ItemType = "OT", Quantity = 1, UnitPrice = anaesthesia, TotalPrice = anaesthesia, Description = string.Empty, CreatedDate = billDate },
                            new() { ItemName = "Theatre consumables", ItemType = "OT", Quantity = 1, UnitPrice = consumables, TotalPrice = consumables, Description = string.Empty, CreatedDate = billDate },
                        }
                    };
                    if (paid)
                    {
                        bill.Payments.Add(new Payment
                        {
                            PaymentMethod = _rnd.Next(3) switch { 0 => "Insurance", 1 => "Card", _ => "Cash" }, Amount = total, TransactionId = $"TXN{billDate:yyyyMMdd}T{o.Id:D5}",
                            PaymentGateway = string.Empty, Status = "Completed", Notes = string.Empty, PaymentDate = billDate.AddHours(2), ProcessedBy = By
                        });
                    }
                    _db.Bills.Add(bill);
                    await _db.SaveChangesAsync();
                    o.BillId = bill.Id;
                }

                // Admission bills of the 2026 sample: medicines, investigations and doctor's visits besides the bed.
                var ipdBills = await _db.Bills.IgnoreQueryFilters().Include(b => b.BillItems).Include(b => b.Payments)
                    .Where(b => b.BillType == "IPD" && b.BillDate.Year == 2026 && b.CreatedBy == By && b.Notes.StartsWith("IPD daily charges bill for IPD Admission ID:"))
                    .ToListAsync();
                foreach (var bill in ipdBills.Where(b => b.BillItems.Count == 1))
                {
                    var days = Math.Max(1, (int)bill.BillItems.First().Quantity);
                    var extra = new List<BillItem>
                    {
                        new() { ItemName = "Medicines and IV fluids", ItemType = "Medicine", Quantity = 1, UnitPrice = _rnd.Next(12, 45) * 1000m, Description = string.Empty, CreatedDate = bill.BillDate },
                        new() { ItemName = "Laboratory and radiology", ItemType = "Lab", Quantity = 1, UnitPrice = _rnd.Next(8, 25) * 1000m, Description = string.Empty, CreatedDate = bill.BillDate },
                        new() { ItemName = "Consultant visits", ItemType = "Service", Quantity = days, UnitPrice = 5000m, Description = $"{days} day(s)", CreatedDate = bill.BillDate },
                    };
                    foreach (var item in extra) { item.TotalPrice = item.UnitPrice * item.Quantity; bill.BillItems.Add(item); }
                    var added = extra.Sum(i => i.TotalPrice);
                    bill.TotalAmount += added;
                    var payment = bill.Payments.FirstOrDefault();
                    if (payment != null)
                    {
                        payment.Amount += added;
                        bill.PaidAmount = bill.TotalAmount;
                    }
                    bill.PendingAmount = bill.TotalAmount - bill.PaidAmount;
                }

                _db.Settings.Add(new Setting
                {
                    Key = ChargesMarkerKey, Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm", Inv), Type = "string", Category = "SampleData",
                    Description = "Operation bills and admission charges for the 2026 sample were added on this date.", IsSystem = true, ModifiedBy = By, ModifiedDate = DateTime.Now
                });
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                _logger.LogInformation("2026 sample data: {Ops} operation bills added, {Ipd} admission bills completed with medicines and investigations", ops.Count, ipdBills.Count);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Adding the 2026 sample charges failed; nothing was saved.");
            }
        }

        public const string TpaMarkerKey = "SampleData:Year2026:Tpa";

        /// <summary>TPA providers (if there are none) and two insurance claims a month for 2026 – once.</summary>
        private async Task SeedTpaClaimsOnceAsync()
        {
            if (await _db.Settings.AnyAsync(s => s.Key == TpaMarkerKey)) return;
            var patients = await _db.Patients.Where(p => p.IsActive).OrderBy(p => p.Id).ToListAsync();
            if (patients.Count < 5) return;

            var providers = await _db.TpaProviders.Where(p => p.IsActive).ToListAsync();
            if (providers.Count == 0)
            {
                providers = new List<TpaProvider>
                {
                    new() { Name = "Jubilee Life Health TPA", Code = "JLH", ContactPerson = "Saima Rauf", ContactEmail = "claims@jubilee.example", ContactPhone = "021-32611071", Address = "Jubilee Life Building, I.I. Chundrigar Road, Karachi", TpaNetwork = "Corporate health panel", Notes = string.Empty },
                    new() { Name = "State Life Health Insurance", Code = "SLH", ContactPerson = "Kashif Mehmood", ContactEmail = "health@statelife.example", ContactPhone = "042-99203011", Address = "State Life Building, Lahore", TpaNetwork = "Sehat card panel", Notes = string.Empty },
                    new() { Name = "EFU Health TPA", Code = "EFU", ContactPerson = "Rabia Naeem", ContactEmail = "tpa@efuhealth.example", ContactPhone = "051-2870345", Address = "Blue Area, Islamabad", TpaNetwork = "Individual and family plans", Notes = string.Empty },
                };
                _db.TpaProviders.AddRange(providers);
                await _db.SaveChangesAsync();
            }

            var claimNo = await _db.TpaClaims.CountAsync();
            var count = 0;
            foreach (var month in MonthsToSeed())
            {
                for (var i = 0; i < 2; i++)
                {
                    var at = Day(month, i, 2);
                    var claimed = _rnd.Next(25, 180) * 1000m;
                    var settled = month.Month <= 7;
                    _db.TpaClaims.Add(new TpaClaim
                    {
                        TpaProviderId = Pick(providers).Id, PatientId = Pick(patients).Id, ClaimNumber = $"TPA-2026-{++claimNo:D4}",
                        ClaimedAmount = claimed, ApprovedAmount = settled || i == 0 ? Math.Round(claimed * 0.9m, 0) : null,
                        SettledAmount = settled ? Math.Round(claimed * 0.9m, 0) : null,
                        Status = settled ? "Settled" : i == 0 ? "Approved" : "Pending", ClaimDate = at,
                        SettlementDate = settled ? at.AddDays(21) : null, Remarks = string.Empty, CreatedDate = at
                    });
                    count++;
                }
            }
            _db.Settings.Add(new Setting
            {
                Key = TpaMarkerKey, Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm", Inv), Type = "string", Category = "SampleData",
                Description = "Sample insurance claims for 2026 were added on this date.", IsSystem = true, ModifiedBy = By, ModifiedDate = DateTime.Now
            });
            await _db.SaveChangesAsync();
            _logger.LogInformation("2026 sample data: {Count} insurance (TPA) claims added", count);
        }

        private async Task<int> BloodAsync(List<Patient> patients, List<DateTime> months)
        {
            var groups = new[] { "A+", "B+", "O+", "AB+", "A-", "O-" };
            var count = 0;
            foreach (var month in months)
            {
                for (var i = 0; i < 3; i++)
                {
                    var at = Day(month, i, 3);
                    _db.BloodIssues.Add(new BloodIssue
                    {
                        PatientId = Pick(patients).Id, BloodGroup = Pick(groups), UnitsIssued = _rnd.Next(1, 3), IssueDate = at,
                        RequestedBy = "Ward doctor", CrossMatchStatus = "Compatible", Notes = string.Empty, CreatedDate = at
                    });
                    count++;
                }
            }
            await _db.SaveChangesAsync();
            return count;
        }

        private async Task<int> AmbulanceAsync(List<Patient> patients, List<DateTime> months)
        {
            var vehicles = await _db.AmbulanceVehicles.ToListAsync();
            if (vehicles.Count == 0) return 0;
            var areas = new[] { "Model Town, Lahore", "Gulberg III, Lahore", "F-8 Markaz, Islamabad", "Clifton Block 5, Karachi", "Johar Town, Lahore", "G-11, Islamabad" };
            var purposes = new[] { "Emergency pick-up", "Discharge", "Transfer to another hospital", "Dialysis visit" };
            var count = 0;
            foreach (var month in months)
            {
                for (var i = 0; i < 4; i++)
                {
                    var at = Day(month, i, 4);
                    var patient = Pick(patients);
                    var km = _rnd.Next(4, 30);
                    _db.AmbulanceDispatches.Add(new AmbulanceDispatch
                    {
                        AmbulanceVehicleId = Pick(vehicles).Id, PatientId = patient.Id, PatientName = $"{patient.FirstName} {patient.LastName}",
                        PickupAddress = Pick(areas), ContactNumber = patient.Phone, Purpose = Pick(purposes), DispatchTime = at, ReturnTime = at.AddMinutes(_rnd.Next(40, 120)),
                        Status = "Completed", DistanceKm = km, Charges = 1500 + km * 60, Notes = string.Empty, CreatedDate = at
                    });
                    count++;
                }
            }
            await _db.SaveChangesAsync();
            return count;
        }

        private async Task<int> BirthsAndDeathsAsync(List<Patient> patients, List<Doctor> doctors, List<DateTime> months)
        {
            var births = await _db.BirthRecords.CountAsync();
            var deaths = await _db.DeathRecords.CountAsync();
            var babyNames = new[] { "Muhammad Ali", "Eman", "Abdullah", "Hareem", "Ibrahim", "Aleena", "Rayyan", "Zoha", "Musa", "Inaya" };
            var count = 0;
            foreach (var month in months)
            {
                var at = Day(month, 0, 1);
                var doctor = Pick(doctors);
                var name = babyNames[(month.Month - 1) % babyNames.Length];
                _db.BirthRecords.Add(new BirthRecord
                {
                    BabyName = name, Gender = (month.Month % 2 == 0) ? "Female" : "Male", DateOfBirth = at.Date, TimeOfBirth = at.ToString("HH:mm", Inv),
                    WeightKg = 2.6m + _rnd.Next(0, 13) / 10m, MotherName = $"{Pick(new[] { "Ayesha", "Sana", "Mariam", "Hira", "Nida" })} {Pick(LastNames)}",
                    FatherName = $"{Pick(new[] { "Ahmed", "Bilal", "Usman", "Imran", "Faisal" })} {Pick(LastNames)}", GuardianContact = Phone(),
                    DeliveryType = _rnd.Next(3) == 0 ? "Caesarean" : "Normal", AttendingDoctorName = $"Dr. {doctor.FirstName} {doctor.LastName}",
                    CertificateNumber = $"BC-2026-{++births:D4}", CertificateIssued = true, Notes = string.Empty, CreatedDate = at
                });
                count++;
                if (month.Month % 3 == 0)
                {
                    var patient = Pick(patients);
                    _db.DeathRecords.Add(new DeathRecord
                    {
                        PatientId = null, PatientName = PersonName(), Gender = _rnd.Next(2) == 0 ? "Male" : "Female", DateOfDeath = at.Date.AddDays(3),
                        TimeOfDeath = "04:30", CauseOfDeath = Pick(new[] { "Cardiac arrest – end-stage heart failure", "Sepsis with multi-organ failure", "Advanced metastatic cancer" }),
                        AttendingDoctorName = $"Dr. {doctor.FirstName} {doctor.LastName}", NextOfKinName = PersonName(), NextOfKinContact = Phone(),
                        CertificateNumber = $"DC-2026-{++deaths:D4}", CertificateIssued = true, Notes = string.Empty, CreatedDate = at.AddDays(3)
                    });
                    count++;
                }
            }
            await _db.SaveChangesAsync();
            return count;
        }

        // ── front office, HR, quality, equipment, online ─────────

        private async Task<int> FrontOfficeAsync(List<DateTime> months)
        {
            var purposes = new[] { "Visiting patient", "Meeting the administrator", "Delivering reports", "Job interview", "Vendor meeting" };
            var meet = new[] { "Ward 2 (General Female)", "Administration office", "Pharmacy", "HR department", "Accounts" };
            var subjects = new[] { ("Long waiting time at billing", "Waited 40 minutes to pay the bill."), ("Parking not available", "Visitors' parking was full in the evening."), ("Cleanliness of the waiting area", "Waiting area of the OPD was not clean."), ("Rude behaviour at reception", "Receptionist did not answer politely.") };
            var count = 0;
            var refNo = await _db.DispatchReceiveRecords.CountAsync();
            foreach (var month in months)
            {
                for (var i = 0; i < 8; i++)
                {
                    var at = Day(month, i, 8);
                    _db.VisitorLogs.Add(new VisitorLog
                    {
                        VisitorName = PersonName(), Phone = Phone(), Purpose = Pick(purposes), PersonToMeet = Pick(meet), VisitDate = at.Date,
                        CheckInTime = at, CheckOutTime = at.AddMinutes(_rnd.Next(20, 90)), Status = "CheckedOut", Notes = string.Empty, CreatedDate = at
                    });
                    count++;
                }
                for (var i = 0; i < 2; i++)
                {
                    var at = Day(month, i, 2);
                    var (subject, text) = Pick(subjects);
                    var resolved = month.Month < 9 || i == 0;
                    _db.ComplaintRecords.Add(new ComplaintRecord
                    {
                        ComplainantName = PersonName(), Phone = Phone(), Subject = subject, Description = text,
                        Status = resolved ? "Resolved" : "Open", ResolutionNotes = resolved ? "Apologised; staff briefed and process improved." : string.Empty,
                        CreatedDate = at, ResolvedDate = resolved ? at.AddDays(3) : null
                    });
                    count++;
                }
                for (var i = 0; i < 3; i++)
                {
                    var at = Day(month, i, 3);
                    var receive = i != 1;
                    _db.DispatchReceiveRecords.Add(new DispatchReceiveRecord
                    {
                        RecordType = receive ? "Receive" : "Dispatch", ReferenceNumber = $"{(receive ? "RCV" : "DSP")}-2026-{++refNo:D4}",
                        PartyName = receive ? Pick(new[] { "Jubilee Life Insurance", "State Life Insurance", "Punjab Healthcare Commission", "Getz Pharma" }) : Pick(new[] { "Aga Khan University Hospital", "Shifa International Hospital" }),
                        ContactNumber = Phone(), ContentSummary = receive ? Pick(new[] { "Claim settlement letter", "Inspection notice", "Medicine price list" }) : "Patient referral letter",
                        RecordDate = at, Status = "Logged", Notes = string.Empty, CreatedDate = at
                    });
                    count++;
                }
            }
            await _db.SaveChangesAsync();
            return count;
        }

        private async Task<int> HrAsync(List<DateTime> months)
        {
            var staff = await _db.Staff.Where(s => s.IsActive).OrderBy(s => s.EmployeeId).ToListAsync();
            if (staff.Count == 0) return 0;
            var count = 0;

            var leaveTypes = await _db.LeaveTypes.Where(t => t.IsActive).ToListAsync();
            var approver = (await _users.GetUsersInRoleAsync("Admin")).FirstOrDefault();
            if (leaveTypes.Count > 0)
            {
                foreach (var month in months)
                {
                    for (var i = 0; i < 2; i++)
                    {
                        var start = Day(month, i, 2).Date;
                        var days = _rnd.Next(1, 4);
                        var recent = month.Month >= 10;
                        _db.LeaveRequests.Add(new LeaveRequest
                        {
                            StaffId = Pick(staff).Id, LeaveTypeId = Pick(leaveTypes).Id, StartDate = start, EndDate = start.AddDays(days - 1), TotalDays = days,
                            Status = recent ? "Pending" : "Approved", Reason = Pick(new[] { "Family function", "Medical appointment", "Personal work", "Wedding in the family" }),
                            ApproverId = recent ? string.Empty : approver?.Id ?? string.Empty, ApprovedDate = recent ? null : start.AddDays(-2),
                            ApproverRemarks = recent ? string.Empty : "Approved", CreatedDate = start.AddDays(-4)
                        });
                        count++;
                    }
                }
            }

            // Salaries for every finished month (staff without a payroll record for that month).
            var existing = await _db.PayrollRecords.Where(p => p.PayrollMonth.Year == 2026).Select(p => new { p.StaffId, p.PayrollMonth.Month }).ToListAsync();
            var have = existing.Select(e => (e.StaffId, e.Month)).ToHashSet();
            foreach (var month in months.Where(m => m.AddMonths(1) <= DateTime.Today))
            {
                foreach (var s in staff)
                {
                    if (have.Contains((s.Id, month.Month))) continue;
                    var basic = s.Salary > 0 ? s.Salary : 60000m;
                    var allowances = Math.Round(basic * 0.1m, 0);
                    var deductions = Math.Round(basic * 0.05m, 0);
                    _db.PayrollRecords.Add(new PayrollRecord
                    {
                        StaffId = s.Id, PayrollMonth = month, BasicSalary = basic, Allowances = allowances, Deductions = deductions,
                        NetSalary = basic + allowances - deductions, Status = "Paid", PaymentDate = month.AddMonths(1).AddDays(-1 + 1),
                        Notes = string.Empty, CreatedDate = month.AddMonths(1).AddDays(-2)
                    });
                    count++;
                }
            }

            var courses = new[] { ("Basic Life Support (BLS)", "BLS / CPR", "American Heart Association (AHA)", 24), ("Infection prevention and control", "Infection control", "Hospital training unit", 12), ("Fire safety and evacuation", "Fire safety", "Rescue 1122", 12), ("Patient privacy and confidentiality", "Compliance", "Hospital training unit", 12) };
            foreach (var month in months)
            {
                for (var i = 0; i < 2; i++)
                {
                    var at = Day(month, i, 2).Date;
                    var s = Pick(staff);
                    var (title, category, provider, validMonths) = Pick(courses);
                    _db.TrainingRecords.Add(new TrainingRecord
                    {
                        StaffId = s.Id, StaffName = $"{s.FirstName} {s.LastName}", StaffDepartment = s.Department ?? string.Empty, CourseTitle = title, Category = category,
                        Provider = provider, CompletedDate = at, ExpiryDate = at.AddMonths(validMonths), CertificateNumber = $"TRN-2026-{month.Month:D2}{i + 1:D2}",
                        Result = "Pass", Notes = string.Empty, CreatedAt = at, CreatedBy = By
                    });
                    count++;
                }
            }
            await _db.SaveChangesAsync();
            return count;
        }

        private async Task<int> QualityAsync(List<int> hospitals, List<Patient> patients, List<DateTime> months)
        {
            var incidents = new[]
            {
                ("Patient fall in the ward", "Patient safety", "Medium", "Ward 2", "Patient slipped on the way to the washroom; no injury.", "Assisted back to bed; bed rails and non-slip mat checked."),
                ("Wrong label on blood sample", "Laboratory", "High", "Laboratory", "Sample received with another patient's label.", "Sample rejected and recollected; staff counselled."),
                ("Medicine given late", "Medication", "Low", "ICU", "Evening antibiotic given 2 hours late.", "Dose given; reminder added to the nursing chart."),
                ("Fridge temperature out of range", "Equipment", "Medium", "Pharmacy", "Vaccine fridge at 10 °C in the morning.", "Stock moved to the backup fridge; engineer called."),
            };
            var count = 0;
            var number = await _db.QualityIncidents.IgnoreQueryFilters().CountAsync();
            foreach (var month in months)
            {
                var h = hospitals[(month.Month - 1) % hospitals.Count];
                var at = Day(month, 0, 1);
                var (title, category, severity, location, description, action) = Pick(incidents);
                var closed = month.Month <= 7;
                var incident = new QualityIncident
                {
                    HospitalId = h, IncidentNumber = $"INC-2026-{++number:D4}", Title = title, Category = category, Severity = severity, OccurredAt = at,
                    ReportedAt = at.AddHours(1), Location = location, Description = description, ImmediateAction = action, ReportedByName = "Charge nurse",
                    Status = closed ? "Closed" : "Under investigation", RootCause = closed ? "Process step not followed; covered in refresher training." : string.Empty,
                    ClosedAt = closed ? at.AddDays(14) : null, ClosedBy = closed ? "Quality manager" : string.Empty, ClosureNotes = closed ? "Corrective action effective." : string.Empty
                };
                _db.QualityIncidents.Add(incident);
                if (month.Month % 2 == 0)
                {
                    _db.CapaActions.Add(new CapaAction
                    {
                        HospitalId = h, Incident = incident, ActionType = "Corrective", Description = "Refresher training for the unit and a checklist at handover.",
                        OwnerName = "Nursing supervisor", DueDate = at.Date.AddDays(30), Status = closed ? "Verified" : "Open",
                        CompletionNotes = closed ? "Training done; checklist in use." : string.Empty, CompletedAt = closed ? at.AddDays(20) : null,
                        VerifiedBy = closed ? "Quality manager" : string.Empty, VerifiedAt = closed ? at.AddDays(28) : null,
                        EffectivenessNotes = closed ? "No repeat in 3 months." : string.Empty, CreatedAt = at.AddDays(2), CreatedBy = By
                    });
                    count++;
                }
                count++;
            }
            await _db.SaveChangesAsync();
            return count;
        }

        private async Task<int> EquipmentAsync(List<int> hospitals, List<DateTime> months)
        {
            var items = await _db.Equipment.IgnoreQueryFilters().ToListAsync();
            var count = 0;
            if (items.Count == 0)
            {
                var catalogue = new[]
                {
                    ("Patient monitor", "Patient monitoring", "Mindray", "uMEC12", "High", 180, 365),
                    ("Defibrillator", "Life support", "Zoll", "R Series", "High", 90, 365),
                    ("Infusion pump", "Infusion", "B. Braun", "Infusomat Space", "Medium", 180, 365),
                    ("Ultrasound machine", "Imaging", "GE", "Logiq P9", "Medium", 365, 365),
                };
                var tag = 0;
                foreach (var h in hospitals)
                {
                    foreach (var (name, category, maker, model, risk, maintenance, calibration) in catalogue)
                    {
                        var bought = new DateTime(2025, 11 + tag % 2, 10 + tag % 15);
                        var e = new Equipment
                        {
                            HospitalId = h, AssetTag = $"EQ-26{++tag:D3}", Name = name, Category = category, Manufacturer = maker, Model = model,
                            SerialNumber = $"SN{_rnd.Next(100000, 999999)}", Location = category == "Imaging" ? "Radiology" : "Emergency department",
                            Supplier = "Medisafe Surgicals (Pvt) Ltd", PurchaseDate = bought, WarrantyExpiry = bought.AddYears(2), RiskClass = risk, Status = "In service",
                            MaintenanceIntervalDays = maintenance, CalibrationIntervalDays = calibration, LastMaintenanceDate = bought, NextMaintenanceDue = bought.AddDays(maintenance),
                            LastCalibrationDate = bought, NextCalibrationDue = bought.AddDays(calibration), Notes = string.Empty, CreatedAt = bought, CreatedBy = By
                        };
                        _db.Equipment.Add(e);
                        items.Add(e);
                        count++;
                    }
                }
                await _db.SaveChangesAsync();
            }

            foreach (var month in months)
            {
                var e = items[(month.Month - 1) % items.Count];
                var at = Day(month, 0, 1).Date;
                _db.EquipmentServiceRecords.Add(new EquipmentServiceRecord
                {
                    EquipmentId = e.Id, ServiceType = month.Month % 2 == 0 ? "Calibration" : "Preventive maintenance", ServiceDate = at, PerformedBy = "Biomedical engineering",
                    Result = "Pass", Cost = month.Month % 2 == 0 ? 6500 : 4000, CertificateNumber = $"SRV-2026-{month.Month:D2}", Notes = string.Empty, CreatedAt = at, CreatedBy = By
                });
                if (month.Month % 2 == 0) { e.LastCalibrationDate = at; e.NextCalibrationDue = at.AddDays(Math.Max(30, e.CalibrationIntervalDays)); }
                else { e.LastMaintenanceDate = at; e.NextMaintenanceDue = at.AddDays(Math.Max(30, e.MaintenanceIntervalDays)); }
                count++;
            }
            await _db.SaveChangesAsync();
            return count;
        }

        private async Task<int> OnlineAsync(List<Patient> patients, List<Doctor> doctors, List<DateTime> months)
        {
            var count = 0;
            foreach (var month in months)
            {
                for (var i = 0; i < 2; i++)
                {
                    var at = Day(month, i, 2, 17);
                    var patient = Pick(patients);
                    var doctor = Pick(doctors);
                    _db.LiveConsultationSessions.Add(new LiveConsultationSession
                    {
                        PatientId = patient.Id, DoctorName = $"Dr. {doctor.FirstName} {doctor.LastName}", PatientName = $"{patient.FirstName} {patient.LastName}",
                        ScheduledAt = at, DurationMinutes = 20, Platform = i == 0 ? "Zoom" : "GoogleMeet", MeetingLink = string.Empty, MeetingId = string.Empty, MeetingPassword = string.Empty,
                        Status = "Completed", Notes = "Follow-up consultation", CreatedDate = at.AddDays(-2)
                    });
                    var request = Day(month, i, 2, 11);
                    _db.PublicAppointmentRequests.Add(new PublicAppointmentRequest
                    {
                        PatientName = $"{patient.FirstName} {patient.LastName}", Phone = string.IsNullOrWhiteSpace(patient.Phone) ? Phone() : patient.Phone, Email = patient.Email,
                        Gender = patient.Gender, PatientId = patient.Id, DoctorId = doctor.Id, PreferredDate = request.Date.AddDays(3), PreferredTime = new TimeSpan(10 + i, 0, 0),
                        Symptoms = Pick(OpdCases).Symptoms, Status = "Confirmed", AdminNotes = "Appointment booked by reception", CreatedAt = request
                    });
                    count += 2;
                }
            }
            await _db.SaveChangesAsync();
            return count;
        }
    }
}
