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
import 'package:uswatte/features/todays_route_map/domain/enums/route_outlet_status.dart';
import 'package:uswatte/features/todays_route_map/presentation/bloc/todays_route_map_bloc.dart';
import 'package:uswatte/features/todays_route_map/presentation/bloc/todays_route_map_event.dart';
import 'package:uswatte/features/todays_route_map/presentation/bloc/todays_route_map_state.dart';
import 'package:uswatte/features/todays_route_map/presentation/widgets/outlet_map_sheet.dart';

class TodaysRouteMapPage extends StatelessWidget {
  const TodaysRouteMapPage({super.key});

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

    return BlocBuilder<TodaysRouteMapBloc, TodaysRouteMapState>(
      builder: (context, state) {
        return Scaffold(
          backgroundColor: AppColors.background,
          body: Column(
            children: [
              _MapHeader(
                onBack: () => context.pop(),
                isLoading: state is TodaysRouteMapLoading ||
                    state is TodaysRouteMapInitial,
                onRefresh: () => context
                    .read<TodaysRouteMapBloc>()
                    .add(const LoadTodaysRouteMapRequested()),
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

  Widget _buildBody(BuildContext context, TodaysRouteMapState state) {
    if (state is TodaysRouteMapLoading || state is TodaysRouteMapInitial) {
      return const Center(child: AppSpinner());
    }

    if (state is TodaysRouteMapError) {
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
                state.message,
                textAlign: TextAlign.center,
                style: GoogleFonts.barlow(
                  fontSize: 13.sp,
                  color: AppColors.foregroundMuted,
                ),
              ),
              SizedBox(height: 16.h),
              TextButton(
                onPressed: () => context
                    .read<TodaysRouteMapBloc>()
                    .add(const LoadTodaysRouteMapRequested()),
                child: const Text('Retry'),
              ),
            ],
          ),
        ),
      );
    }

    if (state is TodaysRouteMapLoaded) {
      final initialCamera = state.userPosition != null
          ? CameraPosition(
              target: LatLng(
                state.userPosition!.latitude,
                state.userPosition!.longitude,
              ),
              zoom: 13,
            )
          : _defaultCamera;

      return _MapView(
        key: ValueKey(state.outlets.length),
        state: state,
        defaultCamera: initialCamera,
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
  });

  final VoidCallback onBack;
  final bool isLoading;
  final VoidCallback onRefresh;

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
                      "TODAY'S ROUTE MAP",
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
                      'Billed · Not Billed · Pending outlets',
                      style: GoogleFonts.barlow(
                        fontSize: 11.sp,
                        color: Colors.white.withValues(alpha: 0.70),
                      ),
                    ),
                  ],
                ),
              ),
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

// ── Map view ──────────────────────────────────────────────────────────────────

class _MapView extends StatefulWidget {
  final TodaysRouteMapLoaded state;
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

    final lastBilledId = widget.state.lastBilledOutletId;
    if (lastBilledId != null) {
      final match = widget.state.outlets
          .where((ro) => ro.outlet.id == lastBilledId)
          .firstOrNull;
      if (match != null &&
          _hasValidCoords(match.outlet.latitude, match.outlet.longitude)) {
        controller.animateCamera(
          CameraUpdate.newLatLngZoom(
            LatLng(match.outlet.latitude, match.outlet.longitude),
            14,
          ),
        );
        return;
      }
    }

    final userPos = widget.state.userPosition;
    if (userPos != null) {
      controller.animateCamera(
        CameraUpdate.newLatLngZoom(
          LatLng(userPos.latitude, userPos.longitude),
          13,
        ),
      );
      return;
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
    final outlets        = widget.state.outlets;
    final billedCount    = outlets.where((o) => o.status == RouteOutletStatus.billed).length;
    final notBilledCount = outlets.where((o) => o.status == RouteOutletStatus.notBilled).length;
    final pendingCount   = outlets.where((o) => o.status == RouteOutletStatus.pending).length;
    final unmappedCount  = outlets.where((o) {
      final hasBill = o.status == RouteOutletStatus.billed ||
          o.status == RouteOutletStatus.notBilled;
      return hasBill && !_hasValidCoords(o.outlet.latitude, o.outlet.longitude);
    }).length;

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
          unmappedCount: unmappedCount,
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
  final int unmappedCount;

  const _LegendBar({
    required this.billedCount,
    required this.notBilledCount,
    required this.pendingCount,
    required this.unmappedCount,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      color: Colors.white,
      padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 12.h),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Row(
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
          if (unmappedCount > 0) ...[
            SizedBox(height: 8.h),
            _UnmappedBanner(count: unmappedCount),
          ],
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

class _UnmappedBanner extends StatelessWidget {
  final int count;

  const _UnmappedBanner({required this.count});

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 6.h),
      decoration: BoxDecoration(
        color: const Color(0xFFFFF3E0),
        borderRadius: BorderRadius.circular(8.r),
        border: Border.all(color: const Color(0xFFFFCC80)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            Icons.location_off_rounded,
            size: 13.r,
            color: const Color(0xFFE65100),
          ),
          SizedBox(width: 6.w),
          Expanded(
            child: Text.rich(
              TextSpan(
                children: [
                  TextSpan(
                    text: '$count outlet${count > 1 ? 's' : ''} ',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 11.sp,
                      fontWeight: FontWeight.w700,
                      color: const Color(0xFFE65100),
                    ),
                  ),
                  TextSpan(
                    text: 'billed but not mapped correctly (GPS 0,0)',
                    style: GoogleFonts.barlow(
                      fontSize: 10.sp,
                      color: const Color(0xFFBF360C),
                    ),
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
