import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/core/connectivity/connectivity_service.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/features/route_unlock/domain/entities/route_unlock_request.dart';
import 'package:uswatte/features/route_unlock/domain/usecases/cancel_route_unlock_usecase.dart';
import 'package:uswatte/features/route_unlock/domain/usecases/get_today_unlock_request_usecase.dart';
import 'package:uswatte/features/route_unlock/domain/usecases/request_route_unlock_usecase.dart';
import 'package:uswatte/features/route_unlock/presentation/route_unlock_messages.dart';

class RouteUnlockState extends Equatable {
  /// The rep's latest request for today, or null when there is none (or it
  /// has not been fetched yet — see [loaded]).
  final RouteUnlockRequest? request;

  /// At least one fetch has succeeded, so a null [request] really means none.
  final bool loaded;
  final bool loading;

  /// A request or cancel is in flight.
  final bool submitting;

  /// The last action's failure, shown inline — the picker is a modal sheet, so
  /// a page snackbar would land underneath it.
  final String? error;

  const RouteUnlockState({
    this.request,
    this.loaded = false,
    this.loading = false,
    this.submitting = false,
    this.error,
  });

  bool get isPending => request?.isPending ?? false;
  bool get isLive => request?.isLive ?? false;

  /// A new request is only refused server-side while one is Pending or live.
  bool get canRequest => !isPending && !isLive;

  RouteUnlockState copyWith({
    RouteUnlockRequest? request,
    bool clearRequest = false,
    bool? loaded,
    bool? loading,
    bool? submitting,
    String? error,
    bool clearError = false,
  }) =>
      RouteUnlockState(
        request: clearRequest ? null : (request ?? this.request),
        loaded: loaded ?? this.loaded,
        loading: loading ?? this.loading,
        submitting: submitting ?? this.submitting,
        error: clearError ? null : (error ?? this.error),
      );

  @override
  List<Object?> get props => [request, loaded, loading, submitting, error];
}

/// The rep's side of a route unlock: today's request status, asking, and
/// cancelling.
///
/// The unlock itself never lives here — it reaches the device through the
/// outlet sync's geofence policy. This cubit only notices when the two
/// disagree (the server says Approved, the device still enforces) and asks the
/// page to re-sync, so a missed push repairs itself when the picker opens.
class RouteUnlockCubit extends Cubit<RouteUnlockState> {
  final GetTodayUnlockRequestUseCase _getToday;
  final RequestRouteUnlockUseCase _request;
  final CancelRouteUnlockUseCase _cancel;
  final ConnectivityService _connectivity;

  /// Whether the device is currently filtering outlets by distance.
  final bool Function() _isProximityEnforced;

  /// Re-sync today's outlets. [request] is the approved request when there is
  /// one (it carries the route), or null when the server only said "already
  /// exempt".
  final void Function(RouteUnlockRequest? request) _onResyncNeeded;

  /// Requests already healed once. If a re-sync still comes back enforced,
  /// asking again on every picker open would just hammer the server.
  final Set<int> _healed = {};
  bool _healedExempt = false;

  RouteUnlockCubit({
    required GetTodayUnlockRequestUseCase getToday,
    required RequestRouteUnlockUseCase requestUnlock,
    required CancelRouteUnlockUseCase cancelUnlock,
    required ConnectivityService connectivity,
    required bool Function() isProximityEnforced,
    required void Function(RouteUnlockRequest? request) onResyncNeeded,
  })  : _getToday = getToday,
        _request = requestUnlock,
        _cancel = cancelUnlock,
        _connectivity = connectivity,
        _isProximityEnforced = isProximityEnforced,
        _onResyncNeeded = onResyncNeeded,
        super(const RouteUnlockState());

  /// Fetches today's request. Failures are silent: the strip simply keeps what
  /// it last knew, and offline the rep can still see the request button.
  Future<void> load() async {
    if (state.loading) return;
    emit(state.copyWith(loading: true));
    try {
      final request = await _getToday();
      if (isClosed) return;
      emit(state.copyWith(
        request: request,
        clearRequest: request == null,
        loaded: true,
        loading: false,
      ));
      _maybeSelfHeal(request);
    } on AppException {
      if (!isClosed) emit(state.copyWith(loading: false));
    }
  }

  /// Sends a new request. Returns null on success, or the message to show.
  Future<String?> requestUnlock({
    required String reason,
    double? latitude,
    double? longitude,
    double? gpsAccuracyMeters,
  }) async {
    if (state.submitting) return null;
    if (!await _connectivity.hasInternet()) {
      emit(state.copyWith(error: routeUnlockOfflineMessage));
      return routeUnlockOfflineMessage;
    }
    emit(state.copyWith(submitting: true, clearError: true));
    try {
      final created = await _request(
        reason: reason.trim(),
        latitude: latitude,
        longitude: longitude,
        gpsAccuracyMeters: gpsAccuracyMeters,
      );
      emit(state.copyWith(request: created, loaded: true, submitting: false));
      return null;
    } on AppException catch (e) {
      final message = routeUnlockErrorMessage(e);
      emit(state.copyWith(submitting: false, error: message));
      if (e.code == 'ROUTE_UNLOCK_ALREADY_EXEMPT' && !_healedExempt) {
        // The server already relaxes this route; the device just hasn't heard.
        _healedExempt = true;
        _onResyncNeeded(null);
      } else if (e.code == 'ROUTE_UNLOCK_ALREADY_OPEN') {
        // Another device or an earlier tap got there first — show that one.
        await load();
      }
      return message;
    }
  }

  /// Withdraws the pending request. Returns null on success, or the message.
  Future<String?> cancel() async {
    final current = state.request;
    if (current == null || !current.isPending || state.submitting) return null;
    if (!await _connectivity.hasInternet()) {
      const message =
          'You are offline. Connect to the internet to cancel the request.';
      emit(state.copyWith(error: message));
      return message;
    }
    emit(state.copyWith(submitting: true, clearError: true));
    try {
      final updated = await _cancel(current.id, current.rowVersion);
      emit(state.copyWith(request: updated, submitting: false));
      return null;
    } on AppException catch (e) {
      final message = routeUnlockErrorMessage(e);
      emit(state.copyWith(submitting: false, error: message));
      // Most likely it was decided in the meantime — show what it became.
      if (routeUnlockNeedsReload(e)) await load();
      return message;
    }
  }

  void _maybeSelfHeal(RouteUnlockRequest? request) {
    if (request == null || !request.isLive) return;
    if (!_isProximityEnforced()) return;
    if (!_healed.add(request.id)) return;
    _onResyncNeeded(request);
  }
}
