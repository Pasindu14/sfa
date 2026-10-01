import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:uswatte/core/utils/bill_breakdown.dart';
import 'package:uswatte/core/widgets/bill_breakdown_widgets.dart';
import 'package:uswatte/features/bills/domain/entities/bill.dart';
import 'package:uswatte/features/bills/domain/entities/bill_amounts.dart';
import 'package:uswatte/features/bills/domain/entities/bill_item.dart';
import 'package:uswatte/features/bills/domain/entities/sync_status.dart';

// BIL-2026-00008: gross 6128.00, discount 40.79, returns 2352.31, total 3734.90.
Bill _bill() => Bill(
      clientBillId: 'c1',
      outletId: 1,
      billingDate: DateTime(2026, 9, 25),
      billDiscountRate: 0,
      subTotalAmount: 6087.21,
      billDiscountAmount: 0,
      totalAmount: 3734.90,
      createdAt: DateTime(2026, 9, 25),
      syncStatus: SyncStatus.synced,
      items: const [
        BillItem(
          clientBillId: 'c1',
          productId: 1,
          quantity: 100,
          unitPrice: 61.28,
          discountRate: 0.6657,
          lineNumber: 1,
        ),
        BillItem(
          clientBillId: 'c1',
          productId: 2,
          quantity: 10,
          unitPrice: 89.21,
          billingItemType: 'FreeIssue',
          freeIssueSource: 'Company',
          lineNumber: 2,
        ),
      ],
    );

Future<void> _pump(WidgetTester tester, Widget child) async {
  await tester.binding.setSurfaceSize(const Size(390, 844));
  addTearDown(() => tester.binding.setSurfaceSize(null));
  // flutter_test's square-glyph font fakes RenderFlex overflows.
  final defaultOnError = FlutterError.onError!;
  FlutterError.onError = (details) {
    if (details.exceptionAsString().contains('A RenderFlex overflowed')) return;
    defaultOnError(details);
  };
  addTearDown(() => FlutterError.onError = defaultOnError);
  await tester.pumpWidget(
    ScreenUtilInit(
      designSize: const Size(390, 844),
      builder: (_, __) => MaterialApp(home: Scaffold(body: child)),
    ),
  );
  await tester.pump();
}

void main() {
  test('local bill breakdown adds up to the stored total', () {
    final b = _bill().breakdown;
    expect(b.gross, closeTo(6128.00, 0.001));
    expect(b.discount, closeTo(40.79, 0.001));
    expect(b.returns, closeTo(2352.31, 0.001));
    expect(b.freeIssue, closeTo(892.10, 0.001));
    expect(b.gross - b.discount - b.returns, closeTo(b.total, 0.001));
  });

  test('list breakdown without the new fields hides the subline', () {
    final b = BillBreakdown.forList(total: 100);
    expect(b.gross, 100);
    expect(b.hasListSubline, isFalse);
  });

  testWidgets('subline shows only non-zero parts', (tester) async {
    await _pump(
      tester,
      const BillBreakdownSubline(
        breakdown: BillBreakdown(
            gross: 6128, discount: 40.79, returns: 2352.31, total: 3734.9),
      ),
    );
    expect(find.textContaining('Sales 6128.00'), findsOneWidget);
    expect(find.textContaining('Disc −40.79'), findsOneWidget);
    expect(find.textContaining('Returns −2352.31'), findsOneWidget);
  });

  testWidgets('totals card shows breakdown rows and hides zero ones',
      (tester) async {
    await _pump(tester, BillTotalsCard(breakdown: _bill().breakdown));
    expect(find.text('Sales (gross)'), findsOneWidget);
    expect(find.text('Returns'), findsOneWidget);
    expect(find.text('Free issues (info)'), findsOneWidget);
    expect(find.text('Rs. 3734.90'), findsOneWidget);

    await _pump(
      tester,
      const BillTotalsCard(breakdown: BillBreakdown(gross: 50, total: 50)),
    );
    expect(find.text('Returns'), findsNothing);
    expect(find.textContaining('Discount'), findsNothing);
    expect(find.text('Free issues (info)'), findsNothing);
  });
}
