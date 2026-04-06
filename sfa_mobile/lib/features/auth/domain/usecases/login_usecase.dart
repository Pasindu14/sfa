import 'package:uswatte/features/auth/domain/entities/auth_token.dart';
import 'package:uswatte/features/auth/domain/repositories/auth_repository.dart';

class LoginUseCase {
  final AuthRepository _repository;

  const LoginUseCase(this._repository);

  Future<AuthToken> call({
    required String username,
    required String password,
    required String deviceId,
  }) {
    return _repository.login(
      username: username,
      password: password,
      deviceId: deviceId,
    );
  }
}
