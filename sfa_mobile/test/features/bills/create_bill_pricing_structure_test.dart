// Guards the Pricing Structure → Outlet → Products bill flow in the bloc.
//
// The structure is recorded per line: switching the bill's price list only
// affects lines added afterwards, lines already in the cart keep their
// structure and price, and the Sale merge never folds a quantity into a line
// priced from a different structure (which would bill it at the wrong price).
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/bills/domain/entities/bill.dart';
import 'package:uswatte/features/bills/domain/entities/sync_status.dart';
import 'package:uswatte/features/bills/domain/repositories/bills_repository.dart';
import 'package:uswatte/features/bills/domain/usecases/create_bill_usecase.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_bloc.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_event.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_state.dart';
import 'package:uswatte/features/outlets/domain/entities/outlet.dart';
import 'package:uswatte/features/pricing/domain/entities/pricing_structure.dart';
import 'package:uswatte/features/pricing/domain/usecases/get_pricing_structures_usecase.dart';

class _MockRepo extends Mock implements BillsRepository {}

class _FakeStructures extends Fake implements GetPricingStructuresUseCase {
  _FakeStructures(this.structures);
  final List<PricingStructure> structures;

  @override
  Future<List<PricingStructure>> call() async => structures;
}

const _retail = PricingStructure(id: 1, name: 'Retail', isDefault: false);
const _standard = PricingStructure(id: 2, name: 'Standard', isDefault: true);
const _wholesale = PricingStructure(id: 3, name: 'Wholesale', isDefault: false);

/// The same product as the picker returns it when searched in [structureId].
ProductWithPrice _product(int structureId, double packPrice) =>
    ProductWithPrice(
      id: 10,
      code: 'CF02',
      itemDescription: 'CHOCOLATE PUFF 180G',
      pricingStructureId: structureId,
      dealerPackPrice: packPrice,
      dealerCasePrice: packPrice * 12,
      packsPerCase: 12,
      normalStock: 500,
    );

const _outlet = Outlet(
  id: 5,
  name: 'Sunil Stores',
  address: 'Main St',
  tel: '0711234567',
  latitude: 0,
  longitude: 0,
  outletType: 'Retail',
  outletCategory: 'A',
  routeId: 1,
  routeName: 'Route 1',
  isActive: true,
);

Future<CreateBillBloc> _bloc(
  List<PricingStructure> structures, {
  BillsRepository? repo,
}) async {
  final bloc = CreateBillBloc(
    createBillUseCase: CreateBillUseCase(repo ?? _MockRepo()),
    getPricingStructuresUseCase: _FakeStructures(structures),
  );
  addTearDown(bloc.close);
  await pumpEventQueue();
  return bloc;
}

Future<void> _add(CreateBillBloc bloc, CreateBillEvent event) async {
  bloc.add(event);
  await pumpEventQueue();
}

ProductAdded _sale(int structureId, double packPrice, double qty) =>
    ProductAdded(
      _product(structureId, packPrice),
      qty,
      unitPrice: packPrice,
      listUnitPrice: packPrice,
    );

