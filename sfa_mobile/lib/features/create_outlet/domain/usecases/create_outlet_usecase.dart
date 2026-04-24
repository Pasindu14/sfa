import 'package:uswatte/features/create_outlet/domain/entities/new_outlet.dart';
import 'package:uswatte/features/create_outlet/domain/repositories/create_outlet_repository.dart';

class CreateOutletUseCase {
  final CreateOutletRepository _repo;

  const CreateOutletUseCase(this._repo);

  Future<void> call(NewOutlet outlet) => _repo.createOutlet(outlet);
}
