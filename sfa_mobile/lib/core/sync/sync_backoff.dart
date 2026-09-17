/// Retry backoff for outbox rows that failed with a non-terminal error.
///
/// Without it every trigger (connectivity restore, app resume, list load,
/// background task) re-POSTs every failed row immediately, so one bad bill
/// hammers the server on each of them. Delay = [base] · 2^(attempts-1),
/// capped at [cap]. Manual retry / "sync now" actions bypass this.
class SyncBackoff {
  static const Duration base = Duration(seconds: 30);
  static const Duration cap = Duration(minutes: 15);

  const SyncBackoff._();

  /// Delay to wait after the [attempts]-th failed attempt.
  static Duration delayFor(int attempts) {
    if (attempts <= 0) return Duration.zero;
    // 30s · 2^5 = 16 min already exceeds the cap; clamp the exponent so large
    // attempt counts can't overflow the shift.
    final exp = (attempts - 1).clamp(0, 10);
    final delay = base * (1 << exp);
    return delay > cap ? cap : delay;
  }

  /// True when a `failed` row may be retried now. Rows without a recorded
  /// attempt time (written before the column existed) are always due.
  static bool isDue({
    required int attempts,
    required DateTime? lastAttemptAt,
    required DateTime now,
  }) {
    if (lastAttemptAt == null) return true;
    final nextAt = lastAttemptAt.toUtc().add(delayFor(attempts));
    return !now.toUtc().isBefore(nextAt);
  }
}
