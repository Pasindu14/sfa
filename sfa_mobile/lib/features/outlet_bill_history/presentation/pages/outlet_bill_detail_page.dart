import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/core/widgets/app_spinner.dart';
import 'package:uswatte/features/outlet_bill_history/domain/entities/outlet_bill_detail.dart';
import 'package:uswatte/features/outlet_bill_history/domain/entities/outlet_bill_item.dart';
import 'package:uswatte/features/outlet_bill_history/presentation/cubit/outlet_bill_detail_cubit.dart';
import 'package:uswatte/features/outlet_bill_history/presentation/cubit/outlet_bill_detail_state.dart';

class OutletBillDetailPage extends StatefulWidget {
  final int billingId;

  const OutletBillDetailPage({super.key, required this.billingId});

  @override
  State<OutletBillDetailPage> createState() => _OutletBillDetailPageState();
}

class _OutletBillDetailPageState extends State<OutletBillDetailPage> {
  @override
  void initState() {
    super.initState();
    context.read<OutletBillDetailCubit>().load(widget.billingId);
  }

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    return Scaffold(
      backgroundColor: AppColors.background,
      body: Column(
        children: [
          // ── Gradient header ────────────────────────────────────────────────
          Container(
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
                colors: [AppColors.primaryDark, AppColors.primary],
              ),
            ),
            child: SafeArea(
              bottom: false,
              child: Padding(
                padding: EdgeInsets.fromLTRB(8.w, 4.h, 8.w, 16.h),
                child: Row(
                  children: [
                    GestureDetector(
                      onTap: () => context.pop(),
                      child: Container(
                        width: 40.r,
                        height: 40.r,
                        margin: EdgeInsets.all(4.r),
                        decoration: BoxDecoration(
                          color: Colors.white.withValues(alpha: 0.15),
                          borderRadius: BorderRadius.circular(10.r),
                          border: Border.all(
                              color: Colors.white.withValues(alpha: 0.25)),
                        ),
                        child: Icon(Icons.arrow_back_ios_new_rounded,
                            size: 15.r, color: Colors.white),
                      ),
                    ),
                    SizedBox(width: 4.w),
                    Expanded(
                      child: Text(
                        'BILL DETAILS',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 18.sp,
                          fontWeight: FontWeight.w800,
                          letterSpacing: 1.5,
                          height: 1.0,
                          color: Colors.white,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),

          // ── Body ──────────────────────────────────────────────────────────
          Expanded(
            child: BlocBuilder<OutletBillDetailCubit, OutletBillDetailState>(
              builder: (ctx, state) {
                return switch (state) {
                  OutletBillDetailInitial() ||
                  OutletBillDetailLoading() =>
                    const Center(child: AppSpinner()),
                  OutletBillDetailError(:final message) => _ErrorView(
                      message: message,
                      onRetry: () => ctx
                          .read<OutletBillDetailCubit>()
                          .load(widget.billingId),
                    ),
                  OutletBillDetailLoaded(:final bill) => ListView(
                      padding:
                          EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 40.h),
                      children: [
                        _InfoCard(bill: bill),
                        SizedBox(height: 16.h),
                        _SectionLabel('LINE ITEMS'),
                        SizedBox(height: 8.h),
                        _ItemsCard(items: bill.items),
                        SizedBox(height: 12.h),
                        _TotalsCard(bill: bill),
                      ],
                    ),
                };
              },
            ),
          ),
        ],
      ),
    );
  }
}

// ── Info card ─────────────────────────────────────────────────────────────────

class _InfoCard extends StatelessWidget {
  final OutletBillDetail bill;
  const _InfoCard({required this.bill});

  @override
  Widget build(BuildContext context) {
    final repColor = switch (bill.repStatus.toLowerCase()) {
      'approved' => AppColors.success,
      'submitted' => AppColors.primary,
      'rejected' || 'cancelled' => AppColors.error,
      _ => AppColors.foregroundMuted,
    };
    final distColor = switch (bill.distributorStatus.toLowerCase()) {
      'approved' => AppColors.success,
      'rejected' => AppColors.error,
      'cancelled' => AppColors.error,
      _ => AppColors.foregroundMuted,
    };
    final isRejected = bill.distributorStatus.toLowerCase() == 'rejected';

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // ── Rejection banner ──────────────────────────────────────────────────
        if (isRejected)
          Container(
            margin: EdgeInsets.only(bottom: 10.h),
            padding: EdgeInsets.all(12.r),
            decoration: BoxDecoration(
              color: AppColors.error.withValues(alpha: 0.07),
              borderRadius: BorderRadius.circular(10.r),
              border: Border.all(color: AppColors.error.withValues(alpha: 0.30)),
            ),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Icon(Icons.cancel_rounded,
                    size: 16.r, color: AppColors.error),
                SizedBox(width: 8.w),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Rejected by Distributor',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 13.sp,
                          fontWeight: FontWeight.w700,
                          color: AppColors.error,
                        ),
                      ),
                      if (bill.rejectionReason != null &&
                          bill.rejectionReason!.isNotEmpty) ...[
                        SizedBox(height: 3.h),
                        Text(
                          bill.rejectionReason!,
                          style: GoogleFonts.barlow(
                            fontSize: 12.sp,
                            color: AppColors.error.withValues(alpha: 0.85),
                          ),
                        ),
                      ] else ...[
                        SizedBox(height: 3.h),
                        Text(
                          'No reason provided.',
                          style: GoogleFonts.barlow(
                            fontSize: 12.sp,
                            color: AppColors.foregroundMuted,
                            fontStyle: FontStyle.italic,
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
              ],
            ),
          ),

        // ── Main info card ────────────────────────────────────────────────────
        Container(
          padding: EdgeInsets.all(16.r),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(12.r),
            border: Border.all(color: AppColors.surfaceVariant),
            boxShadow: [
              BoxShadow(
                color: AppColors.primary.withValues(alpha: 0.06),
                blurRadius: 12,
                offset: const Offset(0, 4),
              ),
            ],
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: Text(
                      bill.billingNumber,
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 22.sp,
                        fontWeight: FontWeight.w800,
                        letterSpacing: -0.3,
                        color: AppColors.foreground,
                      ),
                    ),
                  ),
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      _statusChip(bill.repStatus, repColor),
                      SizedBox(height: 4.h),
                      _statusChip('Dist: ${bill.distributorStatus}', distColor),
                    ],
                  ),
                ],
              ),
              SizedBox(height: 10.h),
              Divider(height: 1, color: AppColors.surfaceVariant),
              SizedBox(height: 10.h),
              _infoRow(Icons.storefront_rounded, 'Outlet', bill.outletName),
              SizedBox(height: 6.h),
              _infoRow(Icons.person_rounded, 'Sales Rep', bill.salesRepName),
              SizedBox(height: 6.h),
              _infoRow(Icons.local_shipping_rounded, 'Distributor',
                  bill.distributorName),
              SizedBox(height: 6.h),
              _infoRow(Icons.calendar_today_rounded, 'Billing Date',
                  _formatDate(bill.billingDate)),
              SizedBox(height: 6.h),
              _infoRow(Icons.access_time_rounded, 'Created',
                  _formatDate(bill.createdAt)),
              if (bill.notes != null && bill.notes!.isNotEmpty) ...[
                SizedBox(height: 6.h),
                _infoRow(Icons.notes_rounded, 'Notes', bill.notes!),
              ],
            ],
          ),
        ),
      ],
    );
  }

  Widget _statusChip(String label, Color color) {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 4.h),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.10),
        borderRadius: BorderRadius.circular(999),
        border: Border.all(color: color.withValues(alpha: 0.35)),
      ),
      child: Text(
        label,
        style: GoogleFonts.barlow(
          fontSize: 10.sp,
          fontWeight: FontWeight.w600,
          color: color,
        ),
      ),
    );
  }

  Widget _infoRow(IconData icon, String label, String value) {
    return Row(
      children: [
        Icon(icon, size: 13.r, color: AppColors.foregroundMuted),
        SizedBox(width: 6.w),
        Text(
          '$label: ',
          style: GoogleFonts.barlow(
              fontSize: 12.sp, color: AppColors.foregroundMuted),
        ),
        Expanded(
          child: Text(
            value,
            style: GoogleFonts.barlowCondensed(
              fontSize: 13.sp,
              fontWeight: FontWeight.w600,
              color: AppColors.foreground,
            ),
            overflow: TextOverflow.ellipsis,
          ),
        ),
      ],
    );
  }

  String _formatDate(DateTime d) {
    final m = d.month.toString().padLeft(2, '0');
    final day = d.day.toString().padLeft(2, '0');
    return '${d.year}-$m-$day';
  }
}

