import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_state.dart';

typedef QtyChanged = void Function(double newQty);

class CartRow extends StatelessWidget {
  final CartLine line;
  final QtyChanged onChanged;
  final VoidCallback onRemoved;

  const CartRow({
    super.key,
    required this.line,
    required this.onChanged,
    required this.onRemoved,
  });

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
        padding: EdgeInsets.symmetric(vertical: 8.h, horizontal: 2.w),
        child: Row(
          children: [
            // Product info
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
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
                  SizedBox(height: 1.h),
                  Text(
                    '${line.product.code}  ·  Rs. ${line.unitPrice.toStringAsFixed(0)}',
                    style: GoogleFonts.barlow(
                      fontSize: 10.sp,
                      color: Colors.white.withValues(alpha: 0.45),
                    ),
                  ),
                ],
              ),
            ),
            SizedBox(width: 8.w),

            // Qty stepper
            _QtyStepper(value: line.quantity, onChanged: onChanged),
            SizedBox(width: 8.w),

            // Line total
            SizedBox(
              width: 58.w,
              child: Text(
                'Rs. ${line.lineTotal.toStringAsFixed(0)}',
                textAlign: TextAlign.right,
                style: GoogleFonts.barlowCondensed(
                  fontSize: 14.sp,
                  fontWeight: FontWeight.w800,
                  color: AppColors.amber,
                ),
              ),
            ),
            SizedBox(width: 4.w),
            // Delete button
            GestureDetector(
              onTap: onRemoved,
              child: Container(
                width: 26.r,
                height: 26.r,
                decoration: BoxDecoration(
                  color: AppColors.error.withValues(alpha: 0.15),
                  borderRadius: BorderRadius.circular(6.r),
                ),
                child: Icon(
                  Icons.close_rounded,
                  size: 13.r,
                  color: AppColors.error.withValues(alpha: 0.80),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _QtyStepper extends StatelessWidget {
  final double value;
  final ValueChanged<double> onChanged;
  const _QtyStepper({required this.value, required this.onChanged});

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(8.r),
        border: Border.all(color: Colors.white.withValues(alpha: 0.15)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          GestureDetector(
            onTap: value > 1 ? () => onChanged(value - 1) : null,
            child: Container(
              width: 28.r,
              height: 28.r,
              alignment: Alignment.center,
              child: Icon(
                Icons.remove,
                size: 13.r,
                color: value > 1
                    ? Colors.white.withValues(alpha: 0.80)
                    : Colors.white.withValues(alpha: 0.25),
              ),
            ),
          ),
          SizedBox(
            width: 30.w,
            child: Text(
              value.toStringAsFixed(
                  value.truncateToDouble() == value ? 0 : 1),
              textAlign: TextAlign.center,
              style: GoogleFonts.barlowCondensed(
                fontSize: 14.sp,
                fontWeight: FontWeight.w800,
                color: Colors.white,
              ),
            ),
          ),
          GestureDetector(
            onTap: () => onChanged(value + 1),
            child: Container(
              width: 28.r,
              height: 28.r,
              alignment: Alignment.center,
              child: Icon(
                Icons.add,
                size: 13.r,
                color: Colors.white.withValues(alpha: 0.80),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
