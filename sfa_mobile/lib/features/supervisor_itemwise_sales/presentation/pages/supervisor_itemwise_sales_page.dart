import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/core/widgets/app_spinner.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
import 'package:uswatte/features/supervisor_itemwise_sales/domain/entities/rep_itemwise_sales.dart';
import 'package:uswatte/features/supervisor_itemwise_sales/presentation/cubit/supervisor_itemwise_sales_cubit.dart';
import 'package:uswatte/features/supervisor_itemwise_sales/presentation/cubit/supervisor_itemwise_sales_state.dart';
import 'package:uswatte/features/supervisor_sales_summary/presentation/widgets/sales_report_filters.dart';

// Every Icons.* here must already be used by release 1.0.8+10 — a Shorebird
// patch cannot ship new glyphs of the tree-shaken icon font.

/// Packs are whole in practice; show decimals only when a line really has them.
String _fmtQty(double v) => v == v.roundToDouble()
    ? v.toInt().toString().replaceAllMapped(
        RegExp(r'(\d)(?=(\d{3})+$)'),
        (m) => '${m[1]},',
      )
    : v.toStringAsFixed(2);

class SupervisorItemwiseSalesPage extends StatelessWidget {
  const SupervisorItemwiseSalesPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF5F4EE),
      body: Column(
        children: [
          const SalesReportAppBar(
            title: 'ITEM-WISE SALES',
            subtitle: 'Rep sales per product over a date range',
            icon: Icons.inventory_2_rounded,
          ),
          Expanded(
            child:
                BlocBuilder<
                  SupervisorItemwiseSalesCubit,
                  SupervisorItemwiseSalesState
                >(
                  builder: (context, state) => switch (state) {
                    ItemwiseSalesLoadingReps() => const Center(
                      child: AppSpinner(),
                    ),
                    ItemwiseSalesRepsError(:final message) =>
                      SalesReportErrorBody(
                        message: message,
                        onRetry: () => context
                            .read<SupervisorItemwiseSalesCubit>()
                            .loadReps(),
                      ),
                    ItemwiseSalesReady() => _ReadyBody(state: state),
                  },
                ),
          ),
        ],
      ),
    );
  }
}

// ── Ready body ────────────────────────────────────────────────────────────────

class _ReadyBody extends StatefulWidget {
  final ItemwiseSalesReady state;
  const _ReadyBody({required this.state});

  @override
  State<_ReadyBody> createState() => _ReadyBodyState();
}

class _ReadyBodyState extends State<_ReadyBody> {
  final _search = TextEditingController();
  String _q = '';