// ── Section label ─────────────────────────────────────────────────────────────

class _SectionLabel extends StatelessWidget {
  final String text;
  const _SectionLabel(this.text);

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Container(
          width: 3.w,
          height: 12.h,
          decoration: BoxDecoration(
            color: AppColors.primary,
            borderRadius: BorderRadius.circular(2.r),
          ),
        ),
        SizedBox(width: 8.w),
        Text(
          text,
          style: GoogleFonts.barlowCondensed(
            fontSize: 11.sp,
            fontWeight: FontWeight.w700,
            letterSpacing: 2.5,
            color: AppColors.foregroundMuted,
          ),
        ),
      ],
    );
  }
}

// ── Items card ────────────────────────────────────────────────────────────────

class _ItemsCard extends StatelessWidget {
  final List<OutletBillItem> items;
  const _ItemsCard({required this.items});

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12.r),
        border: Border.all(color: AppColors.surfaceVariant),
        boxShadow: [
          BoxShadow(
            color: AppColors.primary.withValues(alpha: 0.04),
            blurRadius: 10,
            offset: const Offset(0, 3),
          ),
        ],
      ),
      child: Column(
        children: [
          for (var i = 0; i < items.length; i++) ...[
            if (i > 0) Divider(height: 1, color: AppColors.surfaceVariant),
            _ItemRow(item: items[i]),
          ],
        ],
      ),
    );
  }
}

