using sfa_api.Features.SalesTargets.Enums;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.SalesTargets.Entities;

public class SalesTargetImportBatch
{
    public int Id { get; set; }
    public string BatchNumber { get; set; } = string.Empty;   // STG-2026-00001
    public string FileName    { get; set; } = string.Empty;

    public int Year  { get; set; }
    public int Month { get; set; }

    public int TotalRows    { get; set; }
    public int InsertedRows { get; set; }
    public int UpdatedRows  { get; set; }
    public int SkippedRows  { get; set; }

    public SalesTargetImportBatchStatus Status { get; set; } = SalesTargetImportBatchStatus.Processing;
    public string? ErrorSummary { get; set; }   // JSON: [{ rowIndex, repsCode, itemCode, reason }]

    public int      ImportedBy { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive  { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Navigation
    public User? Importer { get; set; }
}
