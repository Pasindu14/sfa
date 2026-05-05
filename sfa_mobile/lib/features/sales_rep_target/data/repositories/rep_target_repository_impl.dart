import 'package:uswatte/features/sales_rep_target/data/datasources/rep_target_remote_datasource.dart';
import 'package:uswatte/features/sales_rep_target/domain/entities/rep_monthly_target.dart';
import 'package:uswatte/features/sales_rep_target/domain/repositories/rep_target_repository.dart';

class RepTargetRepositoryImpl implements RepTargetRepository {
  final RepTargetRemoteDatasource _datasource;
  const RepTargetRepositoryImpl(this._datasource);

  @override
  Future<RepMonthlyTarget> getMonthlyTarget(int year, int month) =>
      _datasource.getMonthlyTarget(year, month);
}
