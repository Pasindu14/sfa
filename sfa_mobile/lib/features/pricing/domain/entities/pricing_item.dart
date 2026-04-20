import 'package:equatable/equatable.dart';

class PricingItem extends Equatable {
  final int id;
  final int pricingStructureId;
  final int productId;
  final String productCode;
  final String productItemDescription;
  final double? dealerPackPrice;
  final double? dealerCasePrice;
  final double? promotionalPrice;

  const PricingItem({
    required this.id,
    required this.pricingStructureId,
    required this.productId,
    required this.productCode,
    required this.productItemDescription,
    this.dealerPackPrice,
    this.dealerCasePrice,
    this.promotionalPrice,
  });

  @override
  List<Object?> get props => [id];
}
