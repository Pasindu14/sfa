import 'package:uswatte/features/outlet_billings/data/models/bill_line_model.dart';
import 'package:uswatte/features/outlet_billings/domain/entities/outlet_billing_summary.dart';

class OutletSummaryModel {
  final int outletId;
  final String outletName;
  final int billingCount;
  final double totalAmount;
  final List<BillLineModel> bills;

  const OutletSummaryModel({
    required this.outletId,
    required this.outletName,
    required this.billingCount,
    required this.totalAmount,
    required this.bills,
  });

  factory OutletSummaryModel.fromJson(Map<String, dynamic> json) =>
      OutletSummaryModel(
        outletId: json['outletId'] as int,
        outletName: json['outletName'] as String,
        billingCount: json['billingCount'] as int,
        totalAmount: (json['totalAmount'] as num).toDouble(),
        bills: (json['bills'] as List<dynamic>)
            .map((e) => BillLineModel.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

  OutletBillingSummary toEntity() => OutletBillingSummary(
        outletId: outletId,
        outletName: outletName,
        billingCount: billingCount,
        totalAmount: totalAmount,
        bills: bills.map((b) => b.toEntity()).toList(),
      );
}
