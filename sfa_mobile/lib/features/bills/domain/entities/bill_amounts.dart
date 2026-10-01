import 'dart:math' as math;

import 'package:uswatte/core/utils/bill_breakdown.dart';

import 'bill.dart';

extension BillAmounts on Bill {
  /// Sales (gross) − Discount − Returns = Total, derived from the bill's own
  /// lines so it works for pending, failed and synced bills alike without
  /// storing anything extra on the device.
  ///
  /// [Bill.subTotalAmount] is already net of line discounts, so gross comes
  /// from the sale lines and discount is `gross − subTotal + billDiscount`.
  /// Returns are `gross − discount − total` rather than a sum of Return lines:
  /// that keeps the rows adding up to the stored total, and it can never pick
  /// up the distributor-return mirror lines of an adjusted bill (their value is
  /// already in the reduced sale lines).
  BillBreakdown get breakdown {
    final saleLines = items.where((i) => i.isSale);
    // A bill with no lines on hand (should not happen) falls back to its net.
    final gross = saleLines.isEmpty
        ? subTotalAmount
        : saleLines.fold<double>(0, (s, i) => s + i.quantity * i.unitPrice);
    final discount = math.max(0.0, gross - subTotalAmount + billDiscountAmount);
    final returns = math.max(0.0, gross - discount - totalAmount);

    double foc(String? source) => items
        .where((i) => i.isFreeIssue && (source == null || i.freeIssueSource == source))
        .fold<double>(0, (s, i) => s + i.quantity * i.unitPrice);

    return BillBreakdown(
      gross: gross,
      discount: discount,
      returns: returns,
      total: totalAmount,
      freeIssue: foc(null),
      freeIssueCompany: foc('Company'),
      freeIssueDistributor: foc('Distributor'),
    );
  }
}
