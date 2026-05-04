import 'package:uswatte/features/supervisor_summary/data/datasources/supervisor_summary_remote_datasource.dart';
import 'package:uswatte/features/supervisor_summary/domain/entities/supervisor_summary.dart';
import 'package:uswatte/features/supervisor_summary/domain/repositories/supervisor_summary_repository.dart';

class SupervisorSummaryRepositoryImpl implements SupervisorSummaryRepository {
  final SupervisorSummaryRemoteDatasource _datasource;
  const SupervisorSummaryRepositoryImpl(this._datasource);

  @override
  Future<SupervisorSummary> getSummary(String date) =>
      _datasource.getSummary(date);
}
