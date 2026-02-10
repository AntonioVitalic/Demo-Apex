using InvoiceManager.Api.DTOs;
using InvoiceManager.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceManager.Api.Controllers;

[ApiController]
[Route("api/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _service;

    public InvoicesController(IInvoiceService service)
    {
        _service = service;
    }

    // GET /api/invoices?invoiceNumber=&invoiceStatus=&paymentStatus=
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] int? invoiceNumber,
        [FromQuery] string? invoiceStatus,
        [FromQuery] string? paymentStatus,
        CancellationToken ct)
    {
        try
        {
            var results = await _service.SearchAsync(invoiceNumber, invoiceStatus, paymentStatus, ct);
            return Ok(results);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // GET /api/invoices/{invoiceNumber}
    [HttpGet("{invoiceNumber:int}")]
    public async Task<IActionResult> GetByNumber([FromRoute] int invoiceNumber, CancellationToken ct)
    {
        var invoice = await _service.GetByNumberAsync(invoiceNumber, ct);
        if (invoice is null)
            return NotFound(new { error = "Invoice not found." });

        return Ok(invoice);
    }

    // POST /api/invoices/{invoiceNumber}/credit-notes
    [HttpPost("{invoiceNumber:int}/credit-notes")]
    public async Task<IActionResult> AddCreditNote(
        [FromRoute] int invoiceNumber,
        [FromBody] AddCreditNoteRequest request,
        CancellationToken ct)
    {
        try
        {
            var created = await _service.AddCreditNoteAsync(invoiceNumber, request, ct);
            return Ok(created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            // Invoice not found / inconsistent
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Business rule violation (exceed remaining balance, no remaining balance, etc.)
            return Conflict(new { error = ex.Message });
        }
    }
}
