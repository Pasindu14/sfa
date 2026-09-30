// The outlet picker's unlock status strip: what the rep reads while a request
// is pending, after a rejection, and after a revoke — and which action it offers.
import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:mocktail/mocktail.dart';
import 'package:uswatte/core/connectivity/connectivity_service.dart';
import 'package:uswatte/features/route_unlock/domain/entities/route_unlock_request.dart';
import 'package:uswatte/features/route_unlock/domain/usecases/cancel_route_unlock_usecase.dart';
import 'package:uswatte/features/route_unlock/domain/usecases/get_today_unlock_request_usecase.dart';
import 'package:uswatte/features/route_unlock/domain/usecases/request_route_unlock_usecase.dart';
import 'package:uswatte/features/route_unlock/presentation/cubit/route_unlock_cubit.dart';
import 'package:uswatte/features/route_unlock/presentation/widgets/route_unlock_widgets.dart';

import 'route_unlock_fixtures.dart';

class _MockGetToday extends Mock implements GetTodayUnlockRequestUseCase {}

class _MockRequest extends Mock implements RequestRouteUnlockUseCase {}

class _MockCancel extends Mock implements CancelRouteUnlockUseCase {}

class _MockConnectivity extends Mock implements ConnectivityService {}

Future<RouteUnlockCubit> _cubitWith(RouteUnlockRequest? today) async {
  final getToday = _MockGetToday();
  when(() => getToday()).thenAnswer((_) async => today);
  final connectivity = _MockConnectivity();
  when(() => connectivity.hasInternet()).thenAnswer((_) async => true);
  final cubit = RouteUnlockCubit(
    getToday: getToday,
    requestUnlock: _MockRequest(),
    cancelUnlock: _MockCancel(),
    connectivity: connectivity,
    isProximityEnforced: () => true,
    onResyncNeeded: (_) {},
  );
  await cubit.load();
  return cubit;
}

Future<void> _pump(WidgetTester tester, RouteUnlockCubit cubit,
    {VoidCallback? onRequest}) async {
  await tester.binding.setSurfaceSize(const Size(390, 844));
  addTearDown(() => tester.binding.setSurfaceSize(null));

  // flutter_test's square-glyph font measures labels far wider than the real
  // faces; layout is not what this guards, so drop overflow reports only.
  final defaultOnError = FlutterError.onError!;
  FlutterError.onError = (details) {
    if (details.exceptionAsString().contains('A RenderFlex overflowed')) return;
    defaultOnError(details);
  };
  addTearDown(() => FlutterError.onError = defaultOnError);

  await tester.pumpWidget(
    ScreenUtilInit(
      designSize: const Size(390, 844),
      builder: (context, child) => MaterialApp(
        home: Scaffold(
          body: RouteUnlockStrip(cubit: cubit, onRequest: onRequest ?? () {}),
        ),
      ),
    ),
  );
  await tester.pump();
}

void main() {
  setUpAll(() => GoogleFonts.config.allowRuntimeFetching = false);

  testWidgets('pending: waiting for the supervisor, with Cancel',
      (tester) async {
    final cubit = await tester.runAsync(() => _cubitWith(unlock()));
    await _pump(tester, cubit!);

    expect(find.text('Waiting for Nimal S'), findsOneWidget);
    expect(find.text('Cancel'), findsOneWidget);
    expect(find.text('Request again'), findsNothing);
    await cubit.close();
  });

  testWidgets('pending with no supervisor: waiting for approval',
      (tester) async {
    final cubit =
        await tester.runAsync(() => _cubitWith(unlock(supervisorName: null)));
    await _pump(tester, cubit!);

    expect(find.text('Waiting for approval'), findsOneWidget);
    await cubit.close();
  });

  testWidgets('rejected: who and why, with Request again', (tester) async {
    var requested = 0;
    final cubit = await tester.runAsync(() => _cubitWith(unlock(
          status: RouteUnlockStatus.rejected,
          reviewedByName: 'Nimal S',
          reviewNote: 'Stay on route',
        )));
    await _pump(tester, cubit!, onRequest: () => requested++);

    expect(find.text('Rejected by Nimal S: Stay on route'), findsOneWidget);
    await tester.tap(find.text('Request again'));
    expect(requested, 1);
    await cubit.close();
  });

  testWidgets('revoked and expired offer Request again', (tester) async {
    final revoked = await tester
        .runAsync(() => _cubitWith(unlock(status: RouteUnlockStatus.revoked)));
    await _pump(tester, revoked!);
    expect(find.text('Unlock revoked'), findsOneWidget);
    expect(find.text('Request again'), findsOneWidget);
    await revoked.close();

    final expired = await tester.runAsync(() => _cubitWith(unlock(
        status: RouteUnlockStatus.pending,
        effectiveStatus: RouteUnlockStatus.expired)));
    await _pump(tester, expired!);
    expect(find.text('Request expired'), findsOneWidget);
    expect(find.text('Request again'), findsOneWidget);
    await expired.close();
  });

  testWidgets('no request today renders nothing', (tester) async {
    final cubit = await tester.runAsync(() => _cubitWith(null));
    await _pump(tester, cubit!);

    expect(find.byKey(const ValueKey('route-unlock-strip')), findsNothing);
    await cubit.close();
  });
}
