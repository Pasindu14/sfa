import 'package:uswatte/features/rep_assignment/domain/entities/rep_assignment.dart';

abstract class RepAssignmentRepository {
  Future<RepAssignment> getMyAssignment();
}