class _ItemRow extends StatelessWidget {
  final OutletBillItem item;
  const _ItemRow({required this.item});

  @override
  Widget build(BuildContext context) {
    final qtyStr = item.quantity.toStringAsFixed(
        item.quantity.truncateToDouble() == item.quantity ? 0 : 1);

    return Padding(
      padding: EdgeInsets.fromLTRB(14.w, 12.h, 14.w, 12.h),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Text(
                  item.productDescription,
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 14.sp,
                    fontWeight: FontWeight.w700,
                    color: AppColors.foreground,
                  ),
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              SizedBox(width: 8.w),
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  if (item.discountRate > 0)
                    Text(
                      'Rs. ${(item.quantity * item.unitPrice).toStringAsFixed(2)}',
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 11.sp,
                        color: AppColors.foregroundMuted,
                        decoration: TextDecoration.lineThrough,
                        decorationColor: AppColors.foregroundMuted,
                      ),
                    ),
                  Text(
                    'Rs. ${item.totalPrice.toStringAsFixed(2)}',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 15.sp,
                      fontWeight: FontWeight.w800,
                      color: AppColors.amber,
                    ),
                  ),
                ],
              ),
            ],
          ),
          SizedBox(height: 5.h),
          Row(
            children: [
              _chip(Icons.tag_rounded, 'Qty: $qtyStr'),
              SizedBox(width: 8.w),
              _chip(Icons.sell_rounded,
                  'Rs. ${item.unitPrice.toStringAsFixed(2)} / pack'),
              if (item.billingItemType == 'Return') ...[
                SizedBox(width: 8.w),
                _badge(
                  item.returnType ?? 'Return',
                  AppColors.error,
                  Icons.undo_rounded,
                ),
              ] else if (item.discountRate > 0) ...[
                SizedBox(width: 8.w),
                _badge(
                  '${item.discountRate.toStringAsFixed(item.discountRate.truncateToDouble() == item.discountRate ? 0 : 1)}%  −Rs.${item.discountAmount.toStringAsFixed(0)}',
                  AppColors.error,
                  Icons.local_offer_rounded,
                ),
              ],
              if (item.isFreeIssue) ...[
                SizedBox(width: 8.w),
                _freeBadge(),
              ],
            ],
          ),
        ],
      ),
    );
  }

  Widget _chip(IconData icon, String text) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 10.r, color: AppColors.foregroundMuted),
        SizedBox(width: 3.w),
        Text(
          text,
          style:
              GoogleFonts.barlow(fontSize: 11.sp, color: AppColors.foregroundMuted),
        ),
      ],
    );
  }

  Widget _badge(String label, Color color, IconData icon) {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 6.w, vertical: 2.h),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(999),
        border: Border.all(color: color.withValues(alpha: 0.25)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 9.r, color: color),
          SizedBox(width: 3.w),
          Text(
            label,
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

  Widget _freeBadge() {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 6.w, vertical: 2.h),
      decoration: BoxDecoration(
        color: AppColors.success.withValues(alpha: 0.10),
        borderRadius: BorderRadius.circular(999),
        border: Border.all(color: AppColors.success.withValues(alpha: 0.30)),
      ),
      child: Text(
        'FREE',
        style: GoogleFonts.barlowCondensed(
          fontSize: 10.sp,
          fontWeight: FontWeight.w800,
          letterSpacing: 0.5,
          color: AppColors.success,
        ),
      ),
    );
  }
}

