import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/outlets/domain/entities/outlet.dart';

/// Tappable card that opens an outlet bottom-sheet.
class OutletPicker extends StatelessWidget {
  final Outlet? selected;
  final List<Outlet> outlets;
  final ValueChanged<Outlet> onSelected;
  final bool hasActiveAssignment;

  const OutletPicker({
    super.key,
    required this.selected,
    required this.outlets,
    required this.onSelected,
    this.hasActiveAssignment = true,
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
          ),
    );
    if (picked != null) onSelected(picked);
  }
}

class _OutletSheet extends StatefulWidget {
  final List<Outlet> outlets;
  final Outlet? selected;
  final bool hasActiveAssignment;
  const _OutletSheet({
    required this.outlets,
    required this.selected,
    required this.hasActiveAssignment,
  });

  @override
  State<_OutletSheet> createState() => _OutletSheetState();
}

class _OutletSheetState extends State<_OutletSheet> {
  String _query = '';

  @override
  Widget build(BuildContext context) {
    final filtered = widget.outlets
        .where((o) =>
            _query.isEmpty ||
            o.name.toLowerCase().contains(_query.toLowerCase()) ||
            o.address.toLowerCase().contains(_query.toLowerCase()))
        .toList();

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
              ],
            ),
          ),
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
                ? Center(
                    child: Text(
                      'No outlets match your search.',
                      style: GoogleFonts.barlow(
                        fontSize: 13.sp,
                        color: AppColors.foregroundMuted,
                      ),
                    ),
                  )
                : ListView.separated(
                    padding: EdgeInsets.symmetric(horizontal: 16.w),
                    itemCount: filtered.length,
                    separatorBuilder: (_, __) =>
                        Divider(height: 1, color: AppColors.surfaceVariant),
                    itemBuilder: (ctx, i) {
                      final o = filtered[i];
                      final isSelected = widget.selected?.id == o.id;
                      return ListTile(
                        contentPadding:
                            EdgeInsets.symmetric(horizontal: 4.w, vertical: 4.h),
                        leading: Container(
                          width: 36.r,
                          height: 36.r,
                          decoration: BoxDecoration(
                            color: AppColors.primary.withValues(alpha: 0.10),
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
}
