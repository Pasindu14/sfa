import 'package:dio/dio.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/network/api_response.dart';
import 'package:uswatte/features/pricing/data/models/pricing_structure_model.dart';

class PricingRemoteDatasource {
  final Dio _dio;

  const PricingRemoteDatasource(this._dio);

  Future<List<PricingStructureModel>> getPricingStructures() async {
    try {
      final response =
          await _dio.get('/api/v1/pricing-structures/active');

      final body = response.data as Map<String, dynamic>;
      final rawList = body['data'] as List<dynamic>;
      return rawList
          .map((e) =>
              PricingStructureModel.fromJson(e as Map<String, dynamic>))
          .toList();
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
          message: 'Failed to read pricing data from server.');
    }
  }
}
