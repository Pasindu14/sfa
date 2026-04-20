import 'package:equatable/equatable.dart';
import 'package:uswatte/features/pricing/domain/entities/pricing_structure.dart';

sealed class PricingState extends Equatable {
  const PricingState();

  @override
  List<Object?> get props => [];
}

final class PricingInitial extends PricingState {
  const PricingInitial();
}

final class PricingLoading extends PricingState {
  const PricingLoading();
}

final class PricingLoaded extends PricingState {
  final List<PricingStructure> structures;
  final bool isSyncing;
  final DateTime? lastSyncedAt;

  const PricingLoaded({
    required this.structures,
    required this.isSyncing,
    this.lastSyncedAt,
  });

  int get totalItemCount =>
      structures.fold(0, (sum, s) => sum + s.items.length);

  PricingLoaded copyWith({
    List<PricingStructure>? structures,
    bool? isSyncing,
    DateTime? lastSyncedAt,
  }) =>
      PricingLoaded(
        structures: structures ?? this.structures,
        isSyncing: isSyncing ?? this.isSyncing,
        lastSyncedAt: lastSyncedAt ?? this.lastSyncedAt,
      );

  @override
  List<Object?> get props => [structures, isSyncing, lastSyncedAt];
}

final class PricingError extends PricingState {
  final String message;

  const PricingError({required this.message});

  @override
  List<Object?> get props => [message];
}
