// Guards what a bill carries about its prices on the way to SQLite and to the
// server.
//
// Before: BillsRepositoryImpl.createBill rebuilt each line without priceType,
// so every stored line read back as 'Packet' — and a product billed by case
// and by packet collided on the detail page, hiding one line. Now each line
// also records the structure that priced it and the list price used, and the
// upload sends them as pricingStructureId / priceBasis / listUnitPrice.
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:uswatte/core/sync/bill_sync_service.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/bills/data/models/bill_item_model.dart';
import 'package:uswatte/features/bills/data/models/bill_model.dart';
import 'package:uswatte/features/bills/data/repositories/bills_repository_impl.dart';
import 'package:uswatte/features/bills/domain/entities/bill.dart';
import 'package:uswatte/features/bills/domain/entities/bill_item.dart';
import 'package:uswatte/features/bills/domain/entities/sync_status.dart';

class _MockLocal extends Mock implements BillsLocalDatasource {}

class _MockSync extends Mock implements BillSyncService {}

BillModel _model(List<BillItemModel> items, {int? structureId = 3}) =>
    BillModel(
      clientBillId: 'c1',
      outletId: 5,
      billingDate: DateTime(2026, 9, 18),
      billDiscountRate: 0,
      subTotalAmount: 0,
      billDiscountAmount: 0,
      totalAmount: 0,
      createdAt: DateTime(2026, 9, 18, 10),
      syncStatus: SyncStatus.pending,
      pricingStructureId: structureId,
      items: items,
    );

void main() {
  group('toCreateRequestJson', () {
    test('sends the header structure and each line\'s pricing snapshot', () {
      final json = _model([
        const BillItemModel(
          clientBillId: 'c1',
          productId: 10,
          quantity: 24,
          unitPrice: 100,
          lineNumber: 1,
          priceType: 'Case',
          pricingStructureId: 2,
          listUnitPrice: 1200,
        ),
        const BillItemModel(
          clientBillId: 'c1',
          productId: 10,
          quantity: 2,
          unitPrice: 90,
          lineNumber: 2,
          pricingStructureId: 3,
          listUnitPrice: 90,
        ),
        const BillItemModel(
          clientBillId: 'c1',
          productId: 11,
          quantity: 1,
          unitPrice: 55,
          lineNumber: 3,
          billingItemType: 'Return',
          returnType: 'Damage',
          priceType: 'Case',
          pricingStructureId: 3,
        ),
      ]).toCreateRequestJson();

      expect(json['pricingStructureId'], 3);
      final items = (json['items'] as List).cast<Map<String, dynamic>>();

      expect(items[0], containsPair('pricingStructureId', 2));
      expect(items[0], containsPair('priceBasis', 'Case'));
      expect(items[0], containsPair('listUnitPrice', 1200));
      // unitPrice is still per pack, exactly as before.
      expect(items[0], containsPair('unitPrice', 100));

      expect(items[1], containsPair('priceBasis', 'Pack'));
      expect(items[1], containsPair('listUnitPrice', 90));

      expect(items[2], containsPair('priceBasis', 'Manual'));
      expect(items[2], containsPair('listUnitPrice', null));
      expect(items[2], containsPair('unitPrice', 55));
    });

    test('a bill queued before structures existed goes up as legacy', () {
      final json = _model([
        const BillItemModel(
          clientBillId: 'c1',
          productId: 10,
          quantity: 1,
          unitPrice: 40,
          lineNumber: 1,
        ),
      ], structureId: null).toCreateRequestJson();

      expect(json.containsKey('pricingStructureId'), isFalse,
          reason: 'the server then stamps its default structure');
      final item = (json['items'] as List).single as Map<String, dynamic>;
      expect(item['pricingStructureId'], isNull);
      expect(item['priceBasis'], isNull);
      expect(item['listUnitPrice'], isNull);
    });
  });

  test('toMap/fromMap round-trip the new columns', () {
    const item = BillItemModel(
      clientBillId: 'c1',
      productId: 10,
      quantity: 24,
      unitPrice: 100,
      lineNumber: 1,
      priceType: 'Case',
      pricingStructureId: 2,
      listUnitPrice: 1200,
    );
    final back = BillItemModel.fromMap({
      ...item.toMap(),
      'pricing_structure_name': 'Standard',
    });
    expect(back.priceType, 'Case');
    expect(back.pricingStructureId, 2);
    expect(back.listUnitPrice, 1200);
    expect(back.toEntity().pricingStructureLabel, 'Standard');

    final bill = BillModel.fromMap(_model(const []).toMap(), const []);
    expect(bill.pricingStructureId, 3);
  });

  group('structure labels', () {
    test('fall back to the id when the structure is no longer synced', () {
      expect(pricingStructureLabelFor(4, null), 'Price list #4');
      expect(pricingStructureLabelFor(4, 'Retail'), 'Retail');
      expect(pricingStructureLabelFor(null, null), isNull,
          reason: 'legacy bill shows nothing');
    });

    test('server price basis maps back to a case/packet split', () {
      expect(priceTypeForBasis('Case'), 'Case');
      expect(priceTypeForBasis('Pack'), 'Packet');
      expect(priceTypeForBasis('Manual'), 'Packet');
      expect(priceTypeForBasis(null), 'Packet');
    });
  });

  test('createBill persists priceType, structure and list price per line',
      () async {
    final local = _MockLocal();
    final sync = _MockSync();
    registerFallbackValue(_model(const []));
    when(() => local.insert(any())).thenAnswer((_) async {});
    when(() => sync.flushOne(any())).thenAnswer((_) async {});

    final repo = BillsRepositoryImpl(local, sync);
    await repo.createBill(Bill(
      clientBillId: 'c1',
      outletId: 5,
      billingDate: DateTime(2026, 9, 18),
      billDiscountRate: 0,
      subTotalAmount: 3600,
      billDiscountAmount: 0,
      totalAmount: 3600,
      createdAt: DateTime(2026, 9, 18, 10),
      syncStatus: SyncStatus.pending,
      pricingStructureId: 3,
      items: const [
        BillItem(
          clientBillId: 'c1',
          productId: 10,
          quantity: 24,
          unitPrice: 100,
          lineNumber: 1,
          priceType: 'Case',
          pricingStructureId: 2,
          listUnitPrice: 1200,
        ),
        BillItem(
          clientBillId: 'c1',
          productId: 10,
          quantity: 12,
          unitPrice: 100,
          lineNumber: 2,
          pricingStructureId: 3,
          listUnitPrice: 100,
        ),
      ],
    ));

    final stored =
        verify(() => local.insert(captureAny())).captured.single as BillModel;
    expect(stored.pricingStructureId, 3);
    expect(stored.items.map((i) => i.priceType), ['Case', 'Packet']);
    expect(stored.items.map((i) => i.pricingStructureId), [2, 3]);
    expect(stored.items.map((i) => i.listUnitPrice), [1200, 100]);
    expect(stored.items.first.toMap()['price_type'], 'Case');
  });
}
