import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/di/injection.dart';
import 'package:uswatte/core/sync/bill_sync_service.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/bills/domain/entities/bill.dart';
import 'package:uswatte/features/bills/domain/entities/bill_item.dart';
import 'package:uswatte/features/bills/domain/entities/sync_status.dart';
import 'package:uswatte/features/bills/domain/usecases/get_bill_by_id_usecase.dart';
import 'package:uswatte/features/bills/presentation/bloc/bills_list_bloc.dart';
import 'package:uswatte/features/bills/presentation/bloc/bills_list_event.dart';

class BillDetailPage extends StatefulWidget {
  final String clientBillId;
  const BillDetailPage({super.key, required this.clientBillId});

  @override
  State<BillDetailPage> createState() => _BillDetailPageState();
}

class _BillDetailPageState extends State<BillDetailPage> {
  late Future<Bill?> _future;
  StreamSubscription<BillOutboxStatus>? _syncSub;
  SyncStatus? _lastKnownStatus;
  ScaffoldFeatureController<SnackBar, SnackBarClosedReason>? _syncingBar;

  @override
  void initState() {
    super.initState();
    _future = getIt<GetBillByIdUseCase>().call(widget.clientBillId);
    _syncSub = getIt<BillSyncService>().status$.listen((_) => _reloadFromSync());
  }

  @override
  void dispose() {
    _syncSub?.cancel();
    super.dispose();
  }

  void _reload() {
    if (!mounted) return;
    setState(() {
      _future = getIt<GetBillByIdUseCase>().call(widget.clientBillId);
    });
  }

  Future<void> _reloadFromSync() async {
    if (!mounted) return;
    final bill = await getIt<GetBillByIdUseCase>().call(widget.clientBillId);
    if (!mounted) return;

    final messenger = ScaffoldMessenger.of(context);

    if (bill != null) {
      final prev = _lastKnownStatus;
      final curr = bill.syncStatus;

      // pending/failed → syncing: show sticky orange "Syncing…" bar.
      // prev == null means first stream event — still show if currently syncing.
      if (curr == SyncStatus.syncing && prev != SyncStatus.syncing) {
        messenger.hideCurrentSnackBar();
        _syncingBar = messenger.showSnackBar(
          SnackBar(
            behavior: SnackBarBehavior.floating,
            margin: EdgeInsets.fromLTRB(16.w, 0, 16.w, 24.h),
            backgroundColor: AppColors.amber,
            shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(10.r)),
            duration: const Duration(minutes: 5),
            content: Row(
              children: [
                SizedBox(
                  width: 16.r,
                  height: 16.r,
                  child: CircularProgressIndicator(
                    strokeWidth: 2,
                    color: Colors.white,
                  ),
                ),
                SizedBox(width: 10.w),
                Text(
                  'Syncing order to server…',
                  style: GoogleFonts.barlow(
                    fontSize: 13.sp,
                    fontWeight: FontWeight.w600,
                    color: Colors.white,
                  ),
                ),
              ],
            ),
          ),
        );
      }

      // syncing → synced: dismiss spinner bar, show green success bar.
      // Require prev != null so we don't toast if the bill was already synced on open.
      if (prev != null && prev != SyncStatus.synced && curr == SyncStatus.synced) {
        _syncingBar?.close();
        _syncingBar = null;
        final number = bill.serverBillNumber ?? 'Order';
        messenger.showSnackBar(
          SnackBar(
            behavior: SnackBarBehavior.floating,
            margin: EdgeInsets.fromLTRB(16.w, 0, 16.w, 24.h),
            backgroundColor: AppColors.success,
            shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(10.r)),
            duration: const Duration(seconds: 3),
            content: Row(
              children: [
                Icon(Icons.cloud_done_rounded,
                    color: Colors.white, size: 18.r),
                SizedBox(width: 10.w),
                Expanded(
                  child: Text(
                    '$number synced successfully!',
                    style: GoogleFonts.barlow(
                      fontSize: 13.sp,
                      fontWeight: FontWeight.w600,
                      color: Colors.white,
                    ),
                  ),
                ),
              ],
            ),
          ),
        );
      }

