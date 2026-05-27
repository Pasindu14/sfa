import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/features/my_bills/domain/repositories/my_bills_repository.dart';
import 'package:uswatte/features/my_bills/presentation/cubit/my_bills_state.dart';

class MyBillsCubit extends Cubit<MyBillsState> {
  final MyBillsRepository _repository;

  static const int _pageSize = 20;

  int _currentPage = 1;
  String? _dateFrom;
  String? _dateTo;
  String? _billNo;

  MyBillsCubit(this._repository) : super(const MyBillsInitial());

  Future<void> search({
    String? dateFrom,
    String? dateTo,
    String? billNo,
  }) async {
    _currentPage = 1;
    _dateFrom = dateFrom;
    _dateTo = dateTo;
    _billNo = billNo;

    emit(const MyBillsLoading());
    try {
      final result = await _repository.getMyBills(
        dateFrom: dateFrom,
        dateTo: dateTo,
        billNo: billNo,
        page: 1,
        pageSize: _pageSize,
      );
      emit(MyBillsLoaded(result.bills, hasMore: result.hasMore));
    } catch (e) {
      emit(MyBillsError(e.toString()));
    }
  }

  Future<void> loadMore() async {
    final current = state;
    if (current is! MyBillsLoaded) return;
    if (!current.hasMore || current.isLoadingMore) return;

    emit(MyBillsLoaded(current.bills, hasMore: true, isLoadingMore: true));

    try {
      _currentPage++;
      final result = await _repository.getMyBills(
        dateFrom: _dateFrom,
        dateTo: _dateTo,
        billNo: _billNo,
        page: _currentPage,
        pageSize: _pageSize,
      );
      emit(MyBillsLoaded(
        [...current.bills, ...result.bills],
        hasMore: result.hasMore,
      ));
    } catch (_) {
      _currentPage--;
      emit(MyBillsLoaded(current.bills, hasMore: true));
    }
  }
}
