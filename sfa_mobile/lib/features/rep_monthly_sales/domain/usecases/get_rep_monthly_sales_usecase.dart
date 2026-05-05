import 'package:uswatte/features/rep_monthly_sales/domain/entities/rep_monthly_sales.dart';
import 'package:uswatte/features/rep_monthly_sales/domain/repositories/rep_monthly_sales_repository.dart';

class GetRepMonthlySalesUseCase {
  final RepMonthlySalesRepository _repository;
  const GetRepMonthlySalesUseCase(this._repository);

  Future<RepMonthlySales> call(int year, int month) =>
      _repository.getMonthlySales(year, month);
}
