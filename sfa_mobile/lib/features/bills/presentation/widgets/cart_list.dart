import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_bloc.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_event.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_state.dart';

import 'cart_row.dart';

/// Sticky dark cart panel — collapses to a slim summary bar so product search
/// stays fully visible. Tap the chevron (or the bar itself) to expand.
class CartList extends StatefulWidget {
  final CreateBillState state;
  const CartList({super.key, required this.state});

  @override
  State<CartList> createState() => _CartListState();
}

class _CartListState extends State<CartList> {
  bool _expanded = false;

  // Auto-expand when the first item is added so the rep gets feedback.
  @override
  void didUpdateWidget(CartList oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.state.cart.isEmpty && widget.state.cart.isNotEmpty) {
      setState(() => _expanded = true);
    }
    // Auto-collapse when cart is cleared.
    if (widget.state.cart.isEmpty) {
      setState(() => _expanded = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = widget.state;
    final hasItems = state.cart.isNotEmpty;

    // Hide the cart panel while the keyboard is open so the product search
    // results have the full screen. The rep is in "search mode" and doesn't
    // need the cart visible — it will reappear when they dismiss the keyboard.
    final keyboardUp = MediaQuery.viewInsetsOf(context).bottom > 100;

    return AnimatedSize(
      duration: const Duration(milliseconds: 220),
      curve: Curves.easeInOut,
      alignment: Alignment.bottomCenter,
      child: keyboardUp
          ? const SizedBox.shrink()
          : Container(
        decoration: BoxDecoration(
          color: AppColors.darkSurface,
          borderRadius: BorderRadius.vertical(top: Radius.circular(20.r)),
          boxShadow: [
            BoxShadow(
              color: AppColors.foreground.withValues(alpha: 0.25),
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
        padding: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 14.h),
        child: Row(
          children: [
            // Cart icon + badge
            Stack(
              clipBehavior: Clip.none,
              children: [
                Container(
                  width: 36.r,
                  height: 36.r,
                  decoration: BoxDecoration(
                    color: AppColors.primary.withValues(alpha: 0.20),
                    borderRadius: BorderRadius.circular(9.r),
                  ),
                  child: Icon(Icons.shopping_cart_rounded,
                      size: 16.r, color: AppColors.primary),
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
                        border: Border.all(
                            color: AppColors.darkSurface, width: 1.5),
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
                            color: Colors.white.withValues(alpha: 0.50),
                          ),
                        ),
                        Text(
                          'Rs. ${state.total.toStringAsFixed(2)}',
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 18.sp,
                            fontWeight: FontWeight.w900,
                            letterSpacing: -0.3,
                            color: AppColors.amber,
                          ),
                        ),
                      ],
                    )
                  : Text(
                      'Add products above to start an order',
                      style: GoogleFonts.barlow(
                        fontSize: 12.sp,
                        color: Colors.white.withValues(alpha: 0.35),
                      ),
                    ),
            ),

            // CTA or expand chevron
            if (hasItems) ...[
              SizedBox(width: 8.w),
              // Expand chevron
              Container(
                width: 30.r,
                height: 30.r,
                decoration: BoxDecoration(
                  color: Colors.white.withValues(alpha: 0.08),
                  borderRadius: BorderRadius.circular(7.r),
                ),
                child: Icon(Icons.keyboard_arrow_up_rounded,
                    size: 18.r,
                    color: Colors.white.withValues(alpha: 0.60)),
              ),
              SizedBox(width: 8.w),
              // Create Order button
              _CtaButton(state: state),
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
                  width: 28.r,
                  height: 28.r,
                  decoration: BoxDecoration(
                    color: AppColors.primary.withValues(alpha: 0.20),
                    borderRadius: BorderRadius.circular(7.r),
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
                    color: Colors.white,
                  ),
                ),
                SizedBox(width: 6.w),
                Container(
                  padding:
                      EdgeInsets.symmetric(horizontal: 7.w, vertical: 2.h),
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
                Text(
                  'Rs. ${state.total.toStringAsFixed(2)}',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 18.sp,
                    fontWeight: FontWeight.w900,
                    letterSpacing: -0.3,
                    color: AppColors.amber,
                  ),
                ),
                SizedBox(width: 8.w),
                Container(
                  width: 28.r,
                  height: 28.r,
                  decoration: BoxDecoration(
                    color: Colors.white.withValues(alpha: 0.08),
                    borderRadius: BorderRadius.circular(7.r),
                  ),
                  child: Icon(Icons.keyboard_arrow_down_rounded,
                      size: 16.r,
                      color: Colors.white.withValues(alpha: 0.60)),
                ),
              ],
            ),
          ),

          SizedBox(height: 10.h),

          // Line items
          ConstrainedBox(
            constraints: BoxConstraints(maxHeight: 200.h),
            child: ListView.separated(
              shrinkWrap: true,
              padding: EdgeInsets.zero,
              itemCount: state.cart.length,
              separatorBuilder: (_, __) => Divider(
                height: 1,
                color: Colors.white.withValues(alpha: 0.08),
              ),
              itemBuilder: (ctx, i) {
                final line = state.cart[i];
                return CartRow(
                  line: line,
                  onChanged: (q) => ctx
                      .read<CreateBillBloc>()
                      .add(CartItemQtyChanged(line.lineNumber, q)),
                  onRemoved: () => ctx
                      .read<CreateBillBloc>()
                      .add(CartItemRemoved(line.lineNumber)),
                );
              },
            ),
          ),

          SizedBox(height: 10.h),
          Divider(color: Colors.white.withValues(alpha: 0.10), height: 1),
          SizedBox(height: 10.h),

          // Discount + totals
          Row(
            children: [
              SizedBox(
                width: 120.w,
                child: TextField(
                  keyboardType:
                      const TextInputType.numberWithOptions(decimal: true),
                  inputFormatters: [
                    FilteringTextInputFormatter.allow(RegExp(r'[0-9.]')),
                  ],
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 14.sp,
                    color: Colors.white,
                    fontWeight: FontWeight.w600,
                  ),
                  decoration: InputDecoration(
                    labelText: 'Discount %',
                    labelStyle: GoogleFonts.barlowCondensed(
                      fontSize: 11.sp,
                      letterSpacing: 0.8,
                      color: Colors.white.withValues(alpha: 0.50),
                    ),
                    filled: true,
                    fillColor: Colors.white.withValues(alpha: 0.07),
                    contentPadding: EdgeInsets.symmetric(
                        horizontal: 12.w, vertical: 10.h),
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(8.r),
                      borderSide: BorderSide(
                          color: Colors.white.withValues(alpha: 0.15)),
                    ),
                    enabledBorder: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(8.r),
                      borderSide: BorderSide(
                          color: Colors.white.withValues(alpha: 0.15)),
                    ),
                    focusedBorder: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(8.r),
                      borderSide:
                          BorderSide(color: AppColors.amber, width: 1.5),
                    ),
                  ),
                  onChanged: (v) {
                    final rate = double.tryParse(v) ?? 0;
                    context
                        .read<CreateBillBloc>()
                        .add(BillDiscountChanged(rate));
                  },
                ),
              ),
              SizedBox(width: 12.w),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Text(
                      'Subtotal  Rs. ${state.subTotal.toStringAsFixed(2)}',
                      style: GoogleFonts.barlow(
                        fontSize: 11.sp,
                        color: Colors.white.withValues(alpha: 0.45),
                      ),
                    ),
                    if (state.billDiscountAmount > 0) ...[
                      SizedBox(height: 1.h),
                      Text(
                        '− Rs. ${state.billDiscountAmount.toStringAsFixed(2)}',
                        style: GoogleFonts.barlow(
                          fontSize: 11.sp,
                          color: AppColors.amber.withValues(alpha: 0.70),
                        ),
                      ),
                    ],
                    SizedBox(height: 2.h),
                    Text(
                      'Total  Rs. ${state.total.toStringAsFixed(2)}',
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 15.sp,
                        fontWeight: FontWeight.w800,
                        color: Colors.white,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),

