import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/my_bills/domain/entities/my_bill_summary.dart';
import 'package:uswatte/features/my_bills/presentation/cubit/my_bills_cubit.dart';
import 'package:uswatte/features/my_bills/presentation/cubit/my_bills_state.dart';

enum _FilterMode { dateRange, billNumber }

class MyBillsPage extends StatefulWidget {
  const MyBillsPage({super.key});

  @override
  State<MyBillsPage> createState() => _MyBillsPageState();
}

class _MyBillsPageState extends State<MyBillsPage> {
  _FilterMode _mode = _FilterMode.dateRange;

  late DateTime _dateFrom;
  late DateTime _dateTo;
  final _billNoController = TextEditingController();
  final _scrollController = ScrollController();

  @override
  void initState() {
    super.initState();
    _dateTo = DateTime.now();
    _dateFrom = _dateTo.subtract(const Duration(days: 30));
    _scrollController.addListener(_onScroll);
    _searchDateRange();
  }

  @override
  void dispose() {
    _billNoController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_mode == _FilterMode.dateRange &&
        _scrollController.position.pixels >=
            _scrollController.position.maxScrollExtent - 200) {
      context.read<MyBillsCubit>().loadMore();
    }
  }

  void _searchDateRange() {
    context.read<MyBillsCubit>().search(
          dateFrom: _fmt(_dateFrom),
          dateTo: _fmt(_dateTo),
        );
  }

  void _searchBillNo() {
    final billNo = _billNoController.text.trim();
    if (billNo.isEmpty) return;
    context.read<MyBillsCubit>().search(billNo: billNo);
  }

  Future<void> _pickFrom() async {
    final earliest = _dateTo.subtract(const Duration(days: 90));
    final picked = await showDatePicker(
      context: context,
      initialDate: _dateFrom,
      firstDate: earliest.isAfter(DateTime(2020)) ? earliest : DateTime(2020),
      lastDate: _dateTo,
    );
    if (picked != null && picked != _dateFrom) {
      setState(() => _dateFrom = picked);
      _searchDateRange();
    }
  }

  Future<void> _pickTo() async {
    final latest = _dateFrom.add(const Duration(days: 90));
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _dateTo,
      firstDate: _dateFrom,
      lastDate: latest.isBefore(now) ? latest : now,
    );
    if (picked != null && picked != _dateTo) {
      setState(() => _dateTo = picked);
      _searchDateRange();
    }
  }

  void _switchMode(_FilterMode mode) {
    if (_mode == mode) return;
    setState(() => _mode = mode);
    if (mode == _FilterMode.dateRange) {
      _searchDateRange();
    }
  }

  String _fmt(DateTime d) {
    final m = d.month.toString().padLeft(2, '0');
    final day = d.day.toString().padLeft(2, '0');
    return '${d.year}-$m-$day';
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
          _AppBar(onBack: () => context.pop()),
          _ModeToggle(mode: _mode, onSwitch: _switchMode),
          if (_mode == _FilterMode.dateRange)
            _DateRangeBar(
              dateFrom: _dateFrom,
              dateTo: _dateTo,
              onPickFrom: _pickFrom,
              onPickTo: _pickTo,
            )
          else
            _BillNumberBar(
              controller: _billNoController,
              onSearch: _searchBillNo,
            ),
          Expanded(
            child: BlocBuilder<MyBillsCubit, MyBillsState>(
              builder: (ctx, state) {
                return switch (state) {
                  MyBillsInitial() ||
                  MyBillsLoading() =>
                    const Center(
                      child: CircularProgressIndicator(
                        color: AppColors.primary,
                        strokeWidth: 2,
                      ),
                    ),
                  MyBillsError(:final message) => _ErrorView(
                      message: message,
                      onRetry: _mode == _FilterMode.dateRange
                          ? _searchDateRange
                          : _searchBillNo,
                    ),
                  MyBillsLoaded(:final bills, :final hasMore, :final isLoadingMore) =>
                    bills.isEmpty
                      ? _EmptyView(mode: _mode)
                      : ListView.separated(
                          controller: _scrollController,
                          padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 40.h),
                          itemCount: bills.length + (hasMore ? 1 : 0),
                          separatorBuilder: (_, __) => SizedBox(height: 10.h),
                          itemBuilder: (_, i) {
                            if (i == bills.length) {
                              return Padding(
                                padding: EdgeInsets.symmetric(vertical: 16.h),
                                child: Center(
                                  child: isLoadingMore
                                      ? const CircularProgressIndicator(
                                          color: AppColors.primary,
                                          strokeWidth: 2,
                                        )
                                      : const SizedBox.shrink(),
                                ),
                              );
                            }
                            return _BillTile(
                              bill: bills[i],
                              onTap: () => context.pushNamed(
                                'outletBillDetail',
                                pathParameters: {
                                  'billingId': bills[i].id.toString(),
                                },
                              ),
                            );
                          },
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

class _AppBar extends StatelessWidget {
  final VoidCallback onBack;
  const _AppBar({required this.onBack});

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
              Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'INVOICE HISTORY',
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
                    'Search your synced invoices',
                    style: GoogleFonts.barlow(
                      fontSize: 11.sp,
                      color: Colors.white.withValues(alpha: 0.75),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Mode toggle ───────────────────────────────────────────────────────────────

class _ModeToggle extends StatelessWidget {
  final _FilterMode mode;
  final void Function(_FilterMode) onSwitch;

  const _ModeToggle({required this.mode, required this.onSwitch});

  @override
  Widget build(BuildContext context) {
    return Container(
      color: Colors.white,
      padding: EdgeInsets.fromLTRB(16.w, 10.h, 16.w, 10.h),
      child: Row(
        children: [
          Expanded(
            child: _ModeTab(
              label: 'DATE RANGE',
              icon: Icons.calendar_month_rounded,
              selected: mode == _FilterMode.dateRange,
              onTap: () => onSwitch(_FilterMode.dateRange),
            ),
          ),
          SizedBox(width: 8.w),
          Expanded(
            child: _ModeTab(
              label: 'BILL NUMBER',
              icon: Icons.search_rounded,
              selected: mode == _FilterMode.billNumber,
              onTap: () => onSwitch(_FilterMode.billNumber),
            ),
          ),
        ],
      ),
    );
  }
}

class _ModeTab extends StatelessWidget {
  final String label;
  final IconData icon;
  final bool selected;
  final VoidCallback onTap;

  const _ModeTab({
    required this.label,
    required this.icon,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 180),
        padding: EdgeInsets.symmetric(vertical: 8.h),
        decoration: BoxDecoration(
          color: selected
              ? AppColors.primary.withValues(alpha: 0.10)
              : AppColors.surface,
          borderRadius: BorderRadius.circular(8.r),
          border: Border.all(
            color: selected
                ? AppColors.primary.withValues(alpha: 0.35)
                : AppColors.surfaceVariant,
          ),
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon,
                size: 13.r,
                color: selected ? AppColors.primary : AppColors.foregroundMuted),
            SizedBox(width: 6.w),
            Text(
              label,
              style: GoogleFonts.barlowCondensed(
                fontSize: 11.sp,
                fontWeight: FontWeight.w700,
                letterSpacing: 1.2,
                color: selected ? AppColors.primary : AppColors.foregroundMuted,
              ),
            ),
          ],
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
      padding: EdgeInsets.fromLTRB(16.w, 0, 16.w, 10.h),
      child: Row(
        children: [
          Icon(Icons.date_range_rounded,
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
            child: Text('→',
                style: GoogleFonts.barlow(
                    fontSize: 13.sp, color: AppColors.foregroundMuted)),
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

  String _fmt(DateTime d) {
    final m = d.month.toString().padLeft(2, '0');
    final day = d.day.toString().padLeft(2, '0');
    return '${d.year}-$m-$day';
  }

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
}

// ── Bill number bar ───────────────────────────────────────────────────────────

class _BillNumberBar extends StatelessWidget {
  final TextEditingController controller;
  final VoidCallback onSearch;

  const _BillNumberBar({required this.controller, required this.onSearch});

  @override
  Widget build(BuildContext context) {
    return Container(
      color: Colors.white,
      padding: EdgeInsets.fromLTRB(16.w, 0, 16.w, 10.h),
      child: Row(
        children: [
          Expanded(
            child: Container(
              height: 40.h,
              decoration: BoxDecoration(
                color: AppColors.primary.withValues(alpha: 0.06),
                borderRadius: BorderRadius.circular(8.r),
                border:
                    Border.all(color: AppColors.primary.withValues(alpha: 0.20)),
              ),
              child: TextField(
                controller: controller,
                textCapitalization: TextCapitalization.characters,
                style: GoogleFonts.barlowCondensed(
                  fontSize: 14.sp,
                  fontWeight: FontWeight.w700,
                  color: AppColors.foreground,
                ),
                decoration: InputDecoration(
                  hintText: 'e.g. BIL-2026-00123',
                  hintStyle: GoogleFonts.barlowCondensed(
                    fontSize: 13.sp,
                    color: AppColors.foregroundMuted,
                  ),
                  prefixIcon: Icon(Icons.receipt_long_rounded,
                      size: 16.r, color: AppColors.foregroundMuted),
                  border: InputBorder.none,
                  contentPadding: EdgeInsets.symmetric(vertical: 10.h),
                ),
                onSubmitted: (_) => onSearch(),
              ),
            ),
          ),
          SizedBox(width: 8.w),
          GestureDetector(
            onTap: onSearch,
            child: Container(
              height: 40.h,
              width: 40.h,
              decoration: BoxDecoration(
                color: AppColors.primary,
                borderRadius: BorderRadius.circular(8.r),
              ),
              child: Icon(Icons.search_rounded,
                  size: 18.r, color: Colors.white),
            ),
          ),
        ],
      ),
    );
  }
}

// ── Bill tile ─────────────────────────────────────────────────────────────────

class _BillTile extends StatelessWidget {
  final MyBillSummary bill;
  final VoidCallback onTap;

  const _BillTile({required this.bill, required this.onTap});

  String _formatDate(DateTime d) {
    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
    ];
    return '${months[d.month - 1]} ${d.day}, ${d.year}';
  }

  // Accent encodes worst-case status: error > warning > primary > success
  Color _accentColor() {
    final statuses = [
      bill.repStatus.toLowerCase(),
      bill.distributorStatus.toLowerCase(),
    ];
    if (statuses.any((s) => s == 'rejected' || s == 'cancelled')) {
      return AppColors.error;
    }
    if (statuses.every((s) => s == 'approved')) return AppColors.success;
    if (statuses.any((s) => s == 'submitted')) return AppColors.primary;
    return AppColors.warning;
  }

  @override
  Widget build(BuildContext context) {
    final accent = _accentColor();

    return GestureDetector(
      onTap: onTap,
      child: Container(
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(12.r),
          border: Border.all(color: AppColors.surfaceVariant),
          boxShadow: [
            BoxShadow(
              color: accent.withValues(alpha: 0.08),
              blurRadius: 10,
              offset: const Offset(0, 3),
            ),
          ],
        ),
        child: ClipRRect(
          borderRadius: BorderRadius.circular(12.r),
          child: IntrinsicHeight(
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Status accent strip
                Container(width: 4.w, color: accent),
                // Card body
                Expanded(
                  child: Padding(
                    padding: EdgeInsets.fromLTRB(12.w, 11.h, 8.w, 11.h),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        // Bill number + amount
                        Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    bill.billingNumber,
                                    style: GoogleFonts.barlowCondensed(
                                      fontSize: 16.sp,
                                      fontWeight: FontWeight.w800,
                                      letterSpacing: 0.4,
                                      height: 1.1,
                                      color: AppColors.foreground,
                                    ),
                                  ),
                                  SizedBox(height: 3.h),
                                  Row(
                                    children: [
                                      Icon(
                                        Icons.storefront_outlined,
                                        size: 10.r,
                                        color: AppColors.foregroundMuted,
                                      ),
                                      SizedBox(width: 4.w),
                                      Expanded(
                                        child: Text(
                                          bill.outletName,
                                          style: GoogleFonts.barlow(
                                            fontSize: 12.sp,
                                            fontWeight: FontWeight.w500,
                                            color: AppColors.foreground,
                                          ),
                                          maxLines: 1,
                                          overflow: TextOverflow.ellipsis,
                                        ),
                                      ),
                                    ],
                                  ),
                                ],
                              ),
                            ),
                            SizedBox(width: 8.w),
                            Text(
                              'Rs. ${bill.totalAmount.toStringAsFixed(2)}',
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 15.sp,
                                fontWeight: FontWeight.w800,
                                height: 1.1,
                                color: AppColors.amber,
                              ),
                            ),
                          ],
                        ),
                        SizedBox(height: 9.h),
                        Container(height: 1, color: AppColors.surfaceVariant),
                        SizedBox(height: 8.h),
                        // Date + dual status chips
                        Row(
                          children: [
                            Icon(
                              Icons.calendar_today_rounded,
                              size: 10.r,
                              color: AppColors.foregroundMuted,
                            ),
                            SizedBox(width: 4.w),
                            Text(
                              _formatDate(bill.billingDate),
                              style: GoogleFonts.barlow(
                                fontSize: 10.sp,
                                color: AppColors.foregroundMuted,
                              ),
                            ),
                            const Spacer(),
                            _StatusChip(status: bill.repStatus),
                            SizedBox(width: 4.w),
                            _StatusChip(
                              status: bill.distributorStatus,
                              isDistributor: true,
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                ),
                // Chevron
                Padding(
                  padding: EdgeInsets.only(right: 8.w),
                  child: Center(
                    child: Icon(
                      Icons.chevron_right_rounded,
                      size: 16.r,
                      color: AppColors.surfaceVariant,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _StatusChip extends StatelessWidget {
  final String status;
  final bool isDistributor;
  const _StatusChip({required this.status, this.isDistributor = false});

  @override
  Widget build(BuildContext context) {
    final (color, icon) = switch (status.toLowerCase()) {
      'approved'  => (AppColors.success, Icons.check_circle_rounded),
      'submitted' => (AppColors.primary, Icons.send_rounded),
      'rejected'  => (AppColors.error, Icons.cancel_rounded),
      'cancelled' => (AppColors.error, Icons.remove_circle_rounded),
      'pending'   => (AppColors.warning, Icons.pending_rounded),
      _           => (AppColors.foregroundMuted, Icons.help_outline_rounded),
    };

    return Container(
      padding: EdgeInsets.symmetric(horizontal: 6.w, vertical: 3.h),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(4.r),
        border: Border.all(color: color.withValues(alpha: 0.25)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (isDistributor) ...[
            Text(
              'DIST',
              style: GoogleFonts.barlowCondensed(
                fontSize: 8.sp,
                fontWeight: FontWeight.w800,
                letterSpacing: 0.8,
                color: color.withValues(alpha: 0.65),
              ),
            ),
            SizedBox(width: 3.w),
            Container(
              width: 1,
              height: 8.h,
              color: color.withValues(alpha: 0.3),
            ),
            SizedBox(width: 3.w),
          ],
          Icon(icon, size: 9.r, color: color),
          SizedBox(width: 3.w),
          Text(
            status,
            style: GoogleFonts.barlowCondensed(
              fontSize: 9.sp,
              fontWeight: FontWeight.w700,
              letterSpacing: 0.5,
              color: color,
            ),
          ),
        ],
      ),
    );
  }
}

// ── Empty state ───────────────────────────────────────────────────────────────

class _EmptyView extends StatelessWidget {
  final _FilterMode mode;
  const _EmptyView({required this.mode});

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
              'No bills found',
              style: GoogleFonts.barlowCondensed(
                fontSize: 18.sp,
                fontWeight: FontWeight.w700,
                color: AppColors.foreground,
              ),
            ),
            SizedBox(height: 6.h),
            Text(
              mode == _FilterMode.dateRange
                  ? 'No synced bills found for the selected date range.'
                  : 'No bill found with that bill number.',
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
              'Could not load bills',
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
