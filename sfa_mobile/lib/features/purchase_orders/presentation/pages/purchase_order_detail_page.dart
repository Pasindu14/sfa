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

class PurchaseOrderDetailPage extends StatelessWidget {
  const PurchaseOrderDetailPage({super.key});

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
              content: Text(state.message),
              backgroundColor: const Color(0xFF22C55E),
            ),
          );
          if (context.canPop()) context.pop();
        }
        if (state is PurchaseOrdersError) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(state.message),
              backgroundColor: const Color(0xFFEF4444),
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
          isActionInProgress = true;
        }

        return Scaffold(
          backgroundColor: AppColors.background,
          body: Column(
            children: [
              // ── Header ───────────────────────────────────────────────
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
                          onTap: () =>
                              context.canPop() ? context.pop() : null,
                          child: Container(
                            width: 40.r,
                            height: 40.r,
                            margin: EdgeInsets.all(4.r),
                            decoration: BoxDecoration(
                              color: Colors.white.withValues(alpha: 0.15),
                              borderRadius: BorderRadius.circular(10.r),
                              border: Border.all(
                                  color:
                                      Colors.white.withValues(alpha: 0.25)),
                            ),
                            child: Icon(Icons.arrow_back_ios_new_rounded,
                                size: 15.r, color: Colors.white),
                          ),
                        ),
                        SizedBox(width: 4.w),
                        Expanded(
                          child: Text(
                            order?.orderNumber ?? 'PURCHASE ORDER',
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

              // ── Body ─────────────────────────────────────────────────
              Expanded(
                child: isActionInProgress
                    ? Center(
                        child: CircularProgressIndicator(
                            color: AppColors.primary))
                    : order == null
                        ? Center(
                            child: CircularProgressIndicator(
                                color: AppColors.primary))
                        : _DetailBody(order: order),
              ),
            ],
          ),
        );
      },
    );
  }
}

class _DetailBody extends StatefulWidget {
  final PurchaseOrderDetail order;
  const _DetailBody({required this.order});

  @override
  State<_DetailBody> createState() => _DetailBodyState();
}

class _DetailBodyState extends State<_DetailBody> {
  bool _showRejectField = false;
  final _reasonController = TextEditingController();

  @override
  void dispose() {
    _reasonController.dispose();
    super.dispose();
  }

  String _formatDate(DateTime? dt) {
    if (dt == null) return '—';
    return '${dt.day.toString().padLeft(2, '0')}/'
        '${dt.month.toString().padLeft(2, '0')}/'
        '${dt.year}';
  }

  // status=1 is PendingRepApproval
  bool get _canAct => widget.order.status == 1;

