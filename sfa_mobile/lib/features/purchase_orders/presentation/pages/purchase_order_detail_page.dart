import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/purchase_orders/domain/entities/purchase_order_detail.dart';
import 'package:uswatte/features/purchase_orders/presentation/bloc/purchase_orders_bloc.dart';
import 'package:uswatte/features/purchase_orders/presentation/bloc/purchase_orders_event.dart';
import 'package:uswatte/features/purchase_orders/presentation/bloc/purchase_orders_state.dart';
import 'package:uswatte/features/purchase_orders/presentation/pages/purchase_orders_list_page.dart' show ApprovalMode;

// ── Status helpers ────────────────────────────────────────────────────────────

const _statusLabels = {
  0: 'DRAFT',
  1: 'PENDING REP APPROVAL',
  2: 'PENDING MANAGER APPROVAL',
  3: 'PENDING FINALIZATION',
  4: 'FINALIZED',
  5: 'CANCELLED',
  6: 'PENDING ACKNOWLEDGEMENT',
};

const _statusColors = {
  0: Color(0xFF94A3B8),
  1: Color(0xFFF59E0B),
  2: Color(0xFF3B82F6),
  3: Color(0xFF8B5CF6),
  4: Color(0xFF22C55E),
  5: Color(0xFFEF4444),
  6: Color(0xFF06B6D4),
};

// ── Page ──────────────────────────────────────────────────────────────────────

class PurchaseOrderDetailPage extends StatelessWidget {
  final ApprovalMode approvalMode;
  const PurchaseOrderDetailPage({
    super.key,
    this.approvalMode = ApprovalMode.salesRep,
  });

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    return BlocConsumer<PurchaseOrdersBloc, PurchaseOrdersState>(
      listener: (context, state) {
        if (state is PurchaseOrderActionSuccess) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Row(
                children: [
                  Icon(Icons.check_circle_outline_rounded,
                      color: Colors.white, size: 18.r),
                  SizedBox(width: 10.w),
                  Text(state.message,
                      style: GoogleFonts.barlow(
                          fontSize: 13.sp, fontWeight: FontWeight.w600)),
                ],
              ),
              backgroundColor: const Color(0xFF22C55E),
              behavior: SnackBarBehavior.floating,
              shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(10.r)),
              margin: EdgeInsets.all(16.r),
            ),
          );
          if (context.canPop()) context.pop();
        }
        if (state is PurchaseOrdersError) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(state.message,
                  style: GoogleFonts.barlow(fontSize: 13.sp)),
              backgroundColor: const Color(0xFFEF4444),
              behavior: SnackBarBehavior.floating,
              shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(10.r)),
              margin: EdgeInsets.all(16.r),
            ),
          );
        }
      },
      builder: (context, state) {
        PurchaseOrderDetail? order;
        bool isActionInProgress = false;

        if (state is PurchaseOrderDetailLoaded) {
          order = state.order;
        } else if (state is PurchaseOrderActionInProgress) {
          order = state.order;
          isActionInProgress = true;
        }

        if (order == null) {
          return const Scaffold(
            backgroundColor: Color(0xFFF8F7F5),
            body: Center(
              child: CircularProgressIndicator(color: AppColors.primary),
            ),
          );
        }

        return Scaffold(
          backgroundColor: const Color(0xFFF8F7F5),
          body: _DetailView(
            order: order,
            approvalMode: approvalMode,
            isActionInProgress: isActionInProgress,
          ),
        );
      },
    );
  }
}

// ── Full detail view ──────────────────────────────────────────────────────────

class _DetailView extends StatelessWidget {
  final PurchaseOrderDetail order;
  final ApprovalMode approvalMode;
  final bool isActionInProgress;
  const _DetailView({
    required this.order,
    required this.approvalMode,
    required this.isActionInProgress,
  });

  @override
  Widget build(BuildContext context) {
    return CustomScrollView(
      slivers: [
        _SliverHeader(order: order),
        SliverToBoxAdapter(
          child: _MetaGrid(order: order),
        ),
        SliverToBoxAdapter(
          child: _ItemsSection(order: order),
        ),
        if (order.history.isNotEmpty)
          SliverToBoxAdapter(
            child: _HistorySection(order: order),
          ),
        SliverToBoxAdapter(
          child: _ActionSection(
            order: order,
            approvalMode: approvalMode,
            isActionInProgress: isActionInProgress,
          ),
        ),
        SliverToBoxAdapter(child: SizedBox(height: 40.h)),
      ],
    );
  }
}

// ── Sliver header ─────────────────────────────────────────────────────────────

class _SliverHeader extends StatelessWidget {
  final PurchaseOrderDetail order;
  const _SliverHeader({required this.order});

