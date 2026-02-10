using InvoiceManager.Api.Domain;
using InvoiceManager.Api.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace InvoiceManager.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceDetail> InvoiceDetails => Set<InvoiceDetail>();
    public DbSet<CreditNote> CreditNotes => Set<CreditNote>();

    // Keyless sets for SQLite views
    public DbSet<InvoiceSearchRow> InvoiceSearchRows => Set<InvoiceSearchRow>();
    public DbSet<InconsistentInvoiceRow> InconsistentInvoiceRows => Set<InconsistentInvoiceRow>();
    public DbSet<OverdueNoActionRow> OverdueNoActionRows => Set<OverdueNoActionRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dateOnlyConverter = new ValueConverter<DateOnly, string>(
            d => d.ToString("yyyy-MM-dd"),
            s => DateOnly.Parse(s));

        var nullableDateOnlyConverter = new ValueConverter<DateOnly?, string?>(
            d => d.HasValue ? d.Value.ToString("yyyy-MM-dd") : null,
            s => string.IsNullOrWhiteSpace(s) ? null : DateOnly.Parse(s));

        // Invoice
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(i => i.InvoiceNumber);
            entity.Property(i => i.InvoiceNumber).ValueGeneratedNever();

            entity.Property(i => i.InvoiceDate).HasConversion(dateOnlyConverter);
            entity.Property(i => i.PaymentDueDate).HasConversion(dateOnlyConverter);
            entity.Property(i => i.PaymentDate).HasConversion(nullableDateOnlyConverter);

            entity.Property(i => i.CustomerRun).HasMaxLength(64);
            entity.Property(i => i.CustomerName).HasMaxLength(200);
            entity.Property(i => i.CustomerEmail).HasMaxLength(200);

            entity.HasMany(i => i.Details)
                .WithOne(d => d.Invoice)
                .HasForeignKey(d => d.InvoiceNumber)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(i => i.CreditNotes)
                .WithOne(cn => cn.Invoice)
                .HasForeignKey(cn => cn.InvoiceNumber)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(i => i.IsConsistent);
        });

        // InvoiceDetail
        modelBuilder.Entity<InvoiceDetail>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.ProductName).HasMaxLength(200);
        });

        // CreditNote
        modelBuilder.Entity<CreditNote>(entity =>
        {
            entity.HasKey(cn => cn.Id);
            entity.Property(cn => cn.CreditNoteDate).HasConversion(dateOnlyConverter);

            // Unique per invoice
            entity.HasIndex(cn => new { cn.InvoiceNumber, cn.CreditNoteNumber }).IsUnique();
        });

        // Views (keyless)
        modelBuilder.Entity<InvoiceSearchRow>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_invoice_search");
            entity.Property(v => v.InvoiceDate).HasConversion(dateOnlyConverter);
            entity.Property(v => v.PaymentDueDate).HasConversion(dateOnlyConverter);
        });

        modelBuilder.Entity<InconsistentInvoiceRow>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_inconsistent_invoices");
            entity.Property(v => v.InvoiceDate).HasConversion(dateOnlyConverter);
        });

        modelBuilder.Entity<OverdueNoActionRow>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_report_overdue_30_no_action");
            entity.Property(v => v.InvoiceDate).HasConversion(dateOnlyConverter);
            entity.Property(v => v.PaymentDueDate).HasConversion(dateOnlyConverter);
        });
    }
}
