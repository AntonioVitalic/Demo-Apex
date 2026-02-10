using Microsoft.EntityFrameworkCore;

namespace InvoiceManager.Api.Data;

public static class DbViews
{
    public static async Task EnsureViewsAsync(AppDbContext db)
    {
        // SQLite does not support CREATE VIEW IF NOT EXISTS... so we drop + create.
        var statements = new[]
        {
            "DROP VIEW IF EXISTS vw_invoice_search;",
            "DROP VIEW IF EXISTS vw_inconsistent_invoices;",
            "DROP VIEW IF EXISTS vw_report_overdue_30_no_action;",

            // Search view (consistent invoices only) + computed statuses
            @"CREATE VIEW vw_invoice_search AS
              SELECT
                i.InvoiceNumber AS InvoiceNumber,
                i.InvoiceDate AS InvoiceDate,
                i.TotalAmount AS TotalAmount,
                i.PaymentDueDate AS PaymentDueDate,
                i.PaymentDate AS PaymentDate,
                i.CustomerRun AS CustomerRun,
                i.CustomerName AS CustomerName,
                i.CustomerEmail AS CustomerEmail,
                COALESCE(cn.total_cn, 0) AS CreditNoteTotal,
                (i.TotalAmount - COALESCE(cn.total_cn, 0)) AS RemainingBalance,
                CASE
                  WHEN COALESCE(cn.total_cn, 0) = 0 THEN 'Issued'
                  WHEN COALESCE(cn.total_cn, 0) = i.TotalAmount THEN 'Cancelled'
                  WHEN COALESCE(cn.total_cn, 0) < i.TotalAmount THEN 'Partial'
                  ELSE 'Partial'
                END AS InvoiceStatus,
                CASE
                  WHEN i.PaymentDate IS NOT NULL THEN 'Paid'
                  WHEN date('now') > i.PaymentDueDate THEN 'Overdue'
                  ELSE 'Pending'
                END AS PaymentStatus
              FROM Invoices i
              LEFT JOIN (
                SELECT InvoiceNumber, SUM(Amount) AS total_cn
                FROM CreditNotes
                GROUP BY InvoiceNumber
              ) cn ON cn.InvoiceNumber = i.InvoiceNumber
              WHERE i.IsConsistent = 1;",

            // Inconsistent invoices view
            @"CREATE VIEW vw_inconsistent_invoices AS
              SELECT
                i.InvoiceNumber AS InvoiceNumber,
                i.InvoiceDate AS InvoiceDate,
                i.TotalAmount AS DeclaredTotalAmount,
                i.ProductsSubtotalSum AS ComputedProductsTotal,
                i.DiscrepancyAmount AS DiscrepancyAmount,
                i.CustomerRun AS CustomerRun,
                i.CustomerName AS CustomerName,
                i.CustomerEmail AS CustomerEmail
              FROM Invoices i
              WHERE i.IsConsistent = 0;",

            // Report: consistent invoices overdue > 30 days with no payment and no credit notes
            @"CREATE VIEW vw_report_overdue_30_no_action AS
              SELECT
                s.InvoiceNumber AS InvoiceNumber,
                s.InvoiceDate AS InvoiceDate,
                s.TotalAmount AS TotalAmount,
                s.PaymentDueDate AS PaymentDueDate,
                CAST((julianday('now') - julianday(s.PaymentDueDate)) AS INTEGER) AS DaysOverdue,
                s.CustomerName AS CustomerName,
                s.CustomerRun AS CustomerRun,
                s.CustomerEmail AS CustomerEmail
              FROM vw_invoice_search s
              WHERE s.PaymentStatus = 'Overdue'
                AND s.PaymentDate IS NULL
                AND s.CreditNoteTotal = 0
                AND (julianday('now') - julianday(s.PaymentDueDate)) > 30;"
        };

        foreach (var sql in statements)
        {
            await db.Database.ExecuteSqlRawAsync(sql);
        }
    }
}
