import 'package:dio/dio.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/network/api_response.dart';
import 'package:uswatte/features/create_outlet/domain/entities/new_outlet.dart';

class CreateOutletRemoteDatasource {
  final Dio _dio;

  const CreateOutletRemoteDatasource(this._dio);

  Future<void> createOutlet(NewOutlet outlet) async {
    try {
      await _dio.post('/api/v1/outlets', data: _toJson(outlet));
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      final interceptorError = e.error;
      if (interceptorError is AppException) throw interceptorError;

      final statusCode = e.response?.statusCode ?? 500;
      if (e.response?.data is Map<String, dynamic>) {
        final body = e.response!.data as Map<String, dynamic>;
        final errorJson = body['error'];
        if (errorJson is Map<String, dynamic>) {
          throw ApiError.fromJson(errorJson).toException(statusCode);
        }
      }

      final message = switch (e.type) {
        DioExceptionType.connectionTimeout ||
        DioExceptionType.sendTimeout =>
          'Connection timed out. Check your network.',
        DioExceptionType.receiveTimeout => 'Server took too long to respond.',
        DioExceptionType.connectionError => 'No internet connection.',
        DioExceptionType.cancel => 'Request was cancelled.',
        _ => 'Network error. Please try again.',
      };
      throw NetworkException(message: message);
    } catch (_) {
      throw const NetworkException(message: 'Failed to register outlet.');
    }
  }

  Map<String, dynamic> _toJson(NewOutlet o) => {
        'name': o.name,
        'address': o.address,
        'tel': o.tel,
        'nicNo': o.nicNo,
        'outletType': o.outletType,
        'outletCategory': o.outletCategory,
        'latitude': o.latitude,
        'longitude': o.longitude,
        'routeId': o.routeId,
        'creditLimit': o.creditLimit,
        if (o.email != null) 'email': o.email,
        if (o.contactPerson != null) 'contactPerson': o.contactPerson,
        if (o.vatNo != null) 'vatNo': o.vatNo,
        if (o.remarks != null) 'remarks': o.remarks,
        if (o.ownerDOB != null) 'ownerDOB': o.ownerDOB!.toUtc().toIso8601String(),
        if (o.image != null) 'image': o.image,
        if (o.provinceCode != null) 'provinceCode': o.provinceCode,
        if (o.districtCode != null) 'districtCode': o.districtCode,
      };
}
