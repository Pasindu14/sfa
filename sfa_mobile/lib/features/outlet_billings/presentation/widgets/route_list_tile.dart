import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/outlet_billings/domain/entities/assigned_route.dart';

class RouteListTile extends StatelessWidget {
  final AssignedRoute route;
  final bool isSelected;
  final VoidCallback onTap;

  const RouteListTile({
    super.key,
    required this.route,
    required this.isSelected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(10.r),
      child: Container(
        margin: EdgeInsets.symmetric(horizontal: 16.w, vertical: 4.h),
        padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 14.h),
        decoration: BoxDecoration(
          color: isSelected
              ? AppColors.primary.withValues(alpha: 0.08)
              : Colors.white,
          borderRadius: BorderRadius.circular(10.r),
          border: Border.all(
            color: isSelected ? AppColors.primary : AppColors.surfaceVariant,
            width: isSelected ? 1.5 : 1,
          ),
          boxShadow: [
            BoxShadow(
              color: AppColors.primary.withValues(alpha: isSelected ? 0.08 : 0.03),
              blurRadius: 6,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: Row(
          children: [
            Container(
              width: 32.r,
              height: 32.r,
              decoration: BoxDecoration(
                color: isSelected
                    ? AppColors.primary.withValues(alpha: 0.12)
                    : AppColors.surfaceVariant.withValues(alpha: 0.5),
                borderRadius: BorderRadius.circular(8.r),
              ),
              child: Icon(
                Icons.route_rounded,
                size: 16.r,
                color: isSelected ? AppColors.primary : AppColors.foregroundMuted,
              ),
            ),
            SizedBox(width: 12.w),
            Expanded(
              child: Text(
                route.routeName,
                style: GoogleFonts.barlowCondensed(
                  fontSize: 15.sp,
                  fontWeight: isSelected ? FontWeight.w700 : FontWeight.w600,
                  letterSpacing: 0.2,
                  color: isSelected ? AppColors.primary : AppColors.foreground,
                ),
              ),
            ),
            Icon(
              Icons.chevron_right_rounded,
              size: 18.r,
              color: isSelected ? AppColors.primary : AppColors.foregroundMuted,
            ),
          ],
        ),
      ),
    );
  }
}
