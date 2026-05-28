import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:google_maps_flutter/google_maps_flutter.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/core/widgets/app_spinner.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
import 'package:uswatte/features/supervisor_route_map/presentation/bloc/supervisor_route_map_bloc.dart';
import 'package:uswatte/features/supervisor_route_map/presentation/bloc/supervisor_route_map_event.dart';
import 'package:uswatte/features/supervisor_route_map/presentation/bloc/supervisor_route_map_state.dart';
import 'package:uswatte/features/todays_route_map/domain/enums/route_outlet_status.dart';
import 'package:uswatte/features/todays_route_map/presentation/widgets/outlet_map_sheet.dart';

class SupervisorRouteMapPage extends StatelessWidget {
  const SupervisorRouteMapPage({super.key});

  static const _defaultCamera = CameraPosition(
    target: LatLng(7.8731, 80.7718),
    zoom: 8,
  );

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    return BlocBuilder<SupervisorRouteMapBloc, SupervisorRouteMapState>(
      builder: (context, state) {
        final isOnMap = state is SupervisorRouteMapLoaded;
        final isLoading = state is SupervisorRouteMapLoadingReps ||
            state is SupervisorRouteMapInitial ||
            (state is SupervisorRouteMapReady && state.isLoadingMap);

        return Scaffold(
          backgroundColor: AppColors.surface,
          body: Column(
            children: [
              _MapHeader(
                onBack: isOnMap
                    ? () => context.read<SupervisorRouteMapBloc>()
                        .add(const SupervisorRouteMapBackToSelectorRequested())
                    : () => context.pop(),
                isLoading: isLoading,
                onRefresh: isOnMap
                    ? () => context.read<SupervisorRouteMapBloc>()
                        .add(const SupervisorRouteMapRefreshRequested())
                    : null,
                isOnMap: isOnMap,
                repName: state is SupervisorRouteMapLoaded ? state.rep.userName : null,
              ),
              Expanded(
                child: _buildBody(context, state),
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _buildBody(BuildContext context, SupervisorRouteMapState state) {
    if (state is SupervisorRouteMapInitial ||
        state is SupervisorRouteMapLoadingReps) {
      return const Center(child: AppSpinner());
    }

    if (state is SupervisorRouteMapRepsError) {
      return _ErrorView(
        message: state.message,
        onRetry: () => context
            .read<SupervisorRouteMapBloc>()
            .add(const SupervisorRouteMapRepsRequested()),
      );
    }

    if (state is SupervisorRouteMapReady) {
      return _SelectorView(state: state);
    }

    if (state is SupervisorRouteMapLoaded) {
      return _MapView(
        key: ValueKey('${state.rep.userId}_${state.outlets.length}'),
        state: state,
        defaultCamera: _defaultCamera,
      );
    }

    return const SizedBox.shrink();
  }
}

// ── Header ────────────────────────────────────────────────────────────────────

class _MapHeader extends StatelessWidget {
  const _MapHeader({
    required this.onBack,
    required this.isLoading,
    required this.onRefresh,
    required this.isOnMap,
    this.repName,
  });

  final VoidCallback onBack;
  final bool isLoading;
  final VoidCallback? onRefresh;
  final bool isOnMap;
  final String? repName;

  @override
  Widget build(BuildContext context) {
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
        child: Padding(
          padding: EdgeInsets.fromLTRB(8.w, 4.h, 16.w, 16.h),
          child: Row(
            children: [
              GestureDetector(
                onTap: onBack,
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
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      "REP ROUTE MAP",
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
                      isOnMap && repName != null
                          ? repName!
                          : "Today's route outlets for a rep",
                      style: GoogleFonts.barlow(
                        fontSize: 11.sp,
                        color: Colors.white.withValues(alpha: 0.70),
                      ),
                    ),
                  ],
                ),
              ),
              if (isOnMap && onRefresh != null)
                GestureDetector(
                  onTap: isLoading ? null : onRefresh,
                  child: isLoading
                      ? const AppSpinner.small(color: Colors.white)
                      : Icon(Icons.sync_rounded,
                          size: 20.r, color: Colors.white),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Error view ────────────────────────────────────────────────────────────────

class _ErrorView extends StatelessWidget {
  final String message;
  final VoidCallback onRetry;
  const _ErrorView({required this.message, required this.onRetry});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: EdgeInsets.all(24.r),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.cloud_off_rounded,
                size: 40.r, color: AppColors.foregroundMuted),
            SizedBox(height: 12.h),
            Text(
              message,
              textAlign: TextAlign.center,
              style: GoogleFonts.barlow(
                fontSize: 13.sp,
                color: AppColors.foregroundMuted,
              ),
            ),
            SizedBox(height: 16.h),
            TextButton(
              onPressed: onRetry,
              child: const Text('Retry'),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Selector view ─────────────────────────────────────────────────────────────

class _SelectorView extends StatelessWidget {
  final SupervisorRouteMapReady state;
  const _SelectorView({required this.state});

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: EdgeInsets.fromLTRB(16.w, 20.h, 16.w, 40.h),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _StepCard(
            step: '01',
            label: 'SALES REP',
            icon: Icons.person_rounded,
            isComplete: state.selectedRep != null,
            child: _SearchableSelectField<RepSummary>(
              value: state.selectedRep,
              items: state.reps,
              labelBuilder: (r) => r.userName,
              placeholder: 'Select a sales rep...',
              sheetTitle: 'SELECT SALES REP',
              enabled: !state.isLoadingMap,
              onChanged: (rep) {
                if (rep != null) {
                  context
                      .read<SupervisorRouteMapBloc>()
                      .add(SupervisorRouteMapRepSelected(rep));
                }
              },
            ),
          ),
          SizedBox(height: 24.h),

          if (state.mapError != null) ...[
            _ErrorBanner(message: state.mapError!),
            SizedBox(height: 16.h),
          ],

          _ViewMapButton(
            canLoad: state.canLoad,
            isLoading: state.isLoadingMap,
            onTap: () => context
                .read<SupervisorRouteMapBloc>()
                .add(const SupervisorRouteMapLoadRequested()),
          ),
        ],
      ),
    );
  }
}

// ── Step card ─────────────────────────────────────────────────────────────────

class _StepCard extends StatelessWidget {
  final String step;
  final String label;
  final IconData icon;
  final bool isComplete;
  final Widget child;

  const _StepCard({
    required this.step,
    required this.label,
    required this.icon,
    required this.isComplete,
    required this.child,
  });

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
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: EdgeInsets.fromLTRB(16.w, 14.h, 16.w, 12.h),
            decoration: const BoxDecoration(
              border: Border(bottom: BorderSide(color: Color(0xFFEEEDE6))),
            ),
            child: Row(
              children: [
                Container(
                  width: 28.r,
                  height: 28.r,
                  decoration: BoxDecoration(
                    color: isComplete
                        ? AppColors.primary
                        : const Color(0xFFEEEDE6),
                    borderRadius: BorderRadius.circular(8.r),
                  ),
                  child: Center(
                    child: Text(
                      step,
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 11.sp,
                        fontWeight: FontWeight.w800,
                        letterSpacing: 0.5,
                        color: isComplete
                            ? Colors.white
                            : AppColors.foregroundMuted,
                      ),
                    ),
                  ),
                ),
                SizedBox(width: 10.w),
                Icon(icon,
                    size: 14.r,
                    color: isComplete
                        ? AppColors.primary
                        : AppColors.foregroundMuted),
                SizedBox(width: 6.w),
                Text(
                  label,
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 12.sp,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 2.0,
                    color: isComplete
                        ? AppColors.foreground
                        : AppColors.foregroundMuted,
                  ),
                ),
                const Spacer(),
                if (isComplete)
                  Container(
                    padding:
                        EdgeInsets.symmetric(horizontal: 8.w, vertical: 3.h),
                    decoration: BoxDecoration(
                      color: AppColors.primary.withValues(alpha: 0.10),
                      borderRadius: BorderRadius.circular(20.r),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(Icons.check_rounded,
                            size: 10.r, color: AppColors.primary),
                        SizedBox(width: 3.w),
                        Text(
                          'SET',
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 9.sp,
                            fontWeight: FontWeight.w700,
                            letterSpacing: 1.0,
                            color: AppColors.primary,
                          ),
                        ),
                      ],
                    ),
                  ),
              ],
            ),
          ),
          Padding(
            padding: EdgeInsets.all(16.r),
            child: child,
          ),
        ],
      ),
    );
  }
}

