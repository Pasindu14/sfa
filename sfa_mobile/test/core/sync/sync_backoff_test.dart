import 'package:flutter_test/flutter_test.dart';
import 'package:uswatte/core/sync/sync_backoff.dart';

void main() {
  test('delay doubles from 30s and caps at 15 minutes', () {
    expect(SyncBackoff.delayFor(0), Duration.zero);
    expect(SyncBackoff.delayFor(1), const Duration(seconds: 30));
    expect(SyncBackoff.delayFor(2), const Duration(seconds: 60));
    expect(SyncBackoff.delayFor(5), const Duration(minutes: 8));
    expect(SyncBackoff.delayFor(6), const Duration(minutes: 15));
    expect(SyncBackoff.delayFor(1000), const Duration(minutes: 15));
  });

  test('row with no recorded attempt is always due', () {
    expect(
      SyncBackoff.isDue(attempts: 9, lastAttemptAt: null, now: DateTime.now()),
      isTrue,
    );
  });

  test('compares in UTC regardless of the clock offset', () {
    final last = DateTime.utc(2026, 9, 17, 10);
    expect(
      SyncBackoff.isDue(
          attempts: 1,
          lastAttemptAt: last,
          now: last.add(const Duration(seconds: 29)).toLocal()),
      isFalse,
    );
    expect(
      SyncBackoff.isDue(
          attempts: 1,
          lastAttemptAt: last,
          now: last.add(const Duration(seconds: 30)).toLocal()),
      isTrue,
    );
  });
}
