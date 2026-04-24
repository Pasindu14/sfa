import 'package:equatable/equatable.dart';

class CreateOutletState extends Equatable {
  final String name;
  final String address;
  final String tel;
  final String nicNo;
  final String outletType;
  final String outletCategory;
  final double? latitude;
  final double? longitude;
  final String creditLimit;
  final String email;
  final String contactPerson;
  final String vatNo;
  final String remarks;
  final DateTime? ownerDOB;
  final String image;
  final int? provinceCode;
  final int? districtCode;

  final bool isLocating;
  final bool isSubmitting;
  final bool submitted;
  final Map<String, String> errors;
  final String? submitError;

  const CreateOutletState({
    this.name = '',
    this.address = '',
    this.tel = '',
    this.nicNo = '',
    this.outletType = '',
    this.outletCategory = '',
    this.latitude,
    this.longitude,
    this.creditLimit = '0',
    this.email = '',
    this.contactPerson = '',
    this.vatNo = '',
    this.remarks = '',
    this.ownerDOB,
    this.image = '',
    this.provinceCode,
    this.districtCode,
    this.isLocating = false,
    this.isSubmitting = false,
    this.submitted = false,
    this.errors = const {},
    this.submitError,
  });

  bool get hasLocation => latitude != null && longitude != null;

  bool get canSubmit =>
      name.isNotEmpty &&
      address.isNotEmpty &&
      tel.isNotEmpty &&
      nicNo.isNotEmpty &&
      outletType.isNotEmpty &&
      outletCategory.isNotEmpty &&
      hasLocation &&
      !isSubmitting;

  CreateOutletState copyWith({
    String? name,
    String? address,
    String? tel,
    String? nicNo,
    String? outletType,
    String? outletCategory,
    double? latitude,
    double? longitude,
    bool clearLocation = false,
    String? creditLimit,
    String? email,
    String? contactPerson,
    String? vatNo,
    String? remarks,
    DateTime? ownerDOB,
    bool clearOwnerDOB = false,
    String? image,
    int? provinceCode,
    bool clearProvince = false,
    int? districtCode,
    bool clearDistrict = false,
    bool? isLocating,
    bool? isSubmitting,
    bool? submitted,
    Map<String, String>? errors,
    String? submitError,
    bool clearSubmitError = false,
  }) {
    return CreateOutletState(
      name: name ?? this.name,
      address: address ?? this.address,
      tel: tel ?? this.tel,
      nicNo: nicNo ?? this.nicNo,
      outletType: outletType ?? this.outletType,
      outletCategory: outletCategory ?? this.outletCategory,
      latitude: clearLocation ? null : (latitude ?? this.latitude),
      longitude: clearLocation ? null : (longitude ?? this.longitude),
      creditLimit: creditLimit ?? this.creditLimit,
      email: email ?? this.email,
      contactPerson: contactPerson ?? this.contactPerson,
      vatNo: vatNo ?? this.vatNo,
      remarks: remarks ?? this.remarks,
      ownerDOB: clearOwnerDOB ? null : (ownerDOB ?? this.ownerDOB),
      image: image ?? this.image,
      provinceCode: clearProvince ? null : (provinceCode ?? this.provinceCode),
      districtCode: clearDistrict ? null : (districtCode ?? this.districtCode),
      isLocating: isLocating ?? this.isLocating,
      isSubmitting: isSubmitting ?? this.isSubmitting,
      submitted: submitted ?? this.submitted,
      errors: errors ?? this.errors,
      submitError:
          clearSubmitError ? null : (submitError ?? this.submitError),
    );
  }

  @override
  List<Object?> get props => [
        name, address, tel, nicNo, outletType, outletCategory,
        latitude, longitude, creditLimit, email, contactPerson, vatNo,
        remarks, ownerDOB, image, provinceCode, districtCode,
        isLocating, isSubmitting, submitted, errors, submitError,
      ];
}
