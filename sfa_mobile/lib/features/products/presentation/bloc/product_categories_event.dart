import 'package:equatable/equatable.dart';

sealed class ProductCategoriesEvent extends Equatable {
  const ProductCategoriesEvent();

  @override
  List<Object?> get props => [];
}

/// Load local cache immediately, then kick off a background sync.
final class LoadProductCategoriesRequested extends ProductCategoriesEvent {
  const LoadProductCategoriesRequested();
}