// ── Totals card ───────────────────────────────────────────────────────────────

class _TotalsCard extends StatelessWidget {
  final OutletBillDetail bill;
  const _TotalsCard({required this.bill});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.all(14.r),
      decoration: BoxDecoration(
        color: AppColors.darkSurface,
        borderRadius: BorderRadius.circular(12.r),
      ),
      child: Column(
        children: [
          _line('Subtotal', bill.subTotalAmount),
          if (bill.billDiscountAmount > 0) ...[
            SizedBox(height: 6.h),
            _discountLine(bill.billDiscountRate, bill.billDiscountAmount),
          ],
          SizedBox(height: 8.h),
          Divider(color: Colors.white.withValues(alpha: 0.10), height: 1),
          SizedBox(height: 10.h),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'TOTAL',
                style: GoogleFonts.barlowCondensed(
                  fontSize: 13.sp,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 1.5,
                  color: Colors.white.withValues(alpha: 0.60),
                ),
              ),
              Text(
                'Rs. ${bill.totalAmount.toStringAsFixed(2)}',
                style: GoogleFonts.barlowCondensed(
                  fontSize: 22.sp,
                  fontWeight: FontWeight.w900,
                  letterSpacing: -0.3,
                  color: AppColors.amber,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _line(String label, double amount) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          label,
          style: GoogleFonts.barlow(
              fontSize: 12.sp, color: Colors.white.withValues(alpha: 0.45)),
        ),
        Text(
          'Rs. ${amount.toStringAsFixed(2)}',
          style: GoogleFonts.barlowCondensed(
            fontSize: 14.sp,
            fontWeight: FontWeight.w600,
            color: Colors.white.withValues(alpha: 0.80),
          ),
        ),
      ],
    );
  }

  Widget _discountLine(double rate, double amount) {
    final rateStr =
        rate.toStringAsFixed(rate.truncateToDouble() == rate ? 0 : 1);
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          'Discount ($rateStr%)',
          style: GoogleFonts.barlow(
              fontSize: 12.sp, color: AppColors.error.withValues(alpha: 0.7)),
        ),
        Text(
          '− Rs. ${amount.toStringAsFixed(2)}',
          style: GoogleFonts.barlowCondensed(
            fontSize: 14.sp,
            fontWeight: FontWeight.w600,
            color: AppColors.error.withValues(alpha: 0.7),
          ),
        ),
      ],
    );
  }
}

// ── Error state ───────────────────────────────────────────────────────────────

class _ErrorView extends StatelessWidget {
  final String message;
  final VoidCallback onRetry;

  const _ErrorView({required this.message, required this.onRetry});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: EdgeInsets.symmetric(horizontal: 32.w),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.cloud_off_rounded,
                size: 48.r, color: AppColors.foregroundMuted),
            SizedBox(height: 16.h),
            Text(
              'Could not load bill',
              style: GoogleFonts.barlowCondensed(
                fontSize: 18.sp,
                fontWeight: FontWeight.w700,
                color: AppColors.foreground,
              ),
            ),
            SizedBox(height: 6.h),
            Text(
              message,
              textAlign: TextAlign.center,
              style: GoogleFonts.barlow(
                  fontSize: 12.sp, color: AppColors.foregroundMuted),
            ),
            SizedBox(height: 20.h),
            TextButton.icon(
              onPressed: onRetry,
              icon: Icon(Icons.refresh_rounded, size: 16.r),
              label: Text('Retry', style: GoogleFonts.barlow(fontSize: 14.sp)),
              style: TextButton.styleFrom(foregroundColor: AppColors.primary),
            ),
          ],
        ),
      ),
    );
  }
}
