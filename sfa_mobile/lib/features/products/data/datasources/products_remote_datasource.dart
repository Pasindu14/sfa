import 'package:dio/dio.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/network/api_response.dart';
import 'package:uswatte/features/products/data/models/product_model.dart';

class ProductsRemoteDatasource {
  final Dio _dio;

  const ProductsRemoteDatasource(this._dio);

  Future<ProductListResponseModel> getProducts() async {
    try {
      final response = await _dio.get('/api/v1/mobile/products');

      final apiResponse = ApiResponse.fromJson(
        response.data as Map<String, dynamic>,
        ProductListResponseModel.fromJson,
      );

      final data = apiResponse.data;
      if (data == null) {
        throw const ServerException(
          code: 'INVALID_RESPONSE',
          message: 'Server returned an empty product list.',
        );
      }

      return data;
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      // Re-throw typed exceptions injected by TokenInterceptor (e.g. UnauthorizedException)
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
          message: 'Failed to read product data from server.');
    }
  }
}
