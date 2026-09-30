import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/route_unlock/domain/entities/route_unlock_detail.dart';
import 'package:uswatte/features/route_unlock/domain/entities/route_unlock_request.dart';
import 'package:uswatte/features/route_unlock/presentation/cubit/unlock_requests_cubit.dart';
import 'package:uswatte/features/route_unlock/presentation/route_unlock_messages.dart';
import 'package:uswatte/features/route_unlock/presentation/widgets/unlock_status_style.dart';

/// Opens a request's detail. Resolves to a message for the page to show once
/// the sheet has closed (after a decision, or after the request turned out to
/// be stale), or null when the supervisor just looked.
Future<String?> showUnlockRequestDetailSheet(
  BuildContext context, {
  required UnlockRequestsCubit cubit,
  required RouteUnlockRequest request,
}) {
  return showModalBottomSheet<String>(
    context: context,
    isScrollControlled: true,
    backgroundColor: Colors.white,
    shape: RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(20.r)),
    ),
    builder: (_) => _DetailSheet(cubit: cubit, initial: request),
  );
}

enum _Decision { approve, reject, revoke }

class _DetailSheet extends StatefulWidget {
  final UnlockRequestsCubit cubit;
  final RouteUnlockRequest initial;

  const _DetailSheet({required this.cubit, required this.initial});

  @override
  State<_DetailSheet> createState() => _DetailSheetState();
}

class _DetailSheetState extends State<_DetailSheet> {
  RouteUnlockDetail? _detail;
  String? _loadError;
  _Decision? _deciding;
  bool _busy = false;
  String? _actionError;
  final _textController = TextEditingController();

  /// The freshest copy — its rowVersion is what the server will check.
  RouteUnlockRequest get _request => _detail?.request ?? widget.initial;

  @override
  void initState() {
    super.initState();
    _textController.addListener(() => setState(() {}));
    _load();
  }

