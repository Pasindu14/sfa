using sfa_api.Features.SalesInvoices.Enums;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.SalesInvoices.Entities;

public class SalesInvoiceImportBatch
{
    public int Id { get; set; }
    public string BatchNumber { get; set; } = string.Empty;   // IMP-2026-00001 (auto-generated)
    public string FileName { get; set; } = string.Empty;
    public int TotalInvoices { get; set; }
    public int TotalItems { get; set; }
    public decimal TotalAmount { get; set; }
    public SalesInvoiceImportBatchStatus Status { get; set; } = SalesInvoiceImportBatchStatus.Processing;
    public string? ErrorSummary { get; set; }   // JSON array of skipped rows + reasons
    public int ImportedBy { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Navigation
    public User? Importer { get; set; }
}
