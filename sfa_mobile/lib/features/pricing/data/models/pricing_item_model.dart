import 'package:uswatte/features/pricing/domain/entities/pricing_item.dart';

class PricingItemModel {
  final int id;
  final int pricingStructureId;
  final int productId;
  final String productCode;
  final String productItemDescription;
  final double? dealerPackPrice;
  final double? dealerCasePrice;
  final double? promotionalPrice;

  const PricingItemModel({
    required this.id,
    required this.pricingStructureId,
    required this.productId,
    required this.productCode,
    required this.productItemDescription,
    this.dealerPackPrice,
    this.dealerCasePrice,
    this.promotionalPrice,
  });

  factory PricingItemModel.fromJson(Map<String, dynamic> json) =>
      PricingItemModel(
        id: json['id'] as int,
        pricingStructureId: json['pricingStructureId'] as int,
        productId: json['productId'] as int,
        productCode: json['productCode'] as String,
        productItemDescription: json['productItemDescription'] as String,
        dealerPackPrice: (json['dealerPackPrice'] as num?)?.toDouble(),
        dealerCasePrice: (json['dealerCasePrice'] as num?)?.toDouble(),
        promotionalPrice: (json['promotionalPrice'] as num?)?.toDouble(),
      );

  factory PricingItemModel.fromMap(Map<String, dynamic> map) =>
      PricingItemModel(
        id: map['id'] as int,
        pricingStructureId: map['pricing_structure_id'] as int,
        productId: map['product_id'] as int,
        productCode: map['product_code'] as String,
        productItemDescription: map['product_item_description'] as String,
        dealerPackPrice: map['dealer_pack_price'] as double?,
        dealerCasePrice: map['dealer_case_price'] as double?,
        promotionalPrice: map['promotional_price'] as double?,
      );

  Map<String, dynamic> toMap() => {
        'id': id,
        'pricing_structure_id': pricingStructureId,
        'product_id': productId,
        'product_code': productCode,
        'product_item_description': productItemDescription,
        'dealer_pack_price': dealerPackPrice,
        'dealer_case_price': dealerCasePrice,
        'promotional_price': promotionalPrice,
      };

  PricingItem toEntity() => PricingItem(
        id: id,
        pricingStructureId: pricingStructureId,
        productId: productId,
        productCode: productCode,
        productItemDescription: productItemDescription,
        dealerPackPrice: dealerPackPrice,
        dealerCasePrice: dealerCasePrice,
        promotionalPrice: promotionalPrice,
      );
}
