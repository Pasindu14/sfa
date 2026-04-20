import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import 'package:uswatte/core/di/injection.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/bills/domain/entities/bill.dart';
import 'package:uswatte/features/bills/domain/entities/sync_status.dart';
import 'package:uswatte/features/bills/domain/usecases/get_bill_by_id_usecase.dart';
import 'package:uswatte/features/bills/presentation/bloc/bills_list_bloc.dart';
import 'package:uswatte/features/bills/presentation/bloc/bills_list_event.dart';

class BillDetailPage extends StatefulWidget {
  final String clientBillId;
  const BillDetailPage({super.key, required this.clientBillId});

  @override
  State<BillDetailPage> createState() => _BillDetailPageState();
}

class _BillDetailPageState extends State<BillDetailPage> {
  late Future<Bill?> _future;

  @override
  void initState() {
    super.initState();
    _future = getIt<GetBillByIdUseCase>().call(widget.clientBillId);
  }

  void _reload() {
    setState(() {
      _future = getIt<GetBillByIdUseCase>().call(widget.clientBillId);
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('Order Details'),
        backgroundColor: Colors.white,
        foregroundColor: AppColors.foreground,
        elevation: 0,
        shape: const Border(
            bottom: BorderSide(color: AppColors.surfaceVariant)),
      ),
      body: FutureBuilder<Bill?>(
        future: _future,
        builder: (ctx, snap) {
          if (!snap.hasData) {
            return const Center(child: CircularProgressIndicator());
          }
          final bill = snap.data;
          if (bill == null) {
            return const Center(child: Text('Bill not found.'));
          }
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              _Header(bill: bill),
              if (bill.syncStatus == SyncStatus.failed &&
                  bill.lastSyncError != null) ...[
                const SizedBox(height: 12),
                _ErrorPanel(
                  code: bill.lastSyncErrorCode ?? 'ERROR',
                  message: bill.lastSyncError!,
                ),
              ],
              const SizedBox(height: 16),
              const Text('Items',
                  style: TextStyle(fontWeight: FontWeight.w700)),
              const SizedBox(height: 6),
              ...bill.items.map((i) => _ItemRow(
                    productId: i.productId,
                    qty: i.quantity,
                    unitPrice: i.unitPrice,
                  )),
              const SizedBox(height: 16),
              _Totals(bill: bill),
              const SizedBox(height: 24),
              if (bill.syncStatus == SyncStatus.failed ||
                  bill.syncStatus == SyncStatus.pending)
                Row(
                  children: [
                    Expanded(
                      child: OutlinedButton.icon(
                        icon: const Icon(Icons.refresh),
                        label: const Text('Retry Sync'),
                        onPressed: () {
                          context
                              .read<BillsListBloc>()
                              .add(RetryBillRequested(bill.clientBillId));
                          _reload();
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(
                              content: Text('Retrying sync…'),
                              duration: Duration(seconds: 1),
                            ),
                          );
                        },
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: OutlinedButton.icon(
                        icon: const Icon(Icons.delete_outline,
                            color: AppColors.error),
                        label: const Text('Delete',
                            style: TextStyle(color: AppColors.error)),
                        style: OutlinedButton.styleFrom(
                          side: const BorderSide(color: AppColors.error),
                        ),
                        onPressed: () async {
                          final confirmed = await _confirmDelete(context);
                          if (!confirmed) return;
                          if (!context.mounted) return;
                          context
                              .read<BillsListBloc>()
                              .add(DeleteBillRequested(bill.clientBillId));
                          context.goNamed('bills');
                        },
                      ),
                    ),
                  ],
                ),
            ],
          );
        },
      ),
    );
  }

  Future<bool> _confirmDelete(BuildContext context) async {
    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Delete this bill?'),
        content: const Text(
            "This removes the bill from your device. It hasn't been synced yet, "
            "so the server won't be affected."),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: AppColors.error),
            onPressed: () => Navigator.of(ctx).pop(true),
            child: const Text('Delete'),
          ),
        ],
      ),
    );
    return result ?? false;
  }
}

class _Header extends StatelessWidget {
  final Bill bill;
  const _Header({required this.bill});

  @override
  Widget build(BuildContext context) {
    final title = bill.syncStatus == SyncStatus.synced
        ? (bill.serverBillNumber ?? '—')
        : 'Draft #${bill.clientBillId.substring(0, 6)}';
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          title,
          style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w700),
        ),
        const SizedBox(height: 4),
        Text(
          'Outlet #${bill.outletId}  ·  ${bill.billingType}',
          style: const TextStyle(color: AppColors.foregroundMuted),
        ),
      ],
    );
  }
}

class _ErrorPanel extends StatelessWidget {
  final String code;
  final String message;
  const _ErrorPanel({required this.code, required this.message});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: AppColors.error.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: AppColors.error.withValues(alpha: 0.4)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.error_outline,
                  color: AppColors.error, size: 18),
              const SizedBox(width: 6),
              Text(
                code,
                style: const TextStyle(
                  color: AppColors.error,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ],
          ),
          const SizedBox(height: 6),
          // message is a newline-joined list of per-product shortages so each
          // blocked product gets its own visual line.
          Text(
            message,
            style: const TextStyle(color: AppColors.error),
          ),
        ],
      ),
    );
  }
}

class _ItemRow extends StatelessWidget {
  final int productId;
  final double qty;
  final double unitPrice;
  const _ItemRow({
    required this.productId,
    required this.qty,
    required this.unitPrice,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        children: [
          Expanded(
            child: Text('Product #$productId'),
          ),
          Text('× ${qty.toStringAsFixed(qty.truncateToDouble() == qty ? 0 : 1)}'),
          const SizedBox(width: 14),
          SizedBox(
            width: 80,
            child: Text(
              'Rs. ${(qty * unitPrice).toStringAsFixed(2)}',
              textAlign: TextAlign.right,
              style: const TextStyle(fontWeight: FontWeight.w600),
            ),
          ),
        ],
      ),
    );
  }
}

class _Totals extends StatelessWidget {
  final Bill bill;
  const _Totals({required this.bill});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Column(
        children: [
          _line('Subtotal', 'Rs. ${bill.subTotalAmount.toStringAsFixed(2)}'),
          if (bill.billDiscountAmount > 0)
            _line('Discount (${bill.billDiscountRate.toStringAsFixed(1)}%)',
                '− Rs. ${bill.billDiscountAmount.toStringAsFixed(2)}'),
          const Divider(),
          _line('Total', 'Rs. ${bill.totalAmount.toStringAsFixed(2)}',
              bold: true),
        ],
      ),
    );
  }

  Widget _line(String label, String value, {bool bold = false}) {
    final style = TextStyle(
      fontWeight: bold ? FontWeight.w700 : FontWeight.w400,
      color: bold ? AppColors.foreground : AppColors.foregroundMuted,
    );
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        children: [
          Expanded(child: Text(label, style: style)),
          Text(value, style: style),
        ],
      ),
    );
  }
}
