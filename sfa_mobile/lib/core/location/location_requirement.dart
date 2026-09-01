import 'package:geolocator/geolocator.dart';

/// What is currently wrong with the phone's location setup, if anything.
enum LocationIssue {
  /// Device-wide location toggle is off.
  servicesOff,

  /// Permission was refused.
  denied,

  /// Refused with "don't ask again" — only Settings can fix it.
  deniedForever,

  /// "While using the app". Enough to raise a bill, not enough to record a route:
  /// capture stops the moment the rep switches away from the app.
  whileInUseOnly,
}

extension LocationIssueX on LocationIssue {
  /// Whether this issue should lock the rep out of the app entirely.
  ///
  /// Only the unambiguous, two-taps-to-fix cases block. [whileInUseOnly] deliberately
  /// does not: it is the default a rep lands on by tapping the obvious button in
  /// Android's permission dialog, upgrading it cannot be done from a dialog on Android
  /// 11+, and some OEM skins bury it. Hard-blocking that would mean a rep with an
  /// uncooperative phone cannot work at all — turning a tracking-quality problem into a
  /// revenue problem, which is the worse failure.
  bool get blocksApp => this != LocationIssue.whileInUseOnly;
}

/// Inspects the phone's location setup. Returns null when everything is as it should be
/// ("Allow all the time" with services on).
///
/// Never throws — a failed check returns null, so a flaky platform call can never lock a
/// rep out of the app.
Future<LocationIssue?> checkLocationRequirement() async {
  try {
    if (!await Geolocator.isLocationServiceEnabled()) {
      return LocationIssue.servicesOff;
    }
    return switch (await Geolocator.checkPermission()) {
      LocationPermission.denied => LocationIssue.denied,
      LocationPermission.deniedForever => LocationIssue.deniedForever,
      LocationPermission.whileInUse => LocationIssue.whileInUseOnly,
      // always / unableToDetermine — nothing actionable.
      _ => null,
    };
  } catch (_) {
    return null;
  }
}

/// Sends the rep wherever they can fix [issue]. Safe to call for any issue.
Future<void> openFixFor(LocationIssue issue) async {
  try {
    switch (issue) {
      case LocationIssue.servicesOff:
        await Geolocator.openLocationSettings();
      case LocationIssue.denied:
        // A prompt can only ever grant "while in use"; going to Settings is how the rep
        // reaches "Allow all the time", so send them there if the prompt didn't finish
        // the job.
        final result = await Geolocator.requestPermission();
        if (result != LocationPermission.always) {
          await Geolocator.openAppSettings();
        }
      case LocationIssue.deniedForever:
      case LocationIssue.whileInUseOnly:
        await Geolocator.openAppSettings();
    }
  } catch (_) {
    // Opening settings can fail on unusual OEM builds — the caller re-checks regardless.
  }
}
