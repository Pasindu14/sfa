import 'package:equatable/equatable.dart';

/// A named price list the rep can bill against. Only active structures reach
/// the device; exactly one of them is normally the default.
///
/// The per-product prices are not carried here — the bill product search joins
/// them straight from `price_structure_items` for the selected structure.
class PricingStructure extends Equatable {
  final int id;
  final String name;
  final bool isDefault;

  /// How many products carry a price in this structure.
  final int itemCount;

  const PricingStructure({
    required this.id,
    required this.name,
    required this.isDefault,
    this.itemCount = 0,
  });

  @override
  List<Object?> get props => [id, name, isDefault, itemCount];
}
