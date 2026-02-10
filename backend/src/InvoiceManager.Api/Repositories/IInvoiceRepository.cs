using InvoiceManager.Api.Domain;
using InvoiceManager.Api.Views;

namespace InvoiceManager.Api.Repositories;

public interface IInvoiceRepository
{
    Task<IReadOnlyList<InvoiceSearchRow>> SearchAsync(
        int? invoiceNumber,
        string? invoiceStatus,
        string? paymentStatus,
        CancellationToken ct = default
    );

    Task<Invoice?> GetConsistentByNumberAsync(
        int invoiceNumber,
        bool track = false,
        CancellationToken ct = default
    );

    Task<int> GetNextCreditNoteNumberAsync(int invoiceNumber, CancellationToken ct = default);

    Task AddCreditNoteAsync(CreditNote creditNote, CancellationToken ct = default);

    Task<IReadOnlyList<OverdueNoActionRow>> GetOverdueNoActionAsync(CancellationToken ct = default);

    Task<IReadOnlyList<InconsistentInvoiceRow>> GetInconsistentAsync(CancellationToken ct = default);

    Task<IReadOnlyList<PaymentStatusCountRow>> GetPaymentStatusCountsAsync(CancellationToken ct = default);

    /// <summary>
    /// Upsert completo de una factura desde import (reemplaza productos, mergea notas de crédito).
    /// </summary>
    Task UpsertFromImportAsync(Invoice invoice, CancellationToken ct = default);
}
