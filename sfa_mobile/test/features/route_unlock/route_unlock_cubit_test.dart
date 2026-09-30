// Rep-side unlock flow: status loading, the self-heal re-sync when the server
// says Approved but the device still enforces, error mapping, and the offline
// short-circuit (no request is attempted without a connection).
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:uswatte/core/connectivity/connectivity_service.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/features/route_unlock/domain/entities/route_unlock_request.dart';
import 'package:uswatte/features/route_unlock/domain/usecases/cancel_route_unlock_usecase.dart';
import 'package:uswatte/features/route_unlock/domain/usecases/get_today_unlock_request_usecase.dart';
import 'package:uswatte/features/route_unlock/domain/usecases/request_route_unlock_usecase.dart';
import 'package:uswatte/features/route_unlock/presentation/cubit/route_unlock_cubit.dart';
import 'package:uswatte/features/route_unlock/presentation/route_unlock_messages.dart';

import 'route_unlock_fixtures.dart';

class _MockGetToday extends Mock implements GetTodayUnlockRequestUseCase {}

class _MockRequest extends Mock implements RequestRouteUnlockUseCase {}

class _MockCancel extends Mock implements CancelRouteUnlockUseCase {}

class _MockConnectivity extends Mock implements ConnectivityService {}

void main() {
  late _MockGetToday getToday;
  late _MockRequest request;
  late _MockCancel cancel;
  late _MockConnectivity connectivity;
  late bool enforced;
  late List<RouteUnlockRequest?> resyncs;

  RouteUnlockCubit build() => RouteUnlockCubit(
        getToday: getToday,
        requestUnlock: request,
        cancelUnlock: cancel,
        connectivity: connectivity,
        isProximityEnforced: () => enforced,
        onResyncNeeded: resyncs.add,
      );

  setUp(() {
    getToday = _MockGetToday();
    request = _MockRequest();
    cancel = _MockCancel();
    connectivity = _MockConnectivity();
    enforced = true;
    resyncs = [];
    when(() => connectivity.hasInternet()).thenAnswer((_) async => true);
  });

  group('load', () {
    blocTest<RouteUnlockCubit, RouteUnlockState>(
      'pending request is shown and triggers no re-sync',
      setUp: () => when(() => getToday()).thenAnswer((_) async => unlock()),
      build: build,
      act: (c) => c.load(),
      verify: (c) {
        expect(c.state.loaded, isTrue);
        expect(c.state.isPending, isTrue);
        expect(c.state.canRequest, isFalse);
        expect(resyncs, isEmpty);
      },
    );

    blocTest<RouteUnlockCubit, RouteUnlockState>(
      'approved + still enforced on device asks for a re-sync, once per request',
      setUp: () => when(() => getToday()).thenAnswer((_) async =>
          unlock(status: RouteUnlockStatus.approved, live: true)),
      build: build,
      act: (c) async {
        await c.load();
        await c.load(); // picker reopened before the sync landed
      },
      verify: (c) {
        expect(c.state.isLive, isTrue);
        expect(resyncs, hasLength(1));
        expect(resyncs.single!.routeId, 3);
        expect(resyncs.single!.routeName, 'Kandy Town A');
      },
    );

    blocTest<RouteUnlockCubit, RouteUnlockState>(
      'approved and already exempt on device needs no re-sync',
      setUp: () {
        enforced = false;
        when(() => getToday()).thenAnswer((_) async =>
            unlock(status: RouteUnlockStatus.approved, live: true));
      },
      build: build,
      act: (c) => c.load(),
      verify: (_) => expect(resyncs, isEmpty),
    );

    blocTest<RouteUnlockCubit, RouteUnlockState>(
      'an approval the server now reports Expired does not re-sync',
      setUp: () => when(() => getToday()).thenAnswer((_) async => unlock(
          status: RouteUnlockStatus.approved,
          effectiveStatus: RouteUnlockStatus.expired)),
      build: build,
      act: (c) => c.load(),
      verify: (c) {
        expect(c.state.request!.isExpired, isTrue);
        expect(c.state.canRequest, isTrue);
        expect(resyncs, isEmpty);
      },
    );

    blocTest<RouteUnlockCubit, RouteUnlockState>(
      'rejected request keeps reviewer + note and allows a new request',
      setUp: () => when(() => getToday()).thenAnswer((_) async => unlock(
          status: RouteUnlockStatus.rejected,
          reviewedByName: 'Nimal S',
          reviewNote: 'Stay on route')),
      build: build,
      act: (c) => c.load(),
      verify: (c) {
        expect(c.state.request!.isRejected, isTrue);
        expect(c.state.request!.reviewNote, 'Stay on route');
        expect(c.state.canRequest, isTrue);
        expect(resyncs, isEmpty);
      },
    );

    blocTest<RouteUnlockCubit, RouteUnlockState>(
      'no request today leaves a clean slate',
      setUp: () => when(() => getToday()).thenAnswer((_) async => null),
      build: build,
      act: (c) => c.load(),
      verify: (c) {
        expect(c.state.loaded, isTrue);
        expect(c.state.request, isNull);
        expect(c.state.canRequest, isTrue);
      },
    );

    blocTest<RouteUnlockCubit, RouteUnlockState>(
      'a failed fetch is silent',
      setUp: () => when(() => getToday())
          .thenThrow(const NetworkException(message: 'No internet connection.')),
      build: build,
      act: (c) => c.load(),
      verify: (c) {
        expect(c.state.loaded, isFalse);
        expect(c.state.loading, isFalse);
        expect(c.state.error, isNull);
      },
    );
  });

  group('requestUnlock', () {
    blocTest<RouteUnlockCubit, RouteUnlockState>(
      'offline: shows the offline message and never calls the API',
      setUp: () =>
          when(() => connectivity.hasInternet()).thenAnswer((_) async => false),
      build: build,
      act: (c) async {
        final result = await c.requestUnlock(reason: 'GPS not accurate');
        expect(result, routeUnlockOfflineMessage);
      },
      verify: (c) {
        expect(c.state.error, routeUnlockOfflineMessage);
        verifyNever(() => request(
              reason: any(named: 'reason'),
              latitude: any(named: 'latitude'),
              longitude: any(named: 'longitude'),
              gpsAccuracyMeters: any(named: 'gpsAccuracyMeters'),
            ));
      },
    );

    blocTest<RouteUnlockCubit, RouteUnlockState>(
      'sends trimmed reason and GPS, then shows the pending request',
      setUp: () => when(() => request(
            reason: any(named: 'reason'),
            latitude: any(named: 'latitude'),
            longitude: any(named: 'longitude'),
            gpsAccuracyMeters: any(named: 'gpsAccuracyMeters'),
          )).thenAnswer((_) async => unlock()),
      build: build,
      act: (c) async {
        final result = await c.requestUnlock(
          reason: '  GPS not accurate ',
          latitude: 7.29,
          longitude: 80.63,
          gpsAccuracyMeters: 12.5,
        );
        expect(result, isNull);
      },
      verify: (c) {
        verify(() => request(
              reason: 'GPS not accurate',
              latitude: 7.29,
              longitude: 80.63,
              gpsAccuracyMeters: 12.5,
            )).called(1);
        expect(c.state.isPending, isTrue);
        expect(c.state.submitting, isFalse);
        expect(c.state.error, isNull);
      },
    );

    blocTest<RouteUnlockCubit, RouteUnlockState>(
      'maps the daily-limit code to a friendly message',
      setUp: () => when(() => request(
            reason: any(named: 'reason'),
            latitude: any(named: 'latitude'),
            longitude: any(named: 'longitude'),
            gpsAccuracyMeters: any(named: 'gpsAccuracyMeters'),
          )).thenThrow(const BusinessRuleException(
              code: 'ROUTE_UNLOCK_DAILY_LIMIT', message: 'raw server text')),
      build: build,
      act: (c) => c.requestUnlock(reason: 'GPS not accurate'),
      verify: (c) {
        expect(c.state.error, contains('limit'));
        expect(c.state.error, isNot(contains('raw server text')));
        expect(resyncs, isEmpty);
      },
    );

    blocTest<RouteUnlockCubit, RouteUnlockState>(
      'ALREADY_EXEMPT asks for a re-sync without a request',
      setUp: () => when(() => request(
            reason: any(named: 'reason'),
            latitude: any(named: 'latitude'),
            longitude: any(named: 'longitude'),
            gpsAccuracyMeters: any(named: 'gpsAccuracyMeters'),
          )).thenThrow(const BusinessRuleException(
              code: 'ROUTE_UNLOCK_ALREADY_EXEMPT', message: 'x')),
      build: build,
      act: (c) => c.requestUnlock(reason: 'GPS not accurate'),
      verify: (_) => expect(resyncs, [null]),
    );

    blocTest<RouteUnlockCubit, RouteUnlockState>(
      'ALREADY_OPEN reloads to show the open request',
      setUp: () {
        when(() => request(
              reason: any(named: 'reason'),
              latitude: any(named: 'latitude'),
              longitude: any(named: 'longitude'),
              gpsAccuracyMeters: any(named: 'gpsAccuracyMeters'),
            )).thenThrow(const ConflictException(
            code: 'ROUTE_UNLOCK_ALREADY_OPEN', message: 'x'));
        when(() => getToday()).thenAnswer((_) async => unlock());
      },
      build: build,
      act: (c) => c.requestUnlock(reason: 'GPS not accurate'),
      verify: (c) {
        verify(() => getToday()).called(1);
        expect(c.state.isPending, isTrue);
      },
    );
  });

  group('cancel', () {
    blocTest<RouteUnlockCubit, RouteUnlockState>(
      'sends the rowVersion and shows the cancelled request',
      setUp: () {
        when(() => getToday()).thenAnswer((_) async => unlock(rowVersion: 42));
        when(() => cancel(12, 42)).thenAnswer((_) async =>
            unlock(status: RouteUnlockStatus.cancelled, rowVersion: 43));
      },
      build: build,
      act: (c) async {
        await c.load();
        expect(await c.cancel(), isNull);
      },
      verify: (c) {
        verify(() => cancel(12, 42)).called(1);
        expect(c.state.request!.isCancelled, isTrue);
        expect(c.state.canRequest, isTrue);
      },
    );

    blocTest<RouteUnlockCubit, RouteUnlockState>(
      'a request decided meanwhile reloads to its new status',
      setUp: () {
        var calls = 0;
        when(() => getToday()).thenAnswer((_) async => calls++ == 0
            ? unlock()
            : unlock(
                status: RouteUnlockStatus.rejected, reviewedByName: 'Nimal S'));
        when(() => cancel(any(), any())).thenThrow(const ConflictException(
            code: 'ROUTE_UNLOCK_INVALID_STATE', message: 'x'));
      },
      build: build,
      act: (c) async {
        await c.load();
        await c.cancel();
      },
      verify: (c) {
        expect(c.state.request!.isRejected, isTrue);
        expect(c.state.error, isNotNull);
      },
    );
  });
}
