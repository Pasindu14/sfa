import 'package:dio/dio.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/network/api_response.dart';
import 'package:uswatte/core/network/conditional_get.dart';
import 'package:uswatte/features/products/data/models/product_model.dart';

class ProductsRemoteDatasource {
  final Dio _dio;

  const ProductsRemoteDatasource(this._dio);

  /// Fetches the product list. With [ifNoneMatch] the request is conditional
  /// and a 304 comes back as [NotModified]; without it the request is sent
  /// exactly as before conditional GETs existed.
  Future<ConditionalResponse<ProductListResponseModel>> getProducts({
    String? ifNoneMatch,
  }) async {
    try {
      final options = conditionalGetOptions(ifNoneMatch);
      final response = await _dio.get(
        '/api/v1/mobile/products',
        options: options,
      );

      if (options != null && response.statusCode == 304) {
        return const NotModified();
      }

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

      return Fetched(data, readEtag(response));
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
