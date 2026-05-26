using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.PurchaseOrders.Entities;
using sfa_api.Features.PurchaseOrders.Enums;
using sfa_api.Features.Distributors.Repositories;
using sfa_api.Features.PurchaseOrders.Repositories;
using sfa_api.Features.PurchaseOrders.Requests;
using sfa_api.Features.PurchaseOrders.Services;
using sfa_api.Features.UserGeoAssignments.Repositories;
using sfa_api.Features.Users.Entities;
using sfa_api.Features.Users.Repositories;
using sfa_api.Infrastructure.Locking;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.UnitTests.Features.PurchaseOrders.Services;

public class PurchaseOrderServiceTests
{
    private readonly Mock<IPurchaseOrderRepository> _repoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IUserGeoAssignmentRepository> _geoRepoMock;
    private readonly Mock<IDistributorRepository> _distributorRepoMock;
    private readonly Mock<IDistributedLockService> _lockMock;
    private readonly AppDbContext _dbContext;
    private readonly PurchaseOrderService _sut;

    public PurchaseOrderServiceTests()
    {
        _repoMock = new Mock<IPurchaseOrderRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _geoRepoMock = new Mock<IUserGeoAssignmentRepository>();
        _distributorRepoMock = new Mock<IDistributorRepository>();

        // Always grant the lock — unit tests don't test locking behaviour
        _lockMock = new Mock<IDistributedLockService>();
        _lockMock
            .Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Mock<IAsyncDisposable>().Object);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _dbContext = new AppDbContext(options);
        _dbContext.Database.OpenConnection();
        // Note: EnsureCreated() is intentionally omitted — SQLite does not support
        // sequences (used by purchase_order_number_seq). The context is only used for
        // BeginTransactionAsync in unit tests, which works without schema creation.

