import 'package:uswatte/features/rep_assignment/domain/entities/rep_assignment.dart';

class RepAssignmentModel extends RepAssignment {
  const RepAssignmentModel({
    super.divisionId,
    super.divisionName,
    super.territoryId,
    super.territoryName,
    super.distributorId,
    super.distributorName,
    super.fleetId,
    super.fleetName,
  });

  factory RepAssignmentModel.fromJson(Map<String, dynamic> json) =>
      RepAssignmentModel(
        divisionId: json['divisionId'] as int?,
        divisionName: json['divisionName'] as String?,
        territoryId: json['territoryId'] as int?,
        territoryName: json['territoryName'] as String?,
        distributorId: json['distributorId'] as int?,
        distributorName: json['distributorName'] as String?,
        fleetId: json['fleetId'] as int?,
        fleetName: json['fleetName'] as String?,
      );
}
