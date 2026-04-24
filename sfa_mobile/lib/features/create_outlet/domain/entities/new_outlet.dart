class NewOutlet {
  final String name;
  final String address;
  final String tel;
  final String nicNo;
  final String outletType;
  final String outletCategory;
  final double latitude;
  final double longitude;
  final int routeId;
  final double creditLimit;
  final String? email;
  final String? contactPerson;
  final String? vatNo;
  final String? remarks;
  final DateTime? ownerDOB;
  final String? image;
  final int? provinceCode;
  final int? districtCode;

  const NewOutlet({
    required this.name,
    required this.address,
    required this.tel,
    required this.nicNo,
    required this.outletType,
    required this.outletCategory,
    required this.latitude,
    required this.longitude,
    required this.routeId,
    required this.creditLimit,
    this.email,
    this.contactPerson,
    this.vatNo,
    this.remarks,
    this.ownerDOB,
    this.image,
    this.provinceCode,
    this.districtCode,
  });
}
