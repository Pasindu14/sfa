import 'package:equatable/equatable.dart';
import 'package:uswatte/core/constants/app_constants.dart';
import 'package:uswatte/features/outlets/domain/entities/outlet.dart';

sealed class OutletsState extends Equatable {
  const OutletsState();

  @override
  List<Object?> get props => [];
}

final class OutletsInitial extends OutletsState {
  const OutletsInitial();
}

final class OutletsLoading extends OutletsState {
  const OutletsLoading();
}

final class OutletsLoaded extends OutletsState {
  final List<Outlet> outlets;
  final bool isSyncing;
  final DateTime? lastSyncedAt;
  final bool hasActiveAssignment;
  final double geofenceRadiusMeters;

  const OutletsLoaded({
    required this.outlets,
    required this.isSyncing,
    this.lastSyncedAt,
    this.hasActiveAssignment = false,
    this.geofenceRadiusMeters = AppConstants.billingProximityRadiusMeters,
  });

  OutletsLoaded copyWith({
    List<Outlet>? outlets,
    bool? isSyncing,
    DateTime? lastSyncedAt,
    bool? hasActiveAssignment,
    double? geofenceRadiusMeters,
  }) =>
      OutletsLoaded(
        outlets: outlets ?? this.outlets,
        isSyncing: isSyncing ?? this.isSyncing,
        lastSyncedAt: lastSyncedAt ?? this.lastSyncedAt,
        hasActiveAssignment: hasActiveAssignment ?? this.hasActiveAssignment,
        geofenceRadiusMeters: geofenceRadiusMeters ?? this.geofenceRadiusMeters,
      );

  @override
  List<Object?> get props =>
      [outlets, isSyncing, lastSyncedAt, hasActiveAssignment, geofenceRadiusMeters];
}

final class OutletsError extends OutletsState {
  final String message;

  const OutletsError({required this.message});

  @override
  List<Object?> get props => [message];
}
