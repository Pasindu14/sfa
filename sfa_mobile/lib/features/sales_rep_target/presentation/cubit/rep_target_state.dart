import 'package:equatable/equatable.dart';
import 'package:uswatte/features/sales_rep_target/domain/entities/rep_monthly_target.dart';

abstract class RepTargetState extends Equatable {
  const RepTargetState();

  @override
  List<Object?> get props => [];
}

class RepTargetInitial extends RepTargetState {
  const RepTargetInitial();
}

class RepTargetLoading extends RepTargetState {
  const RepTargetLoading();
}

class RepTargetLoaded extends RepTargetState {
  final RepMonthlyTarget target;
  const RepTargetLoaded(this.target);

  @override
  List<Object?> get props => [target];
}

class RepTargetError extends RepTargetState {
  const RepTargetError();
}
