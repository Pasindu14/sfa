// Guards the Sync page's price-list viewer: the first screen lists every synced
// price list (default first and marked); a list opens its own products with
// pack / case / MRP, where a blank case price shows the pack × packs-per-case
// value a bill would use.
import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/di/injection.dart';
import 'package:uswatte/features/pricing/data/datasources/pricing_local_datasource.dart';
import 'package:uswatte/features/pricing/data/models/pricing_structure_model.dart';
import 'package:uswatte/features/pricing/presentation/pages/price_lists_page.dart';

class _FakeLocal extends Fake implements PricingLocalDatasource {
  _FakeLocal(this.structures, this.items);

  final List<PricingStructureModel> structures;
  final Map<int, List<PriceListItem>> items;

  @override
  Future<List<PricingStructureModel>> getAllStructures() async => structures;

  @override
  Future<List<PriceListItem>> getStructureItems(int structureId) async =>
      items[structureId] ?? const [];

  @override
  Future<DateTime?> getLastSyncedAt() async => null;
}

const _standard = PricingStructureModel(id: 1, name: 'Standard', isDefault: true, itemCount: 2);
const _promo = PricingStructureModel(id: 2, name: 'Promo', isDefault: false, itemCount: 1);

const _items = {
  1: [
    PriceListItem(
        productId: 10, code: 'CC01', description: 'Real Cream Cracker 125g',
        packsPerCase: 48, packPrice: 100, casePrice: 4800, mrp: 110),
    PriceListItem(
        productId: 11, code: 'CF01', description: 'Chocolate Puff 180g',
        packsPerCase: 30, packPrice: 189.2, casePrice: 5676, mrp: 220),
  ],
  // No case price → billed as pack × 48.
  2: [
    PriceListItem(
        productId: 10, code: 'CC01', description: 'Real Cream Cracker 125g',
        packsPerCase: 48, packPrice: 90),
  ],
};

Future<void> _pump(WidgetTester tester) async {
  await tester.binding.setSurfaceSize(const Size(390, 844));
  addTearDown(() => tester.binding.setSurfaceSize(null));
  final defaultOnError = FlutterError.onError!;
  FlutterError.onError = (details) {
    if (details.exceptionAsString().contains('A RenderFlex overflowed')) return;
    defaultOnError(details);
  };
  addTearDown(() => FlutterError.onError = defaultOnError);

  await tester.runAsync(() async {
    for (final w in [400, 500, 600, 700]) {
      GoogleFonts.barlow(fontWeight: FontWeight.values[w ~/ 100 - 1]);
    }
    for (final w in [700, 800]) {
      GoogleFonts.barlowCondensed(fontWeight: FontWeight.values[w ~/ 100 - 1]);
    }
    await GoogleFonts.pendingFonts();
  });

  // Same shape as the app: detail is a child route of the list.
  final router = GoRouter(
    initialLocation: '/sales-rep/price-lists',
    routes: [
      GoRoute(
        path: '/sales-rep/price-lists',
        builder: (_, __) => const PriceListsPage(),
        routes: [
          GoRoute(
            path: ':id',
            builder: (_, s) => PriceListDetailPage(
              structureId: int.parse(s.pathParameters['id']!),
              initial: s.extra as PricingStructureModel?,
            ),
          ),
        ],
      ),
    ],
  );

  await tester.pumpWidget(
    ScreenUtilInit(
      designSize: const Size(390, 844),
      minTextAdapt: true,
      builder: (_, __) => MaterialApp.router(routerConfig: router),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  setUpAll(() => GoogleFonts.config.allowRuntimeFetching = false);

  setUp(() => getIt.registerSingleton<PricingLocalDatasource>(
        _FakeLocal(const [_standard, _promo], _items),
      ));

  tearDown(() => getIt.unregister<PricingLocalDatasource>());

  testWidgets('first screen lists the price lists, not products', (tester) async {
    await _pump(tester);

    expect(find.text('Standard'), findsOneWidget);
    expect(find.text('Promo'), findsOneWidget);
    expect(find.text('DEFAULT'), findsOneWidget);
    expect(find.text('2 products'), findsOneWidget);
    expect(find.text('1 product'), findsOneWidget);
    expect(find.text('CC01'), findsNothing);
  });

  testWidgets('opening a list shows its products and prices', (tester) async {
    await _pump(tester);

    await tester.tap(find.text('Standard'));
    await tester.pumpAndSettle();

    expect(find.text('STANDARD'), findsOneWidget);
    expect(find.text('CC01'), findsOneWidget);
    expect(find.text('CF01'), findsOneWidget);
    expect(find.text('100.00'), findsOneWidget);
    expect(find.text('4,800.00'), findsOneWidget);
    expect(find.text('110.00'), findsOneWidget);
  });

  testWidgets('a blank case price shows the derived pack × case value', (tester) async {
    await _pump(tester);

    await tester.tap(find.text('Promo'));
    await tester.pumpAndSettle();

    expect(find.text('CF01'), findsNothing, reason: 'Promo does not price the puff');
    expect(find.text('90.00'), findsOneWidget);
    expect(find.text('4,320.00'), findsOneWidget);
    expect(find.text('pack × 48'), findsOneWidget);
  });

  testWidgets('search inside a list filters by code or name', (tester) async {
    await _pump(tester);
    await tester.tap(find.text('Standard'));
    await tester.pumpAndSettle();

    await tester.enterText(find.byType(TextField), 'puff');
    await tester.pumpAndSettle();

    expect(find.text('CF01'), findsOneWidget);
    expect(find.text('CC01'), findsNothing);
  });
}
