import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/outlets/domain/entities/outlet.dart';
import 'package:uswatte/features/outlets/domain/usecases/filter_nearby_outlets.dart';

/// Tappable card that opens an outlet bottom-sheet.
class OutletPicker extends StatelessWidget {
  final Outlet? selected;
  final List<Outlet> outlets;
  final ValueChanged<Outlet> onSelected;
  final bool hasActiveAssignment;
  final double? repLat;
  final double? repLng;
  final double radiusMeters;

  /// False while an admin-granted proximity exemption is in force, in which case
  /// the distance filter is skipped and the whole route is offered.
  final bool proximityEnforced;

  /// When the exemption lapses — shown to the rep so the relaxed behaviour is
  /// visible and dated rather than mysterious.
  final DateTime? exemptionUntil;

  const OutletPicker({
    super.key,
    required this.selected,
    required this.outlets,
    required this.onSelected,
    this.hasActiveAssignment = true,
    this.repLat,
    this.repLng,
    required this.radiusMeters,
    this.proximityEnforced = true,
    this.exemptionUntil,
  });

  @override
  Widget build(BuildContext context) {
    if (!hasActiveAssignment) return _lockedCard();

    return GestureDetector(
      onTap: () => _openSheet(context),
      child: Container(
        padding: EdgeInsets.all(16.r),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(14.r),
          border: Border.all(
            color: selected != null
                ? AppColors.primary.withValues(alpha: 0.35)
                : AppColors.surfaceVariant,
          ),
          boxShadow: [
            BoxShadow(
              color: AppColors.foreground.withValues(alpha: 0.04),
              blurRadius: 10,
              offset: const Offset(0, 3),
            ),
          ],
        ),
        child: Row(
          children: [
            Container(
              width: 38.r,
              height: 38.r,
              decoration: BoxDecoration(
                color: AppColors.primary.withValues(alpha: 0.10),
                borderRadius: BorderRadius.circular(10.r),
              ),
              child: Icon(Icons.storefront_rounded,
                  size: 18.r, color: AppColors.primary),
            ),
            SizedBox(width: 12.w),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    selected != null ? 'OUTLET' : 'SELECT OUTLET',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 9.sp,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 2.0,
                      color: selected != null
                          ? AppColors.primary
                          : AppColors.foregroundMuted,
                    ),
                  ),
                  SizedBox(height: 2.h),
                  Text(
                    selected?.name ?? 'Tap to choose today\'s outlet',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: selected != null ? 16.sp : 14.sp,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 0.3,
                      height: 1.1,
                      color: selected != null
                          ? AppColors.foreground
                          : AppColors.foregroundMuted,
                    ),
                  ),
                  if (selected != null) ...[
                    SizedBox(height: 2.h),
                    Text(
                      selected!.address,
                      style: GoogleFonts.barlow(
                        fontSize: 11.sp,
                        color: AppColors.foregroundMuted,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ],
                ],
              ),
            ),
            Container(
              width: 28.r,
              height: 28.r,
              decoration: BoxDecoration(
                color: AppColors.surface,
                borderRadius: BorderRadius.circular(6.r),
              ),
              child: Icon(
                selected != null
                    ? Icons.swap_horiz_rounded
                    : Icons.keyboard_arrow_down_rounded,
                size: 15.r,
                color: AppColors.foregroundMuted,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _lockedCard() {
    return Container(
      padding: EdgeInsets.all(16.r),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(14.r),
        border: Border.all(color: AppColors.surfaceVariant),
      ),
      child: Row(
        children: [
          Container(
            width: 38.r,
            height: 38.r,
            decoration: BoxDecoration(
              color: AppColors.surfaceVariant,
              borderRadius: BorderRadius.circular(10.r),
            ),
            child: Icon(Icons.lock_outline_rounded,
                size: 17.r, color: AppColors.foregroundMuted),
          ),
          SizedBox(width: 12.w),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'OUTLET UNAVAILABLE',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 9.sp,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 2.0,
                    color: AppColors.foregroundMuted,
                  ),
                ),
                SizedBox(height: 2.h),
                Text(
                  'No route assigned for today',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 14.sp,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 0.3,
                    height: 1.1,
                    color: AppColors.foregroundMuted,
                  ),
                ),
                SizedBox(height: 2.h),
                Text(
                  'Orders can only be created on assigned routes.',
                  style: GoogleFonts.barlow(
                    fontSize: 10.sp,
                    color: AppColors.foregroundMuted.withValues(alpha: 0.7),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _openSheet(BuildContext context) async {
    final picked = await showModalBottomSheet<Outlet>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.white,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20.r)),
      ),
      builder: (ctx) => _OutletSheet(
            outlets: outlets,
            selected: selected,
            hasActiveAssignment: hasActiveAssignment,
            repLat: repLat,
            repLng: repLng,
            radiusMeters: radiusMeters,
            proximityEnforced: proximityEnforced,
            exemptionUntil: exemptionUntil,
          ),
    );
    if (picked != null) onSelected(picked);
  }
}

