// SFA — admin-granted proximity exemptions.
//
// `isEnforcedNow` is the single client-side answer to "may this rep bill a shop
// they are not standing at". Two independent checks read it (the outlet picker's
// distance filter and the defensive guard in CreateBillBloc), and it is evaluated
// against the *device* clock because a cached exemption has to expire without
// waiting for the next sync.
//
// That makes two failure directions worth pinning down:
//   - too strict: a rep with a live grant is blocked in the field with no recourse
//     until someone can get them back online.
//   - too lax: a lapsed grant, or a wound-back phone clock, keeps the geofence off
//     indefinitely. The server still refuses the bill, but the rep only finds out
//     later from the sync failure, after the visit is over.

import 'package:flutter_test/flutter_test.dart';
import 'package:uswatte/features/outlets/domain/entities/proximity_policy.dart';

void main() {
  group('ProximityPolicy.isEnforcedNow', () {
    test('enforces when the server says enforced', () {
      const policy = ProximityPolicy(enforced: true, radiusMeters: 1000);

      expect(policy.isEnforcedNow, isTrue);
      expect(policy.isExempt, isFalse);
    });

    test('relaxes while the exemption window is still open', () {
      final policy = ProximityPolicy(
        enforced: false,
        radiusMeters: 1000,
        enforcedFrom: DateTime.now().toUtc().add(const Duration(hours: 4)),
      );

      expect(policy.isEnforcedNow, isFalse);
      expect(policy.isExempt, isTrue);
    });

    test('re-enforces once the window has closed, with no fresh sync', () {
      // The rep synced this morning under a grant that has since lapsed. Nothing
      // told the device; it has to work this out from the timestamp alone.
      final policy = ProximityPolicy(
        enforced: false,
        radiusMeters: 1000,
        enforcedFrom: DateTime.now().toUtc().subtract(const Duration(minutes: 1)),
      );

      expect(policy.isEnforcedNow, isTrue);
    });

    test('re-enforces exactly at the boundary instant', () {
      // ValidTo is exclusive on the server, so the boundary itself is enforced.
      final now = DateTime.now().toUtc();
      final policy = ProximityPolicy(
        enforced: false,
        radiusMeters: 1000,
        enforcedFrom: now,
      );

      expect(policy.isEnforcedNow, isTrue);
    });

    test('stays relaxed indefinitely when no resumption instant was sent', () {
      // This is the server-wide kill switch (BillingGeo.EnforceProximity=false),
      // which has no scheduled resumption. The client must not invent one and
      // re-arm itself against an operator's deliberate choice.
      const policy = ProximityPolicy(enforced: false, radiusMeters: 1000);

      expect(policy.isEnforcedNow, isFalse);
    });

    test('compares in UTC, so a local-time enforcedFrom is not shifted', () {
      // Sri Lanka is UTC+5:30. Comparing a local DateTime against a UTC now
      // without normalising would move every boundary by five and a half hours.
      final policy = ProximityPolicy(
        enforced: false,
        radiusMeters: 1000,
        enforcedFrom:
            DateTime.now().add(const Duration(hours: 2)), // local, not UTC
      );

      expect(policy.isEnforcedNow, isFalse);
    });

    test('enforced:true wins even if a stale resumption instant is present', () {
      // Belt and braces: the server should not send both, but if it does, the
      // stricter reading is the safe one.
      final policy = ProximityPolicy(
        enforced: true,
        radiusMeters: 1000,
        enforcedFrom: DateTime.now().toUtc().add(const Duration(days: 1)),
      );

      expect(policy.isEnforcedNow, isTrue);
    });
  });

  group('ProximityPolicy.enforcedAt', () {
    test('is the safe default — enforced, no exemption, given radius', () {
      const policy = ProximityPolicy.enforcedAt(1500);

      expect(policy.enforced, isTrue);
      expect(policy.isEnforcedNow, isTrue);
      expect(policy.radiusMeters, 1500);
      expect(policy.enforcedFrom, isNull);
      expect(policy.exemptionReason, isNull);
    });
  });

  group('equality', () {
    test('differs when enforcement flips, so BlocListener refires', () {
      // create_bill_page mirrors the policy across on inequality alone; if these
      // compared equal the picker would keep filtering after a grant landed.
      const enforced = ProximityPolicy(enforced: true, radiusMeters: 1000);
      const exempt = ProximityPolicy(enforced: false, radiusMeters: 1000);

      expect(enforced, isNot(equals(exempt)));
    });

    test('differs when only the expiry moves', () {
      final a = ProximityPolicy(
        enforced: false,
        radiusMeters: 1000,
        enforcedFrom: DateTime.utc(2026, 9, 20),
      );
      final b = ProximityPolicy(
        enforced: false,
        radiusMeters: 1000,
        enforcedFrom: DateTime.utc(2026, 9, 25),
      );

      expect(a, isNot(equals(b)));
    });

    test('equal policies compare equal, so no redundant rebuilds', () {
      const a = ProximityPolicy(enforced: true, radiusMeters: 1000);
      const b = ProximityPolicy(enforced: true, radiusMeters: 1000);

      expect(a, equals(b));
    });
  });
}
