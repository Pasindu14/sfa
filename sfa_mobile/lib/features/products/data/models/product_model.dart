import 'package:uswatte/features/products/domain/entities/product.dart';

class ProductModel {
  final int id;
  final String code;
  final String itemDescription;
  final String? printDescription;
  final int piecesPerPack;
  final String? imageUrl;
  final int? categoryId;
  final double dealerPackPrice;
  final double dealerCasePrice;
  final double mrp;

  const ProductModel({
    required this.id,
    required this.code,
    required this.itemDescription,
    this.printDescription,
    required this.piecesPerPack,
    this.imageUrl,
    this.categoryId,
    this.dealerPackPrice = 0,
    this.dealerCasePrice = 0,
    this.mrp = 0,
  });

  /// Deserializes from the SFA API JSON (camelCase keys).
  factory ProductModel.fromJson(Map<String, dynamic> json) => ProductModel(
        id: json['id'] as int,
        code: json['code'] as String,
        itemDescription: json['itemDescription'] as String,
        printDescription: json['printDescription'] as String?,
        piecesPerPack: json['piecesPerPack'] as int,
        imageUrl: json['imageUrl'] as String?,
        categoryId: json['categoryId'] as int?,
        dealerPackPrice: (json['dealerPackPrice'] as num?)?.toDouble() ?? 0,
        dealerCasePrice: (json['dealerCasePrice'] as num?)?.toDouble() ?? 0,
        mrp: (json['mrp'] as num?)?.toDouble() ?? 0,
      );

  /// Deserializes from a SQLite row map (snake_case keys).
  factory ProductModel.fromMap(Map<String, dynamic> map) => ProductModel(
        id: map['id'] as int,
        code: map['code'] as String,
        itemDescription: map['item_description'] as String,
        printDescription: map['print_description'] as String?,
        piecesPerPack: map['pieces_per_pack'] as int,
        imageUrl: map['image_url'] as String?,
        categoryId: map['category_id'] as int?,
        dealerPackPrice: (map['dealer_pack_price'] as num?)?.toDouble() ?? 0,
        dealerCasePrice: (map['dealer_case_price'] as num?)?.toDouble() ?? 0,
        mrp: (map['mrp'] as num?)?.toDouble() ?? 0,
      );

  /// Serializes to a SQLite row map for insert/replace.
  Map<String, dynamic> toMap() => {
        'id': id,
        'code': code,
        'item_description': itemDescription,
        'print_description': printDescription,
        'pieces_per_pack': piecesPerPack,
        'image_url': imageUrl,
        'category_id': categoryId,
        'dealer_pack_price': dealerPackPrice,
        'dealer_case_price': dealerCasePrice,
        'mrp': mrp,
      };

  Product toEntity() => Product(
        id: id,
        code: code,
        itemDescription: itemDescription,
        printDescription: printDescription,
        piecesPerPack: piecesPerPack,
        imageUrl: imageUrl,
        categoryId: categoryId,
        dealerPackPrice: dealerPackPrice,
        dealerCasePrice: dealerCasePrice,
        mrp: mrp,
      );
}

/// Wrapper that matches the `data` field of the API envelope:
/// { products: [...], totalCount: N, cachedAt: "..." }
class ProductListResponseModel {
  final List<ProductModel> products;
  final int totalCount;
  final DateTime cachedAt;

  const ProductListResponseModel({
    required this.products,
    required this.totalCount,
    required this.cachedAt,
  });

  factory ProductListResponseModel.fromJson(Map<String, dynamic> json) =>
      ProductListResponseModel(
        products: (json['products'] as List<dynamic>)
            .map((e) => ProductModel.fromJson(e as Map<String, dynamic>))
            .toList(),
        totalCount: json['totalCount'] as int,
        cachedAt: DateTime.parse(json['cachedAt'] as String),
      );
}
