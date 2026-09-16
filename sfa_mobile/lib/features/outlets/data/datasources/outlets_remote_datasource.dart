import 'package:dio/dio.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/network/api_response.dart';
import 'package:uswatte/features/outlets/data/models/outlet_model.dart';
import 'package:uswatte/features/outlets/domain/entities/proximity_policy.dart';

class OutletsRemoteDatasource {
  final Dio _dio;

  const OutletsRemoteDatasource(this._dio);

  Future<({List<OutletModel> outlets, ProximityPolicy policy})>
      getOutletsByRoute(int routeId) async {
    try {
      final response = await _dio.get('/api/v1/outlets/by-route/$routeId');

      final body = response.data as Map<String, dynamic>;
      final data = body['data'] as Map<String, dynamic>;
      final rawList = data['outlets'] as List<dynamic>;
      final radiusMeters = (data['geofenceRadiusMeters'] as num).toDouble();

      // Read defensively: a server that predates proximity exemptions sends none
      // of these, and the absent-field default must be "geofence on". Anything
      // laxer would turn an old build talking to an old server into a bypass.
      final enforced = data['geofenceEnforced'] as bool? ?? true;
      final enforcedFromRaw = data['geofenceEnforcedFrom'] as String?;

      return (
        outlets: rawList
            .map((e) => OutletModel.fromJson(e as Map<String, dynamic>))
            .toList(),
        policy: ProximityPolicy(
          enforced: enforced,
          radiusMeters: radiusMeters,
          enforcedFrom:
              enforcedFromRaw == null ? null : DateTime.tryParse(enforcedFromRaw),
          exemptionReason: data['exemptionReason'] as String?,
        ),
      );
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
      throw const ParseException(
          message: 'Failed to read outlet data from server.');
    }
  }
}