// ── Searchable select field ───────────────────────────────────────────────────

class _SearchableSelectField<T> extends StatelessWidget {
  final T? value;
  final List<T> items;
  final String Function(T) labelBuilder;
  final String placeholder;
  final String sheetTitle;
  final bool enabled;
  final ValueChanged<T?>? onChanged;

  const _SearchableSelectField({
    required this.value,
    required this.items,
    required this.labelBuilder,
    required this.placeholder,
    required this.sheetTitle,
    required this.enabled,
    required this.onChanged,
  });

  void _openSheet(BuildContext context) {
    showModalBottomSheet<T>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => _SearchableSheetContent<T>(
        items: items,
        selectedValue: value,
        labelBuilder: labelBuilder,
        title: sheetTitle,
        onSelected: (item) {
          Navigator.of(context).pop();
          onChanged?.call(item);
        },
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final isSelected = value != null;
    return GestureDetector(
      onTap: enabled ? () => _openSheet(context) : null,
      child: Container(
        height: 50.h,
        padding: EdgeInsets.symmetric(horizontal: 14.w),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(10.r),
          border: Border.all(
            color: isSelected
                ? AppColors.primary.withValues(alpha: 0.40)
                : const Color(0xFFE0DFD9),
          ),
        ),
        child: Row(
          children: [
            Icon(
              isSelected
                  ? Icons.check_circle_outline_rounded
                  : Icons.search_rounded,
              size: 16.r,
              color: isSelected ? AppColors.primary : AppColors.foregroundMuted,
            ),
            SizedBox(width: 10.w),
            Expanded(
              child: Text(
                isSelected ? labelBuilder(value as T) : placeholder,
                style: GoogleFonts.barlow(
                  fontSize: 14.sp,
                  fontWeight:
                      isSelected ? FontWeight.w600 : FontWeight.w400,
                  color: isSelected
                      ? AppColors.foreground
                      : AppColors.foregroundMuted,
                ),
                overflow: TextOverflow.ellipsis,
              ),
            ),
            Icon(Icons.keyboard_arrow_down_rounded,
                color: enabled ? AppColors.primary : AppColors.foregroundMuted,
                size: 20.r),
          ],
        ),
      ),
    );
  }
}

