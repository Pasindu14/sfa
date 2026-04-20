import 'package:uswatte/features/pricing/data/models/pricing_item_model.dart';
import 'package:uswatte/features/pricing/domain/entities/pricing_structure.dart';

class PricingStructureModel {
  final int id;
  final String name;
  final bool isDefault;
  final List<PricingItemModel> items;

  const PricingStructureModel({
    required this.id,
    required this.name,
    required this.isDefault,
    required this.items,
  });

  /// Parses a PricingStructureDetailDto from the API (camelCase).
  factory PricingStructureModel.fromJson(Map<String, dynamic> json) =>
      PricingStructureModel(
        id: json['id'] as int,
        name: json['name'] as String,
        isDefault: json['isDefault'] as bool,
        items: (json['items'] as List<dynamic>)
            .map((e) => PricingItemModel.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

  /// Reads a structure row from SQLite (no items — loaded separately).
  factory PricingStructureModel.fromMap(
    Map<String, dynamic> map,
    List<PricingItemModel> items,
  ) =>
      PricingStructureModel(
        id: map['id'] as int,
        name: map['name'] as String,
        isDefault: (map['is_default'] as int) == 1,
        items: items,
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
        items: items.map((i) => i.toEntity()).toList(),
      );
}
