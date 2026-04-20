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
  const ProductAdded(this.product, this.quantity);
  @override
  List<Object?> get props => [product, quantity];
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
