import 'package:equatable/equatable.dart';
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

  const OutletsLoaded({
    required this.outlets,
    required this.isSyncing,
    this.lastSyncedAt,
  });

  OutletsLoaded copyWith({
    List<Outlet>? outlets,
    bool? isSyncing,
    DateTime? lastSyncedAt,
  }) =>
      OutletsLoaded(
        outlets: outlets ?? this.outlets,
        isSyncing: isSyncing ?? this.isSyncing,
        lastSyncedAt: lastSyncedAt ?? this.lastSyncedAt,
      );

  @override
  List<Object?> get props => [outlets, isSyncing, lastSyncedAt];
}

final class OutletsError extends OutletsState {
  final String message;

  const OutletsError({required this.message});

  @override
  List<Object?> get props => [message];
}
