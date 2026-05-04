import 'package:equatable/equatable.dart';
import 'package:uswatte/features/outlets/domain/entities/outlet.dart';
import 'package:uswatte/features/todays_route_map/domain/enums/route_outlet_status.dart';

class RouteMapOutlet extends Equatable {
  final Outlet outlet;
  final RouteOutletStatus status;

  const RouteMapOutlet({required this.outlet, required this.status});

  @override
  List<Object?> get props => [outlet.id, status];
}
