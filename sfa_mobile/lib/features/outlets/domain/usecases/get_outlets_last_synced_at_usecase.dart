import 'package:uswatte/features/outlets/domain/repositories/outlets_repository.dart';

class GetOutletsLastSyncedAtUseCase {
  final OutletsRepository _repository;
  const GetOutletsLastSyncedAtUseCase(this._repository);

  Future<DateTime?> call() => _repository.getLastSyncedAt();
}
