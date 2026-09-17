// T3.23: the results list is a lazy SliverList under the form. Guards the
// header/empty states, the 16/20/16/40 padding, the 10px gap between cards and
// that off-screen cards are not built until scrolled to.
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
import 'package:uswatte/features/supervisor_not_billing/domain/entities/not_billing_reason.dart';
import 'package:uswatte/features/supervisor_not_billing/domain/entities/not_billing_summary.dart';
import 'package:uswatte/features/supervisor_not_billing/presentation/bloc/supervisor_not_billing_bloc.dart';
import 'package:uswatte/features/supervisor_not_billing/presentation/bloc/supervisor_not_billing_event.dart';
import 'package:uswatte/features/supervisor_not_billing/presentation/bloc/supervisor_not_billing_state.dart';
import 'package:uswatte/features/supervisor_not_billing/presentation/pages/supervisor_not_billing_page.dart';

class _MockBloc
    extends MockBloc<SupervisorNotBillingEvent, SupervisorNotBillingState>
    implements SupervisorNotBillingBloc {}

const _rep = RepSummary(userId: 1, userName: 'Rep One');

NotBillingSummary _nb(int i) => NotBillingSummary(
      id: i,
      outletId: i,
      salesRepId: 1,
      notBillingNumber: 'NB-$i',
      notBillingDate: '2026-09-17',
      outletName: 'Outlet $i',
      salesRepName: 'Rep One',
      reason: NotBillingReason.outletClosed,
      createdAt: DateTime.utc(2026, 9, 17),
    );

Future<void> _pump(WidgetTester tester, SupervisorNotBillingState state) async {
  await tester.binding.setSurfaceSize(const Size(390, 844));
  addTearDown(() => tester.binding.setSurfaceSize(null));
  // flutter_test's square-glyph font fakes RenderFlex overflows (see
  // confirm_bill_sheet_test.dart); drop only those.
  final defaultOnError = FlutterError.onError!;
  FlutterError.onError = (details) {
    if (details.exceptionAsString().contains('A RenderFlex overflowed')) return;
    defaultOnError(details);
  };
  addTearDown(() => FlutterError.onError = defaultOnError);

  final bloc = _MockBloc();
  when(() => bloc.state).thenReturn(state);
  await tester.pumpWidget(
    ScreenUtilInit(
      designSize: const Size(390, 844),
      builder: (context, child) => MaterialApp(
        home: BlocProvider<SupervisorNotBillingBloc>.value(
          value: bloc,
          child: const SupervisorNotBillingPage(),
        ),
      ),
    ),
  );
  await tester.pump();
}

void main() {
  testWidgets('empty results show the header and empty state, no cards',
      (tester) async {
    await _pump(
      tester,
      SupervisorNotBillingReady(
        reps: const [_rep],
        selectedRep: _rep,
        selectedDate: DateTime(2026, 9, 17),
        notBillings: const [],
      ),
    );

    expect(find.text('0 RECORDS · Sep 17, 2026'), findsOneWidget);
    expect(find.textContaining('NB-'), findsNothing);
  });

  testWidgets('cards are lazy, left-aligned in the padding and 10px apart',
      (tester) async {
    await _pump(
      tester,
      SupervisorNotBillingReady(
        reps: const [_rep],
        selectedRep: _rep,
        selectedDate: DateTime(2026, 9, 17),
        notBillings: [for (var i = 0; i < 60; i++) _nb(i)],
      ),
    );

    expect(find.text('60 RECORDS · Sep 17, 2026'), findsOneWidget);
    // Far-off cards are not built up front any more.
    expect(find.text('NB-59'), findsNothing);

    final scrollable = find.byType(Scrollable).first;

    // The slot around each card is Padding(bottom: 10.h) (bottom-only, unique
    // inside the page).
    Finder card(String number) => find
        .ancestor(
          of: find.text(number, skipOffstage: false),
          matching: find.byWidgetPredicate(
            (w) => w is Padding && w.padding == EdgeInsets.only(bottom: 10.h),
            skipOffstage: false,
          ),
        )
        .first;
    final r0 = tester.getRect(card('NB-0'));
    final r1 = tester.getRect(card('NB-1'));
    // Each card slot: starts at the 16px side padding, spans the content
    // width, and carries its 10px bottom gap; slots are back to back.
    expect(r0.left, closeTo(16.w, 0.01));
    expect(r0.width, closeTo(390 - 32.w, 0.01));
    expect(r1.top, closeTo(r0.bottom, 0.01));

    await tester.scrollUntilVisible(find.text('NB-59'), 400,
        scrollable: scrollable);
    expect(find.text('NB-59'), findsOneWidget);
  });
}
