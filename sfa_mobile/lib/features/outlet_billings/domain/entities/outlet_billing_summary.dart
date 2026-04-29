import 'package:equatable/equatable.dart';
import 'package:uswatte/features/outlet_billings/domain/entities/bill_line.dart';

class OutletBillingSummary extends Equatable {
  final int outletId;
  final String outletName;
  final int billingCount;
  final double totalAmount;
  final List<BillLine> bills;

  const OutletBillingSummary({
    required this.outletId,
    required this.outletName,
    required this.billingCount,
    required this.totalAmount,
    required this.bills,
  });

  @override
  List<Object?> get props => [outletId, outletName, billingCount, totalAmount, bills];
}
