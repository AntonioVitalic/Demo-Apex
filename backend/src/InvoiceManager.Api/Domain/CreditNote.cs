namespace InvoiceManager.Api.Domain;

public class CreditNote
{
    public int Id { get; set; }

    public int InvoiceNumber { get; set; }
    public Invoice? Invoice { get; set; }

    public int CreditNoteNumber { get; set; }
    public DateOnly CreditNoteDate { get; set; }
    public int Amount { get; set; }
}
