import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/features/outlet_bill_history/domain/repositories/outlet_bill_history_repository.dart';
import 'package:uswatte/features/outlet_bill_history/presentation/cubit/outlet_bill_history_state.dart';

class OutletBillHistoryCubit extends Cubit<OutletBillHistoryState> {
  final OutletBillHistoryRepository _repository;

  static const int _pageSize = 20;

  int _currentPage = 1;
  int? _outletId;
  String? _dateFrom;
  String? _dateTo;

  OutletBillHistoryCubit(this._repository)
      : super(const OutletBillHistoryInitial());

  Future<void> load({
    required int outletId,
    required DateTime dateFrom,
    required DateTime dateTo,
  }) async {
    _currentPage = 1;
    _outletId = outletId;
    _dateFrom = _formatDate(dateFrom);
    _dateTo = _formatDate(dateTo);

    emit(const OutletBillHistoryLoading());
    try {
      final result = await _repository.getBillsForOutlet(
        outletId: outletId,
        dateFrom: _dateFrom!,
        dateTo: _dateTo!,
        page: 1,
        pageSize: _pageSize,
      );
      emit(OutletBillHistoryLoaded(result.bills, hasMore: result.hasMore));
    } catch (e) {
      emit(OutletBillHistoryError(e.toString()));
    }
  }

  Future<void> loadMore() async {
    final current = state;
    if (current is! OutletBillHistoryLoaded) return;
    if (!current.hasMore || current.isLoadingMore) return;

    emit(OutletBillHistoryLoaded(
      current.bills,
      hasMore: true,
      isLoadingMore: true,
    ));

    try {
      _currentPage++;
      final result = await _repository.getBillsForOutlet(
        outletId: _outletId!,
        dateFrom: _dateFrom!,
        dateTo: _dateTo!,
        page: _currentPage,
        pageSize: _pageSize,
      );
      emit(OutletBillHistoryLoaded(
        [...current.bills, ...result.bills],
        hasMore: result.hasMore,
      ));
    } catch (_) {
      _currentPage--;
      emit(OutletBillHistoryLoaded(current.bills, hasMore: true));
    }
  }

  String _formatDate(DateTime d) {
    final m = d.month.toString().padLeft(2, '0');
    final day = d.day.toString().padLeft(2, '0');
    return '${d.year}-$m-$day';
  }
}
