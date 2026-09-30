import 'package:dio/dio.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/network/api_response.dart';
import '../models/route_unlock_models.dart';

class RouteUnlockRemoteDatasource {
  final Dio _dio;
  const RouteUnlockRemoteDatasource(this._dio);

  static const _base = '/api/v1/route-unlock-requests';

  // ── Rep ────────────────────────────────────────────────────────────────────

  Future<RouteUnlockRequestModel?> getMyToday() => _run(
        () => _dio.get('$_base/my/today'),
        (data) => data is Map<String, dynamic>
            ? RouteUnlockRequestModel.fromJson(data)
            : null,
        'Failed to read today\'s unlock request.',
      );

  Future<RouteUnlockRequestModel> request({
    required String reason,
    double? latitude,
    double? longitude,
    double? gpsAccuracyMeters,
  }) =>
      _run(
        () => _dio.post(_base, data: {
          'reason': reason,
          'latitude': latitude,
          'longitude': longitude,
          'gpsAccuracyMeters': gpsAccuracyMeters,
        }),
        _request,
        'Failed to read the unlock request.',
      );

  Future<RouteUnlockRequestModel> cancel(int id, int rowVersion) => _run(
        () => _dio.post('$_base/$id/cancel', data: {'rowVersion': rowVersion}),
        _request,
        'Failed to read the unlock request.',
      );

  // ── Supervisor ─────────────────────────────────────────────────────────────

  Future<List<RouteUnlockRequestModel>> getRequests({
    String? status,
    DateTime? from,
    DateTime? to,
    String? search,
    int page = 1,
    int pageSize = 50,
  }) =>
      _run(
        () => _dio.get(_base, queryParameters: {
          'page': page,
          'pageSize': pageSize,
          if (status != null) 'status': status,
          if (from != null) 'from': _ymd(from),
          if (to != null) 'to': _ymd(to),
          if (search != null && search.isNotEmpty) 'search': search,
        }),
        (data) {
          // Contract: `data` is the array, paging lives in `pagination`. Also
          // accept an `{items: [...]}` wrapper, which some list endpoints use.
          final raw = data is List
              ? data
              : (data is Map<String, dynamic> ? data['items'] : null);
          if (raw is! List) throw const FormatException('list');
          return raw
              .map((e) => RouteUnlockRequestModel.fromJson(e as Map<String, dynamic>))
              .toList();
        },
        'Failed to read unlock requests.',
      );

  Future<int> getPendingCount() => _run(
        () => _dio.get('$_base/pending-count'),
        (data) {
          if (data is num) return data.toInt();
          return ((data as Map<String, dynamic>)['count'] as num).toInt();
        },
        'Failed to read the pending count.',
      );

  Future<RouteUnlockDetailModel> getDetail(int id) => _run(
        () => _dio.get('$_base/$id'),
        (data) => RouteUnlockDetailModel.fromJson(data as Map<String, dynamic>),
        'Failed to read the unlock request.',
      );

  Future<RouteUnlockRequestModel> approve(int id, int rowVersion,
          {String? note}) =>
      _run(
        () => _dio.post('$_base/$id/approve', data: {
          'rowVersion': rowVersion,
          if (note != null && note.isNotEmpty) 'note': note,
        }),
        _request,
        'Failed to read the unlock request.',
      );

  Future<RouteUnlockRequestModel> reject(int id, int rowVersion, String reason) =>
      _run(
        () => _dio.post('$_base/$id/reject',
            data: {'rowVersion': rowVersion, 'reason': reason}),
        _request,
        'Failed to read the unlock request.',
      );

  Future<RouteUnlockRequestModel> revoke(int id, int rowVersion, String reason) =>
      _run(
        () => _dio.post('$_base/$id/revoke',
            data: {'rowVersion': rowVersion, 'reason': reason}),
        _request,
        'Failed to read the unlock request.',
      );

  // ── Plumbing ───────────────────────────────────────────────────────────────

  static RouteUnlockRequestModel _request(Object? data) =>
      RouteUnlockRequestModel.fromJson(data as Map<String, dynamic>);

  static String _ymd(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-'
      '${d.month.toString().padLeft(2, '0')}-'
      '${d.day.toString().padLeft(2, '0')}';

  /// Sends [call], unwraps the envelope's `data` and hands it to [parse].
  Future<T> _run<T>(
    Future<Response<dynamic>> Function() call,
    T Function(Object? data) parse,
    String parseError,
  ) async {
    final Response<dynamic> response;
    try {
      response = await call();
    } on AppException {
      rethrow;
    } on DioException catch (e) {
      throw _mapDioError(e);
    }
    try {
      final body = response.data as Map<String, dynamic>;
      return parse(body['data']);
    } catch (_) {
      throw ParseException(message: parseError);
    }
  }

  AppException _mapDioError(DioException e) {
    final interceptorError = e.error;
    if (interceptorError is AppException) return interceptorError;

    final statusCode = e.response?.statusCode ?? 500;
    if (e.response?.data is Map<String, dynamic>) {
      final body = e.response!.data as Map<String, dynamic>;
      final errorJson = body['error'];
      if (errorJson is Map<String, dynamic>) {
        return ApiError.fromJson(errorJson).toException(statusCode);
      }
      // Flat ApiError (`{code, message, ...}` at the top level).
      if (body['code'] is String) {
        return ApiError.fromJson(body).toException(statusCode);
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
    return NetworkException(message: message);
  }
}
