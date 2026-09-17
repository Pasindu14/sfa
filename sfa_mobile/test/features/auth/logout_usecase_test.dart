import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:uswatte/features/auth/domain/repositories/auth_repository.dart';
import 'package:uswatte/features/auth/domain/usecases/logout_usecase.dart';

class _MockRepo extends Mock implements AuthRepository {}

void main() {
  test('logout runs the session cleanup (ETag wipe) after clearing tokens',
      () async {
    final repo = _MockRepo();
    final calls = <String>[];
    when(() => repo.logout()).thenAnswer((_) async => calls.add('tokens'));

    await LogoutUseCase(repo, onLoggedOut: () async => calls.add('etags'))();

    expect(calls, ['tokens', 'etags']);
  });

  test(
      'cleanup still runs when clearing tokens fails, and its own failure '
      'does not replace the original error', () async {
    final repo = _MockRepo();
    var cleaned = false;
    when(() => repo.logout()).thenThrow(StateError('keystore'));

    await expectLater(
      LogoutUseCase(repo, onLoggedOut: () async {
        cleaned = true;
        throw Exception('db');
      })(),
      throwsStateError,
    );
    expect(cleaned, isTrue);
  });
}
