import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/pricing/domain/entities/pricing_item.dart';
import 'package:uswatte/features/pricing/domain/entities/pricing_structure.dart';

class PricingStructureDetailPage extends StatelessWidget {
  const PricingStructureDetailPage({super.key, required this.structure});

  final PricingStructure structure;

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    return Scaffold(
      backgroundColor: AppColors.background,
      body: CustomScrollView(
        slivers: [
          _DetailAppBar(
            name: structure.name,
            isDefault: structure.isDefault,
            itemCount: structure.items.length,
            onBack: () => context.pop(),
          ),
          if (structure.items.isEmpty)
            SliverFillRemaining(
              child: Center(
                child: Padding(
                  padding: EdgeInsets.all(24.r),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(Icons.price_change_rounded,
                          size: 40.r, color: AppColors.foregroundMuted),
                      SizedBox(height: 12.h),
                      Text(
                        'No prices configured for this structure.',
                        textAlign: TextAlign.center,
                        style: GoogleFonts.barlow(
                          fontSize: 13.sp,
                          color: AppColors.foregroundMuted,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            )
          else
            SliverPadding(
              padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 8.h),
              sliver: SliverList.separated(
                itemCount: structure.items.length,
                separatorBuilder: (_, __) => SizedBox(height: 8.h),
                itemBuilder: (_, i) =>
                    _PricingItemCard(item: structure.items[i]),
              ),
            ),
          SliverToBoxAdapter(child: SizedBox(height: 24.h)),
        ],
      ),
    );
  }
}

// ── App bar ───────────────────────────────────────────────────────────────────

class _DetailAppBar extends StatelessWidget {
  const _DetailAppBar({
    required this.name,
    required this.isDefault,
    required this.itemCount,
    required this.onBack,
  });

  final String name;
  final bool isDefault;
  final int itemCount;
  final VoidCallback onBack;

  @override
  Widget build(BuildContext context) {
    return SliverToBoxAdapter(
      child: Container(
        decoration: BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [AppColors.primaryDark, AppColors.primary],
          ),
        ),
        child: SafeArea(
          bottom: false,
          child: Padding(
            padding: EdgeInsets.fromLTRB(8.w, 4.h, 16.w, 16.h),
            child: Row(
              children: [
                GestureDetector(
                  onTap: onBack,
                  child: Container(
                    width: 40.r,
                    height: 40.r,
                    margin: EdgeInsets.all(4.r),
                    decoration: BoxDecoration(
                      color: Colors.white.withValues(alpha: 0.15),
                      borderRadius: BorderRadius.circular(10.r),
                      border: Border.all(
                          color: Colors.white.withValues(alpha: 0.25)),
                    ),
                    child: Icon(Icons.arrow_back_ios_new_rounded,
                        size: 15.r, color: Colors.white),
                  ),
                ),
                SizedBox(width: 4.w),
                Expanded(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Expanded(
                            child: Text(
                              name.toUpperCase(),
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 17.sp,
                                fontWeight: FontWeight.w800,
                                letterSpacing: 1.2,
                                height: 1.0,
                                color: Colors.white,
                              ),
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                          if (isDefault)
                            Container(
                              margin: EdgeInsets.only(left: 8.w),
                              padding: EdgeInsets.symmetric(
                                  horizontal: 7.w, vertical: 3.h),
                              decoration: BoxDecoration(
                                color: Colors.white.withValues(alpha: 0.20),
                                borderRadius: BorderRadius.circular(4.r),
                              ),
                              child: Text(
                                'DEFAULT',
                                style: GoogleFonts.barlowCondensed(
                                  fontSize: 9.sp,
                                  fontWeight: FontWeight.w800,
                                  letterSpacing: 0.8,
                                  color: Colors.white,
                                ),
                              ),
                            ),
                        ],
                      ),
                      SizedBox(height: 2.r),
                      Text(
                        '$itemCount products',
                        style: GoogleFonts.barlow(
                          fontSize: 11.sp,
                          color: Colors.white.withValues(alpha: 0.70),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

// ── Pricing item card ─────────────────────────────────────────────────────────

class _PricingItemCard extends StatelessWidget {
  const _PricingItemCard({required this.item});
  final PricingItem item;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.all(14.r),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12.r),
        border: Border.all(color: AppColors.surfaceVariant),
        boxShadow: [
          BoxShadow(
            color: AppColors.foreground.withValues(alpha: 0.03),
            blurRadius: 8,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: EdgeInsets.symmetric(horizontal: 7.w, vertical: 4.h),
            decoration: BoxDecoration(
              color: AppColors.surface,
              borderRadius: BorderRadius.circular(5.r),
              border: Border.all(color: AppColors.surfaceVariant),
            ),
            child: Text(
              item.productCode,
              style: GoogleFonts.barlowCondensed(
                fontSize: 10.sp,
                fontWeight: FontWeight.w700,
                letterSpacing: 0.5,
                color: AppColors.foregroundMuted,
              ),
            ),
          ),
          SizedBox(width: 12.w),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  item.productItemDescription,
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 14.sp,
                    fontWeight: FontWeight.w700,
                    height: 1.2,
                    color: AppColors.foreground,
                  ),
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                ),
                SizedBox(height: 7.h),
                Wrap(
                  spacing: 6.w,
                  runSpacing: 4.h,
                  children: [
                    if (item.dealerPackPrice != null)
                      _PriceBadge(
                          label: 'PACK',
                          value: item.dealerPackPrice!,
                          color: AppColors.primary),
                    if (item.dealerCasePrice != null)
                      _PriceBadge(
                          label: 'CASE',
                          value: item.dealerCasePrice!,
                          color: AppColors.success),
                    if (item.promotionalPrice != null)
                      _PriceBadge(
                          label: 'PROMO',
                          value: item.promotionalPrice!,
                          color: AppColors.amber),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _PriceBadge extends StatelessWidget {
  const _PriceBadge(
      {required this.label, required this.value, required this.color});
  final String label;
  final double value;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 4.h),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(5.r),
        border: Border.all(color: color.withValues(alpha: 0.20)),
      ),
      child: RichText(
        text: TextSpan(
          children: [
            TextSpan(
              text: '$label  ',
              style: GoogleFonts.barlowCondensed(
                fontSize: 8.sp,
                fontWeight: FontWeight.w700,
                letterSpacing: 0.8,
                color: color.withValues(alpha: 0.70),
              ),
            ),
            TextSpan(
              text: value.toStringAsFixed(2),
              style: GoogleFonts.barlowCondensed(
                fontSize: 13.sp,
                fontWeight: FontWeight.w800,
                color: color,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
