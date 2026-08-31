import 'package:uswatte/features/outlets/domain/repositories/outlets_repository.dart';

class ClearDailyOutletsUseCase {
  final OutletsRepository _repository;
  const ClearDailyOutletsUseCase(this._repository);

  Future<void> call() => _repository.clearDailyOutlets();
}
