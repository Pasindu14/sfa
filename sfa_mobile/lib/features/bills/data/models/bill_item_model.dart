import 'package:uswatte/features/bills/domain/entities/bill_item.dart';

class BillItemModel {
  final int? id;
  final String clientBillId;
  final int productId;
  final String? productName;
  final double quantity;
  final double unitPrice;
  final double discountRate;
  final String billingItemType; // 'Sale' | 'FreeIssue' | 'Return'
  final String? returnType;
  final String? freeIssueSource; // 'Company' | 'Distributor' — null unless FOC
  final DateTime? expireDate;
  final int lineNumber;
  final String priceType;
  final int? pricingStructureId;

  /// Joined from `price_structures` on read; never written.
  final String? pricingStructureName;
  final double? listUnitPrice;

  const BillItemModel({
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

  factory BillItemModel.fromMap(Map<String, dynamic> map) {
    // Backward-compat: rows persisted before the schema change still carry is_free_issue.
    // Promote them to billing_item_type = 'FreeIssue' on read.
    final rawType = map['billing_item_type'] as String? ?? 'Sale';
    final legacyFi = (map['is_free_issue'] as int? ?? 0) == 1;
    final type = legacyFi && rawType == 'Sale' ? 'FreeIssue' : rawType;

    return BillItemModel(
      id: map['id'] as int?,
      clientBillId: map['client_bill_id'] as String,
      productId: map['product_id'] as int,
      productName: map['product_name'] as String?,
      quantity: (map['quantity'] as num).toDouble(),
      unitPrice: (map['unit_price'] as num).toDouble(),
      discountRate: (map['discount_rate'] as num?)?.toDouble() ?? 0,
      billingItemType: type,
      returnType: map['return_type'] as String?,
      freeIssueSource: map['free_issue_source'] as String?,
      expireDate: map['expire_date'] != null
          ? DateTime.tryParse(map['expire_date'] as String)
          : null,
      lineNumber: map['line_number'] as int,
      priceType: map['price_type'] as String? ?? 'Packet',
      pricingStructureId: map['pricing_structure_id'] as int?,
      pricingStructureName: map['pricing_structure_name'] as String?,
      listUnitPrice: (map['list_unit_price'] as num?)?.toDouble(),
    );
  }

  Map<String, dynamic> toMap() => {
        if (id != null) 'id': id,
        'client_bill_id': clientBillId,
        'product_id': productId,
        'quantity': quantity,
        'unit_price': unitPrice,
        'discount_rate': discountRate,
        'billing_item_type': billingItemType,
        'return_type': returnType,
        'free_issue_source': freeIssueSource,
        'expire_date': expireDate != null ? _dateOnly(expireDate!) : null,
        'line_number': lineNumber,
        'price_type': priceType,
        'pricing_structure_id': pricingStructureId,
        'list_unit_price': listUnitPrice,
      };

  /// One entry of CreateBillingRequest.items. The pricing snapshot fields are
  /// optional on the server; `unitPrice` is still what the server trusts.
  ///
  /// A line queued before this app version has no structure: it goes up
  /// without a basis too, so the server treats it as the legacy line it is
  /// instead of checking its price against a structure it was never priced from.
  Map<String, dynamic> toCreateRequestJson() => {
        'productId': productId,
        'quantity': quantity,
        'unitPrice': unitPrice,
        'discountRate': discountRate,
        'billingItemType': billingItemType,
        'returnType': returnType,
        'freeIssueSource': freeIssueSource,
        'expireDate': expireDate != null ? _dateOnly(expireDate!) : null,
        'pricingStructureId': pricingStructureId,
        'priceBasis': pricingStructureId == null
            ? null
            : priceBasisFor(billingItemType, priceType),
        'listUnitPrice': listUnitPrice,
      };

  BillItem toEntity() => BillItem(
        id: id,
        clientBillId: clientBillId,
        productId: productId,
        productName: productName,
        quantity: quantity,
        unitPrice: unitPrice,
        discountRate: discountRate,
        billingItemType: billingItemType,
        returnType: returnType,
        freeIssueSource: freeIssueSource,
        expireDate: expireDate,
        lineNumber: lineNumber,
        priceType: priceType,
        pricingStructureId: pricingStructureId,
        pricingStructureName: pricingStructureName,
        listUnitPrice: listUnitPrice,
      );

  static String _dateOnly(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-'
      '${d.month.toString().padLeft(2, '0')}-'
      '${d.day.toString().padLeft(2, '0')}';
}
