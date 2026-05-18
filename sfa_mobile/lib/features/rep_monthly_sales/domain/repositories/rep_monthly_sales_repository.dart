import 'package:uswatte/features/rep_monthly_sales/domain/entities/rep_daily_sales.dart';
import 'package:uswatte/features/rep_monthly_sales/domain/entities/rep_monthly_sales.dart';

abstract class RepMonthlySalesRepository {
  Future<RepMonthlySales> getMonthlySales(int year, int month);
  Future<RepDailySales> getDailySales(DateTime date);
}