class _OutletSheet extends StatefulWidget {
  final List<Outlet> outlets;
  final Outlet? selected;
  final bool hasActiveAssignment;
  final double? repLat;
  final double? repLng;
  final double radiusMeters;
  final bool proximityEnforced;
  final DateTime? exemptionUntil;

  const _OutletSheet({
    required this.outlets,
    required this.selected,
    required this.hasActiveAssignment,
    required this.repLat,
    required this.repLng,
    required this.radiusMeters,
    required this.proximityEnforced,
    required this.exemptionUntil,
  });

  @override
  State<_OutletSheet> createState() => _OutletSheetState();
}

class _OutletSheetState extends State<_OutletSheet> {
  String _query = '';

  /// Outlets filtered by proximity, sorted nearest-first.
  /// Computed once when the sheet opens; text search narrows within this list.
  late final List<NearbyOutlet> _nearby;

  @override
  void initState() {
    super.initState();
    final repLat = widget.repLat;
    final repLng = widget.repLng;

    if (repLat != null && repLng != null && !widget.proximityEnforced) {
      // Exempt: show the whole route, but still measure and sort by distance so
      // the nearest shop is still the easiest one to tap.
      _nearby = const FilterNearbyOutlets()(
        repLat: repLat,
        repLng: repLng,
        outlets: widget.outlets,
        radiusMeters: double.infinity,
      );
    } else if (repLat != null && repLng != null) {
      _nearby = const FilterNearbyOutlets()(
        repLat: repLat,
        repLng: repLng,
        outlets: widget.outlets,
        radiusMeters: widget.radiusMeters,
      );
    } else {
      // GPS not yet available — show all, no distance labels.
      _nearby = widget.outlets
          .map((o) => (outlet: o, meters: double.infinity))
          .toList();
    }
  }

