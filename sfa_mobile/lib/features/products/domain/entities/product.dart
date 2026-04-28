import 'package:equatable/equatable.dart';

class Product extends Equatable {
  final int id;
  final String code;
  final String itemDescription;
  final String? printDescription;
  final int piecesPerPack;
  final String? imageUrl;
  final int? categoryId;

  const Product({
    required this.id,
    required this.code,
    required this.itemDescription,
    this.printDescription,
    required this.piecesPerPack,
    this.imageUrl,
    this.categoryId,
  });

  @override
  List<Object?> get props => [
        id,
        code,
        itemDescription,
        printDescription,
        piecesPerPack,
        imageUrl,
        categoryId,
      ];
}