      // syncing → failed: dismiss the spinner bar (error panel updates in place).
      if (prev == SyncStatus.syncing && curr == SyncStatus.failed) {
        _syncingBar?.close();
        _syncingBar = null;
      }
    }

    if (bill != null) _lastKnownStatus = bill.syncStatus;
    setState(() {
      _future = Future.value(bill);
    });
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
                        'ORDER DETAILS',
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
            child: FutureBuilder<Bill?>(
              future: _future,
              builder: (ctx, snap) {
                if (!snap.hasData) {
                  return const Center(
                    child: CircularProgressIndicator(
                        color: AppColors.primary, strokeWidth: 2),
                  );
                }
                final bill = snap.data;
                if (bill == null) {
                  return Center(
                    child: Text('Order not found.',
                        style: GoogleFonts.barlow(color: AppColors.foregroundMuted)),
                  );
                }
                return ListView(
                  padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 40.h),
                  children: [
                    _InfoCard(bill: bill),
                    if (bill.syncStatus == SyncStatus.failed &&
                        bill.lastSyncError != null) ...[
                      SizedBox(height: 12.h),
                      _ErrorPanel(
                        code: bill.lastSyncErrorCode ?? 'ERROR',
                        message: bill.lastSyncError!,
                      ),
                    ],
                    SizedBox(height: 16.h),
                    _SectionLabel('LINE ITEMS'),
                    SizedBox(height: 8.h),
                    _ItemsCard(items: bill.items),
                    SizedBox(height: 12.h),
                    _TotalsCard(bill: bill),
                    if (bill.syncStatus == SyncStatus.failed ||
                        bill.syncStatus == SyncStatus.pending) ...[
                      SizedBox(height: 20.h),
                      _ActionRow(bill: bill, onReload: _reload),
                    ],
                  ],
                );
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
  final Bill bill;
  const _InfoCard({required this.bill});

  @override
  Widget build(BuildContext context) {
    final title = bill.syncStatus == SyncStatus.synced
        ? (bill.serverBillNumber ?? '—')
        : 'Draft #${bill.clientBillId.substring(0, 6).toUpperCase()}';

    final (statusColor, statusLabel, statusIcon) = switch (bill.syncStatus) {
      SyncStatus.synced => (AppColors.success, 'Synced', Icons.cloud_done),
      SyncStatus.syncing =>
        (AppColors.primary, 'Syncing', Icons.cloud_upload_rounded),
      SyncStatus.pending => (AppColors.warning, 'Pending', Icons.schedule),
      SyncStatus.failed => (AppColors.error, 'Failed', Icons.error_outline),
    };

    return Container(
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
                  title,
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 22.sp,
                    fontWeight: FontWeight.w800,
                    letterSpacing: -0.3,
                    color: AppColors.foreground,
                  ),
                ),
              ),
              Container(
                padding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 5.h),
                decoration: BoxDecoration(
                  color: statusColor.withValues(alpha: 0.10),
                  borderRadius: BorderRadius.circular(999),
                  border: Border.all(color: statusColor.withValues(alpha: 0.35)),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(statusIcon, size: 12.r, color: statusColor),
                    SizedBox(width: 4.w),
                    Text(
                      statusLabel,
                      style: GoogleFonts.barlow(
                        fontSize: 11.sp,
                        fontWeight: FontWeight.w600,
                        color: statusColor,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          SizedBox(height: 10.h),
          Divider(height: 1, color: AppColors.surfaceVariant),
          SizedBox(height: 10.h),
          _infoRow(Icons.storefront_rounded, 'Outlet', '#${bill.outletId}'),
          SizedBox(height: 6.h),
          _infoRow(Icons.receipt_rounded, 'Type', bill.billingType),
          SizedBox(height: 6.h),
          _infoRow(Icons.calendar_today_rounded, 'Date',
              _formatDate(bill.billingDate)),
          SizedBox(height: 6.h),
          _infoRow(Icons.access_time_rounded, 'Created',
              _formatDateTime(bill.createdAt)),
          if (bill.syncAttempts > 0) ...[
            SizedBox(height: 6.h),
            _infoRow(Icons.replay_rounded, 'Sync attempts',
                '${bill.syncAttempts}'),
          ],
        ],
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
}

// ── Error panel ───────────────────────────────────────────────────────────────

class _ErrorPanel extends StatelessWidget {
  final String code;
  final String message;
  const _ErrorPanel({required this.code, required this.message});

  String get _friendlyTitle => switch (code) {
        'INSUFFICIENT_STOCK'  => 'Items Out of Stock',
        'VALIDATION_FAILED'   => 'Invalid Order Data',
        'NETWORK_ERROR'       => 'Network Error',
        'FORBIDDEN_ACCESS'    => 'Not Authorised',
        'DUPLICATE_RESOURCE'  => 'Duplicate Order',
        _                     => 'Sync Failed',
      };

  @override
  Widget build(BuildContext context) {
    final lines = message.split('\n').where((l) => l.trim().isNotEmpty).toList();

    return Container(
      padding: EdgeInsets.all(12.r),
      decoration: BoxDecoration(
        color: AppColors.error.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(10.r),
        border: Border.all(color: AppColors.error.withValues(alpha: 0.4)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.warning_amber_rounded, color: AppColors.error, size: 16.r),
              SizedBox(width: 6.w),
              Text(
                _friendlyTitle,
                style: GoogleFonts.barlowCondensed(
                  fontSize: 14.sp,
                  fontWeight: FontWeight.w700,
                  color: AppColors.error,
                ),
              ),
            ],
          ),
          SizedBox(height: 8.h),
          ...lines.map((line) => Padding(
                padding: EdgeInsets.only(bottom: 4.h),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Padding(
                      padding: EdgeInsets.only(top: 3.h, right: 6.w),
                      child: Container(
                        width: 5.r,
                        height: 5.r,
                        decoration: BoxDecoration(
                          color: AppColors.error.withValues(alpha: 0.6),
                          shape: BoxShape.circle,
                        ),
                      ),
                    ),
                    Expanded(
                      child: Text(
                        line,
                        style: GoogleFonts.barlow(
                            fontSize: 12.sp,
                            color: AppColors.error.withValues(alpha: 0.85)),
                      ),
                    ),
                  ],
                ),
              )),
        ],
      ),
    );
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
  final List<BillItem> items;
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
            if (i > 0)
              Divider(height: 1, color: AppColors.surfaceVariant),
            _ItemRow(item: items[i]),
          ],
        ],
      ),
    );
  }
}

