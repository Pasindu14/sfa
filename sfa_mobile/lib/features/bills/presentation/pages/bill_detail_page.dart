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
import 'package:uswatte/core/widgets/app_spinner.dart';
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
                const AppSpinner.small(color: Colors.white),
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
    return AnnotatedRegion<SystemUiOverlayStyle>(
      value: const SystemUiOverlayStyle(
        statusBarColor: Colors.transparent,
        statusBarIconBrightness: Brightness.light,
        statusBarBrightness: Brightness.dark,
      ),
      child: Scaffold(
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
                  return const Center(child: AppSpinner());
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
      SyncStatus.synced    => (AppColors.success, 'Synced', Icons.cloud_done),
      SyncStatus.syncing   => (AppColors.primary, 'Syncing', Icons.cloud_upload_rounded),
      SyncStatus.pending   => (AppColors.warning, 'Pending', Icons.schedule),
      SyncStatus.failed    => (AppColors.error, 'Failed', Icons.error_outline),
      SyncStatus.cancelled => (AppColors.error, 'Cancelled', Icons.cancel_outlined),
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
          _infoRow(Icons.storefront_rounded, 'Outlet',
              bill.outletName ?? '#${bill.outletId}'),
          if (bill.outletCategory != null) ...[
            SizedBox(height: 6.h),
            _infoRow(Icons.category_rounded, 'Category', bill.outletCategory!),
          ],
          SizedBox(height: 6.h),
          _infoRow(Icons.receipt_rounded, 'Type', _billTypeLabel(bill)),
          SizedBox(height: 6.h),
          _infoRow(Icons.calendar_today_rounded, 'Date',
              _formatDate(bill.billingDate)),
          SizedBox(height: 6.h),
          _infoRow(Icons.access_time_rounded, 'Created',
              _formatDateTime(bill.createdAt)),
          if (bill.latitude != null && bill.longitude != null) ...[
            SizedBox(height: 6.h),
            _infoRow(
              Icons.location_on_rounded,
              'Location',
              '${bill.latitude!.toStringAsFixed(6)}, ${bill.longitude!.toStringAsFixed(6)}',
            ),
          ],
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
    final groupOrder = <String>[];
    final groups = <String, Map<String, BillItem>>{};
    for (final item in items) {
      final key = '${item.productId}:${item.billingItemType}';
      if (!groups.containsKey(key)) {
        groupOrder.add(key);
        groups[key] = {};
      }
      groups[key]![item.priceType] = item;
    }
    final groupList = groupOrder.map((k) => groups[k]!).toList();

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
          for (var i = 0; i < groupList.length; i++) ...[
            if (i > 0)
              Divider(height: 1, color: AppColors.surfaceVariant),
            _ItemRow(
              caseItem: groupList[i]['Case'],
              packetItem: groupList[i]['Packet'],
            ),
          ],
        ],
      ),
    );
  }
}

class _ItemRow extends StatelessWidget {
  final BillItem? caseItem;
  final BillItem? packetItem;
  const _ItemRow({this.caseItem, this.packetItem})
      : assert(caseItem != null || packetItem != null);

  BillItem get _primary => caseItem ?? packetItem!;

  double get _combinedGross =>
      (caseItem != null ? caseItem!.quantity * caseItem!.unitPrice : 0) +
      (packetItem != null ? packetItem!.quantity * packetItem!.unitPrice : 0);

  double get _combinedTotal {
    if (_primary.isReturn) return _combinedGross;
    if (_primary.isFreeIssue) return _combinedGross;
    return _combinedGross * (1 - _primary.discountRate / 100);
  }

  double get _combinedDiscount => _combinedGross - _combinedTotal;

  String _fmtQty(double q) =>
      q.toStringAsFixed(q.truncateToDouble() == q ? 0 : 1);

