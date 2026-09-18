import 'package:uswatte/features/pricing/data/models/pricing_structure_item_model.dart';
import 'package:uswatte/features/pricing/domain/entities/pricing_structure.dart';

class PricingStructureModel {
  final int id;
  final String name;
  final bool isDefault;
  final List<PricingStructureItemModel> items;

  /// Item count as read from SQLite, where the items themselves are not loaded.
  final int? _itemCount;

  const PricingStructureModel({
    required this.id,
    required this.name,
    required this.isDefault,
    this.items = const [],
    int? itemCount,
  }) : _itemCount = itemCount;

  int get itemCount => _itemCount ?? items.length;

  factory PricingStructureModel.fromJson(Map<String, dynamic> json) {
    final id = json['id'] as int;
    return PricingStructureModel(
      id: id,
      name: json['name'] as String,
      isDefault: json['isDefault'] as bool? ?? false,
      items: ((json['items'] as List<dynamic>?) ?? const <dynamic>[])
          .map((e) => PricingStructureItemModel.fromJson(
                e as Map<String, dynamic>,
                structureId: id,
              ))
          .toList(),
    );
  }

  /// Reads a `price_structures` row, optionally with an `item_count` column.
  factory PricingStructureModel.fromMap(Map<String, dynamic> map) =>
      PricingStructureModel(
        id: map['id'] as int,
        name: map['name'] as String,
        isDefault: (map['is_default'] as int? ?? 0) == 1,
        itemCount: (map['item_count'] as num?)?.toInt() ?? 0,
      );

  Map<String, dynamic> toMap() => {
        'id': id,
        'name': name,
        'is_default': isDefault ? 1 : 0,
      };

  PricingStructure toEntity() => PricingStructure(
        id: id,
        name: name,
        isDefault: isDefault,
        itemCount: itemCount,
      );
}

/// The `data` field of `GET /api/v1/mobile/pricing-structures`:
/// { pricingStructures: [...], totalCount: N, cachedAt: "..." }
class PricingStructureListResponseModel {
  final List<PricingStructureModel> pricingStructures;
  final int totalCount;
  final DateTime cachedAt;

  const PricingStructureListResponseModel({
    required this.pricingStructures,
    required this.totalCount,
    required this.cachedAt,
  });

  factory PricingStructureListResponseModel.fromJson(
          Map<String, dynamic> json) =>
      PricingStructureListResponseModel(
        pricingStructures:
            ((json['pricingStructures'] as List<dynamic>?) ?? const <dynamic>[])
                .map((e) =>
                    PricingStructureModel.fromJson(e as Map<String, dynamic>))
                .toList(),
        totalCount: (json['totalCount'] as num?)?.toInt() ?? 0,
        cachedAt: DateTime.parse(json['cachedAt'] as String),
      );
}
