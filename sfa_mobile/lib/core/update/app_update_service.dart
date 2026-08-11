import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:shorebird_code_push/shorebird_code_push.dart';

/// Thin wrapper over the Shorebird updater.
///
/// ## Why this exists
///
/// Shorebird swaps the Dart snapshot **only at process start** — it can never
/// change a running app. This app runs a location foreground service
/// (`LocationTrackingService`, `isForegroundMode: true`), which by design keeps
/// the OS process alive after the rep swipes the app away. Reopening therefore
/// resumes the same process running the same old code, so a downloaded patch
/// can sit unapplied indefinitely with no signal that anything is pending.
///
/// This service makes that state visible and actionable: it reports when a
/// patch is staged, and [restart] kills the process so the next launch boots
/// the new code.
class AppUpdateService {
  AppUpdateService._private();
  static final AppUpdateService instance = AppUpdateService._private();

  final ShorebirdUpdater _updater = ShorebirdUpdater();

  /// True once a patch has been downloaded and is waiting for a process
  /// restart to take effect. Widgets (the version badges) listen to this to
  /// show a staged indicator — [currentPatchNumber] alone can't do that, since
  /// it only reflects what's already running, not what's waiting on disk.
  final ValueNotifier<bool> patchStaged = ValueNotifier<bool>(false);

  /// False on builds without the Shorebird engine — a plain `flutter build`,
  /// a debug run, or the emulator. Everything below no-ops in that case.
  bool get isAvailable => _updater.isAvailable;

  /// The patch currently running, or null when on the release baseline.
  /// Useful for surfacing "patch N" in a debug/about screen.
  Future<Patch?> currentPatch() async {
    if (!isAvailable) return null;
    try {
      return await _updater.readCurrentPatch();
    } on ReadPatchException {
      return null;
    }
  }

  /// Patch number currently running, or null on the release baseline.
  /// Exposed as a plain int so presentation need not import the Shorebird
  /// package just to render a diagnostic.
  Future<int?> currentPatchNumber() async => (await currentPatch())?.number;

  /// Downloads a pending patch if one is available.
  ///
  /// Returns true when a patch is staged and the app must restart to run it.
  /// Never throws — a failed update check must not disrupt the rep's work.
  Future<bool> checkAndDownload() async {
    if (!isAvailable) return false;
    try {
      var status = await _updater.checkForUpdate();
      if (status == UpdateStatus.outdated) {
        await _updater.update();
        status = await _updater.checkForUpdate();
      }
      final needsRestart = status == UpdateStatus.restartRequired;
      // Only ever set on a successful check — a transient failure here must
      // not clear a staged flag set by an earlier successful check.
      patchStaged.value = needsRestart;
      return needsRestart;
    } on UpdateException {
      return false;
    } catch (_) {
      return false;
    }
  }

  /// Kills the process so the next launch boots the staged patch.
  ///
  /// `exit(0)` rather than `SystemNavigator.pop()`: popping only finishes the
  /// activity, and the foreground service keeps the process — and therefore the
  /// old Dart snapshot — alive. Nothing is lost, since bills, not-billings and
  /// location pings are all written to SQLite as they are created.
  void restart() => exit(0);
}
