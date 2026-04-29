import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';

class MonthFilterChips extends StatelessWidget {
  final int selectedOffset;
  final String Function(int offset) labelBuilder;
  final ValueChanged<int> onChanged;

  const MonthFilterChips({
    super.key,
    required this.selectedOffset,
    required this.labelBuilder,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 8.h),
      child: Row(
        children: List.generate(3, (index) {
          final isSelected = index == selectedOffset;
          return Expanded(
            child: Padding(
              padding: EdgeInsets.only(right: index < 2 ? 8.w : 0),
              child: GestureDetector(
                onTap: () => onChanged(index),
                child: AnimatedContainer(
                  duration: const Duration(milliseconds: 200),
                  padding: EdgeInsets.symmetric(vertical: 10.h),
                  decoration: BoxDecoration(
                    color: isSelected
                        ? AppColors.primary
                        : AppColors.surface,
                    borderRadius: BorderRadius.circular(8.r),
                    border: Border.all(
                      color: isSelected
                          ? AppColors.primary
                          : AppColors.surfaceVariant,
                    ),
                    boxShadow: isSelected
                        ? [
                            BoxShadow(
                              color: AppColors.primary.withValues(alpha: 0.25),
                              blurRadius: 6,
                              offset: const Offset(0, 2),
                            )
                          ]
                        : null,
                  ),
                  child: Text(
                    labelBuilder(index),
                    textAlign: TextAlign.center,
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 13.sp,
                      fontWeight:
                          isSelected ? FontWeight.w700 : FontWeight.w500,
                      letterSpacing: 0.3,
                      color: isSelected
                          ? AppColors.onPrimary
                          : AppColors.foreground,
                    ),
                  ),
                ),
              ),
            ),
          );
        }),
      ),
    );
  }
}
