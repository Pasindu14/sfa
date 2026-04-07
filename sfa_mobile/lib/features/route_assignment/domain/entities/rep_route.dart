import 'package:equatable/equatable.dart';

class RepRoute extends Equatable {
  final int routeId;
  final String routeName;

  const RepRoute({required this.routeId, required this.routeName});

  @override
  List<Object> get props => [routeId, routeName];
}
