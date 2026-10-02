import 'package:uswatte/features/item_wise_achievement/domain/entities/item_wise_achievement.dart';

double _d(Map<String, dynamic> json, String key) =>
    (json[key] as num?)?.toDouble() ?? 0.0;

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
    super.returnQuantityPacks,
    super.freeIssueQuantityPacks,
    super.pendingQuantityPacks,
    super.pendingQuantity,
    super.hasTarget,
  });

  factory ItemAchievementModel.fromJson(Map<String, dynamic> json) {
    return ItemAchievementModel(
      productId: json['productId'] as int? ?? 0,
      itemCode: json['itemCode'] as String? ?? '',
      itemName: json['itemName'] as String? ?? '',
      targetQuantity: _d(json, 'targetQuantity'),
      soldQuantity: _d(json, 'soldQuantity'),
      soldQuantityPacks: _d(json, 'soldQuantityPacks'),
      soldAmount: _d(json, 'soldAmount'),
      achievementPercent: _d(json, 'achievementPercent'),
      returnQuantityPacks: _d(json, 'returnQuantityPacks'),
      freeIssueQuantityPacks: _d(json, 'freeIssueQuantityPacks'),
      pendingQuantityPacks: _d(json, 'pendingQuantityPacks'),
      pendingQuantity: _d(json, 'pendingQuantity'),
      // Older API builds have no flag — a target above zero means it has one.
      hasTarget: json['hasTarget'] as bool?,
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
    super.totalReturnQuantityPacks,
    super.totalFreeIssueQuantityPacks,
    super.totalPendingQuantityPacks,
    super.overallAchievementPercent,
  });

  factory ItemWiseAchievementModel.fromJson(Map<String, dynamic> json) {
    final rawItems = json['items'] as List<dynamic>? ?? const [];
    final items = rawItems
        .map((e) => ItemAchievementModel.fromJson(e as Map<String, dynamic>))
        .toList();

    // Older API builds do not send the overall %; derive it from targeted items
    // only so untargeted sales never inflate it.
    var overall = _d(json, 'overallAchievementPercent');
    if (!json.containsKey('overallAchievementPercent')) {
      var sold = 0.0, target = 0.0;
      for (final i in items.where((i) => i.hasTarget)) {
        sold += i.soldQuantity;
        target += i.targetQuantity;
      }
      overall = target > 0 ? sold / target * 100 : 0.0;
    }

    return ItemWiseAchievementModel(
      year: json['year'] as int? ?? 0,
      month: json['month'] as int? ?? 0,
      totalTargetQuantity: _d(json, 'totalTargetQuantity'),
      totalSoldQuantity: _d(json, 'totalSoldQuantity'),
      totalSoldQuantityPacks: _d(json, 'totalSoldQuantityPacks'),
      totalSoldAmount: _d(json, 'totalSoldAmount'),
      items: items,
      totalReturnQuantityPacks: _d(json, 'totalReturnQuantityPacks'),
      totalFreeIssueQuantityPacks: _d(json, 'totalFreeIssueQuantityPacks'),
      totalPendingQuantityPacks: _d(json, 'totalPendingQuantityPacks'),
      overallAchievementPercent: overall,
    );
  }
}
