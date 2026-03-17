using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Features.SalesOrders.Entities;
using sfa_api.Features.SalesOrders.Enums;
using sfa_api.Features.SalesOrders.Repositories;
using sfa_api.Features.SalesOrders.Requests;
using sfa_api.Features.SalesOrders.Services;
using sfa_api.Features.Users.Entities;
using sfa_api.Features.Users.Repositories;

namespace sfa_api.UnitTests.Features.SalesOrders.Services;

public class SalesOrderServiceTests
{
    private readonly Mock<ISalesOrderRepository> _repoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly SalesOrderService _sut;

    public SalesOrderServiceTests()
    {
        _repoMock = new Mock<ISalesOrderRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _sut = new SalesOrderService(
            _repoMock.Object,
            _userRepoMock.Object,
            NullLogger<SalesOrderService>.Instance);
    }

    // ─────────────────────────────────────────────────
    // Factory helpers
    // ─────────────────────────────────────────────────

    private static SalesOrder CreateFakeOrder(
        int id = 1,
        SalesOrderStatus status = SalesOrderStatus.Draft,
        int distributorId = 10) => new()
    {
        Id = id,
        OrderNumber = "SO-2026-00001",
        DistributorId = distributorId,
        Status = status,
        IsActive = true,
        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Items = new List<SalesOrderItem>
        {
            new() { Id = 1, SalesOrderId = id, ProductId = 5, Quantity = 2, UnitPrice = 100m, Discount = 0m }
        },
        History = new List<SalesOrderHistory>()
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

    private static CreateSalesOrderRequest CreateValidRequest(int? distributorId = null) => new()
    {
        DistributorId = distributorId,
        Notes = "Test order notes",
        Items = [new CreateSalesOrderItemRequest { ProductId = 5, Quantity = 2, UnitPrice = 100m, Discount = 0m }]
    };

    private static UpdateSalesOrderRequest CreateValidUpdateRequest() => new()
    {
        Notes = "Updated notes",
        Items = [new UpdateSalesOrderItemRequest { ProductId = 5, Quantity = 3, UnitPrice = 100m, Discount = 5m }]
    };

    private static RejectSalesOrderRequest CreateRejectRequest(string reason = "Not acceptable") => new()
    {
        Reason = reason
    };

    private void SetupOrderForCreate(int callerId, int distributorId = 10)
    {
        _repoMock.Setup(r => r.GetNextOrderNumberAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(1L);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<SalesOrder>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddItemsAsync(It.IsAny<IEnumerable<SalesOrderItem>>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddHistoryAsync(It.IsAny<SalesOrderHistory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CreateFakeOrder(1, SalesOrderStatus.Draft, distributorId));
    }

    private void SetupOrderForTransition(SalesOrder order)
    {
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<SalesOrder>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddHistoryAsync(It.IsAny<SalesOrderHistory>(), It.IsAny<CancellationToken>()))
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
        result.Status.Should().Be(SalesOrderStatus.Draft);
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
        result.Status.Should().Be(SalesOrderStatus.Draft);
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
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.Draft, distributorId);
        SetupOrderForTransition(order);
        _userRepoMock.Setup(r => r.GetUserByIdAsync(callerId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(CreateFakeUser(id: callerId, role: UserRole.Distributor, distributorId));
        _repoMock.Setup(r => r.RemoveItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddItemsAsync(It.IsAny<IEnumerable<SalesOrderItem>>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(order.Id, CreateValidUpdateRequest(), callerId, UserRole.Distributor);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_DistributorOnPendingRepApproval_ThrowsBusinessRuleException()
    {
        const int callerId = 200;
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.PendingRepApproval, distributorId: 10);
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
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.PendingRepApproval, distributorId: 10);
        SetupOrderForTransition(order);
        _repoMock.Setup(r => r.RemoveItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddItemsAsync(It.IsAny<IEnumerable<SalesOrderItem>>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(order.Id, CreateValidUpdateRequest(), callerId, UserRole.SalesRep);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ManagerOnPendingManagerApproval_Succeeds()
    {
        const int callerId = 300;
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.PendingManagerApproval, distributorId: 10);
        SetupOrderForTransition(order);
        _repoMock.Setup(r => r.RemoveItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddItemsAsync(It.IsAny<IEnumerable<SalesOrderItem>>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(order.Id, CreateValidUpdateRequest(), callerId, UserRole.Manager);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_AdminOnDraftOrder_Succeeds()
    {
        const int callerId = 100;
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.Draft, distributorId: 10);
        SetupOrderForTransition(order);
        _repoMock.Setup(r => r.RemoveItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddItemsAsync(It.IsAny<IEnumerable<SalesOrderItem>>(), It.IsAny<CancellationToken>()))
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
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.Draft, distributorId);
        _userRepoMock.Setup(r => r.GetUserByIdAsync(callerId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(CreateFakeUser(id: callerId, role: UserRole.Distributor, distributorId));
        SetupOrderForTransition(order);
        // After update, return the updated version
        var submittedOrder = CreateFakeOrder(id: 1, SalesOrderStatus.PendingRepApproval, distributorId);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order)
                 .Callback(() =>
                     _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(submittedOrder));

        var result = await _sut.SubmitAsync(order.Id, callerId, UserRole.Distributor);

        result.Status.Should().Be(SalesOrderStatus.PendingRepApproval);
    }

    [Fact]
    public async Task SubmitAsync_ManagerRole_ThrowsAuthorizationException()
    {
        Func<Task> act = () => _sut.SubmitAsync(1, callerId: 300, callerRole: UserRole.Manager);

        await act.Should().ThrowAsync<AuthorizationException>();
    }

    [Fact]
    public async Task SubmitAsync_DraftOrderBelongingToDifferentDistributor_ThrowsAuthorizationException()
    {
        const int callerId = 200;
        const int orderDistributorId = 10;
        const int callerDistributorId = 99; // different distributor
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.Draft, orderDistributorId);
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
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.PendingRepApproval);
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
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.PendingRepApproval);
        var approvedOrder = CreateFakeOrder(id: 1, SalesOrderStatus.PendingManagerApproval);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order)
                 .Callback(() =>
                     _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(approvedOrder));
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<SalesOrder>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddHistoryAsync(It.IsAny<SalesOrderHistory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.RepApproveAsync(order.Id, callerId, UserRole.SalesRep);

        result.Status.Should().Be(SalesOrderStatus.PendingManagerApproval);
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
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.PendingManagerApproval);
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
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.PendingManagerApproval);
        var approvedOrder = CreateFakeOrder(id: 1, SalesOrderStatus.PendingDistributorFinalization);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order)
                 .Callback(() =>
                     _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(approvedOrder));
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<SalesOrder>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddHistoryAsync(It.IsAny<SalesOrderHistory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.ApproveAsync(order.Id, callerId, UserRole.Manager);

        result.Status.Should().Be(SalesOrderStatus.PendingDistributorFinalization);
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
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.PendingRepApproval);
        var pendingAckOrder = CreateFakeOrder(id: 1, SalesOrderStatus.PendingDistributorAcknowledgement);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order)
                 .Callback(() =>
                     _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(pendingAckOrder));
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<SalesOrder>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddHistoryAsync(It.IsAny<SalesOrderHistory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.RejectAsync(order.Id, CreateRejectRequest(), callerId, UserRole.SalesRep);

        result.Status.Should().Be(SalesOrderStatus.PendingDistributorAcknowledgement);
    }

    [Fact]
    public async Task RejectAsync_ManagerRejectsPendingManagerApproval_TransitionsToPendingDistributorAcknowledgement()
    {
        const int callerId = 300;
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.PendingManagerApproval);
        var pendingAckOrder = CreateFakeOrder(id: 1, SalesOrderStatus.PendingDistributorAcknowledgement);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order)
                 .Callback(() =>
                     _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(pendingAckOrder));
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<SalesOrder>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddHistoryAsync(It.IsAny<SalesOrderHistory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.RejectAsync(order.Id, CreateRejectRequest(), callerId, UserRole.Manager);

        result.Status.Should().Be(SalesOrderStatus.PendingDistributorAcknowledgement);
    }

    [Fact]
    public async Task RejectAsync_SalesRepTriesToRejectPendingManagerApproval_ThrowsAuthorizationException()
    {
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.PendingManagerApproval);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order);

