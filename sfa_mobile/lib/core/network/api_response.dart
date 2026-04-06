/// Matches the SFA API envelope:
/// { "success": true, "data": {...}, "pagination": null, "traceId": "..." }
class ApiResponse<T> {
  final bool success;
  final T? data;
  final String traceId;

  const ApiResponse({
    required this.success,
    required this.traceId,
    this.data,
  });

  factory ApiResponse.fromJson(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic> json)? fromJsonT,
  ) {
    return ApiResponse(
      success: json['success'] as bool,
      traceId: json['traceId'] as String? ?? '',
      data: fromJsonT != null && json['data'] != null
          ? fromJsonT(json['data'] as Map<String, dynamic>)
          : null,
    );
  }
}
