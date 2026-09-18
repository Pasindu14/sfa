// New Order is disabled only when the server has CONFIRMED the rep has no
// distributor. An unreachable server is "unknown", not "none" — the app bills
// offline, so a network failure must never lock the rep out.
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/features/rep_assignment/domain/entities/rep_assignment.dart';
import 'package:uswatte/features/rep_assignment/domain/usecases/get_rep_assignment_usecase.dart';
import 'package:uswatte/features/rep_assignment/presentation/bloc/rep_assignment_bloc.dart';

class _MockGet extends Mock implements GetRepAssignmentUseCase {}

Future<RepAssignmentState> _loadWith(_MockGet get) async {
  final bloc = RepAssignmentBloc(getRepAssignment: get)
    ..add(const LoadRepAssignmentRequested());
  final state = await bloc.stream.firstWhere(
    (s) => s is RepAssignmentLoaded || s is RepAssignmentError,
  );
  await bloc.close();
  return state;
}

void main() {
  late _MockGet get;
  setUp(() => get = _MockGet());

  test('territory with a distributor → enabled', () async {
    when(() => get()).thenAnswer((_) async =>
        const RepAssignment(territoryId: 3, distributorId: 9, distributorName: 'ABC'));
    expect((await _loadWith(get)).hasNoDistributor, isFalse);
  });

  test('territory without a distributor → disabled', () async {
    when(() => get()).thenAnswer((_) async => const RepAssignment(territoryId: 3));
    expect((await _loadWith(get)).hasNoDistributor, isTrue);
  });

  test('no assignment at all (404) → disabled', () async {
    when(() => get()).thenThrow(const NotFoundException(
        code: 'USERASSIGNMENT_NOT_FOUND', message: 'No assignment'));
    expect((await _loadWith(get)).hasNoDistributor, isTrue);
  });

  test('offline / network failure → stays enabled', () async {
    when(() => get()).thenThrow(
        const NetworkException(message: 'No internet connection.'));
    expect((await _loadWith(get)).hasNoDistributor, isFalse);
  });

  test('still loading → stays enabled', () {
    expect(const RepAssignmentLoading().hasNoDistributor, isFalse);
    expect(const RepAssignmentInitial().hasNoDistributor, isFalse);
  });
}
