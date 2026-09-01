import 'package:flutter/material.dart';
import 'package:uswatte/core/location/location_requirement.dart';

/// Warns the rep when location is set to "While using the app".
///
/// The harder failures (location off, permission denied) are handled by [LocationGate],
/// which blocks the app outright. This banner covers the remaining case, which is real but
/// not worth locking someone out over: "while using the app" lets a rep bill and view maps,
/// so nothing looks broken — but background capture stops the moment they switch away,
/// producing exactly the silent part-day gaps that are hard to spot.
///
/// Renders nothing when the setup is correct, so it costs a healthy rep no screen space.
class LocationNagBanner extends StatefulWidget {
  const LocationNagBanner({super.key});

  @override
  State<LocationNagBanner> createState() => _LocationNagBannerState();
}

class _LocationNagBannerState extends State<LocationNagBanner>
    with WidgetsBindingObserver {
  LocationIssue? _issue;
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _check();
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    // Fixing this means a trip to Settings, so returning is when it should disappear.
    if (state == AppLifecycleState.resumed) _check();
  }

  Future<void> _check() async {
    final issue = await checkLocationRequirement();
    if (mounted) setState(() => _issue = issue);
  }

  Future<void> _fix() async {
    if (_busy || _issue == null) return;
    setState(() => _busy = true);
    await openFixFor(_issue!);
    if (mounted) setState(() => _busy = false);
    await _check();
  }

  @override
  Widget build(BuildContext context) {
    final issue = _issue;
    // Blocking issues are the gate's job — showing them here too would just be a second
    // copy of the same message behind a full-screen block.
    if (issue == null || issue.blocksApp) return const SizedBox.shrink();

    const accent = Color(0xFFF57C00);

    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 4, 16, 8),
      child: Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: accent.withValues(alpha: 0.08),
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: accent.withValues(alpha: 0.35)),
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // The _rounded variants specifically: they are already used elsewhere in the
            // app, so they survive in the released build's tree-shaken icon font. A new
            // glyph changes MaterialIcons-Regular.otf, and asset changes cannot ship in a
            // Shorebird patch — the icon would render as an empty box on every phone.
            const Icon(Icons.location_searching_rounded, color: accent, size: 20),
            const SizedBox(width: 10),
            const Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Set location to "Allow all the time"',
                    style: TextStyle(
                      fontWeight: FontWeight.w700,
                      fontSize: 13,
                      color: accent,
                    ),
                  ),
                  SizedBox(height: 2),
                  Text(
                    'Right now your route is only recorded while the app is open. '
                    'Change it so it keeps recording through your day.',
                    style: TextStyle(fontSize: 12, height: 1.3),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 8),
            TextButton(
              onPressed: _busy ? null : _fix,
              style: TextButton.styleFrom(
                foregroundColor: accent,
                padding: const EdgeInsets.symmetric(horizontal: 12),
                visualDensity: VisualDensity.compact,
              ),
              child: const Text(
                'Change',
                style: TextStyle(fontWeight: FontWeight.w700, fontSize: 12),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
