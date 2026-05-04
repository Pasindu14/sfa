import 'package:uswatte/features/supervisor_summary/domain/entities/supervisor_summary.dart';

abstract class SupervisorSummaryRepository {
  Future<SupervisorSummary> getSummary(String date);
}
