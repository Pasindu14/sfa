import 'package:equatable/equatable.dart';

/// One product's line of a rep's item-wise sales
/// (`GET /api/v1/supervisor/rep-itemwise-sales`). Quantities are packs.
class ItemwiseSalesLine extends Equatable {
  final int productId;
  final String itemCode;
  final String itemName;
  final double saleQty;
  final double freeIssueQty;
  final double goodReturnQty;
  final double marketReturnQty;

  /// Sale value before any discount.
  final double grossValue;

  /// Item-wise discount + this item's share of the bill discount.
  final double discount;
  final double goodReturnValue;
  final double marketReturnValue;

  /// grossValue − discount − goodReturnValue − marketReturnValue.
  final double netValue;

  const ItemwiseSalesLine({
    required this.productId,
    required this.itemCode,
    required this.itemName,
    required this.saleQty,
    required this.freeIssueQty,
    required this.goodReturnQty,
    required this.marketReturnQty,
    required this.grossValue,
    required this.discount,
    required this.goodReturnValue,
    required this.marketReturnValue,
    required this.netValue,
  });

  double get returnQty => goodReturnQty + marketReturnQty;
  double get returnValue => goodReturnValue + marketReturnValue;

  factory ItemwiseSalesLine.fromJson(Map<String, dynamic> json) {
    double d(String k) => (json[k] as num?)?.toDouble() ?? 0.0;
    return ItemwiseSalesLine(
      productId: (json['productId'] as num).toInt(),
      itemCode: json['itemCode'] as String? ?? '',
      itemName: json['itemName'] as String? ?? '',
      saleQty: d('saleQty'),
      freeIssueQty: d('freeIssueQty'),
      goodReturnQty: d('goodReturnQty'),
      marketReturnQty: d('marketReturnQty'),
      grossValue: d('grossValue'),
      discount: d('discount'),
      goodReturnValue: d('goodReturnValue'),
      marketReturnValue: d('marketReturnValue'),
      netValue: d('netValue'),
    );
  }

  @override
  List<Object?> get props => [
    productId,
    itemCode,
    itemName,
    saleQty,
    freeIssueQty,
    goodReturnQty,
    marketReturnQty,
    grossValue,
    discount,
    goodReturnValue,
    marketReturnValue,
    netValue,
  ];
}

/// A rep's sales per product over a date range. Same bills as the Sales
/// Summary's money figures: approved + pending; rejected/cancelled excluded.
class RepItemwiseSales extends Equatable {
  final DateTime from;
  final DateTime to;
  final double totalSaleQty;
  final double totalFreeIssueQty;
  final double totalGoodReturnQty;
  final double totalMarketReturnQty;
  final double totalGrossValue;
  final double totalDiscount;
  final double totalGoodReturnValue;
  final double totalMarketReturnValue;
  final double totalNetValue;
  final List<ItemwiseSalesLine> items;

  const RepItemwiseSales({
    required this.from,
    required this.to,
    required this.totalSaleQty,
    required this.totalFreeIssueQty,
    required this.totalGoodReturnQty,
    required this.totalMarketReturnQty,
    required this.totalGrossValue,
    required this.totalDiscount,
    required this.totalGoodReturnValue,
    required this.totalMarketReturnValue,
    required this.totalNetValue,
    required this.items,
  });

  factory RepItemwiseSales.fromJson(Map<String, dynamic> json) {
    double d(String k) => (json[k] as num?)?.toDouble() ?? 0.0;
    return RepItemwiseSales(
      from: DateTime.parse(json['from'] as String),
      to: DateTime.parse(json['to'] as String),
      totalSaleQty: d('totalSaleQty'),
      totalFreeIssueQty: d('totalFreeIssueQty'),
      totalGoodReturnQty: d('totalGoodReturnQty'),
      totalMarketReturnQty: d('totalMarketReturnQty'),
      totalGrossValue: d('totalGrossValue'),
      totalDiscount: d('totalDiscount'),
      totalGoodReturnValue: d('totalGoodReturnValue'),
      totalMarketReturnValue: d('totalMarketReturnValue'),
      totalNetValue: d('totalNetValue'),
      items: (json['items'] as List<dynamic>? ?? const [])
          .map((e) => ItemwiseSalesLine.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  @override
  List<Object?> get props => [
    from,
    to,
    totalSaleQty,
    totalFreeIssueQty,
    totalGoodReturnQty,
    totalMarketReturnQty,
    totalGrossValue,
    totalDiscount,
    totalGoodReturnValue,
    totalMarketReturnValue,
    totalNetValue,
    items,
  ];
}
