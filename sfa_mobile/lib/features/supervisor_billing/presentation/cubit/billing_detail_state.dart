import 'package:equatable/equatable.dart';
import 'package:uswatte/features/supervisor_billing/domain/entities/billing_detail.dart';

abstract class BillingDetailState extends Equatable {
  const BillingDetailState();

  @override
  List<Object?> get props => [];
}

class BillingDetailInitial extends BillingDetailState {
  const BillingDetailInitial();
}

class BillingDetailLoading extends BillingDetailState {
  const BillingDetailLoading();
}

class BillingDetailLoaded extends BillingDetailState {
  final BillingDetail detail;
  const BillingDetailLoaded(this.detail);

  @override
  List<Object?> get props => [detail];
}

class BillingDetailError extends BillingDetailState {
  final String message;
  const BillingDetailError(this.message);

  @override
  List<Object?> get props => [message];
}
