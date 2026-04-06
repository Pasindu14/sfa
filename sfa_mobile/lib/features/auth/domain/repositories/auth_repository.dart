import 'package:uswatte/features/auth/domain/entities/auth_token.dart';

abstract interface class AuthRepository {
  Future<AuthToken> login({
    required String username,
    required String password,
    required String deviceId,
  });
  Future<void> logout();
  Future<AuthToken?> getStoredToken();
}
