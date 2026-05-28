import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/core/widgets/app_spinner.dart';
import 'package:uswatte/features/supervisor_billing/domain/entities/billing_detail.dart';
import 'package:uswatte/features/supervisor_billing/domain/entities/billing_item.dart';
import 'package:uswatte/features/supervisor_billing/domain/entities/billing_summary.dart';
import 'package:uswatte/features/supervisor_billing/presentation/cubit/billing_detail_cubit.dart';
import 'package:uswatte/features/supervisor_billing/presentation/cubit/billing_detail_state.dart';

class BillingDetailPage extends StatefulWidget {
  final int billingId;
  final String? billingNumber;

  const BillingDetailPage({
    super.key,
    required this.billingId,
    this.billingNumber,
  });

  @override
  State<BillingDetailPage> createState() => _BillingDetailPageState();
}

class _BillingDetailPageState extends State<BillingDetailPage> {
  @override
  void initState() {
    super.initState();
    context.read<BillingDetailCubit>().load(widget.billingId);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF5F4EE),
      body: Column(
        children: [
          _OrangeAppBar(billingNumber: widget.billingNumber),
          Expanded(
            child: BlocBuilder<BillingDetailCubit, BillingDetailState>(
              builder: (context, state) {
                if (state is BillingDetailLoading ||
                    state is BillingDetailInitial) {
                  return const _LoadingBody();
                }
                if (state is BillingDetailError) {
                  return _ErrorBody(
                    message: state.message,
                    onRetry: () =>
                        context.read<BillingDetailCubit>().load(widget.billingId),
                  );
                }
                if (state is BillingDetailLoaded) {
                  return _DetailBody(detail: state.detail);
                }
                return const SizedBox.shrink();
              },
            ),
          ),
        ],
      ),
    );
  }
}

// ── Orange app bar ────────────────────────────────────────────────────────────

class _OrangeAppBar extends StatelessWidget {
  final String? billingNumber;
  const _OrangeAppBar({this.billingNumber});

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    return Container(
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [AppColors.primaryDark, AppColors.primary],
        ),
      ),
      child: SafeArea(
        bottom: false,
        child: Stack(
          children: [
            Positioned(
              right: -18.w,
              top: -18.r,
              child: Container(
                width: 90.r,
                height: 90.r,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: Colors.white.withValues(alpha: 0.07),
                ),
              ),
            ),
            Padding(
              padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 10.r),
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
                  Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        billingNumber ?? 'BILL DETAIL',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 18.sp,
                          fontWeight: FontWeight.w800,
                          letterSpacing: 1.5,
                          height: 1.0,
                          color: Colors.white,
                        ),
                      ),
                      SizedBox(height: 2.r),
                      Text(
                        'Billing details',
                        style: GoogleFonts.barlow(
                          fontSize: 11.sp,
                          color: Colors.white.withValues(alpha: 0.70),
                        ),
                      ),
                    ],
                  ),
                  const Spacer(),
                  Container(
                    width: 38.r,
                    height: 38.r,
                    margin: EdgeInsets.only(right: 16.w),
                    decoration: BoxDecoration(
                      color: Colors.white.withValues(alpha: 0.15),
                      borderRadius: BorderRadius.circular(10.r),
                    ),
                    child: Icon(Icons.receipt_long_rounded,
                        size: 18.r, color: Colors.white),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Loading body ──────────────────────────────────────────────────────────────

class _LoadingBody extends StatelessWidget {
  const _LoadingBody();

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const AppSpinner(),
        ],
      ),
    );
  }
}

// ── Error body ────────────────────────────────────────────────────────────────

