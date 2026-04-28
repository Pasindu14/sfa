import 'package:equatable/equatable.dart';

class ProductCategory extends Equatable {
  final int id;
  final String name;
  final int sortOrder;

  const ProductCategory({
    required this.id,
    required this.name,
    required this.sortOrder,
  });

  @override
  List<Object?> get props => [id, name, sortOrder];
}
