import 'package:flutter_test/flutter_test.dart';
import 'package:uswatte/features/rep_monthly_sales/data/datasources/rep_billing_summary_remote_datasource.dart';
import 'package:uswatte/features/rep_monthly_sales/presentation/cubit/rep_billing_summary_cubit.dart';
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
      'freeIssueCompany': 5,
    });
  }
}

void main() {
  test('loads 1st of month to today', () async {
    final remote = _FakeRemote();
    final cubit =
        RepBillingSummaryCubit(remote, clock: () => DateTime(2026, 10, 2, 14));
    await cubit.load();

    expect(remote.from, DateTime(2026, 10, 1));
    expect(remote.to, DateTime(2026, 10, 2));
    expect(cubit.state, isA<RepBillingSummaryLoaded>());
  });

  test('error on first load; keeps loaded figures when a refresh fails',
      () async {
    final remote = _FakeRemote()..fail = true;
    final cubit =
        RepBillingSummaryCubit(remote, clock: () => DateTime(2026, 10, 2));
    await cubit.load();
    expect(cubit.state, isA<RepBillingSummaryError>());

    remote.fail = false;
    await cubit.load();
    expect(cubit.state, isA<RepBillingSummaryLoaded>());

    remote.fail = true;
    await cubit.load();
    expect(cubit.state, isA<RepBillingSummaryLoaded>());
  });
}
