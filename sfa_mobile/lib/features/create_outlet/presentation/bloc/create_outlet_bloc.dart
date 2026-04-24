import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:geolocator/geolocator.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/features/create_outlet/domain/entities/new_outlet.dart';
import 'package:uswatte/features/create_outlet/domain/usecases/create_outlet_usecase.dart';
import 'package:uswatte/features/create_outlet/presentation/bloc/create_outlet_event.dart';
import 'package:uswatte/features/create_outlet/presentation/bloc/create_outlet_state.dart';
import 'package:uswatte/features/outlets/domain/usecases/get_current_route_id_usecase.dart';

class CreateOutletBloc extends Bloc<CreateOutletEvent, CreateOutletState> {
  final CreateOutletUseCase _createOutlet;
  final GetCurrentRouteIdUseCase _getCurrentRouteId;

  CreateOutletBloc({
    required CreateOutletUseCase createOutletUseCase,
    required GetCurrentRouteIdUseCase getCurrentRouteIdUseCase,
  })  : _createOutlet = createOutletUseCase,
        _getCurrentRouteId = getCurrentRouteIdUseCase,
        super(const CreateOutletState()) {
    on<OutletNameChanged>((e, emit) => emit(state.copyWith(name: e.value)));
    on<OutletAddressChanged>((e, emit) => emit(state.copyWith(address: e.value)));
    on<OutletTelChanged>((e, emit) => emit(state.copyWith(tel: e.value)));
    on<OutletNicNoChanged>((e, emit) => emit(state.copyWith(nicNo: e.value)));
    on<OutletTypeChanged>((e, emit) => emit(state.copyWith(outletType: e.value)));
    on<OutletCategoryChanged>((e, emit) => emit(state.copyWith(outletCategory: e.value)));
    on<OutletEmailChanged>((e, emit) => emit(state.copyWith(email: e.value)));
    on<OutletContactPersonChanged>((e, emit) => emit(state.copyWith(contactPerson: e.value)));
    on<OutletVatNoChanged>((e, emit) => emit(state.copyWith(vatNo: e.value)));
    on<OutletCreditLimitChanged>((e, emit) => emit(state.copyWith(creditLimit: e.value)));
    on<OutletRemarksChanged>((e, emit) => emit(state.copyWith(remarks: e.value)));
    on<OutletOwnerDOBChanged>((e, emit) => emit(
          e.value == null
              ? state.copyWith(clearOwnerDOB: true)
              : state.copyWith(ownerDOB: e.value),
        ));
    on<OutletImageChanged>((e, emit) => emit(state.copyWith(image: e.value)));
    on<OutletProvinceChanged>(_onProvinceChanged);
    on<OutletDistrictChanged>((e, emit) => emit(
          e.code == null
              ? state.copyWith(clearDistrict: true)
              : state.copyWith(districtCode: e.code),
        ));
    on<OutletLatLngManualChanged>((e, emit) => emit(state.copyWith(
          latitude: e.lat,
          longitude: e.lng,
        )));
    on<OutletLocationCaptureRequested>(_onLocationCapture);
    on<CreateOutletSubmitRequested>(_onSubmit);
  }

  void _onProvinceChanged(OutletProvinceChanged event, Emitter<CreateOutletState> emit) {
    if (event.code == null) {
      emit(state.copyWith(clearProvince: true, clearDistrict: true));
    } else {
      emit(state.copyWith(provinceCode: event.code, clearDistrict: true));
    }
  }

