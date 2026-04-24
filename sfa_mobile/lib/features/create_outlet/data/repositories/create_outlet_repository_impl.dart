import 'package:uswatte/features/create_outlet/data/datasources/create_outlet_remote_datasource.dart';
import 'package:uswatte/features/create_outlet/domain/entities/new_outlet.dart';
import 'package:uswatte/features/create_outlet/domain/repositories/create_outlet_repository.dart';

class CreateOutletRepositoryImpl implements CreateOutletRepository {
  final CreateOutletRemoteDatasource _remote;

  const CreateOutletRepositoryImpl(this._remote);

  @override
  Future<void> createOutlet(NewOutlet outlet) => _remote.createOutlet(outlet);
}
