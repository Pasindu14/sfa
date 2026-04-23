import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/not_billings/domain/entities/not_billing_reason.dart';

class NotBillingReasonPicker extends StatelessWidget {
  final NotBillingReason? selected;
  final ValueChanged<NotBillingReason> onSelected;

  const NotBillingReasonPicker({
    super.key,
    required this.selected,
    required this.onSelected,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: () => _openSheet(context),
      child: Container(
        padding: EdgeInsets.all(16.r),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(14.r),
          border: Border.all(
            color: selected != null
                ? AppColors.primary.withValues(alpha: 0.35)
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
            Container(
              width: 38.r,
              height: 38.r,
              decoration: BoxDecoration(
                color: AppColors.primary.withValues(alpha: 0.10),
                borderRadius: BorderRadius.circular(10.r),
              ),
              child: Icon(Icons.report_problem_rounded,
                  size: 18.r, color: AppColors.primary),
            ),
            SizedBox(width: 12.w),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    selected != null ? 'REASON' : 'SELECT REASON',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 9.sp,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 2.0,
                      color: selected != null
                          ? AppColors.primary
                          : AppColors.foregroundMuted,
                    ),
                  ),
                  SizedBox(height: 2.h),
                  Text(
                    selected?.displayLabel ?? 'Why wasn\'t this outlet billed?',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: selected != null ? 16.sp : 14.sp,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 0.3,
                      height: 1.1,
                      color: selected != null
                          ? AppColors.foreground
                          : AppColors.foregroundMuted,
                    ),
                  ),
                ],
              ),
            ),
            Container(
              width: 28.r,
              height: 28.r,
              decoration: BoxDecoration(
                color: AppColors.surface,
                borderRadius: BorderRadius.circular(6.r),
              ),
              child: Icon(
                selected != null
                    ? Icons.swap_horiz_rounded
                    : Icons.keyboard_arrow_down_rounded,
                size: 15.r,
                color: AppColors.foregroundMuted,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _openSheet(BuildContext context) async {
    final picked = await showModalBottomSheet<NotBillingReason>(
      context: context,
      backgroundColor: Colors.white,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20.r)),
      ),
      builder: (ctx) => _ReasonSheet(selected: selected),
    );
    if (picked != null) onSelected(picked);
  }
}

class _ReasonSheet extends StatelessWidget {
  final NotBillingReason? selected;

  const _ReasonSheet({this.selected});

  @override
  Widget build(BuildContext context) {
    final reasons = NotBillingReason.values;

    return Padding(
      padding: EdgeInsets.only(
        bottom: MediaQuery.of(context).viewInsets.bottom,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          SizedBox(height: 10.h),
          Container(
            width: 40.w,
            height: 4.h,
            decoration: BoxDecoration(
              color: AppColors.surfaceVariant,
              borderRadius: BorderRadius.circular(2.r),
            ),
          ),
          SizedBox(height: 16.h),
          Padding(
            padding: EdgeInsets.symmetric(horizontal: 16.w),
            child: Row(
              children: [
                Container(
                  width: 3.w,
                  height: 13.h,
                  decoration: BoxDecoration(
                    color: AppColors.primary,
                    borderRadius: BorderRadius.circular(2.r),
                  ),
                ),
                SizedBox(width: 8.w),
                Text(
                  'REASON FOR NOT BILLING',
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
          ...reasons.map((r) => ListTile(
                contentPadding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 4.h),
                leading: Container(
                  width: 36.r,
                  height: 36.r,
                  decoration: BoxDecoration(
                    color: AppColors.primary.withValues(alpha: 0.08),
                    borderRadius: BorderRadius.circular(8.r),
                  ),
                  child: Icon(_iconFor(r), size: 16.r, color: AppColors.primary),
                ),
                title: Text(
                  r.displayLabel,
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 15.sp,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 0.3,
                    color: AppColors.foreground,
                  ),
                ),
                trailing: selected == r
                    ? Icon(Icons.check_circle_rounded,
                        color: AppColors.primary, size: 20.r)
                    : null,
                onTap: () => Navigator.of(context).pop(r),
              )),
          SizedBox(height: 16.h),
        ],
      ),
    );
  }

  IconData _iconFor(NotBillingReason reason) => switch (reason) {
        NotBillingReason.outletClosed => Icons.store_outlined,
        NotBillingReason.ownerAbsent => Icons.person_off_outlined,
        NotBillingReason.creditIssue => Icons.credit_card_off_outlined,
        NotBillingReason.noOrder => Icons.remove_shopping_cart_outlined,
        NotBillingReason.outOfStock => Icons.inventory_2_outlined,
      };
}
