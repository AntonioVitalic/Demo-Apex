using InvoiceManager.Api.DTOs;
using InvoiceManager.Api.Domain;
using InvoiceManager.Api.Repositories;

namespace InvoiceManager.Api.Services;

public sealed class InvoiceService : IInvoiceService
{
    private static readonly HashSet<string> AllowedInvoiceStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "Issued", "Partial", "Cancelled" };

    private static readonly HashSet<string> AllowedPaymentStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "Pending", "Overdue", "Paid" };

    private readonly IInvoiceRepository _repo;
    private readonly IClock _clock;
    private readonly InvoiceStatusCalculator _calculator;

    public InvoiceService(IInvoiceRepository repo, IClock clock, InvoiceStatusCalculator calculator)
    {
        _repo = repo;
        _clock = clock;
        _calculator = calculator;
    }

    public async Task<IReadOnlyList<InvoiceListItemDto>> SearchAsync(
        int? invoiceNumber,
        string? invoiceStatus,
        string? paymentStatus,
        CancellationToken ct = default)
    {
        invoiceStatus = NormalizeOrNull(invoiceStatus);
        paymentStatus = NormalizeOrNull(paymentStatus);

        if (invoiceStatus is not null && !AllowedInvoiceStatuses.Contains(invoiceStatus))
            throw new ArgumentException($"Invalid invoiceStatus. Allowed: {string.Join(", ", AllowedInvoiceStatuses)}");

        if (paymentStatus is not null && !AllowedPaymentStatuses.Contains(paymentStatus))
            throw new ArgumentException($"Invalid paymentStatus. Allowed: {string.Join(", ", AllowedPaymentStatuses)}");

        var rows = await _repo.SearchAsync(invoiceNumber, invoiceStatus, paymentStatus, ct);

        return rows.Select(r => new InvoiceListItemDto(
                r.InvoiceNumber,
                r.InvoiceDate,
                r.TotalAmount,
                r.PaymentDueDate,
                r.InvoiceStatus,
                r.PaymentStatus,
                r.CustomerName,
                r.CustomerRun,
                r.CustomerEmail,
                r.CreditNoteTotal,
                r.RemainingBalance
            ))
            .ToList();
    }

    public async Task<InvoiceDto?> GetByNumberAsync(int invoiceNumber, CancellationToken ct = default)
    {
        var invoice = await _repo.GetConsistentByNumberAsync(invoiceNumber, track: false, ct: ct);
        if (invoice is null) return null;

        var creditTotal = invoice.CreditNotes.Sum(cn => cn.Amount);
        var remaining = invoice.TotalAmount - creditTotal;

        var invoiceStatus = _calculator.CalculateInvoiceStatus(invoice);
        var paymentStatus = _calculator.CalculatePaymentStatus(invoice, _clock.Today);

        var details = invoice.Details
            .Select(d => new InvoiceDetailDto(d.ProductName, d.UnitPrice, d.Quantity, d.Subtotal))
            .ToList();

        var creditNotes = invoice.CreditNotes
            .OrderBy(cn => cn.CreditNoteNumber)
            .Select(cn => new CreditNoteDto(cn.CreditNoteNumber, cn.CreditNoteDate, cn.Amount))
            .ToList();

        return new InvoiceDto(
            invoice.InvoiceNumber,
            invoice.InvoiceDate,
            invoice.TotalAmount,
            invoice.PaymentDueDate,
            invoice.DaysToDue,
            invoiceStatus,
            paymentStatus,
            invoice.PaymentMethod,
            invoice.PaymentDate,
            invoice.CustomerName,
            invoice.CustomerRun,
            invoice.CustomerEmail,
            invoice.IsConsistent,
            invoice.ProductsSubtotalSum,
            invoice.DiscrepancyAmount,
            creditTotal,
            remaining,
            details,
            creditNotes
        );
    }

    public async Task<CreditNoteDto> AddCreditNoteAsync(int invoiceNumber, AddCreditNoteRequest request, CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than 0.");

        // Track invoice (we'll validate against current credit notes)
        var invoice = await _repo.GetConsistentByNumberAsync(invoiceNumber, track: true, ct: ct);
        if (invoice is null)
            throw new KeyNotFoundException("Invoice not found or is inconsistent.");

        var currentCreditTotal = invoice.CreditNotes.Sum(cn => cn.Amount);
        var remaining = invoice.TotalAmount - currentCreditTotal;

        if (remaining <= 0)
            throw new InvalidOperationException("Invoice has no remaining balance.");

        if (request.Amount > remaining)
            throw new InvalidOperationException("Credit note amount cannot exceed remaining balance.");

        var nextCnNumber = await _repo.GetNextCreditNoteNumberAsync(invoiceNumber, ct);

        var cn = new CreditNote
        {
            InvoiceNumber = invoiceNumber,
            CreditNoteNumber = nextCnNumber,
            CreditNoteDate = _clock.Today,
            Amount = request.Amount
        };

        await _repo.AddCreditNoteAsync(cn, ct);

        return new CreditNoteDto(cn.CreditNoteNumber, cn.CreditNoteDate, cn.Amount);
    }

    public async Task<IReadOnlyList<OverdueNoActionDto>> GetOverdueNoActionAsync(CancellationToken ct = default)
    {
        var rows = await _repo.GetOverdueNoActionAsync(ct);

        return rows.Select(r => new OverdueNoActionDto(
                r.InvoiceNumber,
                r.InvoiceDate,
                r.TotalAmount,
                r.PaymentDueDate,
                r.DaysOverdue,
                r.CustomerName,
                r.CustomerRun,
                r.CustomerEmail
            ))
            .ToList();
    }

    public async Task<PaymentStatusSummaryDto> GetPaymentStatusSummaryAsync(CancellationToken ct = default)
    {
        var rows = await _repo.GetPaymentStatusCountsAsync(ct);
        var total = rows.Sum(r => r.Count);

        var mapped = rows.Select(r =>
        {
            var percentage = total == 0 ? 0m : Math.Round((decimal)r.Count * 100m / (decimal)total, 2);
            return new PaymentStatusSummaryRowDto(r.PaymentStatus, r.Count, percentage);
        }).ToList();

        return new PaymentStatusSummaryDto(total, mapped);
    }

    public async Task<IReadOnlyList<InconsistentInvoiceDto>> GetInconsistentAsync(CancellationToken ct = default)
    {
        var rows = await _repo.GetInconsistentAsync(ct);

        return rows.Select(r => new InconsistentInvoiceDto(
                r.InvoiceNumber,
                r.InvoiceDate,
                r.DeclaredTotalAmount,
                r.ComputedProductsTotal,
                r.DiscrepancyAmount,
                r.CustomerName,
                r.CustomerRun,
                r.CustomerEmail
            ))
            .ToList();
    }

    private static string? NormalizeOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim();
    }
}
