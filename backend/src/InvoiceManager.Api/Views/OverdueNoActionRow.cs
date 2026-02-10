namespace InvoiceManager.Api.Views;

public class OverdueNoActionRow
{
    public int InvoiceNumber { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public int TotalAmount { get; set; }
    public DateOnly PaymentDueDate { get; set; }
    public int DaysOverdue { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerRun { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
}