class _ItemRow extends StatelessWidget {
  final BillItem item;
  const _ItemRow({required this.item});

  double get _gross => item.quantity * item.unitPrice;
  double get _discountAmount => _gross * item.discountRate / 100;
  double get _lineTotal => _gross - _discountAmount;

  @override
  Widget build(BuildContext context) {
    final name = item.productName ?? 'Product #${item.productId}';
    final qtyStr = item.quantity.toStringAsFixed(
        item.quantity.truncateToDouble() == item.quantity ? 0 : 1);

    return Padding(
      padding: EdgeInsets.fromLTRB(14.w, 12.h, 14.w, 12.h),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Row 1: name + line total
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Text(
                  name,
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
                      'Rs. ${_gross.toStringAsFixed(2)}',
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 11.sp,
                        color: AppColors.foregroundMuted,
                        decoration: TextDecoration.lineThrough,
                        decorationColor: AppColors.foregroundMuted,
                      ),
                    ),
                  Text(
                    'Rs. ${_lineTotal.toStringAsFixed(2)}',
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
          // Row 2: qty · unit price · discount
          Row(
            children: [
              _chip(Icons.tag_rounded, 'Qty: $qtyStr'),
              SizedBox(width: 8.w),
              _chip(Icons.sell_rounded,
                  'Rs. ${item.unitPrice.toStringAsFixed(2)} / pack'),
              if (item.discountRate > 0) ...[
                SizedBox(width: 8.w),
                _discountChip(item.discountRate, _discountAmount),
              ],
              if (item.isFreeIssue) ...[
                SizedBox(width: 8.w),
                _freeIssueChip(),
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
          style: GoogleFonts.barlow(
              fontSize: 11.sp, color: AppColors.foregroundMuted),
        ),
      ],
    );
  }

  Widget _discountChip(double rate, double amount) {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 6.w, vertical: 2.h),
      decoration: BoxDecoration(
        color: AppColors.error.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(999),
        border: Border.all(color: AppColors.error.withValues(alpha: 0.25)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.local_offer_rounded, size: 9.r, color: AppColors.error),
          SizedBox(width: 3.w),
          Text(
            '${rate.toStringAsFixed(rate.truncateToDouble() == rate ? 0 : 1)}%  −Rs.${amount.toStringAsFixed(0)}',
            style: GoogleFonts.barlow(
              fontSize: 10.sp,
              fontWeight: FontWeight.w600,
              color: AppColors.error,
            ),
          ),
        ],
      ),
    );
  }

  Widget _freeIssueChip() {
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
  final Bill bill;
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
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          'Discount (${rate.toStringAsFixed(rate.truncateToDouble() == rate ? 0 : 1)}%)',
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

// ── Action row ────────────────────────────────────────────────────────────────

class _ActionRow extends StatelessWidget {
  final Bill bill;
  final VoidCallback onReload;
  const _ActionRow({required this.bill, required this.onReload});

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: _ActionButton(
            icon: Icons.refresh_rounded,
            label: 'Retry Sync',
            color: AppColors.primary,
            onTap: () {
              context
                  .read<BillsListBloc>()
                  .add(RetryBillRequested(bill.clientBillId));
              onReload();
            },
          ),
        ),
        SizedBox(width: 12.w),
        Expanded(
          child: _ActionButton(
            icon: Icons.delete_outline_rounded,
            label: 'Delete',
            color: AppColors.error,
            onTap: () async {
              final confirmed = await _confirmDelete(context);
              if (!confirmed) return;
              if (!context.mounted) return;
              context
                  .read<BillsListBloc>()
                  .add(DeleteBillRequested(bill.clientBillId));
              context.goNamed('bills');
            },
          ),
        ),
      ],
    );
  }

  Future<bool> _confirmDelete(BuildContext context) async {
    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('Delete this order?',
            style: GoogleFonts.barlowCondensed(
                fontSize: 18.sp, fontWeight: FontWeight.w700)),
        content: Text(
          "This removes the order from your device. It hasn't been synced yet, "
          "so the server won't be affected.",
          style: GoogleFonts.barlow(fontSize: 13.sp),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: AppColors.error),
            onPressed: () => Navigator.of(ctx).pop(true),
            child: const Text('Delete'),
          ),
        ],
      ),
    );
    return result ?? false;
  }
}

class _ActionButton extends StatelessWidget {
  final IconData icon;
  final String label;
  final Color color;
  final VoidCallback onTap;
  const _ActionButton({
    required this.icon,
    required this.label,
    required this.color,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(10.r),
        child: Ink(
          height: 48.h,
          decoration: BoxDecoration(
            color: color.withValues(alpha: 0.08),
            borderRadius: BorderRadius.circular(10.r),
            border: Border.all(color: color.withValues(alpha: 0.35)),
          ),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(icon, size: 16.r, color: color),
              SizedBox(width: 6.w),
              Text(
                label,
                style: GoogleFonts.barlowCondensed(
                  fontSize: 14.sp,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 0.5,
                  color: color,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Helpers ───────────────────────────────────────────────────────────────────

String _formatDate(DateTime d) {
  String two(int n) => n.toString().padLeft(2, '0');
  return '${d.year}-${two(d.month)}-${two(d.day)}';
}

String _formatDateTime(DateTime d) {
  String two(int n) => n.toString().padLeft(2, '0');
  return '${d.year}-${two(d.month)}-${two(d.day)}  ${two(d.hour)}:${two(d.minute)}';
}
