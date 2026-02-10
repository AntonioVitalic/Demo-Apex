using InvoiceManager.Api.Data;
using InvoiceManager.Api.Domain;
using InvoiceManager.Api.Views;
using Microsoft.EntityFrameworkCore;

namespace InvoiceManager.Api.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _db;

    public InvoiceRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<InvoiceSearchRow>> SearchAsync(
        int? invoiceNumber,
        string? invoiceStatus,
        string? paymentStatus,
        CancellationToken ct = default)
    {
        IQueryable<InvoiceSearchRow> query = _db.InvoiceSearchRows.AsNoTracking();

        if (invoiceNumber.HasValue)
            query = query.Where(r => r.InvoiceNumber == invoiceNumber.Value);

        if (!string.IsNullOrWhiteSpace(invoiceStatus))
        {
            var normalized = invoiceStatus.Trim().ToLowerInvariant();
            query = query.Where(r => r.InvoiceStatus.ToLower() == normalized);
        }

        if (!string.IsNullOrWhiteSpace(paymentStatus))
        {
            var normalized = paymentStatus.Trim().ToLowerInvariant();
            query = query.Where(r => r.PaymentStatus.ToLower() == normalized);
        }

        return await query
            .OrderByDescending(r => r.InvoiceDate)
            .ToListAsync(ct);
    }

    public async Task<Invoice?> GetConsistentByNumberAsync(int invoiceNumber, bool track = false, CancellationToken ct = default)
    {
        IQueryable<Invoice> query = _db.Invoices
            .Include(i => i.Details)
            .Include(i => i.CreditNotes)
            .Where(i => i.InvoiceNumber == invoiceNumber && i.IsConsistent);

        if (!track)
            query = query.AsNoTracking();

        var invoice = await query.FirstOrDefaultAsync(ct);

        if (invoice is null) return null;

        // Orden consistente (útil para UI/serialización)
        invoice.Details = invoice.Details
            .OrderBy(d => d.Id)
            .ToList();

        invoice.CreditNotes = invoice.CreditNotes
            .OrderBy(cn => cn.CreditNoteNumber)
            .ToList();

        return invoice;
    }

    public async Task<int> GetNextCreditNoteNumberAsync(int invoiceNumber, CancellationToken ct = default)
    {
        var max = await _db.CreditNotes
            .Where(cn => cn.InvoiceNumber == invoiceNumber)
            .MaxAsync(cn => (int?)cn.CreditNoteNumber, ct);

        return max.HasValue ? (max.Value + 1) : 10_000;
    }

    public async Task AddCreditNoteAsync(CreditNote creditNote, CancellationToken ct = default)
    {
        _db.CreditNotes.Add(creditNote);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<OverdueNoActionRow>> GetOverdueNoActionAsync(CancellationToken ct = default)
    {
        return await _db.OverdueNoActionRows
            .AsNoTracking()
            .OrderByDescending(r => r.DaysOverdue)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<InconsistentInvoiceRow>> GetInconsistentAsync(CancellationToken ct = default)
    {
        return await _db.InconsistentInvoiceRows
            .AsNoTracking()
            .OrderByDescending(r => r.DiscrepancyAmount)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PaymentStatusCountRow>> GetPaymentStatusCountsAsync(CancellationToken ct = default)
    {
        return await _db.InvoiceSearchRows
            .AsNoTracking()
            .GroupBy(r => r.PaymentStatus)
            .Select(g => new PaymentStatusCountRow(g.Key, g.Count()))
            .OrderBy(r => r.PaymentStatus)
            .ToListAsync(ct);
    }

    public async Task UpsertFromImportAsync(Invoice invoice, CancellationToken ct = default)
    {
        // Tracking query (vamos a modificar)
        var existing = await _db.Invoices
            .Include(i => i.Details)
            .Include(i => i.CreditNotes)
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoice.InvoiceNumber, ct);

        if (existing is null)
        {
            // Inserción completa (EF insertará navegación)
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync(ct);
            return;
        }

        // Update scalar fields
        existing.InvoiceDate = invoice.InvoiceDate;
        existing.TotalAmount = invoice.TotalAmount;
        existing.DaysToDue = invoice.DaysToDue;
        existing.PaymentDueDate = invoice.PaymentDueDate;

        existing.ProductsSubtotalSum = invoice.ProductsSubtotalSum;
        existing.DiscrepancyAmount = invoice.DiscrepancyAmount;
        existing.IsConsistent = invoice.IsConsistent;

        existing.PaymentMethod = invoice.PaymentMethod;
        existing.PaymentDate = invoice.PaymentDate;

        existing.CustomerRun = invoice.CustomerRun;
        existing.CustomerName = invoice.CustomerName;
        existing.CustomerEmail = invoice.CustomerEmail;

        // Replace details (import is source of truth)
        if (existing.Details.Count > 0)
            _db.InvoiceDetails.RemoveRange(existing.Details);

        existing.Details = invoice.Details.Select(d => new InvoiceDetail
        {
            InvoiceNumber = existing.InvoiceNumber,
            ProductName = d.ProductName,
            UnitPrice = d.UnitPrice,
            Quantity = d.Quantity,
            Subtotal = d.Subtotal
        }).ToList();

        // Merge credit notes:
        // - Keep user-created notes
        // - Upsert notes coming from import by CreditNoteNumber
        foreach (var importedCn in invoice.CreditNotes)
        {
            var match = existing.CreditNotes.FirstOrDefault(cn => cn.CreditNoteNumber == importedCn.CreditNoteNumber);

            if (match is null)
            {
                existing.CreditNotes.Add(new CreditNote
                {
                    InvoiceNumber = existing.InvoiceNumber,
                    CreditNoteNumber = importedCn.CreditNoteNumber,
                    CreditNoteDate = importedCn.CreditNoteDate,
                    Amount = importedCn.Amount
                });
            }
            else
            {
                match.CreditNoteDate = importedCn.CreditNoteDate;
                match.Amount = importedCn.Amount;
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
