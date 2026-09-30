import 'package:uswatte/core/errors/app_exception.dart';

/// Server error codes for this feature, in the rep's and supervisor's words.
/// Anything unmapped falls back to the server's own message.
String routeUnlockErrorMessage(AppException e) {
  switch (e.code) {
    case 'ROUTE_UNLOCK_NO_ASSIGNMENT':
      return 'You have no route assigned for today, so there is nothing to unlock.';
    case 'ROUTE_UNLOCK_ALREADY_EXEMPT':
      return 'Your route is already unlocked. Refreshing your outlets.';
    case 'ROUTE_UNLOCK_DAILY_LIMIT':
      return 'You have reached today\'s limit of unlock requests.';
    case 'ROUTE_UNLOCK_ALREADY_OPEN':
      return 'You already have an open unlock request for today.';
    case 'ROUTE_UNLOCK_EXPIRED':
      return 'This request is from an earlier day and can no longer be approved.';
    case 'ROUTE_UNLOCK_ASSIGNMENT_CHANGED':
      return 'The rep\'s route for today has changed since they asked. '
          'They need to send a new request.';
    case 'ROUTE_UNLOCK_INVALID_STATE':
      return 'This request has already been handled. Showing the latest status.';
    case 'ROUTE_UNLOCK_BUSY':
      return 'Someone else is updating this request. Try again in a moment.';
    case 'CONCURRENCY_CONFLICT':
      return 'This request changed while you were looking at it. '
          'Showing the latest status.';
    case 'FORBIDDEN_ACCESS':
      return 'You are not allowed to act on this request.';
    case 'VALIDATION_FAILED':
      if (e is ValidationException) {
        for (final messages in e.fields.values) {
          if (messages.isNotEmpty) return messages.first;
        }
      }
      return e.message;
    default:
      return e.message;
  }
}

/// Codes after which the on-screen copy of a request is stale and must be
/// reloaded before anyone acts on it again.
bool routeUnlockNeedsReload(AppException e) => const {
      'CONCURRENCY_CONFLICT',
      'ROUTE_UNLOCK_INVALID_STATE',
      'ROUTE_UNLOCK_EXPIRED',
      'ROUTE_UNLOCK_ASSIGNMENT_CHANGED',
      'ROUTE_UNLOCK_ALREADY_OPEN',
    }.contains(e.code);

const routeUnlockOfflineMessage =
    'You are offline. Connect to the internet to send an unlock request.';
