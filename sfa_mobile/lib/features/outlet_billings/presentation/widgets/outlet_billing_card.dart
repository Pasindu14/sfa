import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/outlet_billings/domain/entities/outlet_billing_summary.dart';
import 'package:uswatte/features/outlet_billings/presentation/widgets/bill_line_tile.dart';

class OutletBillingCard extends StatefulWidget {
  final OutletBillingSummary summary;

  const OutletBillingCard({super.key, required this.summary});

  @override
  State<OutletBillingCard> createState() => _OutletBillingCardState();
}

class _OutletBillingCardState extends State<OutletBillingCard> {
  bool _expanded = false;

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: EdgeInsets.symmetric(horizontal: 16.w, vertical: 5.h),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12.r),
        border: Border.all(color: AppColors.surfaceVariant),
        boxShadow: [
          BoxShadow(
            color: AppColors.primary.withValues(alpha: 0.05),
            blurRadius: 8,
            offset: const Offset(0, 3),
          ),
        ],
      ),
      child: Column(
        children: [
          // Header — always visible
          InkWell(
            onTap: () => setState(() => _expanded = !_expanded),
            borderRadius: BorderRadius.vertical(top: Radius.circular(12.r)),
            child: Padding(
              padding: EdgeInsets.symmetric(horizontal: 14.w, vertical: 14.h),
              child: Row(
                children: [
                  Container(
                    width: 36.r,
                    height: 36.r,
                    decoration: BoxDecoration(
                      color: AppColors.primary.withValues(alpha: 0.10),
                      borderRadius: BorderRadius.circular(8.r),
                    ),
                    child: Icon(Icons.store_rounded,
                        size: 17.r, color: AppColors.primary),
                  ),
                  SizedBox(width: 12.w),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          widget.summary.outletName,
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 15.sp,
                            fontWeight: FontWeight.w700,
                            color: AppColors.foreground,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                        SizedBox(height: 2.h),
                        Text(
                          '${widget.summary.billingCount} ${widget.summary.billingCount == 1 ? 'bill' : 'bills'}',
                          style: GoogleFonts.barlow(
                            fontSize: 12.sp,
                            color: AppColors.foregroundMuted,
                          ),
                        ),
                      ],
                    ),
                  ),
                  Text(
                    'Rs. ${_formatAmount(widget.summary.totalAmount)}',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 16.sp,
                      fontWeight: FontWeight.w800,
                      color: AppColors.amber,
                    ),
                  ),
                  SizedBox(width: 6.w),
                  AnimatedRotation(
                    turns: _expanded ? 0.5 : 0,
                    duration: const Duration(milliseconds: 200),
                    child: Icon(Icons.keyboard_arrow_down_rounded,
                        size: 20.r, color: AppColors.foregroundMuted),
                  ),
                ],
              ),
            ),
          ),
          // Expanded bill list
          AnimatedCrossFade(
            duration: const Duration(milliseconds: 200),
            crossFadeState: _expanded
                ? CrossFadeState.showSecond
                : CrossFadeState.showFirst,
            firstChild: const SizedBox.shrink(),
            secondChild: Column(
              children: [
                Divider(height: 1, color: AppColors.surfaceVariant),
                SizedBox(height: 4.h),
                ...widget.summary.bills.map((b) => BillLineTile(bill: b)),
                SizedBox(height: 6.h),
              ],
            ),
          ),
        ],
      ),
    );
  }

  String _formatAmount(double amount) {
    return amount.toStringAsFixed(0).replaceAllMapped(
          RegExp(r'(\d)(?=(\d{3})+$)'),
          (m) => '${m[1]},',
        );
  }
}
