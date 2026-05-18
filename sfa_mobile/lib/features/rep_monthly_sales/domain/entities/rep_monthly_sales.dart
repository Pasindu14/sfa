class RepMonthlySales {
  final int year;
  final int month;
  final double totalSales;
  final double pendingTotal;

  const RepMonthlySales({
    required this.year,
    required this.month,
    required this.totalSales,
    this.pendingTotal = 0.0,
  });
}
