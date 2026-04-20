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
  const SyncProductsRequested();
}