  @override
  Widget build(BuildContext context) {
    final statusColor =
        _statusColors[order.status] ?? const Color(0xFF94A3B8);
    final statusLabel =
        _statusLabels[order.status] ?? 'UNKNOWN';

    return SliverAppBar(
      expandedHeight: 180.h,
      pinned: true,
      backgroundColor: AppColors.primaryDark,
      leading: GestureDetector(
        onTap: () => context.canPop() ? context.pop() : null,
        child: Container(
          margin: EdgeInsets.all(8.r),
          decoration: BoxDecoration(
            color: Colors.white.withValues(alpha: 0.15),
            borderRadius: BorderRadius.circular(10.r),
            border:
                Border.all(color: Colors.white.withValues(alpha: 0.25)),
          ),
          child: Icon(Icons.arrow_back_ios_new_rounded,
              size: 15.r, color: Colors.white),
        ),
      ),
      flexibleSpace: FlexibleSpaceBar(
        collapseMode: CollapseMode.pin,
        background: Container(
          decoration: const BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [
                Color(0xFF7C2D12),
                AppColors.primaryDark,
                AppColors.primary,
              ],
            ),
          ),
          child: Stack(
            children: [
              // Decorative circle
              Positioned(
                right: -30.w,
                top: -30.h,
                child: Container(
                  width: 180.r,
                  height: 180.r,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    color: Colors.white.withValues(alpha: 0.05),
                  ),
                ),
              ),
              Positioned(
                right: 50.w,
                bottom: 10.h,
                child: Container(
                  width: 80.r,
                  height: 80.r,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    color: Colors.white.withValues(alpha: 0.04),
                  ),
                ),
              ),
              // Content
              SafeArea(
                child: Padding(
                  padding:
                      EdgeInsets.fromLTRB(20.w, 48.h, 20.w, 20.h),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisAlignment: MainAxisAlignment.end,
                    children: [
                      // Status chip
                      Container(
                        padding: EdgeInsets.symmetric(
                            horizontal: 10.w, vertical: 4.h),
                        decoration: BoxDecoration(
                          color: statusColor.withValues(alpha: 0.22),
                          borderRadius: BorderRadius.circular(4.r),
                          border: Border.all(
                              color: statusColor.withValues(alpha: 0.5),
                              width: 0.8),
                        ),
                        child: Text(
                          statusLabel,
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 9.sp,
                            fontWeight: FontWeight.w800,
                            letterSpacing: 2.0,
                            color: statusColor,
                          ),
                        ),
                      ),
                      SizedBox(height: 8.h),
                      // Order number — big display
                      Text(
                        order.orderNumber,
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 34.sp,
                          fontWeight: FontWeight.w900,
                          letterSpacing: 1.5,
                          height: 1.0,
                          color: Colors.white,
                        ),
                      ),
                      SizedBox(height: 4.h),
                      // Distributor
                      Row(
                        children: [
                          Icon(Icons.storefront_outlined,
                              size: 12.r,
                              color:
                                  Colors.white.withValues(alpha: 0.65)),
                          SizedBox(width: 5.w),
                          Text(
                            order.distributorName,
                            style: GoogleFonts.barlow(
                              fontSize: 12.sp,
                              fontWeight: FontWeight.w500,
                              color: Colors.white.withValues(alpha: 0.75),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
      title: Text(
        order.orderNumber,
        style: GoogleFonts.barlowCondensed(
          fontSize: 16.sp,
          fontWeight: FontWeight.w800,
          letterSpacing: 1.0,
          color: Colors.white,
        ),
      ),
    );
  }
}

// ── Meta grid ────────────────────────────────────────────────────────────────

class _MetaGrid extends StatelessWidget {
  final PurchaseOrderDetail order;
  const _MetaGrid({required this.order});

  String _fmt(DateTime? dt) {
    if (dt == null) return '—';
    return '${dt.day.toString().padLeft(2, '0')}/'
        '${dt.month.toString().padLeft(2, '0')}/'
        '${dt.year}';
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.fromLTRB(16.w, 20.h, 16.w, 0),
      child: Container(
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16.r),
          boxShadow: [
            BoxShadow(
              color: const Color(0xFF1C1917).withValues(alpha: 0.06),
              blurRadius: 16,
              offset: const Offset(0, 4),
            ),
          ],
        ),
        child: Column(
          children: [
            _MetaRow(
              left: _MetaCell(
                label: 'ORDER NO.',
                value: order.orderNumber,
                icon: Icons.tag_rounded,
              ),
              right: _MetaCell(
                label: 'SUBMITTED',
                value: _fmt(order.submittedAt),
                icon: Icons.upload_rounded,
              ),
            ),
            Divider(
                height: 1,
                color: const Color(0xFF1C1917).withValues(alpha: 0.06)),
            _MetaRow(
              left: _MetaCell(
                label: 'CREATED',
                value: _fmt(order.createdAt),
                icon: Icons.calendar_today_outlined,
              ),
              right: _MetaCell(
                label: 'TOTAL AMOUNT',
                value: 'LKR ${order.totalAmount.toStringAsFixed(2)}',
                icon: Icons.payments_outlined,
                valueColor: AppColors.primary,
              ),
            ),
            if (order.notes != null && order.notes!.isNotEmpty) ...[
              Divider(
                  height: 1,
                  color: const Color(0xFF1C1917).withValues(alpha: 0.06)),
              Padding(
                padding:
                    EdgeInsets.symmetric(horizontal: 16.w, vertical: 12.h),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Icon(Icons.notes_rounded,
                        size: 14.r,
                        color: AppColors.foregroundMuted),
                    SizedBox(width: 8.w),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('NOTES',
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 9.sp,
                                fontWeight: FontWeight.w700,
                                letterSpacing: 1.5,
                                color: AppColors.foregroundMuted,
                              )),
                          SizedBox(height: 3.h),
                          Text(order.notes!,
                              style: GoogleFonts.barlow(
                                  fontSize: 12.sp,
                                  color: AppColors.foreground)),
                        ],
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

class _MetaRow extends StatelessWidget {
  final Widget left, right;
  const _MetaRow({required this.left, required this.right});

  @override
  Widget build(BuildContext context) {
    return IntrinsicHeight(
      child: Row(
        children: [
          Expanded(child: left),
          VerticalDivider(
              width: 1,
              color: const Color(0xFF1C1917).withValues(alpha: 0.06)),
          Expanded(child: right),
        ],
      ),
    );
  }
}

class _MetaCell extends StatelessWidget {
  final String label, value;
  final IconData icon;
  final Color? valueColor;
  const _MetaCell({
    required this.label,
    required this.value,
    required this.icon,
    this.valueColor,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 14.h),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 28.r,
            height: 28.r,
            decoration: BoxDecoration(
              color: AppColors.primary.withValues(alpha: 0.08),
              borderRadius: BorderRadius.circular(7.r),
            ),
            child: Icon(icon,
                size: 14.r, color: AppColors.primary),
          ),
          SizedBox(width: 10.w),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(label,
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 9.sp,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 1.5,
                      color: AppColors.foregroundMuted,
                    )),
                SizedBox(height: 2.h),
                Text(value,
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 13.sp,
                      fontWeight: FontWeight.w800,
                      letterSpacing: 0.2,
                      height: 1.2,
                      color: valueColor ?? AppColors.foreground,
                    )),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ── Items section ─────────────────────────────────────────────────────────────

class _ItemsSection extends StatefulWidget {
  final PurchaseOrderDetail order;
  const _ItemsSection({required this.order});

  @override
  State<_ItemsSection> createState() => _ItemsSectionState();
}

class _ItemsSectionState extends State<_ItemsSection> {
  final _scrollController = ScrollController();

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _SectionHeader(
            icon: Icons.inventory_2_outlined,
            title: 'LINE ITEMS',
            badge: '${widget.order.items.length}',
          ),
          SizedBox(height: 10.h),
          Container(
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(16.r),
              boxShadow: [
                BoxShadow(
                  color: const Color(0xFF1C1917).withValues(alpha: 0.06),
                  blurRadius: 16,
                  offset: const Offset(0, 4),
                ),
              ],
            ),
            child: ClipRRect(
              borderRadius: BorderRadius.circular(16.r),
              child: Column(
                children: [
                  // Horizontal scroll table with visible scrollbar
                  Scrollbar(
                    controller: _scrollController,
                    thumbVisibility: true,
                    thickness: 4,
                    radius: const Radius.circular(4),
                    child: SingleChildScrollView(
                      controller: _scrollController,
                      scrollDirection: Axis.horizontal,
                      padding: EdgeInsets.only(bottom: 10.h),
                      child: _ItemsTable(order: widget.order),
                    ),
                  ),
                  // Total row — always full width, outside scroll
                  Container(
                    color: const Color(0xFF1C1917).withValues(alpha: 0.03),
                    padding: EdgeInsets.symmetric(
                        horizontal: 16.w, vertical: 12.h),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(
                          'ORDER TOTAL',
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 11.sp,
                            fontWeight: FontWeight.w800,
                            letterSpacing: 1.5,
                            color: AppColors.foregroundMuted,
                          ),
                        ),
                        Container(
                          padding: EdgeInsets.symmetric(
                              horizontal: 12.w, vertical: 5.h),
                          decoration: BoxDecoration(
                            color: AppColors.primary,
                            borderRadius: BorderRadius.circular(8.r),
                          ),
                          child: Text(
                            'LKR ${widget.order.totalAmount.toStringAsFixed(2)}',
                            style: GoogleFonts.barlowCondensed(
                              fontSize: 15.sp,
                              fontWeight: FontWeight.w900,
                              letterSpacing: 0.5,
                              color: Colors.white,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _ItemsTable extends StatelessWidget {
  final PurchaseOrderDetail order;
  const _ItemsTable({required this.order});

  // Fixed column widths for the scrollable table
  static const double _wCode = 72.0;
  static const double _wDesc = 160.0;
  static const double _wQty = 52.0;
  static const double _wPrice = 88.0;
  static const double _wTotal = 90.0;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Header row
        Container(
          color: AppColors.primaryDark.withValues(alpha: 0.06),
          child: Padding(
            padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 10.h),
            child: Row(
              children: [
                SizedBox(width: _wCode.w, child: _ColHeader('CODE')),
                SizedBox(width: _wDesc.w, child: _ColHeader('DESCRIPTION')),
                SizedBox(width: _wQty.w, child: _ColHeader('QTY', align: TextAlign.center)),
                SizedBox(width: _wPrice.w, child: _ColHeader('UNIT PRICE', align: TextAlign.right)),
                SizedBox(width: _wTotal.w, child: _ColHeader('TOTAL', align: TextAlign.right)),
              ],
            ),
          ),
        ),
        // Data rows
        ...List.generate(order.items.length, (i) {
          final item = order.items[i];
          final isEven = i % 2 == 0;
          return Container(
            color: isEven
                ? Colors.transparent
                : const Color(0xFF1C1917).withValues(alpha: 0.02),
            child: Padding(
              padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 10.h),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  SizedBox(
                    width: _wCode.w,
                    child: Text(
                      item.productCode,
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 11.sp,
                        fontWeight: FontWeight.w800,
                        letterSpacing: 0.5,
                        color: AppColors.primary,
                      ),
                    ),
                  ),
                  SizedBox(
                    width: _wDesc.w,
                    child: Text(
                      item.productDescription,
                      style: GoogleFonts.barlow(
                          fontSize: 11.sp, color: AppColors.foreground),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  SizedBox(
                    width: _wQty.w,
                    child: Text(
                      item.quantity.toString(),
                      textAlign: TextAlign.center,
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 13.sp,
                        fontWeight: FontWeight.w700,
                        color: AppColors.foreground,
                      ),
                    ),
                  ),
                  SizedBox(
                    width: _wPrice.w,
                    child: Text(
                      item.unitPrice.toStringAsFixed(2),
                      textAlign: TextAlign.right,
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 12.sp,
                        fontWeight: FontWeight.w600,
                        color: AppColors.foregroundMuted,
                      ),
                    ),
                  ),
                  SizedBox(
                    width: _wTotal.w,
                    child: Text(
                      item.lineTotal.toStringAsFixed(2),
                      textAlign: TextAlign.right,
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 12.sp,
                        fontWeight: FontWeight.w800,
                        color: AppColors.foreground,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          );
        }),
        Divider(height: 1, color: const Color(0xFF1C1917).withValues(alpha: 0.08)),
      ],
    );
  }
}

class _ColHeader extends StatelessWidget {
  final String text;
  final TextAlign align;
  const _ColHeader(this.text, {this.align = TextAlign.left});

  @override
  Widget build(BuildContext context) {
    return Text(
      text,
      textAlign: align,
      style: GoogleFonts.barlowCondensed(
        fontSize: 9.sp,
        fontWeight: FontWeight.w800,
        letterSpacing: 1.8,
        color: AppColors.foregroundMuted,
      ),
    );
  }
}

// ── History section ───────────────────────────────────────────────────────────

class _HistorySection extends StatefulWidget {
  final PurchaseOrderDetail order;
  const _HistorySection({required this.order});

  @override
  State<_HistorySection> createState() => _HistorySectionState();
}

class _HistorySectionState extends State<_HistorySection> {
  bool _expanded = false;

  String _fmt(DateTime? dt) {
    if (dt == null) return '—';
    return '${dt.day.toString().padLeft(2, '0')}/'
        '${dt.month.toString().padLeft(2, '0')}/'
        '${dt.year}';
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Tappable header — toggles expansion
          GestureDetector(
            onTap: () => setState(() => _expanded = !_expanded),
            behavior: HitTestBehavior.opaque,
            child: Row(
              children: [
                _SectionHeader(
                  icon: Icons.history_rounded,
                  title: 'AUDIT TRAIL',
                  badge: '${widget.order.history.length}',
                ),
                const Spacer(),
                AnimatedRotation(
                  turns: _expanded ? 0.5 : 0.0,
                  duration: const Duration(milliseconds: 200),
                  child: Container(
                    width: 24.r,
                    height: 24.r,
                    decoration: BoxDecoration(
                      color: AppColors.foregroundMuted.withValues(alpha: 0.08),
                      borderRadius: BorderRadius.circular(6.r),
                    ),
                    child: Icon(
                      Icons.keyboard_arrow_down_rounded,
                      size: 16.r,
                      color: AppColors.foregroundMuted,
                    ),
                  ),
                ),
              ],
            ),
          ),
          // Use AnimatedAlign+ClipRect (same as ExpansionTile internally) to
          // avoid AnimatedCrossFade's size-measurement issues with IntrinsicHeight.
          ClipRect(
            child: AnimatedAlign(
              alignment: Alignment.topCenter,
              heightFactor: _expanded ? 1.0 : 0.0,
              duration: const Duration(milliseconds: 220),
              curve: Curves.easeInOut,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  SizedBox(height: 10.h),
                  Container(
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(16.r),
                      boxShadow: [
                        BoxShadow(
                          color:
                              const Color(0xFF1C1917).withValues(alpha: 0.06),
                          blurRadius: 16,
                          offset: const Offset(0, 4),
                        ),
                      ],
                    ),
                    padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 8.h),
                    child: Column(
                      children:
                          List.generate(widget.order.history.length, (i) {
                        final entry = widget.order.history[i];
                        final isLast = i == widget.order.history.length - 1;
                        return _TimelineEntry(
                          entry: entry,
                          isLast: isLast,
                          formatDate: _fmt,
                          orderItems: widget.order.items,
                        );
                      }),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _TimelineEntry extends StatelessWidget {
  final PurchaseOrderHistoryEntry entry;
  final bool isLast;
  final String Function(DateTime?) formatDate;
  final List<PurchaseOrderItem> orderItems;
  const _TimelineEntry({
    required this.entry,
    required this.isLast,
    required this.formatDate,
    required this.orderItems,
  });

  Color get _dotColor {
    final a = entry.action.toLowerCase();
    if (a.contains('cancel') || a.contains('reject')) return const Color(0xFFEF4444);
    if (a.contains('finalize') || a.contains('approved') || a.contains('approve')) {
      return const Color(0xFF22C55E);
    }
    if (a.contains('submit')) return AppColors.primary;
    return const Color(0xFF94A3B8);
  }

  @override
  Widget build(BuildContext context) {
    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Timeline rail
          SizedBox(
            width: 28.w,
            child: Column(
              children: [
                Container(
                  width: 10.r,
                  height: 10.r,
                  decoration: BoxDecoration(
                    color: _dotColor,
                    shape: BoxShape.circle,
                    boxShadow: [
                      BoxShadow(
                        color: _dotColor.withValues(alpha: 0.4),
                        blurRadius: 6,
                        spreadRadius: 1,
                      ),
                    ],
                  ),
                ),
                if (!isLast)
                  Expanded(
                    child: Container(
                      width: 1.5.w,
                      margin: EdgeInsets.only(top: 4.h),
                      decoration: BoxDecoration(
                        gradient: LinearGradient(
                          begin: Alignment.topCenter,
                          end: Alignment.bottomCenter,
                          colors: [
                            _dotColor.withValues(alpha: 0.4),
                            const Color(0xFF94A3B8).withValues(alpha: 0.2),
                          ],
                        ),
                      ),
                    ),
                  ),
              ],
            ),
          ),
          SizedBox(width: 10.w),
          // Entry content
          Expanded(
            child: Padding(
              padding: EdgeInsets.only(bottom: isLast ? 0 : 16.h),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    _formatAction(entry.action),
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 13.sp,
                      fontWeight: FontWeight.w800,
                      letterSpacing: 0.5,
                      color: AppColors.foreground,
                    ),
                  ),
                  SizedBox(height: 2.h),
                  Row(
                    children: [
                      if (entry.performedByName != null) ...[
                        Icon(Icons.person_outline_rounded,
                            size: 11.r, color: AppColors.foregroundMuted),
                        SizedBox(width: 3.w),
                        Text(
                          entry.performedByName!,
                          style: GoogleFonts.barlow(
                              fontSize: 11.sp, color: AppColors.foregroundMuted),
                        ),
                        Container(
                          width: 3.r,
                          height: 3.r,
                          margin: EdgeInsets.symmetric(horizontal: 6.w),
                          decoration: BoxDecoration(
                            color: AppColors.foregroundMuted.withValues(alpha: 0.4),
                            shape: BoxShape.circle,
                          ),
                        ),
                      ],
                      Icon(Icons.schedule_rounded,
                          size: 11.r, color: AppColors.foregroundMuted),
                      SizedBox(width: 3.w),
                      Text(
                        formatDate(entry.performedAt),
                        style: GoogleFonts.barlow(
                            fontSize: 11.sp, color: AppColors.foregroundMuted),
                      ),
                    ],
                  ),
                  if (entry.notes != null && entry.notes!.isNotEmpty) ...[
                    SizedBox(height: 6.h),
                    _buildNotes(entry.notes!),
                  ],
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildNotes(String notes) {
    // Items Edited → parse the AFTER snapshot (itemsSnapshot) so we show
    // the new state, not the old state that was stored in notes.
    if (entry.action.toLowerCase().contains('edit')) {
      final source = entry.itemsSnapshot ?? notes;
      try {
        final parsed = jsonDecode(source);
        if (parsed is List) {
          return _ItemsChangedCard(
            items: parsed.cast<Map<String, dynamic>>(),
            orderItems: orderItems,
          );
        }
      } catch (_) {}
    }
    // Generic notes (rejection reason, etc.)
    return Container(
      padding: EdgeInsets.fromLTRB(10.w, 8.h, 10.w, 8.h),
      decoration: BoxDecoration(
        color: const Color(0xFFF59E0B).withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(8.r),
        border: Border.all(
          color: const Color(0xFFF59E0B).withValues(alpha: 0.25),
        ),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(Icons.format_quote_rounded,
              size: 13.r, color: const Color(0xFFF59E0B)),
          SizedBox(width: 6.w),
          Expanded(
            child: Text(
              notes,
              style: GoogleFonts.barlow(
                  fontSize: 12.sp, color: AppColors.foreground),
            ),
          ),
        ],
      ),
    );
  }

  String _formatAction(String raw) {
    return raw.replaceAllMapped(
      RegExp(r'(?<=[a-z])(?=[A-Z])'),
      (m) => ' ',
    );
  }
}

// ── Items-changed card (shown for "Items Edited" history entries) ──────────────

class _ItemsChangedCard extends StatelessWidget {
  final List<Map<String, dynamic>> items;
  final List<PurchaseOrderItem> orderItems;
  const _ItemsChangedCard({required this.items, required this.orderItems});

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: AppColors.primary.withValues(alpha: 0.04),
        borderRadius: BorderRadius.circular(10.r),
        border: Border.all(color: AppColors.primary.withValues(alpha: 0.18)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Card header
          Padding(
            padding: EdgeInsets.fromLTRB(10.w, 8.h, 10.w, 7.h),
            child: Row(
              children: [
                Icon(Icons.edit_note_rounded,
                    size: 13.r, color: AppColors.primary),
                SizedBox(width: 5.w),
                Text(
                  '${items.length} ITEM${items.length == 1 ? '' : 'S'} UPDATED',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 9.sp,
                    fontWeight: FontWeight.w800,
                    letterSpacing: 1.5,
                    color: AppColors.primary,
                  ),
                ),
              ],
            ),
          ),
          Container(
              height: 1, color: AppColors.primary.withValues(alpha: 0.1)),
          // One row per item
          ...items.asMap().entries.map((e) {
            final idx = e.key;
            final item = e.value;
            final isLast = idx == items.length - 1;
            // camelCase keys from new snapshots; PascalCase from old ones
            final productCode = item['productCode'] as String?
                ?? item['ProductCode'] as String?;
            final productId = item['productId'] as int?
                ?? item['ProductId'] as int?
                ?? 0;
            // For old snapshots without productCode, look it up from the
            // order's current items list (codes are stable on the product entity)
            final lookedUp = orderItems
                .where((o) => o.productId == productId)
                .map((o) => o.productCode)
                .firstOrNull;
            final codeLabel = (productCode != null && productCode.isNotEmpty)
                ? productCode
                : (lookedUp != null && lookedUp.isNotEmpty)
                    ? lookedUp
                    : 'PID $productId';
            final qty = item['quantity'] ?? item['Quantity'] ?? 0;
            final price =
                ((item['unitPrice'] ?? item['UnitPrice']) as num?)?.toDouble() ?? 0.0;
            final discount =
                ((item['discount'] ?? item['Discount']) as num?)?.toDouble() ?? 0.0;
            return Container(
              padding:
                  EdgeInsets.symmetric(horizontal: 10.w, vertical: 8.h),
              decoration: isLast
                  ? null
                  : BoxDecoration(
                      border: Border(
                        bottom: BorderSide(
                          color:
                              AppColors.primary.withValues(alpha: 0.07),
                        ),
                      ),
                    ),
              child: Row(
                children: [
                  // Product code badge
                  Container(
                    padding: EdgeInsets.symmetric(
                        horizontal: 6.w, vertical: 2.h),
                    decoration: BoxDecoration(
                      color: AppColors.primary.withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(4.r),
                    ),
                    child: Text(
                      codeLabel,
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 9.sp,
                        fontWeight: FontWeight.w900,
                        letterSpacing: 0.5,
                        color: AppColors.primary,
                      ),
                    ),
                  ),
                  SizedBox(width: 8.w),
                  _InfoChip(label: 'QTY', value: '$qty'),
                  SizedBox(width: 5.w),
                  _InfoChip(
                      label: 'PRICE',
                      value: 'LKR ${price.toStringAsFixed(2)}'),
                  if (discount > 0) ...[
                    SizedBox(width: 5.w),
                    _InfoChip(
                      label: 'DISC',
                      value: '${discount.toStringAsFixed(1)}%',
                      accent: true,
                    ),
                  ],
                ],
              ),
            );
          }),
        ],
      ),
    );
  }
}

class _InfoChip extends StatelessWidget {
  final String label, value;
  final bool accent;
  const _InfoChip(
      {required this.label, required this.value, this.accent = false});

  @override
  Widget build(BuildContext context) {
    final valueColor =
        accent ? const Color(0xFF3B82F6) : AppColors.foreground;
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 6.w, vertical: 3.h),
      decoration: BoxDecoration(
        color: accent
            ? const Color(0xFF3B82F6).withValues(alpha: 0.08)
            : const Color(0xFF1C1917).withValues(alpha: 0.05),
        borderRadius: BorderRadius.circular(4.r),
      ),
      child: RichText(
        text: TextSpan(
          children: [
            TextSpan(
              text: '$label ',
              style: GoogleFonts.barlowCondensed(
                fontSize: 8.sp,
                fontWeight: FontWeight.w700,
                letterSpacing: 0.8,
                color: AppColors.foregroundMuted,
              ),
            ),
            TextSpan(
              text: value,
              style: GoogleFonts.barlowCondensed(
                fontSize: 10.sp,
                fontWeight: FontWeight.w900,
                color: valueColor,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Action section ────────────────────────────────────────────────────────────

class _ActionSection extends StatefulWidget {
  final PurchaseOrderDetail order;
  final ApprovalMode approvalMode;
  final bool isActionInProgress;
  const _ActionSection({
    required this.order,
    required this.approvalMode,
    required this.isActionInProgress,
  });

  @override
  State<_ActionSection> createState() => _ActionSectionState();
}

class _ActionSectionState extends State<_ActionSection> {
  bool _showRejectField = false;
  final _reasonController = TextEditingController();

  @override
  void dispose() {
    _reasonController.dispose();
    super.dispose();
  }

  bool get _canAct => widget.order.status ==
      (widget.approvalMode == ApprovalMode.salesRep ? 1 : 2);

  @override
  Widget build(BuildContext context) {
    if (!_canAct) return const SizedBox.shrink();

    return Padding(
      padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _SectionHeader(icon: Icons.gavel_rounded, title: 'DECISION'),
          SizedBox(height: 10.h),
          if (_showRejectField) ...[
            Container(
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(16.r),
                border: Border.all(
                    color: const Color(0xFFEF4444).withValues(alpha: 0.3)),
                boxShadow: [
                  BoxShadow(
                    color: const Color(0xFFEF4444).withValues(alpha: 0.08),
                    blurRadius: 16,
                    offset: const Offset(0, 4),
                  ),
                ],
              ),
              padding: EdgeInsets.all(16.r),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Icon(Icons.report_gmailerrorred_rounded,
                          size: 16.r,
                          color: const Color(0xFFEF4444)),
                      SizedBox(width: 8.w),
                      Text(
                        'REJECTION REASON',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 11.sp,
                          fontWeight: FontWeight.w800,
                          letterSpacing: 1.5,
                          color: const Color(0xFFEF4444),
                        ),
                      ),
                    ],
                  ),
                  SizedBox(height: 12.h),
                  TextField(
                    controller: _reasonController,
                    minLines: 3,
                    maxLines: 5,
                    decoration: InputDecoration(
                      hintText: 'Describe the reason for rejection...',
                      hintStyle: GoogleFonts.barlow(
                          fontSize: 12.sp,
                          color: AppColors.foregroundMuted),
                      filled: true,
                      fillColor: const Color(0xFFFFF5F5),
                      border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(10.r),
                        borderSide: BorderSide(
                            color: const Color(0xFFEF4444)
                                .withValues(alpha: 0.3)),
                      ),
                      enabledBorder: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(10.r),
                        borderSide: BorderSide(
                            color: const Color(0xFFEF4444)
                                .withValues(alpha: 0.3)),
                      ),
                      focusedBorder: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(10.r),
                        borderSide: const BorderSide(
                            color: Color(0xFFEF4444), width: 1.5),
                      ),
                      contentPadding: EdgeInsets.all(12.r),
                    ),
                    style: GoogleFonts.barlow(
                        fontSize: 12.sp, color: AppColors.foreground),
                  ),
                  SizedBox(height: 12.h),
                  Row(
                    children: [
                      Expanded(
                        child: TextButton(
                          onPressed: () =>
                              setState(() => _showRejectField = false),
                          style: TextButton.styleFrom(
                            padding:
                                EdgeInsets.symmetric(vertical: 13.h),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(10.r),
                              side: BorderSide(
                                  color: AppColors.foreground
                                      .withValues(alpha: 0.15)),
                            ),
                          ),
                          child: Text(
                            'Cancel',
                            style: GoogleFonts.barlowCondensed(
                              fontSize: 14.sp,
                              fontWeight: FontWeight.w700,
                              color: AppColors.foregroundMuted,
                            ),
                          ),
                        ),
                      ),
                      SizedBox(width: 10.w),
                      Expanded(
                        flex: 2,
                        child: ElevatedButton(
                          onPressed: () {
                            final reason =
                                _reasonController.text.trim();
                            if (reason.isEmpty) return;
                            context.read<PurchaseOrdersBloc>().add(
                                RejectOrder(widget.order.id, reason));
                            _reasonController.clear();
                            setState(() => _showRejectField = false);
                          },
                          style: ElevatedButton.styleFrom(
                            backgroundColor: const Color(0xFFEF4444),
                            padding:
                                EdgeInsets.symmetric(vertical: 13.h),
                            elevation: 0,
                            shape: RoundedRectangleBorder(
                                borderRadius:
                                    BorderRadius.circular(10.r)),
                          ),
                          child: Text(
                            'Confirm Rejection',
                            style: GoogleFonts.barlowCondensed(
                              fontSize: 14.sp,
                              fontWeight: FontWeight.w800,
                              letterSpacing: 0.5,
                              color: Colors.white,
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ] else ...[
            if (widget.approvalMode == ApprovalMode.manager) ...[
              SizedBox(
                width: double.infinity,
                child: _ActionButton(
                  label: 'EDIT ORDER',
                  icon: Icons.edit_outlined,
                  color: const Color(0xFF3B82F6),
                  outlined: true,
                  onTap: () async {
                    final saved = await context.push<bool>(
                      '/supervisor/purchase-orders/${widget.order.id}/edit',
                      extra: widget.order,
                    );
                    if (saved == true && context.mounted) {
                      context.read<PurchaseOrdersBloc>().add(
                            LoadOrderDetail(widget.order.id),
                          );
                    }
                  },
                ),
              ),
              SizedBox(height: 10.h),
            ],
            Row(
              children: [
                // Reject button
                Expanded(
                  child: _ActionButton(
                    label: 'REJECT',
                    icon: Icons.cancel_outlined,
                    color: const Color(0xFFEF4444),
                    outlined: true,
                    onTap: () =>
                        setState(() => _showRejectField = true),
                  ),
                ),
                SizedBox(width: 12.w),
                // Approve button
                Expanded(
                  flex: 2,
                  child: _ActionButton(
                    label: 'APPROVE ORDER',
                    icon: Icons.check_circle_outline_rounded,
                    color: AppColors.primary,
                    outlined: false,
                    isLoading: widget.isActionInProgress,
                    onTap: widget.isActionInProgress
                        ? null
                        : () {
                            final event = widget.approvalMode ==
                                    ApprovalMode.salesRep
                                ? RepApproveOrder(widget.order.id)
                                : ManagerApproveOrder(widget.order.id);
                            context
                                .read<PurchaseOrdersBloc>()
                                .add(event);
                          },
                  ),
                ),
              ],
            ),
          ],
        ],
      ),
    );
  }
}

class _ActionButton extends StatelessWidget {
  final String label;
  final IconData icon;
  final Color color;
  final bool outlined;
  final bool isLoading;
  final VoidCallback? onTap;
  const _ActionButton({
    required this.label,
    required this.icon,
    required this.color,
    required this.outlined,
    required this.onTap,
    this.isLoading = false,
  });

  @override
  Widget build(BuildContext context) {
    if (outlined) {
      return OutlinedButton.icon(
        onPressed: onTap,
        icon: Icon(icon, size: 15.r, color: color),
        label: Text(
          label,
          style: GoogleFonts.barlowCondensed(
            fontSize: 13.sp,
            fontWeight: FontWeight.w800,
            letterSpacing: 1.0,
            color: color,
          ),
        ),
        style: OutlinedButton.styleFrom(
          padding: EdgeInsets.symmetric(vertical: 14.h),
          side: BorderSide(color: color.withValues(alpha: 0.7), width: 1.5),
          shape:
              RoundedRectangleBorder(borderRadius: BorderRadius.circular(12.r)),
        ),
      );
    }
    return ElevatedButton(
      onPressed: onTap,
      style: ElevatedButton.styleFrom(
        backgroundColor: color,
        padding: EdgeInsets.symmetric(vertical: 14.h),
        elevation: 0,
        shadowColor: Colors.transparent,
        shape:
            RoundedRectangleBorder(borderRadius: BorderRadius.circular(12.r)),
      ),
      child: isLoading
          ? SizedBox(
              height: 18.r,
              width: 18.r,
              child: CircularProgressIndicator(
                strokeWidth: 2.5,
                valueColor: const AlwaysStoppedAnimation<Color>(Colors.white),
              ),
            )
          : Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Icon(icon, size: 15.r, color: Colors.white),
                SizedBox(width: 6.w),
                Text(
                  label,
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 14.sp,
                    fontWeight: FontWeight.w800,
                    letterSpacing: 1.0,
                    color: Colors.white,
                  ),
                ),
              ],
            ),
    );
  }
}

// ── Shared section header ─────────────────────────────────────────────────────

class _SectionHeader extends StatelessWidget {
  final IconData icon;
  final String title;
  final String? badge;
  const _SectionHeader(
      {required this.icon, required this.title, this.badge});

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Container(
          width: 28.r,
          height: 28.r,
          decoration: BoxDecoration(
            color: AppColors.primaryDark.withValues(alpha: 0.1),
            borderRadius: BorderRadius.circular(7.r),
          ),
          child: Icon(icon, size: 14.r, color: AppColors.primaryDark),
        ),
        SizedBox(width: 8.w),
        Text(
          title,
          style: GoogleFonts.barlowCondensed(
            fontSize: 11.sp,
            fontWeight: FontWeight.w800,
            letterSpacing: 2.0,
            color: AppColors.foregroundMuted,
          ),
        ),
        if (badge != null) ...[
          SizedBox(width: 8.w),
          Container(
            padding:
                EdgeInsets.symmetric(horizontal: 7.w, vertical: 2.h),
            decoration: BoxDecoration(
              color: AppColors.primary.withValues(alpha: 0.12),
              borderRadius: BorderRadius.circular(20.r),
            ),
            child: Text(
              badge!,
              style: GoogleFonts.barlowCondensed(
                fontSize: 10.sp,
                fontWeight: FontWeight.w800,
                color: AppColors.primary,
              ),
            ),
          ),
        ],
      ],
    );
  }
}