  @override
  Widget build(BuildContext context) {
    final order = widget.order;

    return SingleChildScrollView(
      padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 40.h),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // ── Order Info ───────────────────────────────────────────────
          _InfoCard(children: [
            _InfoRow('Order No.', order.orderNumber),
            _InfoRow('Distributor', order.distributorName),
            _InfoRow('Submitted', _formatDate(order.submittedAt)),
            _InfoRow('Created', _formatDate(order.createdAt)),
            if (order.notes != null && order.notes!.isNotEmpty)
              _InfoRow('Notes', order.notes!),
          ]),

          SizedBox(height: 12.h),

          // ── Items ────────────────────────────────────────────────────
          _SectionLabel('ITEMS'),
          SizedBox(height: 8.h),
          _InfoCard(
            children: [
              // Table header
              Padding(
                padding: EdgeInsets.only(bottom: 6.h),
                child: Row(
                  children: [
                    Expanded(flex: 3, child: _TableHeader('Product')),
                    Expanded(child: _TableHeader('Qty')),
                    Expanded(child: _TableHeader('Price')),
                    Expanded(child: _TableHeader('Total')),
                  ],
                ),
              ),
              Divider(
                  height: 1,
                  color: AppColors.foreground.withValues(alpha: 0.08)),
              SizedBox(height: 6.h),
              ...order.items.map((item) => Padding(
                    padding: EdgeInsets.symmetric(vertical: 4.h),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Expanded(
                          flex: 3,
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                item.productCode,
                                style: GoogleFonts.barlowCondensed(
                                  fontSize: 11.sp,
                                  fontWeight: FontWeight.w700,
                                  color: AppColors.primary,
                                  letterSpacing: 0.5,
                                ),
                              ),
                              Text(
                                item.productDescription,
                                style: GoogleFonts.barlow(
                                    fontSize: 11.sp,
                                    color: AppColors.foreground),
                              ),
                            ],
                          ),
                        ),
                        Expanded(child: _TableCell(item.quantity.toString())),
                        Expanded(child: _TableCell(item.unitPrice.toStringAsFixed(2))),
                        Expanded(child: _TableCell(item.lineTotal.toStringAsFixed(2))),
                      ],
                    ),
                  )),
              Divider(
                  height: 12.h,
                  color: AppColors.foreground.withValues(alpha: 0.08)),
              Row(
                children: [
                  const Expanded(flex: 3, child: SizedBox()),
                  Expanded(
                    flex: 3,
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(
                          'TOTAL',
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 12.sp,
                            fontWeight: FontWeight.w800,
                            letterSpacing: 1.0,
                            color: AppColors.foreground,
                          ),
                        ),
                        Text(
                          order.totalAmount.toStringAsFixed(2),
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 12.sp,
                            fontWeight: FontWeight.w800,
                            color: AppColors.foreground,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ],
          ),

          // ── History ──────────────────────────────────────────────────
          if (order.history.isNotEmpty) ...[
            SizedBox(height: 12.h),
            _SectionLabel('HISTORY'),
            SizedBox(height: 8.h),
            _InfoCard(
              children: order.history.map((entry) {
                return Padding(
                  padding: EdgeInsets.symmetric(vertical: 5.h),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Container(
                        width: 8.r,
                        height: 8.r,
                        margin: EdgeInsets.only(top: 3.h, right: 10.w),
                        decoration: BoxDecoration(
                          color: AppColors.primary,
                          shape: BoxShape.circle,
                        ),
                      ),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              entry.action,
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 12.sp,
                                fontWeight: FontWeight.w700,
                                color: AppColors.foreground,
                              ),
                            ),
                            Text(
                              [
                                if (entry.performedByName != null)
                                  entry.performedByName!,
                                _formatDate(entry.performedAt),
                              ].join(' · '),
                              style: GoogleFonts.barlow(
                                  fontSize: 11.sp,
                                  color: AppColors.foregroundMuted),
                            ),
                            if (entry.notes != null &&
                                entry.notes!.isNotEmpty) ...[
                              SizedBox(height: 2.h),
                              Text(
                                entry.notes!,
                                style: GoogleFonts.barlow(
                                    fontSize: 11.sp,
                                    color: AppColors.foreground),
                              ),
                            ],
                          ],
                        ),
                      ),
                    ],
                  ),
                );
              }).toList(),
            ),
          ],

          // ── Actions ──────────────────────────────────────────────────
          if (_canAct) ...[
            SizedBox(height: 20.h),
            if (_showRejectField) ...[
              TextField(
                controller: _reasonController,
                minLines: 2,
                maxLines: 4,
                decoration: InputDecoration(
                  hintText: 'Enter rejection reason...',
                  hintStyle: GoogleFonts.barlow(
                      fontSize: 13.sp,
                      color: AppColors.foregroundMuted),
                  filled: true,
                  fillColor: Colors.white,
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(10.r),
                    borderSide: BorderSide(
                        color:
                            AppColors.foreground.withValues(alpha: 0.15)),
                  ),
                  enabledBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(10.r),
                    borderSide: BorderSide(
                        color:
                            AppColors.foreground.withValues(alpha: 0.15)),
                  ),
                  focusedBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(10.r),
                    borderSide:
                        BorderSide(color: AppColors.primary),
                  ),
                  contentPadding: EdgeInsets.all(12.r),
                ),
                style: GoogleFonts.barlow(
                    fontSize: 13.sp, color: AppColors.foreground),
              ),
              SizedBox(height: 10.h),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: () =>
                          setState(() => _showRejectField = false),
                      style: OutlinedButton.styleFrom(
                        padding: EdgeInsets.symmetric(vertical: 12.h),
                        side: BorderSide(
                            color: AppColors.foreground
                                .withValues(alpha: 0.2)),
                        shape: RoundedRectangleBorder(
                            borderRadius:
                                BorderRadius.circular(10.r)),
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
                    child: ElevatedButton(
                      onPressed: () {
                        final reason = _reasonController.text.trim();
                        if (reason.isEmpty) return;
                        context.read<PurchaseOrdersBloc>().add(
                            RejectOrder(widget.order.id, reason));
                      },
                      style: ElevatedButton.styleFrom(
                        backgroundColor: const Color(0xFFEF4444),
                        padding: EdgeInsets.symmetric(vertical: 12.h),
                        elevation: 0,
                        shape: RoundedRectangleBorder(
                            borderRadius:
                                BorderRadius.circular(10.r)),
                      ),
                      child: Text(
                        'Confirm Reject',
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
            ] else ...[
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: () =>
                          setState(() => _showRejectField = true),
                      style: OutlinedButton.styleFrom(
                        padding: EdgeInsets.symmetric(vertical: 14.h),
                        side: const BorderSide(
                            color: Color(0xFFEF4444)),
                        shape: RoundedRectangleBorder(
                            borderRadius:
                                BorderRadius.circular(10.r)),
                      ),
                      child: Text(
                        'Reject',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 15.sp,
                          fontWeight: FontWeight.w700,
                          color: const Color(0xFFEF4444),
                          letterSpacing: 0.5,
                        ),
                      ),
                    ),
                  ),
                  SizedBox(width: 12.w),
                  Expanded(
                    child: ElevatedButton(
                      onPressed: () => context
                          .read<PurchaseOrdersBloc>()
                          .add(RepApproveOrder(widget.order.id)),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppColors.primary,
                        padding: EdgeInsets.symmetric(vertical: 14.h),
                        elevation: 0,
                        shape: RoundedRectangleBorder(
                            borderRadius:
                                BorderRadius.circular(10.r)),
                      ),
                      child: Text(
                        'Approve',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 15.sp,
                          fontWeight: FontWeight.w700,
                          color: Colors.white,
                          letterSpacing: 0.5,
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ],
          ],
        ],
      ),
    );
  }
}

