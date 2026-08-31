import 'package:equatable/equatable.dart';

sealed class OutletsEvent extends Equatable {
  const OutletsEvent();

  @override
  List<Object?> get props => [];
}

/// Load local cache immediately, then sync in background using the stored routeId.
final class LoadOutletsRequested extends OutletsEvent {
  const LoadOutletsRequested();
}

/// Explicit sync for a specific route — fired by the home page when today's
/// assignment loads, and by the sync page's sync button.
final class SyncDailyOutletsRequested extends OutletsEvent {
  final int routeId;
  final String routeName;

  const SyncDailyOutletsRequested({
    required this.routeId,
    required this.routeName,
  });

  @override
  List<Object?> get props => [routeId, routeName];
}

/// Fired when the caller has confirmed (via a fresh server call) that there is
/// no route assignment for today. Wipes any outlets + sync stamp left over
/// from a previous day so a stale "today" timestamp can't keep the billing
/// flow unlocked.
final class NoAssignmentTodayConfirmed extends OutletsEvent {
  const NoAssignmentTodayConfirmed();
}
