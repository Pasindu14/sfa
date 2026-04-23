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

class NotBillingDetailPage extends StatelessWidget {
  final String clientNotBillingId;

  const NotBillingDetailPage({super.key, required this.clientNotBillingId});

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    return BlocBuilder<NotBillingsListBloc, NotBillingsListState>(
      builder: (context, state) {
        NotBilling? record;
        if (state is NotBillingsListLoaded) {
          try {
            record = state.records
                .firstWhere((r) => r.clientNotBillingId == clientNotBillingId);
          } catch (_) {}
        }

        return Scaffold(
          backgroundColor: AppColors.background,
          body: Column(
            children: [
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
                          onTap: () => context.canPop() ? context.pop() : null,
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
                            record?.serverNotBillingNumber ?? 'NOT BILLING',
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
              Expanded(
                child: record == null
                    ? const Center(child: CircularProgressIndicator())
                    : _DetailBody(record: record),
              ),
            ],
          ),
        );
      },
    );
  }
}

class _DetailBody extends StatelessWidget {
  final NotBilling record;

  const _DetailBody({required this.record});

  @override
  Widget build(BuildContext context) {
    final (chipColor, chipLabel) = switch (record.syncStatus) {
      SyncStatus.synced  => (const Color(0xFF22C55E), 'Synced'),
      SyncStatus.syncing => (const Color(0xFF3B82F6), 'Syncing'),
      SyncStatus.pending => (const Color(0xFFF59E0B), 'Pending sync'),
      SyncStatus.failed  => (const Color(0xFFEF4444), 'Sync failed'),
    };

    return SingleChildScrollView(
      padding: EdgeInsets.all(16.w),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Status chip
          Container(
            padding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 4.h),
            decoration: BoxDecoration(
              color: chipColor.withValues(alpha: 0.12),
              borderRadius: BorderRadius.circular(20.r),
            ),
            child: Text(
              chipLabel,
              style: GoogleFonts.barlowCondensed(
                fontSize: 11.sp,
                fontWeight: FontWeight.w700,
                color: chipColor,
                letterSpacing: 0.5,
              ),
            ),
          ),
          SizedBox(height: 16.h),

          _InfoCard(children: [
            _InfoRow('Outlet ID', '#${record.outletId}'),
            _InfoRow('Date', '${record.notBillingDate.day.toString().padLeft(2, '0')}/'
                '${record.notBillingDate.month.toString().padLeft(2, '0')}/'
                '${record.notBillingDate.year}'),
            _InfoRow('Reason', record.reason.displayLabel),
            if (record.notes != null && record.notes!.isNotEmpty)
              _InfoRow('Notes', record.notes!),
          ]),

          if (record.serverNotBillingNumber != null) ...[
            SizedBox(height: 12.h),
            _InfoCard(children: [
              _InfoRow('Server Number', record.serverNotBillingNumber!),
            ]),
          ],

          if (record.syncStatus == SyncStatus.failed &&
              record.lastSyncError != null) ...[
            SizedBox(height: 12.h),
            Container(
              padding: EdgeInsets.all(12.r),
              decoration: BoxDecoration(
                color: const Color(0xFFEF4444).withValues(alpha: 0.07),
                borderRadius: BorderRadius.circular(10.r),
                border: Border.all(
                    color: const Color(0xFFEF4444).withValues(alpha: 0.25)),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Icon(Icons.error_outline_rounded,
                          size: 14.r, color: const Color(0xFFEF4444)),
                      SizedBox(width: 6.w),
                      Text(
                        'SYNC ERROR',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 10.sp,
                          fontWeight: FontWeight.w700,
                          color: const Color(0xFFEF4444),
                          letterSpacing: 1.5,
                        ),
                      ),
                    ],
                  ),
                  SizedBox(height: 6.h),
                  Text(
                    record.lastSyncError!,
                    style: GoogleFonts.barlow(
                        fontSize: 12.sp, color: const Color(0xFFEF4444)),
                  ),
                  SizedBox(height: 10.h),
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton(
                      onPressed: () => context
                          .read<NotBillingsListBloc>()
                          .add(RetryNotBillingRequested(
                              record.clientNotBillingId)),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppColors.primary,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(8.r),
                        ),
                        padding: EdgeInsets.symmetric(vertical: 10.h),
                        elevation: 0,
                      ),
                      child: Text(
                        'Retry Sync',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 14.sp,
                          fontWeight: FontWeight.w700,
                          color: Colors.white,
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _InfoCard extends StatelessWidget {
  final List<Widget> children;
  const _InfoCard({required this.children});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.all(14.r),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12.r),
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
        children: children,
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  final String label;
  final String value;
  const _InfoRow(this.label, this.value);

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.symmetric(vertical: 5.h),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 110.w,
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
              style: GoogleFonts.barlow(
                fontSize: 12.sp,
                fontWeight: FontWeight.w600,
                color: AppColors.foreground,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
