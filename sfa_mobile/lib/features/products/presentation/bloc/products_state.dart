import 'package:equatable/equatable.dart';
import 'package:uswatte/features/products/domain/entities/product.dart';

sealed class ProductsState extends Equatable {
  const ProductsState();

  @override
  List<Object?> get props => [];
}

final class ProductsInitial extends ProductsState {
  const ProductsInitial();
}

final class ProductsLoading extends ProductsState {
  const ProductsLoading();
}

final class ProductsLoaded extends ProductsState {
  final List<Product> products;

  /// True while a background API sync is in progress.
  final bool isSyncing;

  /// Timestamp from the server's cached response; null until first successful sync.
  final DateTime? lastSyncedAt;

  const ProductsLoaded({
    required this.products,
    required this.isSyncing,
    this.lastSyncedAt,
  });

  ProductsLoaded copyWith({
    List<Product>? products,
    bool? isSyncing,
    DateTime? lastSyncedAt,
  }) =>
      ProductsLoaded(
        products: products ?? this.products,
        isSyncing: isSyncing ?? this.isSyncing,
        lastSyncedAt: lastSyncedAt ?? this.lastSyncedAt,
      );

  @override
  List<Object?> get props => [products, isSyncing, lastSyncedAt];
}

final class ProductsError extends ProductsState {
  final String message;

  const ProductsError({required this.message});

  @override
  List<Object?> get props => [message];
}
