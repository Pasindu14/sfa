import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/features/supervisor_summary/domain/usecases/get_supervisor_summary_usecase.dart';
import 'package:uswatte/features/supervisor_summary/presentation/cubit/supervisor_summary_state.dart';

class SupervisorSummaryCubit extends Cubit<SupervisorSummaryState> {
  final GetSupervisorSummaryUseCase _getSummary;

  SupervisorSummaryCubit(this._getSummary) : super(const SupervisorSummaryInitial()) {
    _load();
  }

  String get _today {
    final d = DateTime.now();
    return '${d.year}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';
  }

  Future<void> _load() async {
    emit(const SupervisorSummaryLoading());
    try {
      final summary = await _getSummary(_today);
      emit(SupervisorSummaryLoaded(summary));
    } catch (_) {
      emit(const SupervisorSummaryError());
    }
  }

  void refresh() => _load();
}
