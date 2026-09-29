import 'package:dio/dio.dart';
import 'package:flutter/material.dart' show DateTimeRange;
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_my_reps_usecase.dart';
import 'package:uswatte/features/supervisor_itemwise_sales/data/datasources/supervisor_itemwise_sales_remote_datasource.dart';
import 'package:uswatte/features/supervisor_itemwise_sales/domain/entities/rep_itemwise_sales.dart';
import 'package:uswatte/features/supervisor_itemwise_sales/presentation/cubit/supervisor_itemwise_sales_cubit.dart';
import 'package:uswatte/features/supervisor_itemwise_sales/presentation/cubit/supervisor_itemwise_sales_state.dart';

class _MockReps extends Mock implements GetMyRepsUseCase {}

class _MockRemote extends Mock
    implements SupervisorItemwiseSalesRemoteDatasource {}

const _rep = RepSummary(userId: 17, userName: 'A M Anjula Nirmal');

final _json = <String, dynamic>{
  'from': '2026-09-01',
  'to': '2026-09-29',
  'totalSaleQty': 14,
  'totalFreeIssueQty': 1,
  'totalGoodReturnQty': 1,
  'totalMarketReturnQty': 2,
  'totalGrossValue': 1200,
  'totalDiscount': 59.5,
  'totalGoodReturnValue': 100,
  'totalMarketReturnValue': 40,
  'totalNetValue': 1000.5,
  'items': [
    {
      'productId': 1,
      'itemCode': 'P001',
      'itemName': 'Tea 100g',
      'saleQty': 10,
      'freeIssueQty': 0,
      'goodReturnQty': 1,
      'marketReturnQty': 0,
      'grossValue': 1000,
      'discount': 59.5,
      'goodReturnValue': 100,
      'marketReturnValue': 0,
      'netValue': 840.5,
    },
    {
      'productId': 2,
      'itemCode': 'P002',
      'itemName': 'Biscuit',
      'saleQty': 4,
      'freeIssueQty': 1,
      'goodReturnQty': 0,
      'marketReturnQty': 2,
      'grossValue': 200,
      'discount': 0,
      'goodReturnValue': 0,
      'marketReturnValue': 40,
      'netValue': 160,
    },
  ],
};

void main() {
  setUpAll(() => registerFallbackValue(DateTime(2026)));

  test('RepItemwiseSales parses the API DTO', () {
    final s = RepItemwiseSales.fromJson(_json);
    expect(s.from, DateTime(2026, 9, 1));
    expect(s.to, DateTime(2026, 9, 29));
    expect(s.totalNetValue, 1000.5);
    expect(s.items, hasLength(2));
    final tea = s.items.first;
    expect(tea.itemCode, 'P001');
    expect(tea.discount, 59.5);
    expect(
      tea.netValue,
      tea.grossValue -
          tea.discount -
          tea.goodReturnValue -
          tea.marketReturnValue,
    );
    expect(s.items[1].returnQty, 2);
    expect(s.items[1].returnValue, 40);
  });

  test('missing items parses as empty', () {
    final s = RepItemwiseSales.fromJson({
      'from': '2026-09-01',
      'to': '2026-09-01',
    });
    expect(s.items, isEmpty);
    expect(s.totalNetValue, 0);
  });

  group('SupervisorItemwiseSalesCubit', () {
    late _MockReps reps;
    late _MockRemote remote;

    SupervisorItemwiseSalesCubit build() => SupervisorItemwiseSalesCubit(
      getMyReps: reps,
      remote: remote,
      clock: () => DateTime(2026, 9, 29, 15, 30),
    );

    setUp(() {
      reps = _MockReps();
      remote = _MockRemote();
      when(() => reps()).thenAnswer((_) async => [_rep]);
    });

    test('defaults to this month so far', () async {
      final c = build();
      await c.loadReps();
      final s = c.state as ItemwiseSalesReady;
      expect(s.range.start, DateTime(2026, 9, 1));
      expect(s.range.end, DateTime(2026, 9, 29));
      expect(s.canLoad, isFalse, reason: 'no rep selected yet');
      await c.close();
    });

    test('load does nothing without a rep', () async {
      final c = build();
      await c.loadReps();
      await c.load();
      verifyNever(() => remote.getRepItemwiseSales(any(), any(), any()));
      await c.close();
    });

    test(
      'loads for the rep and range; changing either clears the result',
      () async {
        when(
          () => remote.getRepItemwiseSales(17, any(), any()),
        ).thenAnswer((_) async => RepItemwiseSales.fromJson(_json));
        final c = build();
        await c.loadReps();
        c.selectRep(_rep);
        await c.load();
        expect((c.state as ItemwiseSalesReady).sales?.items, hasLength(2));

        c.selectRange(
          DateTimeRange(
            start: DateTime(2026, 8, 1),
            end: DateTime(2026, 8, 31),
          ),
        );
        expect((c.state as ItemwiseSalesReady).sales, isNull);

        await c.load();
        verify(
          () => remote.getRepItemwiseSales(
            17,
            DateTime(2026, 8, 1),
            DateTime(2026, 8, 31),
          ),
        ).called(1);
        await c.close();
      },
    );

    test('surfaces the API field error message', () async {
      final req = RequestOptions(path: '/api/v1/supervisor/rep-itemwise-sales');
      when(() => remote.getRepItemwiseSales(17, any(), any())).thenThrow(
        DioException(
          requestOptions: req,
          response: Response(
            requestOptions: req,
            statusCode: 400,
            data: {
              'message': 'Validation failed',
              'fields': {
                'to': ['The date range may not exceed 92 days.'],
              },
            },
          ),
        ),
      );
      final c = build();
      await c.loadReps();
      c.selectRep(_rep);
      await c.load();
      final s = c.state as ItemwiseSalesReady;
      expect(s.isLoading, isFalse);
      expect(s.error, 'The date range may not exceed 92 days.');
      await c.close();
    });
  });
}
