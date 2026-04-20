import 'package:equatable/equatable.dart';
import 'package:uswatte/features/pricing/domain/entities/pricing_item.dart';

class PricingStructure extends Equatable {
  final int id;
  final String name;
  final bool isDefault;
  final List<PricingItem> items;

  const PricingStructure({
    required this.id,
    required this.name,
    required this.isDefault,
    required this.items,
  });

  @override
  List<Object?> get props => [id];
}
