import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/features/rep_monthly_sales/domain/usecases/get_rep_daily_sales_usecase.dart';
import 'package:uswatte/features/rep_monthly_sales/presentation/cubit/rep_daily_sales_state.dart';

class RepDailySalesCubit extends Cubit<RepDailySalesState> {
  final GetRepDailySalesUseCase _getSales;

  RepDailySalesCubit(this._getSales) : super(const RepDailySalesInitial());

  Future<void> load(DateTime date) async {
    emit(const RepDailySalesLoading());
    try {
      final sales = await _getSales(date);
      emit(RepDailySalesLoaded(sales));
    } catch (_) {
      emit(const RepDailySalesError());
    }
  }
}
