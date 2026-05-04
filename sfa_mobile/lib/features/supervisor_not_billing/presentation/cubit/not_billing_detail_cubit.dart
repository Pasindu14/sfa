import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/features/supervisor_not_billing/domain/usecases/get_not_billing_detail_usecase.dart';
import 'package:uswatte/features/supervisor_not_billing/presentation/cubit/not_billing_detail_state.dart';

class NotBillingDetailCubit extends Cubit<NotBillingDetailState> {
  final GetNotBillingDetailUseCase _getDetail;

  NotBillingDetailCubit(this._getDetail)
      : super(const NotBillingDetailInitial());

  Future<void> load(int notBillingId) async {
    emit(const NotBillingDetailLoading());
    try {
      final detail = await _getDetail(notBillingId);
      emit(NotBillingDetailLoaded(detail));
    } catch (e) {
      emit(const NotBillingDetailError('Failed to load not-billing details.'));
    }
  }
}
