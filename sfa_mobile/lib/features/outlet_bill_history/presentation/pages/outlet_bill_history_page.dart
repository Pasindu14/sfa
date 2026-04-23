import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/outlet_bill_history/domain/entities/outlet_bill_summary.dart';
import 'package:uswatte/features/outlet_bill_history/presentation/cubit/outlet_bill_history_cubit.dart';
import 'package:uswatte/features/outlet_bill_history/presentation/cubit/outlet_bill_history_state.dart';

class OutletBillHistoryPage extends StatefulWidget {
  final int outletId;
  final String outletName;

  const OutletBillHistoryPage({
    super.key,
    required this.outletId,
    required this.outletName,
  });

  @override
  State<OutletBillHistoryPage> createState() => _OutletBillHistoryPageState();
}

class _OutletBillHistoryPageState extends State<OutletBillHistoryPage> {
  late DateTime _dateFrom;
  late DateTime _dateTo;

  @override
  void initState() {
    super.initState();
    _dateTo = DateTime.now();
    _dateFrom = _dateTo.subtract(const Duration(days: 30));
    _load();
  }

  void _load() {
    context.read<OutletBillHistoryCubit>().load(
          outletId: widget.outletId,
          dateFrom: _dateFrom,
          dateTo: _dateTo,
        );
  }

  Future<void> _pickFrom() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _dateFrom,
      firstDate: DateTime(2020),
      lastDate: _dateTo,
    );
    if (picked != null && picked != _dateFrom) {
      setState(() => _dateFrom = picked);
      _load();
    }
  }

  Future<void> _pickTo() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _dateTo,
      firstDate: _dateFrom,
      lastDate: DateTime.now(),
    );
    if (picked != null && picked != _dateTo) {
      setState(() => _dateTo = picked);
      _load();
    }
  }

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    return Scaffold(
      backgroundColor: AppColors.surface,
      body: Column(
        children: [
          _HistoryAppBar(
            outletName: widget.outletName,
            onBack: () => context.pop(),
          ),
          _DateRangeBar(
            dateFrom: _dateFrom,
            dateTo: _dateTo,
            onPickFrom: _pickFrom,
            onPickTo: _pickTo,
          ),
          Expanded(
            child: BlocBuilder<OutletBillHistoryCubit, OutletBillHistoryState>(
              builder: (ctx, state) {
                return switch (state) {
                  OutletBillHistoryInitial() ||
                  OutletBillHistoryLoading() =>
                    const Center(
                      child: CircularProgressIndicator(
                        color: AppColors.primary,
                        strokeWidth: 2,
                      ),
                    ),
                  OutletBillHistoryError(:final message) => _ErrorView(
                      message: message,
                      onRetry: _load,
                    ),
                  OutletBillHistoryLoaded(:final bills) => bills.isEmpty
                      ? _EmptyView(
                          dateFrom: _dateFrom,
                          dateTo: _dateTo,
                        )
                      : ListView.separated(
                          padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 40.h),
                          itemCount: bills.length,
                          separatorBuilder: (_, __) => SizedBox(height: 10.h),
                          itemBuilder: (_, i) => _BillTile(
                            bill: bills[i],
                            onTap: () => context.pushNamed(
                              'outletBillDetail',
                              pathParameters: {
                                'billingId': bills[i].id.toString(),
                              },
                            ),
                          ),
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

// ── App bar ───────────────────────────────────────────────────────────────────

class _HistoryAppBar extends StatelessWidget {
  final String outletName;
  final VoidCallback onBack;

  const _HistoryAppBar({required this.outletName, required this.onBack});

  @override
  Widget build(BuildContext context) {
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
        child: Padding(
          padding: EdgeInsets.fromLTRB(8.w, 4.h, 16.w, 16.h),
          child: Row(
            children: [
              GestureDetector(
                onTap: onBack,
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
              SizedBox(width: 8.w),
              Expanded(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'OUTLET BILL HISTORY',
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 18.sp,
                        fontWeight: FontWeight.w800,
                        letterSpacing: 1.5,
                        height: 1.0,
                        color: Colors.white,
                      ),
                    ),
                    SizedBox(height: 2.h),
                    Text(
                      outletName,
                      style: GoogleFonts.barlow(
                        fontSize: 11.sp,
                        color: Colors.white.withValues(alpha: 0.75),
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Date range bar ────────────────────────────────────────────────────────────

class _DateRangeBar extends StatelessWidget {
  final DateTime dateFrom;
  final DateTime dateTo;
  final VoidCallback onPickFrom;
  final VoidCallback onPickTo;

  const _DateRangeBar({
    required this.dateFrom,
    required this.dateTo,
    required this.onPickFrom,
    required this.onPickTo,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      color: Colors.white,
      padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 10.h),
      child: Row(
        children: [
          Icon(Icons.calendar_month_rounded,
              size: 14.r, color: AppColors.foregroundMuted),
          SizedBox(width: 8.w),
          Expanded(
            child: _DateChip(
              label: 'From',
              date: dateFrom,
              onTap: onPickFrom,
            ),
          ),
          Padding(
            padding: EdgeInsets.symmetric(horizontal: 8.w),
            child: Text(
              '→',
              style: GoogleFonts.barlow(
                  fontSize: 13.sp, color: AppColors.foregroundMuted),
            ),
          ),
          Expanded(
            child: _DateChip(
              label: 'To',
              date: dateTo,
              onTap: onPickTo,
            ),
          ),
        ],
      ),
    );
  }
}

class _DateChip extends StatelessWidget {
  final String label;
  final DateTime date;
  final VoidCallback onTap;

  const _DateChip({
    required this.label,
    required this.date,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 6.h),
        decoration: BoxDecoration(
          color: AppColors.primary.withValues(alpha: 0.06),
          borderRadius: BorderRadius.circular(8.r),
          border:
              Border.all(color: AppColors.primary.withValues(alpha: 0.20)),
        ),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    label.toUpperCase(),
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 9.sp,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 1.5,
                      color: AppColors.foregroundMuted,
                    ),
                  ),
                  Text(
                    _fmt(date),
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 13.sp,
                      fontWeight: FontWeight.w700,
                      color: AppColors.foreground,
                    ),
                  ),
                ],
              ),
            ),
            Icon(Icons.edit_calendar_rounded,
                size: 12.r, color: AppColors.primary),
          ],
        ),
      ),
    );
  }

  String _fmt(DateTime d) {
    final m = d.month.toString().padLeft(2, '0');
    final day = d.day.toString().padLeft(2, '0');
    return '${d.year}-$m-$day';
  }
}

// ── Bill tile ─────────────────────────────────────────────────────────────────

class _BillTile extends StatelessWidget {
  final OutletBillSummary bill;
  final VoidCallback onTap;

  const _BillTile({required this.bill, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: EdgeInsets.all(14.r),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(12.r),
          border: Border.all(color: AppColors.surfaceVariant),
          boxShadow: [
            BoxShadow(
              color: AppColors.foreground.withValues(alpha: 0.04),
              blurRadius: 8,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: Row(
          children: [
            Container(
              width: 40.r,
              height: 40.r,
              decoration: BoxDecoration(
                color: AppColors.primary.withValues(alpha: 0.08),
                borderRadius: BorderRadius.circular(10.r),
              ),
              child: Icon(Icons.receipt_long_rounded,
                  size: 18.r, color: AppColors.primary),
            ),
            SizedBox(width: 12.w),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    bill.billingNumber,
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 15.sp,
                      fontWeight: FontWeight.w800,
                      color: AppColors.foreground,
                    ),
                  ),
                  SizedBox(height: 2.h),
                  Text(
                    _formatDate(bill.billingDate),
                    style: GoogleFonts.barlow(
                        fontSize: 11.sp, color: AppColors.foregroundMuted),
                  ),
                ],
              ),
            ),
            Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text(
                  'Rs. ${bill.totalAmount.toStringAsFixed(2)}',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 15.sp,
                    fontWeight: FontWeight.w800,
                    color: AppColors.amber,
                  ),
                ),
                SizedBox(height: 4.h),
                _StatusChip(status: bill.status),
              ],
            ),
            SizedBox(width: 6.w),
            Icon(Icons.chevron_right_rounded,
                size: 16.r, color: AppColors.surfaceVariant),
          ],
        ),
      ),
    );
  }

  String _formatDate(DateTime d) {
    final m = d.month.toString().padLeft(2, '0');
    final day = d.day.toString().padLeft(2, '0');
    return '${d.year}-$m-$day';
  }
}

