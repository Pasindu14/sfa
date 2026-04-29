import 'package:equatable/equatable.dart';

class BillLine extends Equatable {
  final int id;
  final String billingNumber;
  final String billingDate;
  final double totalAmount;
  final String status;

  const BillLine({
    required this.id,
    required this.billingNumber,
    required this.billingDate,
    required this.totalAmount,
    required this.status,
  });

  @override
  List<Object?> get props => [id, billingNumber, billingDate, totalAmount, status];
}
