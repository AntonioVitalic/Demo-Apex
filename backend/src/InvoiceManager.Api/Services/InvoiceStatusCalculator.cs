using InvoiceManager.Api.Domain;

namespace InvoiceManager.Api.Services;

public sealed class InvoiceStatusCalculator
{
    public string CalculateInvoiceStatus(Invoice invoice)
    {
        var creditTotal = invoice.CreditNotes.Sum(cn => cn.Amount);

        if (creditTotal <= 0) return "Issued";
        if (creditTotal >= invoice.TotalAmount) return "Cancelled";
        return "Partial";
    }

    public string CalculatePaymentStatus(Invoice invoice, DateOnly today)
    {
        if (invoice.PaymentDate is not null) return "Paid";
        if (today > invoice.PaymentDueDate) return "Overdue";
        return "Pending";
    }
}