class _ErrorBody extends StatelessWidget {
  final String message;
  final VoidCallback onRetry;
  const _ErrorBody({required this.message, required this.onRetry});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: EdgeInsets.symmetric(horizontal: 32.w),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 60.r,
              height: 60.r,
              decoration: BoxDecoration(
                color: AppColors.error.withValues(alpha: 0.08),
                shape: BoxShape.circle,
              ),
              child: Icon(Icons.error_outline_rounded,
                  size: 28.r, color: AppColors.error),
            ),
            SizedBox(height: 14.h),
            Text(message,
                textAlign: TextAlign.center,
                style: GoogleFonts.barlow(
                    fontSize: 14.sp,
                    color: AppColors.foregroundMuted,
                    height: 1.5)),
            SizedBox(height: 20.h),
            GestureDetector(
              onTap: onRetry,
              child: Container(
                padding:
                    EdgeInsets.symmetric(horizontal: 24.w, vertical: 12.h),
                decoration: BoxDecoration(
                  color: AppColors.primary,
                  borderRadius: BorderRadius.circular(10.r),
                ),
                child: Text('Try Again',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 15.sp,
                      fontWeight: FontWeight.w700,
                      color: Colors.white,
                      letterSpacing: 0.5,
                    )),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Detail body ───────────────────────────────────────────────────────────────

class _DetailBody extends StatelessWidget {
  final BillingDetail detail;
  const _DetailBody({required this.detail});

  String _formatDate(String dateStr) {
    final parts = dateStr.split('-');
    if (parts.length != 3) return dateStr;
    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
    ];
    final month = int.tryParse(parts[1]) ?? 1;
    return '${months[month - 1]} ${parts[2]}, ${parts[0]}';
  }

  String _formatAmount(double amount) {
    return 'LKR ${amount.toStringAsFixed(2).replaceAllMapped(RegExp(r'(\d)(?=(\d{3})+\.)'), (m) => '${m[1]},')}';
  }

  Color _statusColor(BillingStatus status) {
    switch (status) {
      case BillingStatus.approved:
        return AppColors.success;
      case BillingStatus.cancelled:
        return AppColors.error;
      case BillingStatus.submitted:
        return AppColors.warning;
    }
  }

  String _statusLabel(BillingStatus status) {
    switch (status) {
      case BillingStatus.approved:
        return 'Approved';
      case BillingStatus.cancelled:
        return 'Cancelled';
      case BillingStatus.submitted:
        return 'Submitted';
    }
  }

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: EdgeInsets.fromLTRB(16.w, 20.h, 16.w, 40.h),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // ── Info card ────────────────────────────────────────────────────
          _InfoCard(
            detail: detail,
            statusColor: _statusColor(detail.status),
            statusLabel: _statusLabel(detail.status),
            formatDate: _formatDate,
            formatAmount: _formatAmount,
          ),
          SizedBox(height: 16.h),

          // ── Items card ───────────────────────────────────────────────────
          _ItemsCard(
            detail: detail,
            formatAmount: _formatAmount,
          ),

          // ── Notes ────────────────────────────────────────────────────────
          if (detail.notes != null && detail.notes!.isNotEmpty) ...[
            SizedBox(height: 16.h),
            _NotesCard(notes: detail.notes!),
          ],
        ],
      ),
    );
  }
}

// ── Info card ─────────────────────────────────────────────────────────────────

class _InfoCard extends StatelessWidget {
  final BillingDetail detail;
  final Color statusColor;
  final String statusLabel;
  final String Function(String) formatDate;
  final String Function(double) formatAmount;

  const _InfoCard({
    required this.detail,
    required this.statusColor,
    required this.statusLabel,
    required this.formatDate,
    required this.formatAmount,
  });

