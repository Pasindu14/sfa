class EditableOrderItem {
  final int productId;
  final String productCode;
  final String productDescription;
  final double unitPrice;
  int quantity;
  double discount;

  EditableOrderItem({
    required this.productId,
    required this.productCode,
    required this.productDescription,
    required this.unitPrice,
    required this.quantity,
    this.discount = 0,
  });

  double get lineTotal => quantity * unitPrice * (1 - discount / 100);
}
