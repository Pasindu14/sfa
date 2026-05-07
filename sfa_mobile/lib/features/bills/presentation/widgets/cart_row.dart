import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_state.dart';

typedef QtyChanged = void Function(double newQty);
typedef DiscountChanged = void Function(double discountRate);
typedef TypeChanged = void Function(String billingItemType);
typedef ReturnTypeChanged = void Function(String returnType);
typedef ExpireDateChanged = void Function(DateTime? date);
typedef PriceChanged = void Function(double price);

class CartRow extends StatelessWidget {
  final CartLine line;
  final QtyChanged onChanged;
  final DiscountChanged onDiscountChanged;
  final VoidCallback onRemoved;
  final TypeChanged onTypeChanged;
  final ReturnTypeChanged onReturnTypeChanged;
  final ExpireDateChanged onExpireDateChanged;
  final PriceChanged onPriceChanged;

  const CartRow({
    super.key,
    required this.line,
    required this.onChanged,
    required this.onDiscountChanged,
    required this.onRemoved,
    required this.onTypeChanged,
    required this.onReturnTypeChanged,
    required this.onExpireDateChanged,
    required this.onPriceChanged,
  });

  double get _gross => line.quantity * line.unitPrice;
  double get _discountAmount => _gross * line.discountRate / 100;
  Color get _accentColor {
    if (line.isReturn)    return AppColors.error;
    if (line.isFreeIssue) return AppColors.success;
    return AppColors.success;
  }

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
      child: Container(
        margin: EdgeInsets.symmetric(vertical: 4.h),
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(10.r),
          gradient: LinearGradient(
            begin: Alignment.centerLeft,
            end: Alignment.centerRight,
            stops: const [0.0, 0.35, 1.0],
            colors: [
              _accentColor.withValues(alpha: 0.13),
              _accentColor.withValues(alpha: 0.05),
              _accentColor.withValues(alpha: 0.01),
            ],
          ),
          border: Border.all(
            color: _accentColor.withValues(alpha: 0.28),
            width: 1.0,
          ),
          boxShadow: [
            BoxShadow(
              color: _accentColor.withValues(alpha: 0.18),
              blurRadius: 12.r,
              spreadRadius: -2,
              offset: Offset(0, 4.h),
            ),
          ],
        ),
        padding: EdgeInsets.fromLTRB(8.w, 8.h, 6.w, 8.h),
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
                  line.isReturn
                      ? '−Rs. ${line.lineTotal.toStringAsFixed(0)}'
                      : line.isFreeIssue
                          ? 'FOC · Rs. ${line.lineTotal.toStringAsFixed(0)}'
                          : 'Rs. ${line.lineTotal.toStringAsFixed(0)}',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 14.sp,
                    fontWeight: FontWeight.w800,
                    color: line.isReturn
                        ? AppColors.error
                        : line.isFreeIssue
                            ? AppColors.success
                            : AppColors.amber,
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