  @override
  Widget build(BuildContext context) {
    final double grossAmount = detail.items
        .where((i) => !i.isFreeIssue && i.billingItemType == BillingItemType.sale)
        .fold(0.0, (sum, i) => sum + i.quantity * i.unitPrice);
    final double itemDiscountTotal = (grossAmount - detail.subTotalAmount)
        .clamp(0.0, double.infinity);
    final bool hasItemDiscount = itemDiscountTotal > 0.001;
    final bool hasBillDiscount = detail.billDiscountRate > 0;

    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16.r),
        boxShadow: [
          BoxShadow(
            color: const Color(0xFF1A1A11).withValues(alpha: 0.06),
            blurRadius: 16,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        children: [
          // Card header with status badge
          Container(
            padding: EdgeInsets.fromLTRB(16.w, 14.h, 16.w, 12.h),
            decoration: const BoxDecoration(
              border: Border(
                bottom: BorderSide(color: Color(0xFFEEEDE6)),
              ),
            ),
            child: Row(
              children: [
                Container(
                  width: 36.r,
                  height: 36.r,
                  decoration: BoxDecoration(
                    color: AppColors.primary.withValues(alpha: 0.08),
                    borderRadius: BorderRadius.circular(10.r),
                  ),
                  child: Icon(Icons.info_outline_rounded,
                      size: 18.r, color: AppColors.primary),
                ),
                SizedBox(width: 10.w),
                Text(
                  'BILLING INFO',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 12.sp,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 2.0,
                    color: AppColors.foreground,
                  ),
                ),
                const Spacer(),
                Container(
                  padding:
                      EdgeInsets.symmetric(horizontal: 10.w, vertical: 4.h),
                  decoration: BoxDecoration(
                    color: statusColor.withValues(alpha: 0.10),
                    borderRadius: BorderRadius.circular(20.r),
                  ),
                  child: Text(
                    statusLabel,
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 11.sp,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 0.5,
                      color: statusColor,
                    ),
                  ),
                ),
              ],
            ),
          ),
          // Info rows
          Padding(
            padding: EdgeInsets.all(16.r),
            child: Column(
              children: [
                _InfoRow(label: 'Bill No', value: detail.billingNumber, mono: true),
                _InfoRow(label: 'Date', value: formatDate(detail.billingDate)),
                _InfoRow(label: 'Outlet', value: detail.outletName),
                _InfoRow(label: 'Sales Rep', value: detail.salesRepName),
                _InfoRow(label: 'Distributor', value: detail.distributorName),
                if (detail.supervisorName != null)
                  _InfoRow(label: 'Supervisor', value: detail.supervisorName!),
                SizedBox(height: 4.h),
                Divider(height: 1, color: const Color(0xFFEEEDE6)),
                SizedBox(height: 12.h),
                if (hasItemDiscount) ...[
                  _InfoRow(
                    label: 'Gross',
                    value: formatAmount(grossAmount),
                  ),
                  _InfoRow(
                    label: 'Item Discount',
                    value: '−${formatAmount(itemDiscountTotal)}',
                    valueColor: AppColors.error,
                  ),
                ],
                _InfoRow(
                  label: 'Sub-total',
                  value: formatAmount(detail.subTotalAmount),
                ),
                if (hasBillDiscount)
                  _InfoRow(
                    label: 'Bill Discount (${detail.billDiscountRate.toStringAsFixed(0)}%)',
                    value: '−${formatAmount(detail.billDiscountAmount)}',
                    valueColor: AppColors.error,
                  ),
                Divider(height: 1, color: const Color(0xFFEEEDE6)),
                SizedBox(height: 4.h),
                _InfoRow(
                  label: 'Total',
                  value: formatAmount(detail.totalAmount),
                  bold: true,
                  valueColor: AppColors.foreground,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  final String label;
  final String value;
  final bool mono;
  final bool bold;
  final Color? valueColor;

  const _InfoRow({
    required this.label,
    required this.value,
    this.mono = false,
    this.bold = false,
    this.valueColor,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.symmetric(vertical: 5.h),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 100.w,
            child: Text(
              label,
              style: GoogleFonts.barlow(
                fontSize: 12.sp,
                color: AppColors.foregroundMuted,
              ),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: mono
                  ? GoogleFonts.robotoMono(
                      fontSize: 12.sp,
                      fontWeight: FontWeight.w600,
                      color: AppColors.foreground,
                    )
                  : GoogleFonts.barlow(
                      fontSize: 12.sp,
                      fontWeight:
                          bold ? FontWeight.w700 : FontWeight.w500,
                      color: valueColor ?? AppColors.foreground,
                    ),
              textAlign: TextAlign.right,
            ),
          ),
        ],
      ),
    );
  }
}

// ── Items card ────────────────────────────────────────────────────────────────

class _ItemsCard extends StatelessWidget {
  final BillingDetail detail;
  final String Function(double) formatAmount;

  const _ItemsCard({required this.detail, required this.formatAmount});

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16.r),
        boxShadow: [
          BoxShadow(
            color: const Color(0xFF1A1A11).withValues(alpha: 0.06),
            blurRadius: 16,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        children: [
          // Header
          Container(
            padding: EdgeInsets.fromLTRB(16.w, 14.h, 16.w, 12.h),
            decoration: const BoxDecoration(
              border: Border(
                bottom: BorderSide(color: Color(0xFFEEEDE6)),
              ),
            ),
            child: Row(
              children: [
                Container(
                  width: 36.r,
                  height: 36.r,
                  decoration: BoxDecoration(
                    color: AppColors.primary.withValues(alpha: 0.08),
                    borderRadius: BorderRadius.circular(10.r),
                  ),
                  child: Icon(Icons.inventory_2_rounded,
                      size: 18.r, color: AppColors.primary),
                ),
                SizedBox(width: 10.w),
                Text(
                  'ITEMS',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 12.sp,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 2.0,
                    color: AppColors.foreground,
                  ),
                ),
                const Spacer(),
                Text(
                  '${detail.items.length} product${detail.items.length == 1 ? '' : 's'}',
                  style: GoogleFonts.barlow(
                    fontSize: 11.sp,
                    color: AppColors.foregroundMuted,
                  ),
                ),
              ],
            ),
          ),
          // Items list
          ...detail.items.asMap().entries.map((entry) {
            final i = entry.key;
            final item = entry.value;
            return Column(
              children: [
                _ItemRow(item: item, formatAmount: formatAmount),
                if (i < detail.items.length - 1)
                  Divider(
                      height: 1,
                      indent: 16.w,
                      endIndent: 16.w,
                      color: const Color(0xFFF0EFEA)),
              ],
            );
          }),
        ],
      ),
    );
  }
}

class _ItemRow extends StatelessWidget {
  final BillingItem item;
  final String Function(double) formatAmount;

