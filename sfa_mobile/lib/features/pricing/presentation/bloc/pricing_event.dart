import 'package:equatable/equatable.dart';

sealed class PricingEvent extends Equatable {
  const PricingEvent();

  @override
  List<Object?> get props => [];
}

final class LoadPricingRequested extends PricingEvent {
  const LoadPricingRequested();
}

final class SyncPricingRequested extends PricingEvent {
  const SyncPricingRequested();
}
