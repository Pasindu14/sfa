import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/core/widgets/app_spinner.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_bloc.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_event.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_state.dart';

import 'cart_row.dart';

/// Sticky light cart panel — collapses to a slim summary bar so product search
/// stays fully visible. Tap the chevron (or the bar itself) to expand.
class CartList extends StatefulWidget {
  final CreateBillState state;
  const CartList({super.key, required this.state});

  @override
  State<CartList> createState() => _CartListState();
}

class _CartListState extends State<CartList> {
  bool _expanded = false;

  @override
  void didUpdateWidget(CartList oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.state.cart.isEmpty && widget.state.cart.isNotEmpty) {
      setState(() => _expanded = true);
    }
    if (widget.state.cart.isEmpty) {
      setState(() => _expanded = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = widget.state;
    final hasItems = state.cart.isNotEmpty;
    final keyboardUp = MediaQuery.viewInsetsOf(context).bottom > 100;

    return AnimatedSize(
      duration: const Duration(milliseconds: 220),
      curve: Curves.easeInOut,
      alignment: Alignment.bottomCenter,
      child: keyboardUp
          ? const SizedBox.shrink()
          : Container(
              decoration: BoxDecoration(
                color: AppColors.background,
                borderRadius: BorderRadius.vertical(top: Radius.circular(20.r)),
                border: Border(
                  top: BorderSide(
                    color: AppColors.primary.withValues(alpha: 0.25),
                    width: 2,
                  ),
                ),
                boxShadow: [
                  BoxShadow(
                    color: AppColors.foreground.withValues(alpha: 0.10),
                    blurRadius: 20,
                    offset: const Offset(0, -4),
                  ),
                ],
              ),
              child: SafeArea(
                top: false,
                child: _expanded
                    ? _buildExpanded(context, state)
                    : _buildCollapsed(context, state, hasItems),
              ),
            ),
    );
  }

  // ── Collapsed: single summary bar ─────────────────────────────────────────

  Widget _buildCollapsed(
      BuildContext context, CreateBillState state, bool hasItems) {
    return GestureDetector(
      behavior: HitTestBehavior.opaque,
      onTap: hasItems ? () => setState(() => _expanded = true) : null,
      child: Padding(
        padding: EdgeInsets.fromLTRB(16.w, 14.h, 16.w, 14.h),
        child: Row(
          children: [
            // Cart icon + badge
            Stack(
              clipBehavior: Clip.none,
              children: [
                Container(
                  width: 38.r,
                  height: 38.r,
                  decoration: BoxDecoration(
                    color: AppColors.primary.withValues(alpha: 0.10),
                    borderRadius: BorderRadius.circular(10.r),
                    border: Border.all(
                      color: AppColors.primary.withValues(alpha: 0.25),
                    ),
                  ),
                  child: Icon(Icons.shopping_cart_rounded,
                    size: 17.r,
                    color: AppColors.primary,
                  ),
                ),
                if (hasItems)
                  Positioned(
                    top: -4.r,
                    right: -4.r,
                    child: Container(
                      width: 16.r,
                      height: 16.r,
                      decoration: BoxDecoration(
                        color: AppColors.primary,
                        shape: BoxShape.circle,
                        border: Border.all(color: Colors.white, width: 1.5),
                      ),
                      alignment: Alignment.center,
                      child: Text(
                        '${state.cart.length}',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 8.sp,
                          fontWeight: FontWeight.w900,
                          color: Colors.white,
                        ),
                      ),
                    ),
                  ),
              ],
            ),
            SizedBox(width: 12.w),

            // Label + hint
            Expanded(
              child: hasItems
                  ? Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(
                          '${state.cart.length} item${state.cart.length == 1 ? '' : 's'}  ·  tap to review',
                          style: GoogleFonts.barlow(
                            fontSize: 11.sp,
                            color: AppColors.foregroundMuted,
                          ),
                        ),
                        Text(
                          'Rs. ${state.total.toStringAsFixed(2)}',
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 19.sp,
                            fontWeight: FontWeight.w900,
                            letterSpacing: -0.3,
                            color: AppColors.primaryDark,
                          ),
                        ),
                      ],
                    )
                  : Text(
                      'Add products above to start an order',
                      style: GoogleFonts.barlow(
                        fontSize: 12.sp,
                        color: AppColors.foregroundMuted,
                      ),
                    ),
            ),

            // CTA or expand chevron
            if (hasItems) ...[
              SizedBox(width: 8.w),
              Container(
                width: 30.r,
                height: 30.r,
                decoration: BoxDecoration(
                  color: AppColors.surface,
                  borderRadius: BorderRadius.circular(8.r),
                  border: Border.all(color: AppColors.surfaceVariant),
                ),
                child: Icon(Icons.keyboard_arrow_up_rounded,
                  size: 18.r,
                  color: AppColors.foregroundMuted,
                ),
              ),
              SizedBox(width: 8.w),
              _CtaButton(state: state, compact: true),
            ],
          ],
        ),
      ),
    );
  }

  // ── Expanded: full cart panel ──────────────────────────────────────────────

  Widget _buildExpanded(BuildContext context, CreateBillState state) {
    return Padding(
      padding: EdgeInsets.fromLTRB(16.w, 14.h, 16.w, 12.h),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Header row with collapse chevron
          GestureDetector(
            onTap: () => setState(() => _expanded = false),
            behavior: HitTestBehavior.opaque,
            child: Row(
              children: [
                Container(
                  width: 30.r,
                  height: 30.r,
                  decoration: BoxDecoration(
                    color: AppColors.primary.withValues(alpha: 0.10),
                    borderRadius: BorderRadius.circular(8.r),
                    border: Border.all(
                      color: AppColors.primary.withValues(alpha: 0.25),
                    ),
                  ),
                  child: Icon(Icons.shopping_cart_rounded,
                      size: 14.r, color: AppColors.primary),
                ),
                SizedBox(width: 8.w),
                Text(
                  'CART',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 13.sp,
                    fontWeight: FontWeight.w800,
                    letterSpacing: 2.0,
                    color: AppColors.foreground,
                  ),
                ),
                SizedBox(width: 6.w),
                Container(
                  padding: EdgeInsets.symmetric(horizontal: 7.w, vertical: 2.h),
                  decoration: BoxDecoration(
                    color: AppColors.primary,
                    borderRadius: BorderRadius.circular(10.r),
                  ),
                  child: Text(
                    '${state.cart.length}',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 11.sp,
                      fontWeight: FontWeight.w800,
                      color: Colors.white,
                    ),
                  ),
                ),
                const Spacer(),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    if (state.hasReturns)
                      Text(
                        'Rs. ${state.saleSubTotal.toStringAsFixed(2)}',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 11.sp,
                          fontWeight: FontWeight.w600,
                          color: AppColors.foregroundMuted,
                          decoration: TextDecoration.lineThrough,
                          decorationColor: AppColors.foregroundMuted,
                        ),
                      ),
                    Text(
                      'Rs. ${state.total.toStringAsFixed(2)}',
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 19.sp,
                        fontWeight: FontWeight.w900,
                        letterSpacing: -0.3,
                        color: AppColors.primaryDark,
                      ),
                    ),
                  ],
                ),
                SizedBox(width: 8.w),
                Container(
                  width: 28.r,
                  height: 28.r,
                  decoration: BoxDecoration(
                    color: AppColors.surface,
                    borderRadius: BorderRadius.circular(7.r),
                    border: Border.all(color: AppColors.surfaceVariant),
                  ),
                  child: Icon(Icons.keyboard_arrow_down_rounded,
                    size: 16.r,
                    color: AppColors.foregroundMuted,
                  ),
                ),
              ],
            ),
          ),

          SizedBox(height: 12.h),
          Divider(color: AppColors.surfaceVariant, height: 1),
          SizedBox(height: 8.h),

          // Line items — taller max height
          ConstrainedBox(
            constraints: BoxConstraints(maxHeight: 280.h),
            child: ListView.separated(
              shrinkWrap: true,
              padding: EdgeInsets.symmetric(vertical: 2.h),
              itemCount: state.cart.length,
              separatorBuilder: (_, __) => SizedBox(height: 4.h),
              itemBuilder: (ctx, i) {
                final line = state.cart[i];
                return CartRow(
                  line: line,
                  onChanged: (q) => ctx
                      .read<CreateBillBloc>()
                      .add(CartItemQtyChanged(line.lineNumber, q)),
                  onDiscountChanged: (d) => ctx
                      .read<CreateBillBloc>()
                      .add(CartItemDiscountChanged(line.lineNumber, d)),
                  onRemoved: () => ctx
                      .read<CreateBillBloc>()
                      .add(CartItemRemoved(line.lineNumber)),
                  onTypeChanged: (t) => ctx
                      .read<CreateBillBloc>()
                      .add(CartItemTypeChanged(line.lineNumber, t)),
                  onReturnTypeChanged: (rt) => ctx
                      .read<CreateBillBloc>()
                      .add(CartItemReturnTypeChanged(line.lineNumber, rt)),
                  onFreeIssueSourceChanged: (s) => ctx
                      .read<CreateBillBloc>()
                      .add(CartItemFreeIssueSourceChanged(line.lineNumber, s)),
                  onExpireDateChanged: (d) => ctx
                      .read<CreateBillBloc>()
                      .add(CartItemExpireDateChanged(line.lineNumber, d)),
                  onPriceChanged: (p) => ctx
                      .read<CreateBillBloc>()
                      .add(CartItemPriceChanged(line.lineNumber, p)),
                );
              },
            ),
          ),

          SizedBox(height: 10.h),
          Divider(color: AppColors.surfaceVariant, height: 1),
          SizedBox(height: 10.h),

          // Total breakdown
          if (state.hasReturns || state.hasFreeIssues) ...[
            _TotalRow(
              label: 'SALES',
              value: 'Rs. ${state.saleSubTotal.toStringAsFixed(2)}',
              valueColor: AppColors.foreground,
            ),
            if (state.billDiscountRate > 0)
              _TotalRow(
                label: 'DISCOUNT',
                value: '−Rs. ${state.billDiscountAmount.toStringAsFixed(2)}',
                valueColor: AppColors.success,
              ),
            if (state.hasReturns)
              _TotalRow(
                label: 'RETURNS',
                value: '−Rs. ${state.returnTotal.toStringAsFixed(2)}',
                valueColor: AppColors.error,
              ),
            if (state.hasFreeIssues) ...[
              _TotalRow(
                label: 'FREE ISSUES (info)',
                value: 'Rs. ${state.freeIssueValue.toStringAsFixed(2)}',
                valueColor: AppColors.primary,
              ),
              if (state.freeIssueValueCompany > 0 &&
                  state.freeIssueValueDistributor > 0) ...[
                _TotalRow(
                  label: '  · BY COMPANY',
                  value:
                      'Rs. ${state.freeIssueValueCompany.toStringAsFixed(2)}',
                  valueColor: AppColors.primary,
                ),
                _TotalRow(
                  label: '  · BY DISTRIBUTOR',
                  value:
                      'Rs. ${state.freeIssueValueDistributor.toStringAsFixed(2)}',
                  valueColor: AppColors.primary,
                ),
              ],
            ],
            SizedBox(height: 6.h),
            Divider(color: AppColors.surfaceVariant, height: 1),
            SizedBox(height: 6.h),
          ],
          _TotalRow(
            label: 'NET TOTAL',
            value: 'Rs. ${state.total.toStringAsFixed(2)}',
            valueColor: AppColors.primaryDark,
            large: true,
          ),

          SizedBox(height: 14.h),

          _CtaButton(state: state),
        ],
      ),
    );
  }
}

