import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/features/item_wise_achievement/data/models/item_wise_achievement_model.dart';
import 'package:uswatte/features/item_wise_achievement/domain/entities/item_wise_achievement.dart';
import 'package:uswatte/features/item_wise_achievement/domain/repositories/item_wise_achievement_repository.dart';
import 'package:uswatte/features/item_wise_achievement/domain/usecases/get_item_wise_achievement_usecase.dart';
import 'package:uswatte/features/item_wise_achievement/presentation/cubit/item_wise_achievement_cubit.dart';
import 'package:uswatte/features/item_wise_achievement/presentation/pages/item_wise_achievement_page.dart';

class _FakeRepo implements ItemWiseAchievementRepository {
  final ItemWiseAchievement data;
  _FakeRepo(this.data);

  @override
  Future<ItemWiseAchievement> getItemWiseAchievement(int year, int month) async =>
      data;
}

Map<String, dynamic> _json() => {
      'year': 2026,
      'month': 10,
      'totalTargetQuantity': 4,
      'totalSoldQuantity': 1.5,
      'totalSoldQuantityPacks': 88,
      'totalSoldAmount': 1200.5,
      'overallAchievementPercent': 25,
      'totalPendingQuantityPacks': 16,
      'totalReturnQuantityPacks': 2,
      'totalFreeIssueQuantityPacks': 8,
      'items': [
        {
          'productId': 1,
          'itemCode': 'RC125',
          'itemName': 'Real Cream Cracker 125g',
          'targetQuantity': 4,
          'soldQuantity': 1,
          'soldQuantityPacks': 48,
          'soldAmount': 800,
          'achievementPercent': 25,
          'returnQuantityPacks': 2,
          'freeIssueQuantityPacks': 8,
          'pendingQuantityPacks': 16,
          'pendingQuantity': 0.3,
          'hasTarget': true,
        },
        {
          'productId': 3,
          'itemCode': 'MS200',
          'itemName': 'Milk Sorties 200g',
          'targetQuantity': 0,
          'soldQuantity': 0.5,
          'soldQuantityPacks': 40,
          'soldAmount': 400.5,
          'achievementPercent': 0,
          'hasTarget': false,
        },
      ],
    };

void main() {
  setUpAll(() => GoogleFonts.config.allowRuntimeFetching = false);

  test('older API payloads without the new fields still parse', () {
    final model = ItemWiseAchievementModel.fromJson({
      'year': 2026,
      'month': 10,
      'totalTargetQuantity': 4,
      'totalSoldQuantity': 3,
      'items': [
        {'productId': 1, 'targetQuantity': 4, 'soldQuantity': 1},
        {'productId': 2, 'targetQuantity': 0, 'soldQuantity': 2},
      ],
    });

    expect(model.items[0].hasTarget, isTrue);
    expect(model.items[1].hasTarget, isFalse);
    // Only the targeted item counts: 1 of 4 cases, not 3 of 4.
    expect(model.overallAchievementPercent, 25);
    expect(model.items[0].pendingQuantityPacks, 0);
  });

  testWidgets('shows overall %, groups untargeted items, and the extra chips',
      (tester) async {
    final defaultOnError = FlutterError.onError!;
    FlutterError.onError = (details) {
      if (details.exceptionAsString().contains('A RenderFlex overflowed')) {
        return;
      }
      defaultOnError(details);
    };
    addTearDown(() => FlutterError.onError = defaultOnError);
    await tester.binding.setSurfaceSize(const Size(390, 1400));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    final cubit = ItemWiseAchievementCubit(GetItemWiseAchievementUseCase(
        _FakeRepo(ItemWiseAchievementModel.fromJson(_json()))))
      ..load(2026, 10);

    await tester.pumpWidget(ScreenUtilInit(
      designSize: const Size(390, 844),
      minTextAdapt: true,
      splitScreenMode: true,
      builder: (_, __) => MaterialApp(
        home: BlocProvider.value(
          value: cubit,
          child: const ItemWiseAchievementPage(),
        ),
      ),
    ));
    await tester.pumpAndSettle();

    expect(find.text('25.0%'), findsNWidgets(2)); // hero + the targeted item
    expect(find.text('ITEMS WITH A TARGET'), findsOneWidget);
    expect(find.text('SOLD WITHOUT A TARGET'), findsOneWidget);
    await tester.scrollUntilVisible(find.text('NO TARGET'), 200,
        scrollable: find.byType(Scrollable).first);
    expect(find.text('NO TARGET'), findsOneWidget);
    expect(find.text('+16 PKT PENDING APPROVAL'), findsOneWidget);
    expect(find.text('−2 PKT RETURNED'), findsOneWidget);
    expect(find.text('8 PKT FREE ISSUE'), findsOneWidget);
  });
}
