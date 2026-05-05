import 'package:uswatte/features/item_wise_achievement/domain/entities/item_wise_achievement.dart';

class ItemAchievementModel extends ItemAchievement {
  const ItemAchievementModel({
    required super.productId,
    required super.itemCode,
    required super.itemName,
    required super.targetQuantity,
    required super.soldQuantity,
    required super.soldQuantityPacks,
    required super.soldAmount,
    required super.achievementPercent,
  });

  factory ItemAchievementModel.fromJson(Map<String, dynamic> json) {
    return ItemAchievementModel(
      productId:          json['productId']          as int? ?? 0,
      itemCode:           json['itemCode']           as String? ?? '',
      itemName:           json['itemName']           as String? ?? '',
      targetQuantity:    (json['targetQuantity']     as num?)?.toDouble() ?? 0.0,
      soldQuantity:      (json['soldQuantity']       as num?)?.toDouble() ?? 0.0,
      soldQuantityPacks: (json['soldQuantityPacks']  as num?)?.toDouble() ?? 0.0,
      soldAmount:        (json['soldAmount']         as num?)?.toDouble() ?? 0.0,
      achievementPercent:(json['achievementPercent'] as num?)?.toDouble() ?? 0.0,
    );
  }
}

class ItemWiseAchievementModel extends ItemWiseAchievement {
  const ItemWiseAchievementModel({
    required super.year,
    required super.month,
    required super.totalTargetQuantity,
    required super.totalSoldQuantity,
    required super.totalSoldQuantityPacks,
    required super.totalSoldAmount,
    required super.items,
  });

  factory ItemWiseAchievementModel.fromJson(Map<String, dynamic> json) {
    final rawItems = json['items'] as List<dynamic>? ?? const [];
    return ItemWiseAchievementModel(
      year:                   json['year']  as int? ?? 0,
      month:                  json['month'] as int? ?? 0,
      totalTargetQuantity:   (json['totalTargetQuantity']    as num?)?.toDouble() ?? 0.0,
      totalSoldQuantity:     (json['totalSoldQuantity']      as num?)?.toDouble() ?? 0.0,
      totalSoldQuantityPacks:(json['totalSoldQuantityPacks'] as num?)?.toDouble() ?? 0.0,
      totalSoldAmount:       (json['totalSoldAmount']        as num?)?.toDouble() ?? 0.0,
      items: rawItems
          .map((e) => ItemAchievementModel.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}
