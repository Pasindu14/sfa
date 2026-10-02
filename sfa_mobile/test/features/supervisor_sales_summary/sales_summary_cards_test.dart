import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/features/supervisor_sales_summary/domain/entities/rep_billing_summary.dart';
import 'package:uswatte/features/supervisor_sales_summary/presentation/widgets/sales_summary_cards.dart';

RepBillingSummary _summary() => RepBillingSummary.fromJson({
      'from': '2026-10-01',
      'to': '2026-10-02',
      'totalBills': 7,
      'approvedCount': 1,
      'pendingCount': 2,
      'rejectedCount': 1,
      'cancelledCount': 3,
      'totalBilled': 13371.30,
      'approvedSales': 5901.46,
      'pendingValue': 7469.84,
      'totalDiscount': 299.43,
      'goodReturn': 5377.32,
      'marketReturn': 4621.96,
      'freeIssueCompany': 1250.5,
      'freeIssueDistributor': 310.25,
    });

void main() {
  setUpAll(() => GoogleFonts.config.allowRuntimeFetching = false);

  testWidgets('shows summary cards incl. free issue, without NET SALES card',
      (tester) async {
    // flutter_test's square-glyph font fakes RenderFlex overflows (the hero's
    // date chip is the unchanged pre-existing row); this test guards content.
    final defaultOnError = FlutterError.onError!;
    FlutterError.onError = (details) {
      if (details.exceptionAsString().contains('A RenderFlex overflowed')) return;
      defaultOnError(details);
    };
    addTearDown(() => FlutterError.onError = defaultOnError);
    await tester.binding.setSurfaceSize(const Size(390, 1200));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    await tester.pumpWidget(ScreenUtilInit(
      designSize: const Size(390, 844),
      minTextAdapt: true,
      splitScreenMode: true,
      builder: (_, __) => MaterialApp(
        home: Scaffold(
          body: SingleChildScrollView(
            child: SalesSummaryCards(summary: _summary(), repName: 'Nuwan'),
          ),
        ),
      ),
    ));

    expect(find.text('SALES · APPROVED'), findsOneWidget);
    expect(find.text('DISCOUNT'), findsOneWidget);
    expect(find.text('LKR 299.43'), findsOneWidget);
    expect(find.text('FREE ISSUE'), findsOneWidget);
    expect(find.text('LKR 1,560.75'), findsOneWidget);
    expect(find.text('TOTAL DISCOUNT'), findsNothing);
    // Hero = billed 13,371.30 less free issue 1,560.75.
    expect(find.text('LKR 11,810.55'), findsOneWidget);
    expect(find.text('NET SALES'), findsNothing);
  });

  test('freeIssueTotal and missing free-issue fields default to zero', () {
    expect(_summary().freeIssueTotal, 1560.75);
    expect(_summary().netSales, closeTo(11810.55, 0.001));
    // Gross is unaffected by free issue.
    expect(_summary().grossSales, closeTo(13371.30 + 299.43 + 5377.32 + 4621.96, 0.001));
    final old = RepBillingSummary.fromJson({
      'from': '2026-10-01',
      'to': '2026-10-02',
    });
    expect(old.freeIssueCompany, 0);
    expect(old.freeIssueDistributor, 0);
  });
}
