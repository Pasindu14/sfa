import 'package:uswatte/features/auth/domain/entities/user_role.dart';

class AuthToken {
  final String accessToken;
  final String? refreshToken;
  final UserRole role;

  const AuthToken({
    required this.accessToken,
    this.refreshToken,
    required this.role,
  });
}
