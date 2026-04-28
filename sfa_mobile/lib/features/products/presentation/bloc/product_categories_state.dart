import 'package:equatable/equatable.dart';
import 'package:uswatte/features/products/domain/entities/product_category.dart';

sealed class ProductCategoriesState extends Equatable {
  const ProductCategoriesState();

  @override
  List<Object?> get props => [];
}

final class ProductCategoriesInitial extends ProductCategoriesState {
  const ProductCategoriesInitial();
}

final class ProductCategoriesLoading extends ProductCategoriesState {
  const ProductCategoriesLoading();
}

final class ProductCategoriesLoaded extends ProductCategoriesState {
  final List<ProductCategory> categories;
  final bool isSyncing;
  final DateTime? lastSyncedAt;

  const ProductCategoriesLoaded({
    required this.categories,
    required this.isSyncing,
    this.lastSyncedAt,
  });

  ProductCategoriesLoaded copyWith({
    List<ProductCategory>? categories,
    bool? isSyncing,
    DateTime? lastSyncedAt,
  }) =>
      ProductCategoriesLoaded(
        categories: categories ?? this.categories,
        isSyncing: isSyncing ?? this.isSyncing,
        lastSyncedAt: lastSyncedAt ?? this.lastSyncedAt,
      );

  @override
  List<Object?> get props => [categories, isSyncing, lastSyncedAt];
}

final class ProductCategoriesError extends ProductCategoriesState {
  final String message;

  const ProductCategoriesError({required this.message});

  @override
  List<Object?> get props => [message];
}