  const _ItemRow({required this.item, required this.formatAmount});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 12.h),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Line number badge
              Container(
                width: 22.r,
                height: 22.r,
                decoration: BoxDecoration(
                  color: const Color(0xFFEEEDE6),
                  borderRadius: BorderRadius.circular(6.r),
                ),
                child: Center(
                  child: Text(
                    '${item.lineNumber}',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 10.sp,
                      fontWeight: FontWeight.w700,
                      color: AppColors.foregroundMuted,
                    ),
                  ),
                ),
              ),
              SizedBox(width: 10.w),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      item.productDescription,
                      style: GoogleFonts.barlow(
                        fontSize: 13.sp,
                        fontWeight: FontWeight.w600,
                        color: AppColors.foreground,
                      ),
                    ),
                    SizedBox(height: 2.h),
                    Row(
                      children: [
                        Text(
                          item.productCode,
                          style: GoogleFonts.robotoMono(
                            fontSize: 10.sp,
                            color: AppColors.foregroundMuted,
                          ),
                        ),
                        if (item.isFreeIssue) ...[
                          SizedBox(width: 6.w),
                          _Badge(
                              label: 'FREE ISSUE',
                              color: AppColors.amber),
                        ],
                        if (item.billingItemType ==
                            BillingItemType.returnItem) ...[
                          SizedBox(width: 6.w),
                          _Badge(
                            label: item.returnType != null
                                ? 'RETURN · ${_returnLabel(item.returnType!)}'
                                : 'RETURN',
                            color: Colors.blue.shade600,
                          ),
                        ],
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
          SizedBox(height: 8.h),
          // Price row
          Row(
            children: [
              SizedBox(width: 32.r),
              _PriceChip(
                label: 'QTY',
                value: item.quantity.toStringAsFixed(
                    item.quantity == item.quantity.roundToDouble() ? 0 : 2),
              ),
              SizedBox(width: 8.w),
              if (!item.isFreeIssue) ...[
                _PriceChip(
                    label: 'PRICE',
                    value: formatAmount(item.unitPrice)),
                if (item.discountRate > 0) ...[
                  SizedBox(width: 8.w),
                  _PriceChip(
                      label: 'DISC',
                      value:
                          '${item.discountRate.toStringAsFixed(0)}%'),
                ],
              ],
              const Spacer(),
              Text(
                item.isFreeIssue ? 'FREE' : formatAmount(item.totalPrice),
                style: GoogleFonts.barlowCondensed(
                  fontSize: 14.sp,
                  fontWeight: FontWeight.w800,
                  color: item.isFreeIssue
                      ? AppColors.amber
                      : AppColors.foreground,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  String _returnLabel(ReturnType t) {
    switch (t) {
      case ReturnType.marketResell:
        return 'Resell';
      case ReturnType.damage:
        return 'Damage';
      case ReturnType.expire:
        return 'Expire';
    }
  }
}

class _Badge extends StatelessWidget {
  final String label;
  final Color color;

  const _Badge({required this.label, required this.color});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 5.w, vertical: 2.h),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(4.r),
      ),
      child: Text(
        label,
        style: GoogleFonts.barlowCondensed(
          fontSize: 9.sp,
          fontWeight: FontWeight.w700,
          letterSpacing: 0.5,
          color: color,
        ),
      ),
    );
  }
}

class _PriceChip extends StatelessWidget {
  final String label;
  final String value;

  const _PriceChip({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 7.w, vertical: 3.h),
      decoration: BoxDecoration(
        color: const Color(0xFFF0EFEA),
        borderRadius: BorderRadius.circular(6.r),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            '$label ',
            style: GoogleFonts.barlowCondensed(
              fontSize: 9.sp,
              fontWeight: FontWeight.w600,
              color: AppColors.foregroundMuted,
              letterSpacing: 0.5,
            ),
          ),
          Text(
            value,
            style: GoogleFonts.barlowCondensed(
              fontSize: 11.sp,
              fontWeight: FontWeight.w700,
              color: AppColors.foreground,
            ),
          ),
        ],
      ),
    );
  }
}

// ── Notes card ────────────────────────────────────────────────────────────────

class _NotesCard extends StatelessWidget {
  final String notes;
  const _NotesCard({required this.notes});

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: EdgeInsets.all(16.r),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16.r),
        boxShadow: [
          BoxShadow(
            color: const Color(0xFF1A1A11).withValues(alpha: 0.06),
            blurRadius: 16,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'NOTES',
            style: GoogleFonts.barlowCondensed(
              fontSize: 11.sp,
              fontWeight: FontWeight.w700,
              letterSpacing: 2.0,
              color: AppColors.foregroundMuted,
            ),
          ),
          SizedBox(height: 8.h),
          Text(
            notes,
            style: GoogleFonts.barlow(
              fontSize: 13.sp,
              color: AppColors.foreground,
              height: 1.5,
            ),
          ),
        ],
      ),
    );
  }
}
