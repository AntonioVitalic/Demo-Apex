namespace InvoiceManager.Api.Domain;

public class Invoice
{
    public int InvoiceNumber { get; set; }

    public DateOnly InvoiceDate { get; set; }

    public int TotalAmount { get; set; }

    public int DaysToDue { get; set; }

    public DateOnly PaymentDueDate { get; set; }

    // Stored for consistency checks/reporting
    public int ProductsSubtotalSum { get; set; }
    public int DiscrepancyAmount { get; set; }
    public bool IsConsistent { get; set; }

    // Payment (if present => Paid)
    public string? PaymentMethod { get; set; }
    public DateOnly? PaymentDate { get; set; }

    // Customer
    public string CustomerRun { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;

    // Navigation
    public List<InvoiceDetail> Details { get; set; } = new();
    public List<CreditNote> CreditNotes { get; set; } = new();
}
