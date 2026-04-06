import 'package:uswatte/features/auth/data/datasources/auth_local_datasource.dart';
import 'package:uswatte/features/auth/data/datasources/auth_remote_datasource.dart';
import 'package:uswatte/features/auth/domain/entities/auth_token.dart';
import 'package:uswatte/features/auth/domain/repositories/auth_repository.dart';

class AuthRepositoryImpl implements AuthRepository {
  final AuthRemoteDatasource _remote;
  final AuthLocalDatasource _local;

  const AuthRepositoryImpl(this._remote, this._local);

  @override
  Future<AuthToken> login({
    required String username,
    required String password,
    required String deviceId,
  }) async {
    final model = await _remote.login(
      username: username,
      password: password,
      deviceId: deviceId,
    );
    final token = model.toEntity();
    await _local.saveToken(token);
    return token;
  }

  @override
  Future<void> logout() => _local.clearToken();

  @override
  Future<AuthToken?> getStoredToken() => _local.getToken();
}
