namespace InvoiceManager.Api.DTOs;

public record InvoiceListItemDto(
    int InvoiceNumber,
    DateOnly InvoiceDate,
    int TotalAmount,
    DateOnly PaymentDueDate,
    string InvoiceStatus,
    string PaymentStatus,
    string CustomerName,
    string CustomerRun,
    string CustomerEmail,
    int CreditNoteTotal,
    int RemainingBalance
);

public record InvoiceDetailDto(
    string ProductName,
    int UnitPrice,
    int Quantity,
    int Subtotal
);

public record CreditNoteDto(
    int CreditNoteNumber,
    DateOnly CreditNoteDate,
    int Amount
);

public record InvoiceDto(
    int InvoiceNumber,
    DateOnly InvoiceDate,
    int TotalAmount,
    DateOnly PaymentDueDate,
    int DaysToDue,
    string InvoiceStatus,
    string PaymentStatus,
    string? PaymentMethod,
    DateOnly? PaymentDate,
    string CustomerName,
    string CustomerRun,
    string CustomerEmail,
    bool IsConsistent,
    int ProductsSubtotalSum,
    int DiscrepancyAmount,
    int CreditNoteTotal,
    int RemainingBalance,
    IReadOnlyList<InvoiceDetailDto> Details,
    IReadOnlyList<CreditNoteDto> CreditNotes
);

public record AddCreditNoteRequest(int Amount);

public record PaymentStatusSummaryRowDto(string PaymentStatus, int Count, decimal Percentage);

public record PaymentStatusSummaryDto(int TotalInvoices, IReadOnlyList<PaymentStatusSummaryRowDto> Rows);

public record OverdueNoActionDto(
    int InvoiceNumber,
    DateOnly InvoiceDate,
    int TotalAmount,
    DateOnly PaymentDueDate,
    int DaysOverdue,
    string CustomerName,
    string CustomerRun,
    string CustomerEmail
);

public record InconsistentInvoiceDto(
    int InvoiceNumber,
    DateOnly InvoiceDate,
    int DeclaredTotalAmount,
    int ComputedProductsTotal,
    int DiscrepancyAmount,
    string CustomerName,
    string CustomerRun,
    string CustomerEmail
);