  @override
  Widget build(BuildContext context) {
    final hasGps = widget.repLat != null && widget.repLng != null;

    final filtered = _nearby.where((n) {
      if (_query.isEmpty) return true;
      final q = _query.toLowerCase();
      return n.outlet.name.toLowerCase().contains(q) ||
          n.outlet.address.toLowerCase().contains(q);
    }).toList();

    final radiusLabel = widget.radiusMeters >= 1000
        ? '${(widget.radiusMeters / 1000).toStringAsFixed(1)} km'
        : '${widget.radiusMeters.toStringAsFixed(0)} m';

    return FractionallySizedBox(
      heightFactor: 0.88,
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
          SizedBox(height: 16.h),
          Padding(
            padding: EdgeInsets.symmetric(horizontal: 16.w),
            child: Row(
              children: [
                Container(
                  width: 3.w,
                  height: 13.h,
                  decoration: BoxDecoration(
                    color: AppColors.primary,
                    borderRadius: BorderRadius.circular(2.r),
                  ),
                ),
                SizedBox(width: 8.w),
                Text(
                  'TODAY\'S OUTLETS',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 11.sp,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 2.5,
                    color: AppColors.foregroundMuted,
                  ),
                ),
                if (hasGps) ...[
                  const Spacer(),
                  // The chip is the rep's only signal that the distance rule is
                  // off. A silent exemption reads as a loophole; a labelled one
                  // reads as a supervised allowance — and it saves the support
                  // call asking why distant shops suddenly appeared.
                  _chip(
                    label: widget.proximityEnforced
                        ? 'Within $radiusLabel'
                        : 'Distance check off',
                    color: widget.proximityEnforced
                        ? AppColors.primary
                        : AppColors.warning,
                  ),
                ],
              ],
            ),
          ),
          if (!widget.proximityEnforced) _exemptionBanner(),
          SizedBox(height: 12.h),
          Padding(
            padding: EdgeInsets.symmetric(horizontal: 16.w),
            child: TextField(
              autofocus: false,
              decoration: InputDecoration(
                prefixIcon: Icon(Icons.search, size: 18.r),
                hintText: 'Search by name or address',
                filled: true,
                fillColor: AppColors.surface,
                contentPadding:
                    EdgeInsets.symmetric(horizontal: 16.w, vertical: 12.h),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(10.r),
                  borderSide: BorderSide.none,
                ),
              ),
              onChanged: (v) => setState(() => _query = v),
            ),
          ),
          SizedBox(height: 8.h),
          Expanded(
            child: filtered.isEmpty
                ? _emptyState(hasGps, radiusLabel)
                : ListView.separated(
                    padding: EdgeInsets.symmetric(horizontal: 16.w),
                    itemCount: filtered.length,
                    separatorBuilder: (_, __) =>
                        Divider(height: 1, color: AppColors.surfaceVariant),
                    itemBuilder: (ctx, i) {
                      final n = filtered[i];
                      final o = n.outlet;
                      final isSelected = widget.selected?.id == o.id;
                      final distLabel = _formatDistance(n.meters);

                      return ListTile(
                        contentPadding: EdgeInsets.symmetric(
                            horizontal: 4.w, vertical: 4.h),
                        leading: Container(
                          width: 36.r,
                          height: 36.r,
                          decoration: BoxDecoration(
                            color:
                                AppColors.primary.withValues(alpha: 0.10),
                            borderRadius: BorderRadius.circular(8.r),
                          ),
                          child: Icon(Icons.storefront_rounded,
                              size: 16.r, color: AppColors.primary),
                        ),
                        title: Row(
                          children: [
                            Expanded(
                              child: Text(
                                o.name,
                                style: GoogleFonts.barlowCondensed(
                                  fontSize: 15.sp,
                                  fontWeight: FontWeight.w700,
                                  letterSpacing: 0.3,
                                  color: AppColors.foreground,
                                ),
                              ),
                            ),
                            if (o.lastBillDate == null)
                              Container(
                                margin: EdgeInsets.only(left: 6.w),
                                padding: EdgeInsets.symmetric(
                                    horizontal: 6.w, vertical: 2.h),
                                decoration: BoxDecoration(
                                  color: Colors.amber.shade700,
                                  borderRadius: BorderRadius.circular(4.r),
                                ),
                                child: Text(
                                  'NEW',
                                  style: GoogleFonts.barlowCondensed(
                                    fontSize: 8.sp,
                                    fontWeight: FontWeight.w700,
                                    letterSpacing: 1.2,
                                    color: Colors.white,
                                  ),
                                ),
                              ),
                            if (distLabel != null) ...[
                              SizedBox(width: 6.w),
                              Container(
                                padding: EdgeInsets.symmetric(
                                    horizontal: 6.w, vertical: 2.h),
                                decoration: BoxDecoration(
                                  color: AppColors.primary
                                      .withValues(alpha: 0.08),
                                  borderRadius: BorderRadius.circular(4.r),
                                ),
                                child: Text(
                                  distLabel,
                                  style: GoogleFonts.barlowCondensed(
                                    fontSize: 9.sp,
                                    fontWeight: FontWeight.w700,
                                    letterSpacing: 0.5,
                                    color: AppColors.primary,
                                  ),
                                ),
                              ),
                            ],
                          ],
                        ),
                        subtitle: Text(
                          o.address,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: GoogleFonts.barlow(
                            fontSize: 11.sp,
                            color: AppColors.foregroundMuted,
                          ),
                        ),
                        trailing: isSelected
                            ? Icon(Icons.check_circle_rounded,
                                color: AppColors.primary, size: 20.r)
                            : null,
                        onTap: () => Navigator.of(ctx).pop(o),
                      );
                    },
                  ),
          ),
          SizedBox(height: 16.h),
        ],
      ),
    );
  }

  Widget _chip({required String label, required Color color}) => Container(
        padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 3.h),
        decoration: BoxDecoration(
          color: color.withValues(alpha: 0.10),
          borderRadius: BorderRadius.circular(20.r),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.my_location_rounded, size: 10.r, color: color),
            SizedBox(width: 4.w),
            Text(
              label,
              style: GoogleFonts.barlowCondensed(
                fontSize: 10.sp,
                fontWeight: FontWeight.w700,
                color: color,
              ),
            ),
          ],
        ),
      );

  /// Explains the relaxed behaviour in the rep's own terms, and dates it, so an
  /// exemption never looks like the app quietly losing its rules.
  Widget _exemptionBanner() {
    final until = widget.exemptionUntil;
    const accent = AppColors.warning;

    return Padding(
      padding: EdgeInsets.fromLTRB(16.w, 10.h, 16.w, 0),
      child: Container(
        padding: EdgeInsets.all(12.r),
        decoration: BoxDecoration(
          color: accent.withValues(alpha: 0.08),
          borderRadius: BorderRadius.circular(12.r),
          border: Border.all(color: accent.withValues(alpha: 0.35)),
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(Icons.location_searching_rounded, size: 20.r, color: accent),
            SizedBox(width: 10.w),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Distance check turned off',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 13.sp,
                      fontWeight: FontWeight.w700,
                      color: accent,
                    ),
                  ),
                  SizedBox(height: 2.h),
                  Text(
                    until == null
                        ? 'You can bill any outlet on today\'s route, whatever the distance.'
                        : 'You can bill any outlet on today\'s route until '
                            '${_formatUntil(until)}, whatever the distance.',
                    style: GoogleFonts.barlow(
                      fontSize: 12.sp,
                      height: 1.3,
                      color: AppColors.foregroundMuted,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  /// Local wall-clock rendering — the rep thinks in Sri Lanka time, and the
  /// instant arrives from the server as UTC.
  static String _formatUntil(DateTime utc) {
    final local = utc.toLocal();
    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
    ];
    // ValidTo is the exclusive midnight boundary, so the last day the rep can
    // actually use it is the day before — say that, not the boundary date.
    final lastDay = local.subtract(const Duration(minutes: 1));
    return '${lastDay.day} ${months[lastDay.month - 1]}';
  }

  Widget _emptyState(bool hasGps, String radiusLabel) {
    // With the distance rule off, an empty list means the route itself is empty —
    // telling the rep to "move closer" would be nonsense.
    if (!widget.proximityEnforced && _query.isEmpty) {
      return Center(
        child: Text(
          'No outlets on today\'s route.',
          style: GoogleFonts.barlow(
              fontSize: 13.sp, color: AppColors.foregroundMuted),
        ),
      );
    }
    if (!hasGps) {
      return Center(
        child: Text(
          'No outlets match your search.',
          style:
              GoogleFonts.barlow(fontSize: 13.sp, color: AppColors.foregroundMuted),
        ),
      );
    }
    if (_query.isNotEmpty) {
      return Center(
        child: Text(
          'No nearby outlets match your search.',
          style:
              GoogleFonts.barlow(fontSize: 13.sp, color: AppColors.foregroundMuted),
        ),
      );
    }
    return Center(
      child: Padding(
        padding: EdgeInsets.symmetric(horizontal: 28.w),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 56.r,
              height: 56.r,
              decoration: BoxDecoration(
                color: AppColors.primary.withValues(alpha: 0.08),
                shape: BoxShape.circle,
              ),
              child: Icon(Icons.location_searching_rounded,
                  size: 26.r, color: AppColors.primary),
            ),
            SizedBox(height: 16.h),
            Text(
              'No outlets within $radiusLabel',
              textAlign: TextAlign.center,
              style: GoogleFonts.barlowCondensed(
                fontSize: 18.sp,
                fontWeight: FontWeight.w800,
                color: AppColors.foreground,
              ),
            ),
            SizedBox(height: 6.h),
            Text(
              'Move closer to a shop on your route and try again.',
              textAlign: TextAlign.center,
              style: GoogleFonts.barlow(
                fontSize: 13.sp,
                color: AppColors.foregroundMuted,
                height: 1.5,
              ),
            ),
          ],
        ),
      ),
    );
  }

  /// Returns `"120 m"` / `"0.8 km"` / `null` (for outlets without coordinates).
  String? _formatDistance(double meters) {
    if (meters == double.infinity) return null;
    if (meters < 1000) return '${meters.toStringAsFixed(0)} m';
    return '${(meters / 1000).toStringAsFixed(1)} km';
  }
}
