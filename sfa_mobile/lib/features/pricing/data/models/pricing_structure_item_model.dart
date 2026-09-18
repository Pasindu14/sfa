/// One product's prices inside a pricing structure. A product with no row in a
/// structure has no price there ("No price" in the bill picker).
class PricingStructureItemModel {
  final int structureId;
  final int productId;
  final double dealerPackPrice;
  final double? dealerCasePrice;
  final double? mrp;

  const PricingStructureItemModel({
    required this.structureId,
    required this.productId,
    required this.dealerPackPrice,
    this.dealerCasePrice,
    this.mrp,
  });

  /// Parses an item from `GET /mobile/pricing-structures`. The item JSON does
  /// not repeat its structure id, so the parent passes it in.
  factory PricingStructureItemModel.fromJson(
    Map<String, dynamic> json, {
    required int structureId,
  }) =>
      PricingStructureItemModel(
        structureId: structureId,
        productId: json['productId'] as int,
        dealerPackPrice: (json['dealerPackPrice'] as num).toDouble(),
        dealerCasePrice: (json['dealerCasePrice'] as num?)?.toDouble(),
        mrp: (json['mrp'] as num?)?.toDouble(),
      );

  Map<String, dynamic> toMap() => {
        'structure_id': structureId,
        'product_id': productId,
        'dealer_pack_price': dealerPackPrice,
        'dealer_case_price': dealerCasePrice,
        'mrp': mrp,
      };
}
