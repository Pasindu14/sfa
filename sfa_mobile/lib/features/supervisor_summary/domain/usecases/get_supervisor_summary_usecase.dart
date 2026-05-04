import 'package:uswatte/features/supervisor_summary/domain/entities/supervisor_summary.dart';
import 'package:uswatte/features/supervisor_summary/domain/repositories/supervisor_summary_repository.dart';

class GetSupervisorSummaryUseCase {
  final SupervisorSummaryRepository _repository;
  const GetSupervisorSummaryUseCase(this._repository);

  Future<SupervisorSummary> call(String date) => _repository.getSummary(date);
}
