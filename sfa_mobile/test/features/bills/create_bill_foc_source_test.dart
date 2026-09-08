// Guards the funding source assigned to a free-issue cart line.
//
// Company-funded FOC draws the FreeIssue stock pool; Distributor-funded FOC
// draws Normal (BillingService.CreateAsync). The bloc used to hardcode
// 'Company' as the default in both the add path and the cart's type toggle, so
// switching any line to FOC put it on the FreeIssue pool even for a product
// holding none. Nothing surfaced until the bill had already been queued offline
// and the server rejected the whole thing at sync time.
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/bills/domain/repositories/bills_repository.dart';
import 'package:uswatte/features/bills/domain/usecases/create_bill_usecase.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_bloc.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_event.dart';

class _MockRepo extends Mock implements BillsRepository {}

ProductWithPrice _product({double? normalStock, double? freeIssueStock}) =>
    ProductWithPrice(
      id: 1,
      code: 'RL10',
      itemDescription: 'ROLLIES WAFERS 20G',
      dealerPackPrice: 40,
      dealerCasePrice: 40,
      packsPerCase: 1,
      normalStock: normalStock,
      freeIssueStock: freeIssueStock,
    );

CreateBillBloc _bloc() =>
    CreateBillBloc(createBillUseCase: CreateBillUseCase(_MockRepo()));

/// Bloc handlers run asynchronously, so the event queue has to drain before the
/// resulting state can be read.
Future<void> _add(CreateBillBloc bloc, CreateBillEvent event) async {
  bloc.add(event);
  await pumpEventQueue();
}

void main() {
  group('free-issue funding source follows the pool that can fund it', () {
    test('defaults to Company when the product holds free-issue stock', () async {
      final bloc = _bloc();
      addTearDown(bloc.close);

      await _add(bloc, ProductAdded(
        _product(normalStock: 100, freeIssueStock: 15),
        5,
        unitPrice: 40,
        billingItemType: 'FreeIssue',
      ));

      expect(bloc.state.cart.single.freeIssueSource, 'Company');
    });

    test('falls back to Distributor when there is no free-issue pool', () async {
      final bloc = _bloc();
      addTearDown(bloc.close);

      // This is the screenshot case: a product with normal stock but no FOC
      // allocation. Company would draw an empty pool.
      await _add(bloc, ProductAdded(
        _product(normalStock: 100, freeIssueStock: null),
        5,
        unitPrice: 40,
        billingItemType: 'FreeIssue',
      ));

      expect(bloc.state.cart.single.freeIssueSource, 'Distributor');
    });

    test('switching a cart line to FOC picks a fundable source', () async {
      final bloc = _bloc();
      addTearDown(bloc.close);

      await _add(bloc, ProductAdded(
        _product(normalStock: 100, freeIssueStock: null),
        5,
        unitPrice: 40,
      ));
      expect(bloc.state.cart.single.isSale, isTrue);

      await _add(bloc, CartItemTypeChanged(bloc.state.cart.single.lineNumber, 'FreeIssue'));

      // Previously hardcoded to 'Company' regardless of stock.
      expect(bloc.state.cart.single.freeIssueSource, 'Distributor');
    });

    test('an explicit source is honoured when its pool can fund it', () async {
      final bloc = _bloc();
      addTearDown(bloc.close);

      await _add(bloc, ProductAdded(
        _product(normalStock: 100, freeIssueStock: 15),
        5,
        unitPrice: 40,
        billingItemType: 'FreeIssue',
        freeIssueSource: 'Distributor',
      ));

      expect(bloc.state.cart.single.freeIssueSource, 'Distributor');
    });

    test('an explicit source drawing an empty pool is re-picked', () async {
      final bloc = _bloc();
      addTearDown(bloc.close);

      await _add(bloc, ProductAdded(
        _product(normalStock: 100, freeIssueStock: null),
        5,
        unitPrice: 40,
        billingItemType: 'FreeIssue',
        freeIssueSource: 'Company',
      ));

      expect(bloc.state.cart.single.freeIssueSource, 'Distributor');
    });

    test('a Sale line carries no funding source at all', () async {
      final bloc = _bloc();
      addTearDown(bloc.close);

      await _add(bloc, ProductAdded(
        _product(normalStock: 100, freeIssueStock: 15),
        5,
        unitPrice: 40,
      ));

      expect(bloc.state.cart.single.freeIssueSource, isNull);
    });
  });
}
