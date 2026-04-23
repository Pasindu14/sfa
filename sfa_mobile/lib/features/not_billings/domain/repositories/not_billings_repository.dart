import 'package:uswatte/features/not_billings/domain/entities/not_billing.dart';

abstract class NotBillingsRepository {
  Future<NotBilling> createNotBilling(NotBilling record);
  Future<List<NotBilling>> getNotBillings({int? limit});
  Future<NotBilling?> getNotBillingById(String clientNotBillingId);
  Future<int> countPendingOrFailed();
  Future<void> deleteLocalNotBilling(String clientNotBillingId);
  Future<void> retrySync(String clientNotBillingId);
}