  @override
  Widget build(BuildContext context) {
    final name = _primary.productName ?? 'Product #${_primary.productId}';
    final totalColor = _primary.isReturn
        ? AppColors.error
        : _primary.isFreeIssue
            ? AppColors.primary
            : AppColors.amber;

    return Padding(
      padding: EdgeInsets.fromLTRB(14.w, 12.h, 14.w, 12.h),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Product name + combined total
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
                  if (_primary.discountRate > 0)
                    Text(
                      'Rs. ${_combinedGross.toStringAsFixed(2)}',
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 11.sp,
                        color: AppColors.foregroundMuted,
                        decoration: TextDecoration.lineThrough,
                        decorationColor: AppColors.foregroundMuted,
                      ),
                    ),
                  Text(
                    'Rs. ${_combinedTotal.toStringAsFixed(2)}',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 15.sp,
                      fontWeight: FontWeight.w800,
                      color: totalColor,
                    ),
                  ),
                ],
              ),
            ],
          ),
          SizedBox(height: 8.h),
          // Sub-line per type
          if (caseItem != null) _subLine(caseItem!, 'CASE', AppColors.primary),
          if (caseItem != null && packetItem != null) SizedBox(height: 5.h),
          if (packetItem != null) _subLine(packetItem!, 'PKT', AppColors.amber),
          // Extra badges (return type, FOC source, discount)
          if (_primary.isReturn || _primary.isFreeIssue || _primary.discountRate > 0) ...[
            SizedBox(height: 6.h),
            Row(
              children: [
                if (_primary.isReturn)
                  _returnTypeChip(_primary.returnType ?? 'Return'),
                if (_primary.isFreeIssue)
                  _freeIssueChip(_primary.freeIssueSource),
                if (_primary.discountRate > 0) ...[
                  _discountChip(_primary.discountRate, _combinedDiscount),
                ],
              ],
            ),
          ],
        ],
      ),
    );
  }

  Widget _subLine(BillItem item, String typeLabel, Color typeColor) {
    final lineGross = item.quantity * item.unitPrice;
    final lineTotal = item.isReturn || item.isFreeIssue
        ? lineGross
        : lineGross * (1 - item.discountRate / 100);

    return Row(
      children: [
        _priceTypeChip(typeLabel, typeColor),
        SizedBox(width: 6.w),
        Icon(Icons.tag_rounded, size: 10.r, color: AppColors.foregroundMuted),
        SizedBox(width: 2.w),
        Text(
          '${_fmtQty(item.quantity)} pks',
          style: GoogleFonts.barlow(fontSize: 11.sp, color: AppColors.foregroundMuted),
        ),
        SizedBox(width: 8.w),
        Icon(Icons.sell_rounded, size: 10.r, color: AppColors.foregroundMuted),
        SizedBox(width: 2.w),
        Text(
          'Rs. ${item.unitPrice.toStringAsFixed(2)}/pk',
          style: GoogleFonts.barlow(fontSize: 11.sp, color: AppColors.foregroundMuted),
        ),
        const Spacer(),
        Text(
          'Rs. ${lineTotal.toStringAsFixed(2)}',
          style: GoogleFonts.barlowCondensed(
            fontSize: 12.sp,
            fontWeight: FontWeight.w700,
            color: item.isReturn ? AppColors.error : AppColors.foreground,
          ),
        ),
      ],
    );
  }

  // ignore: unused_element
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

  Widget _priceTypeChip(String label, Color color) {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 5.w, vertical: 2.h),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.10),
        borderRadius: BorderRadius.circular(4.r),
        border: Border.all(color: color.withValues(alpha: 0.30)),
      ),
      child: Text(
        label,
        style: GoogleFonts.barlowCondensed(
          fontSize: 9.sp,
          fontWeight: FontWeight.w800,
          letterSpacing: 0.5,
          color: color,
        ),
      ),
    );
  }

  Widget _returnTypeChip(String returnType) {
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
          Icon(Icons.undo_rounded, size: 9.r, color: AppColors.error),
          SizedBox(width: 3.w),
          Text(
            returnType,
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

  Widget _freeIssueChip(String? source) {
    final suffix = source != null ? ' · ${source.toUpperCase()}' : '';
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 6.w, vertical: 2.h),
      decoration: BoxDecoration(
        color: AppColors.success.withValues(alpha: 0.10),
        borderRadius: BorderRadius.circular(999),
        border: Border.all(color: AppColors.success.withValues(alpha: 0.30)),
      ),
      child: Text(
        'FREE$suffix',
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

  // FOC values derived from items — server doesn't ship the per-source split into
  // the local Bill entity, but we already have the lines so we recompute here.
  double _focValue(String? source) => bill.items
      .where((i) => i.isFreeIssue && i.freeIssueSource == source)
      .fold<double>(0, (s, i) => s + i.quantity * i.unitPrice);

  @override
  Widget build(BuildContext context) {
    final focCompany = _focValue('Company');
    final focDistributor = _focValue('Distributor');
    final hasFoc = focCompany > 0 || focDistributor > 0;

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
          if (hasFoc) ...[
            SizedBox(height: 6.h),
            _line('Free issues (info)', focCompany + focDistributor),
            if (focCompany > 0 && focDistributor > 0) ...[
              SizedBox(height: 4.h),
              _subLine('  · By Company', focCompany),
              SizedBox(height: 2.h),
              _subLine('  · By Distributor', focDistributor),
            ],
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

  Widget _subLine(String label, double amount) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          label,
          style: GoogleFonts.barlow(
              fontSize: 11.sp, color: Colors.white.withValues(alpha: 0.40)),
        ),
        Text(
          'Rs. ${amount.toStringAsFixed(2)}',
          style: GoogleFonts.barlowCondensed(
            fontSize: 12.sp,
            fontWeight: FontWeight.w600,
            color: Colors.white.withValues(alpha: 0.65),
          ),
        ),
      ],
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

  static const _terminalCodes = {'INSUFFICIENT_STOCK', 'VALIDATION_FAILED'};

  bool get _isTerminalFailure =>
      _terminalCodes.contains(bill.lastSyncErrorCode);

  @override
  Widget build(BuildContext context) {
    final deleteButton = Expanded(
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
          // Pop back to the existing list (which reloads on return) rather than
          // goNamed('bills'), which would discard the SalesRepHome page underneath
          // and leave the empty /sales-rep shell (black screen) on the next back.
          context.pop();
        },
      ),
    );

    if (_isTerminalFailure) {
      return Row(children: [deleteButton]);
    }

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
        deleteButton,
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

String _billTypeLabel(Bill bill) {
  final hasSale   = bill.items.any((i) => i.billingItemType == 'Sale');
  final hasReturn = bill.items.any((i) => i.billingItemType == 'Return');
  if (hasSale && hasReturn) return 'Mixed';
  if (hasReturn) return 'Return';
  return 'Sale';
}

String _formatDate(DateTime d) {
  String two(int n) => n.toString().padLeft(2, '0');
  return '${d.year}-${two(d.month)}-${two(d.day)}';
}

String _formatDateTime(DateTime d) {
  String two(int n) => n.toString().padLeft(2, '0');
  return '${d.year}-${two(d.month)}-${two(d.day)}  ${two(d.hour)}:${two(d.minute)}';
}
