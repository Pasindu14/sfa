import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/features/supervisor_billing/domain/usecases/get_billing_detail_usecase.dart';
import 'package:uswatte/features/supervisor_billing/presentation/cubit/billing_detail_state.dart';

class BillingDetailCubit extends Cubit<BillingDetailState> {
  final GetBillingDetailUseCase _getDetail;

  BillingDetailCubit(this._getDetail) : super(const BillingDetailInitial());

  Future<void> load(int billingId) async {
    emit(const BillingDetailLoading());
    try {
      final detail = await _getDetail(billingId);
      emit(BillingDetailLoaded(detail));
    } catch (e) {
      emit(const BillingDetailError('Failed to load billing details.'));
    }
  }
}
