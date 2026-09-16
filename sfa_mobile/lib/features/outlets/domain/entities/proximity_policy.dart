import 'package:equatable/equatable.dart';

/// The rep's effective billing-geofence policy, as handed down by the server on
/// the daily outlet sync.
///
/// Normally [enforced] is true and the app hides outlets further than
/// [radiusMeters] away. While an admin-granted proximity exemption is live the
/// server sends `enforced: false` together with [enforcedFrom] — the instant
/// enforcement resumes — so the device can expire the exemption on its own
/// instead of behaving as exempt until the next sync.
///
/// Read [isEnforcedNow], never [enforced], at every decision point.
class ProximityPolicy extends Equatable {
  /// What the server said at sync time.
  final bool enforced;

  final double radiusMeters;

  /// When enforcement resumes. Null when enforcement is already on, or when the
  /// server has it switched off outright (no scheduled resumption).
  final DateTime? enforcedFrom;

  /// Reason code for the exemption, for display only.
  final String? exemptionReason;

  const ProximityPolicy({
    required this.enforced,
    required this.radiusMeters,
    this.enforcedFrom,
    this.exemptionReason,
  });

  const ProximityPolicy.enforcedAt(double radius)
      : enforced = true,
        radiusMeters = radius,
        enforcedFrom = null,
        exemptionReason = null;

  /// Whether the distance filter should apply right now.
  ///
  /// A cached exemption stops applying once its window closes, without needing a
  /// sync to tell it so — otherwise a rep who synced at 09:00 under a grant that
  /// expires at 17:00 would keep billing from anywhere all evening.
  ///
  /// Device time is trusted only to *re-enable* the check, never to relax it, so
  /// winding the phone clock back cannot buy a bypass. The server re-checks every
  /// bill regardless; this is UX, not the gate.
  bool get isEnforcedNow {
    if (enforced) return true;
    final until = enforcedFrom;
    if (until == null) return false;
    return !DateTime.now().toUtc().isBefore(until.toUtc());
  }

  /// True when the rep is currently billing under an exemption.
  bool get isExempt => !isEnforcedNow;

  ProximityPolicy copyWith({
    bool? enforced,
    double? radiusMeters,
    DateTime? enforcedFrom,
    String? exemptionReason,
  }) =>
      ProximityPolicy(
        enforced: enforced ?? this.enforced,
        radiusMeters: radiusMeters ?? this.radiusMeters,
        enforcedFrom: enforcedFrom ?? this.enforcedFrom,
        exemptionReason: exemptionReason ?? this.exemptionReason,
      );

  @override
  List<Object?> get props => [enforced, radiusMeters, enforcedFrom, exemptionReason];
}
