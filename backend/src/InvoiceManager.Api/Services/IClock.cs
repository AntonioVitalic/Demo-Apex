namespace InvoiceManager.Api.Services;

public interface IClock
{
    DateOnly Today { get; }
}
