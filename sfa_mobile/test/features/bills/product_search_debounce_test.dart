// Guards the product picker's search debounce and category-sync throttle.
//
// Before: every keystroke ran a query and swapped the FutureBuilder future, so
// the list flashed to a spinner per key, and every open hit the categories
// endpoint. What must hold now: one query per typing pause, the previous list
// stays up while a query runs, the clear button is still instant, pending
// timers die with the page, and opening only syncs categories when stale.
import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/di/injection.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/bills/domain/usecases/search_products_for_bill_usecase.dart';
import 'package:uswatte/features/bills/presentation/widgets/product_search_delegate.dart';
import 'package:uswatte/features/products/domain/usecases/sync_product_categories_usecase.dart';

class _FakeSearch extends Fake implements SearchProductsForBillUseCase {
  final queries = <String>[];
  final pending = <String, Completer<List<ProductWithPrice>>>{};
  List<ProductWithPrice> Function(String q) results = (_) => const [];

  @override
  Future<List<ProductWithPrice>> call(String query, {int limit = 200}) {
    queries.add(query);
    final completer = pending[query];
    return completer != null ? completer.future : Future.value(results(query));
  }
}

class _FakeCategorySync extends Fake implements SyncProductCategoriesUseCase {
  final maxAges = <Duration>[];
  bool synced = false;

  @override
  Future<bool> syncIfStale(Duration maxAge) async {
    maxAges.add(maxAge);
    return synced;
  }
}

const _soap = ProductWithPrice(
  id: 1,
  code: 'S1',
  itemDescription: 'Sunlight Soap',
  categoryName: 'Soaps',
);
const _bar = ProductWithPrice(
  id: 2,
  code: 'D1',
  itemDescription: 'Detergent Bar',
  categoryName: 'Detergents',
);

Future<void> _openPicker(WidgetTester tester, _FakeSearch search) async {
  await tester.binding.setSurfaceSize(const Size(390, 844));
  addTearDown(() => tester.binding.setSurfaceSize(null));

  // flutter_test's square glyphs fake overflows no device shows; layout is not
  // what this test guards.
  final defaultOnError = FlutterError.onError!;
  FlutterError.onError = (details) {
    if (details.exceptionAsString().contains('A RenderFlex overflowed')) return;
    defaultOnError(details);
  };
  addTearDown(() => FlutterError.onError = defaultOnError);

  // Fonts are real bundled assets now and load asynchronously. One landing
  // between layout and paint trips a debug-only TextPainter assertion, so load
  // every bundled Barlow face up front.
  await tester.runAsync(() async {
    for (final w in [400, 500, 600, 700]) {
      GoogleFonts.barlow(fontWeight: FontWeight.values[w ~/ 100 - 1]);
    }
    for (final w in [400, 500, 600, 700, 800, 900]) {
      GoogleFonts.barlowCondensed(fontWeight: FontWeight.values[w ~/ 100 - 1]);
    }
    GoogleFonts.barlow(fontStyle: FontStyle.italic);
    GoogleFonts.barlowCondensed(fontStyle: FontStyle.italic);
    await GoogleFonts.pendingFonts();
  });

  await tester.pumpWidget(
    ScreenUtilInit(
      designSize: const Size(390, 844),
      minTextAdapt: true,
      splitScreenMode: true,
      builder: (context, child) => MaterialApp(
        home: Scaffold(
          body: Builder(
            builder: (ctx) => ElevatedButton(
              onPressed: () => showProductSearch(
                ctx,
                searchUseCase: search,
                onProductAdded:
                    (
                      _,
                      __,
                      ___,
                      ____,
                      _____,
                      ______,
                      _______,
                      ________,
                      _________,
                    ) {},
              ),
              child: const Text('open'),
            ),
          ),
        ),
      ),
    ),
  );
  await tester.tap(find.text('open'));
  // Route transition (300ms) + the initial query + the post-frame sync.
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 350));
}

