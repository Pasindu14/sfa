import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/route_unlock/domain/entities/route_unlock_request.dart';

class UnlockStatusStyle {
  final String label;
  final Color color;
  const UnlockStatusStyle(this.label, this.color);
}

/// Label and colour for a request, from its effectiveStatus. An Approved
/// request that has since lapsed reads as Expired, not Approved.
UnlockStatusStyle unlockStatusStyle(RouteUnlockRequest r) {
  if (r.isPending) return const UnlockStatusStyle('PENDING', AppColors.warning);
  if (r.isLive) return const UnlockStatusStyle('UNLOCKED', AppColors.success);
  return switch (r.effectiveStatus) {
    RouteUnlockStatus.rejected =>
      const UnlockStatusStyle('REJECTED', AppColors.error),
    RouteUnlockStatus.revoked =>
      const UnlockStatusStyle('REVOKED', AppColors.error),
    RouteUnlockStatus.cancelled =>
      const UnlockStatusStyle('CANCELLED', AppColors.foregroundMuted),
    RouteUnlockStatus.expired =>
      const UnlockStatusStyle('EXPIRED', AppColors.foregroundMuted),
    RouteUnlockStatus.approved =>
      const UnlockStatusStyle('APPROVED', AppColors.success),
    final other => UnlockStatusStyle(other.toUpperCase(), AppColors.foregroundMuted),
  };
}

class UnlockStatusPill extends StatelessWidget {
  final UnlockStatusStyle style;
  const UnlockStatusPill({super.key, required this.style});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 3.h),
      decoration: BoxDecoration(
        color: style.color.withValues(alpha: 0.10),
        borderRadius: BorderRadius.circular(20.r),
      ),
      child: Text(
        style.label,
        style: GoogleFonts.barlowCondensed(
          fontSize: 10.sp,
          fontWeight: FontWeight.w800,
          letterSpacing: 1,
          color: style.color,
        ),
      ),
    );
  }
}

/// `HH:mm` on the device clock — instants arrive as UTC, reps and supervisors
/// think in Sri Lanka time.
String formatUnlockTime(DateTime utc) {
  final t = utc.toLocal();
  return '${t.hour.toString().padLeft(2, '0')}:'
      '${t.minute.toString().padLeft(2, '0')}';
}

/// Where the rep was when they asked, as far as the fix goes.
String describeUnlockLocation(RouteUnlockRequest r) {
  if (r.requestLatitude == null || r.requestLongitude == null) {
    return 'No location sent';
  }
  final acc = r.requestGpsAccuracyMeters;
  final coords = '${r.requestLatitude!.toStringAsFixed(5)}, '
      '${r.requestLongitude!.toStringAsFixed(5)}';
  return acc == null ? coords : '$coords  (±${acc.toStringAsFixed(0)} m)';
}
