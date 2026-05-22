import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/purchase_orders/domain/entities/purchase_order_summary.dart';
import 'package:uswatte/features/purchase_orders/presentation/bloc/purchase_orders_bloc.dart';
import 'package:uswatte/features/purchase_orders/presentation/bloc/purchase_orders_event.dart';
import 'package:uswatte/features/purchase_orders/presentation/bloc/purchase_orders_state.dart';

// Shared across list + detail pages
enum ApprovalMode { salesRep, manager }

class PurchaseOrdersListPage extends StatelessWidget {
  final ApprovalMode approvalMode;
  const PurchaseOrdersListPage({
    super.key,
    this.approvalMode = ApprovalMode.salesRep,
  });

  String get _detailRoutePrefix => approvalMode == ApprovalMode.salesRep
      ? '/sales-rep/purchase-orders'
      : '/supervisor/purchase-orders';

  String get _homeRouteName => approvalMode == ApprovalMode.salesRep
      ? 'salesRepHome'
      : 'supervisorHome';

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    return Scaffold(
      backgroundColor: const Color(0xFFF8F7F5),
      body: Column(
        children: [
          _Header(approvalMode: approvalMode, homeRouteName: _homeRouteName),
          Expanded(
            child: BlocBuilder<PurchaseOrdersBloc, PurchaseOrdersState>(
              builder: (context, state) {
                if (state is PurchaseOrdersInitial ||
                    state is PurchaseOrdersLoading) {
                  return _LoadingView();
                }

                if (state is PurchaseOrdersError) {
                  return _ErrorView(
                    message: state.message,
                    onRetry: () => context
                        .read<PurchaseOrdersBloc>()
                        .add(const RefreshOrders()),
                  );
                }

                if (state is PurchaseOrdersLoaded) {
                  if (state.orders.isEmpty) return const _EmptyView();

                  return RefreshIndicator(
                    color: AppColors.primary,
                    backgroundColor: Colors.white,
                    onRefresh: () async {
                      context
                          .read<PurchaseOrdersBloc>()
                          .add(const RefreshOrders());
                      await context.read<PurchaseOrdersBloc>().stream
                          .firstWhere((s) =>
                              s is PurchaseOrdersLoaded ||
                              s is PurchaseOrdersError);
                    },
                    child: CustomScrollView(
                      slivers: [
                        SliverToBoxAdapter(
                          child: _StatsBar(count: state.orders.length),
                        ),
                        SliverPadding(
                          padding: EdgeInsets.fromLTRB(
                              16.w, 8.h, 16.w, 40.h),
                          sliver: SliverList(
                            delegate: SliverChildBuilderDelegate(
                              (context, i) {
                                final order = state.orders[i];
                                final bloc =
                                    context.read<PurchaseOrdersBloc>();
                                return Padding(
                                  padding: EdgeInsets.only(bottom: 10.h),
                                  child: _PurchaseOrderCard(
                                    order: order,
                                    approvalMode: approvalMode,
                                    onTap: () async {
                                      await context.push(
                                          '$_detailRoutePrefix/${order.id}');
                                      bloc.add(const RefreshOrders());
                                    },
                                  ),
                                );
                              },
                              childCount: state.orders.length,
                            ),
                          ),
                        ),
                      ],
                    ),
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

// ── Header ────────────────────────────────────────────────────────────────────

class _Header extends StatelessWidget {
  final ApprovalMode approvalMode;
  final String homeRouteName;
  const _Header(
      {required this.approvalMode, required this.homeRouteName});

  @override
  Widget build(BuildContext context) {
    final label = approvalMode == ApprovalMode.salesRep
        ? 'PURCHASE ORDERS'
        : 'PURCHASE ORDERS';
    final subtitle = approvalMode == ApprovalMode.salesRep
        ? 'Awaiting your approval'
        : 'Awaiting manager approval';

    return Container(
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [Color(0xFF7C2D12), AppColors.primaryDark, AppColors.primary],
        ),
      ),
      child: Stack(
        children: [
          // Decorative circles
          Positioned(
            right: -20.w,
            top: -20.h,
            child: Container(
              width: 130.r,
              height: 130.r,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: Colors.white.withValues(alpha: 0.05),
              ),
            ),
          ),
          Positioned(
            right: 60.w,
            bottom: -10.h,
            child: Container(
              width: 60.r,
              height: 60.r,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: Colors.white.withValues(alpha: 0.04),
              ),
            ),
          ),
          SafeArea(
            bottom: false,
            child: Padding(
              padding: EdgeInsets.fromLTRB(8.w, 4.h, 16.w, 18.h),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.center,
                children: [
                  // Back button
                  GestureDetector(
                    onTap: () => context.canPop()
                        ? context.pop()
                        : context.goNamed(homeRouteName),
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
                  SizedBox(width: 6.w),
                  // Title block
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(
                          label,
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 22.sp,
                            fontWeight: FontWeight.w900,
                            letterSpacing: 1.5,
                            height: 1.0,
                            color: Colors.white,
                          ),
                        ),
                        SizedBox(height: 2.h),
                        Text(
                          subtitle,
                          style: GoogleFonts.barlow(
                            fontSize: 11.sp,
                            color: Colors.white.withValues(alpha: 0.65),
                          ),
                        ),
                      ],
                    ),
                  ),
                  // Icon badge
                  Container(
                    width: 40.r,
                    height: 40.r,
                    decoration: BoxDecoration(
                      color: Colors.white.withValues(alpha: 0.12),
                      borderRadius: BorderRadius.circular(10.r),
                      border: Border.all(
                          color: Colors.white.withValues(alpha: 0.2)),
                    ),
                    child: Icon(Icons.assignment_outlined,
                        size: 20.r, color: Colors.white),
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

// ── Stats bar ─────────────────────────────────────────────────────────────────

class _StatsBar extends StatelessWidget {
  final int count;
  const _StatsBar({required this.count});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 4.h),
      child: Row(
        children: [
          Container(
            padding:
                EdgeInsets.symmetric(horizontal: 12.w, vertical: 6.h),
            decoration: BoxDecoration(
              color: AppColors.primary.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(8.r),
              border: Border.all(
                  color: AppColors.primary.withValues(alpha: 0.2)),
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Container(
                  width: 6.r,
                  height: 6.r,
                  decoration: BoxDecoration(
                    color: AppColors.primary,
                    shape: BoxShape.circle,
                  ),
                ),
                SizedBox(width: 6.w),
                Text(
                  '$count PENDING',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 11.sp,
                    fontWeight: FontWeight.w800,
                    letterSpacing: 1.5,
                    color: AppColors.primary,
                  ),
                ),
              ],
            ),
          ),
          SizedBox(width: 8.w),
          Text(
            'Pull to refresh',
            style: GoogleFonts.barlow(
                fontSize: 11.sp, color: AppColors.foregroundMuted),
          ),
        ],
      ),
    );
  }
}

// ── Order card ────────────────────────────────────────────────────────────────

class _PurchaseOrderCard extends StatelessWidget {
  final PurchaseOrderSummary order;
  final ApprovalMode approvalMode;
  final VoidCallback onTap;

  const _PurchaseOrderCard({
    required this.order,
    required this.approvalMode,
    required this.onTap,
  });

  String _fmt(DateTime? dt) {
    if (dt == null) return '—';
    return '${dt.day.toString().padLeft(2, '0')}/'
        '${dt.month.toString().padLeft(2, '0')}/'
        '${dt.year}';
  }

  @override
  Widget build(BuildContext context) {
    final chipLabel = approvalMode == ApprovalMode.salesRep
        ? 'REP APPROVAL'
        : 'MGR APPROVAL';
    const chipColor = Color(0xFFF59E0B);

    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(16.r),
        child: Ink(
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
          child: IntrinsicHeight(
            child: Row(
              children: [
                // Left accent bar
                Container(
                  width: 4.w,
                  decoration: BoxDecoration(
                    color: chipColor,
                    borderRadius: BorderRadius.only(
                      topLeft: Radius.circular(16.r),
                      bottomLeft: Radius.circular(16.r),
                    ),
                  ),
                ),
                // Card content
                Expanded(
                  child: Padding(
                    padding: EdgeInsets.fromLTRB(14.w, 14.h, 14.w, 14.h),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        // Top row: order number + status chip
                        Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    order.orderNumber,
                                    style: GoogleFonts.barlowCondensed(
                                      fontSize: 17.sp,
                                      fontWeight: FontWeight.w900,
                                      letterSpacing: 0.8,
                                      height: 1.0,
                                      color: AppColors.foreground,
                                    ),
                                  ),
                                  SizedBox(height: 4.h),
                                  Row(
                                    children: [
                                      Icon(Icons.storefront_outlined,
                                          size: 11.r,
                                          color: AppColors.foregroundMuted),
                                      SizedBox(width: 4.w),
                                      Expanded(
                                        child: Text(
                                          order.distributorName,
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
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.end,
                              children: [
                                Container(
                                  padding: EdgeInsets.symmetric(
                                      horizontal: 8.w, vertical: 4.h),
                                  decoration: BoxDecoration(
                                    color:
                                        chipColor.withValues(alpha: 0.10),
                                    borderRadius:
                                        BorderRadius.circular(5.r),
                                    border: Border.all(
                                      color: chipColor
                                          .withValues(alpha: 0.35),
                                      width: 0.8,
                                    ),
                                  ),
                                  child: Text(
                                    chipLabel,
                                    style: GoogleFonts.barlowCondensed(
                                      fontSize: 9.sp,
                                      fontWeight: FontWeight.w800,
                                      letterSpacing: 1.2,
                                      color: chipColor,
                                    ),
                                  ),
                                ),
                                SizedBox(height: 6.h),
                                Icon(Icons.arrow_forward_ios_rounded,
                                    size: 11.r,
                                    color: AppColors.foregroundMuted
                                        .withValues(alpha: 0.5)),
                              ],
                            ),
                          ],
                        ),
                        SizedBox(height: 12.h),
                        // Divider
                        Divider(
                          height: 1,
                          color: const Color(0xFF1C1917)
                              .withValues(alpha: 0.07),
                        ),
                        SizedBox(height: 10.h),
                        // Bottom stat row
                        Row(
                          children: [
                            _StatChip(
                              icon: Icons.payments_outlined,
                              label: 'LKR ${order.totalAmount.toStringAsFixed(2)}',
                              color: AppColors.primary,
                            ),
                            SizedBox(width: 10.w),
                            _StatChip(
                              icon: Icons.inventory_2_outlined,
                              label:
                                  '${order.itemCount} item${order.itemCount == 1 ? '' : 's'}',
                              color: AppColors.foregroundMuted,
                            ),
                            const Spacer(),
                            Row(
                              children: [
                                Icon(Icons.schedule_rounded,
                                    size: 11.r,
                                    color: AppColors.foregroundMuted
                                        .withValues(alpha: 0.7)),
                                SizedBox(width: 3.w),
                                Text(
                                  _fmt(order.submittedAt),
                                  style: GoogleFonts.barlow(
                                    fontSize: 11.sp,
                                    color: AppColors.foregroundMuted,
                                  ),
                                ),
                              ],
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
      ),
    );
  }
}

class _StatChip extends StatelessWidget {
  final IconData icon;
  final String label;
  final Color color;
  const _StatChip(
      {required this.icon, required this.label, required this.color});

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 11.r, color: color),
        SizedBox(width: 4.w),
        Text(
          label,
          style: GoogleFonts.barlowCondensed(
            fontSize: 12.sp,
            fontWeight: FontWeight.w700,
            color: color,
          ),
        ),
      ],
    );
  }
}

// ── States ────────────────────────────────────────────────────────────────────

class _LoadingView extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          SizedBox(
            width: 44.r,
            height: 44.r,
            child: CircularProgressIndicator(
              color: AppColors.primary,
              strokeWidth: 2.5,
            ),
          ),
          SizedBox(height: 16.h),
          Text(
            'Loading orders...',
            style: GoogleFonts.barlow(
                fontSize: 13.sp, color: AppColors.foregroundMuted),
          ),
        ],
      ),
    );
  }
}

class _EmptyView extends StatelessWidget {
  const _EmptyView();

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 72.r,
            height: 72.r,
            decoration: BoxDecoration(
              color: AppColors.primary.withValues(alpha: 0.08),
              borderRadius: BorderRadius.circular(20.r),
            ),
            child: Icon(Icons.inbox_outlined,
                size: 34.r,
                color: AppColors.primary.withValues(alpha: 0.5)),
          ),
          SizedBox(height: 16.h),
          Text(
            'ALL CLEAR',
            style: GoogleFonts.barlowCondensed(
              fontSize: 20.sp,
              fontWeight: FontWeight.w900,
              letterSpacing: 2.0,
              color: AppColors.foreground,
            ),
          ),
          SizedBox(height: 4.h),
          Text(
            'No orders are pending approval',
            style: GoogleFonts.barlow(
                fontSize: 13.sp, color: AppColors.foregroundMuted),
          ),
        ],
      ),
    );
  }
}

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
            Container(
              width: 64.r,
              height: 64.r,
              decoration: BoxDecoration(
                color: const Color(0xFFEF4444).withValues(alpha: 0.08),
                borderRadius: BorderRadius.circular(18.r),
              ),
              child: Icon(Icons.wifi_off_rounded,
                  size: 30.r,
                  color: const Color(0xFFEF4444).withValues(alpha: 0.6)),
            ),
            SizedBox(height: 16.h),
            Text(
              'Could not load orders',
              style: GoogleFonts.barlowCondensed(
                fontSize: 18.sp,
                fontWeight: FontWeight.w800,
                color: AppColors.foreground,
              ),
            ),
            SizedBox(height: 4.h),
            Text(
              message,
              textAlign: TextAlign.center,
              style: GoogleFonts.barlow(
                  fontSize: 12.sp, color: AppColors.foregroundMuted),
            ),
            SizedBox(height: 20.h),
            ElevatedButton.icon(
              onPressed: onRetry,
              icon: Icon(Icons.refresh_rounded, size: 16.r),
              label: Text(
                'Retry',
                style: GoogleFonts.barlowCondensed(
                  fontSize: 14.sp,
                  fontWeight: FontWeight.w700,
                ),
              ),
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.primary,
                foregroundColor: Colors.white,
                padding:
                    EdgeInsets.symmetric(horizontal: 24.w, vertical: 12.h),
                elevation: 0,
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(10.r)),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