class _SectionLabel extends StatelessWidget {
  final String text;
  const _SectionLabel(this.text);

  @override
  Widget build(BuildContext context) {
    return Text(
      text,
      style: GoogleFonts.barlowCondensed(
        fontSize: 11.sp,
        fontWeight: FontWeight.w700,
        letterSpacing: 1.5,
        color: AppColors.foregroundMuted,
      ),
    );
  }
}

class _TableHeader extends StatelessWidget {
  final String text;
  const _TableHeader(this.text);

  @override
  Widget build(BuildContext context) {
    return Text(
      text,
      style: GoogleFonts.barlowCondensed(
        fontSize: 10.sp,
        fontWeight: FontWeight.w700,
        letterSpacing: 0.5,
        color: AppColors.foregroundMuted,
      ),
    );
  }
}

class _TableCell extends StatelessWidget {
  final String text;
  const _TableCell(this.text);

  @override
  Widget build(BuildContext context) {
    return Text(
      text,
      style: GoogleFonts.barlow(
          fontSize: 11.sp, color: AppColors.foreground),
      textAlign: TextAlign.left,
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
            width: 100.w,
            child: Text(
              label,
              style: GoogleFonts.barlow(
                  fontSize: 12.sp,
                  color: AppColors.foregroundMuted),
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