          SizedBox(height: 14.h),

          _CtaButton(state: state),
        ],
      ),
    );
  }
}

// ── Shared Create Order button ────────────────────────────────────────────────

class _CtaButton extends StatelessWidget {
  const _CtaButton({required this.state});
  final CreateBillState state;

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
          height: 52.h,
          decoration: BoxDecoration(
            color: state.canSubmit
                ? AppColors.primary
                : AppColors.primary.withValues(alpha: 0.35),
            borderRadius: BorderRadius.circular(12.r),
            boxShadow: state.canSubmit
                ? [
                    BoxShadow(
                      color: AppColors.primary.withValues(alpha: 0.35),
                      blurRadius: 14,
                      offset: const Offset(0, 5),
                    ),
                  ]
                : null,
          ),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              if (state.submitting)
                SizedBox(
                  width: 16.r,
                  height: 16.r,
                  child: CircularProgressIndicator(
                      strokeWidth: 1.8, color: Colors.white),
                )
              else
                Icon(Icons.check_circle_outline_rounded,
                    size: 18.r, color: Colors.white),
              SizedBox(width: 10.w),
              Text(
                state.submitting ? 'SAVING…' : 'CREATE ORDER',
                style: GoogleFonts.barlowCondensed(
                  fontSize: 17.sp,
                  fontWeight: FontWeight.w800,
                  letterSpacing: 2.0,
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
