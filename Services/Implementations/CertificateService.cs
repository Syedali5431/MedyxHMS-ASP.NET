using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

// Purpose: Contains application code for CertificateService and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    public class CertificateService : ICertificateService
    {
        private readonly ApplicationDbContext _context;

        public CertificateService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CertificateRecord>> GetCertificatesAsync(string staffId = null)
        {
            var query = _context.CertificateRecords
                .Include(x => x.Staff)
                .ThenInclude(s => s.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(staffId))
            {
                query = query.Where(x => x.StaffId == staffId);
            }

            return await query.OrderByDescending(x => x.IssueDate).ToListAsync();
        }

        public async Task<CertificateRecord> GenerateCertificateAsync(CertificateRecord certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));

            if (string.IsNullOrWhiteSpace(certificate.StaffId))
                throw new InvalidOperationException("Staff is required.");

            certificate.IssueDate = certificate.IssueDate == default ? DateTime.UtcNow : certificate.IssueDate;
            certificate.CreatedDate = DateTime.UtcNow;

            _context.CertificateRecords.Add(certificate);
            await _context.SaveChangesAsync();
            return certificate;
        }

        public async Task<IEnumerable<IdCardRecord>> GetIdCardsAsync(string staffId = null)
        {
            var query = _context.IdCardRecords
                .Include(x => x.Staff)
                .ThenInclude(s => s.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(staffId))
            {
                query = query.Where(x => x.StaffId == staffId);
            }

            return await query.OrderByDescending(x => x.IssueDate).ToListAsync();
        }

        public async Task<IdCardRecord> GenerateIdCardAsync(IdCardRecord idCard)
        {
            if (idCard == null)
                throw new ArgumentNullException(nameof(idCard));

            if (string.IsNullOrWhiteSpace(idCard.StaffId))
                throw new InvalidOperationException("Staff is required.");

            if (string.IsNullOrWhiteSpace(idCard.CardNumber))
            {
                idCard.CardNumber = $"ID-{DateTime.UtcNow:yyyyMMddHHmmss}";
            }

            if (idCard.IssueDate == default)
                idCard.IssueDate = DateTime.UtcNow;

            _context.IdCardRecords.Add(idCard);
            await _context.SaveChangesAsync();
            return idCard;
        }
    }
}
