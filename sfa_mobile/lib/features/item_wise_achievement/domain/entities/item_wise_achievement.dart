class ItemAchievement {
  final int productId;
  final String itemCode;
  final String itemName;
  final double targetQuantity;       // cases
  final double soldQuantity;         // cases
  final double soldQuantityPacks;    // raw packs
  final double soldAmount;
  final double achievementPercent;

  const ItemAchievement({
    required this.productId,
    required this.itemCode,
    required this.itemName,
    required this.targetQuantity,
    required this.soldQuantity,
    required this.soldQuantityPacks,
    required this.soldAmount,
    required this.achievementPercent,
  });
}

class ItemWiseAchievement {
  final int year;
  final int month;
  final double totalTargetQuantity;
  final double totalSoldQuantity;
  final double totalSoldQuantityPacks;
  final double totalSoldAmount;
  final List<ItemAchievement> items;

  const ItemWiseAchievement({
    required this.year,
    required this.month,
    required this.totalTargetQuantity,
    required this.totalSoldQuantity,
    required this.totalSoldQuantityPacks,
    required this.totalSoldAmount,
    required this.items,
  });
}
