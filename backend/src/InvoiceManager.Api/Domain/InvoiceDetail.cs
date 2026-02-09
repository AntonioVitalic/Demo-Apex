namespace InvoiceManager.Api.Domain;

public class InvoiceDetail
{
    public int Id { get; set; }

    public int InvoiceNumber { get; set; }
    public Invoice? Invoice { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public int UnitPrice { get; set; }
    public int Quantity { get; set; }
    public int Subtotal { get; set; }
}
