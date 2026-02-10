using System.Text.Json;
using System.Text.Json.Serialization;
using InvoiceManager.Api.Domain;
using InvoiceManager.Api.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceManager.Api.Services;

public sealed class JsonInvoiceImportHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JsonInvoiceImportHostedService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _env;

    public JsonInvoiceImportHostedService(
        IServiceProvider serviceProvider,
        ILogger<JsonInvoiceImportHostedService> logger,
        IConfiguration configuration,
        IHostEnvironment env)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        _env = env;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var runOnStartup = _configuration.GetValue<bool?>("Import:RunOnStartup") ?? true;
        if (!runOnStartup)
        {
            _logger.LogInformation("JSON import disabled (Import:RunOnStartup=false).");
            return;
        }

        var jsonPath = _configuration.GetValue<string>("Import:JsonFilePath") ?? "../../bd_exam.json";
        var resolvedPath = ResolvePath(jsonPath);

        if (!File.Exists(resolvedPath))
        {
            _logger.LogWarning("Import file not found: {Path}", resolvedPath);
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(resolvedPath, cancellationToken);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var root = JsonSerializer.Deserialize<InvoiceRoot>(json, options);
            var invoices = root?.Invoices ?? new List<InvoiceJson>();

            if (invoices.Count == 0)
            {
                _logger.LogInformation("No invoices found in import file: {Path}", resolvedPath);
                return;
            }

            var dup = invoices
                .GroupBy(i => i.InvoiceNumber)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (dup.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Duplicate invoice_number in import file: {string.Join(", ", dup)}");
            }

            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();

            var imported = 0;
            var inconsistent = 0;

            foreach (var inv in invoices)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var entity = MapToEntity(inv);

                if (!entity.IsConsistent)
                    inconsistent++;

                await repo.UpsertFromImportAsync(entity, cancellationToken);
                imported++;
            }

            _logger.LogInformation(
                "Imported {Imported} invoices from {Path}. Inconsistent: {Inconsistent}.",
                imported,
                resolvedPath,
                inconsistent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing invoices from JSON file: {Path}", resolvedPath);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
            return path;

        // resolve relative to content root (backend/src/InvoiceManager.Api)
        return Path.GetFullPath(Path.Combine(_env.ContentRootPath, path));
    }

    private static Invoice MapToEntity(InvoiceJson inv)
    {
        var invoiceDate = DateOnly.Parse(inv.InvoiceDate);
        var paymentDueDate = DateOnly.Parse(inv.PaymentDueDate);

        var paymentMethod = inv.InvoicePayment?.PaymentMethod;
        DateOnly? paymentDate = null;
        if (!string.IsNullOrWhiteSpace(inv.InvoicePayment?.PaymentDate))
            paymentDate = DateOnly.Parse(inv.InvoicePayment!.PaymentDate!);

        var details = (inv.InvoiceDetail ?? new List<InvoiceDetailJson>())
            .Select(d => new InvoiceDetail
            {
                InvoiceNumber = inv.InvoiceNumber,
                ProductName = d.ProductName ?? string.Empty,
                UnitPrice = d.UnitPrice,
                Quantity = d.Quantity,
                Subtotal = d.Subtotal
            })
            .ToList();

        var productsSubtotalSum = details.Sum(d => d.Subtotal);
        var discrepancyAmount = inv.TotalAmount - productsSubtotalSum;
        var isConsistent = discrepancyAmount == 0;

        var creditNotes = (inv.InvoiceCreditNote ?? new List<CreditNoteJson>())
            .Select(cn => new CreditNote
            {
                InvoiceNumber = inv.InvoiceNumber,
                CreditNoteNumber = cn.CreditNoteNumber,
                CreditNoteDate = DateOnly.Parse(cn.CreditNoteDate),
                Amount = cn.CreditNoteAmount
            })
            .ToList();

        return new Invoice
        {
            InvoiceNumber = inv.InvoiceNumber,
            InvoiceDate = invoiceDate,
            TotalAmount = inv.TotalAmount,
            DaysToDue = inv.DaysToDue,
            PaymentDueDate = paymentDueDate,

            ProductsSubtotalSum = productsSubtotalSum,
            DiscrepancyAmount = discrepancyAmount,
            IsConsistent = isConsistent,

            PaymentMethod = paymentMethod,
            PaymentDate = paymentDate,

            CustomerRun = inv.Customer?.CustomerRun ?? string.Empty,
            CustomerName = inv.Customer?.CustomerName ?? string.Empty,
            CustomerEmail = inv.Customer?.CustomerEmail ?? string.Empty,

            Details = details,
            CreditNotes = creditNotes
        };
    }

    private sealed class InvoiceRoot
    {
        [JsonPropertyName("invoices")]
        public List<InvoiceJson> Invoices { get; set; } = new();
    }

    private sealed class InvoiceJson
    {
        [JsonPropertyName("invoice_number")]
        public int InvoiceNumber { get; set; }

        [JsonPropertyName("invoice_date")]
        public string InvoiceDate { get; set; } = string.Empty;

        [JsonPropertyName("total_amount")]
        public int TotalAmount { get; set; }

        [JsonPropertyName("days_to_due")]
        public int DaysToDue { get; set; }

        [JsonPropertyName("payment_due_date")]
        public string PaymentDueDate { get; set; } = string.Empty;

        [JsonPropertyName("invoice_detail")]
        public List<InvoiceDetailJson>? InvoiceDetail { get; set; }

        [JsonPropertyName("invoice_payment")]
        public InvoicePaymentJson? InvoicePayment { get; set; }

        [JsonPropertyName("invoice_credit_note")]
        public List<CreditNoteJson>? InvoiceCreditNote { get; set; }

        [JsonPropertyName("customer")]
        public CustomerJson? Customer { get; set; }
    }

    private sealed class InvoiceDetailJson
    {
        [JsonPropertyName("product_name")]
        public string? ProductName { get; set; }

        [JsonPropertyName("unit_price")]
        public int UnitPrice { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("subtotal")]
        public int Subtotal { get; set; }
    }

    private sealed class InvoicePaymentJson
    {
        [JsonPropertyName("payment_method")]
        public string? PaymentMethod { get; set; }

        [JsonPropertyName("payment_date")]
        public string? PaymentDate { get; set; }
    }

    private sealed class CreditNoteJson
    {
        [JsonPropertyName("credit_note_number")]
        public int CreditNoteNumber { get; set; }

        [JsonPropertyName("credit_note_date")]
        public string CreditNoteDate { get; set; } = string.Empty;

        [JsonPropertyName("credit_note_amount")]
        public int CreditNoteAmount { get; set; }
    }

    private sealed class CustomerJson
    {
        [JsonPropertyName("customer_run")]
        public string? CustomerRun { get; set; }

        [JsonPropertyName("customer_name")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("customer_email")]
        public string? CustomerEmail { get; set; }
    }
}
