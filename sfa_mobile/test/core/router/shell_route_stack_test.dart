// Guards the fix for the "back button goes black" bug.
//
// The '/sales-rep' and '/supervisor' parents in app_router.dart must stay
// redirect-only groupings. If either gets a `builder` again, go_router pushes
// it as a real page under every child and backing out of home lands on it.
//
// This replicates the router's shape rather than importing AppRouter, which
// needs AuthBloc + the whole getIt graph. What is under test is go_router's
// behaviour for this route shape, so a faithful miniature proves it.
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';

/// Number of pages go_router put on the Navigator for the current location.
///
/// Counts the declarative `pages` list, which is what go_router actually
/// supplies — a phantom parent page shows up here. (Do not use
/// `NavigatorState.popUntil` to count: it stops at the top route as soon as the
/// predicate returns true, so it always reports 1.)
int _stackDepth(WidgetTester tester) {
  return tester
      .widgetList<Navigator>(find.byType(Navigator))
      .map((n) => n.pages.length)
      .fold<int>(0, (a, b) => a > b ? a : b);
}

GoRouter _buildRouter() => GoRouter(
      initialLocation: '/sales-rep/home',
      routes: [
        GoRoute(
          path: '/sales-rep',
          // The shape under test: redirect-only, no builder.
          redirect: (_, state) =>
              state.uri.path == '/sales-rep' ? '/sales-rep/home' : null,
          routes: [
            GoRoute(
              path: 'home',
              name: 'salesRepHome',
              builder: (_, __) => const Scaffold(body: Text('HOME')),
            ),
            GoRoute(
              path: 'bills',
              name: 'bills',
              builder: (_, __) => const Scaffold(body: Text('BILLS')),
            ),
          ],
        ),
      ],
    );

Future<void> _pump(WidgetTester tester, GoRouter router) async {
  await tester.pumpWidget(MaterialApp.router(routerConfig: router));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('home sits alone on the stack — no phantom parent page',
      (tester) async {
    await _pump(tester, _buildRouter());

    expect(find.text('HOME'), findsOneWidget);
    expect(_stackDepth(tester), 1,
        reason: 'a 2nd page here is the invisible parent — the black screen');
  });

  testWidgets('child routes are NOT swallowed by the parent redirect',
      (tester) async {
    final router = _buildRouter();
    await _pump(tester, router);

    router.push('/sales-rep/bills');
    await tester.pumpAndSettle();

    // Regression guard: with `state.matchedLocation` instead of `state.uri.path`
    // this lands on HOME, because go_router evaluates the parent's redirect
    // using the parent's own match.
    expect(find.text('BILLS'), findsOneWidget);
    expect(find.text('HOME'), findsNothing);
  });

  testWidgets('back from a pushed child returns to home, not a blank page',
      (tester) async {
    final router = _buildRouter();
    await _pump(tester, router);

    router.push('/sales-rep/bills');
    await tester.pumpAndSettle();
    expect(_stackDepth(tester), 2);

    router.pop();
    await tester.pumpAndSettle();

    expect(find.text('HOME'), findsOneWidget);
    expect(_stackDepth(tester), 1);
  });

  testWidgets('bare /sales-rep redirects to home', (tester) async {
    final router = _buildRouter();
    await _pump(tester, router);

    router.go('/sales-rep');
    await tester.pumpAndSettle();

    expect(find.text('HOME'), findsOneWidget);
    expect(_stackDepth(tester), 1);
  });
}
