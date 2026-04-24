import 'package:uswatte/features/create_outlet/domain/entities/new_outlet.dart';

abstract class CreateOutletRepository {
  Future<void> createOutlet(NewOutlet outlet);
}
