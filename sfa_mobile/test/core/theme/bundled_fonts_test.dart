// Guards the bundled google_fonts faces.
//
// main.dart sets GoogleFonts.config.allowRuntimeFetching = false, so a
// GoogleFonts.<family>(fontWeight: …) call whose weight/style has no file in
// google_fonts/ logs an exception and renders in the platform fallback font on
// every device. Nothing at compile time catches that, so this test scans lib/
// for every GoogleFonts call, derives each weight/style it can request, and
// loads each one from the real asset bundle with fetching disabled.
import 'dart:async';
import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';

/// GoogleFonts method name → Google Fonts family name. A new family in lib/
/// fails the scan below until its TTFs are added to google_fonts/ and it is
/// listed here.
const _families = {
  'barlow': 'Barlow',
  'barlowCondensed': 'Barlow Condensed',
  'robotoMono': 'Roboto Mono',
  'sourceCodePro': 'Source Code Pro',
};

class _Use {
  _Use(this.family, this.weight, this.style, this.where);
  final String family;
  final FontWeight weight;
  final FontStyle style;
  final String where;
  String get key => '$family/${weight.value}/${style.name}';
}

/// Every (family, weight, style) a GoogleFonts call in lib/ can request.
/// Ternaries contribute each literal weight they name. Reads the directory
/// directly (not git), so git-ignored sources such as the debug page count.
({List<_Use> uses, List<String> problems}) _scanLib() {
  final uses = <_Use>[];
  final problems = <String>[];
  final callRe = RegExp(r'GoogleFonts\.(\w+)\(');
  final weightRe = RegExp(r'FontWeight\.(w\d00|bold|normal)');

  for (final entity in Directory('lib').listSync(recursive: true)) {
    if (entity is! File || !entity.path.endsWith('.dart')) continue;
    final src = entity.readAsStringSync();
    for (final m in callRe.allMatches(src)) {
      final method = m.group(1)!;
      if (method.endsWith('TextTheme') || method == 'config') continue;
      final line = '\n'.allMatches(src.substring(0, m.start)).length + 1;
      final where = '${entity.path}:$line';

      final family = _families[method];
      if (family == null) {
        problems.add(
          '$where uses GoogleFonts.$method — bundle its TTFs in '
          'google_fonts/ and add it to _families',
        );
        continue;
      }

      // Argument list up to the matching ')'.
      var depth = 1, i = m.end;
      while (depth > 0 && i < src.length) {
        if (src[i] == '(') depth++;
        if (src[i] == ')') depth--;
        i++;
      }
      final args = src.substring(m.end, i - 1);
      if (args.contains('textStyle:')) {
        problems.add(
          '$where passes textStyle: — weight cannot be derived '
          'statically; check it by hand and extend this test',
        );
      }

      final styles = <FontStyle>{
        if (!args.contains('fontStyle:') ||
            args.contains('FontStyle.normal') ||
            !args.contains('FontStyle.italic'))
          FontStyle.normal,
        if (args.contains('FontStyle.italic')) FontStyle.italic,
      };

      final weightArg = RegExp(r'fontWeight:\s*([^,]+)').firstMatch(args);
      final weights = <FontWeight>{};
      if (weightArg == null) {
        weights.add(FontWeight.w400);
      } else {
        for (final w in weightRe.allMatches(weightArg.group(1)!)) {
          weights.add(switch (w.group(1)!) {
            'bold' => FontWeight.w700,
            'normal' => FontWeight.w400,
            final v => FontWeight.values.firstWhere(
              (fw) => fw.value == int.parse(v.substring(1)),
            ),
          });
        }
        if (weights.isEmpty) {
          problems.add(
            '$where has a non-literal fontWeight — check it by hand '
            'and extend this test',
          );
        }
      }

      for (final w in weights) {
        for (final s in styles) {
          uses.add(_Use(family, w, s, where));
        }
      }
    }
  }
  return (uses: uses, problems: problems);
}

/// Requests every style, then waits for the loads. A missing asset surfaces
/// either through [GoogleFonts.pendingFonts] or as an uncaught async error;
/// both are collected.
Future<List<Object>> _loadAll(void Function() request) async {
  final errors = <Object>[];
  await runZonedGuarded(
    () async {
      request();
      try {
        await GoogleFonts.pendingFonts();
      } catch (e) {
        errors.add(e);
      }
      // Let the package's derived futures settle and report.
      await Future<void>.delayed(Duration.zero);
    },
    (e, _) => errors.add(e),
    zoneSpecification: ZoneSpecification(
      print: (_, __, ___, ____) {}, // google_fonts prints its own diagnostics
    ),
  );
  return errors;
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUpAll(() => GoogleFonts.config.allowRuntimeFetching = false);

  test('every GoogleFonts call in lib/ is statically checkable', () {
    final scan = _scanLib();
    expect(scan.uses, isNotEmpty, reason: 'scan found no GoogleFonts calls');
    expect(scan.problems, isEmpty);
  });

  test(
    'every weight/style used in lib/ loads from the bundled assets',
    () async {
      final byKey = <String, _Use>{};
      for (final u in _scanLib().uses) {
        byKey.putIfAbsent(u.key, () => u);
      }

      for (final u in byKey.values) {
        final errors = await _loadAll(
          () => GoogleFonts.getFont(
            u.family,
            fontWeight: u.weight,
            fontStyle: u.style,
          ),
        );
        expect(
          errors,
          isEmpty,
          reason: '${u.key} (first used at ${u.where}) has no bundled file',
        );
      }
    },
  );

  test('the app theme text styles load from the bundled assets', () async {
    final errors = await _loadAll(() => AppTheme.light);
    expect(errors, isEmpty);
  });

  test(
    'control: an unbundled weight is reported, so the checks above bite',
    () async {
      // Barlow Thin is deliberately not bundled — nothing uses it.
      final errors = await _loadAll(
        () => GoogleFonts.barlow(fontWeight: FontWeight.w100),
      );
      expect(errors, isNotEmpty);
    },
  );
}
