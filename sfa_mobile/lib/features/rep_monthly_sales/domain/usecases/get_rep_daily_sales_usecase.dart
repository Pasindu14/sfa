import 'package:uswatte/features/rep_monthly_sales/domain/entities/rep_daily_sales.dart';
import 'package:uswatte/features/rep_monthly_sales/domain/repositories/rep_monthly_sales_repository.dart';

class GetRepDailySalesUseCase {
  final RepMonthlySalesRepository _repository;
  const GetRepDailySalesUseCase(this._repository);

  Future<RepDailySales> call(DateTime date) => _repository.getDailySales(date);
}
