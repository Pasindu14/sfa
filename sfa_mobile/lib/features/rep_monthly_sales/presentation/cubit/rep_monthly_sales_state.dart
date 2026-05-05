import 'package:equatable/equatable.dart';
import 'package:uswatte/features/rep_monthly_sales/domain/entities/rep_monthly_sales.dart';

abstract class RepMonthlySalesState extends Equatable {
  const RepMonthlySalesState();

  @override
  List<Object?> get props => [];
}

class RepMonthlySalesInitial extends RepMonthlySalesState {
  const RepMonthlySalesInitial();
}

class RepMonthlySalesLoading extends RepMonthlySalesState {
  const RepMonthlySalesLoading();
}

class RepMonthlySalesLoaded extends RepMonthlySalesState {
  final RepMonthlySales sales;
  const RepMonthlySalesLoaded(this.sales);

  @override
  List<Object?> get props => [sales];
}

class RepMonthlySalesError extends RepMonthlySalesState {
  const RepMonthlySalesError();
}
