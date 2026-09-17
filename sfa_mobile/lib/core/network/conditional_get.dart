import 'package:dio/dio.dart';

/// Outcome of a GET that may have been sent with `If-None-Match`.
sealed class ConditionalResponse<T> {
  const ConditionalResponse();
}

/// Server answered 304: the local copy matching the sent ETag is still current.
final class NotModified<T> extends ConditionalResponse<T> {
  const NotModified();
}

/// Server answered 2xx with a body. [etag] is null when the server sent none
/// (older API builds), in which case the caller must forget any stored ETag.
final class Fetched<T> extends ConditionalResponse<T> {
  final T data;
  final String? etag;
  const Fetched(this.data, this.etag);
}

/// Request options for a conditional GET.
///
/// Returns null when there is no ETag to send, so the request is exactly what
/// it was before conditional GETs existed (Dio's default status validation,
/// no extra headers). With an ETag, 304 is accepted as a success status on
/// *this* request only — Dio's default rejects it as an error, which would
/// route it through the error interceptors and the datasource's catch blocks.
///
/// Both the header and the validator live on the resulting RequestOptions, so
/// TokenInterceptor's 401 → refresh → `fetch(err.requestOptions)` retry keeps
/// them.
Options? conditionalGetOptions(String? ifNoneMatch) {
  if (ifNoneMatch == null || ifNoneMatch.isEmpty) return null;
  return Options(
    headers: {'If-None-Match': ifNoneMatch},
    validateStatus: (status) =>
        status != null && ((status >= 200 && status < 300) || status == 304),
  );
}

/// The response's ETag header, or null when absent/blank.
String? readEtag(Response<dynamic> response) {
  final values = response.headers['etag'];
  if (values == null || values.isEmpty) return null;
  final value = values.first.trim();
  return value.isEmpty ? null : value;
}
