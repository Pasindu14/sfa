import 'package:uswatte/features/auth/domain/entities/auth_token.dart';
import 'package:uswatte/features/auth/domain/repositories/auth_repository.dart';

class GetCurrentAuthUseCase {
  final AuthRepository _repository;

  const GetCurrentAuthUseCase(this._repository);

  Future<AuthToken?> call() => _repository.getStoredToken();
}
