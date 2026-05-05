import 'package:uswatte/features/sales_rep_target/domain/entities/rep_monthly_target.dart';
import 'package:uswatte/features/sales_rep_target/domain/repositories/rep_target_repository.dart';

class GetRepMonthlyTargetUseCase {
  final RepTargetRepository _repository;
  const GetRepMonthlyTargetUseCase(this._repository);

  Future<RepMonthlyTarget> call(int year, int month) =>
      _repository.getMonthlyTarget(year, month);
}
