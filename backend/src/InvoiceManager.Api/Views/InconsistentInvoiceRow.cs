namespace InvoiceManager.Api.Views;

public class InconsistentInvoiceRow
{
    public int InvoiceNumber { get; set; }
    public DateOnly InvoiceDate { get; set; }

    public int DeclaredTotalAmount { get; set; }
    public int ComputedProductsTotal { get; set; }
    public int DiscrepancyAmount { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerRun { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
}
