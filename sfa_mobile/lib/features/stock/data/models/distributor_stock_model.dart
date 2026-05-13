class DistributorStockModel {
  final int productId;
  final String stockType;
  final double quantityOnHand;
  final String lastUpdatedAt;

  const DistributorStockModel({
    required this.productId,
    required this.stockType,
    required this.quantityOnHand,
    required this.lastUpdatedAt,
  });

  factory DistributorStockModel.fromJson(Map<String, dynamic> json) =>
      DistributorStockModel(
        productId: json['productId'] as int,
        stockType: json['stockType'] as String,
        quantityOnHand: (json['quantityOnHand'] as num).toDouble(),
        lastUpdatedAt: json['lastUpdatedAt'] as String,
      );

  factory DistributorStockModel.fromMap(Map<String, dynamic> map) =>
      DistributorStockModel(
        productId: map['product_id'] as int,
        stockType: map['stock_type'] as String,
        quantityOnHand: (map['quantity_on_hand'] as num).toDouble(),
        lastUpdatedAt: map['last_updated_at'] as String,
      );

  Map<String, dynamic> toMap() => {
        'product_id': productId,
        'stock_type': stockType,
        'quantity_on_hand': quantityOnHand,
        'last_updated_at': lastUpdatedAt,
      };
}
