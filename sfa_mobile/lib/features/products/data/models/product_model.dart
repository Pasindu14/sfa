import 'package:uswatte/features/products/domain/entities/product.dart';

class ProductModel {
  final int id;
  final String code;
  final String itemDescription;
  final String? printDescription;
  final int piecesPerPack;
  final String? imageUrl;

  const ProductModel({
    required this.id,
    required this.code,
    required this.itemDescription,
    this.printDescription,
    required this.piecesPerPack,
    this.imageUrl,
  });

  /// Deserializes from the SFA API JSON (camelCase keys).
  factory ProductModel.fromJson(Map<String, dynamic> json) => ProductModel(
        id: json['id'] as int,
        code: json['code'] as String,
        itemDescription: json['itemDescription'] as String,
        printDescription: json['printDescription'] as String?,
        piecesPerPack: json['piecesPerPack'] as int,
        imageUrl: json['imageUrl'] as String?,
      );

  /// Deserializes from a SQLite row map (snake_case keys).
  factory ProductModel.fromMap(Map<String, dynamic> map) => ProductModel(
        id: map['id'] as int,
        code: map['code'] as String,
        itemDescription: map['item_description'] as String,
        printDescription: map['print_description'] as String?,
        piecesPerPack: map['pieces_per_pack'] as int,
        imageUrl: map['image_url'] as String?,
      );

  /// Serializes to a SQLite row map for insert/replace.
  Map<String, dynamic> toMap() => {
        'id': id,
        'code': code,
        'item_description': itemDescription,
        'print_description': printDescription,
        'pieces_per_pack': piecesPerPack,
        'image_url': imageUrl,
      };

  Product toEntity() => Product(
        id: id,
        code: code,
        itemDescription: itemDescription,
        printDescription: printDescription,
        piecesPerPack: piecesPerPack,
        imageUrl: imageUrl,
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
