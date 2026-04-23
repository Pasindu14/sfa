import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/features/outlet_bill_history/domain/repositories/outlet_bill_history_repository.dart';
import 'package:uswatte/features/outlet_bill_history/presentation/cubit/outlet_bill_history_state.dart';

class OutletBillHistoryCubit extends Cubit<OutletBillHistoryState> {
  final OutletBillHistoryRepository _repository;

  OutletBillHistoryCubit(this._repository)
      : super(const OutletBillHistoryInitial());

  Future<void> load({
    required int outletId,
    required DateTime dateFrom,
    required DateTime dateTo,
  }) async {
    emit(const OutletBillHistoryLoading());
    try {
      final bills = await _repository.getBillsForOutlet(
        outletId: outletId,
        dateFrom: _formatDate(dateFrom),
        dateTo: _formatDate(dateTo),
      );
      emit(OutletBillHistoryLoaded(bills));
    } catch (e) {
      emit(OutletBillHistoryError(e.toString()));
    }
  }

  String _formatDate(DateTime d) {
    final m = d.month.toString().padLeft(2, '0');
    final day = d.day.toString().padLeft(2, '0');
    return '${d.year}-$m-$day';
  }
}
