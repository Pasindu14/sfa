import 'package:equatable/equatable.dart';

abstract class CreateOutletEvent extends Equatable {
  const CreateOutletEvent();
  @override
  List<Object?> get props => [];
}

class OutletNameChanged extends CreateOutletEvent {
  final String value;
  const OutletNameChanged(this.value);
  @override
  List<Object?> get props => [value];
}

class OutletAddressChanged extends CreateOutletEvent {
  final String value;
  const OutletAddressChanged(this.value);
  @override
  List<Object?> get props => [value];
}

class OutletTelChanged extends CreateOutletEvent {
  final String value;
  const OutletTelChanged(this.value);
  @override
  List<Object?> get props => [value];
}

class OutletNicNoChanged extends CreateOutletEvent {
  final String value;
  const OutletNicNoChanged(this.value);
  @override
  List<Object?> get props => [value];
}

class OutletTypeChanged extends CreateOutletEvent {
  final String value;
  const OutletTypeChanged(this.value);
  @override
  List<Object?> get props => [value];
}

class OutletCategoryChanged extends CreateOutletEvent {
  final String value;
  const OutletCategoryChanged(this.value);
  @override
  List<Object?> get props => [value];
}

class OutletEmailChanged extends CreateOutletEvent {
  final String value;
  const OutletEmailChanged(this.value);
  @override
  List<Object?> get props => [value];
}

class OutletContactPersonChanged extends CreateOutletEvent {
  final String value;
  const OutletContactPersonChanged(this.value);
  @override
  List<Object?> get props => [value];
}

class OutletVatNoChanged extends CreateOutletEvent {
  final String value;
  const OutletVatNoChanged(this.value);
  @override
  List<Object?> get props => [value];
}

class OutletCreditLimitChanged extends CreateOutletEvent {
  final String value;
  const OutletCreditLimitChanged(this.value);
  @override
  List<Object?> get props => [value];
}

class OutletRemarksChanged extends CreateOutletEvent {
  final String value;
  const OutletRemarksChanged(this.value);
  @override
  List<Object?> get props => [value];
}

class OutletOwnerDOBChanged extends CreateOutletEvent {
  final DateTime? value;
  const OutletOwnerDOBChanged(this.value);
  @override
  List<Object?> get props => [value];
}

class OutletImageChanged extends CreateOutletEvent {
  final String value;
  const OutletImageChanged(this.value);
  @override
  List<Object?> get props => [value];
}

class OutletProvinceChanged extends CreateOutletEvent {
  final int? code;
  const OutletProvinceChanged(this.code);
  @override
  List<Object?> get props => [code];
}

class OutletDistrictChanged extends CreateOutletEvent {
  final int? code;
  const OutletDistrictChanged(this.code);
  @override
  List<Object?> get props => [code];
}

class OutletLocationCaptureRequested extends CreateOutletEvent {
  const OutletLocationCaptureRequested();
}

class OutletLatLngManualChanged extends CreateOutletEvent {
  final double? lat;
  final double? lng;
  const OutletLatLngManualChanged({this.lat, this.lng});
  @override
  List<Object?> get props => [lat, lng];
}

class CreateOutletSubmitRequested extends CreateOutletEvent {
  const CreateOutletSubmitRequested();
}
