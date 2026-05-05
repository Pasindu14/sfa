import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/features/sales_rep_target/domain/usecases/get_rep_monthly_target_usecase.dart';
import 'package:uswatte/features/sales_rep_target/presentation/cubit/rep_target_state.dart';

class RepTargetCubit extends Cubit<RepTargetState> {
  final GetRepMonthlyTargetUseCase _getTarget;

  RepTargetCubit(this._getTarget) : super(const RepTargetInitial());

  Future<void> load(int year, int month) async {
    emit(const RepTargetLoading());
    try {
      final target = await _getTarget(year, month);
      emit(RepTargetLoaded(target));
    } catch (_) {
      emit(const RepTargetError());
    }
  }
}
