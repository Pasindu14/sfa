import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/route_unlock/domain/entities/route_unlock_request.dart';
import 'package:uswatte/features/route_unlock/presentation/cubit/route_unlock_cubit.dart';

/// Opens the "Request unlock" sheet. [latitude]/[longitude]/[gpsAccuracyMeters]
/// are the fix the bill screen already holds; the supervisor sees where the rep
/// was when they asked.
Future<void> showRouteUnlockRequestSheet(
  BuildContext context, {
  required RouteUnlockCubit cubit,
  double? latitude,
  double? longitude,
  double? gpsAccuracyMeters,
}) {
  return showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    backgroundColor: Colors.white,
    shape: RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(20.r)),
    ),
    builder: (_) => _RequestUnlockSheet(
      cubit: cubit,
      latitude: latitude,
      longitude: longitude,
      gpsAccuracyMeters: gpsAccuracyMeters,
    ),
  );
}

class _RequestUnlockSheet extends StatefulWidget {
  final RouteUnlockCubit cubit;
  final double? latitude;
  final double? longitude;
  final double? gpsAccuracyMeters;

  const _RequestUnlockSheet({
    required this.cubit,
    this.latitude,
    this.longitude,
    this.gpsAccuracyMeters,
  });

  @override
  State<_RequestUnlockSheet> createState() => _RequestUnlockSheetState();
}

class _RequestUnlockSheetState extends State<_RequestUnlockSheet> {
  static const _quickReasons = [
    'GPS not accurate',
    'Outlet location wrong',
    'Need to cover the full route',
  ];

  final _controller = TextEditingController();
  String? _error;
  bool _sending = false;

