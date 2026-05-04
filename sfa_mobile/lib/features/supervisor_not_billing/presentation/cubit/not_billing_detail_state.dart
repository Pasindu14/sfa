import 'package:equatable/equatable.dart';
import 'package:uswatte/features/supervisor_not_billing/domain/entities/rep_not_billing_detail.dart';

abstract class NotBillingDetailState extends Equatable {
  const NotBillingDetailState();

  @override
  List<Object?> get props => [];
}

class NotBillingDetailInitial extends NotBillingDetailState {
  const NotBillingDetailInitial();
}

class NotBillingDetailLoading extends NotBillingDetailState {
  const NotBillingDetailLoading();
}

class NotBillingDetailLoaded extends NotBillingDetailState {
  final RepNotBillingDetail detail;
  const NotBillingDetailLoaded(this.detail);

  @override
  List<Object?> get props => [detail];
}

class NotBillingDetailError extends NotBillingDetailState {
  final String message;
  const NotBillingDetailError(this.message);

  @override
  List<Object?> get props => [message];
}