        Func<Task> act = () => _sut.RejectAsync(order.Id, CreateRejectRequest(), callerId: 200, callerRole: UserRole.SalesRep);

        await act.Should().ThrowAsync<AuthorizationException>();
    }

    [Fact]
    public async Task RejectAsync_ManagerTriesToRejectPendingRepApproval_ThrowsAuthorizationException()
    {
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.PendingRepApproval);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order);

        Func<Task> act = () => _sut.RejectAsync(order.Id, CreateRejectRequest(), callerId: 300, callerRole: UserRole.Manager);

        await act.Should().ThrowAsync<AuthorizationException>();
    }

    [Fact]
    public async Task RejectAsync_OrderInDraftStatus_ThrowsBusinessRuleException()
    {
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.Draft);
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
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.PendingDistributorAcknowledgement, distributorId);
        var cancelledOrder = CreateFakeOrder(id: 1, SalesOrderStatus.Cancelled, distributorId);
        _userRepoMock.Setup(r => r.GetUserByIdAsync(callerId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(CreateFakeUser(id: callerId, role: UserRole.Distributor, distributorId));
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order)
                 .Callback(() =>
                     _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(cancelledOrder));
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<SalesOrder>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddHistoryAsync(It.IsAny<SalesOrderHistory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.AcknowledgeAsync(order.Id, callerId, UserRole.Distributor);

        result.Status.Should().Be(SalesOrderStatus.Cancelled);
    }

    [Fact]
    public async Task AcknowledgeAsync_AdminOnPendingAckOrder_TransitionsToCancelled()
    {
        const int callerId = 100;
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.PendingDistributorAcknowledgement);
        var cancelledOrder = CreateFakeOrder(id: 1, SalesOrderStatus.Cancelled);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order)
                 .Callback(() =>
                     _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(cancelledOrder));
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<SalesOrder>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddHistoryAsync(It.IsAny<SalesOrderHistory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.AcknowledgeAsync(order.Id, callerId, UserRole.Admin);

        result.Status.Should().Be(SalesOrderStatus.Cancelled);
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
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.PendingRepApproval);
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
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.PendingDistributorAcknowledgement, orderDistributorId);
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
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.PendingDistributorFinalization, distributorId);
        var finalizedOrder = CreateFakeOrder(id: 1, SalesOrderStatus.Finalized, distributorId);
        _userRepoMock.Setup(r => r.GetUserByIdAsync(callerId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(CreateFakeUser(id: callerId, role: UserRole.Distributor, distributorId));
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order)
                 .Callback(() =>
                     _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(finalizedOrder));
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<SalesOrder>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddHistoryAsync(It.IsAny<SalesOrderHistory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.FinalizeAsync(order.Id, callerId, UserRole.Distributor);

        result.Status.Should().Be(SalesOrderStatus.Finalized);
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
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.PendingManagerApproval);
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
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.Draft, distributorId);
        var cancelledOrder = CreateFakeOrder(id: 1, SalesOrderStatus.Cancelled, distributorId);
        _userRepoMock.Setup(r => r.GetUserByIdAsync(callerId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(CreateFakeUser(id: callerId, role: UserRole.Distributor, distributorId));
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order)
                 .Callback(() =>
                     _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(cancelledOrder));
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<SalesOrder>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddHistoryAsync(It.IsAny<SalesOrderHistory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.CancelAsync(order.Id, CreateRejectRequest(), callerId, UserRole.Distributor);

        result.Status.Should().Be(SalesOrderStatus.Cancelled);
    }

    [Fact]
    public async Task CancelAsync_DistributorOnPendingRepApprovalOrder_ThrowsBusinessRuleException()
    {
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.PendingRepApproval);
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
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.PendingRepApproval);
        var cancelledOrder = CreateFakeOrder(id: 1, SalesOrderStatus.Cancelled);
        _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(order)
                 .Callback(() =>
                     _repoMock.Setup(r => r.GetByIdWithItemsAsync(order.Id, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(cancelledOrder));
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<SalesOrder>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddHistoryAsync(It.IsAny<SalesOrderHistory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var result = await _sut.CancelAsync(order.Id, CreateRejectRequest(), callerId, UserRole.Admin);

        result.Status.Should().Be(SalesOrderStatus.Cancelled);
    }

    [Fact]
    public async Task CancelAsync_AlreadyFinalizedOrder_ThrowsBusinessRuleException()
    {
        var order = CreateFakeOrder(id: 1, SalesOrderStatus.Finalized);
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
                 .ReturnsAsync((SalesOrder?)null);

        Func<Task> act = () => _sut.GetByIdAsync(999, callerId: 100, callerRole: UserRole.Admin);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
