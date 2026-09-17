import 'package:equatable/equatable.dart';

sealed class ProductsEvent extends Equatable {
  const ProductsEvent();

  @override
  List<Object?> get props => [];
}

/// Load local cache immediately, then kick off a background sync.
final class LoadProductsRequested extends ProductsEvent {
  const LoadProductsRequested();
}

/// Explicit user-triggered sync (pull-to-refresh or sync button).
final class SyncProductsRequested extends ProductsEvent {
  /// Skip the stored ETag and always download the full list. The default
  /// still refreshes: an unchanged list just comes back as a cheap 304.
  final bool force;

  const SyncProductsRequested({this.force = false});

  @override
  List<Object?> get props => [force];
}