            // ── Row 2: code·price · qty stepper · disc stepper ────────────
            Row(
              children: [
                // Code + unit price (tappable for return items)
                GestureDetector(
                  onTap: line.isReturn
                      ? () => _showPriceDialog(context)
                      : null,
                  child: Container(
                    padding: line.isReturn
                        ? EdgeInsets.symmetric(horizontal: 6.w, vertical: 2.h)
                        : EdgeInsets.zero,
                    decoration: line.isReturn
                        ? BoxDecoration(
                            border: Border.all(
                                color: AppColors.amber.withValues(alpha: 0.5)),
                            borderRadius: BorderRadius.circular(4.r),
                          )
                        : null,
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(
                          '${line.product.code} · Rs.${line.unitPrice.toStringAsFixed(0)}',
                          style: GoogleFonts.barlow(
                            fontSize: 10.sp,
                            color: line.isReturn
                                ? AppColors.amber
                                : Colors.white.withValues(alpha: 0.35),
                          ),
                        ),
                        if (line.isReturn) ...[
                          SizedBox(width: 3.w),
                          Icon(Icons.edit_rounded,
                              size: 9.r,
                              color: AppColors.amber.withValues(alpha: 0.7)),
                        ],
                      ],
                    ),
                  ),
                ),
                const Spacer(),
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
                if (line.isSale) ...[
                  SizedBox(width: 8.w),
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
              ],
            ),

            SizedBox(height: 6.h),

            // ── Row 3: Sale/Return toggle · return type chips ─────────────
            Row(
              children: [
                _TypeToggle(
                  current: line.billingItemType,
                  onChanged: onTypeChanged,
                ),
                if (line.isReturn) ...[
                  SizedBox(width: 8.w),
                  _ReturnTypeChip(
                    label: 'Damage',
                    selected: line.returnType == 'Damage',
                    onTap: () => onReturnTypeChanged('Damage'),
                  ),
                  SizedBox(width: 4.w),
                  _ReturnTypeChip(
                    label: 'Expire',
                    selected: line.returnType == 'Expire',
                    onTap: () => onReturnTypeChanged('Expire'),
                  ),
                  if (line.returnType == 'Expire') ...[
                    SizedBox(width: 4.w),
                    GestureDetector(
                      onTap: () => _showExpireDatePicker(context),
                      child: Container(
                        padding: EdgeInsets.symmetric(
                            horizontal: 7.w, vertical: 3.h),
                        decoration: BoxDecoration(
                          color: AppColors.error.withValues(alpha: 0.15),
                          borderRadius: BorderRadius.circular(5.r),
                          border: Border.all(
                              color: AppColors.error.withValues(alpha: 0.4)),
                        ),
                        child: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(Icons.calendar_today_rounded,
                                size: 9.r,
                                color: AppColors.error.withValues(alpha: 0.8)),
                            SizedBox(width: 3.w),
                            Text(
                              line.expireDate != null
                                  ? _formatDate(line.expireDate!)
                                  : 'Pick date',
                              style: GoogleFonts.barlow(
                                fontSize: 9.sp,
                                fontWeight: FontWeight.w600,
                                color: line.expireDate != null
                                    ? AppColors.error
                                    : AppColors.error.withValues(alpha: 0.6),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ],
                ],
              ],
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _showExpireDatePicker(BuildContext context) async {
    final today = DateTime.now();
    final initial = line.expireDate ?? today;
    final picked = await showDatePicker(
      context: context,
      initialDate: initial.isAfter(today) ? today : initial,
      firstDate: DateTime(today.year - 5),
      lastDate: today,
      helpText: 'Select expire date',
    );
    if (picked != null) onExpireDateChanged(picked);
  }

  Future<void> _showPriceDialog(BuildContext context) async {
    final controller =
        TextEditingController(text: line.unitPrice.toStringAsFixed(0));
    final result = await showDialog<double>(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: AppColors.darkSurfaceCard,
        title: Text(
          'Edit Return Price',
          style: GoogleFonts.barlowCondensed(
            color: Colors.white,
            fontSize: 16.sp,
            fontWeight: FontWeight.w700,
          ),
        ),
        content: TextField(
          controller: controller,
          autofocus: true,
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          inputFormatters: [
            FilteringTextInputFormatter.allow(RegExp(r'^\d*\.?\d*')),
          ],
          style: GoogleFonts.barlowCondensed(
            color: Colors.white,
            fontSize: 20.sp,
            fontWeight: FontWeight.w700,
          ),
          decoration: InputDecoration(
            prefixText: 'Rs. ',
            prefixStyle: GoogleFonts.barlow(
                color: Colors.white.withValues(alpha: 0.5), fontSize: 14.sp),
            enabledBorder: UnderlineInputBorder(
                borderSide: BorderSide(
                    color: Colors.white.withValues(alpha: 0.3))),
            focusedBorder: const UnderlineInputBorder(
                borderSide: BorderSide(color: AppColors.amber)),
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: Text('Cancel',
                style: GoogleFonts.barlow(
                    color: Colors.white.withValues(alpha: 0.5))),
          ),
          TextButton(
            onPressed: () {
              final v = double.tryParse(controller.text);
              Navigator.pop(ctx, v);
            },
            child: Text('Set',
                style: GoogleFonts.barlow(
                    color: AppColors.amber, fontWeight: FontWeight.w700)),
          ),
        ],
      ),
    );
    if (result != null && result >= 0) onPriceChanged(result);
  }

  String _formatDate(DateTime d) =>
      '${d.day.toString().padLeft(2, '0')}/'
      '${d.month.toString().padLeft(2, '0')}/'
      '${d.year}';

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

// ── Sale / Return toggle ──────────────────────────────────────────────────────

class _TypeToggle extends StatelessWidget {
  final String current; // 'Sale' | 'FreeIssue' | 'Return'
  final void Function(String billingItemType) onChanged;

  const _TypeToggle({
    required this.current,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: 0.06),
        borderRadius: BorderRadius.circular(6.r),
        border: Border.all(color: Colors.white.withValues(alpha: 0.12)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          _tab('Sale',   'Sale',      AppColors.success),
          _tab('FOC',    'FreeIssue', AppColors.success),
          _tab('Return', 'Return',    AppColors.error),
        ],
      ),
    );
  }

  Widget _tab(String label, String value, Color activeColor) {
    final active = current == value;
    return GestureDetector(
      onTap: active ? null : () => onChanged(value),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        padding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 3.h),
        decoration: BoxDecoration(
          color: active ? activeColor.withValues(alpha: 0.20) : Colors.transparent,
          borderRadius: BorderRadius.circular(5.r),
          border: active
              ? Border.all(color: activeColor.withValues(alpha: 0.5))
              : null,
        ),
        child: Text(
          label,
          style: GoogleFonts.barlow(
            fontSize: 9.sp,
            fontWeight: FontWeight.w700,
            letterSpacing: 0.4,
            color: active
                ? activeColor
                : Colors.white.withValues(alpha: 0.35),
          ),
        ),
      ),
    );
  }
}

// ── Return type chip ──────────────────────────────────────────────────────────

class _ReturnTypeChip extends StatelessWidget {
  final String label;
  final bool selected;
  final VoidCallback onTap;

  const _ReturnTypeChip({
    required this.label,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: selected ? null : onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 3.h),
        decoration: BoxDecoration(
          color: selected
              ? AppColors.error.withValues(alpha: 0.20)
              : Colors.white.withValues(alpha: 0.06),
          borderRadius: BorderRadius.circular(5.r),
          border: Border.all(
            color: selected
                ? AppColors.error.withValues(alpha: 0.5)
                : Colors.white.withValues(alpha: 0.12),
          ),
        ),
        child: Text(
          label,
          style: GoogleFonts.barlow(
            fontSize: 9.sp,
            fontWeight: FontWeight.w700,
            letterSpacing: 0.4,
            color: selected
                ? AppColors.error
                : Colors.white.withValues(alpha: 0.35),
          ),
        ),
      ),
    );
  }
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
              child: Icon(Icons.remove,
                  size: 11.r,
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
              child: Icon(Icons.add,
                  size: 11.r,
                  color: Colors.white.withValues(alpha: 0.80)),
            ),
          ),
        ],
      ),
    );
  }
}