        _sut = new PurchaseOrderService(
            _repoMock.Object,
            _userRepoMock.Object,
            _geoRepoMock.Object,
            _distributorRepoMock.Object,
            _dbContext,
            _lockMock.Object,
            NullLogger<PurchaseOrderService>.Instance);
    }

    // ─────────────────────────────────────────────────
    // Factory helpers
    // ─────────────────────────────────────────────────

    private static PurchaseOrder CreateFakeOrder(
        int id = 1,
        PurchaseOrderStatus status = PurchaseOrderStatus.Draft,
        int distributorId = 10) => new()
    {
        Id = id,
        OrderNumber = "SO-2026-00001",
        DistributorId = distributorId,
        Status = status,
        IsActive = true,
        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Items = new List<PurchaseOrderItem>
        {
            new() { Id = 1, PurchaseOrderId = id, ProductId = 5, Quantity = 2, UnitPrice = 100m, Discount = 0m }
        },
        History = new List<PurchaseOrderHistory>()
    };

    private static User CreateFakeUser(
        int id = 1,
        UserRole role = UserRole.Distributor,
        int? distributorId = 10) => new()
    {
        Id = id,
        Role = role,
        DistributorId = distributorId,
        IsActive = true,
        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Email = "test@sfa.com",
        Name = "Test User"
    };

    private static CreatePurchaseOrderRequest CreateValidRequest(int? distributorId = null) => new()
    {
        DistributorId = distributorId,
        Notes = "Test order notes",
        Items = [new CreatePurchaseOrderItemRequest { ProductId = 5, Quantity = 2, UnitPrice = 100m, Discount = 0m }]
    };

    private static UpdatePurchaseOrderRequest CreateValidUpdateRequest() => new()
    {
        Notes = "Updated notes",
        Items = [new UpdatePurchaseOrderItemRequest { ProductId = 5, Quantity = 3, UnitPrice = 100m, Discount = 5m }]
    };

    private static RejectPurchaseOrderRequest CreateRejectRequest(string reason = "Not acceptable") => new()
    {
        Reason = reason
    };

    private void SetupOrderForCreate(int callerId, int distributorId = 10)
    {
        _repoMock.Setup(r => r.GetNextOrderNumberAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(1L);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddItemsAsync(It.IsAny<IEnumerable<PurchaseOrderItem>>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddHistoryAsync(It.IsAny<PurchaseOrderHistory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeOrder(1, PurchaseOrderStatus.Draft, distributorId));
    }

    private void SetupOrderForTransition(PurchaseOrder order)
    {
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddHistoryAsync(It.IsAny<PurchaseOrderHistory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
    }

    // ─────────────────────────────────────────────────
    // CreateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_AdminWithoutDistributorId_ThrowsValidationException()
    {
        var request = CreateValidRequest(distributorId: null);

        Func<Task> act = () => _sut.CreateAsync(request, callerId: 100, callerRole: UserRole.Admin);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_AdminWithDistributorId_ReturnsDraftOrder()
    {
        const int distributorId = 10;
        SetupOrderForCreate(callerId: 100, distributorId);

        var result = await _sut.CreateAsync(CreateValidRequest(distributorId), callerId: 100, callerRole: UserRole.Admin);

        result.Should().NotBeNull();
        result.Status.Should().Be(PurchaseOrderStatus.Draft);
        result.DistributorId.Should().Be(distributorId);
    }

    [Fact]
    public async Task CreateAsync_DistributorWithNoDistributorIdAssigned_ThrowsAuthorizationException()
    {
        const int callerId = 200;
        _userRepoMock.Setup(r => r.GetUserByIdAsync(callerId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(CreateFakeUser(id: callerId, role: UserRole.Distributor, distributorId: null));

        Func<Task> act = () => _sut.CreateAsync(CreateValidRequest(), callerId, callerRole: UserRole.Distributor);

        await act.Should().ThrowAsync<AuthorizationException>();
    }

    [Fact]
    public async Task CreateAsync_DistributorWithAssignedDistributorId_ResolvesFromUserRecordAndReturnsDraft()
    {
        const int callerId = 200;
        const int distributorId = 10;
        _userRepoMock.Setup(r => r.GetUserByIdAsync(callerId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(CreateFakeUser(id: callerId, role: UserRole.Distributor, distributorId));
        SetupOrderForCreate(callerId, distributorId);

        var result = await _sut.CreateAsync(CreateValidRequest(), callerId, callerRole: UserRole.Distributor);

        result.Should().NotBeNull();
        result.Status.Should().Be(PurchaseOrderStatus.Draft);
        result.DistributorId.Should().Be(distributorId);
    }

    // ─────────────────────────────────────────────────
    // UpdateAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_DistributorOnDraftOrder_Succeeds()
    {
        const int callerId = 200;
        const int distributorId = 10;
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.Draft, distributorId);
        SetupOrderForTransition(order);
        _userRepoMock.Setup(r => r.GetUserByIdAsync(callerId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(CreateFakeUser(id: callerId, role: UserRole.Distributor, distributorId));
        _repoMock.Setup(r => r.RemoveItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddItemsAsync(It.IsAny<IEnumerable<PurchaseOrderItem>>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(order.Id, CreateValidUpdateRequest(), callerId, UserRole.Distributor);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_DistributorOnPendingRepApproval_ThrowsBusinessRuleException()
    {
        const int callerId = 200;
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingRepApproval, distributorId: 10);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order);

        Func<Task> act = () => _sut.UpdateAsync(order.Id, CreateValidUpdateRequest(), callerId, UserRole.Distributor);

        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.ErrorCode.Should().Be("ORDER_NOT_EDITABLE");
    }

    [Fact]
    public async Task UpdateAsync_SalesRepOnPendingRepApproval_Succeeds()
    {
        const int callerId = 200;
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingRepApproval, distributorId: 10);
        SetupOrderForTransition(order);
        _repoMock.Setup(r => r.RemoveItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddItemsAsync(It.IsAny<IEnumerable<PurchaseOrderItem>>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(order.Id, CreateValidUpdateRequest(), callerId, UserRole.SalesRep);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ManagerOnPendingManagerApproval_Succeeds()
    {
        const int callerId = 300;
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingManagerApproval, distributorId: 10);
        SetupOrderForTransition(order);
        _repoMock.Setup(r => r.RemoveItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddItemsAsync(It.IsAny<IEnumerable<PurchaseOrderItem>>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(order.Id, CreateValidUpdateRequest(), callerId, UserRole.Supervisor);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_AdminOnDraftOrder_Succeeds()
    {
        const int callerId = 100;
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.Draft, distributorId: 10);
        SetupOrderForTransition(order);
        _repoMock.Setup(r => r.RemoveItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddItemsAsync(It.IsAny<IEnumerable<PurchaseOrderItem>>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(order.Id, CreateValidUpdateRequest(), callerId, UserRole.Admin);

        result.Should().NotBeNull();
    }

    // ─────────────────────────────────────────────────
    // SubmitAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task SubmitAsync_DistributorOnDraftOrder_TransitionsToPendingRepApproval()
    {
        const int callerId = 200;
        const int distributorId = 10;
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.Draft, distributorId);
        _userRepoMock.Setup(r => r.GetUserByIdAsync(callerId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(CreateFakeUser(id: callerId, role: UserRole.Distributor, distributorId));
        SetupOrderForTransition(order);
        // After update, return the updated version
        var submittedOrder = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingRepApproval, distributorId);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order)
                 .Callback(() =>
                     _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(submittedOrder));

        var result = await _sut.SubmitAsync(order.Id, callerId, UserRole.Distributor);

        result.Status.Should().Be(PurchaseOrderStatus.PendingRepApproval);
    }

    [Fact]
    public async Task SubmitAsync_ManagerRole_ThrowsAuthorizationException()
    {
        Func<Task> act = () => _sut.SubmitAsync(1, callerId: 300, callerRole: UserRole.Supervisor);

        await act.Should().ThrowAsync<AuthorizationException>();
    }

    [Fact]
    public async Task SubmitAsync_DraftOrderBelongingToDifferentDistributor_ThrowsAuthorizationException()
    {
        const int callerId = 200;
        const int orderDistributorId = 10;
        const int callerDistributorId = 99; // different distributor
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.Draft, orderDistributorId);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order);
        _userRepoMock.Setup(r => r.GetUserByIdAsync(callerId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(CreateFakeUser(id: callerId, role: UserRole.Distributor, distributorId: callerDistributorId));

        Func<Task> act = () => _sut.SubmitAsync(order.Id, callerId, UserRole.Distributor);

        await act.Should().ThrowAsync<AuthorizationException>();
    }

    [Fact]
    public async Task SubmitAsync_NonDraftOrder_ThrowsBusinessRuleException()
    {
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingRepApproval);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order);

        Func<Task> act = () => _sut.SubmitAsync(order.Id, callerId: 100, callerRole: UserRole.Admin);

        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.ErrorCode.Should().Be("ORDER_NOT_SUBMITTABLE");
    }

    // ─────────────────────────────────────────────────
    // RepApproveAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task RepApproveAsync_SalesRepOnPendingRepApproval_TransitionsToPendingManagerApproval()
    {
        const int callerId = 200;
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingRepApproval);
        var approvedOrder = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingManagerApproval);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order)
                 .Callback(() =>
                     _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(approvedOrder));
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddHistoryAsync(It.IsAny<PurchaseOrderHistory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.RepApproveAsync(order.Id, callerId, UserRole.SalesRep);

        result.Status.Should().Be(PurchaseOrderStatus.PendingManagerApproval);
    }

    [Fact]
    public async Task RepApproveAsync_DistributorRole_ThrowsAuthorizationException()
    {
        Func<Task> act = () => _sut.RepApproveAsync(1, callerId: 200, callerRole: UserRole.Distributor);

        await act.Should().ThrowAsync<AuthorizationException>();
    }

    [Fact]
    public async Task RepApproveAsync_WrongStatus_ThrowsBusinessRuleException()
    {
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingManagerApproval);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order);

        Func<Task> act = () => _sut.RepApproveAsync(order.Id, callerId: 200, callerRole: UserRole.SalesRep);

        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.ErrorCode.Should().Be("ORDER_NOT_PENDING_REP_APPROVAL");
    }

    // ─────────────────────────────────────────────────
    // ApproveAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task ApproveAsync_ManagerOnPendingManagerApproval_TransitionsToPendingDistributorFinalization()
    {
        const int callerId = 300;
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingManagerApproval);
        var approvedOrder = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingDistributorFinalization);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order)
                 .Callback(() =>
                     _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(approvedOrder));
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddHistoryAsync(It.IsAny<PurchaseOrderHistory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.ApproveAsync(order.Id, callerId, UserRole.Supervisor);

        result.Status.Should().Be(PurchaseOrderStatus.PendingDistributorFinalization);
    }

    [Fact]
    public async Task ApproveAsync_SalesRepRole_ThrowsAuthorizationException()
    {
        Func<Task> act = () => _sut.ApproveAsync(1, callerId: 200, callerRole: UserRole.SalesRep);

        await act.Should().ThrowAsync<AuthorizationException>();
    }

    // ─────────────────────────────────────────────────
    // RejectAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task RejectAsync_SalesRepRejectsPendingRepApproval_TransitionsToPendingDistributorAcknowledgement()
    {
        const int callerId = 200;
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingRepApproval);
        var pendingAckOrder = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingDistributorAcknowledgement);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order)
                 .Callback(() =>
                     _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(pendingAckOrder));
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddHistoryAsync(It.IsAny<PurchaseOrderHistory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.RejectAsync(order.Id, CreateRejectRequest(), callerId, UserRole.SalesRep);

        result.Status.Should().Be(PurchaseOrderStatus.PendingDistributorAcknowledgement);
    }

    [Fact]
    public async Task RejectAsync_ManagerRejectsPendingManagerApproval_TransitionsToPendingDistributorAcknowledgement()
    {
        const int callerId = 300;
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingManagerApproval);
        var pendingAckOrder = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingDistributorAcknowledgement);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order)
                 .Callback(() =>
                     _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(pendingAckOrder));
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddHistoryAsync(It.IsAny<PurchaseOrderHistory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.RejectAsync(order.Id, CreateRejectRequest(), callerId, UserRole.Supervisor);

        result.Status.Should().Be(PurchaseOrderStatus.PendingDistributorAcknowledgement);
    }

    [Fact]
    public async Task RejectAsync_SalesRepTriesToRejectPendingManagerApproval_ThrowsAuthorizationException()
    {
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingManagerApproval);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order);

        Func<Task> act = () => _sut.RejectAsync(order.Id, CreateRejectRequest(), callerId: 200, callerRole: UserRole.SalesRep);

        await act.Should().ThrowAsync<AuthorizationException>();
    }

    [Fact]
    public async Task RejectAsync_ManagerTriesToRejectPendingRepApproval_ThrowsAuthorizationException()
    {
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingRepApproval);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order);

        Func<Task> act = () => _sut.RejectAsync(order.Id, CreateRejectRequest(), callerId: 300, callerRole: UserRole.Supervisor);

        await act.Should().ThrowAsync<AuthorizationException>();
    }

    [Fact]
    public async Task RejectAsync_OrderInDraftStatus_ThrowsBusinessRuleException()
    {
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.Draft);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order);

        Func<Task> act = () => _sut.RejectAsync(order.Id, CreateRejectRequest(), callerId: 100, callerRole: UserRole.Admin);

        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.ErrorCode.Should().Be("ORDER_NOT_REJECTABLE");
    }

    // ─────────────────────────────────────────────────
    // AcknowledgeAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task AcknowledgeAsync_DistributorOnOwnPendingAckOrder_TransitionsToCancelled()
    {
        const int callerId = 200;
        const int distributorId = 10;
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingDistributorAcknowledgement, distributorId);
        var cancelledOrder = CreateFakeOrder(id: 1, PurchaseOrderStatus.Cancelled, distributorId);
        _userRepoMock.Setup(r => r.GetUserByIdAsync(callerId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(CreateFakeUser(id: callerId, role: UserRole.Distributor, distributorId));
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order)
                 .Callback(() =>
                     _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(cancelledOrder));
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddHistoryAsync(It.IsAny<PurchaseOrderHistory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.AcknowledgeAsync(order.Id, callerId, UserRole.Distributor);

        result.Status.Should().Be(PurchaseOrderStatus.Cancelled);
    }

    [Fact]
    public async Task AcknowledgeAsync_AdminOnPendingAckOrder_TransitionsToCancelled()
    {
        const int callerId = 100;
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingDistributorAcknowledgement);
        var cancelledOrder = CreateFakeOrder(id: 1, PurchaseOrderStatus.Cancelled);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order)
                 .Callback(() =>
                     _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(cancelledOrder));
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddHistoryAsync(It.IsAny<PurchaseOrderHistory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.AcknowledgeAsync(order.Id, callerId, UserRole.Admin);

        result.Status.Should().Be(PurchaseOrderStatus.Cancelled);
    }

    [Fact]
    public async Task AcknowledgeAsync_SalesRepRole_ThrowsAuthorizationException()
    {
        Func<Task> act = () => _sut.AcknowledgeAsync(1, callerId: 200, callerRole: UserRole.SalesRep);

        await act.Should().ThrowAsync<AuthorizationException>();
    }

    [Fact]
    public async Task AcknowledgeAsync_WrongStatus_ThrowsBusinessRuleException()
    {
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingRepApproval);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order);

        Func<Task> act = () => _sut.AcknowledgeAsync(order.Id, callerId: 100, callerRole: UserRole.Admin);

        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.ErrorCode.Should().Be("ORDER_NOT_PENDING_ACKNOWLEDGEMENT");
    }

    [Fact]
    public async Task AcknowledgeAsync_DistributorOnAnotherDistributorsOrder_ThrowsAuthorizationException()
    {
        const int callerId = 200;
        const int orderDistributorId = 10;
        const int callerDistributorId = 99;
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingDistributorAcknowledgement, orderDistributorId);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order);
        _userRepoMock.Setup(r => r.GetUserByIdAsync(callerId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(CreateFakeUser(id: callerId, role: UserRole.Distributor, distributorId: callerDistributorId));

        Func<Task> act = () => _sut.AcknowledgeAsync(order.Id, callerId, UserRole.Distributor);

        await act.Should().ThrowAsync<AuthorizationException>();
    }

    // ─────────────────────────────────────────────────
    // FinalizeAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task FinalizeAsync_DistributorOnPendingDistributorFinalization_TransitionsToFinalized()
    {
        const int callerId = 200;
        const int distributorId = 10;
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingDistributorFinalization, distributorId);
        var finalizedOrder = CreateFakeOrder(id: 1, PurchaseOrderStatus.Finalized, distributorId);
        _userRepoMock.Setup(r => r.GetUserByIdAsync(callerId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(CreateFakeUser(id: callerId, role: UserRole.Distributor, distributorId));
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order)
                 .Callback(() =>
                     _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(finalizedOrder));
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddHistoryAsync(It.IsAny<PurchaseOrderHistory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.FinalizeAsync(order.Id, callerId, UserRole.Distributor);

        result.Status.Should().Be(PurchaseOrderStatus.Finalized);
    }

    [Fact]
    public async Task FinalizeAsync_SalesRepRole_ThrowsAuthorizationException()
    {
        Func<Task> act = () => _sut.FinalizeAsync(1, callerId: 200, callerRole: UserRole.SalesRep);

        await act.Should().ThrowAsync<AuthorizationException>();
    }

    [Fact]
    public async Task FinalizeAsync_WrongStatus_ThrowsBusinessRuleException()
    {
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingManagerApproval);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order);

        Func<Task> act = () => _sut.FinalizeAsync(order.Id, callerId: 100, callerRole: UserRole.Admin);

        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.ErrorCode.Should().Be("ORDER_NOT_PENDING_FINALIZATION");
    }

    // ─────────────────────────────────────────────────
    // CancelAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task CancelAsync_DistributorOnDraftOrder_TransitionsToCancelled()
    {
        const int callerId = 200;
        const int distributorId = 10;
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.Draft, distributorId);
        var cancelledOrder = CreateFakeOrder(id: 1, PurchaseOrderStatus.Cancelled, distributorId);
        _userRepoMock.Setup(r => r.GetUserByIdAsync(callerId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(CreateFakeUser(id: callerId, role: UserRole.Distributor, distributorId));
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order)
                 .Callback(() =>
                     _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(cancelledOrder));
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddHistoryAsync(It.IsAny<PurchaseOrderHistory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.CancelAsync(order.Id, CreateRejectRequest(), callerId, UserRole.Distributor);

        result.Status.Should().Be(PurchaseOrderStatus.Cancelled);
    }

    [Fact]
    public async Task CancelAsync_DistributorOnPendingRepApprovalOrder_ThrowsBusinessRuleException()
    {
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingRepApproval);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order);

        Func<Task> act = () => _sut.CancelAsync(order.Id, CreateRejectRequest(), callerId: 200, callerRole: UserRole.Distributor);

        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.ErrorCode.Should().Be("ORDER_NOT_CANCELLABLE");
    }

    [Fact]
    public async Task CancelAsync_AdminOnPendingRepApprovalOrder_TransitionsToCancelled()
    {
        const int callerId = 100;
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.PendingRepApproval);
        var cancelledOrder = CreateFakeOrder(id: 1, PurchaseOrderStatus.Cancelled);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order)
                 .Callback(() =>
                     _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(cancelledOrder));
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddHistoryAsync(It.IsAny<PurchaseOrderHistory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.CancelAsync(order.Id, CreateRejectRequest(), callerId, UserRole.Admin);

        result.Status.Should().Be(PurchaseOrderStatus.Cancelled);
    }

    [Fact]
    public async Task CancelAsync_AlreadyFinalizedOrder_ThrowsBusinessRuleException()
    {
        var order = CreateFakeOrder(id: 1, PurchaseOrderStatus.Finalized);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order);

        Func<Task> act = () => _sut.CancelAsync(order.Id, CreateRejectRequest(), callerId: 100, callerRole: UserRole.Admin);

        var ex = await act.Should().ThrowAsync<BusinessRuleException>();
        ex.Which.ErrorCode.Should().Be("ORDER_NOT_CANCELLABLE");
    }

    // ─────────────────────────────────────────────────
    // GetByIdAsync
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_DistributorAccessingOwnOrder_ReturnsOrder()
    {
        const int callerId = 200;
        const int distributorId = 10;
        var order = CreateFakeOrder(id: 1, distributorId: distributorId);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order);
        _userRepoMock.Setup(r => r.GetUserByIdAsync(callerId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(CreateFakeUser(id: callerId, role: UserRole.Distributor, distributorId));

        var result = await _sut.GetByIdAsync(order.Id, callerId, UserRole.Distributor);

        result.Should().NotBeNull();
        result.Id.Should().Be(order.Id);
    }

    [Fact]
    public async Task GetByIdAsync_DistributorAccessingOtherDistributorsOrder_ThrowsAuthorizationException()
    {
        const int callerId = 200;
        const int orderDistributorId = 10;
        const int callerDistributorId = 99; // different distributor
        var order = CreateFakeOrder(id: 1, distributorId: orderDistributorId);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order);
        _userRepoMock.Setup(r => r.GetUserByIdAsync(callerId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(CreateFakeUser(id: callerId, role: UserRole.Distributor, distributorId: callerDistributorId));

        Func<Task> act = () => _sut.GetByIdAsync(order.Id, callerId, UserRole.Distributor);

        await act.Should().ThrowAsync<AuthorizationException>();
    }

    [Fact]
    public async Task GetByIdAsync_OrderNotFound_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(999, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((PurchaseOrder?)null);

        Func<Task> act = () => _sut.GetByIdAsync(999, callerId: 100, callerRole: UserRole.Admin);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
