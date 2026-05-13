import 'package:uswatte/features/outlets/domain/entities/outlet.dart';

class OutletModel {
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
  final DateTime? lastBillDate;

  const OutletModel({
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
    this.lastBillDate,
  });

  factory OutletModel.fromJson(Map<String, dynamic> json) => OutletModel(
        id: json['id'] as int,
        name: json['name'] as String,
        address: json['address'] as String,
        tel: json['tel'] as String,
        email: json['email'] as String?,
        contactPerson: json['contactPerson'] as String?,
        latitude: (json['latitude'] as num).toDouble(),
        longitude: (json['longitude'] as num).toDouble(),
        outletType: json['outletType'] as String,
        outletCategory: json['outletCategory'] as String,
        routeId: json['routeId'] as int,
        routeName: json['routeName'] as String,
        isActive: json['isActive'] as bool,
        lastBillDate: json['lastBillDate'] != null
            ? DateTime.parse(json['lastBillDate'] as String)
            : null,
      );

  factory OutletModel.fromMap(Map<String, dynamic> map) => OutletModel(
        id: map['id'] as int,
        name: map['name'] as String,
        address: map['address'] as String,
        tel: map['tel'] as String,
        email: map['email'] as String?,
        contactPerson: map['contact_person'] as String?,
        latitude: map['latitude'] as double,
        longitude: map['longitude'] as double,
        outletType: map['outlet_type'] as String,
        outletCategory: map['outlet_category'] as String,
        routeId: map['route_id'] as int,
        routeName: map['route_name'] as String,
        isActive: (map['is_active'] as int) == 1,
        lastBillDate: map['last_bill_date'] != null
            ? DateTime.parse(map['last_bill_date'] as String)
            : null,
      );

  Map<String, dynamic> toMap() => {
        'id': id,
        'name': name,
        'address': address,
        'tel': tel,
        'email': email,
        'contact_person': contactPerson,
        'latitude': latitude,
        'longitude': longitude,
        'outlet_type': outletType,
        'outlet_category': outletCategory,
        'route_id': routeId,
        'route_name': routeName,
        'is_active': isActive ? 1 : 0,
        'last_bill_date': lastBillDate?.toIso8601String(),
      };

  Outlet toEntity() => Outlet(
        id: id,
        name: name,
        address: address,
        tel: tel,
        email: email,
        contactPerson: contactPerson,
        latitude: latitude,
        longitude: longitude,
        outletType: outletType,
        outletCategory: outletCategory,
        routeId: routeId,
        routeName: routeName,
        isActive: isActive,
        lastBillDate: lastBillDate,
      );
}
