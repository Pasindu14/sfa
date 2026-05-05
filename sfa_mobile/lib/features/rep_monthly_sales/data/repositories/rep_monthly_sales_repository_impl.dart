import 'package:uswatte/features/rep_monthly_sales/data/datasources/rep_monthly_sales_remote_datasource.dart';
import 'package:uswatte/features/rep_monthly_sales/domain/entities/rep_monthly_sales.dart';
import 'package:uswatte/features/rep_monthly_sales/domain/repositories/rep_monthly_sales_repository.dart';

class RepMonthlySalesRepositoryImpl implements RepMonthlySalesRepository {
  final RepMonthlySalesRemoteDatasource _datasource;
  const RepMonthlySalesRepositoryImpl(this._datasource);

  @override
  Future<RepMonthlySales> getMonthlySales(int year, int month) =>
      _datasource.getMonthlySales(year, month);
}