  Future<void> _onLocationCapture(
    OutletLocationCaptureRequested event,
    Emitter<CreateOutletState> emit,
  ) async {
    emit(state.copyWith(isLocating: true, clearSubmitError: true));
    try {
      // Check if device location services (GPS) are switched on
      final serviceEnabled = await Geolocator.isLocationServiceEnabled();
      if (!serviceEnabled) {
        emit(state.copyWith(
          isLocating: false,
          submitError: 'Location services are disabled. Please turn on GPS and try again.',
        ));
        return;
      }

      // Check / request permission
      var permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
      }
      if (permission == LocationPermission.denied ||
          permission == LocationPermission.deniedForever) {
        emit(state.copyWith(
          isLocating: false,
          submitError: permission == LocationPermission.deniedForever
              ? 'Location permission permanently denied. Please enable it in app settings.'
              : 'Location permission denied. Please allow it and try again.',
        ));
        return;
      }

      final position = await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(
          accuracy: LocationAccuracy.high,
        ),
      );

      emit(state.copyWith(
        latitude: position.latitude,
        longitude: position.longitude,
        isLocating: false,
      ));
    } on LocationServiceDisabledException {
      emit(state.copyWith(
        isLocating: false,
        submitError: 'Location services are disabled. Please turn on GPS and try again.',
      ));
    } on PermissionDeniedException {
      emit(state.copyWith(
        isLocating: false,
        submitError: 'Location permission denied. Please allow it in app settings.',
      ));
    } catch (e) {
      emit(state.copyWith(
        isLocating: false,
        submitError: 'Could not get location: ${e.runtimeType}. Try again or enter coordinates manually.',
      ));
    }
  }

  Future<void> _onSubmit(
    CreateOutletSubmitRequested event,
    Emitter<CreateOutletState> emit,
  ) async {
    final validationErrors = _validate();
    if (validationErrors.isNotEmpty) {
      emit(state.copyWith(errors: validationErrors));
      return;
    }

    emit(state.copyWith(isSubmitting: true, errors: const {}, clearSubmitError: true));

    try {
      final routeId = await _getCurrentRouteId();
      if (routeId == null) {
        emit(state.copyWith(
          isSubmitting: false,
          submitError: 'No active route assignment. Cannot register outlet.',
        ));
        return;
      }

      final outlet = NewOutlet(
        name: state.name.trim(),
        address: state.address.trim(),
        tel: state.tel.trim(),
        nicNo: state.nicNo.trim(),
        outletType: state.outletType,
        outletCategory: state.outletCategory,
        latitude: state.latitude!,
        longitude: state.longitude!,
        routeId: routeId,
        creditLimit: double.tryParse(state.creditLimit) ?? 0,
        email: state.email.trim().isEmpty ? null : state.email.trim(),
        contactPerson: state.contactPerson.trim().isEmpty ? null : state.contactPerson.trim(),
        vatNo: state.vatNo.trim().isEmpty ? null : state.vatNo.trim(),
        remarks: state.remarks.trim().isEmpty ? null : state.remarks.trim(),
        ownerDOB: state.ownerDOB,
        image: state.image.trim().isEmpty ? null : state.image.trim(),
        provinceCode: state.provinceCode,
        districtCode: state.districtCode,
      );

      await _createOutlet(outlet);
      emit(state.copyWith(isSubmitting: false, submitted: true));
    } on AppException catch (e) {
      emit(state.copyWith(isSubmitting: false, submitError: e.message));
    } catch (_) {
      emit(state.copyWith(
        isSubmitting: false,
        submitError: 'Something went wrong. Please try again.',
      ));
    }
  }

  Map<String, String> _validate() {
    final errors = <String, String>{};
    if (state.name.trim().isEmpty) errors['name'] = 'Outlet name is required.';
    if (state.address.trim().isEmpty) errors['address'] = 'Address is required.';
    if (state.tel.trim().isEmpty) errors['tel'] = 'Phone number is required.';
    if (state.nicNo.trim().isEmpty) errors['nicNo'] = 'NIC number is required.';
    if (state.outletType.isEmpty) errors['outletType'] = 'Select an outlet type.';
    if (state.outletCategory.isEmpty) errors['outletCategory'] = 'Select a category.';
    if (!state.hasLocation) errors['location'] = 'Location is required. Tap "Use My Location".';
    return errors;
  }
}
