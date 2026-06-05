import 'package:equatable/equatable.dart';

class Product extends Equatable {
  final int id;
  final String code;
  final String itemDescription;
  final String? printDescription;
  final int piecesPerPack;
  final String? imageUrl;
  final int? categoryId;
  final double dealerPackPrice;
  final double dealerCasePrice;
  final double mrp;

  const Product({
    required this.id,
    required this.code,
    required this.itemDescription,
    this.printDescription,
    required this.piecesPerPack,
    this.imageUrl,
    this.categoryId,
    this.dealerPackPrice = 0,
    this.dealerCasePrice = 0,
    this.mrp = 0,
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
        dealerPackPrice,
        dealerCasePrice,
        mrp,
      ];
}