  @override
  void didUpdateWidget(covariant _ReadyBody old) {
    super.didUpdateWidget(old);
    // New result set → start unfiltered.
    if (!identical(old.state.sales, widget.state.sales) && _q.isNotEmpty) {
      _search.clear();
      _q = '';
    }
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = widget.state;
    final cubit = context.read<SupervisorItemwiseSalesCubit>();
    final sales = state.sales;
    final q = _q.trim().toLowerCase();
    final items = sales == null
        ? const <ItemwiseSalesLine>[]
        : q.isEmpty
        ? sales.items
        : sales.items
              .where(
                (i) =>
                    i.itemName.toLowerCase().contains(q) ||
                    i.itemCode.toLowerCase().contains(q),
              )
              .toList();

    return ListView(
      padding: EdgeInsets.fromLTRB(16.w, 20.h, 16.w, 40.h),
      children: [
        StepCard(
          step: '01',
          label: 'SALES REP',
          icon: Icons.person_rounded,
          isComplete: state.selectedRep != null,
          child: SelectBox(
            icon: Icons.person_search_rounded,
            text: state.selectedRep?.userName ?? 'Select a sales rep...',
            filled: state.selectedRep != null,
            enabled: !state.isLoading,
            onTap: () async {
              final rep = await showModalBottomSheet<RepSummary>(
                context: context,
                isScrollControlled: true,
                backgroundColor: Colors.white,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.vertical(
                    top: Radius.circular(20.r),
                  ),
                ),
                builder: (_) => RepPickerSheet(
                  reps: state.reps,
                  selected: state.selectedRep,
                ),
              );
              if (rep != null) cubit.selectRep(rep);
            },
          ),
        ),
        const StepConnector(),
        StepCard(
          step: '02',
          label: 'DATE RANGE',
          icon: Icons.date_range_rounded,
          isComplete: true,
          child: DateRangeFilter(
            range: state.range,
            enabled: !state.isLoading,
            onChanged: cubit.selectRange,
          ),
        ),
        SizedBox(height: 24.h),
        ReportActionButton(
          enabled: state.canLoad,
          loading: state.isLoading,
          onTap: cubit.load,
          label: 'GET ITEM-WISE SALES',
          icon: Icons.inventory_2_rounded,
        ),
        if (state.error != null) ...[
          SizedBox(height: 16.h),
          ReportErrorBanner(message: state.error!, onRetry: cubit.load),
        ],
        if (sales != null) ...[
          SizedBox(height: 24.h),
          _HeroCard(
            sales: sales,
            repName: state.selectedRep?.userName ?? '',
            rangeLabel: fmtReportRange(
              DateTimeRange(start: sales.from, end: sales.to),
            ),
          ),
          SizedBox(height: 12.h),
          _TotalsCard(sales: sales),
          SizedBox(height: 16.h),
          if (sales.items.isEmpty)
            const _EmptyState(
              icon: Icons.inventory_2_outlined,
              message: 'No items billed in this period.',
            )
          else ...[
            _SearchField(
              controller: _search,
              onChanged: (v) => setState(() => _q = v),
              onClear: () => setState(() {
                _search.clear();
                _q = '';
              }),
            ),
            SizedBox(height: 10.h),
            Padding(
              padding: EdgeInsets.only(left: 4.w, bottom: 8.h),
              child: Text(
                q.isEmpty
                    ? '${sales.items.length} ITEMS · HIGHEST SALES FIRST'
                    : '${items.length} OF ${sales.items.length} ITEMS',
                style: GoogleFonts.barlowCondensed(
                  fontSize: 11.sp,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 1.5,
                  color: AppColors.foregroundMuted,
                ),
              ),
            ),
            if (items.isEmpty)
              const _EmptyState(
                icon: Icons.search_off_rounded,
                message: 'No items match your search.',
              )
            else
              for (final item in items) ...[
                _ItemCard(item: item),
                SizedBox(height: 10.h),
              ],
          ],
          SizedBox(height: 6.h),
          Text(
            'Covers approved + pending bills; rejected and cancelled bills '
            'are excluded. Quantities are in packs. Discount includes each '
            "item's share of bill discounts.",
            style: GoogleFonts.barlow(
              fontSize: 10.sp,
              color: AppColors.foregroundMuted,
            ),
          ),
        ],
      ],
    );
  }
}

// ── Hero ──────────────────────────────────────────────────────────────────────

class _HeroCard extends StatelessWidget {
  final RepItemwiseSales sales;
  final String repName;
  final String rangeLabel;

  const _HeroCard({
    required this.sales,
    required this.repName,
    required this.rangeLabel,
  });

