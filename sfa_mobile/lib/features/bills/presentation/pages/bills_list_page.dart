import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/core/widgets/app_spinner.dart';
import 'package:uswatte/features/bills/domain/entities/bill.dart';
import 'package:uswatte/features/bills/domain/entities/sync_status.dart';
import 'package:uswatte/features/bills/presentation/bloc/bills_list_bloc.dart';
import 'package:uswatte/features/bills/presentation/bloc/bills_list_event.dart';
import 'package:uswatte/features/bills/presentation/bloc/bills_list_state.dart';

class BillsListPage extends StatelessWidget {
  const BillsListPage({super.key});

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    return Scaffold(
      backgroundColor: AppColors.background,
      floatingActionButton: FloatingActionButton.extended(
        backgroundColor: AppColors.primary,
        icon: const Icon(Icons.add, color: Colors.white),
        label: Text('New Order',
            style: GoogleFonts.barlowCondensed(
              color: Colors.white,
              fontWeight: FontWeight.w700,
              fontSize: 15.sp,
              letterSpacing: 0.5,
            )),
        onPressed: () async {
          await context.pushNamed('createBill');
          // Refresh the list so a just-created bill appears (create pops back here).
          if (context.mounted) {
            context.read<BillsListBloc>().add(const LoadBillsRequested());
          }
        },
      ),
      body: Column(
        children: [
          // ── Gradient header ───────────────────────────────────────────────
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
                      onTap: () {
                        if (context.canPop()) {
                          context.pop();
                        } else {
                          context.goNamed('salesRepHome');
                        }
                      },
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
                        'MY ORDERS',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 18.sp,
                          fontWeight: FontWeight.w800,
                          letterSpacing: 1.5,
                          height: 1.0,
                          color: Colors.white,
                        ),
                      ),
                    ),
                    // Sync button
                    BlocBuilder<BillsListBloc, BillsListState>(
                      builder: (context, state) {
                        final isSyncing = state is BillsListLoading;
                        return GestureDetector(
                          onTap: isSyncing
                              ? null
                              : () => context
                                  .read<BillsListBloc>()
                                  .add(const FlushAllRequested()),
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
                            child: Center(
                              child: isSyncing
                                  ? const AppSpinner.small(
                                      color: Colors.white)
                                  : Icon(Icons.cloud_sync_rounded,
                                      size: 16.r, color: Colors.white),
                            ),
                          ),
                        );
                      },
                    ),
                  ],
                ),
              ),
            ),
          ),

          // ── Body ──────────────────────────────────────────────────────────
          Expanded(
            child: BlocBuilder<BillsListBloc, BillsListState>(
              builder: (ctx, state) {
                if (state is BillsListLoading || state is BillsListInitial) {
                  return const Center(child: AppSpinner());
                }
                if (state is BillsListError) {
                  return Center(child: Text(state.message));
                }
                final loaded = state as BillsListLoaded;
                if (loaded.bills.isEmpty) return const _EmptyView();
                return ListView.separated(
                  padding: EdgeInsets.only(bottom: 100.h),
                  itemCount: loaded.bills.length,
                  separatorBuilder: (_, __) => const Divider(
                      height: 1, color: AppColors.surfaceVariant),
                  itemBuilder: (_, i) => _BillTile(bill: loaded.bills[i]),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

class _BillTile extends StatelessWidget {
  final Bill bill;
  const _BillTile({required this.bill});

  @override
  Widget build(BuildContext context) {
    final label = bill.syncStatus == SyncStatus.synced
        ? (bill.serverBillNumber ?? '—')
        : '#${bill.clientBillId.substring(0, 6).toUpperCase()}';

    return InkWell(
      onTap: () async {
        await context.pushNamed('billDetail',
            pathParameters: {'id': bill.clientBillId});
        // Refresh on return so a delete done from the detail page is reflected.
        if (context.mounted) {
          context.read<BillsListBloc>().add(const LoadBillsRequested());
        }
      },
      child: Padding(
        padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 12.h),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Text(
                        label,
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 15.sp,
                          fontWeight: FontWeight.w700,
                          color: AppColors.foreground,
                        ),
                      ),
                      SizedBox(width: 8.w),
                      _StatusChip(status: bill.syncStatus),
                    ],
                  ),
                  SizedBox(height: 3.h),
                  Text(
                    bill.outletName ?? '#${bill.outletId}',
                    style: GoogleFonts.barlow(
                      fontSize: 13.sp,
                      fontWeight: FontWeight.w600,
                      color: AppColors.foreground,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  SizedBox(height: 2.h),
                  Text(
                    [
                      if (bill.outletCategory != null) bill.outletCategory!,
                      '${bill.items.length} items',
                      'Rs. ${bill.totalAmount.toStringAsFixed(2)}',
                    ].join('  ·  '),
                    style: GoogleFonts.barlow(
                      fontSize: 12.sp,
                      color: AppColors.foregroundMuted,
                    ),
                  ),
                  Text(
                    _formatDateTime(bill.createdAt),
                    style: GoogleFonts.barlow(
                      fontSize: 11.sp,
                      color: AppColors.foregroundMuted.withValues(alpha: 0.65),
                    ),
                  ),
                ],
              ),
            ),
            Icon(Icons.chevron_right_rounded,
                size: 18.r, color: AppColors.foregroundMuted),
          ],
        ),
      ),
    );
  }
}

class _StatusChip extends StatelessWidget {
  final SyncStatus status;
  const _StatusChip({required this.status});

  @override
  Widget build(BuildContext context) {
    final (color, label, icon) = switch (status) {
      SyncStatus.synced    => (AppColors.success, 'Synced', Icons.cloud_done),
      SyncStatus.syncing   => (AppColors.primary, 'Syncing', Icons.cloud_upload_rounded),
      SyncStatus.pending   => (AppColors.warning, 'Pending', Icons.schedule),
      SyncStatus.failed    => (AppColors.error, 'Failed', Icons.error_outline),
      SyncStatus.cancelled => (AppColors.error, 'Cancelled', Icons.cancel_outlined),
    };
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 7.w, vertical: 3.h),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(999),
        border: Border.all(color: color.withValues(alpha: 0.35)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 10.r, color: color),
          SizedBox(width: 3.w),
          Text(
            label,
            style: GoogleFonts.barlow(
              color: color,
              fontWeight: FontWeight.w600,
              fontSize: 10.sp,
            ),
          ),
        ],
      ),
    );
  }
}

String _formatDateTime(DateTime d) {
  // Server timestamps arrive as UTC; convert to the rep's local (SL) time before
  // formatting. .toLocal() is a no-op on values that are already local.
  final local = d.toLocal();
  String two(int n) => n.toString().padLeft(2, '0');
  return '${local.year}-${two(local.month)}-${two(local.day)}  ${two(local.hour)}:${two(local.minute)}';
}

class _EmptyView extends StatelessWidget {
  const _EmptyView();

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: EdgeInsets.all(32.r),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.receipt_long_rounded,
                size: 56.r, color: AppColors.surfaceVariant),
            SizedBox(height: 14.h),
            Text(
              'No orders yet',
              style: GoogleFonts.barlowCondensed(
                fontSize: 20.sp,
                fontWeight: FontWeight.w700,
                color: AppColors.foreground,
              ),
            ),
            SizedBox(height: 4.h),
            Text(
              'Tap New Order to create your first bill.',
              style: GoogleFonts.barlow(
                fontSize: 13.sp,
                color: AppColors.foregroundMuted,
              ),
              textAlign: TextAlign.center,
            ),
          ],
        ),
      ),
    );
  }
}
