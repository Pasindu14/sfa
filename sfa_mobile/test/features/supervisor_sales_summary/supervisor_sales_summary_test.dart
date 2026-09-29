import 'package:flutter/material.dart' show DateTimeRange;
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_my_reps_usecase.dart';
import 'package:uswatte/features/supervisor_sales_summary/data/datasources/supervisor_sales_summary_remote_datasource.dart';
import 'package:uswatte/features/supervisor_sales_summary/domain/entities/rep_billing_summary.dart';
import 'package:uswatte/features/supervisor_sales_summary/presentation/cubit/supervisor_sales_summary_cubit.dart';
import 'package:uswatte/features/supervisor_sales_summary/presentation/cubit/supervisor_sales_summary_state.dart';

class _MockReps extends Mock implements GetMyRepsUseCase {}

class _MockRemote extends Mock implements SupervisorSalesSummaryRemoteDatasource {}

const _rep = RepSummary(userId: 17, userName: 'A M Anjula Nirmal');

final _json = <String, dynamic>{
  'from': '2026-09-01',
  'to': '2026-09-29',
  'totalBills': 7,
  'approvedCount': 3,
  'pendingCount': 2,
  'rejectedCount': 1,
  'cancelledCount': 1,
  'totalBilled': 1400,
  'approvedSales': 1000.5,
  'pendingValue': 399.5,
  'totalDiscount': 70,
  'goodReturn': 30,
  'marketReturn': 12.25,
};

void main() {
  setUpAll(() => registerFallbackValue(DateTime(2026)));

  test('RepBillingSummary parses the API DTO', () {
    final s = RepBillingSummary.fromJson(_json);
    expect(s.from, DateTime(2026, 9, 1));
    expect(s.to, DateTime(2026, 9, 29));
    expect(s.totalBills, 7);
    expect(s.cancelledCount, 1);
    expect(s.approvedSales, 1000.5);
    expect(s.marketReturn, 12.25);
  });

  test('net sales matches the rep bill: gross − discount − returns', () {
    final s = RepBillingSummary.fromJson(_json);
    expect(s.netSales, 1400);
    expect(s.grossSales, 1400 + 70 + 30 + 12.25);
    expect(s.grossSales - s.totalDiscount - s.goodReturn - s.marketReturn,
        s.netSales);
  });

  group('SupervisorSalesSummaryCubit', () {
    late _MockReps reps;
    late _MockRemote remote;

    SupervisorSalesSummaryCubit build() => SupervisorSalesSummaryCubit(
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
      final s = c.state as SalesSummaryReady;
      expect(s.range.start, DateTime(2026, 9, 1));
      expect(s.range.end, DateTime(2026, 9, 29));
      expect(s.canLoad, isFalse, reason: 'no rep selected yet');
      await c.close();
    });

    test('load does nothing without a rep', () async {
      final c = build();
      await c.loadReps();
      await c.load();
      verifyNever(() => remote.getRepBillingSummary(any(), any(), any()));
      await c.close();
    });

    test('loads for the rep and range; changing either clears the result',
        () async {
      when(() => remote.getRepBillingSummary(17, any(), any()))
          .thenAnswer((_) async => RepBillingSummary.fromJson(_json));
      final c = build();
      await c.loadReps();
      c.selectRep(_rep);
      await c.load();
      expect((c.state as SalesSummaryReady).summary?.totalBills, 7);

      c.selectRange(DateTimeRange(
          start: DateTime(2026, 8, 1), end: DateTime(2026, 8, 31)));
      expect((c.state as SalesSummaryReady).summary, isNull);

      await c.load();
      verify(() => remote.getRepBillingSummary(
          17, DateTime(2026, 8, 1), DateTime(2026, 8, 31))).called(1);
      await c.close();
    });
  });
}
