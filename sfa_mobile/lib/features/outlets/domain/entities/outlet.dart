import 'package:equatable/equatable.dart';

class Outlet extends Equatable {
  final int id;
  final String name;
  final String address;
  final String tel;
  final String? email;
  final String? contactPerson;
  final double latitude;
  final double longitude;
  final String outletType;
  final String outletCategory;
  final int routeId;
  final String routeName;
  final bool isActive;

  const Outlet({
    required this.id,
    required this.name,
    required this.address,
    required this.tel,
    this.email,
    this.contactPerson,
    required this.latitude,
    required this.longitude,
    required this.outletType,
    required this.outletCategory,
    required this.routeId,
    required this.routeName,
    required this.isActive,
  });

  @override
  List<Object?> get props => [id];
}
