class DistributorStockModel {
  final int productId;
  final String stockType;
  final double quantityOnHand;
  final String lastUpdatedAt;

  /// Denormalized from the distributor server-side. Nullable: the distributor may
  /// have no fleet, and a deactivated fleet resolves to a null name with the id set.
  final int? fleetId;
  final String? fleetName;

  const DistributorStockModel({
    required this.productId,
    required this.stockType,
    required this.quantityOnHand,
    required this.lastUpdatedAt,
    this.fleetId,
    this.fleetName,
  });

  factory DistributorStockModel.fromJson(Map<String, dynamic> json) =>
      DistributorStockModel(
        productId: json['productId'] as int,
        stockType: json['stockType'] as String,
        quantityOnHand: (json['quantityOnHand'] as num).toDouble(),
        lastUpdatedAt: json['lastUpdatedAt'] as String,
        fleetId: json['fleetId'] as int?,
        fleetName: json['fleetName'] as String?,
      );

  factory DistributorStockModel.fromMap(Map<String, dynamic> map) =>
      DistributorStockModel(
        productId: map['product_id'] as int,
        stockType: map['stock_type'] as String,
        quantityOnHand: (map['quantity_on_hand'] as num).toDouble(),
        lastUpdatedAt: map['last_updated_at'] as String,
        fleetId: map['fleet_id'] as int?,
        fleetName: map['fleet_name'] as String?,
      );

  Map<String, dynamic> toMap() => {
        'product_id': productId,
        'stock_type': stockType,
        'quantity_on_hand': quantityOnHand,
        'last_updated_at': lastUpdatedAt,
        'fleet_id': fleetId,
        'fleet_name': fleetName,
      };
}
