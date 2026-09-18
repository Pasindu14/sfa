// Guards the two-pool stock contract of the Create Bill quantity sheet.
//
// The server keeps Normal and FreeIssue as physically separate inventory pools
// and draws each line type from a specific one (BillingService.CreateAsync):
// Sale and Distributor-funded free issue come out of Normal, Company-funded
// free issue comes out of FreeIssue. If the sheet lets a rep stage a line
// against an empty pool, nothing surfaces until the bill has already been
// written to the offline outbox and the server rejects it with a 422 at sync
// time — by then the rep has left the outlet.
//
// These tests pin the three things that keep that from happening: the sheet
// opens on a type the stock can actually supply, disabled types stay
// untappable, and staged quantities are capped against the right pool.
import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/bills/presentation/widgets/quantity_dialog.dart';

ProductWithPrice _product({
  double? normalStock,
  double? freeIssueStock,
  int packsPerCase = 1,
  double packPrice = 100,
  double? casePrice,
  bool casePriced = true,
}) =>
    ProductWithPrice(
      id: 1,
      code: 'P-1042',
      itemDescription: 'COCONUT BISCUIT 200G',
      dealerPackPrice: packPrice,
      pricingStructureId: 7,
      dealerCasePrice:
          casePriced ? (casePrice ?? packPrice * packsPerCase) : null,
      packsPerCase: packsPerCase,
      normalStock: normalStock,
      freeIssueStock: freeIssueStock,
    );

/// Holds the sheet's return value. The sheet is still open when _open returns,
/// so the result has to arrive through a box rather than by value.
class _Box {
  List<QuantityDialogResult>? value;
}

/// Opens the sheet and hands back a box that receives whatever it returns.
Future<_Box> _open(
  WidgetTester tester,
  ProductWithPrice product,
) async {
  // Match the design baseline so ScreenUtil scales 1:1 as it does on a handset.
  await tester.binding.setSurfaceSize(const Size(390, 844));
  addTearDown(() => tester.binding.setSurfaceSize(null));

  // flutter_test's default font draws every glyph as a square of width
  // fontSize, so the two pool figures measure roughly twice as wide as the real
  // Barlow faces and manufacture overflows no device shows. Layout is not what
  // these tests guard, so overflow reports are dropped while every other error
  // still fails the test.
  final defaultOnError = FlutterError.onError!;
  FlutterError.onError = (details) {
    if (details.exceptionAsString().contains('A RenderFlex overflowed')) return;
    defaultOnError(details);
  };
  addTearDown(() => FlutterError.onError = defaultOnError);

  final box = _Box();
  await tester.pumpWidget(
    ScreenUtilInit(
      designSize: const Size(390, 844),
      minTextAdapt: true,
      splitScreenMode: true,
      builder: (context, child) => MaterialApp(
        home: Scaffold(
          body: Builder(
            builder: (ctx) => ElevatedButton(
              onPressed: () async {
                box.value = await showQuantityDialog(ctx, product: product);
              },
              child: const Text('open'),
            ),
          ),
        ),
      ),
    ),
  );

  await tester.tap(find.text('open'));
  await tester.pumpAndSettle();
  return box;
}

/// The submit button's label changes with the selected item type, so it is
/// addressed by type instead.
final Finder _addButton = find.byType(FilledButton);

/// The sheet is taller than the 844pt test surface, so the add button sits a
/// little below the fold and a plain tap lands on nothing at all — silently,
/// because tap() only warns. Scroll it into view first.
Future<void> _tapAdd(WidgetTester tester) async {
  await tester.ensureVisible(_addButton);
  await tester.pumpAndSettle();
  await tester.tap(_addButton);
  await tester.pumpAndSettle();
}

/// The quantity field is addressed by its hint rather than by index: the
/// Return tab puts its price field ahead of the quantity field in the tree, so
/// a positional finder silently types into the wrong box.
///
/// Every product here has packsPerCase == 1, so the cases field is hidden and
/// the packs field is the only quantity input on screen.
final Finder _qtyField = find.byWidgetPredicate(
  (w) => w is TextField && w.decoration?.hintText == '# packs',
);

Future<void> _enterPacks(WidgetTester tester, String value) async {
  await tester.enterText(_qtyField, value);
  await tester.pumpAndSettle();
}