void main() {
  late _FakeSearch search;
  late _FakeCategorySync categorySync;

  setUpAll(() => GoogleFonts.config.allowRuntimeFetching = false);

  setUp(() {
    search = _FakeSearch()
      ..results = (q) => q.isEmpty ? const [_soap] : const [_bar];
    categorySync = _FakeCategorySync();
    getIt.registerSingleton<SyncProductCategoriesUseCase>(categorySync);
  });

  tearDown(() => getIt.unregister<SyncProductCategoriesUseCase>());

  testWidgets('a burst of keystrokes runs one query, after the pause', (
    tester,
  ) async {
    await _openPicker(tester, search);
    expect(search.queries, ['']);

    for (final text in ['d', 'de', 'det']) {
      await tester.enterText(find.byType(TextField), text);
      await tester.pump(const Duration(milliseconds: 100));
    }
    expect(search.queries, [''], reason: 'no query while still typing');

    await tester.pump(productSearchDebounce);
    expect(search.queries, ['', 'det']);
    await tester.pump();
    expect(find.text('Detergent Bar'), findsOneWidget);
  });

  testWidgets('previous results stay visible while the next query runs', (
    tester,
  ) async {
    await _openPicker(tester, search);
    expect(find.text('Soaps'), findsOneWidget);

    final slow = Completer<List<ProductWithPrice>>();
    search.pending['det'] = slow;
    await tester.enterText(find.byType(TextField), 'det');
    await tester.pump(productSearchDebounce);
    await tester.pump();
    expect(search.queries.last, 'det');

    expect(find.text('Loading products…'), findsNothing);
    expect(find.text('Soaps'), findsOneWidget);

    slow.complete(const [_bar]);
    await tester.pump();
    expect(find.text('Detergent Bar'), findsOneWidget);
    expect(find.text('Soaps'), findsNothing);
  });

  testWidgets('the clear button resets immediately, without the debounce', (
    tester,
  ) async {
    await _openPicker(tester, search);
    await tester.enterText(find.byType(TextField), 'det');
    await tester.pump(productSearchDebounce);
    expect(search.queries, ['', 'det']);

    await tester.tap(find.byIcon(Icons.clear_rounded));
    await tester.pump();
    expect(search.queries, ['', 'det', '']);
    expect(find.byIcon(Icons.clear_rounded), findsNothing);
  });

  testWidgets('clear button appears as soon as the rep types', (tester) async {
    await _openPicker(tester, search);
    expect(find.byIcon(Icons.clear_rounded), findsNothing);
    await tester.enterText(find.byType(TextField), 'd');
    await tester.pump();
    expect(find.byIcon(Icons.clear_rounded), findsOneWidget);
    await tester.pump(productSearchDebounce);
  });

  testWidgets('closing the picker mid-debounce cancels the pending query', (
    tester,
  ) async {
    await _openPicker(tester, search);
    await tester.enterText(find.byType(TextField), 'det');

    // Back straight away: the 220ms exit transition ends before the 275ms
    // debounce would fire, so only dispose() can stop the query.
    await tester.tap(find.byIcon(Icons.arrow_back_ios_new_rounded));
    await tester.pump();
    // Frame by frame, as on a device: one big pump would fire the timer
    // before any animation frame runs.
    for (var i = 0; i < 30; i++) {
      await tester.pump(const Duration(milliseconds: 16));
    }
    expect(find.text('open'), findsOneWidget);
    expect(search.queries, [''], reason: 'timer must die with the page');
  });

  testWidgets('opening syncs categories only if stale; a fresh sync re-reads', (
    tester,
  ) async {
    await _openPicker(tester, search);
    expect(categorySync.maxAges, [productPickerCategoryMaxAge]);
    expect(search.queries, [''], reason: 'nothing synced, nothing to re-read');
  });

  testWidgets('a sync that ran re-reads the list', (tester) async {
    categorySync.synced = true;
    await _openPicker(tester, search);
    expect(search.queries, ['', '']);
  });
}
