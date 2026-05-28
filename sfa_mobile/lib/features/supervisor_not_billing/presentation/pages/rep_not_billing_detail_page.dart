import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/core/widgets/app_spinner.dart';
import 'package:uswatte/features/supervisor_not_billing/domain/entities/not_billing_reason.dart';
import 'package:uswatte/features/supervisor_not_billing/domain/entities/rep_not_billing_detail.dart';
import 'package:uswatte/features/supervisor_not_billing/presentation/cubit/not_billing_detail_cubit.dart';
import 'package:uswatte/features/supervisor_not_billing/presentation/cubit/not_billing_detail_state.dart';

class RepNotBillingDetailPage extends StatefulWidget {
  final int notBillingId;
  final String? notBillingNumber;

  const RepNotBillingDetailPage({
    super.key,
    required this.notBillingId,
    this.notBillingNumber,
  });

  @override
  State<RepNotBillingDetailPage> createState() =>
      _RepNotBillingDetailPageState();
}

class _RepNotBillingDetailPageState extends State<RepNotBillingDetailPage> {
  @override
  void initState() {
    super.initState();
    context.read<NotBillingDetailCubit>().load(widget.notBillingId);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF5F4EE),
      body: Column(
        children: [
          _OrangeAppBar(notBillingNumber: widget.notBillingNumber),
          Expanded(
            child: BlocBuilder<NotBillingDetailCubit, NotBillingDetailState>(
              builder: (context, state) {
                if (state is NotBillingDetailLoading ||
                    state is NotBillingDetailInitial) {
                  return const _LoadingBody();
                }
                if (state is NotBillingDetailError) {
                  return _ErrorBody(
                    message: state.message,
                    onRetry: () => context
                        .read<NotBillingDetailCubit>()
                        .load(widget.notBillingId),
                  );
                }
                if (state is NotBillingDetailLoaded) {
                  return _DetailBody(detail: state.detail);
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

// ── Orange app bar ────────────────────────────────────────────────────────────

class _OrangeAppBar extends StatelessWidget {
  final String? notBillingNumber;
  const _OrangeAppBar({this.notBillingNumber});

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

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
        child: Stack(
          children: [
            Positioned(
              right: -18.w,
              top: -18.r,
              child: Container(
                width: 90.r,
                height: 90.r,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: Colors.white.withValues(alpha: 0.07),
                ),
              ),
            ),
            Padding(
              padding:
                  EdgeInsets.symmetric(horizontal: 8.w, vertical: 10.r),
              child: Row(
                children: [
                  GestureDetector(
                    onTap: () => context.pop(),
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
                  Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        notBillingNumber ?? 'NON-BILLING DETAIL',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 18.sp,
                          fontWeight: FontWeight.w800,
                          letterSpacing: 1.5,
                          height: 1.0,
                          color: Colors.white,
                        ),
                      ),
                      SizedBox(height: 2.r),
                      Text(
                        'Non-billing details',
                        style: GoogleFonts.barlow(
                          fontSize: 11.sp,
                          color: Colors.white.withValues(alpha: 0.70),
                        ),
                      ),
                    ],
                  ),
                  const Spacer(),
                  Container(
                    width: 38.r,
                    height: 38.r,
                    margin: EdgeInsets.only(right: 16.w),
                    decoration: BoxDecoration(
                      color: Colors.white.withValues(alpha: 0.15),
                      borderRadius: BorderRadius.circular(10.r),
                    ),
                    child: Icon(Icons.block_rounded,
                        size: 18.r, color: Colors.white),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Loading / Error ───────────────────────────────────────────────────────────

class _LoadingBody extends StatelessWidget {
  const _LoadingBody();

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const AppSpinner(),
        ],
      ),
    );
  }
}

class _ErrorBody extends StatelessWidget {
  final String message;
  final VoidCallback onRetry;
  const _ErrorBody({required this.message, required this.onRetry});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: EdgeInsets.symmetric(horizontal: 32.w),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 60.r,
              height: 60.r,
              decoration: BoxDecoration(
                color: AppColors.error.withValues(alpha: 0.08),
                shape: BoxShape.circle,
              ),
              child: Icon(Icons.error_outline_rounded,
                  size: 28.r, color: AppColors.error),
            ),
            SizedBox(height: 14.h),
            Text(message,
                textAlign: TextAlign.center,
                style: GoogleFonts.barlow(
                    fontSize: 14.sp,
                    color: AppColors.foregroundMuted,
                    height: 1.5)),
            SizedBox(height: 20.h),
            GestureDetector(
              onTap: onRetry,
              child: Container(
                padding: EdgeInsets.symmetric(
                    horizontal: 24.w, vertical: 12.h),
                decoration: BoxDecoration(
                  color: AppColors.primary,
                  borderRadius: BorderRadius.circular(10.r),
                ),
                child: Text('Try Again',
                    style: GoogleFonts.barlowCondensed(
                        fontSize: 15.sp,
                        fontWeight: FontWeight.w700,
                        color: Colors.white,
                        letterSpacing: 0.5)),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Detail body ───────────────────────────────────────────────────────────────

class _DetailBody extends StatelessWidget {
  final RepNotBillingDetail detail;
  const _DetailBody({required this.detail});

  String _formatDate(String dateStr) {
    final parts = dateStr.split('-');
    if (parts.length != 3) return dateStr;
    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
    ];
    final month = int.tryParse(parts[1]) ?? 1;
    return '${months[month - 1]} ${parts[2]}, ${parts[0]}';
  }

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: EdgeInsets.fromLTRB(16.w, 20.h, 16.w, 40.h),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _InfoCard(detail: detail, formatDate: _formatDate),
          SizedBox(height: 16.h),
          _ReasonCard(reason: detail.reason),
          if (detail.notes != null && detail.notes!.isNotEmpty) ...[
            SizedBox(height: 16.h),
            _NotesCard(notes: detail.notes!),
          ],
        ],
      ),
    );
  }
}

// ── Info card ─────────────────────────────────────────────────────────────────

class _InfoCard extends StatelessWidget {
  final RepNotBillingDetail detail;
  final String Function(String) formatDate;

  const _InfoCard({required this.detail, required this.formatDate});

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16.r),
        boxShadow: [
          BoxShadow(
            color: const Color(0xFF1A1A11).withValues(alpha: 0.06),
            blurRadius: 16,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        children: [
          Container(
            padding: EdgeInsets.fromLTRB(16.w, 14.h, 16.w, 12.h),
            decoration: const BoxDecoration(
              border: Border(
                  bottom: BorderSide(color: Color(0xFFEEEDE6))),
            ),
            child: Row(
              children: [
                Container(
                  width: 36.r,
                  height: 36.r,
                  decoration: BoxDecoration(
                    color: AppColors.primary.withValues(alpha: 0.08),
                    borderRadius: BorderRadius.circular(10.r),
                  ),
                  child: Icon(Icons.info_outline_rounded,
                      size: 18.r, color: AppColors.primary),
                ),
                SizedBox(width: 10.w),
                Text(
                  'VISIT INFO',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 12.sp,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 2.0,
                    color: AppColors.foreground,
                  ),
                ),
              ],
            ),
          ),
          Padding(
            padding: EdgeInsets.all(16.r),
            child: Column(
              children: [
                _InfoRow(
                    label: 'Record No',
                    value: detail.notBillingNumber,
                    mono: true),
                _InfoRow(
                    label: 'Date',
                    value: formatDate(detail.notBillingDate)),
                _InfoRow(label: 'Outlet', value: detail.outletName),
                _InfoRow(label: 'Sales Rep', value: detail.salesRepName),
                if (detail.supervisorName != null)
                  _InfoRow(
                      label: 'Supervisor',
                      value: detail.supervisorName!),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ── Reason card ───────────────────────────────────────────────────────────────

class _ReasonCard extends StatelessWidget {
  final NotBillingReason reason;
  const _ReasonCard({required this.reason});

  _ReasonInfo _info(NotBillingReason r) {
    switch (r) {
      case NotBillingReason.outletClosed:
        return _ReasonInfo(
          label: 'Outlet Closed',
          description: 'The outlet was physically closed during the visit.',
          color: AppColors.warning,
          icon: Icons.store_outlined,
        );
      case NotBillingReason.ownerAbsent:
        return _ReasonInfo(
          label: 'Owner Absent',
          description: 'The outlet was open but the decision-maker was unavailable.',
          color: Colors.blue.shade600,
          icon: Icons.person_off_outlined,
        );
      case NotBillingReason.creditIssue:
        return _ReasonInfo(
          label: 'Credit Issue',
          description: 'Outstanding payment or credit dispute prevented a sale.',
          color: AppColors.error,
          icon: Icons.credit_card_off_outlined,
        );
      case NotBillingReason.noOrder:
        return _ReasonInfo(
          label: 'No Order',
          description: 'Owner was present and had sufficient stock but placed no order.',
          color: AppColors.foregroundMuted,
          icon: Icons.shopping_cart_outlined,
        );
      case NotBillingReason.outOfStock:
        return _ReasonInfo(
          label: 'Out of Stock',
          description: "The outlet's own stock was depleted, so no purchase was needed.",
          color: AppColors.amber,
          icon: Icons.inventory_2_outlined,
        );
    }
  }

  @override
  Widget build(BuildContext context) {
    final info = _info(reason);
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16.r),
        boxShadow: [
          BoxShadow(
            color: const Color(0xFF1A1A11).withValues(alpha: 0.06),
            blurRadius: 16,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        children: [
          Container(
            padding: EdgeInsets.fromLTRB(16.w, 14.h, 16.w, 12.h),
            decoration: const BoxDecoration(
              border: Border(
                  bottom: BorderSide(color: Color(0xFFEEEDE6))),
            ),
            child: Row(
              children: [
                Container(
                  width: 36.r,
                  height: 36.r,
                  decoration: BoxDecoration(
                    color: info.color.withValues(alpha: 0.10),
                    borderRadius: BorderRadius.circular(10.r),
                  ),
                  child:
                      Icon(info.icon, size: 18.r, color: info.color),
                ),
                SizedBox(width: 10.w),
                Text(
                  'NON-BILLING REASON',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 12.sp,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 2.0,
                    color: AppColors.foreground,
                  ),
                ),
              ],
            ),
          ),
          Padding(
            padding: EdgeInsets.all(16.r),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Container(
                  padding: EdgeInsets.symmetric(
                      horizontal: 10.w, vertical: 5.h),
                  decoration: BoxDecoration(
                    color: info.color.withValues(alpha: 0.10),
                    borderRadius: BorderRadius.circular(20.r),
                    border: Border.all(
                        color: info.color.withValues(alpha: 0.25)),
                  ),
                  child: Text(
                    info.label,
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 13.sp,
                      fontWeight: FontWeight.w800,
                      letterSpacing: 0.5,
                      color: info.color,
                    ),
                  ),
                ),
                SizedBox(width: 12.w),
                Expanded(
                  child: Text(
                    info.description,
                    style: GoogleFonts.barlow(
                      fontSize: 12.sp,
                      color: AppColors.foregroundMuted,
                      height: 1.5,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _ReasonInfo {
  final String label;
  final String description;
  final Color color;
  final IconData icon;
  const _ReasonInfo({
    required this.label,
    required this.description,
    required this.color,
    required this.icon,
  });
}

// ── Info row ──────────────────────────────────────────────────────────────────

class _InfoRow extends StatelessWidget {
  final String label;
  final String value;
  final bool mono;

  const _InfoRow({required this.label, required this.value, this.mono = false});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.symmetric(vertical: 5.h),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 100.w,
            child: Text(label,
                style: GoogleFonts.barlow(
                    fontSize: 12.sp, color: AppColors.foregroundMuted)),
          ),
          Expanded(
            child: Text(
              value,
              textAlign: TextAlign.right,
              style: mono
                  ? GoogleFonts.robotoMono(
                      fontSize: 12.sp,
                      fontWeight: FontWeight.w600,
                      color: AppColors.foreground)
                  : GoogleFonts.barlow(
                      fontSize: 12.sp,
                      fontWeight: FontWeight.w500,
                      color: AppColors.foreground),
            ),
          ),
        ],
      ),
    );
  }
}

// ── Notes card ────────────────────────────────────────────────────────────────

class _NotesCard extends StatelessWidget {
  final String notes;
  const _NotesCard({required this.notes});

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: EdgeInsets.all(16.r),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16.r),
        boxShadow: [
          BoxShadow(
            color: const Color(0xFF1A1A11).withValues(alpha: 0.06),
            blurRadius: 16,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('NOTES',
              style: GoogleFonts.barlowCondensed(
                  fontSize: 11.sp,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 2.0,
                  color: AppColors.foregroundMuted)),
          SizedBox(height: 8.h),
          Text(notes,
              style: GoogleFonts.barlow(
                  fontSize: 13.sp,
                  color: AppColors.foreground,
                  height: 1.5)),
        ],
      ),
    );
  }
}
