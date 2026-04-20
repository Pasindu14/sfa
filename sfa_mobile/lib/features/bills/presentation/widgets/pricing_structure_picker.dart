import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/pricing/domain/entities/pricing_structure.dart';

/// Card that shows the active pricing structure and opens a bottom sheet to change it.
class PricingStructurePicker extends StatelessWidget {
  final PricingStructure? selected;
  final List<PricingStructure> structures;
  final ValueChanged<PricingStructure> onSelected;

  const PricingStructurePicker({
    super.key,
    required this.selected,
    required this.structures,
    required this.onSelected,
  });

  @override
  Widget build(BuildContext context) {
    final isEmpty = structures.isEmpty;

    return GestureDetector(
      onTap: isEmpty ? null : () => _openSheet(context),
      child: Container(
        padding: EdgeInsets.all(16.r),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(14.r),
          border: Border.all(
            color: selected != null
                ? AppColors.amber.withValues(alpha: 0.50)
                : AppColors.surfaceVariant,
          ),
          boxShadow: [
            BoxShadow(
              color: AppColors.foreground.withValues(alpha: 0.04),
              blurRadius: 10,
              offset: const Offset(0, 3),
            ),
          ],
        ),
        child: Row(
          children: [
            // Icon badge
            Container(
              width: 38.r,
              height: 38.r,
              decoration: BoxDecoration(
                color: AppColors.amber.withValues(alpha: 0.12),
                borderRadius: BorderRadius.circular(10.r),
              ),
              child: Icon(Icons.price_change_rounded,
                  size: 18.r, color: AppColors.warning),
            ),
            SizedBox(width: 12.w),

            // Label + name
            Expanded(
              child: isEmpty
                  ? Text(
                      'No pricing structures — sync required',
                      style: GoogleFonts.barlow(
                        fontSize: 12.sp,
                        color: AppColors.foregroundMuted,
                      ),
                    )
                  : Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          selected != null ? 'PRICING STRUCTURE' : 'SELECT PRICING',
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 9.sp,
                            fontWeight: FontWeight.w700,
                            letterSpacing: 2.0,
                            color: selected != null
                                ? AppColors.warning
                                : AppColors.foregroundMuted,
                          ),
                        ),
                        SizedBox(height: 2.h),
                        Row(
                          children: [
                            Flexible(
                              child: Text(
                                selected?.name ?? 'Tap to choose a price list',
                                style: GoogleFonts.barlowCondensed(
                                  fontSize: selected != null ? 16.sp : 14.sp,
                                  fontWeight: FontWeight.w700,
                                  letterSpacing: 0.3,
                                  height: 1.1,
                                  color: selected != null
                                      ? AppColors.foreground
                                      : AppColors.foregroundMuted,
                                ),
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                              ),
                            ),
                            if (selected?.isDefault == true) ...[
                              SizedBox(width: 6.w),
                              Container(
                                padding: EdgeInsets.symmetric(
                                    horizontal: 6.w, vertical: 2.h),
                                decoration: BoxDecoration(
                                  color:
                                      AppColors.amber.withValues(alpha: 0.15),
                                  borderRadius: BorderRadius.circular(4.r),
                                ),
                                child: Text(
                                  'DEFAULT',
                                  style: GoogleFonts.barlowCondensed(
                                    fontSize: 8.sp,
                                    fontWeight: FontWeight.w800,
                                    letterSpacing: 1.2,
                                    color: AppColors.warning,
                                  ),
                                ),
                              ),
                            ],
                          ],
                        ),
                      ],
                    ),
            ),

            if (!isEmpty) ...[
              Container(
                width: 28.r,
                height: 28.r,
                decoration: BoxDecoration(
                  color: AppColors.surface,
                  borderRadius: BorderRadius.circular(6.r),
                ),
                child: Icon(
                  Icons.keyboard_arrow_down_rounded,
                  size: 15.r,
                  color: AppColors.foregroundMuted,
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Future<void> _openSheet(BuildContext context) async {
    final picked = await showModalBottomSheet<PricingStructure>(
      context: context,
      backgroundColor: Colors.white,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20.r)),
      ),
      builder: (ctx) => _PricingSheet(
        structures: structures,
        selected: selected,
      ),
    );
    if (picked != null) onSelected(picked);
  }
}