// ── Total row helper ──────────────────────────────────────────────────────────

class _TotalRow extends StatelessWidget {
  final String label;
  final String value;
  final Color valueColor;
  final bool large;

  const _TotalRow({
    required this.label,
    required this.value,
    required this.valueColor,
    this.large = false,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          label,
          style: GoogleFonts.barlowCondensed(
            fontSize: large ? 12.sp : 10.sp,
            fontWeight: FontWeight.w600,
            letterSpacing: 1.2,
            color: large
                ? AppColors.foregroundMuted
                : AppColors.foregroundMuted,
          ),
        ),
        Text(
          value,
          style: GoogleFonts.barlowCondensed(
            fontSize: large ? 19.sp : 13.sp,
            fontWeight: large ? FontWeight.w800 : FontWeight.w600,
            color: valueColor,
          ),
        ),
      ],
    );
  }
}

// ── Shared Create Order button ────────────────────────────────────────────────

class _CtaButton extends StatelessWidget {
  const _CtaButton({required this.state, this.compact = false});
  final CreateBillState state;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: state.canSubmit
            ? () =>
                context.read<CreateBillBloc>().add(const SubmitPressed())
            : null,
        borderRadius: BorderRadius.circular(12.r),
        child: Ink(
          height: compact ? 44.h : 54.h,
          padding: compact
              ? EdgeInsets.symmetric(horizontal: 16.w)
              : EdgeInsets.zero,
          decoration: BoxDecoration(
            color: state.canSubmit
                ? AppColors.primary
                : AppColors.primary.withValues(alpha: 0.30),
            borderRadius: BorderRadius.circular(12.r),
            boxShadow: state.canSubmit
                ? [
                    BoxShadow(
                      color: AppColors.primary.withValues(alpha: 0.30),
                      blurRadius: 12,
                      offset: const Offset(0, 4),
                    ),
                  ]
                : null,
          ),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              if (state.submitting)
                const AppSpinner.button()
              else
                Icon(Icons.check_circle_outline_rounded,
                    size: compact ? 15.r : 18.r, color: Colors.white),
              SizedBox(width: 8.w),
              Text(
                state.submitting ? 'SAVING…' : 'CREATE ORDER',
                style: GoogleFonts.barlowCondensed(
                  fontSize: compact ? 13.sp : 17.sp,
                  fontWeight: FontWeight.w800,
                  letterSpacing: compact ? 1.2 : 2.0,
                  color: Colors.white,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
