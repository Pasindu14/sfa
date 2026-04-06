import 'package:flutter_test/flutter_test.dart';
import 'package:uswatte/main.dart';

void main() {
  testWidgets('App renders without crashing', (WidgetTester tester) async {
    await tester.pumpWidget(const SfaApp());
    expect(find.text('SFA Uswatte'), findsNothing); // router handles initial render
  });
}
