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

class PurchaseOrdersListPage extends StatelessWidget {
  const PurchaseOrdersListPage({super.key});

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
                        'PURCHASE ORDERS',
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

          // ── List ────────────────────────────────────────────────────────
          Expanded(
            child: BlocBuilder<PurchaseOrdersBloc, PurchaseOrdersState>(
              builder: (context, state) {
                if (state is PurchaseOrdersInitial ||
                    state is PurchaseOrdersLoading) {
                  return Center(
                      child: CircularProgressIndicator(
                          color: AppColors.primary));
                }

                if (state is PurchaseOrdersError) {
                  return Center(
                    child: Text(
                      state.message,
                      style: GoogleFonts.barlow(
                          fontSize: 13.sp,
                          color: AppColors.foregroundMuted),
                    ),
                  );
                }

                if (state is PurchaseOrdersLoaded) {
                  if (state.orders.isEmpty) {
                    return Center(
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(Icons.inbox_outlined,
                              size: 48.r,
                              color: AppColors.foregroundMuted
                                  .withValues(alpha: 0.4)),
                          SizedBox(height: 12.h),
                          Text(
                            'No pending purchase orders',
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

                  return RefreshIndicator(
                    color: AppColors.primary,
                    onRefresh: () async {
                      context
                          .read<PurchaseOrdersBloc>()
                          .add(const RefreshOrders());
                      await context.read<PurchaseOrdersBloc>().stream
                          .firstWhere((s) =>
                              s is PurchaseOrdersLoaded ||
                              s is PurchaseOrdersError);
                    },
                    child: ListView.separated(
                      padding:
                          EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 40.h),
                      itemCount: state.orders.length,
                      separatorBuilder: (_, __) => SizedBox(height: 10.h),
                      itemBuilder: (context, i) {
                        final order = state.orders[i];
                        final bloc = context.read<PurchaseOrdersBloc>();
                        return _PurchaseOrderCard(
                          order: order,
                          onTap: () async {
                            await context.push(
                                '/sales-rep/purchase-orders/${order.id}');
                            bloc.add(const RefreshOrders());
                          },
                        );
                      },
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

class _PurchaseOrderCard extends StatelessWidget {
  final PurchaseOrderSummary order;
  final VoidCallback onTap;

  const _PurchaseOrderCard({required this.order, required this.onTap});

  String _formatDate(DateTime? dt) {
    if (dt == null) return '—';
    return '${dt.day.toString().padLeft(2, '0')}/'
        '${dt.month.toString().padLeft(2, '0')}/'
        '${dt.year}';
  }

  @override
  Widget build(BuildContext context) {
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
                    order.orderNumber,
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
                    color: const Color(0xFFF59E0B).withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(20.r),
                  ),
                  child: Text(
                    'Pending Approval',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 10.sp,
                      fontWeight: FontWeight.w700,
                      color: const Color(0xFFF59E0B),
                      letterSpacing: 0.5,
                    ),
                  ),
                ),
              ],
            ),
            SizedBox(height: 6.h),
            Text(
              order.distributorName,
              style: GoogleFonts.barlowCondensed(
                fontSize: 13.sp,
                fontWeight: FontWeight.w600,
                color: AppColors.foreground,
              ),
            ),
            SizedBox(height: 2.h),
            Text(
              [
                'LKR ${order.totalAmount.toStringAsFixed(2)}',
                '${order.itemCount} item${order.itemCount == 1 ? '' : 's'}',
                _formatDate(order.submittedAt),
              ].join('  ·  '),
              style: GoogleFonts.barlow(
                fontSize: 11.sp,
                color: AppColors.foregroundMuted,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