  @override
  void initState() {
    super.initState();
    _controller.addListener(() => setState(() {}));
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  bool get _valid {
    final length = _controller.text.trim().length;
    return length >= 3 && length <= 500;
  }

  Future<void> _submit() async {
    if (!_valid || _sending) return;
    setState(() {
      _sending = true;
      _error = null;
    });
    final error = await widget.cubit.requestUnlock(
      reason: _controller.text,
      latitude: widget.latitude,
      longitude: widget.longitude,
      gpsAccuracyMeters: widget.gpsAccuracyMeters,
    );
    if (!mounted) return;
    if (error == null) {
      Navigator.of(context).pop();
    } else {
      setState(() {
        _sending = false;
        _error = error;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(
        left: 16.w,
        right: 16.w,
        top: 10.h,
        bottom: MediaQuery.of(context).viewInsets.bottom + 16.h,
      ),
      child: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Center(
              child: Container(
                width: 40.w,
                height: 4.h,
                decoration: BoxDecoration(
                  color: AppColors.surfaceVariant,
                  borderRadius: BorderRadius.circular(2.r),
                ),
              ),
            ),
            SizedBox(height: 16.h),
            Text(
              'REQUEST ROUTE UNLOCK',
              style: GoogleFonts.barlowCondensed(
                fontSize: 16.sp,
                fontWeight: FontWeight.w800,
                letterSpacing: 1.5,
                color: AppColors.foreground,
              ),
            ),
            SizedBox(height: 4.h),
            Text(
              'Your supervisor will be asked to let you bill every outlet on '
              'today\'s route, whatever the distance, until midnight.',
              style: GoogleFonts.barlow(
                fontSize: 12.sp,
                height: 1.4,
                color: AppColors.foregroundMuted,
              ),
            ),
            SizedBox(height: 14.h),
            Wrap(
              spacing: 8.w,
              runSpacing: 8.h,
              children: [
                for (final reason in _quickReasons)
                  ActionChip(
                    label: Text(
                      reason,
                      style: GoogleFonts.barlow(
                        fontSize: 12.sp,
                        fontWeight: FontWeight.w600,
                        color: AppColors.primary,
                      ),
                    ),
                    backgroundColor: AppColors.primary.withValues(alpha: 0.08),
                    side: BorderSide(
                        color: AppColors.primary.withValues(alpha: 0.25)),
                    onPressed: () {
                      _controller.text = reason;
                      _controller.selection = TextSelection.collapsed(
                          offset: reason.length);
                    },
                  ),
              ],
            ),
            SizedBox(height: 12.h),
            TextField(
              key: const ValueKey('route-unlock-reason'),
              controller: _controller,
              minLines: 2,
              maxLines: 4,
              maxLength: 500,
              textCapitalization: TextCapitalization.sentences,
              decoration: InputDecoration(
                hintText: 'Why do you need the route unlocked?',
                hintStyle: GoogleFonts.barlow(
                    fontSize: 12.sp, color: AppColors.foregroundMuted),
                filled: true,
                fillColor: AppColors.surface,
                contentPadding: EdgeInsets.all(12.r),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(10.r),
                  borderSide: BorderSide.none,
                ),
              ),
              style:
                  GoogleFonts.barlow(fontSize: 13.sp, color: AppColors.foreground),
            ),
            if (widget.latitude == null || widget.longitude == null) ...[
              Text(
                'Your location is not available yet; the request will be sent without it.',
                style: GoogleFonts.barlow(
                    fontSize: 11.sp, color: AppColors.foregroundMuted),
              ),
              SizedBox(height: 8.h),
            ],
            if (_error != null) ...[
              Text(
                _error!,
                key: const ValueKey('route-unlock-request-error'),
                style: GoogleFonts.barlow(
                  fontSize: 12.sp,
                  fontWeight: FontWeight.w600,
                  color: AppColors.error,
                ),
              ),
              SizedBox(height: 8.h),
            ],
            SizedBox(height: 4.h),
            ElevatedButton(
              onPressed: _valid && !_sending ? _submit : null,
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.primary,
                disabledBackgroundColor:
                    AppColors.primary.withValues(alpha: 0.35),
                padding: EdgeInsets.symmetric(vertical: 14.h),
                elevation: 0,
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(10.r)),
              ),
              child: _sending
                  ? SizedBox(
                      width: 18.r,
                      height: 18.r,
                      child: const CircularProgressIndicator(
                          strokeWidth: 2, color: Colors.white),
                    )
                  : Text(
                      'SEND REQUEST',
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 15.sp,
                        fontWeight: FontWeight.w800,
                        letterSpacing: 1,
                        color: Colors.white,
                      ),
                    ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Status of today's unlock request, shown in the outlet picker while the
/// distance check is on. Renders nothing when there is nothing to say.
class RouteUnlockStrip extends StatelessWidget {
  final RouteUnlockCubit cubit;

  /// Opens the request sheet (for "Request again").
  final VoidCallback onRequest;

  const RouteUnlockStrip({
    super.key,
    required this.cubit,
    required this.onRequest,
  });

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<RouteUnlockCubit, RouteUnlockState>(
      bloc: cubit,
      builder: (context, state) {
        final content = _content(state.request);
        final error = state.error;
        if (content == null && error == null) return const SizedBox.shrink();

        return Padding(
          padding: EdgeInsets.fromLTRB(16.w, 10.h, 16.w, 0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (content != null)
                _StripCard(
                  key: const ValueKey('route-unlock-strip'),
                  content: content,
                  busy: state.submitting,
                  onAction: switch (content.action) {
                    _StripAction.cancel => () => cubit.cancel(),
                    _StripAction.requestAgain => onRequest,
                    _StripAction.none => null,
                  },
                ),
              if (error != null) ...[
                SizedBox(height: 6.h),
                Text(
                  error,
                  style: GoogleFonts.barlow(
                    fontSize: 11.sp,
                    fontWeight: FontWeight.w600,
                    color: AppColors.error,
                  ),
                ),
              ],
            ],
          ),
        );
      },
    );
  }

  static _StripContent? _content(RouteUnlockRequest? r) {
    if (r == null) return null;
    if (r.isPending) {
      return _StripContent(
        icon: Icons.hourglass_top_rounded,
        color: AppColors.warning,
        title: 'Unlock requested',
        body: 'Waiting for ${r.supervisorName ?? 'approval'}',
        action: _StripAction.cancel,
      );
    }
    if (r.isLive) {
      // Approved, but this device still filters by distance: the page is
      // re-syncing outlets. Once the policy lands the exemption banner takes
      // over.
      return _StripContent(
        icon: Icons.my_location_rounded,
        color: AppColors.success,
        title: 'Unlock approved',
        body: r.reviewedByName == null
            ? 'Refreshing your outlets. Reopen this list in a moment.'
            : 'Approved by ${r.reviewedByName}. Refreshing your outlets. '
                'Reopen this list in a moment.',
        action: _StripAction.none,
      );
    }
    if (r.isRejected) {
      final by = r.reviewedByName ?? 'your supervisor';
      final note = r.reviewNote;
      return _StripContent(
        icon: Icons.block_rounded,
        color: AppColors.error,
        title: 'Unlock rejected',
        body: note == null || note.isEmpty
            ? 'Rejected by $by'
            : 'Rejected by $by: $note',
        action: _StripAction.requestAgain,
      );
    }
    if (r.isRevoked) {
      final by = r.revokedByName;
      return _StripContent(
        icon: Icons.lock_outline_rounded,
        color: AppColors.error,
        title: 'Unlock revoked',
        body: by == null ? 'Your unlock was withdrawn.' : 'Withdrawn by $by.',
        action: _StripAction.requestAgain,
      );
    }
    if (r.isExpired) {
      return const _StripContent(
        icon: Icons.schedule_rounded,
        color: AppColors.foregroundMuted,
        title: 'Request expired',
        body: 'Your last unlock request is no longer valid.',
        action: _StripAction.requestAgain,
      );
    }
    // Cancelled: the header's Request unlock button is enough.
    return null;
  }
}

enum _StripAction { none, cancel, requestAgain }

class _StripContent {
  final IconData icon;
  final Color color;
  final String title;
  final String body;
  final _StripAction action;

  const _StripContent({
    required this.icon,
    required this.color,
    required this.title,
    required this.body,
    required this.action,
  });
}

class _StripCard extends StatelessWidget {
  final _StripContent content;
  final bool busy;
  final VoidCallback? onAction;

  const _StripCard({
    super.key,
    required this.content,
    required this.busy,
    required this.onAction,
  });

  @override
  Widget build(BuildContext context) {
    final color = content.color;
    final label = switch (content.action) {
      _StripAction.cancel => 'Cancel',
      _StripAction.requestAgain => 'Request again',
      _StripAction.none => null,
    };

    return Container(
      padding: EdgeInsets.fromLTRB(12.w, 10.h, 6.w, 10.h),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(12.r),
        border: Border.all(color: color.withValues(alpha: 0.35)),
      ),
      child: Row(
        children: [
          Icon(content.icon, size: 18.r, color: color),
          SizedBox(width: 10.w),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  content.title,
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 13.sp,
                    fontWeight: FontWeight.w700,
                    color: color,
                  ),
                ),
                SizedBox(height: 2.h),
                Text(
                  content.body,
                  style: GoogleFonts.barlow(
                    fontSize: 12.sp,
                    height: 1.3,
                    color: AppColors.foregroundMuted,
                  ),
                ),
              ],
            ),
          ),
          if (label != null)
            busy
                ? Padding(
                    padding: EdgeInsets.symmetric(horizontal: 12.w),
                    child: SizedBox(
                      width: 16.r,
                      height: 16.r,
                      child: CircularProgressIndicator(
                          strokeWidth: 2, color: color),
                    ),
                  )
                : TextButton(
                    onPressed: onAction,
                    child: Text(
                      label,
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 13.sp,
                        fontWeight: FontWeight.w700,
                        color: AppColors.primary,
                      ),
                    ),
                  ),
        ],
      ),
    );
  }
}
