import 'package:equatable/equatable.dart';
import 'package:uswatte/features/rep_monthly_sales/domain/entities/rep_daily_sales.dart';

abstract class RepDailySalesState extends Equatable {
  const RepDailySalesState();

  @override
  List<Object?> get props => [];
}

class RepDailySalesInitial extends RepDailySalesState {
  const RepDailySalesInitial();
}

class RepDailySalesLoading extends RepDailySalesState {
  const RepDailySalesLoading();
}

class RepDailySalesLoaded extends RepDailySalesState {
  final RepDailySales sales;
  const RepDailySalesLoaded(this.sales);

  @override
  List<Object?> get props => [sales];
}

class RepDailySalesError extends RepDailySalesState {
  const RepDailySalesError();
}
