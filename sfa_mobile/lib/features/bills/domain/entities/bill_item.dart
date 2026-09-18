import 'package:equatable/equatable.dart';

class BillItem extends Equatable {
  final int? id;
  final String clientBillId;
  final int productId;
  final String? productName;
  final double quantity;
  final double unitPrice;
  final double discountRate;
  final String billingItemType; // 'Sale' | 'FreeIssue' | 'Return'
  final String? returnType;     // 'Damage' | 'Expire' | 'MarketResell' — null for Sale and FreeIssue
  final String? freeIssueSource; // 'Company' | 'Distributor' — only set when billingItemType == 'FreeIssue'
  final DateTime? expireDate;   // Only when returnType == 'Expire'
  final int lineNumber;
  final String priceType; // 'Case' | 'Packet'

  /// The pricing structure that priced this line. Null on legacy lines billed
  /// before structures existed.
  final int? pricingStructureId;

  /// Local name of [pricingStructureId]; null when that structure is no longer
  /// synced (read-only, joined for display).
  final String? pricingStructureName;

  /// The structure's list price for this line's basis at billing time — the
  /// pack price for Pack lines, the full case price for Case lines, null for
  /// Manual (rep-typed return) lines.
  final double? listUnitPrice;

  const BillItem({
    this.id,
    required this.clientBillId,
    required this.productId,
    this.productName,
    required this.quantity,
    required this.unitPrice,
    this.discountRate = 0,
    this.billingItemType = 'Sale',
    this.returnType,
    this.freeIssueSource,
    this.expireDate,
    required this.lineNumber,
    this.priceType = 'Packet',
    this.pricingStructureId,
    this.pricingStructureName,
    this.listUnitPrice,
  });

  bool get isFreeIssue => billingItemType == 'FreeIssue';
  bool get isReturn    => billingItemType == 'Return';
  bool get isSale      => billingItemType == 'Sale';

  /// The server's PriceBasis for this line.
  String get priceBasis => priceBasisFor(billingItemType, priceType);

  /// What to call this line's structure on screen, or null for a legacy line.
  String? get pricingStructureLabel =>
      pricingStructureLabelFor(pricingStructureId, pricingStructureName);

  @override
  List<Object?> get props => [
        id,
        clientBillId,
        productId,
        productName,
        quantity,
        unitPrice,
        discountRate,
        billingItemType,
        returnType,
        freeIssueSource,
        expireDate,
        lineNumber,
        priceType,
        pricingStructureId,
        pricingStructureName,
        listUnitPrice,
      ];
}

/// Maps the app's line shape onto the server's PriceBasis: a return carries a
/// rep-typed price (Manual); everything else was priced from the structure
/// per case or per pack.
String priceBasisFor(String billingItemType, String priceType) {
  if (billingItemType == 'Return') return 'Manual';
  return priceType == 'Case' ? 'Case' : 'Pack';
}

/// Inverse of [priceBasisFor] for lines pulled back from the server. Manual
/// and legacy (null) lines have no case/packet split to recover, so they read
/// as Packet — the column's historic default.
String priceTypeForBasis(String? priceBasis) =>
    priceBasis == 'Case' ? 'Case' : 'Packet';

/// Display label for a structure reference: its name when the structure is
/// still synced, "Price list #id" when it is not, null for a legacy bill.
String? pricingStructureLabelFor(int? id, String? name) {
  if (id == null) return null;
  return (name != null && name.isNotEmpty) ? name : 'Price list #$id';
}