class _StatusChip extends StatelessWidget {
  final String status;
  const _StatusChip({required this.status});

  @override
  Widget build(BuildContext context) {
    final color = switch (status.toLowerCase()) {
      'confirmed' || 'finalized' => AppColors.success,
      'draft' => AppColors.warning,
      'cancelled' => AppColors.error,
      _ => AppColors.foregroundMuted,
    };

    return Container(
      padding: EdgeInsets.symmetric(horizontal: 7.w, vertical: 3.h),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.10),
        borderRadius: BorderRadius.circular(999),
        border: Border.all(color: color.withValues(alpha: 0.30)),
      ),
      child: Text(
        status,
        style: GoogleFonts.barlow(
          fontSize: 10.sp,
          fontWeight: FontWeight.w600,
          color: color,
        ),
      ),
    );
  }
}

// ── Empty state ───────────────────────────────────────────────────────────────

class _EmptyView extends StatelessWidget {
  final DateTime dateFrom;
  final DateTime dateTo;

  const _EmptyView({required this.dateFrom, required this.dateTo});

  String _fmt(DateTime d) {
    final m = d.month.toString().padLeft(2, '0');
    final day = d.day.toString().padLeft(2, '0');
    return '${d.year}-$m-$day';
  }

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: EdgeInsets.symmetric(horizontal: 32.w),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.receipt_outlined,
                size: 48.r, color: AppColors.surfaceVariant),
            SizedBox(height: 16.h),
            Text(
              'No orders found',
              style: GoogleFonts.barlowCondensed(
                fontSize: 18.sp,
                fontWeight: FontWeight.w700,
                color: AppColors.foreground,
              ),
            ),
            SizedBox(height: 6.h),
            Text(
              'No bills for this outlet between ${_fmt(dateFrom)} and ${_fmt(dateTo)}.',
              textAlign: TextAlign.center,
              style: GoogleFonts.barlow(
                  fontSize: 13.sp, color: AppColors.foregroundMuted),
            ),
          ],
        ),
      ),
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
              'Could not load history',
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
