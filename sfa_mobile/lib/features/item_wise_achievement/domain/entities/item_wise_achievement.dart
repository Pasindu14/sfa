class ItemAchievement {
  final int productId;
  final String itemCode;
  final String itemName;
  final double targetQuantity;       // cases
  final double soldQuantity;         // cases — approved sales net of outlet returns
  final double soldQuantityPacks;    // packs — same basis
  final double soldAmount;
  final double achievementPercent;

  /// Outlet returns already subtracted from the sold figures (packs).
  final double returnQuantityPacks;

  /// Informational only — free issue is never counted as sold (packs).
  final double freeIssueQuantityPacks;

  /// Bills still waiting for distributor approval; not yet in the sold figures.
  final double pendingQuantityPacks;
  final double pendingQuantity;      // cases

  /// False for items that sold without a target set — no % is meaningful there.
  final bool hasTarget;

  const ItemAchievement({
    required this.productId,
    required this.itemCode,
    required this.itemName,
    required this.targetQuantity,
    required this.soldQuantity,
    required this.soldQuantityPacks,
    required this.soldAmount,
    required this.achievementPercent,
    this.returnQuantityPacks = 0.0,
    this.freeIssueQuantityPacks = 0.0,
    this.pendingQuantityPacks = 0.0,
    this.pendingQuantity = 0.0,
    bool? hasTarget,
  }) : hasTarget = hasTarget ?? targetQuantity > 0;
}

class ItemWiseAchievement {
  final int year;
  final int month;
  final double totalTargetQuantity;
  final double totalSoldQuantity;
  final double totalSoldQuantityPacks;
  final double totalSoldAmount;
  final List<ItemAchievement> items;
  final double totalReturnQuantityPacks;
  final double totalFreeIssueQuantityPacks;
  final double totalPendingQuantityPacks;

  /// Sold cases of items that HAVE a target over their total target cases —
  /// items sold without a target do not inflate it.
  final double overallAchievementPercent;

  const ItemWiseAchievement({
    required this.year,
    required this.month,
    required this.totalTargetQuantity,
    required this.totalSoldQuantity,
    required this.totalSoldQuantityPacks,
    required this.totalSoldAmount,
    required this.items,
    this.totalReturnQuantityPacks = 0.0,
    this.totalFreeIssueQuantityPacks = 0.0,
    this.totalPendingQuantityPacks = 0.0,
    this.overallAchievementPercent = 0.0,
  });
}