class _PricingSheet extends StatelessWidget {
  final List<PricingStructure> structures;
  final PricingStructure? selected;

  const _PricingSheet({required this.structures, required this.selected});

  @override
  Widget build(BuildContext context) {
    return FractionallySizedBox(
      heightFactor: 0.75,
      child: Column(
        children: [
          // ── Drag handle ───────────────────────────────────────────────
          SizedBox(height: 10.h),
          Center(
            child: Container(
              width: 40.w,
              height: 4.h,
              decoration: BoxDecoration(
                color: AppColors.surfaceVariant,
                borderRadius: BorderRadius.circular(2.r),
              ),
            ),
          ),
          SizedBox(height: 16.h),

          // ── Section label ─────────────────────────────────────────────
          Padding(
            padding: EdgeInsets.symmetric(horizontal: 20.w),
            child: Row(
              children: [
                Container(
                  width: 3.w,
                  height: 13.h,
                  decoration: BoxDecoration(
                    color: AppColors.amber,
                    borderRadius: BorderRadius.circular(2.r),
                  ),
                ),
                SizedBox(width: 8.w),
                Text(
                  'PRICING STRUCTURES',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 11.sp,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 2.5,
                    color: AppColors.foregroundMuted,
                  ),
                ),
              ],
            ),
          ),
          SizedBox(height: 8.h),

          // ── Scrollable list ───────────────────────────────────────────
          Expanded(
            child: ListView.separated(
              padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 4.h),
              itemCount: structures.length,
              separatorBuilder: (_, __) =>
                  Divider(height: 1, color: AppColors.surfaceVariant),
              itemBuilder: (ctx, i) {
                final s = structures[i];
                final isSelected = selected?.id == s.id;
                return InkWell(
                  onTap: () => Navigator.of(ctx).pop(s),
                  borderRadius: BorderRadius.circular(8.r),
                  child: Padding(
                    padding: EdgeInsets.symmetric(
                        horizontal: 4.w, vertical: 12.h),
                    child: Row(
                      children: [
                        Container(
                          width: 36.r,
                          height: 36.r,
                          decoration: BoxDecoration(
                            color: isSelected
                                ? AppColors.amber.withValues(alpha: 0.15)
                                : AppColors.surface,
                            borderRadius: BorderRadius.circular(8.r),
                          ),
                          child: Icon(
                            Icons.price_change_rounded,
                            size: 16.r,
                            color: isSelected
                                ? AppColors.warning
                                : AppColors.foregroundMuted,
                          ),
                        ),
                        SizedBox(width: 12.w),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                s.name,
                                style: GoogleFonts.barlowCondensed(
                                  fontSize: 15.sp,
                                  fontWeight: FontWeight.w700,
                                  color: AppColors.foreground,
                                ),
                              ),
                              Text(
                                '${s.items.length} products',
                                style: GoogleFonts.barlow(
                                  fontSize: 11.sp,
                                  color: AppColors.foregroundMuted,
                                ),
                              ),
                            ],
                          ),
                        ),
                        if (s.isDefault) ...[
                          Container(
                            padding: EdgeInsets.symmetric(
                                horizontal: 6.w, vertical: 2.h),
                            decoration: BoxDecoration(
                              color: AppColors.amber.withValues(alpha: 0.15),
                              borderRadius: BorderRadius.circular(4.r),
                            ),
                            child: Text(
                              'DEFAULT',
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 8.sp,
                                fontWeight: FontWeight.w800,
                                letterSpacing: 1.2,
                                color: AppColors.warning,
                              ),
                            ),
                          ),
                          SizedBox(width: 8.w),
                        ],
                        Icon(
                          isSelected
                              ? Icons.check_circle_rounded
                              : Icons.radio_button_unchecked_rounded,
                          size: 20.r,
                          color: isSelected
                              ? AppColors.primary
                              : AppColors.surfaceVariant,
                        ),
                      ],
                    ),
                  ),
                );
              },
            ),
          ),
          SizedBox(height: 16.h),
        ],
      ),
    );
  }
}