  @override
  void dispose() {
    _textController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() => _loadError = null);
    try {
      final detail = await widget.cubit.loadDetail(widget.initial.id);
      if (mounted) setState(() => _detail = detail);
    } on AppException catch (e) {
      if (mounted) setState(() => _loadError = routeUnlockErrorMessage(e));
    }
  }

  bool get _canSubmit {
    if (_busy || _deciding == null) return false;
    final text = _textController.text.trim();
    if (text.length > 500) return false;
    // Approve takes an optional note; reject and revoke need a reason.
    return _deciding == _Decision.approve || text.length >= 3;
  }

  Future<void> _submit() async {
    final decision = _deciding;
    if (decision == null || !_canSubmit) return;
    setState(() {
      _busy = true;
      _actionError = null;
    });
    final text = _textController.text.trim();
    final result = switch (decision) {
      _Decision.approve => await widget.cubit
          .approve(_request, note: text.isEmpty ? null : text),
      _Decision.reject => await widget.cubit.reject(_request, text),
      _Decision.revoke => await widget.cubit.revoke(_request, text),
    };
    if (!mounted) return;
    if (result.success || result.stale) {
      // Stale: the list has already reloaded; this sheet's copy is out of date,
      // so close it rather than let the supervisor act on it again.
      Navigator.of(context).pop(result.message);
      return;
    }
    setState(() {
      _busy = false;
      _actionError = result.message;
    });
  }

  @override
  Widget build(BuildContext context) {
    final r = _request;
    final style = unlockStatusStyle(r);

    return FractionallySizedBox(
      heightFactor: 0.9,
      child: Padding(
        padding: EdgeInsets.only(
            bottom: MediaQuery.of(context).viewInsets.bottom),
        child: Column(
          children: [
            SizedBox(height: 10.h),
            Container(
              width: 40.w,
              height: 4.h,
              decoration: BoxDecoration(
                color: AppColors.surfaceVariant,
                borderRadius: BorderRadius.circular(2.r),
              ),
            ),
            Expanded(
              child: ListView(
                padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 16.h),
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          r.userName ?? r.loginName ?? 'Rep #${r.userId}',
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 20.sp,
                            fontWeight: FontWeight.w800,
                            color: AppColors.foreground,
                          ),
                        ),
                      ),
                      UnlockStatusPill(style: style),
                    ],
                  ),
                  SizedBox(height: 12.h),
                  _Fact('Route', r.routeName ?? 'Route #${r.routeId}'),
                  _Fact('Date', r.businessDate),
                  _Fact('Asked at', formatUnlockTime(r.requestedAt)),
                  _Fact('Reason', r.requestReason),
                  _Fact('Location', describeUnlockLocation(r)),
                  if (r.supervisorName != null)
                    _Fact('Sent to', r.supervisorName!),
                  if (r.validFrom != null && r.validTo != null)
                    _Fact(
                      'Unlocked',
                      '${formatUnlockTime(r.validFrom!)} until midnight',
                    ),
                  _SectionHeader('TIMELINE'),
                  if (_detail == null && _loadError == null)
                    Padding(
                      padding: EdgeInsets.symmetric(vertical: 12.h),
                      child: const Center(
                        child: CircularProgressIndicator(
                            color: AppColors.primary),
                      ),
                    )
                  else if (_loadError != null)
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            _loadError!,
                            style: GoogleFonts.barlow(
                                fontSize: 12.sp, color: AppColors.error),
                          ),
                        ),
                        TextButton(onPressed: _load, child: const Text('Retry')),
                      ],
                    )
                  else ...[
                    for (final e in _detail!.events) _TimelineRow(event: e),
                    _SectionHeader('BILLS USING THIS UNLOCK'),
                    if (_detail!.bills.isEmpty)
                      Text(
                        'None yet.',
                        style: GoogleFonts.barlow(
                            fontSize: 12.sp, color: AppColors.foregroundMuted),
                      )
                    else
                      for (final b in _detail!.bills) _BillRow(bill: b),
                  ],
                  _decisionSection(r),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _decisionSection(RouteUnlockRequest r) {
    final canDecide = r.isPending;
    final canRevoke = r.isLive;
    if (!canDecide && !canRevoke) return const SizedBox.shrink();

    final deciding = _deciding;
    if (deciding == null) {
      return Padding(
        padding: EdgeInsets.only(top: 20.h),
        child: canDecide
            ? Row(
                children: [
                  Expanded(
                    child: _ActionButton(
                      label: 'REJECT',
                      color: AppColors.error,
                      outlined: true,
                      onTap: () => setState(() => _deciding = _Decision.reject),
                    ),
                  ),
                  SizedBox(width: 12.w),
                  Expanded(
                    flex: 2,
                    child: _ActionButton(
                      label: 'APPROVE',
                      color: AppColors.success,
                      outlined: false,
                      onTap: () =>
                          setState(() => _deciding = _Decision.approve),
                    ),
                  ),
                ],
              )
            : _ActionButton(
                label: 'REVOKE UNLOCK',
                color: AppColors.error,
                outlined: true,
                onTap: () => setState(() => _deciding = _Decision.revoke),
              ),
      );
    }

    final (title, hint, color, confirm) = switch (deciding) {
      _Decision.approve => (
          'APPROVE — NOTE (OPTIONAL)',
          'Anything the rep should know...',
          AppColors.success,
          'Confirm Approval',
        ),
      _Decision.reject => (
          'REJECTION REASON',
          'Tell the rep why...',
          AppColors.error,
          'Confirm Rejection',
        ),
      _Decision.revoke => (
          'REVOKE REASON',
          'Tell the rep why the unlock is withdrawn...',
          AppColors.error,
          'Confirm Revoke',
        ),
    };

    return Container(
      margin: EdgeInsets.only(top: 20.h),
      padding: EdgeInsets.all(16.r),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16.r),
        border: Border.all(color: color.withValues(alpha: 0.3)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            title,
            style: GoogleFonts.barlowCondensed(
              fontSize: 11.sp,
              fontWeight: FontWeight.w800,
              letterSpacing: 1.5,
              color: color,
            ),
          ),
          SizedBox(height: 10.h),
          TextField(
            key: const ValueKey('unlock-decision-text'),
            controller: _textController,
            minLines: 2,
            maxLines: 4,
            maxLength: 500,
            textCapitalization: TextCapitalization.sentences,
            decoration: InputDecoration(
              hintText: hint,
              hintStyle: GoogleFonts.barlow(
                  fontSize: 12.sp, color: AppColors.foregroundMuted),
              filled: true,
              fillColor: color.withValues(alpha: 0.04),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10.r),
                borderSide: BorderSide(color: color.withValues(alpha: 0.3)),
              ),
              contentPadding: EdgeInsets.all(12.r),
            ),
            style:
                GoogleFonts.barlow(fontSize: 12.sp, color: AppColors.foreground),
          ),
          if (_actionError != null) ...[
            Text(
              _actionError!,
              style: GoogleFonts.barlow(
                fontSize: 12.sp,
                fontWeight: FontWeight.w600,
                color: AppColors.error,
              ),
            ),
            SizedBox(height: 8.h),
          ],
          Row(
            children: [
              Expanded(
                child: TextButton(
                  onPressed: _busy
                      ? null
                      : () => setState(() {
                            _deciding = null;
                            _actionError = null;
                            _textController.clear();
                          }),
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
                  onPressed: _canSubmit ? _submit : null,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: color,
                    disabledBackgroundColor: color.withValues(alpha: 0.35),
                    padding: EdgeInsets.symmetric(vertical: 13.h),
                    elevation: 0,
                    shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(10.r)),
                  ),
                  child: _busy
                      ? SizedBox(
                          width: 18.r,
                          height: 18.r,
                          child: const CircularProgressIndicator(
                              strokeWidth: 2, color: Colors.white),
                        )
                      : Text(
                          confirm,
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 14.sp,
                            fontWeight: FontWeight.w800,
                            color: Colors.white,
                          ),
                        ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _Fact extends StatelessWidget {
  final String label;
  final String value;
  const _Fact(this.label, this.value);

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(bottom: 6.h),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 80.w,
            child: Text(
              label,
              style: GoogleFonts.barlow(
                  fontSize: 12.sp, color: AppColors.foregroundMuted),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: GoogleFonts.barlow(
                fontSize: 13.sp,
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

class _SectionHeader extends StatelessWidget {
  final String text;
  const _SectionHeader(this.text);

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(top: 18.h, bottom: 8.h),
      child: Text(
        text,
        style: GoogleFonts.barlowCondensed(
          fontSize: 11.sp,
          fontWeight: FontWeight.w700,
          letterSpacing: 2.5,
          color: AppColors.foregroundMuted,
        ),
      ),
    );
  }
}

class _TimelineRow extends StatelessWidget {
  final RouteUnlockEvent event;
  const _TimelineRow({required this.event});

  @override
  Widget build(BuildContext context) {
    final who = event.performedByName ?? 'User #${event.performedByUserId}';
    final note = event.note;
    return Padding(
      padding: EdgeInsets.only(bottom: 10.h),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            margin: EdgeInsets.only(top: 5.h),
            width: 8.r,
            height: 8.r,
            decoration: const BoxDecoration(
              color: AppColors.primary,
              shape: BoxShape.circle,
            ),
          ),
          SizedBox(width: 10.w),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  '${event.action} · ${formatUnlockTime(event.performedAt)}',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 14.sp,
                    fontWeight: FontWeight.w700,
                    color: AppColors.foreground,
                  ),
                ),
                Text(
                  event.performedByRole.isEmpty
                      ? who
                      : '$who (${event.performedByRole})',
                  style: GoogleFonts.barlow(
                      fontSize: 12.sp, color: AppColors.foregroundMuted),
                ),
                if (note != null && note.isNotEmpty)
                  Text(
                    '"$note"',
                    style: GoogleFonts.barlow(
                        fontSize: 12.sp, color: AppColors.foreground),
                  ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _BillRow extends StatelessWidget {
  final RouteUnlockBill bill;
  const _BillRow({required this.bill});

  @override
  Widget build(BuildContext context) {
    final distance = bill.distanceFromOutletMeters;
    final distanceLabel = distance == null
        ? null
        : distance >= 1000
            ? '${(distance / 1000).toStringAsFixed(1)} km away'
            : '${distance.toStringAsFixed(0)} m away';
    return Padding(
      padding: EdgeInsets.only(bottom: 8.h),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  bill.outletName,
                  style: GoogleFonts.barlow(
                    fontSize: 13.sp,
                    fontWeight: FontWeight.w600,
                    color: AppColors.foreground,
                  ),
                ),
                Text(
                  [
                    bill.billingNumber ?? 'Bill #${bill.billingId}',
                    if (distanceLabel != null) distanceLabel,
                  ].join('  ·  '),
                  style: GoogleFonts.barlow(
                      fontSize: 11.sp, color: AppColors.foregroundMuted),
                ),
              ],
            ),
          ),
          Text(
            'Rs ${bill.totalAmount.toStringAsFixed(2)}',
            style: GoogleFonts.barlowCondensed(
              fontSize: 14.sp,
              fontWeight: FontWeight.w700,
              color: AppColors.foreground,
            ),
          ),
        ],
      ),
    );
  }
}

class _ActionButton extends StatelessWidget {
  final String label;
  final Color color;
  final bool outlined;
  final VoidCallback onTap;

  const _ActionButton({
    required this.label,
    required this.color,
    required this.outlined,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final text = Text(
      label,
      style: GoogleFonts.barlowCondensed(
        fontSize: 14.sp,
        fontWeight: FontWeight.w800,
        letterSpacing: 0.5,
        color: outlined ? color : Colors.white,
      ),
    );
    final shape =
        RoundedRectangleBorder(borderRadius: BorderRadius.circular(10.r));
    final padding = EdgeInsets.symmetric(vertical: 13.h);
    return outlined
        ? OutlinedButton(
            onPressed: onTap,
            style: OutlinedButton.styleFrom(
              side: BorderSide(color: color.withValues(alpha: 0.6)),
              padding: padding,
              shape: shape,
            ),
            child: text,
          )
        : ElevatedButton(
            onPressed: onTap,
            style: ElevatedButton.styleFrom(
              backgroundColor: color,
              elevation: 0,
              padding: padding,
              shape: shape,
            ),
            child: text,
          );
  }
}