  @override
  Widget build(BuildContext context) {
    final s = sales;
    Widget stat(String value, String label) => Expanded(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          FittedBox(
            fit: BoxFit.scaleDown,
            alignment: Alignment.centerLeft,
            child: Text(
              value,
              style: GoogleFonts.barlowCondensed(
                fontSize: 20.sp,
                fontWeight: FontWeight.w900,
                height: 1.0,
                color: Colors.white,
              ),
            ),
          ),
          SizedBox(height: 2.h),
          Text(
            label,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: GoogleFonts.barlowCondensed(
              fontSize: 10.sp,
              fontWeight: FontWeight.w700,
              letterSpacing: 1.2,
              color: Colors.white.withValues(alpha: 0.8),
            ),
          ),
        ],
      ),
    );

    return Container(
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [AppColors.primary, AppColors.primaryLight],
        ),
        borderRadius: BorderRadius.circular(16.r),
        boxShadow: [
          BoxShadow(
            color: AppColors.primary.withValues(alpha: 0.30),
            blurRadius: 20,
            offset: const Offset(0, 8),
          ),
        ],
      ),
      padding: EdgeInsets.all(20.r),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 4.h),
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: 0.18),
              borderRadius: BorderRadius.circular(20.r),
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(Icons.date_range_rounded, size: 11.r, color: Colors.white),
                SizedBox(width: 5.w),
                Text(
                  rangeLabel.toUpperCase(),
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 10.sp,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 1.2,
                    color: Colors.white,
                  ),
                ),
              ],
            ),
          ),
          SizedBox(height: 12.h),
          Text(
            repName,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: GoogleFonts.barlow(
              fontSize: 13.sp,
              color: Colors.white.withValues(alpha: 0.8),
            ),
          ),
          SizedBox(height: 2.h),
          Text(
            'NET SALES · AFTER DISCOUNT & RETURNS',
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: GoogleFonts.barlowCondensed(
              fontSize: 11.sp,
              fontWeight: FontWeight.w700,
              letterSpacing: 2.0,
              color: Colors.white.withValues(alpha: 0.75),
            ),
          ),
          FittedBox(
            fit: BoxFit.scaleDown,
            alignment: Alignment.centerLeft,
            child: Text(
              fmtLkr(s.totalNetValue),
              style: GoogleFonts.barlowCondensed(
                fontSize: 34.sp,
                fontWeight: FontWeight.w900,
                height: 1.05,
                letterSpacing: -0.5,
                color: Colors.white,
              ),
            ),
          ),
          SizedBox(height: 14.h),
          Container(
            padding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 10.h),
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: 0.14),
              borderRadius: BorderRadius.circular(10.r),
            ),
            child: Row(
              children: [
                stat('${s.items.length}', 'ITEMS'),
                stat(_fmtQty(s.totalSaleQty), 'PACKS SOLD'),
                stat(_fmtQty(s.totalFreeIssueQty), 'FREE ISSUE'),
                stat(
                  _fmtQty(s.totalGoodReturnQty + s.totalMarketReturnQty),
                  'RETURNED',
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ── Totals breakdown ──────────────────────────────────────────────────────────

class _TotalsCard extends StatelessWidget {
  final RepItemwiseSales sales;
  const _TotalsCard({required this.sales});

  @override
  Widget build(BuildContext context) {
    final s = sales;
    Widget row(String label, String value, {bool minus = false}) => Padding(
      padding: EdgeInsets.symmetric(vertical: 5.h),
      child: Row(
        children: [
          Expanded(
            child: Text(
              label,
              style: GoogleFonts.barlowCondensed(
                fontSize: 12.sp,
                fontWeight: FontWeight.w700,
                letterSpacing: 1.0,
                color: AppColors.foregroundMuted,
              ),
            ),
          ),
          Text(
            '${minus ? '−' : ''}$value',
            style: GoogleFonts.barlowCondensed(
              fontSize: 15.sp,
              fontWeight: FontWeight.w700,
              color: AppColors.foreground,
            ),
          ),
        ],
      ),
    );

    return Container(
      padding: EdgeInsets.fromLTRB(16.w, 10.h, 16.w, 10.h),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16.r),
        border: Border.all(color: AppColors.primary.withValues(alpha: 0.18)),
      ),
      child: Column(
        children: [
          row('GROSS SALES · ITEMS SOLD', fmtLkr(s.totalGrossValue)),
          row('DISCOUNT', fmtLkr(s.totalDiscount), minus: true),
          row('GOOD RETURN', fmtLkr(s.totalGoodReturnValue), minus: true),
          row('MARKET RETURN', fmtLkr(s.totalMarketReturnValue), minus: true),
        ],
      ),
    );
  }
}

// ── Search ────────────────────────────────────────────────────────────────────

class _SearchField extends StatelessWidget {
  final TextEditingController controller;
  final ValueChanged<String> onChanged;
  final VoidCallback onClear;

  const _SearchField({
    required this.controller,
    required this.onChanged,
    required this.onClear,
  });

  @override
  Widget build(BuildContext context) {
    return TextField(
      controller: controller,
      onChanged: onChanged,
      style: GoogleFonts.barlow(fontSize: 14.sp),
      decoration: InputDecoration(
        hintText: 'Search item name or code',
        prefixIcon: Icon(Icons.search_rounded, size: 18.r),
        suffixIcon: controller.text.isEmpty
            ? null
            : IconButton(
                icon: Icon(Icons.close_rounded, size: 18.r),
                onPressed: onClear,
              ),
        isDense: true,
        filled: true,
        fillColor: Colors.white,
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12.r),
          borderSide: const BorderSide(color: Color(0xFFE5E4DC)),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12.r),
          borderSide: const BorderSide(color: Color(0xFFE5E4DC)),
        ),
      ),
    );
  }
}

