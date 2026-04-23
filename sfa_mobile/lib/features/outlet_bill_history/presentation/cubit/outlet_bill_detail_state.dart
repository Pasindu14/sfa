import 'package:uswatte/features/outlet_bill_history/domain/entities/outlet_bill_detail.dart';

sealed class OutletBillDetailState {
  const OutletBillDetailState();
}

class OutletBillDetailInitial extends OutletBillDetailState {
  const OutletBillDetailInitial();
}

class OutletBillDetailLoading extends OutletBillDetailState {
  const OutletBillDetailLoading();
}

class OutletBillDetailLoaded extends OutletBillDetailState {
  final OutletBillDetail bill;
  const OutletBillDetailLoaded(this.bill);
}

class OutletBillDetailError extends OutletBillDetailState {
  final String message;
  const OutletBillDetailError(this.message);
}
