import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:uswatte/features/products/domain/repositories/product_categories_repository.dart';
import 'package:uswatte/features/products/domain/usecases/sync_product_categories_usecase.dart';

class _MockRepo extends Mock implements ProductCategoriesRepository {}

void main() {
  final now = DateTime.utc(2026, 9, 17, 10);
  const maxAge = Duration(minutes: 30);
  late _MockRepo repo;
  late SyncProductCategoriesUseCase useCase;

  setUp(() {
    repo = _MockRepo();
    useCase = SyncProductCategoriesUseCase(repo, clock: () => now);
    when(
      () => repo.syncCategories(force: any(named: 'force')),
    ).thenAnswer((_) async => (const <Never>[], now));
  });

  test('skips the sync when the last one is recent', () async {
    when(
      () => repo.getLastSyncedAt(),
    ).thenAnswer((_) async => now.subtract(const Duration(minutes: 5)));
    expect(await useCase.syncIfStale(maxAge), isFalse);
    verifyNever(() => repo.syncCategories(force: any(named: 'force')));
  });

  test('syncs when the last one is older than maxAge', () async {
    when(
      () => repo.getLastSyncedAt(),
    ).thenAnswer((_) async => now.subtract(const Duration(minutes: 31)));
    expect(await useCase.syncIfStale(maxAge), isTrue);
    verify(() => repo.syncCategories()).called(1);
  });

  test('syncs when never synced', () async {
    when(() => repo.getLastSyncedAt()).thenAnswer((_) async => null);
    expect(await useCase.syncIfStale(maxAge), isTrue);
  });

  test('syncs when the stamp is in the future (clock skew)', () async {
    when(
      () => repo.getLastSyncedAt(),
    ).thenAnswer((_) async => now.add(const Duration(hours: 2)));
    expect(await useCase.syncIfStale(maxAge), isTrue);
  });

  test('syncs when the stamp cannot be read', () async {
    when(() => repo.getLastSyncedAt()).thenThrow(Exception('db'));
    expect(await useCase.syncIfStale(maxAge), isTrue);
  });

  test('a failing (e.g. offline) sync propagates like call() does', () async {
    when(() => repo.getLastSyncedAt()).thenAnswer((_) async => null);
    when(
      () => repo.syncCategories(force: any(named: 'force')),
    ).thenThrow(Exception('offline'));
    await expectLater(useCase.syncIfStale(maxAge), throwsException);
  });
}
