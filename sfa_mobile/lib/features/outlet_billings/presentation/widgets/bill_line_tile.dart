import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/outlet_billings/domain/entities/bill_line.dart';

class BillLineTile extends StatelessWidget {
  final BillLine bill;

  const BillLineTile({super.key, required this.bill});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.symmetric(horizontal: 14.w, vertical: 8.h),
      child: Row(
        children: [
          Expanded(
            flex: 3,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  bill.billingNumber,
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 13.sp,
                    fontWeight: FontWeight.w700,
                    color: AppColors.foreground,
                  ),
                ),
                SizedBox(height: 2.h),
                Text(
                  _formatDate(bill.billingDate),
                  style: GoogleFonts.barlow(
                    fontSize: 11.sp,
                    color: AppColors.foregroundMuted,
                  ),
                ),
              ],
            ),
          ),
          _StatusChip(status: bill.status),
          SizedBox(width: 10.w),
          Text(
            'Rs. ${_formatAmount(bill.totalAmount)}',
            style: GoogleFonts.barlowCondensed(
              fontSize: 13.sp,
              fontWeight: FontWeight.w700,
              color: AppColors.amber,
            ),
          ),
        ],
      ),
    );
  }

  String _formatDate(String date) {
    try {
      final d = DateTime.parse(date);
      const months = [
        'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
        'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
      ];
      return '${d.day} ${months[d.month - 1]}';
    } catch (_) {
      return date;
    }
  }

  String _formatAmount(double amount) {
    if (amount >= 1000) {
      return amount.toStringAsFixed(0).replaceAllMapped(
            RegExp(r'(\d)(?=(\d{3})+$)'),
            (m) => '${m[1]},',
          );
    }
    return amount.toStringAsFixed(2);
  }
}

class _StatusChip extends StatelessWidget {
  final String status;

  const _StatusChip({required this.status});

  @override
  Widget build(BuildContext context) {
    final (color, icon) = switch (status.toLowerCase()) {
      'approved' => (AppColors.success, Icons.check_circle_outline_rounded),
      'cancelled' => (AppColors.error, Icons.cancel_outlined),
      _ => (AppColors.warning, Icons.schedule_rounded),
    };
    return Container(
      margin: EdgeInsets.only(right: 8.w),
      padding: EdgeInsets.symmetric(horizontal: 7.w, vertical: 3.h),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(999),
        border: Border.all(color: color.withValues(alpha: 0.35)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 9.r, color: color),
          SizedBox(width: 3.w),
          Text(
            status,
            style: GoogleFonts.barlow(
              fontSize: 10.sp,
              fontWeight: FontWeight.w600,
              color: color,
            ),
          ),
        ],
      ),
    );
  }
}
