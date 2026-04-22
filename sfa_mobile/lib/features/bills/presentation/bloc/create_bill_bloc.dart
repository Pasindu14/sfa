import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/features/bills/domain/entities/bill.dart';
import 'package:uswatte/features/bills/domain/entities/bill_item.dart';
import 'package:uswatte/features/bills/domain/entities/sync_status.dart';
import 'package:uswatte/features/bills/domain/usecases/create_bill_usecase.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_event.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_state.dart';
import 'package:uswatte/features/pricing/data/datasources/pricing_local_datasource.dart';
import 'package:uuid/uuid.dart';

class CreateBillBloc extends Bloc<CreateBillEvent, CreateBillState> {
  final CreateBillUseCase _createBill;
  final PricingLocalDatasource _pricingLocal;
  final Uuid _uuid;

  CreateBillBloc({
    required CreateBillUseCase createBillUseCase,
    required PricingLocalDatasource pricingLocalDatasource,
    Uuid? uuid,
  })  : _createBill = createBillUseCase,
        _pricingLocal = pricingLocalDatasource,
        _uuid = uuid ?? const Uuid(),
        super(const CreateBillState()) {
    on<PricingStructuresLoaded>(_onStructuresLoaded);
    on<OutletSelected>(_onOutletSelected);
    on<PricingStructureSelected>(_onStructureSelected);
    on<ProductAdded>(_onProductAdded);
    on<CartItemQtyChanged>(_onQtyChanged);
    on<CartItemRemoved>(_onRemoved);
    on<CartItemDiscountChanged>(_onLineDiscountChanged);
    on<BillDiscountChanged>(_onDiscountChanged);
    on<SubmitPressed>(_onSubmit);
    _loadPricingStructures();
  }

  Future<void> _loadPricingStructures() async {
    try {
      final models = await _pricingLocal.getAllStructures();
      final structures = models.map((m) => m.toEntity()).toList();
      add(PricingStructuresLoaded(structures));
    } catch (_) {
      // If structures fail to load (e.g. never synced), the picker shows empty.
      add(const PricingStructuresLoaded([]));
    }
  }

  void _onStructuresLoaded(
      PricingStructuresLoaded e, Emitter<CreateBillState> emit) {
    // Auto-select the default structure so the rep rarely needs to change it.
    final defaultStructure = e.structures.where((s) => s.isDefault).firstOrNull
        ?? e.structures.firstOrNull;
    emit(state.copyWith(
      pricingStructures: e.structures,
      selectedPricingStructure: defaultStructure,
    ));
  }

  void _onOutletSelected(OutletSelected e, Emitter<CreateBillState> emit) {
    emit(state.copyWith(outlet: e.outlet, clearError: true));
  }

  void _onStructureSelected(
      PricingStructureSelected e, Emitter<CreateBillState> emit) {
    // Re-price any cart items already added using the new structure's prices.
    final repricedCart = state.cart.map((line) {
      final item = e.structure.items
          .where((i) => i.productId == line.product.id)
          .firstOrNull;
      final newPrice = item?.dealerPackPrice ?? 0.0;
      return line.copyWith(unitPrice: newPrice);
    }).toList();

    emit(state.copyWith(
      selectedPricingStructure: e.structure,
      cart: repricedCart,
    ));
  }

  void _onProductAdded(ProductAdded e, Emitter<CreateBillState> emit) {
    final existingIdx =
        state.cart.indexWhere((l) => l.product.id == e.product.id);
    if (existingIdx >= 0) {
      final existing = state.cart[existingIdx];
      final updated = [...state.cart];
      updated[existingIdx] =
          existing.copyWith(quantity: existing.quantity + e.quantity);
      emit(state.copyWith(cart: updated));
      return;
    }

    final nextLine = state.cart.length + 1;
    emit(state.copyWith(cart: [
      ...state.cart,
      CartLine(
        lineNumber: nextLine,
        product: e.product,
        quantity: e.quantity,
        unitPrice: e.unitPrice,
        discountRate: e.discountRate,
      ),
    ]));
  }

  void _onQtyChanged(CartItemQtyChanged e, Emitter<CreateBillState> emit) {
    final updated = state.cart.map((l) {
      if (l.lineNumber != e.lineNumber) return l;
      return l.copyWith(quantity: e.newQuantity);
    }).toList();
    emit(state.copyWith(cart: updated));
  }

  void _onRemoved(CartItemRemoved e, Emitter<CreateBillState> emit) {
    final filtered =
        state.cart.where((l) => l.lineNumber != e.lineNumber).toList();
    final renumbered = <CartLine>[];
    for (var i = 0; i < filtered.length; i++) {
      renumbered.add(CartLine(
        lineNumber: i + 1,
        product: filtered[i].product,
        quantity: filtered[i].quantity,
        unitPrice: filtered[i].unitPrice,
        discountRate: filtered[i].discountRate,
        isFreeIssue: filtered[i].isFreeIssue,
      ));
    }
    emit(state.copyWith(cart: renumbered));
  }

  void _onLineDiscountChanged(
      CartItemDiscountChanged e, Emitter<CreateBillState> emit) {
    final clamped = e.discountRate.clamp(0.0, 100.0);
    final updated = state.cart.map((l) {
      if (l.lineNumber != e.lineNumber) return l;
      return l.copyWith(discountRate: clamped);
    }).toList();
    emit(state.copyWith(cart: updated));
  }

  void _onDiscountChanged(
      BillDiscountChanged e, Emitter<CreateBillState> emit) {
    final clamped = e.rate.clamp(0.0, 100.0);
    emit(state.copyWith(billDiscountRate: clamped.toDouble()));
  }

  Future<void> _onSubmit(
      SubmitPressed e, Emitter<CreateBillState> emit) async {
    if (!state.canSubmit) return;
    emit(state.copyWith(submitting: true, clearError: true));

    final clientBillId = _uuid.v4();
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);

    final items = state.cart
        .map((l) => BillItem(
              clientBillId: clientBillId,
              productId: l.product.id,
              quantity: l.quantity,
              unitPrice: l.unitPrice,
              discountRate: l.discountRate,
              isFreeIssue: l.isFreeIssue,
              lineNumber: l.lineNumber,
            ))
        .toList();

    final bill = Bill(
      clientBillId: clientBillId,
      outletId: state.outlet!.id,
      billingType: 'Sale',
      billingDate: today,
      billDiscountRate: state.billDiscountRate,
      subTotalAmount: state.subTotal,
      billDiscountAmount: state.billDiscountAmount,
      totalAmount: state.total,
      createdAt: now,
      syncStatus: SyncStatus.pending,
      items: items,
    );

    try {
      await _createBill(bill);
      emit(state.copyWith(
        submitting: false,
        submittedClientBillId: clientBillId,
      ));
    } on AppException catch (ex) {
      emit(state.copyWith(submitting: false, errorMessage: ex.message));
    }
  }
}

extension<T> on Iterable<T> {
  T? get firstOrNull {
    final it = iterator;
    return it.moveNext() ? it.current : null;
  }
}
