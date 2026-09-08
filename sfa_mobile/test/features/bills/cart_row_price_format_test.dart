// SFA-124: money in the cart row must carry two decimals.
//
// cart_row rounded to whole rupees while the cart_list totals below it used two,
// so a bill's line items visibly failed to sum to its total (a line reading
// "Rs. 1081" above a sales total of "Rs. 1080.50").
//
// The damaging case is the return-price dialog: its seed string is editable, so
// opening it on a 100.45 return and pressing Save rewrote the line as 100.00 —
// the cents left the submitted data, not just the screen.
import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_state.dart';
import 'package:uswatte/features/bills/presentation/widgets/cart_row.dart';

const _product = ProductWithPrice(
  id: 1,
  code: 'CF02',
  itemDescription: 'CHOCOLATE PUFF 180G',
  dealerPackPrice: 100.45,
  dealerCasePrice: 100.45,
  packsPerCase: 1,
  normalStock: 500,
  freeIssueStock: 20,
);

CartLine _line({
  required String type,
  double unitPrice = 100.45,
  double quantity = 1,
  String? returnType,
}) =>
    CartLine(
      lineNumber: 1,
      product: _product,
      quantity: quantity,
      unitPrice: unitPrice,
      billingItemType: type,
      returnType: returnType,
    );

Future<void> _pump(WidgetTester tester, CartLine line) async {
  await tester.binding.setSurfaceSize(const Size(390, 844));
  addTearDown(() => tester.binding.setSurfaceSize(null));

  // flutter_test's default font draws every glyph as a square of width fontSize,
  // so this dense row measures far wider than the real Barlow faces and reports
  // overflows no device shows. Formatting is what this guards, not layout.
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
          body: CartRow(
            packetLine: line,
            onPacketQtyChanged: (_) {},
            onDiscountChanged: (_) {},
            onRemoved: () {},
            onTypeChanged: (_) {},
            onReturnTypeChanged: (_) {},
            onFreeIssueSourceChanged: (_) {},
            onExpireDateChanged: (_) {},
            onPriceChanged: (_) {},
          ),
        ),
      ),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  group('cart row prices carry two decimals', () {
    testWidgets('return line shows the unit price with cents', (tester) async {
      await _pump(tester, _line(type: 'Return', returnType: 'Damage'));

      expect(find.textContaining('CF02 · Rs.100.45'), findsOneWidget);
    });

    testWidgets('return line total shows cents', (tester) async {
      await _pump(tester,
          _line(type: 'Return', returnType: 'Damage', quantity: 2));

      // 2 × 100.45 = 200.90 — previously rendered as "−Rs. 201".
      expect(find.text('−Rs. 200.90'), findsOneWidget);
    });

    testWidgets('sale line total shows cents', (tester) async {
      await _pump(tester, _line(type: 'Sale', quantity: 2));

      expect(find.text('Rs. 200.90'), findsOneWidget);
    });

    testWidgets('a whole-rupee price still renders as .00', (tester) async {
      await _pump(tester, _line(type: 'Sale', unitPrice: 100));

      expect(find.textContaining('Rs.100.00'), findsOneWidget);
    });

    testWidgets('the return price dialog seeds with cents intact',
        (tester) async {
      await _pump(tester, _line(type: 'Return', returnType: 'Damage'));

      // The unit-price label opens the "Edit Return Price" dialog.
      await tester.tap(find.textContaining('Rs.100.45'));
      await tester.pumpAndSettle();

      expect(find.text('Edit Return Price'), findsOneWidget);
      final field = tester.widget<TextField>(find.byType(TextField).last);
      expect(field.controller?.text, '100.45');
    });
  });
}
