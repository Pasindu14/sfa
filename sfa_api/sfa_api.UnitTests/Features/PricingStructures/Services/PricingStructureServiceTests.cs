using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.PricingStructures;
using sfa_api.Features.PricingStructures.DTOs;
using sfa_api.Features.PricingStructures.Entities;
using sfa_api.Features.PricingStructures.Repositories;
using sfa_api.Features.PricingStructures.Requests;
using sfa_api.Features.PricingStructures.Services;
using sfa_api.Infrastructure.Caching;
using sfa_api.Infrastructure.Locking;
using sfa_api.UnitTests.Infrastructure;

namespace sfa_api.UnitTests.Features.PricingStructures.Services;

public class PricingStructureServiceTests
{
    private readonly Mock<IPricingStructureRepository> _repoMock = new();
    private readonly Mock<IDistributedLockService> _lockMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly PricingStructureService _sut;

    public PricingStructureServiceTests()
    {
        _lockMock.Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Mock.Of<IAsyncDisposable>());
        _repoMock.Setup(r => r.CreateExecutionStrategy()).Returns(new ImmediateExecutionStrategy());
        _repoMock.Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Mock.Of<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>());
        _sut = new PricingStructureService(_repoMock.Object, _lockMock.Object, _cacheMock.Object,
            NullLogger<PricingStructureService>.Instance);
    }

    private static PricingStructure Structure(int id, bool isDefault = false, bool isActive = true, string? name = null) => new()
    {
        Id = id,
        Name = name ?? $"Structure {id}",
        IsDefault = isDefault,
        IsActive = isActive,
        RowVersion = 7
    };

    private void VerifyInvalidated()
    {
        _cacheMock.Verify(c => c.RemoveAsync(PricingStructureCacheKeys.MobileSync, It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(PricingStructureCacheKeys.MobileProducts, It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(PricingStructureCacheKeys.DefaultPrices, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WhenADefaultExists_CreatesInactiveNonDefault()
    {
        _repoMock.Setup(r => r.GetDefaultIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        PricingStructure? added = null;
        _repoMock.Setup(r => r.AddAsync(It.IsAny<PricingStructure>(), It.IsAny<CancellationToken>()))
                 .Callback<PricingStructure, CancellationToken>((s, _) => added = s);

        var result = await _sut.CreateAsync(new CreatePricingStructureRequest("  Promo  ", "Oct promo"), callerId: 5);

        added.Should().NotBeNull();
        added!.Name.Should().Be("Promo");
        added.IsActive.Should().BeFalse();
        added.IsDefault.Should().BeFalse();
        result.IsActive.Should().BeFalse();
        VerifyInvalidated();
    }

    [Fact]
    public async Task CreateAsync_FirstStructure_BecomesActiveDefault()
    {
        _repoMock.Setup(r => r.GetDefaultIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync((int?)null);
        PricingStructure? added = null;
        _repoMock.Setup(r => r.AddAsync(It.IsAny<PricingStructure>(), It.IsAny<CancellationToken>()))
                 .Callback<PricingStructure, CancellationToken>((s, _) => added = s);

        await _sut.CreateAsync(new CreatePricingStructureRequest("Standard", null), callerId: 5);

        added!.IsDefault.Should().BeTrue();
        added.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_Throws()
    {
        _repoMock.Setup(r => r.NameExistsAsync("Standard", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => _sut.CreateAsync(new CreatePricingStructureRequest("Standard", null), 5);

        await act.Should().ThrowAsync<DuplicateResourceException>();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<PricingStructure>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_AppliesClientRowVersion()
    {
        var s = Structure(3);
        _repoMock.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(s);

        await _sut.UpdateAsync(3, new UpdatePricingStructureRequest("Renamed", "d", 42), 5);

        _repoMock.Verify(r => r.ApplyConcurrencyToken(s, 42u), Times.Once);
        s.Name.Should().Be("Renamed");
    }

    [Fact]
    public async Task UpdateAsync_DeletedOrMissing_ThrowsNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync((PricingStructure?)null);

        var act = () => _sut.UpdateAsync(9, new UpdatePricingStructureRequest("x", null, 1), 5);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── Duplicate ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task DuplicateAsync_CopiesEveryItem_IntoInactiveNonDefaultCopy()
    {
        var source = Structure(1, isDefault: true);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(source);
        _repoMock.Setup(r => r.GetItemsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new PricingStructureItem { PricingStructureId = 1, ProductId = 10, DealerPackPrice = 50m, DealerCasePrice = 580m, Mrp = 60m },
            new PricingStructureItem { PricingStructureId = 1, ProductId = 11, DealerPackPrice = 20m },
            new PricingStructureItem { PricingStructureId = 1, ProductId = 12 },   // unpriced row travels too
        ]);
        PricingStructure? copy = null;
        _repoMock.Setup(r => r.AddAsync(It.IsAny<PricingStructure>(), It.IsAny<CancellationToken>()))
                 .Callback<PricingStructure, CancellationToken>((s, _) => { s.Id = 99; copy = s; });
        List<PricingStructureItem> copiedItems = [];
        _repoMock.Setup(r => r.AddItemsAsync(It.IsAny<IEnumerable<PricingStructureItem>>(), It.IsAny<CancellationToken>()))
                 .Callback<IEnumerable<PricingStructureItem>, CancellationToken>((items, _) => copiedItems.AddRange(items))
                 .Returns(Task.CompletedTask);

        var result = await _sut.DuplicateAsync(1, new DuplicatePricingStructureRequest("Standard (copy)", null), 5);

        result.Id.Should().Be(99);
        copy!.IsActive.Should().BeFalse();
        copy.IsDefault.Should().BeFalse();
        copiedItems.Should().HaveCount(3).And.OnlyContain(i => i.PricingStructureId == 99);
        copiedItems.Single(i => i.ProductId == 10).Should().BeEquivalentTo(
            new { DealerPackPrice = 50m, DealerCasePrice = 580m, Mrp = 60m }, o => o.ExcludingMissingMembers());
        source.IsDefault.Should().BeTrue("duplicating must not touch the source");
        VerifyInvalidated();
    }

    // ── Set default ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SetDefaultAsync_ClearsOldDefault_ThenSetsNew()
    {
        var current = Structure(1, isDefault: true);
        var target = Structure(2);
        _repoMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(target);
        _repoMock.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>())).ReturnsAsync(current);

        // The partial unique index allows one default at any instant: the first save must already
        // have cleared the old one.
        var saves = new List<(bool OldDefault, bool NewDefault)>();
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Callback(() => saves.Add((current.IsDefault, target.IsDefault)))
                 .Returns(Task.CompletedTask);

        var result = await _sut.SetDefaultAsync(2, new SetDefaultPricingStructureRequest(11), 5);

        result.IsDefault.Should().BeTrue();
        current.IsDefault.Should().BeFalse();
        saves.Should().Equal((false, false), (false, true));
        _repoMock.Verify(r => r.ApplyConcurrencyToken(target, 11u), Times.Once);
        VerifyInvalidated();
    }

    [Fact]
    public async Task SetDefaultAsync_InactiveTarget_Throws()
    {
        _repoMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(Structure(2, isActive: false));

        var act = () => _sut.SetDefaultAsync(2, new SetDefaultPricingStructureRequest(1), 5);

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.ErrorCode.Should().Be("PRICING_STRUCTURE_INACTIVE");
    }

    [Fact]
    public async Task SetDefaultAsync_AlreadyDefault_IsNoOp()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Structure(1, isDefault: true));

        await _sut.SetDefaultAsync(1, new SetDefaultPricingStructureRequest(1), 5);

        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetDefaultAsync_LockHeld_ThrowsConflict()
    {
        _lockMock.Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((IAsyncDisposable?)null);

        var act = () => _sut.SetDefaultAsync(2, new SetDefaultPricingStructureRequest(1), 5);

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    // ── Activate / deactivate / delete ────────────────────────────────────────

    [Fact]
    public async Task DeactivateAsync_Default_Throws()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Structure(1, isDefault: true));

        var act = () => _sut.DeactivateAsync(1, 5);

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.ErrorCode.Should().Be("PRICING_STRUCTURE_IS_DEFAULT");
    }

    [Fact]
    public async Task DeleteAsync_Default_Throws()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Structure(1, isDefault: true));

        var act = () => _sut.DeleteAsync(1, 5);

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.ErrorCode.Should().Be("PRICING_STRUCTURE_IS_DEFAULT");
    }

    [Fact]
    public async Task DeleteAsync_NonDefault_SoftDeletes()
    {
        var s = Structure(2);
        _repoMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(s);

        await _sut.DeleteAsync(2, 5);

        s.IsDeleted.Should().BeTrue();
        s.IsActive.Should().BeFalse();
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        VerifyInvalidated();
    }

    [Fact]
    public async Task ActivateAsync_Inactive_Activates()
    {
        var s = Structure(2, isActive: false);
        _repoMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(s);

        await _sut.ActivateAsync(2, 5);

        s.IsActive.Should().BeTrue();
        VerifyInvalidated();
    }

    // ── Items ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpsertItemsAsync_UpdatesExisting_AddsNewPriced_SkipsNewUnpriced()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Structure(1));
        _repoMock.Setup(r => r.GetExistingProductIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync([10, 11, 12]);
        var existing = new PricingStructureItem { PricingStructureId = 1, ProductId = 10, DealerPackPrice = 50m, DealerCasePrice = 580m };
        _repoMock.Setup(r => r.GetItemsForProductsAsync(1, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync([existing]);
        List<PricingStructureItem> added = [];
        _repoMock.Setup(r => r.AddItemsAsync(It.IsAny<IEnumerable<PricingStructureItem>>(), It.IsAny<CancellationToken>()))
                 .Callback<IEnumerable<PricingStructureItem>, CancellationToken>((items, _) => added.AddRange(items))
                 .Returns(Task.CompletedTask);

        await _sut.UpsertItemsAsync(1, new BulkUpsertPricingStructureItemsRequest(
        [
            new PricingStructureItemUpsert(10, null, null, null),     // clear → row stays, prices null
            new PricingStructureItemUpsert(11, 30m, 340m, 35m),       // new priced row
            new PricingStructureItemUpsert(12, null, null, null),     // new unpriced → nothing to store
        ]), 5);

        existing.DealerPackPrice.Should().BeNull();
        existing.DealerCasePrice.Should().BeNull();
        added.Should().ContainSingle(i => i.ProductId == 11 && i.DealerPackPrice == 30m && i.DealerCasePrice == 340m && i.Mrp == 35m);
        VerifyInvalidated();
    }

    [Fact]
    public async Task UpsertItemsAsync_UnknownProduct_ThrowsNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Structure(1));
        _repoMock.Setup(r => r.GetExistingProductIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync([10]);

        var act = () => _sut.UpsertItemsAsync(1, new BulkUpsertPricingStructureItemsRequest(
            [new PricingStructureItemUpsert(10, 1m, null, null), new PricingStructureItemUpsert(77, 1m, null, null)]), 5);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Message.Should().Contain("77");
    }

    // ── Default prices ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDefaultPricesAsync_NoDefault_ThrowsNotFound()
    {
        _repoMock.Setup(r => r.GetDefaultPricesAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync((DefaultPricingStructurePricesDto?)null);

        var act = () => _sut.GetDefaultPricesAsync();

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
