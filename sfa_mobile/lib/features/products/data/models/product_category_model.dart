import 'package:uswatte/features/products/domain/entities/product_category.dart';

class ProductCategoryModel {
  final int id;
  final String name;
  final int sortOrder;

  const ProductCategoryModel({
    required this.id,
    required this.name,
    required this.sortOrder,
  });

  /// Deserializes from the SFA API JSON (camelCase keys).
  factory ProductCategoryModel.fromJson(Map<String, dynamic> json) =>
      ProductCategoryModel(
        id: json['id'] as int,
        name: json['name'] as String,
        sortOrder: json['sortOrder'] as int? ?? 0,
      );

  /// Deserializes from a SQLite row map (snake_case keys).
  factory ProductCategoryModel.fromMap(Map<String, dynamic> map) =>
      ProductCategoryModel(
        id: map['id'] as int,
        name: map['name'] as String,
        sortOrder: map['sort_order'] as int? ?? 0,
      );

  /// Serializes to a SQLite row map for insert/replace.
  Map<String, dynamic> toMap() => {
        'id': id,
        'name': name,
        'sort_order': sortOrder,
      };

  ProductCategory toEntity() => ProductCategory(
        id: id,
        name: name,
        sortOrder: sortOrder,
      );
}

/// Wrapper that matches the `data` field of the API envelope:
/// { categories: [...], totalCount: N, cachedAt: "..." }
class ProductCategoryListResponseModel {
  final List<ProductCategoryModel> categories;
  final int totalCount;
  final DateTime cachedAt;

  const ProductCategoryListResponseModel({
    required this.categories,
    required this.totalCount,
    required this.cachedAt,
  });

  factory ProductCategoryListResponseModel.fromJson(
          Map<String, dynamic> json) =>
      ProductCategoryListResponseModel(
        categories: (json['categories'] as List<dynamic>)
            .map((e) => ProductCategoryModel.fromJson(e as Map<String, dynamic>))
            .toList(),
        totalCount: json['totalCount'] as int,
        cachedAt: DateTime.parse(json['cachedAt'] as String),
      );
}
