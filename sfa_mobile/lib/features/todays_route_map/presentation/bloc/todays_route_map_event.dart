import 'package:equatable/equatable.dart';

abstract class TodaysRouteMapEvent extends Equatable {
  const TodaysRouteMapEvent();
  @override
  List<Object?> get props => [];
}

class LoadTodaysRouteMapRequested extends TodaysRouteMapEvent {
  const LoadTodaysRouteMapRequested();
}
