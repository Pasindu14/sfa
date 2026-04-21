import 'package:uswatte/features/rep_assignment/data/datasources/rep_assignment_remote_datasource.dart';
import 'package:uswatte/features/rep_assignment/domain/entities/rep_assignment.dart';
import 'package:uswatte/features/rep_assignment/domain/repositories/rep_assignment_repository.dart';

class RepAssignmentRepositoryImpl implements RepAssignmentRepository {
  final RepAssignmentRemoteDatasource _remote;
  const RepAssignmentRepositoryImpl(this._remote);

  @override
  Future<RepAssignment> getMyAssignment() => _remote.getMyAssignment();
}
