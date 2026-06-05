import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/bills/domain/repositories/bills_repository.dart';

class SearchProductsForBillUseCase {
  final BillsRepository _repo;
  const SearchProductsForBillUseCase(this._repo);

  Future<List<ProductWithPrice>> call(
    String query, {
    int limit = 200,
  }) =>
      _repo.searchProducts(query, limit: limit);
}
