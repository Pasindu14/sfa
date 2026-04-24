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
  final String billingItemType; // 'Sale' | 'Return'
  final String? returnType;     // 'Damage' | 'Expire' | 'MarketResell'
  final DateTime? expireDate;   // Only when returnType == 'Expire'

  const CartLine({
    required this.lineNumber,
    required this.product,
    required this.quantity,
    required this.unitPrice,
    this.discountRate = 0,
    this.isFreeIssue = false,
    this.billingItemType = 'Sale',
    this.returnType,
    this.expireDate,
  });

  double get lineTotal {
    if (isFreeIssue) return 0;
    final gross = quantity * unitPrice;
    final disc = gross * discountRate / 100.0;
    return gross - disc;
  }

  bool get isReturn => billingItemType == 'Return';

  CartLine copyWith({
    double? quantity,
    double? unitPrice,
    double? discountRate,
    String? billingItemType,
    String? returnType,
    bool clearReturnType = false,
    DateTime? expireDate,
    bool clearExpireDate = false,
  }) =>
      CartLine(
        lineNumber: lineNumber,
        product: product,
        quantity: quantity ?? this.quantity,
        unitPrice: unitPrice ?? this.unitPrice,
        discountRate: discountRate ?? this.discountRate,
        isFreeIssue: isFreeIssue,
        billingItemType: billingItemType ?? this.billingItemType,
        returnType: clearReturnType ? null : (returnType ?? this.returnType),
        expireDate: clearExpireDate ? null : (expireDate ?? this.expireDate),
      );

  @override
  List<Object?> get props => [
        lineNumber,
        product.id,
        quantity,
        unitPrice,
        discountRate,
        isFreeIssue,
        billingItemType,
        returnType,
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

  double get saleSubTotal => cart.where((l) => !l.isReturn).fold<double>(0, (s, l) => s + l.lineTotal);
  double get returnTotal  => cart.where((l) =>  l.isReturn).fold<double>(0, (s, l) => s + l.lineTotal);
  double get billDiscountAmount => saleSubTotal * billDiscountRate / 100.0;
  double get total => saleSubTotal - billDiscountAmount - returnTotal;
  bool   get hasReturns => cart.any((l) => l.isReturn);

  bool get canSubmit =>
      outlet != null &&
      selectedPricingStructure != null &&
      cart.isNotEmpty &&
      cart.every((l) => !l.isReturn || l.returnType != null) &&
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
