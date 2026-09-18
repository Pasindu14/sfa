// Guards how the cart pairs Case and Packet lines into rows.
//
// Rows used to be keyed by product + line type only, with the price type as
// the slot. Once one bill can price the same product from two structures, the
// second structure's line landed in an occupied slot and replaced the first on
// screen — still counted in the total, but invisible to the rep.
import 'package:flutter_test/flutter_test.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_state.dart';
import 'package:uswatte/features/bills/presentation/widgets/cart_list.dart';

const _product = ProductWithPrice(
  id: 10,
  code: 'CF02',
  itemDescription: 'CHOCOLATE PUFF 180G',
  packsPerCase: 12,
);

CartLine _line(
  int n, {
  String priceType = 'Packet',
  int? structure = 1,
  String type = 'Sale',
  String? returnType,
}) =>
    CartLine(
      lineNumber: n,
      product: _product,
      quantity: 1,
      unitPrice: 100,
      priceType: priceType,
      pricingStructureId: structure,
      billingItemType: type,
      returnType: returnType,
    );

void main() {
  test('case and packet lines of one structure share a row', () {
    final rows = groupCartLines([
      _line(1, priceType: 'Case'),
      _line(2),
    ]);
    expect(rows, hasLength(1));
    expect(rows.single.keys, containsAll(['Case', 'Packet']));
  });

  test('lines from different structures get their own rows', () {
    final rows = groupCartLines([_line(1, structure: 1), _line(2, structure: 2)]);
    expect(rows, hasLength(2));
    expect(rows.map((r) => r['Packet']!.lineNumber), [1, 2]);
  });

  test('two returns of different types are not collapsed', () {
    final rows = groupCartLines([
      _line(1, type: 'Return', returnType: 'Damage'),
      _line(2, type: 'Return', returnType: 'Expire'),
    ]);
    expect(rows, hasLength(2));
  });

  test('a second line for a taken slot starts a new row', () {
    final rows = groupCartLines([_line(1), _line(2)]);
    expect(rows, hasLength(2),
        reason: 'no line may be hidden behind another');
  });
}
