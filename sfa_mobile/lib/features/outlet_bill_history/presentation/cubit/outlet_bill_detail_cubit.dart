import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/features/outlet_bill_history/domain/repositories/outlet_bill_history_repository.dart';
import 'package:uswatte/features/outlet_bill_history/presentation/cubit/outlet_bill_detail_state.dart';

class OutletBillDetailCubit extends Cubit<OutletBillDetailState> {
  final OutletBillHistoryRepository _repository;

  OutletBillDetailCubit(this._repository)
      : super(const OutletBillDetailInitial());

  Future<void> load(int billingId) async {
    emit(const OutletBillDetailLoading());
    try {
      final bill = await _repository.getBillDetail(billingId);
      emit(OutletBillDetailLoaded(bill));
    } catch (e) {
      emit(OutletBillDetailError(e.toString()));
    }
  }
}
