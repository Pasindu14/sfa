import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/bills/domain/entities/sync_status.dart';
import 'package:uswatte/features/not_billings/domain/entities/not_billing.dart';
import 'package:uswatte/features/not_billings/presentation/bloc/not_billings_list_bloc.dart';
import 'package:uswatte/features/not_billings/presentation/bloc/not_billings_list_event.dart';
import 'package:uswatte/features/not_billings/presentation/bloc/not_billings_list_state.dart';

class NotBillingsListPage extends StatelessWidget {
  const NotBillingsListPage({super.key});

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
        label: Text(
          'Not Billing',
          style: GoogleFonts.barlowCondensed(
            color: Colors.white,
            fontWeight: FontWeight.w700,
            fontSize: 15.sp,
            letterSpacing: 0.5,
          ),
        ),
        onPressed: () => context.goNamed('createNotBilling'),
      ),
      body: Column(
        children: [
          // ── Header ─────────────────────────────────────────────────────
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
                      onTap: () => context.canPop()
                          ? context.pop()
                          : context.goNamed('salesRepHome'),
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
                        'NOT BILLING',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 18.sp,
                          fontWeight: FontWeight.w800,
                          letterSpacing: 1.5,
                          height: 1.0,
                          color: Colors.white,
                        ),
                      ),
                    ),
                    GestureDetector(
                      onTap: () => context
                          .read<NotBillingsListBloc>()
                          .add(const FlushAllNotBillingsRequested()),
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
                        child: Icon(Icons.sync_rounded,
                            size: 18.r, color: Colors.white),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),

          // ── List ────────────────────────────────────────────────────────
          Expanded(
            child: BlocBuilder<NotBillingsListBloc, NotBillingsListState>(
              builder: (context, state) {
                if (state is NotBillingsListLoading ||
                    state is NotBillingsListInitial) {
                  return const Center(child: CircularProgressIndicator());
                }

                if (state is NotBillingsListError) {
                  return Center(
                    child: Text(
                      state.message,
                      style: GoogleFonts.barlow(
                          fontSize: 13.sp, color: AppColors.foregroundMuted),
                    ),
                  );
                }

                if (state is NotBillingsListLoaded) {
                  if (state.records.isEmpty) {
                    return Center(
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(Icons.report_problem_outlined,
                              size: 48.r,
                              color: AppColors.foregroundMuted
                                  .withValues(alpha: 0.4)),
                          SizedBox(height: 12.h),
                          Text(
                            'No not-billing records yet',
                            style: GoogleFonts.barlowCondensed(
                              fontSize: 16.sp,
                              fontWeight: FontWeight.w600,
                              color: AppColors.foregroundMuted,
                            ),
                          ),
                        ],
                      ),
                    );
                  }

                  return ListView.separated(
                    padding:
                        EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 100.h),
                    itemCount: state.records.length,
                    separatorBuilder: (_, __) => SizedBox(height: 10.h),
                    itemBuilder: (context, i) {
                      final r = state.records[i];
                      return _NotBillingCard(
                        record: r,
                        onRetry: () => context
                            .read<NotBillingsListBloc>()
                            .add(RetryNotBillingRequested(
                                r.clientNotBillingId)),
                        onDelete: () => context
                            .read<NotBillingsListBloc>()
                            .add(DeleteNotBillingRequested(
                                r.clientNotBillingId)),
                        onTap: () => context.goNamed(
                          'notBillingDetail',
                          pathParameters: {'id': r.clientNotBillingId},
                        ),
                      );
                    },
                  );
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

class _NotBillingCard extends StatelessWidget {
  final NotBilling record;
  final VoidCallback onRetry;
  final VoidCallback onDelete;
  final VoidCallback onTap;

  const _NotBillingCard({
    required this.record,
    required this.onRetry,
    required this.onDelete,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final (chipColor, chipLabel, chipIcon) = switch (record.syncStatus) {
      SyncStatus.synced  => (const Color(0xFF22C55E), 'Synced', Icons.cloud_done_outlined),
      SyncStatus.syncing => (const Color(0xFF3B82F6), 'Syncing', Icons.cloud_upload_outlined),
      SyncStatus.pending => (const Color(0xFFF59E0B), 'Pending', Icons.schedule_rounded),
      SyncStatus.failed  => (const Color(0xFFEF4444), 'Failed', Icons.error_outline_rounded),
    };

    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: EdgeInsets.all(14.r),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(14.r),
          boxShadow: [
            BoxShadow(
              color: AppColors.foreground.withValues(alpha: 0.04),
              blurRadius: 8,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    record.serverNotBillingNumber ?? 'Pending sync...',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 15.sp,
                      fontWeight: FontWeight.w800,
                      letterSpacing: 0.5,
                      color: AppColors.foreground,
                    ),
                  ),
                ),
                Container(
                  padding:
                      EdgeInsets.symmetric(horizontal: 8.w, vertical: 3.h),
                  decoration: BoxDecoration(
                    color: chipColor.withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(20.r),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(chipIcon, size: 10.r, color: chipColor),
                      SizedBox(width: 4.w),
                      Text(
                        chipLabel,
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 10.sp,
                          fontWeight: FontWeight.w700,
                          color: chipColor,
                          letterSpacing: 0.5,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            SizedBox(height: 6.h),
            Text(
              record.reason.displayLabel,
              style: GoogleFonts.barlowCondensed(
                fontSize: 13.sp,
                fontWeight: FontWeight.w600,
                color: AppColors.foregroundMuted,
              ),
            ),
            SizedBox(height: 2.h),
            Text(
              '${record.notBillingDate.day.toString().padLeft(2, '0')}/'
              '${record.notBillingDate.month.toString().padLeft(2, '0')}/'
              '${record.notBillingDate.year}  ·  Outlet #${record.outletId}',
              style: GoogleFonts.barlow(
                fontSize: 11.sp,
                color: AppColors.foregroundMuted,
              ),
            ),
            if (record.syncStatus == SyncStatus.failed &&
                record.lastSyncError != null) ...[
              SizedBox(height: 8.h),
              Container(
                padding: EdgeInsets.all(8.r),
                decoration: BoxDecoration(
                  color: const Color(0xFFEF4444).withValues(alpha: 0.07),
                  borderRadius: BorderRadius.circular(8.r),
                ),
                child: Row(
                  children: [
                    Icon(Icons.error_outline_rounded,
                        size: 13.r, color: const Color(0xFFEF4444)),
                    SizedBox(width: 6.w),
                    Expanded(
                      child: Text(
                        record.lastSyncError!,
                        style: GoogleFonts.barlow(
                          fontSize: 11.sp,
                          color: const Color(0xFFEF4444),
                        ),
                      ),
                    ),
                    TextButton(
                      onPressed: onRetry,
                      style: TextButton.styleFrom(
                        padding: EdgeInsets.symmetric(
                            horizontal: 8.w, vertical: 2.h),
                        minimumSize: Size.zero,
                        tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                      ),
                      child: Text(
                        'Retry',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 12.sp,
                          fontWeight: FontWeight.w700,
                          color: AppColors.primary,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
