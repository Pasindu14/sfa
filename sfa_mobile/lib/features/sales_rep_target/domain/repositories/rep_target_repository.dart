import 'package:uswatte/features/sales_rep_target/domain/entities/rep_monthly_target.dart';

abstract class RepTargetRepository {
  Future<RepMonthlyTarget> getMonthlyTarget(int year, int month);
}
