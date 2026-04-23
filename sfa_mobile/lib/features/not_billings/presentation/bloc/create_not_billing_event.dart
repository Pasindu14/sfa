import 'package:equatable/equatable.dart';
import 'package:uswatte/features/not_billings/domain/entities/not_billing_reason.dart';

sealed class CreateNotBillingEvent extends Equatable {
  const CreateNotBillingEvent();
  @override
  List<Object?> get props => [];
}

final class OutletSelectedForNotBilling extends CreateNotBillingEvent {
  final int outletId;
  final String outletName;
  const OutletSelectedForNotBilling({required this.outletId, required this.outletName});
  @override
  List<Object?> get props => [outletId, outletName];
}

final class NotBillingReasonSelected extends CreateNotBillingEvent {
  final NotBillingReason reason;
  const NotBillingReasonSelected(this.reason);
  @override
  List<Object?> get props => [reason];
}

final class NotBillingNotesChanged extends CreateNotBillingEvent {
  final String? notes;
  const NotBillingNotesChanged(this.notes);
  @override
  List<Object?> get props => [notes];
}

final class SubmitNotBillingPressed extends CreateNotBillingEvent {
  const SubmitNotBillingPressed();
}