void main() {
  group('quantity sheet — pool visibility', () {
    testWidgets('shows both pool figures, not just the normal one',
        (tester) async {
      await _open(tester, _product(normalStock: 48, freeIssueStock: 12));

      expect(find.text('Normal: 48'), findsOneWidget);
      expect(find.text('Free issue: 12'), findsOneWidget);
    });

    testWidgets('renders an empty pool as zero rather than hiding it',
        (tester) async {
      await _open(tester, _product(normalStock: 0, freeIssueStock: 6));

      // "We checked and it is empty" must stay distinguishable from
      // "we never looked" — a hidden figure reads as the latter.
      expect(find.text('Normal: 0'), findsOneWidget);
      expect(find.text('Free issue: 6'), findsOneWidget);
    });
  });

  // SFA-122: prices carry cents. Rounding the prefilled return price is not
  // cosmetic — that value is what the line is submitted with, so a 100.45
  // dealer price silently became a 100.00 return credit.
  group('quantity sheet — prices keep two decimals', () {
    testWidgets('prefills the return price with cents intact', (tester) async {
      await _open(
        tester,
        _product(normalStock: 0, freeIssueStock: 0, packPrice: 100.45),
      );

      // Both pools empty, so the sheet opens on Return with its price prefilled.
      final price = tester.widget<TextField>(
        find.byWidgetPredicate(
          (w) => w is TextField && w.decoration?.hintText != '# packs',
        ),
      );
      expect(price.controller?.text, '100.45');
    });

    testWidgets('shows a whole-rupee price as .00 rather than bare', (tester) async {
      await _open(
        tester,
        _product(normalStock: 0, freeIssueStock: 0, packPrice: 100),
      );

      final price = tester.widget<TextField>(
        find.byWidgetPredicate(
          (w) => w is TextField && w.decoration?.hintText != '# packs',
        ),
      );
      expect(price.controller?.text, '100.00');
    });
  });

  group('quantity sheet — opening mode follows available stock', () {
    testWidgets('opens on Sale when the normal pool has cover', (tester) async {
      await _open(tester, _product(normalStock: 48, freeIssueStock: 12));

      // The Sale tab owns the discount field; it is absent on the other tabs.
      expect(find.text('Discount'), findsOneWidget);
    });

    testWidgets('opens on Free Issue when only the FOC pool has cover',
        (tester) async {
      await _open(tester, _product(normalStock: 0, freeIssueStock: 6));

      // Landing on Sale here would strand the rep on an unusable tab.
      expect(find.text('FUNDED BY'), findsOneWidget);
      expect(find.text('Discount'), findsNothing);
    });

    testWidgets('opens on Return when both pools are empty', (tester) async {
      await _open(tester, _product(normalStock: 0, freeIssueStock: 0));

      // Returns credit stock rather than consuming it, so they stay reachable
      // for a sold-out product.
      expect(find.text('RETURN TYPE'), findsOneWidget);
      expect(find.text('FUNDED BY'), findsNothing);
    });

    testWidgets(
        'preselects Distributor funding when the company FOC pool is empty',
        (tester) async {
      await _open(tester, _product(normalStock: 40, freeIssueStock: 0));

      // Switch to Free Issue, which is enabled here only via the Normal pool.
      await tester.tap(find.text('Free Issue'));
      await tester.pumpAndSettle();

      // Company draws the empty FreeIssue pool, so the sheet must not sit on it.
      final companyChip = tester.widget<Opacity>(
        find
            .ancestor(
              of: find.text('Company'),
              matching: find.byType(Opacity),
            )
            .first,
      );
      expect(companyChip.opacity, lessThan(1.0));
    });
  });

  group('quantity sheet — disabled types stay untappable', () {
    testWidgets('tapping Sale does nothing when the normal pool is empty',
        (tester) async {
      await _open(tester, _product(normalStock: 0, freeIssueStock: 6));

      await tester.tap(find.text('Sale'));
      await tester.pumpAndSettle();

      // Still on Free Issue — the tap must not have switched tabs.
      expect(find.text('FUNDED BY'), findsOneWidget);
      expect(find.text('Discount'), findsNothing);
    });

    testWidgets('tapping Free Issue does nothing when both FOC routes are empty',
        (tester) async {
      // Normal is empty, so Distributor-funded FOC has nothing to give away
      // either — the whole Free Issue tab is unusable.
      await _open(tester, _product(normalStock: 0, freeIssueStock: 0));

      await tester.tap(find.text('Free Issue'));
      await tester.pumpAndSettle();

      expect(find.text('RETURN TYPE'), findsOneWidget);
      expect(find.text('FUNDED BY'), findsNothing);
    });
  });

  group('quantity sheet — quantities are capped against the right pool', () {
    testWidgets('rejects a sale larger than the normal pool', (tester) async {
      final box =
          await _open(tester, _product(normalStock: 5, freeIssueStock: 100));

      await _enterPacks(tester, '6');
      await _tapAdd(tester);

      // The generous FOC pool must not cover a Sale line.
      expect(find.textContaining('Only 5 units in normal stock'), findsWidgets);
      expect(box.value, isNull);
    });

    testWidgets('accepts a sale that fits the normal pool', (tester) async {
      final box =
          await _open(tester, _product(normalStock: 5, freeIssueStock: 0));

      await _enterPacks(tester, '5');
      await _tapAdd(tester);

      expect(box.value, isNotNull);
      expect(box.value!.single.quantity, 5);
    });

    testWidgets('caps company-funded free issue against the FOC pool',
        (tester) async {
      final box =
          await _open(tester, _product(normalStock: 100, freeIssueStock: 4));

      // Opens on Sale (normal has cover), so move to Free Issue. Company is
      // preselected because the FOC pool is non-empty.
      await tester.tap(find.text('Free Issue'));
      await tester.pumpAndSettle();
      await _enterPacks(tester, '5');
      await _tapAdd(tester);

      // The large Normal pool is the wrong one to draw a Company FOC from.
      expect(
        find.textContaining('Only 4 units in free issue stock'),
        findsWidgets,
      );
      expect(box.value, isNull);
    });

    testWidgets(
        'sums sale and distributor-funded free issue against the normal pool',
        (tester) async {
      final box =
          await _open(tester, _product(normalStock: 10, freeIssueStock: 0));

      await _enterPacks(tester, '7');

      await tester.tap(find.text('Free Issue'));
      await tester.pumpAndSettle();
      await _enterPacks(tester, '5');

      await _tapAdd(tester);

      // 7 + 5 both come out of Normal, so 10 units cannot cover them even
      // though neither line exceeds 10 on its own.
      expect(find.textContaining('Sale + free issue needs 12'), findsWidgets);
      expect(box.value, isNull);
    });

    testWidgets('leaves returns exempt from the stock cap', (tester) async {
      final box =
          await _open(tester, _product(normalStock: 0, freeIssueStock: 0));

      // Opens on Return. A return credits stock back, so a sold-out product
      // must still accept one.
      await _enterPacks(tester, '3');
      await _tapAdd(tester);

      expect(box.value, isNotNull);
      expect(box.value!.single.quantity, 3);
    });
  });

  // A structure may price a product per pack only (dealerCasePrice null). The
  // case line must then bill pack price × packs per case — the old product
  // column defaulted the case price to 0 and billed whole cases for nothing.
  group('quantity sheet — case price and pricing snapshot', () {
    final Finder casesField = find.byWidgetPredicate(
      (w) => w is TextField && w.decoration?.labelText == 'Cases',
    );

    Future<List<QuantityDialogResult>?> addOneCase(
      WidgetTester tester,
      ProductWithPrice product,
    ) async {
      final box = await _open(tester, product);
      await tester.enterText(casesField, '1');
      await tester.pumpAndSettle();
      await _tapAdd(tester);
      return box.value;
    }

    testWidgets('falls back to pack × packs per case when unpriced per case',
        (tester) async {
      final result = await addOneCase(
        tester,
        _product(normalStock: 100, packsPerCase: 12, casePriced: false),
      );

      final line = result!.single;
      expect(line.priceType, 'Case');
      expect(line.quantity, 12);
      expect(line.unitPrice, 100, reason: '1200 / 12 per pack, not 0');
      expect(line.listUnitPrice, 1200);
    });

    testWidgets('treats a zero case price as unpriced', (tester) async {
      final result = await addOneCase(
        tester,
        _product(normalStock: 100, packsPerCase: 12, casePrice: 0),
      );

      expect(result!.single.unitPrice, 100);
      expect(result.single.listUnitPrice, 1200);
    });

    testWidgets('uses the structure case price when it has one',
        (tester) async {
      final result = await addOneCase(
        tester,
        _product(normalStock: 100, packsPerCase: 12, casePrice: 1080),
      );

      final line = result!.single;
      expect(line.unitPrice, 90);
      expect(line.listUnitPrice, 1080, reason: 'the full case price');
      expect(line.pricingStructureId, 7);
    });

    testWidgets('a packet line carries the pack list price and structure',
        (tester) async {
      final box = await _open(tester, _product(normalStock: 10));
      await _enterPacks(tester, '2');
      await _tapAdd(tester);

      final line = box.value!.single;
      expect(line.priceType, 'Packet');
      expect(line.listUnitPrice, 100);
      expect(line.pricingStructureId, 7);
    });
  });
}
