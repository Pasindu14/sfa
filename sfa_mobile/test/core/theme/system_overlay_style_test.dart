// T3.25: why the SystemChrome calls in build() could be replaced by an
// AnnotatedRegion carrying the app-wide style without any visual change.
// Flutter applies the innermost AnnotatedRegion under the status bar on every
// frame, and the app wraps everything in one (main.dart), so an imperative call
// from build() is overridden in the same frame.
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:uswatte/core/theme/app_theme.dart';

const _lightIcons = SystemUiOverlayStyle(
  statusBarColor: Colors.transparent,
  statusBarIconBrightness: Brightness.light,
);

class _ImperativePage extends StatelessWidget {
  const _ImperativePage();
  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(_lightIcons);
    return const Scaffold(body: SizedBox.expand());
  }
}

Widget _app(Widget home) => AnnotatedRegion<SystemUiOverlayStyle>(
      value: AppTheme.systemOverlayStyle,
      child: MaterialApp(home: home),
    );

// ignore: invalid_use_of_visible_for_testing_member
SystemUiOverlayStyle? get _applied => SystemChrome.latestStyle;

void main() {
  testWidgets('an imperative call from build() is overridden by the app region',
      (tester) async {
    await tester.pumpWidget(_app(const _ImperativePage()));
    await tester.pump();

    expect(_applied, AppTheme.systemOverlayStyle);
  });

  testWidgets('a page region with the app-wide style applies the same style',
      (tester) async {
    await tester.pumpWidget(_app(const AnnotatedRegion<SystemUiOverlayStyle>(
      value: AppTheme.systemOverlayStyle,
      child: Scaffold(body: SizedBox.expand()),
    )));
    await tester.pump();

    expect(_applied, AppTheme.systemOverlayStyle);
  });
}
