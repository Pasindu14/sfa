using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Areas.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.IntegrationTests.Infrastructure;

/// <summary>
/// Test-only AppDbContext subclass used for both schema creation (EnsureCreated) and
/// as the runtime DI implementation in integration tests.
///
/// Applies three SQLite compatibility patches that cannot be made to AppDbContext itself:
/// 1. Removes all PostgreSQL sequences — SQLite does not support sequences.
/// 2. Rewrites Area.RowVersion to a plain INTEGER column. In PostgreSQL, RowVersion maps to
///    the system column "xmin" (type "xid"), which is auto-set by the engine so EF omits it
///    from INSERT statements. SQLite has no such system column, so the NOT NULL constraint
///    fails. ValueGeneratedNever() forces EF to include the column in INSERT (value = 0).
///    IsConcurrencyToken(false) removes the optimistic concurrency WHERE clause so updates
///    still succeed even though SQLite's "xmin" is never incremented by the engine.
/// </summary>
public class TestAppDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Remove all PostgreSQL sequences that SQLite cannot handle.
        // Each sequence is replaced by an in-process atomic counter in the corresponding test repository.
        foreach (var seqName in new[]
        {
            "purchase_order_number_seq",
            "sales_invoice_import_batch_number_seq",
            "grn_number_seq",
            "billing_number_seq",
            "not_billing_number_seq",
            "sales_target_batch_number_seq"
        })
        {
            var seq = modelBuilder.Model.FindSequence(seqName);
            if (seq != null)
                modelBuilder.Model.RemoveSequence(seq.Name, seq.Schema);
        }

        // Fix Area.RowVersion for SQLite: the base model maps it to PostgreSQL's xmin system column
        // via IsRowVersion() + HasColumnType("xid"), which makes EF exclude it from INSERT and expect
        // the database to set it automatically. SQLite can't do that — EF omits xmin from INSERT and
        // the NOT NULL constraint fires.
        //
        // Patch: change to ValueGeneratedOnAdd with DEFAULT 1. EF still omits the column from INSERT
        // so SQLite's DEFAULT 1 fills it in, and EF reads it back via RETURNING. The value 1 (not 0)
        // is important because UpdateAreaValidator rejects rowVersion == 0. Concurrency checking is
        // disabled (IsConcurrencyToken false) so updates never need to match xmin.
        modelBuilder.Entity<Area>()
            .Property(x => x.RowVersion)
            .HasColumnType("INTEGER")
            .HasDefaultValue(1u)
            .ValueGeneratedOnAdd()
            .IsConcurrencyToken(false);
    }
}