// ── Searchable sheet content ──────────────────────────────────────────────────

class _SearchableSheetContent<T> extends StatefulWidget {
  final List<T> items;
  final T? selectedValue;
  final String Function(T) labelBuilder;
  final String title;
  final ValueChanged<T> onSelected;

  const _SearchableSheetContent({
    required this.items,
    required this.selectedValue,
    required this.labelBuilder,
    required this.title,
    required this.onSelected,
  });

  @override
  State<_SearchableSheetContent<T>> createState() =>
      _SearchableSheetContentState<T>();
}

class _SearchableSheetContentState<T>
    extends State<_SearchableSheetContent<T>> {
  final _controller = TextEditingController();
  late List<T> _filtered;

  @override
  void initState() {
    super.initState();
    _filtered = widget.items;
    _controller.addListener(_onSearch);
  }

  void _onSearch() {
    final q = _controller.text.toLowerCase();
    setState(() {
      _filtered = q.isEmpty
          ? widget.items
          : widget.items
              .where((i) => widget.labelBuilder(i).toLowerCase().contains(q))
              .toList();
    });
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return DraggableScrollableSheet(
      initialChildSize: 0.65,
      minChildSize: 0.4,
      maxChildSize: 0.92,
      expand: false,
      builder: (_, scrollController) => ClipRRect(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20.r)),
        child: Container(
          color: Colors.white,
          child: Column(
            children: [
              Container(
                decoration: BoxDecoration(
                  gradient: LinearGradient(
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                    colors: [AppColors.primaryDark, AppColors.primary],
                  ),
                ),
                child: Column(
                  children: [
                    SizedBox(height: 10.h),
                    Container(
                      width: 32.w,
                      height: 3.5.h,
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: 0.35),
                        borderRadius: BorderRadius.circular(2.r),
                      ),
                    ),
                    SizedBox(height: 12.h),
                    Padding(
                      padding: EdgeInsets.fromLTRB(20.w, 0, 16.w, 16.h),
                      child: Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  widget.title,
                                  style: GoogleFonts.barlowCondensed(
                                    fontSize: 19.sp,
                                    fontWeight: FontWeight.w800,
                                    letterSpacing: 1.2,
                                    color: Colors.white,
                                    height: 1.0,
                                  ),
                                ),
                                SizedBox(height: 2.h),
                                Text(
                                  '${widget.items.length} available',
                                  style: GoogleFonts.barlow(
                                    fontSize: 11.sp,
                                    color:
                                        Colors.white.withValues(alpha: 0.65),
                                  ),
                                ),
                              ],
                            ),
                          ),
                          GestureDetector(
                            onTap: () => Navigator.of(context).pop(),
                            child: Container(
                              width: 32.r,
                              height: 32.r,
                              decoration: BoxDecoration(
                                color: Colors.white.withValues(alpha: 0.18),
                                shape: BoxShape.circle,
                              ),
                              child: Icon(Icons.close_rounded,
                                  size: 15.r, color: Colors.white),
                            ),
                          ),
                        ],
                      ),
                    ),
                    Padding(
                      padding: EdgeInsets.fromLTRB(16.w, 0, 16.w, 14.h),
                      child: Container(
                        height: 52.h,
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(10.r),
                        ),
                        child: Row(
                          children: [
                            Padding(
                              padding:
                                  EdgeInsets.symmetric(horizontal: 12.w),
                              child: Icon(Icons.search_rounded,
                                  size: 17.r,
                                  color: const Color(0xFFADADA5)),
                            ),
                            Expanded(
                              child: TextField(
                                controller: _controller,
                                cursorColor: AppColors.primary,
                                style: GoogleFonts.barlow(
                                    fontSize: 14.sp,
                                    color: AppColors.foreground),
                                decoration: InputDecoration(
                                  hintText: 'Search...',
                                  hintStyle: GoogleFonts.barlow(
                                      fontSize: 14.sp,
                                      color: const Color(0xFFADADA5)),
                                  border: InputBorder.none,
                                  enabledBorder: InputBorder.none,
                                  focusedBorder: InputBorder.none,
                                  filled: true,
                                  fillColor: Colors.white,
                                  isDense: true,
                                  contentPadding: EdgeInsets.zero,
                                ),
                              ),
                            ),
                            if (_controller.text.isNotEmpty)
                              GestureDetector(
                                onTap: () => _controller.clear(),
                                child: Padding(
                                  padding: EdgeInsets.symmetric(
                                      horizontal: 10.w),
                                  child: Icon(Icons.cancel_rounded,
                                      size: 16.r,
                                      color: const Color(0xFFADADA5)),
                                ),
                              )
                            else
                              SizedBox(width: 12.w),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              Container(
                color: const Color(0xFFF8F8F5),
                padding:
                    EdgeInsets.symmetric(horizontal: 20.w, vertical: 8.h),
                child: Row(
                  children: [
                    Text(
                      _controller.text.isEmpty
                          ? 'All results'
                          : '${_filtered.length} result${_filtered.length == 1 ? '' : 's'}',
                      style: GoogleFonts.barlow(
                        fontSize: 11.sp,
                        fontWeight: FontWeight.w600,
                        color: const Color(0xFFADADA5),
                        letterSpacing: 0.3,
                      ),
                    ),
                  ],
                ),
              ),
              Divider(height: 1, color: const Color(0xFFF0EFEA)),
              Expanded(
                child: _filtered.isEmpty
                    ? Center(
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(Icons.search_off_rounded,
                                size: 34.r,
                                color: const Color(0xFFCCCCC4)),
                            SizedBox(height: 10.h),
                            Text('No results',
                                style: GoogleFonts.barlow(
                                    fontSize: 14.sp,
                                    fontWeight: FontWeight.w500,
                                    color: const Color(0xFFADADA5))),
                          ],
                        ),
                      )
                    : ListView.builder(
                        controller: scrollController,
                        padding: EdgeInsets.only(bottom: 24.h),
                        itemCount: _filtered.length,
                        itemBuilder: (_, index) {
                          final item = _filtered[index];
                          final isActive = item == widget.selectedValue;
                          final label = widget.labelBuilder(item);
                          final initial =
                              label.isNotEmpty ? label[0].toUpperCase() : '?';
                          return GestureDetector(
                            onTap: () => widget.onSelected(item),
                            behavior: HitTestBehavior.opaque,
                            child: Container(
                              color: isActive
                                  ? AppColors.primary.withValues(alpha: 0.05)
                                  : Colors.white,
                              padding: EdgeInsets.symmetric(
                                  horizontal: 20.w, vertical: 12.h),
                              child: Row(
                                children: [
                                  Container(
                                    width: 38.r,
                                    height: 38.r,
                                    decoration: BoxDecoration(
                                      color: isActive
                                          ? AppColors.primary
                                          : const Color(0xFFF0EFEA),
                                      shape: BoxShape.circle,
                                    ),
                                    child: Center(
                                      child: Text(
                                        initial,
                                        style: GoogleFonts.barlowCondensed(
                                          fontSize: 16.sp,
                                          fontWeight: FontWeight.w800,
                                          color: isActive
                                              ? Colors.white
                                              : const Color(0xFF8A8A82),
                                        ),
                                      ),
                                    ),
                                  ),
                                  SizedBox(width: 14.w),
                                  Expanded(
                                    child: Text(
                                      label,
                                      style: GoogleFonts.barlow(
                                        fontSize: 14.sp,
                                        fontWeight: isActive
                                            ? FontWeight.w700
                                            : FontWeight.w500,
                                        color: isActive
                                            ? AppColors.primary
                                            : AppColors.foreground,
                                      ),
                                      overflow: TextOverflow.ellipsis,
                                    ),
                                  ),
                                  if (isActive)
                                    Icon(Icons.check_rounded,
                                        size: 18.r, color: AppColors.primary)
                                  else
                                    SizedBox(width: 18.r),
                                ],
                              ),
                            ),
                          );
                        },
                      ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Error banner ──────────────────────────────────────────────────────────────

class _ErrorBanner extends StatelessWidget {
  final String message;
  const _ErrorBanner({required this.message});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.all(14.r),
      decoration: BoxDecoration(
        color: AppColors.error.withValues(alpha: 0.06),
        borderRadius: BorderRadius.circular(12.r),
        border: Border.all(color: AppColors.error.withValues(alpha: 0.25)),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 28.r,
            height: 28.r,
            decoration: BoxDecoration(
              color: AppColors.error.withValues(alpha: 0.10),
              borderRadius: BorderRadius.circular(8.r),
            ),
            child: Icon(Icons.error_outline_rounded,
                color: AppColors.error, size: 15.r),
          ),
          SizedBox(width: 12.w),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Load Failed',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 13.sp,
                      fontWeight: FontWeight.w700,
                      color: AppColors.error,
                      letterSpacing: 0.3,
                    )),
                SizedBox(height: 2.h),
                Text(message,
                    style: GoogleFonts.barlow(
                        fontSize: 12.sp,
                        color: AppColors.foreground,
                        height: 1.4)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ── View map button ───────────────────────────────────────────────────────────

class _ViewMapButton extends StatelessWidget {
  final bool canLoad;
  final bool isLoading;
  final VoidCallback onTap;

  const _ViewMapButton({
    required this.canLoad,
    required this.isLoading,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: canLoad ? onTap : null,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        width: double.infinity,
        height: 56.h,
        decoration: BoxDecoration(
          gradient: canLoad
              ? LinearGradient(
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                  colors: [AppColors.primaryDark, AppColors.primary],
                )
              : null,
          color: canLoad ? null : const Color(0xFFEEEDE6),
          borderRadius: BorderRadius.circular(14.r),
          boxShadow: canLoad
              ? [
                  BoxShadow(
                    color: AppColors.primary.withValues(alpha: 0.35),
                    blurRadius: 20,
                    offset: const Offset(0, 6),
                  ),
                ]
              : null,
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            if (isLoading)
              const AppSpinner.button()
            else
              Icon(
                Icons.map_rounded,
                size: 18.r,
                color: canLoad ? Colors.white : AppColors.foregroundMuted,
              ),
            SizedBox(width: 10.w),
            Text(
              'VIEW MAP',
              style: GoogleFonts.barlowCondensed(
                fontSize: 16.sp,
                fontWeight: FontWeight.w800,
                letterSpacing: 1.5,
                color: canLoad ? Colors.white : AppColors.foregroundMuted,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Map view ──────────────────────────────────────────────────────────────────

class _MapView extends StatefulWidget {
  final SupervisorRouteMapLoaded state;
  final CameraPosition defaultCamera;

  const _MapView({
    super.key,
    required this.state,
    required this.defaultCamera,
  });

  @override
  State<_MapView> createState() => _MapViewState();
}

class _MapViewState extends State<_MapView> {
  final Completer<GoogleMapController> _controllerCompleter = Completer();
  GoogleMapController? _controller;
  Set<Marker> _markers = {};

  @override
  void initState() {
    super.initState();
    _buildMarkers();
  }

  @override
  void dispose() {
    _controller?.dispose();
    super.dispose();
  }

  bool _hasValidCoords(double lat, double lng) =>
      !(lat == 0.0 && lng == 0.0);

  void _buildMarkers() {
    final markers = <Marker>{};
    for (final routeOutlet in widget.state.outlets) {
      final outlet = routeOutlet.outlet;
      if (!_hasValidCoords(outlet.latitude, outlet.longitude)) continue;
      markers.add(
        Marker(
          markerId: MarkerId('outlet_${outlet.id}'),
          position: LatLng(outlet.latitude, outlet.longitude),
          icon: BitmapDescriptor.defaultMarkerWithHue(
              _hueForStatus(routeOutlet.status)),
          infoWindow: InfoWindow(title: outlet.name),
          onTap: () => _showOutletSheet(routeOutlet),
        ),
      );
    }
    setState(() => _markers = markers);
  }

  double _hueForStatus(RouteOutletStatus status) {
    return switch (status) {
      RouteOutletStatus.billed    => BitmapDescriptor.hueGreen,
      RouteOutletStatus.notBilled => BitmapDescriptor.hueOrange,
      RouteOutletStatus.pending   => BitmapDescriptor.hueRed,
    };
  }

  Future<void> _fitBounds(GoogleMapController controller) async {
    final outlets = widget.state.outlets
        .where((ro) => _hasValidCoords(ro.outlet.latitude, ro.outlet.longitude))
        .toList();
    if (outlets.isEmpty) return;

    if (outlets.length == 1) {
      final o = outlets.first.outlet;
      await controller.animateCamera(
        CameraUpdate.newLatLngZoom(LatLng(o.latitude, o.longitude), 13),
      );
      return;
    }

    double minLat = outlets.first.outlet.latitude;
    double maxLat = outlets.first.outlet.latitude;
    double minLng = outlets.first.outlet.longitude;
    double maxLng = outlets.first.outlet.longitude;

    for (final ro in outlets) {
      final lat = ro.outlet.latitude;
      final lng = ro.outlet.longitude;
      if (lat < minLat) minLat = lat;
      if (lat > maxLat) maxLat = lat;
      if (lng < minLng) minLng = lng;
      if (lng > maxLng) maxLng = lng;
    }

    if (!mounted) return;
    await controller.animateCamera(
      CameraUpdate.newLatLngBounds(
        LatLngBounds(
          southwest: LatLng(minLat, minLng),
          northeast: LatLng(maxLat, maxLng),
        ),
        72,
      ),
    );
  }

  void _onMapCreated(GoogleMapController controller) {
    _controller = controller;
    if (!_controllerCompleter.isCompleted) {
      _controllerCompleter.complete(controller);
    }
    _fitBounds(controller);
  }

  void _showOutletSheet(routeOutlet) {
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
      ),
      builder: (_) => OutletMapSheet(routeOutlet: routeOutlet),
    );
  }

  @override
  Widget build(BuildContext context) {
    final outlets     = widget.state.outlets;
    final billedCount    = outlets.where((o) => o.status == RouteOutletStatus.billed).length;
    final notBilledCount = outlets.where((o) => o.status == RouteOutletStatus.notBilled).length;
    final pendingCount   = outlets.where((o) => o.status == RouteOutletStatus.pending).length;

    if (outlets.isEmpty) {
      return Column(
        children: [
          Expanded(
            child: Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(Icons.map_outlined,
                      size: 48.r, color: AppColors.foregroundMuted),
                  SizedBox(height: 12.h),
                  Text(
                    'No route assigned today',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 18.sp,
                      fontWeight: FontWeight.w700,
                      color: AppColors.foreground,
                    ),
                  ),
                  SizedBox(height: 4.h),
                  Text(
                    '${widget.state.rep.userName} has no route assignment for today',
                    textAlign: TextAlign.center,
                    style: GoogleFonts.barlow(
                      fontSize: 13.sp,
                      color: AppColors.foregroundMuted,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      );
    }

    return Column(
      children: [
        Expanded(
          child: GoogleMap(
            initialCameraPosition: widget.defaultCamera,
            markers: _markers,
            myLocationEnabled: true,
            myLocationButtonEnabled: true,
            zoomControlsEnabled: true,
            onMapCreated: _onMapCreated,
          ),
        ),
        _LegendBar(
          billedCount: billedCount,
          notBilledCount: notBilledCount,
          pendingCount: pendingCount,
        ),
      ],
    );
  }
}

// ── Legend bar ────────────────────────────────────────────────────────────────

class _LegendBar extends StatelessWidget {
  final int billedCount;
  final int notBilledCount;
  final int pendingCount;

  const _LegendBar({
    required this.billedCount,
    required this.notBilledCount,
    required this.pendingCount,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      color: Colors.white,
      padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 12.h),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceEvenly,
        children: [
          _LegendItem(
            color: const Color(0xFF4CAF50),
            label: 'Billed',
            count: billedCount,
          ),
          Container(width: 1, height: 24.h, color: AppColors.surfaceVariant),
          _LegendItem(
            color: const Color(0xFFFF9800),
            label: 'Not Billed',
            count: notBilledCount,
          ),
          Container(width: 1, height: 24.h, color: AppColors.surfaceVariant),
          _LegendItem(
            color: const Color(0xFFF44336),
            label: 'Pending',
            count: pendingCount,
          ),
        ],
      ),
    );
  }
}

class _LegendItem extends StatelessWidget {
  final Color color;
  final String label;
  final int count;

  const _LegendItem({
    required this.color,
    required this.label,
    required this.count,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 10.r,
          height: 10.r,
          decoration: BoxDecoration(color: color, shape: BoxShape.circle),
        ),
        SizedBox(width: 6.w),
        Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              label.toUpperCase(),
              style: GoogleFonts.barlowCondensed(
                fontSize: 9.sp,
                fontWeight: FontWeight.w700,
                letterSpacing: 1.0,
                color: AppColors.foregroundMuted,
              ),
            ),
            Text(
              count.toString(),
              style: GoogleFonts.barlowCondensed(
                fontSize: 18.sp,
                fontWeight: FontWeight.w900,
                height: 1.0,
                color: color,
              ),
            ),
          ],
        ),
      ],
    );
  }
}
