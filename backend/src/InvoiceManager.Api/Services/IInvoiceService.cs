using InvoiceManager.Api.DTOs;

namespace InvoiceManager.Api.Services;

public interface IInvoiceService
{
    Task<IReadOnlyList<InvoiceListItemDto>> SearchAsync(
        int? invoiceNumber,
        string? invoiceStatus,
        string? paymentStatus,
        CancellationToken ct = default);

    Task<InvoiceDto?> GetByNumberAsync(int invoiceNumber, CancellationToken ct = default);

    Task<CreditNoteDto> AddCreditNoteAsync(int invoiceNumber, AddCreditNoteRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<OverdueNoActionDto>> GetOverdueNoActionAsync(CancellationToken ct = default);

    Task<PaymentStatusSummaryDto> GetPaymentStatusSummaryAsync(CancellationToken ct = default);

    Task<IReadOnlyList<InconsistentInvoiceDto>> GetInconsistentAsync(CancellationToken ct = default);
}
