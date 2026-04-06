class AppException implements Exception {
  final String code;
  final String message;

  const AppException({required this.code, required this.message});

  @override
  String toString() => 'AppException($code): $message';
}

class NetworkException extends AppException {
  const NetworkException({required super.message})
      : super(code: 'NETWORK_ERROR');
}

class UnauthorizedException extends AppException {
  const UnauthorizedException()
      : super(code: 'UNAUTHORIZED', message: 'Session expired. Please log in again.');
}

class ServerException extends AppException {
  const ServerException({required super.code, required super.message});
}
