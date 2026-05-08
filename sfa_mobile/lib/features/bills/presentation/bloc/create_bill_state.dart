import 'package:equatable/equatable.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/outlets/domain/entities/outlet.dart';
import 'package:uswatte/features/pricing/domain/entities/pricing_structure.dart';

/// In-memory cart line during Create Bill editing.
///
/// `billingItemType` is the single source of truth for line kind:
///   - 'Sale'      → contributes to subtotal; charged to outlet
///   - 'FreeIssue' → contributes to freeIssueValue (informational); not charged
///   - 'Return'    → contributes to returnTotal; credit back to distributor
class CartLine extends Equatable {
  final int lineNumber;
  final ProductWithPrice product;
  final double quantity;
  final double unitPrice;
  final double discountRate;
  final String billingItemType; // 'Sale' | 'FreeIssue' | 'Return'
  final String? returnType;     // 'Damage' | 'Expire' | 'MarketResell'
  final String? freeIssueSource; // 'Company' | 'Distributor' — only set when isFreeIssue
  final DateTime? expireDate;   // Only when returnType == 'Expire'

  const CartLine({
    required this.lineNumber,
    required this.product,
    required this.quantity,
    required this.unitPrice,
    this.discountRate = 0,
    this.billingItemType = 'Sale',
    this.returnType,
    this.freeIssueSource,
    this.expireDate,
  });

  bool get isFreeIssue => billingItemType == 'FreeIssue';
  bool get isReturn    => billingItemType == 'Return';
  bool get isSale      => billingItemType == 'Sale';

  /// Line total used for *display* and aggregation:
  /// - Sale:      qty × price × (1 − discount/100)
  /// - FreeIssue: qty × price  (informational FOC value; excluded from subtotal)
  /// - Return:    qty × price  (no discount applied to returns)
  double get lineTotal {
    if (isFreeIssue) return quantity * unitPrice;
    if (isReturn)    return quantity * unitPrice;
    final gross = quantity * unitPrice;
    final disc  = gross * discountRate / 100.0;
    return gross - disc;
  }

  CartLine copyWith({
    double? quantity,
    double? unitPrice,
    double? discountRate,
    String? billingItemType,
    String? returnType,
    bool clearReturnType = false,
    String? freeIssueSource,
    bool clearFreeIssueSource = false,
    DateTime? expireDate,
    bool clearExpireDate = false,
  }) =>
      CartLine(
        lineNumber: lineNumber,
        product: product,
        quantity: quantity ?? this.quantity,
        unitPrice: unitPrice ?? this.unitPrice,
        discountRate: discountRate ?? this.discountRate,
        billingItemType: billingItemType ?? this.billingItemType,
        returnType: clearReturnType ? null : (returnType ?? this.returnType),
        freeIssueSource: clearFreeIssueSource
            ? null
            : (freeIssueSource ?? this.freeIssueSource),
        expireDate: clearExpireDate ? null : (expireDate ?? this.expireDate),
      );

  @override
  List<Object?> get props => [
        lineNumber,
        product.id,
        quantity,
        unitPrice,
        discountRate,
        billingItemType,
        returnType,
        freeIssueSource,
        expireDate,
      ];
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
  final double? latitude;
  final double? longitude;

  const CreateBillState({
    this.outlet,
    this.pricingStructures = const [],
    this.selectedPricingStructure,
    this.cart = const [],
    this.billDiscountRate = 0,
    this.submitting = false,
    this.errorMessage,
    this.submittedClientBillId,
    this.latitude,
    this.longitude,
  });

  // Aggregates — each line type contributes to its own bucket only.
  double get saleSubTotal    => cart.where((l) => l.isSale     ).fold<double>(0, (s, l) => s + l.lineTotal);
  double get freeIssueValue  => cart.where((l) => l.isFreeIssue).fold<double>(0, (s, l) => s + l.lineTotal);
  double get freeIssueValueCompany =>
      cart.where((l) => l.isFreeIssue && l.freeIssueSource == 'Company')
          .fold<double>(0, (s, l) => s + l.lineTotal);
  double get freeIssueValueDistributor =>
      cart.where((l) => l.isFreeIssue && l.freeIssueSource == 'Distributor')
          .fold<double>(0, (s, l) => s + l.lineTotal);
  double get returnTotal     => cart.where((l) => l.isReturn   ).fold<double>(0, (s, l) => s + l.lineTotal);

  double get billDiscountAmount => saleSubTotal * billDiscountRate / 100.0;
  double get total              => saleSubTotal - billDiscountAmount - returnTotal;

  bool get hasReturns    => cart.any((l) => l.isReturn);
  bool get hasFreeIssues => cart.any((l) => l.isFreeIssue);

  bool get canSubmit =>
      outlet != null &&
      selectedPricingStructure != null &&
      cart.isNotEmpty &&
      cart.every((l) => !l.isReturn || l.returnType != null) &&
      cart.every((l) => !l.isFreeIssue || l.freeIssueSource != null) &&
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
    double? latitude,
    double? longitude,
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
        latitude: latitude ?? this.latitude,
        longitude: longitude ?? this.longitude,
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
        latitude,
        longitude,
      ];
}
