import 'package:equatable/equatable.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';

abstract class SupervisorNotBillingEvent extends Equatable {
  const SupervisorNotBillingEvent();

  @override
  List<Object?> get props => [];
}

class LoadRepsRequested extends SupervisorNotBillingEvent {
  const LoadRepsRequested();
}

class RepSelected extends SupervisorNotBillingEvent {
  final RepSummary rep;
  const RepSelected(this.rep);

  @override
  List<Object?> get props => [rep];
}

class DateSelected extends SupervisorNotBillingEvent {
  final DateTime date;
  const DateSelected(this.date);

  @override
  List<Object?> get props => [date];
}

class LoadNotBillingsRequested extends SupervisorNotBillingEvent {
  const LoadNotBillingsRequested();
}
