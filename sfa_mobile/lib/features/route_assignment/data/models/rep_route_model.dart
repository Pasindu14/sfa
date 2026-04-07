import 'package:uswatte/features/route_assignment/domain/entities/rep_route.dart';

class RepRouteModel extends RepRoute {
  const RepRouteModel({required super.routeId, required super.routeName});

  factory RepRouteModel.fromJson(Map<String, dynamic> json) => RepRouteModel(
        routeId: json['routeId'] as int,
        routeName: json['routeName'] as String,
      );
}
