import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_my_reps_usecase.dart';
import 'package:uswatte/features/supervisor_route_map/domain/entities/rep_last_location.dart';
import 'package:uswatte/features/supervisor_route_map/domain/usecases/get_rep_last_location_usecase.dart';
import 'package:uswatte/features/supervisor_route_map/domain/usecases/get_supervisor_route_map_usecase.dart';
import 'package:uswatte/features/supervisor_route_map/presentation/bloc/supervisor_route_map_bloc.dart';
import 'package:uswatte/features/supervisor_route_map/presentation/bloc/supervisor_route_map_event.dart';
import 'package:uswatte/features/supervisor_route_map/presentation/bloc/supervisor_route_map_state.dart';

class _MockReps extends Mock implements GetMyRepsUseCase {}

class _MockRouteMap extends Mock implements GetSupervisorRouteMapUseCase {}

class _MockLastLocation extends Mock implements GetRepLastLocationUseCase {}

const _rep = RepSummary(userId: 17, userName: 'Rep');

final _ping = RepLastLocation(
  latitude: 6.9271,
  longitude: 79.8612,
  accuracyMeters: 12,
  recordedAt: DateTime(2026, 9, 29, 10, 42),
);

void main() {
  setUpAll(() => registerFallbackValue(DateTime(2026)));

  test('fromJson reads the API DTO and converts to local time', () {
    final l = RepLastLocation.fromJson({
      'latitude': 6.9271,
      'longitude': 79.8612,
      'accuracy': 12.5,
      'recordedAt': '2026-09-29T05:12:00+00:00',
      'receivedAt': '2026-09-29T05:15:00+00:00',
    });
    expect(l.latitude, 6.9271);
    expect(l.accuracyMeters, 12.5);
    expect(l.recordedAt.isUtc, isFalse);
    expect(l.recordedAt.toUtc(), DateTime.utc(2026, 9, 29, 5, 12));
  });

  test('a fix older than 30 minutes is stale', () {
    expect(_ping.isStale(DateTime(2026, 9, 29, 11, 0)), isFalse);
    expect(_ping.isStale(DateTime(2026, 9, 29, 11, 30)), isTrue);
  });

  group('SupervisorRouteMapBloc last location', () {
    late _MockReps reps;
    late _MockRouteMap routeMap;
    late _MockLastLocation lastLocation;

    Future<SupervisorRouteMapBloc> ready() async {
      final bloc = SupervisorRouteMapBloc(
        getMyReps: reps,
        getRouteMap: routeMap,
        getLastLocation: lastLocation,
      );
      addTearDown(bloc.close);
      bloc.add(const SupervisorRouteMapRepsRequested());
      await pumpEventQueue();
      bloc.add(const SupervisorRouteMapRepSelected(_rep));
      await pumpEventQueue();
      return bloc;
    }

    setUp(() {
      reps = _MockReps();
      routeMap = _MockRouteMap();
      lastLocation = _MockLastLocation();
      when(() => reps()).thenAnswer((_) async => [_rep]);
      when(() => routeMap(17, any())).thenAnswer((_) async => []);
    });

    test('loads the last location alongside the outlets', () async {
      when(() => lastLocation(17)).thenAnswer((_) async => _ping);
      final bloc = await ready();

      bloc.add(const SupervisorRouteMapLoadRequested());
      await pumpEventQueue();

      final s = bloc.state as SupervisorRouteMapLoaded;
      expect(s.lastLocation, _ping);
      expect(s.lastLocationFailed, isFalse);
    });

    test('a failed location lookup still shows the map', () async {
      when(() => lastLocation(17)).thenThrow(Exception('404'));
      final bloc = await ready();

      bloc.add(const SupervisorRouteMapLoadRequested());
      await pumpEventQueue();

      final s = bloc.state as SupervisorRouteMapLoaded;
      expect(s.lastLocation, isNull);
      expect(s.lastLocationFailed, isTrue);
    });

    test('no ping yet is not a failure', () async {
      when(() => lastLocation(17)).thenAnswer((_) async => null);
      final bloc = await ready();

      bloc.add(const SupervisorRouteMapLoadRequested());
      await pumpEventQueue();

      final s = bloc.state as SupervisorRouteMapLoaded;
      expect(s.lastLocation, isNull);
      expect(s.lastLocationFailed, isFalse);
    });
  });
}
