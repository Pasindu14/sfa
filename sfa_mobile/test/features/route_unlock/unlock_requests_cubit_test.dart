// Supervisor queue: today's requests split into Pending and decisions, the
// rowVersion goes back on every decision, and a stale copy (someone else acted
// first) reloads the list and tells the sheet to close.
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/features/route_unlock/domain/entities/route_unlock_request.dart';
import 'package:uswatte/features/route_unlock/domain/usecases/approve_unlock_usecase.dart';
import 'package:uswatte/features/route_unlock/domain/usecases/get_unlock_request_detail_usecase.dart';
import 'package:uswatte/features/route_unlock/domain/usecases/get_unlock_requests_usecase.dart';
import 'package:uswatte/features/route_unlock/domain/usecases/reject_unlock_usecase.dart';
import 'package:uswatte/features/route_unlock/domain/usecases/revoke_unlock_usecase.dart';
import 'package:uswatte/features/route_unlock/presentation/cubit/unlock_requests_cubit.dart';

import 'route_unlock_fixtures.dart';

class _MockList extends Mock implements GetUnlockRequestsUseCase {}

class _MockDetail extends Mock implements GetUnlockRequestDetailUseCase {}

class _MockApprove extends Mock implements ApproveUnlockUseCase {}

class _MockReject extends Mock implements RejectUnlockUseCase {}

class _MockRevoke extends Mock implements RevokeUnlockUseCase {}

void main() {
  late _MockList list;
  late _MockApprove approve;
  late _MockReject reject;
  late UnlockRequestsCubit cubit;

  void stubList(List<RouteUnlockRequest> items) => when(() => list(
        status: any(named: 'status'),
        from: any(named: 'from'),
        to: any(named: 'to'),
        search: any(named: 'search'),
        page: any(named: 'page'),
        pageSize: any(named: 'pageSize'),
      )).thenAnswer((_) async => items);

  setUp(() {
    list = _MockList();
    approve = _MockApprove();
    reject = _MockReject();
    cubit = UnlockRequestsCubit(
      getRequests: list,
      getDetail: _MockDetail(),
      approve: approve,
      reject: reject,
      revoke: _MockRevoke(),
    );
  });

  tearDown(() => cubit.close());

  test('splits today into pending and decided', () async {
    stubList([
      unlock(id: 1),
      unlock(id: 2, status: RouteUnlockStatus.rejected),
      unlock(id: 3, status: RouteUnlockStatus.approved, live: true),
      unlock(
          id: 4,
          status: RouteUnlockStatus.pending,
          effectiveStatus: RouteUnlockStatus.expired),
    ]);

    await cubit.load();

    expect(cubit.state.loaded, isTrue);
    expect(cubit.state.pending.map((r) => r.id), [1]);
    expect(cubit.state.decided.map((r) => r.id), containsAll([2, 3, 4]));
  });

  test('approve sends the rowVersion and the note, then reloads', () async {
    stubList([unlock(id: 1, rowVersion: 77)]);
    when(() => approve(1, 77, note: 'ok')).thenAnswer(
        (_) async => unlock(id: 1, status: RouteUnlockStatus.approved));

    final result = await cubit.approve(unlock(id: 1, rowVersion: 77), note: 'ok');

    expect(result.success, isTrue);
    verify(() => approve(1, 77, note: 'ok')).called(1);
    expect(cubit.state.loaded, isTrue);
    expect(cubit.state.actingOn, isNull);
  });

  test('a concurrency conflict reloads and marks the sheet stale', () async {
    stubList([unlock(id: 1, status: RouteUnlockStatus.approved, live: true)]);
    when(() => reject(1, 5, 'no')).thenThrow(
        const ConflictException(code: 'CONCURRENCY_CONFLICT', message: 'x'));

    final result = await cubit.reject(unlock(id: 1, rowVersion: 5), 'no');

    expect(result.success, isFalse);
    expect(result.stale, isTrue);
    expect(cubit.state.pending, isEmpty,
        reason: 'the reload shows it was already approved');
    expect(cubit.state.decided.single.id, 1);
  });

  test('a busy lock is not stale: the sheet stays open to retry', () async {
    when(() => reject(1, 5, 'no')).thenThrow(
        const ConflictException(code: 'ROUTE_UNLOCK_BUSY', message: 'x'));

    final result = await cubit.reject(unlock(id: 1, rowVersion: 5), 'no');

    expect(result.success, isFalse);
    expect(result.stale, isFalse);
    expect(result.message, contains('Try again'));
  });
}
