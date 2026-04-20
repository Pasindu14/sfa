import 'package:equatable/equatable.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/outlets/domain/entities/outlet.dart';
import 'package:uswatte/features/pricing/domain/entities/pricing_structure.dart';

/// In-memory cart line during Create Bill editing.
class CartLine extends Equatable {
  final int lineNumber;
  final ProductWithPrice product;
  final double quantity;
  final double unitPrice;
  final double discountRate;
  final bool isFreeIssue;

  const CartLine({
    required this.lineNumber,
    required this.product,
    required this.quantity,
    required this.unitPrice,
    this.discountRate = 0,
    this.isFreeIssue = false,
  });

  double get lineTotal {
    if (isFreeIssue) return 0;
    final gross = quantity * unitPrice;
    final disc = gross * discountRate / 100.0;
    return gross - disc;
  }

  CartLine copyWith({double? quantity, double? unitPrice}) => CartLine(
        lineNumber: lineNumber,
        product: product,
        quantity: quantity ?? this.quantity,
        unitPrice: unitPrice ?? this.unitPrice,
        discountRate: discountRate,
        isFreeIssue: isFreeIssue,
      );

  @override
  List<Object?> get props =>
      [lineNumber, product.id, quantity, unitPrice, discountRate, isFreeIssue];
}

class CreateBillState extends Equatable {
  final Outlet? outlet;
  final List<PricingStructure> pricingStructures;
  final PricingStructure? selectedPricingStructure;
  final List<CartLine> cart;
  final double billDiscountRate;
  final bool submitting;
  final String? errorMessage;
  final String? submittedClientBillId;

  const CreateBillState({
    this.outlet,
    this.pricingStructures = const [],
    this.selectedPricingStructure,
    this.cart = const [],
    this.billDiscountRate = 0,
    this.submitting = false,
    this.errorMessage,
    this.submittedClientBillId,
  });

  double get subTotal => cart.fold<double>(0, (s, l) => s + l.lineTotal);
  double get billDiscountAmount => subTotal * billDiscountRate / 100.0;
  double get total => subTotal - billDiscountAmount;

  bool get canSubmit =>
      outlet != null &&
      selectedPricingStructure != null &&
      cart.isNotEmpty &&
      !submitting;

  CreateBillState copyWith({
    Outlet? outlet,
    List<PricingStructure>? pricingStructures,
    PricingStructure? selectedPricingStructure,
    bool clearSelectedStructure = false,
    List<CartLine>? cart,
    double? billDiscountRate,
    bool? submitting,
    String? errorMessage,
    String? submittedClientBillId,
    bool clearError = false,
  }) =>
      CreateBillState(
        outlet: outlet ?? this.outlet,
        pricingStructures: pricingStructures ?? this.pricingStructures,
        selectedPricingStructure: clearSelectedStructure
            ? null
            : (selectedPricingStructure ?? this.selectedPricingStructure),
        cart: cart ?? this.cart,
        billDiscountRate: billDiscountRate ?? this.billDiscountRate,
        submitting: submitting ?? this.submitting,
        errorMessage: clearError ? null : (errorMessage ?? this.errorMessage),
        submittedClientBillId:
            submittedClientBillId ?? this.submittedClientBillId,
      );

  @override
  List<Object?> get props => [
        outlet?.id,
        pricingStructures,
        selectedPricingStructure?.id,
        cart,
        billDiscountRate,
        submitting,
        errorMessage,
        submittedClientBillId,
      ];
}