void main() {
  setUpAll(() {
    registerFallbackValue(Bill(
      clientBillId: 'x',
      outletId: 0,
      billingDate: DateTime(2026),
      billDiscountRate: 0,
      subTotalAmount: 0,
      billDiscountAmount: 0,
      totalAmount: 0,
      createdAt: DateTime(2026),
      syncStatus: SyncStatus.pending,
    ));
  });

  group('structure selection', () {
    test('preselects the default structure, wherever it is listed', () async {
      final bloc = await _bloc([_retail, _standard, _wholesale]);

      expect(bloc.state.pricingStructuresLoaded, isTrue);
      expect(bloc.state.pricingStructures, hasLength(3));
      expect(bloc.state.selectedPricingStructure, _standard);
    });

    test('falls back to the first structure when none is default', () async {
      final bloc = await _bloc([_retail, _wholesale]);
      expect(bloc.state.selectedPricingStructure, _retail);
    });

    test('nothing synced leaves no selection and blocks submit', () async {
      final bloc = await _bloc(const []);

      expect(bloc.state.pricingStructuresLoaded, isTrue);
      expect(bloc.state.selectedPricingStructure, isNull);

      await _add(bloc, const BillLocationStatusChanged(LocationCheckStatus.ready));
      await _add(bloc, const OutletSelected(_outlet));
      await _add(bloc, _sale(2, 100, 1));
      expect(bloc.state.canSubmit, isFalse);
    });
  });

  group('switching structure', () {
    test('only affects lines added afterwards', () async {
      final bloc = await _bloc([_standard, _wholesale]);

      await _add(bloc, _sale(2, 100, 3));
      await _add(bloc, const PricingStructureSelected(_wholesale));

      // The existing line keeps its structure and price.
      final kept = bloc.state.cart.single;
      expect(kept.pricingStructureId, 2);
      expect(kept.unitPrice, 100);
      expect(bloc.state.selectedPricingStructure, _wholesale);

      await _add(bloc, _sale(3, 90, 2));

      expect(bloc.state.cart, hasLength(2));
      expect(bloc.state.cart[0].pricingStructureId, 2);
      expect(bloc.state.cart[0].unitPrice, 100);
      expect(bloc.state.cart[1].pricingStructureId, 3);
      expect(bloc.state.cart[1].unitPrice, 90);
      expect(bloc.state.cartMixesStructures, isTrue);
      expect(bloc.state.pricingStructureNameFor(3), 'Wholesale');
    });

    test('same product, same structure and price type still merges', () async {
      final bloc = await _bloc([_standard]);

      await _add(bloc, _sale(2, 100, 3));
      await _add(bloc, _sale(2, 100, 2));

      expect(bloc.state.cart.single.quantity, 5);
      expect(bloc.state.cartMixesStructures, isFalse);
    });

    test('the merge key includes the structure', () async {
      final bloc = await _bloc([_standard, _wholesale]);

      await _add(bloc, _sale(2, 100, 3));
      // Same product, same Sale/Packet shape — but priced from another list.
      await _add(bloc, _sale(3, 100, 2));

      expect(bloc.state.cart, hasLength(2),
          reason: 'must not fold into the Standard-priced line');
      expect(bloc.state.cart.map((l) => l.quantity), [3, 2]);
    });

    test('the merge key includes the price type', () async {
      final bloc = await _bloc([_standard]);

      await _add(bloc, _sale(2, 100, 3));
      await _add(bloc, ProductAdded(
        _product(2, 100),
        12,
        unitPrice: 100,
        priceType: 'Case',
        listUnitPrice: 1200,
      ));

      expect(bloc.state.cart, hasLength(2));
      expect(bloc.state.cart.map((l) => l.priceType), ['Packet', 'Case']);
    });

    test('removing a line keeps the others\' structure and list price', () async {
      final bloc = await _bloc([_standard, _wholesale]);

      await _add(bloc, _sale(2, 100, 1));
      await _add(bloc, _sale(3, 90, 1));
      await _add(bloc, const CartItemRemoved(1));

      final left = bloc.state.cart.single;
      expect(left.lineNumber, 1);
      expect(left.pricingStructureId, 3);
      expect(left.listUnitPrice, 90);
    });
  });

  group('submit', () {
    test('records the selected structure on the bill and each line\'s own',
        () async {
      final repo = _MockRepo();
      when(() => repo.createBill(any())).thenAnswer(
          (inv) async => inv.positionalArguments.first as Bill);
      final bloc = await _bloc([_standard, _wholesale], repo: repo);

      await _add(bloc, const BillLocationStatusChanged(LocationCheckStatus.ready));
      await _add(bloc, const OutletSelected(_outlet));
      await _add(bloc, ProductAdded(
        _product(2, 100),
        24,
        unitPrice: 100,
        priceType: 'Case',
        listUnitPrice: 1200,
      ));
      await _add(bloc, const PricingStructureSelected(_wholesale));
      await _add(bloc, _sale(3, 90, 2));
      await _add(bloc, ProductAdded(
        _product(3, 90),
        1,
        unitPrice: 55,
        billingItemType: 'Return',
        returnType: 'Damage',
        listUnitPrice: 90,
      ));
      expect(bloc.state.canSubmit, isTrue);

      await _add(bloc, const SubmitPressed());

      final bill =
          verify(() => repo.createBill(captureAny())).captured.single as Bill;
      expect(bill.pricingStructureId, 3, reason: 'selected at submit time');

      final caseLine = bill.items[0];
      expect(caseLine.pricingStructureId, 2);
      expect(caseLine.priceType, 'Case');
      expect(caseLine.priceBasis, 'Case');
      expect(caseLine.listUnitPrice, 1200);
      expect(caseLine.unitPrice, 100);

      final packLine = bill.items[1];
      expect(packLine.pricingStructureId, 3);
      expect(packLine.priceBasis, 'Pack');
      expect(packLine.listUnitPrice, 90);

      final returnLine = bill.items[2];
      expect(returnLine.pricingStructureId, 3);
      expect(returnLine.priceBasis, 'Manual');
      expect(returnLine.listUnitPrice, isNull,
          reason: 'a rep-typed return price has no list price');
      expect(returnLine.unitPrice, 55);
    });
  });

  test('switching a return back to a sale restores the list price', () async {
    final bloc = await _bloc([_standard]);

    await _add(bloc, ProductAdded(
      _product(2, 100),
      12,
      unitPrice: 55,
      billingItemType: 'Return',
      returnType: 'Damage',
      priceType: 'Case',
      listUnitPrice: 1200,
    ));
    await _add(bloc, const CartItemTypeChanged(1, 'Sale'));

    expect(bloc.state.cart.single.isSale, isTrue);
    expect(bloc.state.cart.single.unitPrice, 100,
        reason: 'case list price 1200 / 12 packs, not the typed 55');
  });
}
