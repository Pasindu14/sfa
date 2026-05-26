import 'package:bloc_concurrency/bloc_concurrency.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/features/notifications/domain/entities/notification_entity.dart';
import 'package:uswatte/features/notifications/domain/usecases/get_notifications_usecase.dart';
import 'package:uswatte/features/notifications/domain/usecases/get_unread_count_usecase.dart';
import 'package:uswatte/features/notifications/domain/usecases/mark_all_read_usecase.dart';
import 'package:uswatte/features/notifications/domain/usecases/mark_notification_read_usecase.dart';

part 'notifications_event.dart';
part 'notifications_state.dart';

const _pageSize = 20;

class NotificationsBloc extends Bloc<NotificationsEvent, NotificationsState> {
  final GetNotificationsUseCase _getNotifications;
  final GetUnreadCountUseCase _getUnreadCount;
  final MarkNotificationReadUseCase _markRead;
  final MarkAllReadUseCase _markAllRead;

  NotificationsBloc({
    required GetNotificationsUseCase getNotifications,
    required GetUnreadCountUseCase getUnreadCount,
    required MarkNotificationReadUseCase markRead,
    required MarkAllReadUseCase markAllRead,
  })  : _getNotifications = getNotifications,
        _getUnreadCount = getUnreadCount,
        _markRead = markRead,
        _markAllRead = markAllRead,
        super(const NotificationsInitial()) {
    on<LoadNotifications>(_onLoad);
    on<LoadMoreNotifications>(_onLoadMore, transformer: droppable());
    on<MarkNotificationRead>(_onMarkRead, transformer: sequential());
    on<MarkAllNotificationsRead>(_onMarkAllRead, transformer: sequential());
  }

  Future<void> _onLoad(
      LoadNotifications event, Emitter<NotificationsState> emit) async {
    emit(const NotificationsLoading());
    try {
      final (items, totalCount) =
          await _getNotifications(page: 1, pageSize: _pageSize);
      final unreadCount = await _getUnreadCount();
      emit(NotificationsLoaded(
        items: items,
        page: 1,
        hasMore: items.length == _pageSize,
        unreadCount: unreadCount,
      ));
    } on AppException catch (e) {
      emit(NotificationsError(e.message));
    }
  }

  Future<void> _onLoadMore(
      LoadMoreNotifications event, Emitter<NotificationsState> emit) async {
    final s = state;
    if (s is! NotificationsLoaded || !s.hasMore) return;

    final nextPage = s.page + 1;
    emit(NotificationsLoadingMore(items: s.items, page: s.page));
    try {
      final (newItems, _) =
          await _getNotifications(page: nextPage, pageSize: _pageSize);
      emit(NotificationsLoaded(
        items: [...s.items, ...newItems],
        page: nextPage,
        hasMore: newItems.length == _pageSize,
        unreadCount: s.unreadCount,
      ));
    } on AppException catch (e) {
      // Restore previous loaded state on failure
      emit(NotificationsLoaded(
        items: s.items,
        page: s.page,
        hasMore: s.hasMore,
        unreadCount: s.unreadCount,
      ));
      emit(NotificationsError(e.message));
    }
  }

  Future<void> _onMarkRead(
      MarkNotificationRead event, Emitter<NotificationsState> emit) async {
    final s = state;
    if (s is! NotificationsLoaded) return;
    try {
      await _markRead(event.id);
      final updatedItems = s.items.map((n) {
        if (n.id == event.id) {
          return NotificationEntity(
            id: n.id,
            title: n.title,
            body: n.body,
            data: n.data,
            isRead: true,
            createdAt: n.createdAt,
          );
        }
        return n;
      }).toList();
      emit(s.copyWith(
        items: updatedItems,
        unreadCount: (s.unreadCount - 1).clamp(0, s.unreadCount),
      ));
    } on AppException {
      // Silently ignore — UI already looks responsive, no state change needed
    }
  }

  Future<void> _onMarkAllRead(
      MarkAllNotificationsRead event, Emitter<NotificationsState> emit) async {
    final s = state;
    if (s is! NotificationsLoaded) return;
    try {
      await _markAllRead();
      final updatedItems = s.items.map((n) => NotificationEntity(
            id: n.id,
            title: n.title,
            body: n.body,
            data: n.data,
            isRead: true,
            createdAt: n.createdAt,
          )).toList();
      emit(s.copyWith(items: updatedItems, unreadCount: 0));
    } on AppException catch (e) {
      emit(NotificationsError(e.message));
    }
  }
}
