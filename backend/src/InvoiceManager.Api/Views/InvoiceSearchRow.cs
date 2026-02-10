namespace InvoiceManager.Api.Views;

public class InvoiceSearchRow
{
    public int InvoiceNumber { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public int TotalAmount { get; set; }
    public DateOnly PaymentDueDate { get; set; }

    public string InvoiceStatus { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerRun { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;

    public int CreditNoteTotal { get; set; }
    public int RemainingBalance { get; set; }
}
