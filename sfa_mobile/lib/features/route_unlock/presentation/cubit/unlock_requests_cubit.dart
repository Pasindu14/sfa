import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/features/route_unlock/domain/entities/route_unlock_detail.dart';
import 'package:uswatte/features/route_unlock/domain/entities/route_unlock_request.dart';
import 'package:uswatte/features/route_unlock/domain/usecases/approve_unlock_usecase.dart';
import 'package:uswatte/features/route_unlock/domain/usecases/get_pending_unlock_count_usecase.dart';
import 'package:uswatte/features/route_unlock/domain/usecases/get_unlock_request_detail_usecase.dart';
import 'package:uswatte/features/route_unlock/domain/usecases/get_unlock_requests_usecase.dart';
import 'package:uswatte/features/route_unlock/domain/usecases/reject_unlock_usecase.dart';
import 'package:uswatte/features/route_unlock/domain/usecases/revoke_unlock_usecase.dart';
import 'package:uswatte/features/route_unlock/presentation/route_unlock_messages.dart';

class UnlockRequestsState extends Equatable {
  final bool loading;
  final bool loaded;

  /// A load failure with nothing to show yet.
  final String? error;

  /// Waiting on a decision, oldest first — the one waiting longest is on top.
  final List<RouteUnlockRequest> pending;

  /// Everything else from today, newest first.
  final List<RouteUnlockRequest> decided;

  /// Id of the request an approve/reject/revoke is in flight for.
  final int? actingOn;

  const UnlockRequestsState({
    this.loading = false,
    this.loaded = false,
    this.error,
    this.pending = const [],
    this.decided = const [],
    this.actingOn,
  });

  UnlockRequestsState copyWith({
    bool? loading,
    bool? loaded,
    String? error,
    bool clearError = false,
    List<RouteUnlockRequest>? pending,
    List<RouteUnlockRequest>? decided,
    int? actingOn,
    bool clearActingOn = false,
  }) =>
      UnlockRequestsState(
        loading: loading ?? this.loading,
        loaded: loaded ?? this.loaded,
        error: clearError ? null : (error ?? this.error),
        pending: pending ?? this.pending,
        decided: decided ?? this.decided,
        actingOn: clearActingOn ? null : (actingOn ?? this.actingOn),
      );

  @override
  List<Object?> get props => [loading, loaded, error, pending, decided, actingOn];
}

/// Outcome of a supervisor decision, for the sheet to show.
class UnlockActionResult {
  final bool success;
  final String message;

  /// The request changed underneath the sheet; the list has been reloaded and
  /// the sheet should close rather than act on its stale copy.
  final bool stale;

  const UnlockActionResult(this.success, this.message, {this.stale = false});
}

/// The supervisor's review queue: today's requests from their direct reports.
class UnlockRequestsCubit extends Cubit<UnlockRequestsState> {
  final GetUnlockRequestsUseCase _getRequests;
  final GetUnlockRequestDetailUseCase _getDetail;
  final ApproveUnlockUseCase _approve;
  final RejectUnlockUseCase _reject;
  final RevokeUnlockUseCase _revoke;

  UnlockRequestsCubit({
    required GetUnlockRequestsUseCase getRequests,
    required GetUnlockRequestDetailUseCase getDetail,
    required ApproveUnlockUseCase approve,
    required RejectUnlockUseCase reject,
    required RevokeUnlockUseCase revoke,
  })  : _getRequests = getRequests,
        _getDetail = getDetail,
        _approve = approve,
        _reject = reject,
        _revoke = revoke,
        super(const UnlockRequestsState());

  /// Loads today's requests in one call and splits them. Pending on the server
  /// means "pending and today", so filtering by today's date covers both lists.
  Future<void> load() async {
    emit(state.copyWith(loading: true, clearError: true));
    try {
      final today = DateTime.now();
      final all = await _getRequests(from: today, to: today, pageSize: 100);
      if (isClosed) return;
      final pending = all.where((r) => r.isPending).toList()
        ..sort((a, b) => a.requestedAt.compareTo(b.requestedAt));
      final decided = all.where((r) => !r.isPending).toList()
        ..sort((a, b) => _decidedAt(b).compareTo(_decidedAt(a)));
      emit(state.copyWith(
        loading: false,
        loaded: true,
        pending: pending,
        decided: decided,
      ));
    } on AppException catch (e) {
      if (!isClosed) {
        emit(state.copyWith(loading: false, error: routeUnlockErrorMessage(e)));
      }
    }
  }

  static DateTime _decidedAt(RouteUnlockRequest r) =>
      r.revokedAt ?? r.reviewedAt ?? r.cancelledAt ?? r.requestedAt;

  Future<RouteUnlockDetail> loadDetail(int id) => _getDetail(id);

  Future<UnlockActionResult> approve(RouteUnlockRequest r, {String? note}) =>
      _act(r, () => _approve(r.id, r.rowVersion, note: note), 'Unlock approved.');

  Future<UnlockActionResult> reject(RouteUnlockRequest r, String reason) =>
      _act(r, () => _reject(r.id, r.rowVersion, reason), 'Request rejected.');

  Future<UnlockActionResult> revoke(RouteUnlockRequest r, String reason) =>
      _act(r, () => _revoke(r.id, r.rowVersion, reason), 'Unlock revoked.');

  Future<UnlockActionResult> _act(
    RouteUnlockRequest r,
    Future<RouteUnlockRequest> Function() call,
    String successMessage,
  ) async {
    if (state.actingOn != null) {
      return const UnlockActionResult(false, 'Another action is in progress.');
    }
    emit(state.copyWith(actingOn: r.id));
    try {
      await call();
      emit(state.copyWith(clearActingOn: true));
      await load();
      return UnlockActionResult(true, successMessage);
    } on AppException catch (e) {
      emit(state.copyWith(clearActingOn: true));
      final stale = routeUnlockNeedsReload(e);
      if (stale) await load();
      return UnlockActionResult(false, routeUnlockErrorMessage(e), stale: stale);
    }
  }
}

/// Pending-today count for the supervisor home tile badge. Failures keep the
/// last known count — a missing badge is better than a wrong error on home.
class PendingUnlockCountCubit extends Cubit<int> {
  final GetPendingUnlockCountUseCase _getCount;

  PendingUnlockCountCubit(this._getCount) : super(0);

  Future<void> refresh() async {
    try {
      final count = await _getCount();
      if (!isClosed) emit(count);
    } on AppException {
      // keep the last value
    }
  }
}
