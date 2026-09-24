// Guards the cart summary breakdown the client checks against their old app:
// SALES is the gross amount (qty × price), DISCOUNT is every line discount plus
// the bill discount, and SALES − DISCOUNT − RETURNS must equal the net total.
// Numbers are the client's reference order (Invoice Total 6128.00, Discount
// 40.79 / 0.67%, Returns 2352.27, Final 3734.94).
import 'package:flutter_test/flutter_test.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_state.dart';

const _product = ProductWithPrice(id: 1, code: 'X', itemDescription: 'X', packsPerCase: 12);

CartLine _line(int n, double qty, double price, {double disc = 0, String type = 'Sale'}) => CartLine(
      lineNumber: n,
      product: _product,
      quantity: qty,
      unitPrice: price,
      discountRate: disc,
      billingItemType: type,
    );

void main() {
  final state = CreateBillState(cart: [
    _line(1, 6, 110.50, disc: 3.2), // Chocolate Cream 100g
    _line(2, 5, 87.00, disc: 4.5), // Custard Cream 90g
    _line(3, 8, 172.00), // Milk Sorites 200g
    _line(4, 20, 182.70), // Milk Sorties 200g
    _line(5, 5, 125.11, type: 'Return'),
    _line(6, 3, 312.28, type: 'Return'),
    _line(7, 4, 197.47, type: 'Return'),
    _line(8, 2, 172.00, type: 'FreeIssue'), // info only — never in the totals
  ]);

  test('SALES is gross qty × price, before any discount', () {
    expect(state.saleGrossTotal, closeTo(6128.00, 0.005));
  });

  test('DISCOUNT totals every line discount', () {
    expect(state.totalDiscountAmount, closeTo(40.79, 0.01));
    expect(state.totalDiscountPercent, closeTo(0.67, 0.005));
    expect(state.hasDiscount, isTrue);
  });

  test('SALES − DISCOUNT − RETURNS equals the net total the order submits', () {
    expect(state.returnTotal, closeTo(2352.27, 0.005));
    expect(state.saleGrossTotal - state.totalDiscountAmount - state.returnTotal,
        closeTo(state.total, 0.005));
    expect(state.total, closeTo(3734.94, 0.01));
  });

  test('bill discount is included in the DISCOUNT row', () {
    final withBill = CreateBillState(cart: [_line(1, 10, 100)], billDiscountRate: 5);
    expect(withBill.saleGrossTotal, 1000);
    expect(withBill.totalDiscountAmount, closeTo(50, 0.001));
    expect(withBill.total, closeTo(950, 0.001));
  });

  test('no discount → no DISCOUNT row', () {
    expect(CreateBillState(cart: [_line(1, 2, 50)]).hasDiscount, isFalse);
  });
}
