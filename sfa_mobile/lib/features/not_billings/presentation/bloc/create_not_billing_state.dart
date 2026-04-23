import 'package:equatable/equatable.dart';
import 'package:uswatte/features/not_billings/domain/entities/not_billing_reason.dart';

class CreateNotBillingState extends Equatable {
  final int? outletId;
  final String? outletName;
  final String? routeName;
  final NotBillingReason? reason;
  final String? notes;
  final bool submitting;
  final String? errorMessage;
  final String? submittedClientId; // set on success → triggers navigation

  const CreateNotBillingState({
    this.outletId,
    this.outletName,
    this.routeName,
    this.reason,
    this.notes,
    this.submitting = false,
    this.errorMessage,
    this.submittedClientId,
  });

  bool get canSubmit => outletId != null && reason != null && !submitting;

  CreateNotBillingState copyWith({
    int? outletId,
    String? outletName,
    String? routeName,
    NotBillingReason? reason,
    String? notes,
    bool? submitting,
    String? errorMessage,
    String? submittedClientId,
    bool clearError = false,
    bool clearSubmitted = false,
  }) =>
      CreateNotBillingState(
        outletId: outletId ?? this.outletId,
        outletName: outletName ?? this.outletName,
        routeName: routeName ?? this.routeName,
        reason: reason ?? this.reason,
        notes: notes ?? this.notes,
        submitting: submitting ?? this.submitting,
        errorMessage: clearError ? null : (errorMessage ?? this.errorMessage),
        submittedClientId: clearSubmitted ? null : (submittedClientId ?? this.submittedClientId),
      );

  @override
  List<Object?> get props => [
        outletId,
        outletName,
        routeName,
        reason,
        notes,
        submitting,
        errorMessage,
        submittedClientId,
      ];
}
