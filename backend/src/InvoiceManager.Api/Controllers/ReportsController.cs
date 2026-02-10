using InvoiceManager.Api.DTOs;
using InvoiceManager.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceManager.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IInvoiceService _service;

    public ReportsController(IInvoiceService service)
    {
        _service = service;
    }

    /// <summary>
    /// Consistent invoices with more than 30 days overdue, without payment and without credit notes.
    /// </summary>
    [HttpGet("overdue-30-no-action")]
    [ProducesResponseType(typeof(IReadOnlyList<OverdueNoActionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> OverdueNoAction(CancellationToken ct)
    {
        var rows = await _service.GetOverdueNoActionAsync(ct);
        return Ok(rows);
    }

    /// <summary>
    /// Total and percentage of active (consistent) invoices by payment status.
    /// </summary>
    [HttpGet("payment-status-summary")]
    [ProducesResponseType(typeof(PaymentStatusSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PaymentStatusSummary(CancellationToken ct)
    {
        var summary = await _service.GetPaymentStatusSummaryAsync(ct);
        return Ok(summary);
    }

    /// <summary>
    /// Inconsistent invoices where total_amount doesn't match the sum of product subtotals.
    /// </summary>
    [HttpGet("inconsistent")]
    [ProducesResponseType(typeof(IReadOnlyList<InconsistentInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Inconsistent(CancellationToken ct)
    {
        var rows = await _service.GetInconsistentAsync(ct);
        return Ok(rows);
    }
}
