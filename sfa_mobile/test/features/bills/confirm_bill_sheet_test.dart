// Guards the cancel-vs-confirm contract of the close-bill remarks sheet (SFA-120).
//
// The sheet's return value is what decides whether a bill is submitted at all:
// `null` means the rep backed out and nothing must be sent, while an empty
// string means they confirmed without writing a remark. If those two ever
// collapse into one value, dismissing the sheet would silently submit the bill
// (or confirming would silently do nothing) — neither is visible in the UI.
import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_state.dart';
import 'package:uswatte/features/bills/presentation/widgets/confirm_bill_sheet.dart';

/// Pumps a bare button that opens the sheet and records whatever it returns.
/// Deliberately avoids CreateBillBloc and getIt — the sheet is a pure
/// input/output widget, so a faithful miniature is enough.
Future<void> _pumpHost(
  WidgetTester tester, {
  required void Function(String?) onResult,
}) async {
  // Match the design baseline so ScreenUtil scales 1:1 as it does on a handset.
  await tester.binding.setSurfaceSize(const Size(390, 844));
  addTearDown(() => tester.binding.setSurfaceSize(null));

  // flutter_test's default font draws every glyph as a square of width
  // fontSize, so labels measure roughly twice as wide as the real Barlow
  // Condensed faces and manufacture overflows that no device shows. Layout is
  // not what this test guards — the sheet's return-value contract is — so
  // overflow reports are dropped while every other error still fails the test.
  final defaultOnError = FlutterError.onError!;
  FlutterError.onError = (details) {
    if (details.exceptionAsString().contains('A RenderFlex overflowed')) return;
    defaultOnError(details);
  };
  addTearDown(() => FlutterError.onError = defaultOnError);

  await tester.pumpWidget(
    ScreenUtilInit(
      designSize: const Size(390, 844),
      minTextAdapt: true,
      splitScreenMode: true,
      builder: (context, child) => MaterialApp(
        home: Scaffold(
          body: Builder(
            builder: (ctx) => ElevatedButton(
              onPressed: () async {
                final result = await showConfirmBillSheet(
                  ctx,
                  state: const CreateBillState(),
                );
                onResult(result);
              },
              child: const Text('open'),
            ),
          ),
        ),
      ),
    ),
  );
  await tester.tap(find.text('open'));
  await tester.pumpAndSettle();
}

void main() {
  setUpAll(() {
    // Tests have no network; without this google_fonts tries to fetch and the
    // pump drowns in exceptions instead of falling back to a bundled face.
    GoogleFonts.config.allowRuntimeFetching = false;
  });

  testWidgets('CANCEL returns null so the bill is never submitted',
      (tester) async {
    String? result;
    var called = false;
    await _pumpHost(tester, onResult: (r) {
      result = r;
      called = true;
    });

    expect(find.text('Confirm Order'), findsOneWidget);

    await tester.tap(find.text('CANCEL'));
    await tester.pumpAndSettle();

    expect(called, isTrue);
    expect(result, isNull,
        reason: 'null is the only signal the caller has that the rep backed out');
  });

  testWidgets('confirming carries the typed remark back to the caller',
      (tester) async {
    String? result;
    await _pumpHost(tester, onResult: (r) => result = r);

    await tester.enterText(
        find.byType(TextField), '  Shop closed early  ');
    await tester.pumpAndSettle();

    await tester.tap(find.text('CONFIRM & SAVE'));
    await tester.pumpAndSettle();

    expect(result, 'Shop closed early',
        reason: 'the sheet trims before handing the remark over');
  });

  testWidgets('confirming without typing returns empty, not null',
      (tester) async {
    String? result;
    var called = false;
    await _pumpHost(tester, onResult: (r) {
      result = r;
      called = true;
    });

    await tester.tap(find.text('CONFIRM & SAVE'));
    await tester.pumpAndSettle();

    expect(called, isTrue);
    expect(result, isNotNull,
        reason: 'an empty remark is still a confirmation — the bill must submit');
    expect(result, isEmpty);
  });
}
