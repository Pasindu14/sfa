import 'package:equatable/equatable.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/outlets/domain/entities/outlet.dart';
import 'package:uswatte/features/pricing/domain/entities/pricing_structure.dart';

sealed class CreateBillEvent extends Equatable {
  const CreateBillEvent();

  @override
  List<Object?> get props => [];
}

final class OutletSelected extends CreateBillEvent {
  final Outlet outlet;
  const OutletSelected(this.outlet);
  @override
  List<Object?> get props => [outlet];
}

final class ProductAdded extends CreateBillEvent {
  final ProductWithPrice product;
  final double quantity;
  final double unitPrice;
  final double discountRate;
  final String billingItemType;
  final String? returnType;
  final String? freeIssueSource;
  final DateTime? expireDate;
  const ProductAdded(
    this.product,
    this.quantity, {
    required this.unitPrice,
    this.discountRate = 0,
    this.billingItemType = 'Sale',
    this.returnType,
    this.freeIssueSource,
    this.expireDate,
  });
  @override
  List<Object?> get props => [
        product,
        quantity,
        unitPrice,
        discountRate,
        billingItemType,
        returnType,
        freeIssueSource,
        expireDate,
      ];
}

final class CartItemQtyChanged extends CreateBillEvent {
  final int lineNumber;
  final double newQuantity;
  const CartItemQtyChanged(this.lineNumber, this.newQuantity);
  @override
  List<Object?> get props => [lineNumber, newQuantity];
}

final class CartItemRemoved extends CreateBillEvent {
  final int lineNumber;
  const CartItemRemoved(this.lineNumber);
  @override
  List<Object?> get props => [lineNumber];
}

final class CartItemDiscountChanged extends CreateBillEvent {
  final int lineNumber;
  final double discountRate;
  const CartItemDiscountChanged(this.lineNumber, this.discountRate);
  @override
  List<Object?> get props => [lineNumber, discountRate];
}

final class CartItemPriceChanged extends CreateBillEvent {
  final int lineNumber;
  final double unitPrice;
  const CartItemPriceChanged(this.lineNumber, this.unitPrice);
  @override
  List<Object?> get props => [lineNumber, unitPrice];
}

final class CartItemTypeChanged extends CreateBillEvent {
  final int lineNumber;
  final String billingItemType; // 'Sale' | 'FreeIssue' | 'Return'
  const CartItemTypeChanged(this.lineNumber, this.billingItemType);
  @override
  List<Object?> get props => [lineNumber, billingItemType];
}

final class CartItemReturnTypeChanged extends CreateBillEvent {
  final int lineNumber;
  final String returnType; // 'Damage' | 'Expire' | 'MarketResell'
  const CartItemReturnTypeChanged(this.lineNumber, this.returnType);
  @override
  List<Object?> get props => [lineNumber, returnType];
}

final class CartItemFreeIssueSourceChanged extends CreateBillEvent {
  final int lineNumber;
  final String source; // 'Company' | 'Distributor'
  const CartItemFreeIssueSourceChanged(this.lineNumber, this.source);
  @override
  List<Object?> get props => [lineNumber, source];
}

final class CartItemExpireDateChanged extends CreateBillEvent {
  final int lineNumber;
  final DateTime? expireDate;
  const CartItemExpireDateChanged(this.lineNumber, this.expireDate);
  @override
  List<Object?> get props => [lineNumber, expireDate];
}

final class BillDiscountChanged extends CreateBillEvent {
  final double rate;
  const BillDiscountChanged(this.rate);
  @override
  List<Object?> get props => [rate];
}

final class PricingStructureSelected extends CreateBillEvent {
  final PricingStructure structure;
  const PricingStructureSelected(this.structure);
  @override
  List<Object?> get props => [structure];
}

/// Internal event — fired by the bloc itself on init, not by the UI.
final class PricingStructuresLoaded extends CreateBillEvent {
  final List<PricingStructure> structures;
  const PricingStructuresLoaded(this.structures);
  @override
  List<Object?> get props => [structures];
}

final class SubmitPressed extends CreateBillEvent {
  const SubmitPressed();
}

/// Internal event — fired by the bloc itself after GPS resolves on init.
final class BillLocationCaptured extends CreateBillEvent {
  final double? latitude;
  final double? longitude;
  const BillLocationCaptured(this.latitude, this.longitude);
  @override
  List<Object?> get props => [latitude, longitude];
}
