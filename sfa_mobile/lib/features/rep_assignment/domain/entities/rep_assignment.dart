import 'package:equatable/equatable.dart';

class RepAssignment extends Equatable {
  final int? divisionId;
  final String? divisionName;
  final int? territoryId;
  final String? territoryName;
  final int? distributorId;
  final String? distributorName;
  final int? fleetId;
  final String? fleetName;

  const RepAssignment({
    this.divisionId,
    this.divisionName,
    this.territoryId,
    this.territoryName,
    this.distributorId,
    this.distributorName,
    this.fleetId,
    this.fleetName,
  });

  @override
  List<Object?> get props => [
        divisionId,
        divisionName,
        territoryId,
        territoryName,
        distributorId,
        distributorName,
        fleetId,
        fleetName,
      ];
}
