import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/features/rep_monthly_sales/domain/usecases/get_rep_monthly_sales_usecase.dart';
import 'package:uswatte/features/rep_monthly_sales/presentation/cubit/rep_monthly_sales_state.dart';

class RepMonthlySalesCubit extends Cubit<RepMonthlySalesState> {
  final GetRepMonthlySalesUseCase _getSales;

  RepMonthlySalesCubit(this._getSales) : super(const RepMonthlySalesInitial());

  Future<void> load(int year, int month) async {
    emit(const RepMonthlySalesLoading());
    try {
      final sales = await _getSales(year, month);
      emit(RepMonthlySalesLoaded(sales));
    } catch (_) {
      emit(const RepMonthlySalesError());
    }
  }
}
