import 'package:equatable/equatable.dart';
import 'package:uswatte/features/supervisor_summary/domain/entities/supervisor_summary.dart';

abstract class SupervisorSummaryState extends Equatable {
  const SupervisorSummaryState();

  @override
  List<Object?> get props => [];
}

class SupervisorSummaryInitial extends SupervisorSummaryState {
  const SupervisorSummaryInitial();
}

class SupervisorSummaryLoading extends SupervisorSummaryState {
  const SupervisorSummaryLoading();
}

class SupervisorSummaryLoaded extends SupervisorSummaryState {
  final SupervisorSummary summary;
  const SupervisorSummaryLoaded(this.summary);

  @override
  List<Object?> get props => [summary];
}

class SupervisorSummaryError extends SupervisorSummaryState {
  final String message;
  const SupervisorSummaryError([this.message = 'Failed to load summary.']);

  @override
  List<Object?> get props => [message];
}
