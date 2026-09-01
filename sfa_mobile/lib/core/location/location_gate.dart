import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/core/location/location_requirement.dart';
import 'package:uswatte/features/auth/domain/entities/user_role.dart';
import 'package:uswatte/features/auth/presentation/bloc/auth_bloc.dart';

/// Blocks the app for sales reps whose location is off or denied.
///
/// A banner can be scrolled past; this cannot. Location is already mandatory to raise a
/// bill, but tracking failed silently — so a rep could work all day with location off,
/// flick it on only to bill, and face no consequence. That produced days of missing
/// routes. Gating the whole app means the setting has to stay on to use it at all.
///
/// Wraps the router's output rather than a route, deliberately: adding a `builder` to the
/// /sales-rep parent GoRoute pushes a phantom blank page under every screen and breaks the
/// back button (go_router only skips a parent page when its builder is null).
///
/// Scoped to [UserRole.salesRep] — nobody else is tracked, so gating them would cost
/// access for no benefit. Only issues where `blocksApp` is true gate; "while using the
/// app" stays a banner on the home screen.
class LocationGate extends StatefulWidget {
  const LocationGate({super.key, required this.child});

  final Widget child;

  @override
  State<LocationGate> createState() => _LocationGateState();
}

class _LocationGateState extends State<LocationGate> with WidgetsBindingObserver {
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
    // Fixing any of these means a trip to Settings, so returning to the app is exactly
    // when the gate should lift.
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
    final blocked = issue != null && issue.blocksApp;
    if (!blocked) return widget.child;

    // Only sales reps are tracked; everyone else passes straight through. Read from the
    // auth state so the login screen is never gated either.
    final auth = context.watch<AuthBloc>().state;
    final isRep = auth is AuthAuthenticated && auth.role == UserRole.salesRep;
    if (!isRep) return widget.child;

    return _GateScreen(issue: issue, busy: _busy, onFix: _fix, onRecheck: _check);
  }
}

class _GateScreen extends StatelessWidget {
  const _GateScreen({
    required this.issue,
    required this.busy,
    required this.onFix,
    required this.onRecheck,
  });

  final LocationIssue issue;
  final bool busy;
  final VoidCallback onFix;
  final Future<void> Function() onRecheck;

  ({String title, String body, String action}) get _copy => switch (issue) {
        LocationIssue.servicesOff => (
            title: 'Turn on Location',
            body:
                'Location is switched off, so your route cannot be recorded.\n\nTurn it on to continue using the app.',
            action: 'Turn on Location',
          ),
        LocationIssue.denied => (
            title: 'Location permission needed',
            body:
                'The app needs location to record your route and to raise bills.\n\nPlease allow it to continue.',
            action: 'Allow location',
          ),
        LocationIssue.deniedForever => (
            title: 'Location permission blocked',
            body:
                'Location is blocked for this app.\n\nOpen settings, allow Location, and choose "Allow all the time".',
            action: 'Open settings',
          ),
        // Never reached — whileInUseOnly does not block.
        LocationIssue.whileInUseOnly => (
            title: 'Location setting needs changing',
            body: 'Please set location to "Allow all the time".',
            action: 'Open settings',
          ),
      };

  @override
  Widget build(BuildContext context) {
    final copy = _copy;

    // A bare Material wrapper: this renders above the router, so there is no Scaffold or
    // Directionality inherited from the app's normal tree to rely on.
    return Directionality(
      textDirection: TextDirection.ltr,
      child: Material(
        color: Colors.white,
        child: SafeArea(
          child: Center(
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 28),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Container(
                    width: 88,
                    height: 88,
                    decoration: const BoxDecoration(
                      color: Color(0x14D32F2F),
                      shape: BoxShape.circle,
                    ),
                    child: const Icon(Icons.location_off_rounded,
                        size: 44, color: Color(0xFFD32F2F)),
                  ),
                  const SizedBox(height: 24),
                  Text(
                    copy.title,
                    textAlign: TextAlign.center,
                    style: const TextStyle(
                        fontSize: 20, fontWeight: FontWeight.w800),
                  ),
                  const SizedBox(height: 12),
                  Text(
                    copy.body,
                    textAlign: TextAlign.center,
                    style: const TextStyle(
                        fontSize: 14, height: 1.45, color: Color(0xFF5B5B5B)),
                  ),
                  const SizedBox(height: 28),
                  SizedBox(
                    width: double.infinity,
                    height: 50,
                    child: ElevatedButton.icon(
                      onPressed: busy ? null : onFix,
                      icon: const Icon(Icons.settings_rounded, size: 18),
                      label: Text(copy.action),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: const Color(0xFFD32F2F),
                        foregroundColor: Colors.white,
                        shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12)),
                        textStyle: const TextStyle(
                            fontSize: 15, fontWeight: FontWeight.w700),
                      ),
                    ),
                  ),
                  const SizedBox(height: 8),
                  // Escape hatch. The gate lifts automatically on resume, but if a device
                  // reports its state late the rep must never be stuck with no way to
                  // retry short of restarting the app.
                  TextButton.icon(
                    onPressed: busy ? null : () => onRecheck(),
                    icon: const Icon(Icons.refresh_rounded, size: 16),
                    label: const Text('I have turned it on — check again'),
                    style: TextButton.styleFrom(
                      foregroundColor: const Color(0xFF5B5B5B),
                      textStyle: const TextStyle(fontSize: 13),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
