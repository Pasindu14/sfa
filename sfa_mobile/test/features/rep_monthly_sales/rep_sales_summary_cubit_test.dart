import 'package:flutter/material.dart' show DateTimeRange;
import 'package:flutter_test/flutter_test.dart';
import 'package:uswatte/features/rep_monthly_sales/data/datasources/rep_billing_summary_remote_datasource.dart';
import 'package:uswatte/features/rep_monthly_sales/presentation/cubit/rep_sales_summary_cubit.dart';
import 'package:uswatte/features/supervisor_sales_summary/domain/entities/rep_billing_summary.dart';

class _FakeRemote implements RepBillingSummaryRemoteDatasource {
  DateTime? from, to;
  bool fail = false;

  @override
  Future<RepBillingSummary> getMyBillingSummary(DateTime f, DateTime t) async {
    from = f;
    to = t;
    if (fail) throw Exception('boom');
    return RepBillingSummary.fromJson({
      'from': '2026-10-01',
      'to': '2026-10-02',
      'freeIssueDistributor': 5,
    });
  }
}

void main() {
  test('defaults to today only and loads that range', () async {
    final remote = _FakeRemote();
    final cubit =
        RepSalesSummaryCubit(remote, clock: () => DateTime(2026, 10, 2, 14));

    expect(cubit.state.range.start, DateTime(2026, 10, 2));
    expect(cubit.state.range.end, DateTime(2026, 10, 2));

    await cubit.load();
    expect(remote.from, DateTime(2026, 10, 2));
    expect(remote.to, DateTime(2026, 10, 2));
    expect(cubit.state.summary, isNotNull);
    expect(cubit.state.isLoading, isFalse);
  });

  test('picking a new range clears the old summary and loads the new range',
      () async {
    final remote = _FakeRemote();
    final cubit =
        RepSalesSummaryCubit(remote, clock: () => DateTime(2026, 10, 2));
    await cubit.load();
    expect(cubit.state.summary, isNotNull);

    final range =
        DateTimeRange(start: DateTime(2026, 9, 1), end: DateTime(2026, 9, 30));
    cubit.selectRange(range);
    expect(cubit.state.summary, isNull);

    await cubit.load();
    expect(remote.from, DateTime(2026, 9, 1));
    expect(remote.to, DateTime(2026, 9, 30));
  });

  test('a failed load shows an error and can be retried', () async {
    final remote = _FakeRemote()..fail = true;
    final cubit =
        RepSalesSummaryCubit(remote, clock: () => DateTime(2026, 10, 2));
    await cubit.load();
    expect(cubit.state.error, isNotNull);
    expect(cubit.state.summary, isNull);

    remote.fail = false;
    await cubit.load();
    expect(cubit.state.error, isNull);
    expect(cubit.state.summary, isNotNull);
  });
}