// ── Item card ─────────────────────────────────────────────────────────────────

class _ItemCard extends StatelessWidget {
  final ItemwiseSalesLine item;
  const _ItemCard({required this.item});

  @override
  Widget build(BuildContext context) {
    final i = item;

    Widget qty(String label, double v, {bool highlight = false}) => Expanded(
      child: Column(
        children: [
          Text(
            _fmtQty(v),
            style: GoogleFonts.barlowCondensed(
              fontSize: 17.sp,
              fontWeight: FontWeight.w800,
              height: 1.0,
              color: highlight
                  ? AppColors.primaryDark
                  : v == 0
                  ? AppColors.foregroundMuted
                  : AppColors.foreground,
            ),
          ),
          SizedBox(height: 3.h),
          Text(
            label,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: GoogleFonts.barlowCondensed(
              fontSize: 9.sp,
              fontWeight: FontWeight.w700,
              letterSpacing: 1.0,
              color: AppColors.foregroundMuted,
            ),
          ),
        ],
      ),
    );

    Widget money(String label, double v, {bool minus = false}) => Row(
      children: [
        Expanded(
          child: Text(
            label,
            style: GoogleFonts.barlowCondensed(
              fontSize: 11.sp,
              fontWeight: FontWeight.w700,
              letterSpacing: 0.8,
              color: AppColors.foregroundMuted,
            ),
          ),
        ),
        Text(
          '${minus && v != 0 ? '−' : ''}${fmtLkr(v)}',
          style: GoogleFonts.barlowCondensed(
            fontSize: 13.sp,
            fontWeight: FontWeight.w600,
            color: AppColors.foreground,
          ),
        ),
      ],
    );

    return Container(
      padding: EdgeInsets.all(14.r),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14.r),
        boxShadow: [
          BoxShadow(
            color: const Color(0xFF1A1A11).withValues(alpha: 0.05),
            blurRadius: 12,
            offset: const Offset(0, 3),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      i.itemName,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: GoogleFonts.barlow(
                        fontSize: 14.sp,
                        fontWeight: FontWeight.w700,
                        color: AppColors.foreground,
                      ),
                    ),
                    SizedBox(height: 2.h),
                    Text(
                      i.itemCode,
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 11.sp,
                        fontWeight: FontWeight.w600,
                        letterSpacing: 0.8,
                        color: AppColors.foregroundMuted,
                      ),
                    ),
                  ],
                ),
              ),
              SizedBox(width: 10.w),
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Text(
                    fmtLkr(i.netValue),
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 17.sp,
                      fontWeight: FontWeight.w900,
                      color: AppColors.primaryDark,
                    ),
                  ),
                  Text(
                    'NET',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 9.sp,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 1.2,
                      color: AppColors.foregroundMuted,
                    ),
                  ),
                ],
              ),
            ],
          ),
          SizedBox(height: 12.h),
          Container(
            padding: EdgeInsets.symmetric(vertical: 9.h),
            decoration: BoxDecoration(
              color: AppColors.primary.withValues(alpha: 0.05),
              borderRadius: BorderRadius.circular(10.r),
            ),
            child: Row(
              children: [
                qty('SOLD PKS', i.saleQty, highlight: true),
                qty('FREE', i.freeIssueQty),
                qty('GOOD RTN', i.goodReturnQty),
                qty('MKT RTN', i.marketReturnQty),
              ],
            ),
          ),
          SizedBox(height: 10.h),
          money('GROSS', i.grossValue),
          SizedBox(height: 3.h),
          money('DISCOUNT', i.discount, minus: true),
          SizedBox(height: 3.h),
          money('RETURNS', i.returnValue, minus: true),
        ],
      ),
    );
  }
}

class _EmptyState extends StatelessWidget {
  final IconData icon;
  final String message;
  const _EmptyState({required this.icon, required this.message});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.symmetric(vertical: 28.h),
      child: Column(
        children: [
          Icon(icon, size: 36.r, color: AppColors.foregroundMuted),
          SizedBox(height: 10.h),
          Text(
            message,
            textAlign: TextAlign.center,
            style: GoogleFonts.barlow(
              fontSize: 13.sp,
              color: AppColors.foregroundMuted,
            ),
          ),
        ],
      ),
    );
  }
}
