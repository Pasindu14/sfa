import 'package:equatable/equatable.dart';
import 'package:uswatte/core/constants/app_constants.dart';
import 'package:uswatte/features/outlets/domain/entities/outlet.dart';
import 'package:uswatte/features/outlets/domain/entities/proximity_policy.dart';

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

  /// The rep's effective geofence policy, as last received from the server.
  /// Consumers must ask `policy.isEnforcedNow`, not `policy.enforced`, so a
  /// cached exemption expires on time without waiting for a sync.
  final ProximityPolicy policy;

  const OutletsLoaded({
    required this.outlets,
    required this.isSyncing,
    this.lastSyncedAt,
    this.hasActiveAssignment = false,
    this.policy = const ProximityPolicy.enforcedAt(
        AppConstants.billingProximityRadiusMeters),
  });

  OutletsLoaded copyWith({
    List<Outlet>? outlets,
    bool? isSyncing,
    DateTime? lastSyncedAt,
    bool? hasActiveAssignment,
    ProximityPolicy? policy,
  }) =>
      OutletsLoaded(
        outlets: outlets ?? this.outlets,
        isSyncing: isSyncing ?? this.isSyncing,
        lastSyncedAt: lastSyncedAt ?? this.lastSyncedAt,
        hasActiveAssignment: hasActiveAssignment ?? this.hasActiveAssignment,
        policy: policy ?? this.policy,
      );

  @override
  List<Object?> get props =>
      [outlets, isSyncing, lastSyncedAt, hasActiveAssignment, policy];
}

final class OutletsError extends OutletsState {
  final String message;

  const OutletsError({required this.message});

  @override
  List<Object?> get props => [message];
}
