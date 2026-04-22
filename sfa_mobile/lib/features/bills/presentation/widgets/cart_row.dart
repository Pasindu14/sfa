import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_state.dart';

typedef QtyChanged = void Function(double newQty);
typedef DiscountChanged = void Function(double discountRate);

class CartRow extends StatelessWidget {
  final CartLine line;
  final QtyChanged onChanged;
  final DiscountChanged onDiscountChanged;
  final VoidCallback onRemoved;

  const CartRow({
    super.key,
    required this.line,
    required this.onChanged,
    required this.onDiscountChanged,
    required this.onRemoved,
  });

  double get _gross => line.quantity * line.unitPrice;
  double get _discountAmount => _gross * line.discountRate / 100;

  @override
  Widget build(BuildContext context) {
    return Dismissible(
      key: ValueKey('cart-line-${line.lineNumber}'),
      direction: DismissDirection.endToStart,
      background: Container(
        alignment: Alignment.centerRight,
        padding: EdgeInsets.symmetric(horizontal: 16.w),
        color: AppColors.error,
        child: Icon(Icons.delete_rounded, color: Colors.white, size: 18.r),
      ),
      onDismissed: (_) => onRemoved(),
      child: Padding(
        padding: EdgeInsets.fromLTRB(2.w, 8.h, 2.w, 8.h),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // ── Row 1: name · net total · delete ──────────────────────────
            Row(
              children: [
                Expanded(
                  child: Text(
                    line.product.itemDescription,
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 13.sp,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 0.2,
                      color: Colors.white,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
                SizedBox(width: 8.w),
                // Gross struck-through when discounted
                if (line.discountRate > 0) ...[
                  Text(
                    'Rs. ${_gross.toStringAsFixed(0)}',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 11.sp,
                      color: Colors.white.withValues(alpha: 0.35),
                      decoration: TextDecoration.lineThrough,
                      decorationColor: Colors.white.withValues(alpha: 0.35),
                    ),
                  ),
                  SizedBox(width: 4.w),
                ],
                Text(
                  'Rs. ${line.lineTotal.toStringAsFixed(0)}',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 14.sp,
                    fontWeight: FontWeight.w800,
                    color: AppColors.amber,
                  ),
                ),
                SizedBox(width: 6.w),
                GestureDetector(
                  onTap: onRemoved,
                  child: Container(
                    width: 22.r,
                    height: 22.r,
                    decoration: BoxDecoration(
                      color: AppColors.error.withValues(alpha: 0.15),
                      borderRadius: BorderRadius.circular(5.r),
                    ),
                    child: Icon(Icons.close_rounded,
                        size: 11.r,
                        color: AppColors.error.withValues(alpha: 0.80)),
                  ),
                ),
              ],
            ),

            SizedBox(height: 6.h),

            // ── Row 2: code·price · qty stepper · disc stepper · disc amt ─
            Row(
              children: [
                // Code + unit price
                Text(
                  '${line.product.code} · Rs.${line.unitPrice.toStringAsFixed(0)}',
                  style: GoogleFonts.barlow(
                    fontSize: 10.sp,
                    color: Colors.white.withValues(alpha: 0.35),
                  ),
                ),
                const Spacer(),
                // Qty label + stepper
                _inlineLabel('Qty'),
                SizedBox(width: 4.w),
                _Stepper(
                  value: line.quantity,
                  displayText: line.quantity.toStringAsFixed(
                      line.quantity.truncateToDouble() == line.quantity ? 0 : 1),
                  canDecrement: line.quantity > 1,
                  onDecrement: () => onChanged(line.quantity - 1),
                  onIncrement: () => onChanged(line.quantity + 1),
                  valueColor: Colors.white,
                ),
                SizedBox(width: 8.w),
                // Disc label + stepper
                _inlineLabel('Disc'),
                SizedBox(width: 4.w),
                _Stepper(
                  value: line.discountRate,
                  displayText:
                      '${line.discountRate.toStringAsFixed(line.discountRate.truncateToDouble() == line.discountRate ? 0 : 1)}%',
                  canDecrement: line.discountRate > 0,
                  onDecrement: () =>
                      onDiscountChanged((line.discountRate - 1).clamp(0, 100)),
                  onIncrement: () =>
                      onDiscountChanged((line.discountRate + 1).clamp(0, 100)),
                  valueColor: line.discountRate > 0
                      ? AppColors.amber
                      : Colors.white.withValues(alpha: 0.40),
                ),
                // Discount amount badge
                if (line.discountRate > 0) ...[
                  SizedBox(width: 6.w),
                  Text(
                    '−Rs.${_discountAmount.toStringAsFixed(0)}',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 11.sp,
                      fontWeight: FontWeight.w700,
                      color: AppColors.error.withValues(alpha: 0.75),
                    ),
                  ),
                ],
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _inlineLabel(String text) => Text(
        text,
        style: GoogleFonts.barlow(
          fontSize: 9.sp,
          fontWeight: FontWeight.w600,
          letterSpacing: 0.5,
          color: Colors.white.withValues(alpha: 0.35),
        ),
      );
}

// ── Generic stepper ───────────────────────────────────────────────────────────

class _Stepper extends StatelessWidget {
  final double value;
  final String displayText;
  final bool canDecrement;
  final VoidCallback onDecrement;
  final VoidCallback onIncrement;
  final Color valueColor;

  const _Stepper({
    required this.value,
    required this.displayText,
    required this.canDecrement,
    required this.onDecrement,
    required this.onIncrement,
    required this.valueColor,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(7.r),
        border: Border.all(color: Colors.white.withValues(alpha: 0.15)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          GestureDetector(
            onTap: canDecrement ? onDecrement : null,
            child: SizedBox(
              width: 24.r,
              height: 24.r,
              child: Icon(Icons.remove, size: 11.r,
                  color: canDecrement
                      ? Colors.white.withValues(alpha: 0.80)
                      : Colors.white.withValues(alpha: 0.20)),
            ),
          ),
          SizedBox(
            width: 32.w,
            child: Text(
              displayText,
              textAlign: TextAlign.center,
              style: GoogleFonts.barlowCondensed(
                fontSize: 13.sp,
                fontWeight: FontWeight.w800,
                color: valueColor,
              ),
            ),
          ),
          GestureDetector(
            onTap: onIncrement,
            child: SizedBox(
              width: 24.r,
              height: 24.r,
              child: Icon(Icons.add, size: 11.r,
                  color: Colors.white.withValues(alpha: 0.80)),
            ),
          ),
        ],
      ),
    );
  }
}
